"""docchecks/manual.py — the user manual (docs/manual/) against the compiler and against itself (kb/Work PB4453).

The Unit test UserManualDriftTests and the Conformance test StopGobackExitCodeTests' page arm read the manual's Markdown.
What is about the pages alone (frontmatter, links and headings) is checked here. What is about the compiler is checked
against `tests/doc-facts/compiler-facts.json` (the diagnostic codes the compiler allocates, the command line's options and
exit statuses). And what the compiler tests need FROM the manual is data they now read without the Markdown: the complete
COBOL programs the pages print are extracted to `tests/doc-facts/manual-programs/*.cob` (the test compiles each file), and
the exit-status examples the run-units page prints live in `tests/doc-facts/manual-exit-examples.json` (the test runs each
program and expects each exit; this module requires the page to print each span). `--write-manual` regenerates the programs.
"""
from __future__ import annotations

import json
import re
from pathlib import Path

from . import common
from .common import Check, need

MANUAL = "docs/manual"
PROGRAMS_DIR = "tests/doc-facts/manual-programs"
EXIT_EXAMPLES = "tests/doc-facts/manual-exit-examples.json"
RUN_UNITS_PAGE = "user-guide/run-units.md"
COMMAND_LINE_PAGE = "user-guide/command-line.md"
FRONTMATTER = re.compile(r"^---\r?\n(?P<head>.*?)\r?\n---\r?\n", re.DOTALL)
MARKDOWN_LINK = re.compile(r"\]\((?P<target>[^)\s]+)\)")
HEADING = re.compile(r"^#{1,6}\s+(?P<title>.+?)\s*$", re.MULTILINE)
DIAGNOSTIC_CODE = re.compile(r"\b(?:COBOL|CBL)\d{4}\b")
COBOL_BLOCK = re.compile(r"```cobol\r?\n(?P<code>.*?)```", re.DOTALL)


def pages(root: Path) -> dict[str, str]:
    """The hand-written pages (the generated `reference/` is not), keyed by path relative to docs/manual/."""
    prefix = MANUAL + "/"
    return {p[len(prefix):]: common.text(root, p) for p in common.tracked(root, MANUAL)
            if p.endswith(".md") and not p[len(prefix):].startswith("reference/")}


def url_of(page: str) -> str:
    slug = page[:-len(".md")]
    if slug == "index":
        return ""
    if slug.endswith("/index"):
        slug = slug[:-len("/index")]
    return slug + "/"


def slug_of(heading: str) -> str:
    out = []
    for c in heading.replace("`", "").lower():
        if c.isalnum() or c in "-_":
            out.append(c)
        elif c == " ":
            out.append("-")
    return "".join(out)


def resolve(from_url: str, link: str) -> str:
    parts = [s for s in from_url.split("/") if s]
    for seg in (s for s in link.split("/") if s):
        if seg == "..":
            if parts:
                parts.pop()
        elif seg != ".":
            parts.append(seg)
    return "/".join(parts) + "/" if parts else ""


def frontmatter_findings(page_texts: dict[str, str]) -> list[str]:
    """Starlight needs title and description, and the sidebar needs an order."""
    if not page_texts:
        return ["docs/manual/ holds no page"]
    out = []
    for page, body in page_texts.items():
        head = FRONTMATTER.match(body)
        if head is None:
            out.append(f"{page}: no frontmatter")
            continue
        keys = [ln.lstrip() for ln in head.group("head").split("\n")]
        out += [f"{page}: frontmatter lacks {k}" for k in ("title:", "description:", "order:") if not any(x.startswith(k) for x in keys)]
    return out


def link_findings(page_texts: dict[str, str]) -> list[str]:
    by_url = {url_of(p): p for p in page_texts}
    out = []
    for page, body in page_texts.items():
        for m in MARKDOWN_LINK.finditer(body):
            target = m.group("target")
            if "://" in target or target.startswith("mailto:"):
                continue
            path, _, fragment = target.partition("#")
            if path and not path.endswith("/"):
                out.append(f"{page}: {target}: a page link ends in a slash")
                continue
            destination = resolve(url_of(page), path) if path else url_of(page)
            if destination not in by_url:
                out.append(f"{page}: {target}: no page at '{destination}'")
                continue
            if fragment and not any(slug_of(h.group("title")) == fragment for h in HEADING.finditer(page_texts[by_url[destination]])):
                out.append(f"{page}: {target}: '{by_url[destination]}' has no heading '{fragment}'")
    return [f"a manual link must reach a hand-written page, written relative with a trailing slash: {o}" for o in out]


def diagnostic_code_findings(page_texts: dict[str, str], allocated: set[str]) -> list[str]:
    need_codes: list[str] = []
    need(need_codes, len(allocated) > 500, f"the compiler facts hold {len(allocated)} allocated codes: the facts are broken")
    return need_codes + sorted({f"the manual names a diagnostic code no descriptor carries: {p}: {c}"
                                for p, body in page_texts.items() for c in DIAGNOSTIC_CODE.findall(body) if c not in allocated})


def command_line_findings(page: str, options: list[str], statuses: list[int]) -> list[str]:
    out = []
    missing = sorted(o for o in options if f"`{o}" not in page)
    need(out, not missing, f"{COMMAND_LINE_PAGE} does not document: {', '.join(missing)}")
    # Only a backticked token that starts with -- is an option; a COBOL phrase in prose never does.
    named = set(re.findall(r"`(--[a-z][a-z-]*)", page))
    invented = sorted(n for n in named if n not in options)
    need(out, not invented, f"{COMMAND_LINE_PAGE} documents options the command line lacks: {', '.join(invented)}")
    documented = {int(m) for m in re.findall(r"^\| (\d+) \|", page, re.MULTILINE)}
    need(out, documented == set(statuses),
         f"{COMMAND_LINE_PAGE}'s exit status table lists {sorted(documented)}; the command returns {sorted(statuses)}")
    return out


def complete_programs(page_texts: dict[str, str]) -> dict[str, str]:
    """A complete program starts with its IDENTIFICATION DIVISION or a directive before it; a fragment does not."""
    out = {}
    for page, body in page_texts.items():
        n = 0
        for block in COBOL_BLOCK.finditer(body):
            code = block.group("code")
            first = next(ln for ln in code.split("\n") if ln.strip()).strip()
            if first.startswith("IDENTIFICATION DIVISION") or first.startswith(">>"):
                out[f"{page.replace('/', '_')}_{n}.cob"] = code.replace("\r\n", "\n")
                n += 1
    return out


def program_findings(expected: dict[str, str], committed: dict[str, str]) -> list[str]:
    out = []
    need(out, bool(expected), "no complete COBOL program in the manual: the example scan matched nothing")
    out += [f"{PROGRAMS_DIR}/{n} is not printed in the manual any more: regenerate with `python scripts/doc_audits.py --write-manual`"
            for n in sorted(committed.keys() - expected.keys())]
    out += [f"{PROGRAMS_DIR}/{n} is missing or stale (the manual prints another program there): regenerate with "
            "`python scripts/doc_audits.py --write-manual`" for n in sorted(expected) if committed.get(n) != expected[n]]
    return out


def exit_example_findings(page: str, examples: list[dict]) -> list[str]:
    """THE USER MANUAL'S EXIT STATUS EXAMPLES ARE THE PRODUCT'S (kb/Work PB1610): each example the page prints is also
    run by the Conformance test, so neither the page nor DOC-A.1-192's mapping can move without the other."""
    flat = page.replace("\r\n", " ").replace("\n", " ").replace("  ", " ")
    return [f"{RUN_UNITS_PAGE} no longer prints `{e['pageSpan']}`, the exit-status example the Conformance test runs"
            for e in examples if e["pageSpan"] not in flat] + ([] if examples else ["no exit-status example: the data file is empty"])


def _programs_on_disk(root: Path) -> dict[str, str]:
    prefix = PROGRAMS_DIR + "/"
    return {p[len(prefix):]: common.text(root, p) for p in common.tracked(root, PROGRAMS_DIR)}


def write_programs(root: Path) -> None:
    target = root / PROGRAMS_DIR
    target.mkdir(parents=True, exist_ok=True)
    expected = complete_programs(pages(root))
    for old in target.glob("*.cob"):
        if old.name not in expected:
            old.unlink()
    for name, code in expected.items():
        (target / name).write_text(code, encoding="utf-8", newline="\n")


def _page_checks(fn):
    return lambda root: fn(pages(root))


CHECKS = (
    Check("MANUAL FRONTMATTER on every page", _page_checks(frontmatter_findings)),
    Check("MANUAL LINKS reach a page and a heading", _page_checks(link_findings)),
    Check("MANUAL DIAGNOSTIC CODES are allocated",
          lambda r: diagnostic_code_findings(pages(r), set(common.facts(r)["allocatedDiagnosticCodes"]))),
    Check("MANUAL COMMAND-LINE PAGE names every option and exit status",
          lambda r: command_line_findings(pages(r)[COMMAND_LINE_PAGE], common.facts(r)["cliOptions"], common.facts(r)["cliExitStatuses"])),
    Check("MANUAL PROGRAMS the tests compile are the pages' programs",
          lambda r: program_findings(complete_programs(pages(r)), _programs_on_disk(r))),
    Check("MANUAL EXIT-STATUS EXAMPLES are printed on the run-units page",
          lambda r: exit_example_findings(pages(r)[RUN_UNITS_PAGE], json.loads(common.text(r, EXIT_EXAMPLES))["examples"])),
)


def self_test() -> list[str]:
    good = {"index.md": "---\ntitle: T\ndescription: D\norder: 1\n---\n# Top\nsee [a](user-guide/) and [b](user-guide/#sub-head)\n",
            "user-guide/index.md": "---\ntitle: U\ndescription: D\norder: 2\n---\n## Sub head\nback [i](../)\n```cobol\n  IDENTIFICATION DIVISION.\n```\n```cobol\nMOVE 1 TO X.\n```\n"}
    page = "`--run` `--out`\n| 0 | ok |\n| 2 | bad |\n"
    return common.self_test_arms([
        ("a good manual has no frontmatter finding", frontmatter_findings(good), []),
        ("a page without an order is named", frontmatter_findings({"x.md": "---\ntitle: T\ndescription: D\n---\n"}), ["x.md: frontmatter lacks order:"]),
        ("a page without frontmatter is named", frontmatter_findings({"x.md": "# no"}), ["x.md: no frontmatter"]),
        ("good links resolve to pages and headings", link_findings(good), []),
        ("a link without a trailing slash is a finding", len(link_findings({**good, "z.md": "[x](user-guide)"})), 1),
        ("a link to a missing heading is a finding", len(link_findings({**good, "z.md": "[x](user-guide/#nope)"})), 1),
        ("a link to a missing page is a finding", len(link_findings({**good, "z.md": "[x](gone/)"})), 1),
        ("an external link is not judged", link_findings({**good, "z.md": "[x](https://e.com/a)"}), []),
        ("an allocated code is clean", diagnostic_code_findings({"a.md": "COBOL0001"}, {f"COBOL{n:04d}" for n in range(1, 600)}), []),
        ("an unallocated code is a finding", len(diagnostic_code_findings({"a.md": "COBOL9999"}, {f"COBOL{n:04d}" for n in range(1, 600)})), 1),
        ("the command-line page agrees", command_line_findings(page, ["--run", "--out"], [0, 2]), []),
        ("an undocumented option is named", len(command_line_findings(page, ["--run", "--out", "--new"], [0, 2])), 1),
        ("an invented option is named", len(command_line_findings(page + "`--bogus`", ["--run", "--out"], [0, 2])), 1),
        ("a missing exit status is named", len(command_line_findings(page, ["--run", "--out"], [0, 2, 64])), 1),
        ("only the complete program is extracted", list(complete_programs(good)), ["user-guide_index.md_0.cob"]),
        ("a program equal to its page is fresh", program_findings({"a.cob": "x"}, {"a.cob": "x"}), []),
        ("a stale program is a finding", len(program_findings({"a.cob": "x"}, {"a.cob": "y"})), 1),
        ("a program the page dropped is a finding", len(program_findings({"a.cob": "x"}, {"a.cob": "x", "b.cob": "z"})), 1),
        ("a printed exit example is clean", exit_example_findings("a `STOP RUN`\nb", [{"pageSpan": "`STOP RUN` b"}]), []),
        ("an unprinted exit example is a finding", len(exit_example_findings("a", [{"pageSpan": "gone"}])), 1),
    ])
