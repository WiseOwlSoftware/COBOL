#!/usr/bin/env python3
"""conformance_migrate.py — ONE-OFF: cut the regular docs/CONFORMANCE.md (step B1) into docs/conformance/, one directory per
section and one file per entry (kb/Work PB5430; DESIGN-orchestrator-loop.md section 16.8, step B2). Run once by the landing
that adds it, and deleted by the same landing (the devlog_migrate.py shape).

    python scripts/spec/conformance_migrate.py      # writes docs/conformance/, then proves the render byte-identical

The proof is the render: `conformance_source.read_sources` over what this wrote must equal the monolith byte for byte, or
the script exits 1 and the cut is wrong.
"""
from __future__ import annotations

import pathlib
import re
import subprocess
import sys

sys.path.append(str(pathlib.Path(__file__).resolve().parent))      # scripts/spec, for conformance_source
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
import conformance_source as cs  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

#: A key a file may be named by: the document's keys are all of this form (`D-SEQ`, `DOC-A.1-92b`, `28–30`, `A.4.2`).
SAFE_KEY = re.compile(r"[A-Za-z0-9][A-Za-z0-9.-]*")


def write(path: pathlib.Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def main() -> int:
    # the monolith as step B1 left it, from the commit before the cut (the working tree's copy is the stamped view by then)
    monolith = subprocess.run(["git", "-C", str(cs.REPO), "show", f"HEAD:{cs.PATH_REL}"], capture_output=True,
                              text=True, encoding="utf-8", check=True).stdout
    conf = cs.parse(monolith)
    findings = cs.check(conf)
    if findings:
        print("⛔ the monolith is not regular (step B1's check):", *findings[:10], sep="\n  ")
        return 1
    base = cs.REPO / cs.SOURCE_REL
    first = conf._headings[0][0]
    write(base / cs.HEAD_FILE, monolith[:first].strip("\n") + "\n")
    files = 1
    for number in conf.numbers():
        own = conf.own_section(number)
        shape = conf.shape(number)
        directory = base / number
        fields: dict = {"heading": own.heading}
        if shape.kind == cs.TABLE:
            columns = cs.cells(shape.table_head.split("\n")[0])
            assert shape.table_head == cs.row_line(columns) + "\n|" + "---|" * len(columns), (number, shape.table_head)
            fields["columns"] = columns
        write(directory / cs.INDEX_FILE, cs.with_frontmatter(fields, shape.preamble))
        files += 1
        if shape.epilogue is not None:
            write(directory / cs.EPILOGUE_FILE, shape.epilogue + "\n" if shape.epilogue else "")
            files += 1
        bodies = {b.key: b.text for b in shape.bodies}
        for order, entry in enumerate(shape.entries, 1):
            assert SAFE_KEY.fullmatch(cs.file_name(entry.key)), (number, entry.key)
            if shape.kind == cs.BULLETS:
                text = entry.text + "\n"
            elif shape.kind == cs.TABLE:
                text = cs.with_frontmatter({"cells": cs.cells(entry.text)}, bodies.get(entry.key, ""))
            else:
                text = cs.with_frontmatter({"order": order}, entry.text)
            write(directory / f"{cs.file_name(entry.key)}.md", text)
            files += 1
    rendered, problems = cs.read_sources(cs.REPO)
    if problems or rendered != monolith:
        at = next((i for i, (a, b) in enumerate(zip(rendered.split("\n"), monolith.split("\n"))) if a != b), None)
        print(f"⛔ the cut does not render back: {len(problems)} problem(s) {problems[:5]}; first differing line {at}")
        return 1
    print(f"cut {cs.PATH_REL} into {files} files under {cs.SOURCE_REL}/; the render is byte-identical "
          f"({len(monolith.encode('utf-8')):,} bytes)")
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
