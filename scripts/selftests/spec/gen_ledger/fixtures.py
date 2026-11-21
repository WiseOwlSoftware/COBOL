"""The planted parts, survey rows and landing log the completion ledger's cases build (kb/Work PB5994): no case reads the
register, the inventory, the branch survey or the quota."""
import html
import re
import types

import selftest_cases

C = selftest_cases.subject(__file__)   # scripts/spec/gen_ledger.py
S = C.edition_status

NAMES = ("inventory GAP", "code reads", "open work notes", "catalog prose holes")
HEAD = {"sha": "abcd1234", "date": "2026-11-20"}


def sources(*counts):
    """Four sources answering `counts` (None: a source whose reader refuses); the notes part carries a HARM note."""
    def source(name, n):
        def read(year):
            if n is None:
                raise SystemExit(f"{name}: planted refusal")
            data = {"HARM": ["PB1"] * min(n, 1), "QUESTION": [], "TOOLING": [], "OTHER": []} if name == NAMES[2] else {}
            return S.Part(name, n, lines=(f"{n} planted",), data=data)
        return read
    return tuple(source(n, c) for n, c in zip(NAMES, counts))


def ledger(counts_by_year, flight=(), budget=None, landings=None):
    """A ledger of planted editions (`counts_by_year`: year -> four counts) in the page's order."""
    eds = tuple(C.measure_edition(y, sources(*counts_by_year.get(y, (0, 0, 0, 0)))) for y in C.ORDER)
    return C.Ledger(head=HEAD, editions=eds, titles={"PB1": "PB1 - a planted wrong answer"}, flight=flight,
                    budget=budget if budget is not None else C.Unmeasured("planted: no quota"),
                    landings=landings if landings is not None else C.Unmeasured("planted: no log"))


def verdicts(page):
    """Every verdict line the page prints, in order."""
    return [html.unescape(m) for m in re.findall(r'<p class="verdict [a-z]+">(.*?)</p>', page)]


def row(name, state, lead=None, why="evidence"):
    """A survey row in `prune_worktrees.Row`'s shape, as far as the page reads it."""
    return types.SimpleNamespace(name=name, state=state, why=why,
                                 report=types.SimpleNamespace(lead=lead) if lead else None)


LOG = """LANDING aaa1111
diff --git a/kb/Work/PB1.md b/kb/Work/PB1.md
new file mode 100644
--- /dev/null
+++ b/kb/Work/PB1.md
@@ -0,0 +1,3 @@
+status: open
LANDING bbb2222
diff --git a/kb/Work/PB2.md b/kb/Work/PB2.md
--- a/kb/Work/PB2.md
+++ b/kb/Work/PB2.md
@@ -5 +5 @@ kind: defect
-status: open
+status: landed
diff --git a/kb/Work/PB3/2026-11-20-x.md b/kb/Work/PB3/2026-11-20-x.md
new file mode 100644
--- /dev/null
+++ b/kb/Work/PB3/2026-11-20-x.md
@@ -0,0 +1 @@
+status: landed
"""
