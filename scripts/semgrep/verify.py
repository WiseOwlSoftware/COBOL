#!/usr/bin/env python3
"""Drift test for the architectural-invariant rules.

Four independent checks, because a rule set can fail in several opposite directions:

  0. EVERY rule must be TEXT: a single `pattern-regex`, which semgrep matches over the file's characters. semgrep's C#
     grammar predates C# 12 (a primary-constructor class header, an `extension` member, a list pattern with a slice
     designation, a raw interpolated string, `allows ref struct`) and, on a construct it cannot read, SKIPS the span: an
     AST rule (`pattern`, `patterns`, `pattern-either`, `pattern-inside`, ...) is blind there, and `patterns:` even
     over regexes loses a file wholesale. kb/Work PB4462 found 130 of the product's files in that state -- the data binder's
     thousands of lines among them, the raw-code-literal count under-reading 230 of 248 -- while this script read only
     the scan's `results` and never its `errors[]`. A rule that needs the parser is REFUSED here, naming its keys.
  1. EVERY rule must still FIRE where a test says it must. `testdata/Violations.cs` (a file semgrep reads) holds one
     violation per rule and LINE CONTROLS: a trailing `FIRES <rule>` marker says that rule hits that line, `QUIET` that no
     rule does (a `//` inside a string or char literal must not hide a violation, a `//` comment must). `testdata/unparsed/`
     holds one file per rule, named for it, whose violation sits on a line semgrep's grammar cannot read (a C# 12 primary
     constructor): the rule must fire ON a line semgrep itself reports as an unread (`PartialParsing`) span, and a file
     semgrep now reads in full fails as an obsolete control (kb/Work PB4462, PB4474). A rule that silently stops matching
     reports zero findings and looks green -- strictly worse than no rule. This has already happened once: an invented
     `pattern-not-inside` suppression construct disabled two rules while the scan still said "success". The path-scoped
     rules are re-run with their `paths:` filter stripped, since the testdata lives outside src/.
  2. The scan's `errors[]` is READ, in the testdata and in the greenfield tree. A `PartialParsing` span is counted and
     reported (`--gaps` lists them): the text rules of check 0 still match across it -- measured on the product tree, where
     the regexes and an independent Python pass agree file by file. ANYTHING ELSE fails and names the file: a whole-file
     "Syntax error" removes the file from every rule (a text rule's hit there is dropped too), and a timeout or a rule
     error is a rule that did not run. No count is a measurement while one stands.
  3. The greenfield tree's finding count must not EXCEED the recorded baseline. New violations fail the check;
     fixing violations is expected and prints a reminder to lower the baseline.

Usage:  python scripts/semgrep/verify.py [--gaps] [--update-baseline]
        python scripts/semgrep/verify.py --self-test
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys
import tempfile
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[1]
CONFIG = HERE / "invariants.yml"
TESTDATA = HERE / "testdata"
BASELINE = HERE / "baseline.json"
#: Check 1's second control (PB4462, PB4474): one file per rule, `<rule id>.cs`, each with a violation on a line semgrep's
#: C# grammar cannot read.
UNPARSED_DIR = TESTDATA / "unparsed"
#: The text rules that skip a `//` comment all begin with this prefix (kb/Work PB4474): a character that is no part of a
#: literal, a `/` that does not start a comment, or a WHOLE string / verbatim string / char literal, so a `//` inside a
#: literal is consumed with it and only a comment's `//` stops the match. invariants.yml cannot share it (YAML has no
#: interpolation), so `prefix_drift` holds each copy to this text. `\x27` is the apostrophe, which a single-quoted YAML
#: scalar cannot carry bare.
CODE_PREFIX = (r'(?:[^\n"\x27/@]|/(?!/)|@(?!")|"(?:[^"\\\n]|\\.)*"|@"(?:[^"\n]|"")*"|'
               r'\x27(?:[^\x27\\\n]|\\.)*\x27)*?')
#: A line control in Violations.cs (check 1): the rule that must hit the line, or QUIET (no rule may).
LINE_FIRES = re.compile(r"\bFIRES\s+(\S+)\s*$")

GREENFIELD = [
    "src/WiseOwl.Cobol.Cli", "src/WiseOwl.Cobol.Compiler", "src/WiseOwl.Cobol.Editions",
    "src/WiseOwl.Cobol.Frontend", "src/WiseOwl.Cobol.Runtime",
]

#: The product files semgrep's grammar cannot read AT ALL (a whole-file "Syntax error", kb/Work PB4462), each with the
#: first member a prefix bisect at member boundaries found it failing on (a whole-file failure is semgrep giving up after
#: enough unreadable C# 12-14 constructs, so the member is where it tips, not a single cause). A rule hit in such a file is
#: DROPPED, text rule or not, so check 2 fails the moment one would appear; until then the file is a place no rule has
#: looked, and it is named here or the check fails. An entry whose file semgrep now reads is stale and fails too. Each
#: file is valid C# the product writes on purpose, and is not reshaped to dodge a tool's parser (CLAUDE.md rule 4).
WHOLE_FILE_UNREADABLE = {
    "src/WiseOwl.Cobol.Compiler/Binding/ReportSectionBinder.cs": "first failing member: MultipleLineOperands (line 1586)",
    "src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs":
        "first failing members: PictureConstructId and UsageConstructId (lines 487-534), conditional chains over `is` patterns",
    "src/WiseOwl.Cobol.Frontend/Parsing/ConditionalPhrases.cs":
        "first failing member: Owners (line 253), a dictionary initializer of collection expressions with `with { .. }`",
    "src/WiseOwl.Cobol.Frontend/Pipeline/Frontend.cs":
        "line 137: `Expected explicit type`, a target-typed collection expression as one arm of a conditional",
    "src/WiseOwl.Cobol.Runtime/IO/Store/BTree.cs":
        "first failing member: TornPage (line 848), a nested class with a primary constructor and a base-initializer",
}

#: The keys a TEXT rule may carry (check 0). `pattern-regex` is the one matcher; a `patterns:` block, a `pattern:`, a
#: `pattern-either`, a `pattern-not-regex` or a `metavariable-regex` is a combination semgrep evaluates over a parse tree.
TEXT_RULE_KEYS = frozenset({"id", "languages", "severity", "message", "paths", "pattern-regex", "metadata"})


def run(config: pathlib.Path, targets: list[str], no_git_ignore: bool = False) -> dict:
    """One `semgrep scan --json`, whole: `results` AND `errors` (PB4462: this used to return `results` alone)."""
    cmd = ["semgrep", "scan", "--config", str(config), "--metrics=off", "--json", "-q"]
    if no_git_ignore:
        cmd.append("--no-git-ignore")
    cmd += targets
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8",
                          errors="replace", cwd=str(REPO), timeout=900)
    if not proc.stdout.strip():
        sys.exit(f"semgrep produced no output.\n{proc.stderr[-2000:]}")
    return json.loads(proc.stdout)


def rule_ids(config: pathlib.Path) -> list[str]:
    return re.findall(r"^\s*-\s+id:\s*(\S+)", config.read_text(encoding="utf-8"), re.M)


def rule_keys(config_text: str) -> dict[str, list[str]]:
    """Each rule's own keys, in order, by rule id. A rule starts at `  - id:` and its keys sit at four spaces; a
    message's continuation lines and a `paths:` list sit deeper, a comment is skipped."""
    keys: dict[str, list[str]] = {}
    current: list[str] | None = None
    for line in config_text.splitlines():
        m = re.match(r"^  - id:\s*(\S+)", line)
        if m:
            current = keys.setdefault(m.group(1), ["id"])
            continue
        m = re.match(r"^    ([a-z][a-z-]*):", line)
        if m and current is not None:
            current.append(m.group(1))
    return keys


def parser_dependence(config_text: str) -> list[str]:
    """Check 0: one line per rule that needs semgrep's parser -- or has no single `pattern-regex` to run without one."""
    refused = []
    for rid, keys in rule_keys(config_text).items():
        extra = sorted(set(keys) - TEXT_RULE_KEYS)
        if extra:
            refused.append(f"{rid}: carries {', '.join(extra)}, which a text rule does not use (the pattern keys are evaluated over a parse tree)")
        elif keys.count("pattern-regex") != 1:
            refused.append(f"{rid}: carries {keys.count('pattern-regex')} `pattern-regex` keys, not exactly one")
    return refused


def rule_regexes(config_text: str) -> dict[str, str]:
    """Each rule's `pattern-regex` by rule id, as the regex (a single-quoted YAML scalar, its `''` read as `'`)."""
    out: dict[str, str] = {}
    for m in re.finditer(r"^  - id:\s*(\S+)(.*?)(?=^  - id:|\Z)", config_text, re.M | re.S):
        pm = re.search(r"^    pattern-regex: '(.*)'[ \t]*$", m.group(2), re.M)
        if pm:
            out[m.group(1)] = pm.group(1).replace("''", "'")
    return out


def prefix_drift(config_text: str) -> list[str]:
    """Check 0: a rule whose regex opens `(?m)^(?:` (a prefix that walks the line) must open with `(?m)^` + CODE_PREFIX
    exactly, and none may use the tempered form `(?:(?!//)[^\\n])*?`, which rules a match out after a `//` inside a string
    literal (kb/Work PB4474). The rules that skip a comment share one text, which the YAML cannot interpolate."""
    problems = []
    for rid, rx in rule_regexes(config_text).items():
        if "(?!//)" in rx:
            problems.append(f"{rid}: uses the tempered `(?!//)` prefix, which hides a violation after a `//` inside a "
                            "string; begin with verify.CODE_PREFIX")
        elif rx.startswith("(?m)^(?:") and not rx.startswith("(?m)^" + CODE_PREFIX):
            problems.append(f"{rid}: its line prefix differs from verify.CODE_PREFIX")
    return problems


def unparsed_control_problems(declared: list[str], present: set[str], hits: dict[str, dict[str, set[int]]],
                              spans: dict[str, list[tuple[int, int]]]) -> list[str]:
    """Check 1, the unparsed controls: one file `<rule id>.cs` per declared rule (`present`: the names found), the rule
    firing in it ON a line inside a span semgrep reports unread (`hits`: file name -> rule -> lines; `spans`: file name ->
    (first, last) lines). A file with no unread span is an obsolete control, which fails (it used to be a NOTE and PASS)."""
    problems = []
    for rid in declared:
        name = f"{rid}.cs"
        if name not in present:
            problems.append(f"{rid}: no control file unparsed/{name}")
            continue
        lines = hits.get(name, {}).get(rid, set())
        unread = spans.get(name, [])
        if not unread:
            problems.append(f"unparsed/{name}: semgrep reads it in full now, so it no longer proves the rule matches where the "
                            "grammar fails; use a construct semgrep cannot read")
        elif not lines:
            problems.append(f"{rid}: does not fire in unparsed/{name}")
        elif not any(a <= line <= b for line in lines for a, b in unread):
            problems.append(f"{rid}: fires in unparsed/{name} only outside the spans semgrep cannot read "
                            f"(lines {sorted(lines)}, unread {unread})")
    problems += [f"unparsed/{name}: names no declared rule" for name in sorted(present - {f"{r}.cs" for r in declared})]
    return problems


def line_problems(source: str, hits: dict[str, set[int]]) -> list[str]:
    """Check 1, the line controls of Violations.cs: a line ending `FIRES <rule>` must be hit by that rule, and one ending
    `QUIET` by none (`hits`: rule id -> the lines it hit in the file)."""
    problems = []
    for n, line in enumerate(source.splitlines(), 1):
        fires = LINE_FIRES.search(line)
        if fires and n not in hits.get(fires.group(1), set()):
            problems.append(f"Violations.cs:{n}: {fires.group(1)} must fire here and does not")
        elif line.rstrip().endswith("QUIET"):
            problems += [f"Violations.cs:{n}: {rid} fires on a line that must stay quiet"
                         for rid in sorted(hits) if n in hits[rid]]
    return problems


def is_partial_parse(e: dict) -> bool:
    kind = e.get("type")
    return isinstance(kind, list) and bool(kind) and kind[0] == "PartialParsing"


def error_path(e: dict) -> str:
    """The file an `errors[]` entry names, forward-slashed ('' when it names none, as a timeout does)."""
    return str(e.get("path") or "").replace("\\", "/")


def describe_error(e: dict) -> str:
    """One line for a non-partial entry: `file  type: first line of the message`."""
    what = (e.get("message") or "").splitlines()[0] if e.get("message") else ""
    return f"{error_path(e) or '(no file)'}  {e.get('type')}: {what}"


def classify_errors(errors: list[dict]) -> tuple[list[str], list[str]]:
    """Check 2: (the `PartialParsing` spans as `file:first-last`, the FATAL entries as one line each). Fatal is everything
    but a partial parse: a whole-file "Syntax error" (the file is removed from every rule), a timeout, a rule error --
    `file  type: first line of the message`. Every reader of a semgrep scan's `errors[]` reads it through here or through
    `is_partial_parse` / `error_path` / `describe_error` (`scripts/arch/r2_inputs.py`'s `run_semgrep` does: PB4462 review R1-1)."""
    spans = [f"{path}:{first}-{last}" for path, first, last in partial_spans(errors)]
    fatal = [describe_error(e) for e in errors if not is_partial_parse(e)]
    return sorted(set(spans)), fatal


def partial_spans(errors: list[dict]) -> list[tuple[str, int, int]]:
    """Every unread span of the `PartialParsing` entries, as (forward-slashed path, first line, last line)."""
    return [(str(span["path"]).replace("\\", "/"), span["start"]["line"], span["end"]["line"])
            for e in errors if is_partial_parse(e)
            for span in e["type"][1]]   # ["PartialParsing", [{"path", "start": {"line"}, "end": {"line"}}, ...]]


def unreadable_files(cwd: pathlib.Path = REPO, targets: list[str] = GREENFIELD) -> tuple[set[str], list[str]]:
    """(the files under `targets` that semgrep's grammar cannot read at all, the spans it reads only in part), paths as
    semgrep prints them for these targets run from `cwd`. A throwaway AST pattern forces the parse of every file, which no
    text rule does until it has a hit."""
    proc = subprocess.run(["semgrep", "scan", "--pattern", "class $C { ... }", "--lang", "csharp", "--metrics=off", "--json", "-q",
                           *targets], capture_output=True, text=True, encoding="utf-8", errors="replace", cwd=str(cwd),
                          timeout=900)
    if not proc.stdout.strip():
        sys.exit(f"semgrep produced no output.\n{proc.stderr[-2000:]}")
    errors = json.loads(proc.stdout)["errors"]
    whole = {error_path(e) for e in errors if not is_partial_parse(e) and error_path(e)}
    return whole, classify_errors(errors)[0]


def exception_drift(found: set[str], named: dict[str, str]) -> tuple[list[str], list[str]]:
    """(files semgrep cannot read that no exception names, exceptions naming a file semgrep reads now)."""
    return sorted(found - set(named)), sorted(set(named) - found)


def compare_counts(base: dict[str, int], counts: dict[str, int]) -> tuple[list[str], bool, bool]:
    """Check 3: (report lines, whether any rule's count rose, whether any fell)."""
    out, up, down = [], False, False
    for rid in sorted(set(base) | set(counts)):
        now, was = counts.get(rid, 0), base.get(rid, 0)
        flag = "OK  "
        if now > was:
            flag, up = "UP  ", True
        elif now < was:
            flag, down = "DOWN", True
        out.append(f"   {flag}  {rid}: {was} -> {now}")
    return out, up, down


def strip_paths(config: pathlib.Path, into: pathlib.Path) -> pathlib.Path:
    """Copy the config with every `paths:` block removed, so path-scoped rules can be fire-tested. The copy goes into
    the caller's own directory `into`, never a fixed name in the machine-wide temp directory (kb/Work PB2922)."""
    out, skipping, skip_indent = [], False, 0
    for line in config.read_text(encoding="utf-8").splitlines():
        stripped = line.strip()
        indent = len(line) - len(line.lstrip())
        if skipping:
            if stripped and indent <= skip_indent:
                skipping = False
            else:
                continue
        if stripped.startswith("paths:"):
            skipping, skip_indent = True, indent
            continue
        out.append(line)
    copy = into / "invariants-nopaths.yml"
    copy.write_text("\n".join(out) + "\n", encoding="utf-8", newline="\n")
    return copy


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--update-baseline", action="store_true")
    ap.add_argument("--gaps", action="store_true", help="list every span semgrep's C# grammar could not read")
    ap.add_argument("--self-test", action="store_true")
    args = ap.parse_args()
    if args.self_test:
        return self_test()

    declared = rule_ids(CONFIG)
    print(f"{len(declared)} rules declared.\n")

    # ---- Check 0: no rule needs semgrep's C# parser ----------------------------------------------------------
    config_text = CONFIG.read_text(encoding="utf-8")
    refused = parser_dependence(config_text) + prefix_drift(config_text)
    print("CHECK 0 — every rule is text (one `pattern-regex`), so semgrep's C# grammar cannot blind it")
    for line in refused:
        print(f"   REFUSED  {line}")
    if refused:
        print(f"\nFAIL: {len(refused)} rule problem(s): a rule that needs semgrep's parser is blind over every span or file it")
        print("      cannot read (C# 12+ constructs the product uses: a primary-constructor class, a collection expression),")
        print("      and a line prefix that is not CODE_PREFIX hides a violation after a `//` inside a string. Write each rule")
        print("      as one `pattern-regex`, or enforce it with a Roslyn check.")
        return 1
    print("   OK    all rules are text, and the comment-skipping prefix is the one CODE_PREFIX\n")

    # ---- Check 1: every rule fires where a test says it must ----------------------------------------------------
    with tempfile.TemporaryDirectory(prefix="wiseowl-cobol-semgrep-") as tmp:
        scan = run(strip_paths(CONFIG, pathlib.Path(tmp)), [str(TESTDATA)], no_git_ignore=True)
    hit_lines: dict[str, dict[str, set[int]]] = {}   # file name -> rule -> lines
    for r in scan["results"]:
        hit_lines.setdefault(pathlib.PurePath(r["path"]).name, {}).setdefault(
            r["check_id"].split(".")[-1], set()).add(r["start"]["line"])
    unread: dict[str, list[tuple[int, int]]] = {}    # file name -> the spans semgrep reports unread
    for path, first, last in partial_spans(scan["errors"]):
        unread.setdefault(pathlib.PurePath(path).name, []).append((first, last))
    print("CHECK 1 — every rule fires in Violations.cs, on its line controls, and inside an unread span of its own control")
    dead = 0
    for r in declared:
        ok = r in hit_lines.get("Violations.cs", {})
        dead += not ok
        print(f"   {'OK  ' if ok else 'DEAD'}  Violations.cs: {r}")
    problems = line_problems((TESTDATA / "Violations.cs").read_text(encoding="utf-8"), hit_lines.get("Violations.cs", {}))
    problems += unparsed_control_problems(
        declared, {p.name for p in UNPARSED_DIR.glob("*.cs")}, hit_lines, unread)
    for line in problems:
        print(f"   DEAD  {line}")
    if dead or problems:
        print(f"\nFAIL: {dead + len(problems)} control(s) failed. A rule that never fires is worse than no rule —")
        print("      it reports green. Fix the pattern or remove the rule.")
        return 1
    print(f"   OK    the line controls hold, and all {len(declared)} rules fire inside a span semgrep cannot read")
    fatal = classify_errors(scan["errors"])[1]

    # ---- Check 2: the scan's errors[] are read, not dropped -------------------------------------------------------
    result = run(CONFIG, GREENFIELD)
    fatal += classify_errors(result["errors"])[1]
    print("\nCHECK 2 — the scans' errors[]")
    whole, spans = unreadable_files()
    files = {s.rsplit(":", 1)[0] for s in spans}
    print(f"   {len(spans)} span(s) in {len(files)} product file(s) that semgrep's C# grammar reads only in part; every rule is text, "
          "so they are matched across (--gaps lists them)")
    if args.gaps:
        for s in sorted(spans):
            print(f"      {s}")
    for line in fatal:
        print(f"   UNMEASURED  {line}")
    if fatal:
        print(f"\nFAIL: {len(fatal)} error(s) are not a partial parse: a file removed from every rule, a timeout or a rule error —")
        print("      no count below is a measurement while one stands.")
        return 1
    print("   OK    no whole-file, timeout or rule error")
    unnamed, stale = exception_drift(whole, WHOLE_FILE_UNREADABLE)
    for path in unnamed:
        print(f"   UNNAMED  {path}: semgrep cannot read it at all, so a hit there is dropped (name it in WHOLE_FILE_UNREADABLE with a reason)")
    for path in stale:
        print(f"   STALE    {path}: semgrep reads it now (delete its WHOLE_FILE_UNREADABLE entry)")
    if unnamed or stale:
        print(f"\nFAIL: {len(unnamed)} unreadable file(s) unnamed, {len(stale)} stale exception(s).")
        return 1
    print(f"   OK    the {len(WHOLE_FILE_UNREADABLE)} file(s) semgrep cannot read at all are named, with a reason\n")
    counts: dict[str, int] = {}
    for r in result["results"]:
        rid = r["check_id"].split(".")[-1]
        counts[rid] = counts.get(rid, 0) + 1

    if args.update_baseline:
        BASELINE.write_text(json.dumps(counts, indent=2, sort_keys=True) + "\n", encoding="utf-8", newline="\n")
        print(f"Baseline written: {counts}")
        return 0

    base = json.loads(BASELINE.read_text(encoding="utf-8")) if BASELINE.exists() else {}
    print("CHECK 3 — greenfield findings vs baseline")
    lines, up, down = compare_counts(base, counts)
    print("\n".join(lines))
    if up:
        print("\nFAIL: new violations introduced. Fix them, or record an explicit decision and re-baseline.")
        return 1
    if down:
        print("\nViolations went DOWN — re-run with --update-baseline to lock the improvement in.")
    print("\nPASS")
    return 0


def self_test() -> int:
    """The pure parts, against fixtures (no semgrep is needed, so CI's audits run it): the real config is text, a rule
    that needs the parser is refused, every kind of unmeasured place is named, a count that rose is flagged."""
    failures: list[str] = []

    def expect(label: str, got, want) -> None:
        ok = got == want
        print(f"  {'ok  ' if ok else 'FAIL'} {label}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(label)

    expect("the real invariants.yml is all text (check 0 refuses nothing)",
           parser_dependence(CONFIG.read_text(encoding="utf-8")), [])
    expect("the real invariants.yml declares every rule once", len(rule_ids(CONFIG)), len(rule_keys(CONFIG.read_text(encoding="utf-8"))))

    text_rule = "  - id: ok-rule\n    languages: [csharp]\n    severity: ERROR\n    message: >-\n      pattern: a message line\n" \
                "    paths:\n      include:\n        - \"src/**\"\n    pattern-regex: 'x'\n"
    expect("a single pattern-regex with a paths block and a message mentioning `pattern:` is text",
           parser_dependence(text_rule), [])
    for label, body in {
        "`pattern:`": "    pattern: Emit($A, ...)\n",
        "`patterns:` of two regexes": "    patterns:\n      - pattern-regex: 'a'\n      - pattern-not-regex: 'b'\n",
        "`pattern-either:`": "    pattern-either:\n      - pattern: a\n",
        "`pattern-inside` beside the regex": "    pattern-regex: 'a'\n    pattern-inside: class $C { ... }\n",
        "`metavariable-regex`": "    pattern-regex: 'a'\n    metavariable-regex: {}\n",
    }.items():
        refused = parser_dependence("  - id: bad-rule\n    languages: [csharp]\n" + body)
        expect(f"a rule with {label} is refused, by its id", [r.split(":")[0] for r in refused], ["bad-rule"])
    expect("a rule with no pattern-regex is refused",
           len(parser_dependence("  - id: empty-rule\n    languages: [csharp]\n    severity: ERROR\n")), 1)
    expect("a rule with two pattern-regex keys is refused",
           len(parser_dependence("  - id: twice\n    pattern-regex: 'a'\n    pattern-regex: 'b'\n")), 1)

    config_text = CONFIG.read_text(encoding="utf-8")
    expect("the real invariants.yml's comment-skipping rules all open with CODE_PREFIX", prefix_drift(config_text), [])
    expect("a rule with the tempered `(?!//)` prefix, or a prefix of its own, is refused by id",
           [r.split(":")[0] for r in prefix_drift(
               "  - id: old\n    pattern-regex: '(?m)^(?:(?!//)[^\\n])*?decimal'\n"
               "  - id: own\n    pattern-regex: '(?m)^(?:[^/])*?decimal'\n"
               "  - id: ok\n    pattern-regex: '(?m)^" + CODE_PREFIX + "decimal'\n")], ["old", "own"])
    # The line controls of Violations.cs, through the rules' real regexes (Python's `re` reads them as PCRE does here), so
    # CI's audits, which have no semgrep, still hold the string / char / comment behavior of the prefix.
    regexes = {rid: re.compile(rx) for rid, rx in rule_regexes(config_text).items()}
    decimal_rule = regexes["wiseowl-cobol-no-decimal"]
    for label, line, want in (
            ("a `//` inside a string literal", 'string url = "http://x"; decimal d = 1;', True),
            ("a `/` char literal", "char s = '/'; decimal d = 1;", True),
            ("a `\"` char literal", "char q = '\"'; decimal d = 1;", True),
            ("a verbatim string ending in a backslash", 'string p = @"C:\\dir\\"; decimal d = 1;', True),
            ("a verbatim string with a doubled quote and `//`", 'string p = @"a ""// b"" c"; decimal d = 1;', True),
            ("a divide-assign before the violation", "x /= 2; decimal d = 1;", True),
            ("the violation alone", "decimal d = 1;", True),
            ("a trailing `//` comment", "int a = 0; // decimal d = 1;", False),
            ("a whole-line `//` comment", "// decimal d = 1;", False),
            ("a `///` doc comment", "/// <c>decimal d = 1;</c>", False),
            ("a comment after a string", 'string s = "a"; // decimal d = 1;', False),
            ("a comment with an unbalanced quote", "int a; // it's \"quoted decimal d = 1;", False)):
        expect(f"no-decimal on {label}", bool(decimal_rule.search(line + "\n")), want)
    expect("raw-code-literal sees an Emit after a `//` inside a string, and not one in a comment",
           [bool(regexes["wiseowl-cobol-raw-diagnostic-code-literal"].search(s + "\n")) for s in
            ('var u = "http://x"; Emit(Severity.Error, "COBOL1234", "m");', '// Emit(Severity.Error, "COBOL1234", "m");')],
           [True, False])
    expect("byte-storage sees a field after a `//` inside a string, and not one in a comment",
           [bool(regexes["wiseowl-cobol-no-persistent-byte-storage"].search(s + "\n")) for s in
            ('var u = "http://x"; byte[] b = new byte[1];', "int a; // byte[] b;")], [True, False])

    ids = ["r1", "r2"]
    expect("every rule's control present, fired on an unread line: no problem",
           unparsed_control_problems(ids, {"r1.cs", "r2.cs"}, {"r1.cs": {"r1": {3}}, "r2.cs": {"r2": {3}}},
                                     {"r1.cs": [(3, 3)], "r2.cs": [(2, 4)]}), [])
    expect("a missing control, a stray file, a rule that does not fire, a hit outside the unread span, and a file read in full "
           "are each a problem naming the rule or file",
           [p.split(":")[0].split(" ")[0] for p in unparsed_control_problems(
               ["r1", "r2", "r3", "r4"], {"r2.cs", "r3.cs", "r4.cs", "x.cs"},
               {"r3.cs": {"r3": {9}}, "r4.cs": {"r4": {3}}}, {"r2.cs": [(1, 1)], "r3.cs": [(3, 3)], "r4.cs": []})],
           ["r1", "r2", "r3", "unparsed/r4.cs", "unparsed/x.cs"])
    src = "a\nb  // FIRES r1\nc  QUIET\nd  QUIET\n"
    expect("a FIRES line the rule misses and a QUIET line any rule hits are problems; the other lines pass",
           line_problems(src, {"r1": set(), "r2": {3}}), ["Violations.cs:2: r1 must fire here and does not",
                                                             "Violations.cs:3: r2 fires on a line that must stay quiet"])
    expect("FIRES and QUIET lines that hold pass", line_problems(src, {"r1": {2}}), [])
    expect("partial_spans lists each unread span with a forward-slashed path",
           partial_spans([{"type": ["PartialParsing", [{"path": "a\\b.cs", "start": {"line": 3}, "end": {"line": 5}}]]},
                          {"type": "Syntax error", "path": "c.cs"}]), [("a/b.cs", 3, 5)])

    partial = {"type": ["PartialParsing", [{"path": "src\\A\\B.cs", "start": {"line": 14, "col": 1}, "end": {"line": 4492, "col": 2}},
                                          {"path": "src\\A\\B.cs", "start": {"line": 5747, "col": 1}, "end": {"line": 5768, "col": 2}}]]}
    whole = {"type": "Syntax error", "path": "src\\A\\C.cs", "message": "Syntax error at line src\\A\\C.cs:1:\n `// Copyright`"}
    timeout = {"type": "Timeout", "message": "rule x timed out"}
    expect("a PartialParsing is counted by span and is not fatal; a whole-file error and a timeout are fatal, by file and message",
           classify_errors([partial, whole, timeout]),
           (["src/A/B.cs:14-4492", "src/A/B.cs:5747-5768"],
            ["src/A/C.cs  Syntax error: Syntax error at line src\\A\\C.cs:1:", "(no file)  Timeout: rule x timed out"]))
    expect("no error names nothing", classify_errors([]), ([], []))
    expect("an unreadable file no exception names, and an exception for a file now read, are both drift",
           exception_drift({"a.cs", "b.cs"}, {"b.cs": "why", "c.cs": "why"}), (["a.cs"], ["c.cs"]))
    expect("every exception names a file under src/ with a reason",
           [p for p, why in WHOLE_FILE_UNREADABLE.items() if not (p.startswith("src/") and why)], [])

    lines, up, down = compare_counts({"a": 3, "b": 5}, {"a": 4, "b": 2, "c": 0})
    expect("a rule whose count rose is UP and fails; one that fell is DOWN",
           (up, down, lines[0].strip().split()[0], lines[1].strip().split()[0]), (True, True, "UP", "DOWN"))
    expect("equal counts are neither", compare_counts({"a": 1}, {"a": 1})[1:], (False, False))

    print(f"=== SEMGREP VERIFY SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
