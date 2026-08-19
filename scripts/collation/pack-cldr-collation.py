#!/usr/bin/env python3
"""
pack-cldr-collation.py — bundle the pinned CLDR collation data into the ONE embedded pack the runtime's CLDR locale
loader reads (src/Cobol.Net.Runtime/Collation/CLDR/CldrLocaleLoader.cs).

    python scripts/collation/pack-cldr-collation.py [--data data/unicode/cldr] [--out src/Cobol.Net.Runtime/Collation/CLDR/Data]

Inputs (pinned, committed, Unicode License — data/unicode/LICENSE-UNICODE.txt; provenance in data/unicode/SOURCES.md):
  <data>/collation/*.xml   every file of cldr/common/collation/ at the pinned release (135 files at release-48-2)
  <data>/bcp47/collation.xml   the BCP 47 "u" extension keys of collation (co/ka/kb/kc/kf/kh/kk/kn/kr/ks/kv) — the
                               drift test pins CldrLocaleTag's key table against it
  <data>/supplemental/supplementalData.xml   read for its <parentLocales> (the general table and the
                               component="collations" table): the non-truncating parents of the locale fallback
                               chain (nb → no, yue → zh_Hant, sr_Latn → root, …)

Output: <out>/cldr-collation.zip   a DETERMINISTIC zip (sorted entries, fixed timestamps, deflate) — same inputs,
                                   same bytes; entries "collation/<name>.xml" and "bcp47/collation.xml"
        <out>/cldr-collation.manifest.json   the release tag, every input's SHA-256, the pack's SHA-256, statistics

⚖ LEGAL: the pack holds Unicode CLDR data only. Nothing here reads or embeds ISO/IEC 14651 text.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import sys
import zipfile
from collections import OrderedDict

FIXED_TIME = (1980, 1, 1, 0, 0, 0)     # zip epoch — no build-time timestamp leaks into the artifact


def sha256_bytes(b: bytes) -> str:
    return hashlib.sha256(b).hexdigest()


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", default="data/unicode/cldr")
    ap.add_argument("--out", default="src/Cobol.Net.Runtime/Collation/CLDR/Data")
    ap.add_argument("--release", default=None, help="the CLDR release tag (default: read from <data>/RELEASE)")
    args = ap.parse_args()

    coll_dir = os.path.join(args.data, "collation")
    bcp47 = os.path.join(args.data, "bcp47", "collation.xml")
    supplemental = os.path.join(args.data, "supplemental", "supplementalData.xml")
    if not os.path.isdir(coll_dir):
        raise SystemExit(f"missing {coll_dir} — see data/unicode/SOURCES.md")
    for p in (bcp47, supplemental):
        if not os.path.exists(p):
            raise SystemExit(f"missing {p}")
    release = args.release
    if release is None:
        rel_file = os.path.join(args.data, "RELEASE")
        if not os.path.exists(rel_file):
            raise SystemExit(f"missing {rel_file} (one line: the CLDR release tag) — or pass --release")
        with open(rel_file, encoding="utf-8") as f:
            release = f.read().strip()

    entries = []                       # (arcname, bytes)
    for name in sorted(os.listdir(coll_dir)):
        if not name.endswith(".xml"):
            continue
        with open(os.path.join(coll_dir, name), "rb") as f:
            data = f.read()
        if b"<ldml" not in data:
            raise SystemExit(f"{name}: not an LDML file")
        entries.append((f"collation/{name}", data))
    with open(bcp47, "rb") as f:
        entries.append(("bcp47/collation.xml", f.read()))
    with open(supplemental, "rb") as f:
        entries.append(("supplemental/supplementalData.xml", f.read()))

    os.makedirs(args.out, exist_ok=True)
    out_zip = os.path.join(args.out, "cldr-collation.zip")
    with zipfile.ZipFile(out_zip, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as z:
        for arcname, data in entries:
            info = zipfile.ZipInfo(arcname, date_time=FIXED_TIME)
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = 0o644 << 16
            z.writestr(info, data)
    with open(out_zip, "rb") as f:
        pack = f.read()

    locales = [re.sub(r"\.xml$", "", os.path.basename(a)) for a, _ in entries if a.startswith("collation/")]
    manifest = OrderedDict(
        release=release,
        generator="scripts/collation/pack-cldr-collation.py",
        inputs=OrderedDict((a, sha256_bytes(d)) for a, d in entries),
        outputSha256=sha256_bytes(pack),
        stats=OrderedDict(files=len(locales), rawBytes=sum(len(d) for _, d in entries), packBytes=len(pack)),
        locales=locales,
    )
    with open(os.path.join(args.out, "cldr-collation.manifest.json"), "w", encoding="utf-8", newline="\n") as f:
        json.dump(manifest, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print(f"wrote {out_zip}: CLDR {release}, {len(locales)} collation files, {manifest['stats']['rawBytes']} raw / {len(pack)} packed bytes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
