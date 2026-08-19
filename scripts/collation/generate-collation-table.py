#!/usr/bin/env python3
"""
generate-collation-table.py — build COBOL.NET's DERIVED root collation table from Unicode CLDR / UCA data.

    python scripts/collation/generate-collation-table.py [--data data/unicode] [--out src/Cobol.Net.Runtime/Collation/Data]

Inputs (all pinned, all committed under data/unicode/, all under the Unicode License — data/unicode/LICENSE-UNICODE.txt):
  allkeys_CLDR.txt   the CLDR ROOT collation in DUCET format (cldr/common/uca, release tag in SOURCES.md) — the weights
  allkeys.txt        the DUCET of the same UCA version — read ONLY for its @version cross-check (the CLDR file's own
                     @version is authoritative; a mismatch aborts, so the two sources can never drift silently)
  UnicodeData.txt    canonical combining classes (field 3) → the NON-STARTER set the runtime needs for canonical
                     reordering / discontiguous contractions; canonical decomposition mappings (field 5) → the
                     runtime's OWN NFD (so canonical equivalence never depends on the host's Unicode version); the
                     First/Last ranges → ASSIGNED code points of the siniform blocks (UTS #10 Table 16)
  PropList.txt       Unified_Ideograph → the Han implicit-weight sets (UTS #10 Table 16)
  Blocks.txt         the block boundaries Table 16 names

Output: <out>/root-collation.bin (the table, deflate-compressed; format documented in CollationTable.cs and README.md)
        <out>/root-collation.manifest.json (versions, input hashes, statistics — the drift test reads it)

⚖ LEGAL: this program never reads, copies or embeds ISO/IEC 14651 text or its Common Template Table. Every weight
comes from the Unicode data files above; the output is a DERIVED table (primaries are re-scaled by
PRIMARY_SHIFT so tailorings have room to insert between adjacent root primaries; the order is preserved exactly).
The COBOL.NET conformance statement (owner decision Q4, 2026-08-18, verbatim): "Implements collation behavior
consistent with ISO/IEC 14651 through derived tables and CLDR/UCA data."
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import struct
import sys
import zlib
from collections import OrderedDict

FORMAT_VERSION = 1
PRIMARY_SHIFT = 4          # stored primaries are the raw 16-bit CLDR values; the runtime shifts them left by this
MAGIC = b"CNCT"            # "COBOL.NET Collation Table"

CE_RE = re.compile(r"\[([.*])([0-9A-Fa-f]{4})\.([0-9A-Fa-f]{4})\.([0-9A-Fa-f]{4})\]")
LINE_RE = re.compile(r"^([0-9A-Fa-f]{4,6}(?:\s+[0-9A-Fa-f]{4,6})*)\s*;\s*((?:\[[.*][0-9A-Fa-f.]+\])+)\s*(?:#.*)?$")


def sha256(path: str) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


def read_version(path: str) -> str:
    with open(path, encoding="utf-8") as f:
        for line in f:
            if line.startswith("@version"):
                return line.split()[1].strip()
    raise SystemExit(f"{path}: no @version line")


def parse_allkeys(path: str):
    """→ (version, [(cps: tuple[int,...], ces: tuple[(p,s,t,var)...])...])"""
    version = None
    entries = []
    with open(path, encoding="utf-8") as f:
        for lineno, raw in enumerate(f, 1):
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            if line.startswith("@version"):
                version = line.split()[1]
                continue
            if line.startswith("@"):
                continue                       # @implicitweights etc. — the runtime derives implicit weights from Table 16
            m = LINE_RE.match(line)
            if not m:
                raise SystemExit(f"{path}:{lineno}: unrecognized line: {line[:80]}")
            cps = tuple(int(x, 16) for x in m.group(1).split())
            ces = tuple((int(p, 16), int(s, 16), int(t, 16), v == "*") for v, p, s, t in CE_RE.findall(m.group(2)))
            if not ces:
                raise SystemExit(f"{path}:{lineno}: no collation elements: {line[:80]}")
            entries.append((cps, ces))
    if version is None:
        raise SystemExit(f"{path}: no @version line")
    return version, entries


def parse_unicode_data(path: str):
    """→ (ccc: {cp: ccc≠0},
         assigned_ranges: [(first,last)] covering every assigned code point,
         nfd: {cp: [cp...]} — the FULL canonical decomposition (recursively expanded, canonically reordered) of every
         code point with a canonical decomposition mapping: field 5 without a <compatibility> tag. Hangul syllables
         are algorithmic and excluded.)"""
    ccc = {}
    assigned = []
    direct = {}
    pending_first = None
    with open(path, encoding="utf-8") as f:
        for raw in f:
            fields = raw.rstrip("\r\n").split(";")
            if len(fields) < 6:
                continue
            cp = int(fields[0], 16)
            name = fields[1]
            c = int(fields[3])
            if name.endswith(", First>"):
                pending_first = cp
                continue
            if name.endswith(", Last>"):
                assigned.append((pending_first, cp))
                pending_first = None
                continue
            assigned.append((cp, cp))
            if c:
                ccc[cp] = c
            d = fields[5]
            if d and not d.startswith("<"):
                direct[cp] = [int(x, 16) for x in d.split()]
    assigned.sort()
    # coalesce
    merged = []
    for a, b in assigned:
        if merged and a <= merged[-1][1] + 1:
            merged[-1] = (merged[-1][0], max(merged[-1][1], b))
        else:
            merged.append((a, b))

    def expand(cp, depth=0):
        if depth > 8:
            raise SystemExit(f"canonical decomposition of U+{cp:04X} does not terminate")
        if cp not in direct:
            return [cp]
        out = []
        for d in direct[cp]:
            out.extend(expand(d, depth + 1))
        return out

    def reorder(cps):
        # Canonical Ordering Algorithm (The Unicode Standard §3.11): stable-sort every maximal run of non-starters by ccc.
        cps = list(cps)
        i = 0
        while i < len(cps):
            if ccc.get(cps[i], 0) == 0:
                i += 1
                continue
            j = i
            while j < len(cps) and ccc.get(cps[j], 0) != 0:
                j += 1
            cps[i:j] = sorted(cps[i:j], key=lambda x: ccc[x])   # Python's sort is stable
            i = j
        return cps

    nfd = {cp: reorder(expand(cp)) for cp in direct}
    return ccc, merged, nfd


def parse_ranges_file(path: str, prop: str | None = None):
    """Blocks.txt (prop=None → {name: (first,last)}) or PropList.txt (prop given → [(first,last)])."""
    out = {} if prop is None else []
    rng = re.compile(r"^([0-9A-Fa-f]{4,6})(?:\.\.([0-9A-Fa-f]{4,6}))?\s*;\s*([^#]+?)\s*(?:#.*)?$")
    with open(path, encoding="utf-8") as f:
        for raw in f:
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            m = rng.match(line)
            if not m:
                continue
            first = int(m.group(1), 16)
            last = int(m.group(2), 16) if m.group(2) else first
            name = m.group(3).strip()
            if prop is None:
                out[name] = (first, last)
            elif name == prop:
                out.append((first, last))
    return out


def intersect(ranges_a, ranges_b):
    out = []
    for a0, a1 in ranges_a:
        for b0, b1 in ranges_b:
            lo, hi = max(a0, b0), min(a1, b1)
            if lo <= hi:
                out.append((lo, hi))
    out.sort()
    return out


def subtract(ranges, holes):
    out = []
    for a0, a1 in ranges:
        cur = [(a0, a1)]
        for h0, h1 in holes:
            nxt = []
            for c0, c1 in cur:
                if h1 < c0 or h0 > c1:
                    nxt.append((c0, c1))
                    continue
                if c0 < h0:
                    nxt.append((c0, h0 - 1))
                if h1 < c1:
                    nxt.append((h1 + 1, c1))
            cur = nxt
        out.extend(cur)
    out.sort()
    return out


def implicit_ranges(blocks, unified, assigned):
    """UTS #10 (rev. 53, UCA 17.0) Table 16 — Computing Implicit Weights. Kind 0 = siniform (AAAA = base,
    BBBB = (cp − subtract) | 0x8000); kind 1 = core Han (AAAA = 0xFB40 + (cp >> 15)); kind 2 = other Han
    (0xFB80 + (cp >> 15)). Everything else is 'unassigned/other' (0xFBC0 + (cp >> 15)) and needs no range."""
    def block(name):
        if name not in blocks:
            raise SystemExit(f"Blocks.txt: block '{name}' not found — Table 16 names it")
        return [blocks[name]]

    out = []
    siniform = [
        # (blocks, base, subtract) — the four rows of Table 16's siniform section
        (["Tangut", "Tangut Supplement"], 0xFB00, 0x17000),
        (["Tangut Components", "Tangut Components Supplement"], 0xFB01, 0x18800),
        (["Nushu"], 0xFB02, 0x1B170),
        (["Khitan Small Script"], 0xFB03, 0x18B00),
    ]
    for names, base, sub in siniform:
        blk = []
        for n in names:
            blk.extend(block(n))
        for lo, hi in intersect(blk, assigned):          # "Assigned code points in Block=…"
            out.append((lo, hi, base, sub, 0))
    core_blocks = block("CJK Unified Ideographs") + block("CJK Compatibility Ideographs")
    core = intersect(unified, core_blocks)
    other = subtract(unified, core)
    for lo, hi in core:
        out.append((lo, hi, 0xFB40, 0, 1))
    for lo, hi in other:
        out.append((lo, hi, 0xFB80, 0, 2))
    out.sort()
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", default="data/unicode")
    ap.add_argument("--out", default="src/Cobol.Net.Runtime/Collation/Data")
    args = ap.parse_args()

    p_cldr = os.path.join(args.data, "allkeys_CLDR.txt")
    p_ducet = os.path.join(args.data, "allkeys.txt")
    p_ud = os.path.join(args.data, "UnicodeData.txt")
    p_props = os.path.join(args.data, "PropList.txt")
    p_blocks = os.path.join(args.data, "Blocks.txt")
    for p in (p_cldr, p_ducet, p_ud, p_props, p_blocks):
        if not os.path.exists(p):
            raise SystemExit(f"missing input {p} — see data/unicode/SOURCES.md for the pinned URLs")

    version, entries = parse_allkeys(p_cldr)
    ducet_version = read_version(p_ducet)
    if ducet_version != version:
        raise SystemExit(f"UCA version skew: allkeys_CLDR.txt is {version}, allkeys.txt is {ducet_version}")
    ccc, assigned, nfd = parse_unicode_data(p_ud)
    blocks = parse_ranges_file(p_blocks)
    unified = parse_ranges_file(p_props, "Unified_Ideograph")
    ranges = implicit_ranges(blocks, unified, assigned)

    # ---- element pool (deduplicated by sequence) -------------------------------------------------------------
    pool: list[tuple[int, int, int, bool]] = []
    seq_index: dict[tuple, int] = {}

    def intern(ces):
        if ces in seq_index:
            return seq_index[ces]
        off = len(pool)
        pool.extend(ces)
        seq_index[ces] = off
        return off

    singles: dict[int, tuple[int, int]] = {}
    contractions: list[tuple[tuple[int, ...], int, int]] = []
    max_p = max_s = max_t = max_len = 0
    variable_count = 0
    for cps, ces in entries:
        for p, s, t, v in ces:
            if p > 0xFFFF or s > 0xFFFF or t > 0xFF:
                raise SystemExit(f"weight out of storage range for {cps}: {ces}")
            max_p, max_s, max_t = max(max_p, p), max(max_s, s), max(max_t, t)
            variable_count += v
        max_len = max(max_len, len(ces))
        off = intern(ces)
        if len(cps) == 1:
            if cps[0] in singles:
                raise SystemExit(f"duplicate mapping for U+{cps[0]:04X}")
            singles[cps[0]] = (off, len(ces))
        else:
            contractions.append((cps, off, len(ces)))
    if max_len > 255:
        raise SystemExit("element count per mapping exceeds one byte")

    # Every contraction's first code point must ALSO have a single mapping (or the runtime's longest-match walk
    # would have to fall back to the implicit rule mid-contraction). UCA guarantees it; verify rather than assume.
    for cps, _, _ in contractions:
        if cps[0] not in singles:
            raise SystemExit(f"contraction {['U+%04X' % c for c in cps]} whose first code point has no single mapping")

    # ---- serialize --------------------------------------------------------------------------------------------
    source_tag = f"CLDR allkeys_CLDR.txt (UCA {version}) + UCD {version}"
    body = bytearray()
    body += struct.pack("<HB", FORMAT_VERSION, PRIMARY_SHIFT)
    for s in (version, source_tag):
        b = s.encode("utf-8")
        body += struct.pack("<B", len(b)) + b
    body += struct.pack("<I", len(pool))
    for p, s, t, v in pool:
        body += struct.pack("<HHBB", p, s, t, 1 if v else 0)
    body += struct.pack("<I", len(singles))
    for cp in sorted(singles):
        off, n = singles[cp]
        body += struct.pack("<IIB", cp, off, n)
    body += struct.pack("<I", len(contractions))
    for cps, off, n in sorted(contractions):
        body += struct.pack("<B", len(cps))
        for cp in cps:
            body += struct.pack("<I", cp)
        body += struct.pack("<IB", off, n)
    body += struct.pack("<I", len(ccc))
    for cp in sorted(ccc):
        body += struct.pack("<IB", cp, ccc[cp])
    body += struct.pack("<H", len(ranges))
    for lo, hi, base, sub, kind in ranges:
        body += struct.pack("<IIHIB", lo, hi, base, sub, kind)
    body += struct.pack("<I", len(nfd))
    for cp in sorted(nfd):
        seq = nfd[cp]
        if len(seq) > 255:
            raise SystemExit("decomposition longer than one byte")
        body += struct.pack("<IB", cp, len(seq))
        for d in seq:
            body += struct.pack("<I", d)

    compressor = zlib.compressobj(9, zlib.DEFLATED, -15)      # raw deflate — what .NET's DeflateStream reads
    payload = compressor.compress(bytes(body)) + compressor.flush()
    blob = MAGIC + struct.pack("<I", len(body)) + payload

    os.makedirs(args.out, exist_ok=True)
    out_bin = os.path.join(args.out, "root-collation.bin")
    with open(out_bin, "wb") as f:
        f.write(blob)

    stats = OrderedDict(
        entries=len(entries), singleMappings=len(singles), contractions=len(contractions),
        maxContractionLength=max(len(c[0]) for c in contractions) if contractions else 1,
        elementPool=len(pool), maxElementsPerMapping=max_len, variableElements=variable_count,
        maxPrimary=f"0x{max_p:04X}", maxSecondary=f"0x{max_s:04X}", maxTertiary=f"0x{max_t:02X}",
        nonStarters=len(ccc), implicitRanges=len(ranges),
        canonicalDecompositions=len(nfd), maxDecompositionLength=max(len(v) for v in nfd.values()),
        rawBytes=len(body), compressedBytes=len(blob),
    )
    manifest = OrderedDict(
        format=FORMAT_VERSION, primaryShift=PRIMARY_SHIFT, ucaVersion=version, sourceTag=source_tag,
        generator="scripts/collation/generate-collation-table.py",
        inputs=OrderedDict((os.path.basename(p), sha256(p)) for p in (p_cldr, p_ducet, p_ud, p_props, p_blocks)),
        outputSha256=hashlib.sha256(blob).hexdigest(),
        stats=stats,
        implicitRanges=[OrderedDict(first=f"U+{lo:04X}", last=f"U+{hi:04X}", base=f"0x{base:04X}",
                                    subtract=f"0x{sub:04X}", kind=("siniform", "han-core", "han-other")[kind])
                        for lo, hi, base, sub, kind in ranges],
    )
    with open(os.path.join(args.out, "root-collation.manifest.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, indent=2)
        f.write("\n")

    print(f"wrote {out_bin}: UCA {version}, {len(singles)} single mappings, {len(contractions)} contractions, "
          f"{len(pool)} pooled elements, {len(ccc)} non-starters, {len(ranges)} implicit ranges, "
          f"{len(nfd)} canonical decompositions, {len(body)} raw / {len(blob)} compressed bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
