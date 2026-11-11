#!/usr/bin/env python3
"""Render one or more pages of a licensed COBOL standard PDF (default: the canonical ISO 2023 one) to PNG for inspection.

The markdown `<a id="page-N">` anchors map 1:1 onto PDF pages, so N is passed directly.
Usage:  python3 scripts/render-spec-page.py <page> [<page> ...] [--dpi 300] [--out DIR] [--edition 1985|2002|2014|2023]

Each page is written to DIR/spec_p<N>.png and its path printed. Without --out, DIR is a NEW directory of this run's own
under the machine-wide temp directory (`spec-pages-XXXXXXXX`): the temp directory is one place for every agent on the
host, so a fixed name there would let two renders of one page overwrite each other (kb/Work PB2922). Read the path
this prints, never a guessed one.
"""
import argparse
import os
import sys
import tempfile

from standard_streams import ensure_utf8

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), 'spec'))
import spec_pdf  # noqa: E402  (scripts/spec: the one edition table, kb/Work PB4815)

def main(argv):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('pages', nargs='+', type=int, help='PDF page numbers')
    ap.add_argument('--dpi', type=int, default=300)
    ap.add_argument('--out', help='output directory (default: a new temp directory of this run)')
    spec_pdf.add_edition_argument(ap)
    args = ap.parse_args(argv)
    PDF = spec_pdf.require(args.edition)
    out = args.out
    if out is None:
        out = tempfile.mkdtemp(prefix='spec-pages-')
    else:
        os.makedirs(out, exist_ok=True)
    import pymupdf                                   # imported late: --help and a usage error run without PyMuPDF
    doc = pymupdf.open(PDF)
    for p in args.pages:
        if not (1 <= p <= doc.page_count):
            print(f'!! page {p} out of range (1..{doc.page_count})'); continue
        pix = doc[p-1].get_pixmap(dpi=args.dpi)          # anchor page-N == PDF page N
        f = os.path.join(out, f'spec_p{p}.png')
        pix.save(f)
        print(f'{f}  ({pix.width}x{pix.height} @ {args.dpi}dpi)')

if __name__ == '__main__':
    ensure_utf8()
    main(sys.argv[1:])
