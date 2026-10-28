#!/usr/bin/env python3
"""standard_streams.py — EVERY REPOSITORY PYTHON PROGRAM PUTS ITS STANDARD STREAMS IN UTF-8 AT STARTUP, IN ONE PLACE
(kb/Work PB3637, PB1546).

    from standard_streams import ensure_utf8         # every entry point: the FIRST statement of its __main__ block
    python scripts/standard_streams.py --check       # the rule below over every live tool, its --help probes included
    python scripts/standard_streams.py --self-test   # the planted cases, then --check's source scan of this tree

⛔ WHY. When a standard stream is not a console (a pipe, a redirect: every agent's shell, every gate and every capture),
CPython on Windows encodes it in the ANSI code page, cp1252, and the scripts' help texts, verdict lines and reports
carry ⛔, →, § and em dashes. So `python scripts/spec/work.py --help` died with UnicodeEncodeError inside argparse.
Each script had its own copy of `sys.stdout.reconfigure(encoding="utf-8")` (126 copies, written about ten ways, some
stdout only, some stderr only), and in most of them the copy ran inside main() AFTER parse_args(), which prints --help
and every usage error and exits before the copy is reached. ONE function, called first, replaces every copy.

This is the scripts' counterpart of the compiler's src/WiseOwl.Cobol.Runtime/IO/StandardStreams.cs#EnsureUtf8 (kb/Work
PB899), and it sits IN THE PROGRAM by the owner's direction (2026-10-10, kb/Work PB1546): "this would work if the
compiler startup (plus likely test cases) set the console output encoding to UTF-8"; the environment variable alone does
not do it. Measured: PYTHONIOENCODING=cp1252 beats PYTHONUTF8=1 for the standard streams, and a hand run from a shell
carries neither, so only a call in the program holds whatever the launcher sets. The launchers that set PYTHONUTF8 or
PYTHONIOENCODING for a child (tests/_shared/PythonInstrument.cs, scripts/self_tests.py, orchestrate.ps1's
Start-PyBackground) keep doing so, as "plus likely test cases" asks.

THE RULE (--check), over every tracked .py file under live_tools.ROOTS:
  1. COPY    no file but this one reconfigures a standard stream's encoding or errors handler: a `reconfigure(` call
             (or a `python -c` string holding one) that passes `encoding=` or `errors=` or `**` keywords, on however
             many lines it is written (read on the parse tree, kb/Work PB3822). A buffering-only call such as
             `sys.stdout.reconfigure(line_buffering=True)` is no copy.
  2. ENTRY   a file with an `if __name__ == "__main__":` block imports ensure_utf8 from this module and calls it as that
             block's first statement; a file with no such block that runs code at import (a hook, a stand-alone
             self-test) calls it at module level before its first such statement. A module of definitions needs none.
  3. HELP    every file of 2 that builds an argparse parser prints `--help` (exit 0) and a usage error (exit 2) with no
             traceback while its stdout and stderr are encoded cp1252 (PYTHONIOENCODING=cp1252, PYTHONUTF8=0): run, not
             read, because a scan cannot see a help text built another way (kb/Work PB2615). A parser that accepts
             unknown options (`parse_known_args`) gets the --help probe only, since an unknown option would run it.
Not live, so not scanned: `docs/**/evidence/**` and `adjudication/**` hold frozen records (live_tools.py says why).
"""
from __future__ import annotations

import sys

#: Each standard stream and its errors handler: the ones CPython's UTF-8 mode gives it (PEP 540), so a byte decoded with
#: surrogateescape (a file name, a child's output) is written back unchanged and a diagnostic never raises.
_STREAMS = (("stdin", "surrogateescape"), ("stdout", "surrogateescape"), ("stderr", "backslashreplace"))


def _is_utf8(stream) -> bool:
    import codecs
    try:
        return codecs.lookup(stream.encoding).name == "utf-8"
    except (LookupError, TypeError):  # no encoding, or one this interpreter does not know: not UTF-8
        return False


def ensure_utf8(line_buffering: bool = False) -> None:
    """Put stdin, stdout and stderr in UTF-8; with `line_buffering`, also flush stdout at every newline (a gate's log
    that another process tails). IDEMPOTENT BY CONSTRUCTION (kb/Work PB3823): a stream already in the target state, as
    every stream is under UTF-8 mode (PEP 540) or after an earlier call, is not touched, so a second call is a no-op
    even after the stream was read (`TextIOWrapper.reconfigure` refuses to change the encoding then, with
    io.UnsupportedOperation). That refusal still propagates for a stream that is NOT yet UTF-8: a call that comes
    after a read in another encoding has decoded those bytes wrongly and must not pass for a success. Safe on any
    platform, and in a process with no console or with a stream replaced by one that cannot be reconfigured (None under
    pythonw, a StringIO under a capture): such a stream is left as it is."""
    for name, errors in _STREAMS:
        stream = getattr(sys, name)
        reconfigure = getattr(stream, "reconfigure", None)
        if reconfigure is None:
            continue
        change = {} if _is_utf8(stream) and getattr(stream, "errors", None) == errors \
            else {"encoding": "utf-8", "errors": errors}
        if line_buffering and name == "stdout" and not getattr(stream, "line_buffering", False):
            change["line_buffering"] = True
        if change:
            reconfigure(**change)


# ── the audit (--check, --self-test): imported only when this file runs as a program ─────────────────────────────────

THIS = "scripts/standard_streams.py"
IMPORT = "from standard_streams import ensure_utf8"
#: A copy of the rule inside a STRING (a `python -c` program): a reconfigure call that sets the encoding or the errors
#: handler, or passes `**` keywords that may. A string is text, not a tree, so the match is a regex, and `[^)]*` and
#: `\s*` cross line breaks. Code is read on the parse tree (`_copy_lines`), never by this pattern.
COPY_RE = r"\.reconfigure\([^)]*(?:\b(?:encoding|errors)\s*=|\*\*)"
#: The usage-error probe's option: no parser in this tree defines it.
UNKNOWN_OPTION = "--standard-streams-probe-unknown-option"
PROBE_TIMEOUT_S = 120
#: What the backslashreplace handler writes for a character the stream's encoding lacks.
BACKSLASH_REPLACED = r"\\(?:x[0-9a-fA-F]{2}|u[0-9a-fA-F]{4}|U[0-9a-fA-F]{8})"


def _is_main_guard(node) -> bool:
    import ast
    return (isinstance(node, ast.If) and isinstance(node.test, ast.Compare)
            and isinstance(node.test.left, ast.Name) and node.test.left.id == "__name__"
            and any(isinstance(c, ast.Constant) and c.value == "__main__" for c in node.test.comparators))


def _is_call_to(node, name: str) -> bool:
    import ast
    return (isinstance(node, ast.Expr) and isinstance(node.value, ast.Call)
            and isinstance(node.value.func, ast.Name) and node.value.func.id == name)


def _runs_at_import(node) -> bool:
    """Whether a module-level statement runs code when the file is imported or run, beyond binding names: a call, a
    loop, a with, an assert, a try that does more than import (a hook's `try: data = json.load(sys.stdin)`). Imports,
    definitions, assignments, sys.path set-up, a docstring, an import fallback (`try: import yaml`) and a TYPE_CHECKING
    or __main__ guard do not."""
    import ast
    if isinstance(node, (ast.Import, ast.ImportFrom, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef,
                         ast.Assign, ast.AnnAssign, ast.AugAssign, ast.TypeAlias, ast.Pass)):
        return False
    if isinstance(node, ast.Expr):
        if isinstance(node.value, ast.Constant):
            return False
        func = node.value.func if isinstance(node.value, ast.Call) else None
        return not (isinstance(func, ast.Attribute) and ast.unparse(func.value) == "sys.path")
    if isinstance(node, ast.If):
        return not (_is_main_guard(node) or "TYPE_CHECKING" in ast.unparse(node.test))
    if isinstance(node, ast.Try):
        return not all(isinstance(n, (ast.Import, ast.ImportFrom)) for n in node.body)
    return True


def _copy_lines(tree) -> set[int]:
    """The line of each COPY (rule 1) in a parsed file, however it is written (kb/Work PB3822): a call of `reconfigure`
    (as `x.reconfigure(...)`, or a bare name bound to it) with an `encoding=` or `errors=` keyword or any `**`
    keywords, wrapped over any number of lines; and, in a string constant, the COPY_RE match, at the line it is on.
    A method bound to a name other than `reconfigure` and called through that name is not followed."""
    import ast
    import re
    lines = set()
    for node in ast.walk(tree):
        if isinstance(node, ast.Call):
            func = node.func
            name = func.attr if isinstance(func, ast.Attribute) else func.id if isinstance(func, ast.Name) else None
            if name == "reconfigure" and any(k.arg in ("encoding", "errors") or k.arg is None for k in node.keywords):
                lines.add(node.lineno)
        elif isinstance(node, ast.Constant) and isinstance(node.value, str):
            if (m := re.search(COPY_RE, node.value)) is not None:
                lines.add(node.lineno + node.value.count(chr(10), 0, m.start()))
    return lines


def scan(rel: str, source: str) -> list[str]:
    """Rules 1 and 2 over one file's source; each finding is `path:line: RULE: what`."""
    import ast
    if rel == THIS:
        return []
    tree = ast.parse(source, filename=rel)
    found = [f"{rel}:{n}: COPY: reconfigures a standard stream's encoding itself; call standard_streams.ensure_utf8()"
             for n in sorted(_copy_lines(tree))]
    guard = next((n for n in tree.body if _is_main_guard(n)), None)
    calls = any(isinstance(n, ast.Call) and isinstance(n.func, ast.Name) and n.func.id == "ensure_utf8"
                for n in ast.walk(tree))
    imported = any(isinstance(n, ast.ImportFrom) and n.module == "standard_streams"
                   and any(a.name == "ensure_utf8" and a.asname is None for a in n.names) for n in ast.walk(tree))
    if guard is not None:
        if not _is_call_to(guard.body[0], "ensure_utf8"):
            found.append(f"{rel}:{guard.body[0].lineno}: ENTRY: the __main__ block's first statement is not "
                         f"ensure_utf8()")
    else:
        first = next((n for n in tree.body if _runs_at_import(n) and not _is_call_to(n, "ensure_utf8")), None)
        call = next((n for n in tree.body if _is_call_to(n, "ensure_utf8")), None)
        if first is not None and (call is None or call.lineno > first.lineno):
            found.append(f"{rel}:{first.lineno}: ENTRY: runs code at import before any module-level ensure_utf8()")
    if calls and not imported:
        found.append(f"{rel}:1: ENTRY: calls ensure_utf8 without `{IMPORT}`")
    return found


def is_cli(source: str) -> bool:
    """Whether a file is a probed entry point (rule 3): it builds an argparse parser and runs as a program."""
    import ast
    return "ArgumentParser(" in source and any(_is_main_guard(n) for n in ast.parse(source).body)


def probe_env() -> dict[str, str]:
    """The environment rule 3 probes under: a standard-stream encoding that cannot print ⛔ on every host."""
    import os
    env = {k: v for k, v in os.environ.items() if k not in ("PYTHONUTF8", "PYTHONIOENCODING")}
    env.update(PYTHONIOENCODING="cp1252", PYTHONUTF8="0")
    return env


def probe(repo, path, args: list[str], want: int) -> str | None:
    """Run `path args` under probe_env(); None when it exits `want` with no traceback and a legible stderr, else what
    went wrong. stdout raises on a character cp1252 lacks (its errors handler is strict), so a crash shows as the
    traceback; stderr's handler is backslashreplace, so the same character arrives as `\\u26d4` and the run exits
    cleanly: that escape is the stderr arm's failure."""
    import re
    import subprocess
    try:
        r = subprocess.run([sys.executable, str(path), *args], cwd=repo, env=probe_env(), stdin=subprocess.DEVNULL,
                           capture_output=True, timeout=PROBE_TIMEOUT_S)
    except subprocess.TimeoutExpired:
        return f"`{' '.join(args)}` did not exit within {PROBE_TIMEOUT_S} s"
    err = r.stderr.decode("utf-8", errors="replace")
    tail = " | ".join(err.strip().splitlines()[-2:]) or "(no stderr)"
    if r.returncode != want or "Traceback (most recent call last)" in err:
        return f"`{' '.join(args)}` exited {r.returncode}, want {want}: {tail}"
    if (escape := re.search(BACKSLASH_REPLACED, err)) is not None:
        return f"`{' '.join(args)}` wrote stderr through cp1252 (`{escape.group()}` for a character it lacks): {tail}"
    return None


def probe_cli(repo, rel: str, source: str) -> list[str]:
    """Rule 3 over one entry point: its --help, then (unless it accepts unknown options) a usage error."""
    path = repo / rel
    found = []
    if (why := probe(repo, path, ["--help"], 0)) is not None:
        found.append(f"{rel}:1: HELP: {why}")
    if "parse_known_args" not in source and (why := probe(repo, path, [UNKNOWN_OPTION], 2)) is not None:
        found.append(f"{rel}:1: HELP: {why}")
    return found


def live_python(repo) -> list[str]:
    import live_tools  # this directory's: the one listing of the live tools' trees
    return live_tools.tracked(repo, (".py",))


def check(repo, probes: bool = True) -> int:
    import concurrent.futures
    import os
    files = live_python(repo)
    sources = {rel: (repo / rel).read_text(encoding="utf-8") for rel in files}
    found = [f for rel in files for f in scan(rel, sources[rel])]
    clis = [rel for rel in files if rel != THIS and is_cli(sources[rel])] if probes else []
    with concurrent.futures.ThreadPoolExecutor(max_workers=min(32, os.cpu_count() or 4)) as pool:
        for got in pool.map(lambda rel: probe_cli(repo, rel, sources[rel]), clis):
            found += got
    for f in found:
        print(f"  {f}")
    verdict = "FAIL" if found else "PASS"
    print(f"=== STANDARD STREAMS: {verdict} — {len(files)} live Python files scanned, {len(clis)} entry points "
          f"probed under cp1252, {len(found)} finding(s) ===")
    if not files:
        print("=== STANDARD STREAMS: FAIL — the scan read no file under live_tools.ROOTS: a net over nothing ===")
        return 1
    return 1 if found else 0


def self_test(repo) -> int:
    import os
    import subprocess
    import tempfile
    from pathlib import Path
    scripts = Path(__file__).resolve().parent
    failures: list[str] = []

    def expect(name: str, got, want) -> None:
        ok = got == want
        print(f"{'PASS' if ok else 'FAIL'}  {name}" + ("" if ok else f": got {got!r}, want {want!r}"))
        if not ok:
            failures.append(name)

    def rules(source: str) -> list[str]:
        return [f.split(": ")[1] for f in scan("scripts/x.py", source)]

    head = f"import sys\n{IMPORT}\n"
    main = "\n\nif __name__ == '__main__':\n    ensure_utf8()\n    main()\n"
    cases = (
        ("an entry point calling ensure_utf8() first", head + "def main(): pass" + main, []),
        ("a stdout copy", head + "def main():\n    sys.stdout.reconfigure(encoding='utf-8')" + main, ["COPY"]),
        ("an errors-only copy", head + "def main():\n    sys.stdout.reconfigure(errors='replace')" + main, ["COPY"]),
        ("a stream loop's copy", head + "for s in (sys.stdout, sys.stderr):\n    s.reconfigure(encoding='utf-8',"
                                        " errors='replace')" + main, ["COPY"]),
        ("a copy inside a `python -c` string",
         head + "ARGV = ['-c', \"import sys; sys.stdout.reconfigure(encoding='utf-8'); print(1)\"]" + main, ["COPY"]),
        ("a copy wrapped onto a second line (PB3822)",
         head + "def main():\n    sys.stdout.reconfigure(\n        encoding='utf-8')" + main, ["COPY"]),
        ("an errors-only copy after a buffering keyword on the line before",
         head + "def main():\n    sys.stdout.reconfigure(line_buffering=True,\n                           errors='replace')"
         + main, ["COPY"]),
        ("a copy passed through **kwargs (PB3822)", head + "def main():\n    sys.stdout.reconfigure(**KW)" + main,
         ["COPY"]),
        ("a copy through a bound method named reconfigure",
         head + "def main():\n    reconfigure = sys.stdout.reconfigure\n    reconfigure(encoding='utf-8')" + main,
         ["COPY"]),
        ("a copy inside a wrapped `python -c` string",
         head + "ARGV = ['-c', '''import sys\\nsys.stdout.reconfigure(\\n    encoding='utf-8')''']" + main, ["COPY"]),
        ("a reconfigure named in a comment is no copy",
         head + "def main():\n    # sys.stdout.reconfigure(encoding='utf-8')\n    pass" + main, []),
        ("a buffering-only reconfigure is no copy",
         head + "def main():\n    sys.stdout.reconfigure(line_buffering=True)" + main, []),
        ("a __main__ block without the call", head + "def main(): pass\n\nif __name__ == '__main__':\n    main()\n",
         ["ENTRY"]),
        ("a __main__ block calling it second",
         head + "def main(): pass\n\nif __name__ == '__main__':\n    main()\n    ensure_utf8()\n", ["ENTRY"]),
        ("a call without the import", "def main(): pass" + main, ["ENTRY"]),
        ("a hook running code at import without the call", "import json, sys\nprint(json.load(sys.stdin))\n",
         ["ENTRY"]),
        ("a hook calling it after its first code", head + "print(1)\nensure_utf8()\n", ["ENTRY"]),
        ("a hook calling it before its first code",
         "import sys\nsys.path.insert(0, 'x')\n" + IMPORT + "\n\nensure_utf8()\nprint(1)\n", []),
        ("a module of definitions needs no call",
         "'''doc'''\nimport sys\ntry:\n    import yaml\nexcept ImportError:\n    yaml = None\nX: int = 1\n"
         "def f(): pass\nclass C: pass\n", []),
    )
    for name, source, want in cases:
        expect(f"scan: {name} -> {want}", rules(source), want)
    expect("scan: this file is the one place, not a copy", scan(THIS, "x.reconfigure(encoding='utf-8')\n"), [])
    expect("scan: a wrapped copy is reported at its call's first line, and one inside a string at the line it is on",
           [f.split(": ")[0] for f in scan("scripts/x.py", "import sys\nsys.stdout.reconfigure(\n    encoding='u')\n"
                                                           "S = '''a\nb.reconfigure(\nerrors='r')'''\n") if "COPY" in f],
           ["scripts/x.py:2", "scripts/x.py:5"])

    # PB3823: a second call changes nothing, even after stdin was read, where reconfigure would refuse.
    def run_stdin(program: str, **env: str) -> tuple[int, str, str]:
        r = subprocess.run([sys.executable, "-c", f"import sys; sys.path.insert(0, {str(scripts)!r})\n{program}"],
                           input=b"a line\n", capture_output=True, timeout=PROBE_TIMEOUT_S,
                           env={**os.environ, **env})
        return r.returncode, r.stdout.decode("utf-8", errors="replace").strip(), r.stderr.decode("utf-8", errors="replace")

    twice = "from standard_streams import ensure_utf8\n"
    cp1252 = {"PYTHONIOENCODING": "cp1252", "PYTHONUTF8": "0"}
    for label, program, env in (
            ("a call, a read of stdin, then a second call", twice + "ensure_utf8(); sys.stdin.readline(); "
             "ensure_utf8(); ensure_utf8(line_buffering=True); print('ok')", cp1252),
            ("a read of stdin under UTF-8 mode, then two calls", twice + "sys.stdin.readline(); ensure_utf8(); "
             "ensure_utf8(); print('ok')", {"PYTHONUTF8": "1", "PYTHONIOENCODING": ""})):
        code, out, err = run_stdin(program, **env)
        expect(f"ensure_utf8 twice: {label} -> exit 0 and no error", (code, out, err.strip()), (0, "ok", ""))
    code, out, err = run_stdin(twice + "sys.stdin.readline(); ensure_utf8(); print('ok')", **cp1252)
    expect("ensure_utf8 after a cp1252 read of stdin is refused, not passed off as a success",
           (code, "UnsupportedOperation" in err), (1, True))

    # Rule 3 can fail on this host: the probe environment really is hostile, and the call really cures it.
    planted =("import argparse, sys\n"
               f"sys.path.insert(0, {str(scripts)!r})\n{IMPORT}\n\n"
               "def main():\n"
               "    ap = argparse.ArgumentParser(prog='⛔probe', description='⛔ → §')\n"
               "    ap.parse_args()\n\n"
               "if __name__ == '__main__':\n"
               "    {call}main()\n")
    with tempfile.TemporaryDirectory(prefix="standard-streams-") as d:
        for label, call, want in (("without ensure_utf8()", "", 2), ("with ensure_utf8() first", "ensure_utf8()\n    ", 0)):
            p = Path(d) / "probe_cli.py"
            p.write_text(planted.replace("{call}", call), encoding="utf-8", newline="\n")
            got = probe_cli(Path(d), "probe_cli.py", p.read_text(encoding="utf-8"))
            expect(f"probe: a parser whose help and usage carry ⛔, {label} -> {want} finding(s)", len(got), want)
    expect("probe: an entry point is a file with a parser and a __main__ block",
           [is_cli("import argparse\nargparse.ArgumentParser()\nif __name__ == '__main__': pass\n"),
            is_cli("import argparse\nargparse.ArgumentParser()\n"), is_cli("if __name__ == '__main__': pass\n")],
           [True, False, False])

    print("this tree (rules 1 and 2; --check adds the probes):")
    expect("this tree's live Python files hold rules 1 and 2", check(repo, probes=False), 0)
    print(f"=== STANDARD STREAMS SELF-TEST: {'FAIL — ' + str(len(failures)) if failures else 'PASS'} ===")
    return 1 if failures else 0


def main() -> int:
    import argparse
    from pathlib import Path
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    group = ap.add_mutually_exclusive_group(required=True)
    group.add_argument("--check", action="store_true", help="rules 1-3 over every live tool; exit 1 on a finding")
    group.add_argument("--self-test", action="store_true", help="the planted cases, then rules 1-2 over this tree")
    ap.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1], help=argparse.SUPPRESS)
    args = ap.parse_args()
    return self_test(args.root) if args.self_test else check(args.root)


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
