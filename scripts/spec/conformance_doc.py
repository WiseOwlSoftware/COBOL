#!/usr/bin/env python3
"""conformance_doc.py — renders the conformance statement, docs/CONFORMANCE.md, from its sources under docs/conformance/
(kb/Work PB5430; DESIGN-orchestrator-loop.md section 16.8, step B2).

    python scripts/spec/conformance_doc.py --out <path>   # write the rendered whole to <path>
    python scripts/spec/conformance_doc.py --check        # render in memory: every source out of shape named, exit 1 on any

The whole is a VIEW: `scripts/rendered_views.py`'s `conformance` row runs `--out` into the gitignored `docs/CONFORMANCE.md`
and writes the stamp that makes a hand edit of the view visible (its rule (e)); every NuGet package renders it the same way
at pack time, unstamped (`Directory.Build.props`, `CobolPackConformanceStatement`; kb/Work PB5431). The sources are the thing to edit: a determination is `docs/conformance/3/D-<KEY>.md`, a register
row `docs/conformance/7/DOC-A.1-<n>.md`, and `docs/conformance/README.md` says the rest. The reading and the layout are
`conformance_source.py`'s (`read_sources`, `render_section`), so this script and every reader see one document.
"""
from __future__ import annotations

import argparse
import pathlib
import sys

sys.path.append(str(pathlib.Path(__file__).resolve().parent))      # scripts/spec, for conformance_source
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
import conformance_source  # noqa: E402
from rendered_views import write_rendered  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    mode = ap.add_mutually_exclusive_group(required=True)
    mode.add_argument("--out", type=pathlib.Path, help="write the rendered docs/CONFORMANCE.md here")
    mode.add_argument("--check", action="store_true", help="render in memory and name every source out of shape")
    ap.add_argument("--root", type=pathlib.Path, default=conformance_source.REPO, help="the repository root (a self-test's)")
    a = ap.parse_args(argv)
    if a.check:
        findings = conformance_source.problems(a.root)
        for finding in findings:
            print(f"  ✗ {finding}")
        print(f"{conformance_source.SOURCE_REL}: RED, {len(findings)} finding(s)" if findings else
              f"{conformance_source.SOURCE_REL}: GREEN (the sources render a regular {conformance_source.PATH_REL})")
        return 1 if findings else 0
    text, findings = conformance_source.read_sources(a.root)
    if findings:   # a partial document is never written: a reader of the view would take the gap for the statement
        for finding in findings:
            print(f"  ✗ {finding}", file=sys.stderr)
        print(f"⛔ {conformance_source.SOURCE_REL} does not render: nothing written to {a.out}", file=sys.stderr)
        return 1
    refused = write_rendered(a.out, text, a.root)   # stamped at the view's path, the body alone elsewhere (the package's copy)
    if refused is not None:
        print(f"⛔ {refused}", file=sys.stderr)
        return 1
    print(f"rendered {a.out} from {conformance_source.SOURCE_REL} ({text.count(chr(10)):,} lines)")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main(sys.argv[1:]))
