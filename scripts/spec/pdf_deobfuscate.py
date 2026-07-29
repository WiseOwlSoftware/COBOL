#!/usr/bin/env python3
"""Recover the ISO PDF's lost character mapping and write a copy whose text extracts correctly.

WHAT IS ACTUALLY WRONG WITH THE PDF — it is not encryption, and calling it "obfuscated" led us astray for a long
time. 17 of the 26 fonts are **Type0 / Identity-H TrueType subsets carrying no `/ToUnicode` CMap**:

    <<  /BaseFont /MLLGNI+Cambria  /Encoding /Identity-H  /Subtype /Type0  /Type /Font  >>

Under `Identity-H` the character codes in the content stream ARE glyph indices into the subsetted font. A
`/ToUnicode` CMap is what tells a reader which Unicode character each glyph index represents, and this PDF has
none for those fonts — so every extractor falls back to emitting the raw glyph index as if it were a codepoint.
That is why the text layer reads as `\\x06\\x12\\x10\\x13\\x18\\x17\\x08` where the page says COMPUTE.

Nothing is scrambled and nothing is missing from the page: the mapping was simply never written down. It can be
recovered, and once recovered the standard becomes greppable — which turns the remaining conformance work from
"render the page and squint at it" into mechanical verification.

HOW THE MAPPING IS RECOVERED — and, importantly, WITHOUT consulting our own transcription. The markdown is the
artifact under repair; it cannot serve as its own reference. Instead:

  1. The subset fonts are Cambria, Arial, Times New Roman, Courier New, Calibri and Symbol — all stock Windows
     fonts present on this machine in full, unsubsetted form.
  2. Font subsetting COPIES glyph outlines verbatim. So for each glyph index in the subset, its outline
     (contour endpoints and point coordinates) matches exactly one glyph in the reference font.
  3. The reference font DOES have a `cmap`, giving that glyph's Unicode value.

So: subset GID -> outline -> reference glyph -> Unicode. Pure geometry, and independent of any transcription. The
subsetter stripped the `cmap` and `post` tables from the embedded fonts, which is why the mapping cannot simply be
read out of them.

WHAT COMES OUT. `--write` emits a copy of the PDF with a correct `/ToUnicode` CMap injected into each affected
font, so ordinary text extraction, copy-paste, and search all work. The original is never modified.

THE VERIFICATION IS THE POINT, so it is built to FAIL. `--verify` re-opens the written file and checks it against
facts about the standard that are true independently of anything in this repository — the title on the cover, and
statement names that must appear on their own clauses' pages. A high glyph-coverage percentage is NOT accepted as
evidence on its own: a mapping can be 99% covered and still wrong if it is offset.

    python scripts/spec/pdf_deobfuscate.py --report
    python scripts/spec/pdf_deobfuscate.py --write specs/ISO_COBOL.decoded.pdf --verify
"""
from __future__ import annotations

import argparse
import io
import pathlib
import shutil
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
PDF = next(iter(sorted((REPO / "specs-private").glob("*COBOL*.pdf"))), None)
WINFONTS = pathlib.Path("C:/Windows/Fonts")

# basefont stem (after the subset tag) -> (reference file, face index within a .ttc)
REFERENCES = {
    "cambria":              ("cambria.ttc", 0),
    "cambria,bold":         ("cambriab.ttf", 0),
    "cambria-bold":         ("cambriab.ttf", 0),
    "cambria,italic":       ("cambriai.ttf", 0),
    "cambria-italic":       ("cambriai.ttf", 0),
    "arial":                ("arial.ttf", 0),
    "arial,bold":           ("arialbd.ttf", 0),
    "arial,bolditalic":     ("arialbi.ttf", 0),
    "timesnewroman":        ("times.ttf", 0),
    "timesnewroman,bold":   ("timesbd.ttf", 0),
    "couriernew":           ("cour.ttf", 0),
    "couriernew,bold":      ("courbd.ttf", 0),
    "couriernew,italic":    ("couri.ttf", 0),
    "calibri":              ("calibri.ttf", 0),
    "symbol":               ("symbol.ttf", 0),
}

# Facts about the printed standard, used to prove the decode. None of these come from our transcription.
GROUND_TRUTH = [
    (1,   ["INTERNATIONAL STANDARD", "1989"]),
    (632, ["COMPUTE", "SIZE ERROR"]),
    (722, ["READ", "AT END", "INVALID KEY"]),
    (799, ["UNSTRING", "OVERFLOW"]),
    (687, ["MERGE", "COLLATING"]),
]


def _sig(glyf, name):
    """A glyph's outline signature. Subsetting copies outlines verbatim, so this is an exact key."""
    g = glyf[name]
    if g.numberOfContours == 0:
        return None
    if g.isComposite():
        return ("c", tuple((c.glyphName, round(c.x), round(c.y)) for c in g.components))
    return ("s", tuple(g.endPtsOfContours), tuple((round(x), round(y)) for x, y in g.coordinates))


_refcache: dict[str, dict] = {}
_advcache: dict[str, dict] = {}


def _reference_advance(stem: str, uni: int):
    """The advance width of one character in the stock font — used to identify blank glyphs such as SPACE."""
    if stem not in _advcache:
        from fontTools.ttLib import TTFont, TTCollection

        entry = REFERENCES.get(stem)
        if entry is None:
            _advcache[stem] = {}
        else:
            path = WINFONTS / entry[0]
            font = (TTCollection(str(path)).fonts[entry[1]] if path.suffix.lower() == ".ttc"
                    else TTFont(str(path)))
            cmap, hmtx = font.getBestCmap() or {}, font["hmtx"]
            _advcache[stem] = {u: hmtx[n][0] for u, n in cmap.items() if n in hmtx.metrics}
    return _advcache[stem].get(uni)


def _reference_signatures(stem: str):
    """Outline signature -> Unicode, for the full stock font matching this subset."""
    if stem in _refcache:
        return _refcache[stem]
    from fontTools.ttLib import TTFont, TTCollection

    entry = REFERENCES.get(stem)
    if entry is None:
        return None
    fname, face = entry
    path = WINFONTS / fname
    if not path.exists():
        return None
    font = TTCollection(str(path)).fonts[face] if path.suffix.lower() == ".ttc" else TTFont(str(path))
    if "glyf" not in font:
        return None                                   # a CFF/PostScript outline font: no glyf table to compare
    glyf = font["glyf"]
    cmap = font.getBestCmap()
    if not cmap:
        # Symbol-encoded fonts (Symbol, Wingdings) carry only a (3,0) table keyed in the 0xF000 private-use
        # block. Fold it down to the ASCII range it shadows, which is where the standard's symbols come from.
        cmap = {}
        for tbl in font["cmap"].tables:
            for k, v in tbl.cmap.items():
                cmap[k - 0xF000 if 0xF000 <= k <= 0xF0FF else k] = v
    if not cmap:
        return None
    sigs: dict = {}
    for uni, name in cmap.items():
        try:
            s = _sig(glyf, name)
        except Exception:  # noqa: BLE001
            continue
        if s is not None:
            sigs.setdefault(s, set()).add(uni)
    _refcache[stem] = sigs
    return sigs


def recover(doc, xref: int, basefont: str):
    """Recover {gid: unicode} for one Identity-H subset font, by matching outlines to the stock font."""
    from fontTools.ttLib import TTFont

    stem = basefont.split("+", 1)[-1].lower()
    try:
        sigs = _reference_signatures(stem)
    except Exception as e:  # noqa: BLE001
        return None, f"reference font unusable ({e.__class__.__name__}: {e})"
    if sigs is None:
        return None, f"no usable reference font for {stem!r}"
    try:
        buf = doc.extract_font(xref)[3]
        sub = TTFont(io.BytesIO(buf))
        if "glyf" not in sub:
            return None, "embedded subset has no glyf table"
        glyf, order = sub["glyf"], sub.getGlyphOrder()
    except Exception as e:  # noqa: BLE001
        return None, f"could not read embedded font ({e.__class__.__name__})"

    # A SPACE has no outline, so it can never be matched by shape — and without it the whole decode runs the
    # words together ("TheCOMPUTEstatementassigns"). Blank glyphs are therefore matched on ADVANCE WIDTH against
    # the reference font's own space. This is the one mapping that shape comparison structurally cannot make.
    blanks = 0
    try:
        ref_space = _reference_advance(stem, 0x20)
        sub_hmtx = sub["hmtx"]
    except Exception:  # noqa: BLE001
        ref_space, sub_hmtx = None, None

    mapping, outlined, ambiguous = {}, 0, 0
    for gid, name in enumerate(order):
        try:
            s = _sig(glyf, name)
        except Exception:  # noqa: BLE001
            continue
        if s is None:
            if ref_space is not None and sub_hmtx is not None and gid > 0:
                try:
                    if sub_hmtx[name][0] == ref_space:
                        mapping[gid] = 0x20
                        blanks += 1
                except Exception:  # noqa: BLE001
                    pass
            continue
        outlined += 1
        cands = sigs.get(s)
        if not cands:
            continue
        if len(cands) > 1:
            ambiguous += 1
            # Identical outlines shared by several codepoints: prefer the plain ASCII reading.
            ascii_ = sorted(c for c in cands if 0x20 <= c < 0x7F)
            mapping[gid] = ascii_[0] if ascii_ else min(cands)
        else:
            mapping[gid] = next(iter(cands))
    return mapping, f"{len(mapping)}/{outlined + blanks} glyphs mapped ({blanks} blank, {ambiguous} ambiguous)"


def _cmap_stream(mapping: dict) -> bytes:
    """Build a /ToUnicode CMap. bfchar blocks are capped at 100 entries, per the PDF specification."""
    items = sorted(mapping.items())
    head = ("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n"
            "/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n"
            "/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n"
            "1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n")
    body = []
    for i in range(0, len(items), 100):
        chunk = items[i:i + 100]
        body.append(f"{len(chunk)} beginbfchar\n")
        body += [f"<{g:04X}> <{u:04X}>\n" for g, u in chunk]
        body.append("endbfchar\n")
    tail = "endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n"
    return (head + "".join(body) + tail).encode("latin-1")


def collect_fonts(doc):
    """Every Identity-H subset font in the document, de-duplicated by xref."""
    seen = {}
    for pno in range(doc.page_count):
        for f in doc[pno].get_fonts(full=True):
            xref, ext, typ, base, _name, enc = f[0], f[1], f[2], f[3], f[4], f[5]
            if typ == "Type0" and enc == "Identity-H" and ext == "ttf":
                seen.setdefault(xref, base)
    return seen


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true", help="show per-font recovery coverage")
    ap.add_argument("--write", metavar="OUT", help="write a decoded copy of the PDF")
    ap.add_argument("--verify", action="store_true", help="prove the decode against facts about the standard")
    args = ap.parse_args()
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except Exception:  # noqa: BLE001
        pass
    if PDF is None or not PDF.exists():
        sys.exit("FATAL: the ISO PDF was not found under specs-private/. It is licensed per-copy and lives in a PRIVATE submodule; the public repository carries only the Markdown transcription at specs/ISO_COBOL.md. This tool measures the printed page, so it needs the PDF: "
             "git submodule update --init specs-private")
    import fitz

    doc = fitz.open(PDF)
    fonts = collect_fonts(doc)
    print(f"{len(fonts)} Identity-H subset font(s) with no /ToUnicode\n")

    maps = {}
    for xref, base in sorted(fonts.items(), key=lambda kv: kv[1]):
        mapping, detail = recover(doc, xref, base)
        status = "ok " if mapping else "FAIL"
        print(f"  [{status}] {base:<30} {detail}")
        if mapping:
            maps[xref] = mapping
    if not maps:
        sys.exit("FATAL: no font mapping could be recovered")
    unmapped = [b for x, b in fonts.items() if x not in maps]
    if unmapped:
        print(f"\n  NOT RECOVERED: {unmapped}")

    if args.report and not args.write:
        return 0
    if not args.write:
        ap.error("give --report, or --write OUT")

    # Write INCREMENTALLY. A full save() re-serialises every object and expands the publisher's object streams,
    # doubling the file (9.4 MB -> 18.7 MB) — which matters because this PDF is version-controlled. An
    # incremental save copies the original byte-for-byte and appends only the new ToUnicode objects, so the
    # publisher's own bytes are preserved verbatim and the delta is a few hundred KB.
    out = pathlib.Path(args.write)
    out.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(PDF, out)
    work = fitz.open(str(out))
    for xref, mapping in maps.items():
        tu = work.get_new_xref()
        work.update_object(tu, "<<>>")
        work.update_stream(tu, _cmap_stream(mapping))
        work.xref_set_key(xref, "ToUnicode", f"{tu} 0 R")
    work.save(str(out), incremental=True, encryption=fitz.PDF_ENCRYPT_KEEP)
    work.close()
    print(f"\nwrote {out}  ({out.stat().st_size / 1e6:.1f} MB)")

    if not args.verify:
        return 0

    # The decode is only believable if this check is capable of failing. Glyph coverage is NOT proof: a mapping
    # can be fully covered and still uniformly wrong. These strings are properties of the printed standard.
    print("\n=== verification against the printed standard ===")
    chk = fitz.open(str(out))
    failures = []
    for pno, expected in GROUND_TRUTH:
        # Collapse whitespace: the cover sets "INTERNATIONAL STANDARD" across two lines, and figures are laid
        # out with wide inter-word gaps. Line breaks are typesetting, not evidence about the mapping.
        text = " ".join(chk[pno - 1].get_text().split()).upper()
        missing = [e for e in expected if e.upper() not in text]
        print(f"  p{pno:<5} expect {expected}  ->  {'ok' if not missing else 'MISSING ' + str(missing)}")
        if missing:
            failures.append((pno, missing))
    if failures:
        sys.exit(f"\nFATAL: the decoded text does not match the printed standard: {failures}")
    print("\nall ground-truth checks passed — the decoded PDF is greppable")
    return 0


if __name__ == "__main__":
    sys.exit(main())
