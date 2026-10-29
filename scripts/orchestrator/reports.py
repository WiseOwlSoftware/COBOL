"""THE FLEET-REPORT READER (kb/Work PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.3): a report
file's name (coord.REPORT_NAME), its header (the lines above the first `## ` heading), its STATUS LINE, its header
fields (branch, worktree, HEAD, base, gate run), its branch-review STAMP (kb/Work PB3027), the one "finished report"
predicate (Report.finished, kb/Work PB3181), the one order of reports (report_order, kb/Work PB3625), and the notes a
report answers (answered_notes, through the dispatch-ledger groups coord.ledger_groups reads; kb/Work PB3152, PB3473).

Every reader of a report reads it here: the wave planner (plan_wave.py), the branch survey (prune_worktrees.py, through
report_index and newest_report), the landing lane, the landing check and the landing measure, review_findings.py's
stamp, progress.py and turns_by_group_size.py. A second parse of a report header anywhere is a defect: it is how one
report read as two different branches' (kb/Work PB3077).

This module imports only coord among the orchestrator's modules (the import DAG of section 16.10,
scripts/script_layering.py), and run_gate_legs lazily, for a GATE-PENDING report's gate (Report.gate_state).
"""
from __future__ import annotations

import dataclasses
import functools
import pathlib
import re
import subprocess
import sys
import time
from typing import Any, Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402  (the coordination directory: REPORT_NAME, the dispatch ledger's reader)

REPO = coord.REPO

STATUS_WORD = re.compile(r"^\**(?:Status:\**)?\s*\**\s*(?P<s>DONE|SPLIT|DISCHARGED|BLOCKED|NOT STARTED|GATE-PENDING)\b",
                         re.I)


#: A GATE-PENDING report's gate run (kb/Work PB3536): `Gate run: <run directory>`, read at a field position like
#: BRANCH_FIELD (the start of a header line, or after the template's ` · ` separator).
GATE_RUN = re.compile(r"(?im)(?:^|·)[ \t]*\**gate run:\**\s*`?([^\s`*]+)")
BRANCH = re.compile(r"\b(worktree-[A-Za-z0-9_][A-Za-z0-9_.\-]*[A-Za-z0-9_])")


#: The header's own branch FIELD — `Branch: x` (the O11 header) or `**branch:** `x`` (the report template) — which
#: names exactly the report's branch, read at a FIELD POSITION only: the start of a header line or right after the
#: template's ` · ` separator. Mid-prose `branch:` is not a field — real reports say "this branch: every file …"
#: (w1043x, w1055z), and an unanchored rule took `every` (review round 1 of PB3077). BRANCH, the first `worktree-…`
#: word, is only the fallback for a header with no field (kb/Work PB3077; prune_worktrees.newest_report applies
#: the same field-first rule).
BRANCH_FIELD = re.compile(r"(?im)(?:^|·)[ \t]*\**branch:\**\s*`?([A-Za-z0-9_][A-Za-z0-9_.\-]*[A-Za-z0-9_])")


#: A review stamp line (review_findings.py stamp, kb/Work PB3027), placeholder or not: its reviewer label
#: `land-review-<branch>-r1` names the branch with a suffix, so the branch, worktree and head are read around it.
REVIEW_LINE = re.compile(r"^\**Review:", re.I)


#: The header's worktree: an absolute path ending `worktrees<sep><name>`, a drive path with either separator or a POSIX
#: one (never the tail of a drive path: a `/` after a word or a colon starts none). A GATE-PENDING report's relative
#: `Gate run:` is resolved against it (run_gate_legs.resolve_run; review R1-1 of wave 1152), on every host.
WORKTREE = re.compile(r"([A-Za-z]:[\\/][^\s`*]*?[\\/]worktrees[\\/][A-Za-z0-9_.\-]+|(?<![\w:])/[^\s`*]*?/worktrees/"
                      r"[A-Za-z0-9_.\-]+)")


def sha_field(word: str) -> tuple[re.Pattern[str], re.Pattern[str]]:
    """(the FIELD, the fallback) for a header's `word <sha>` field, read like BRANCH_FIELD: first at a field position
    (a line's start, or after the template's ` · ` or a ` | ` separator: `HEAD: x`, `**head:** `x``, `· head x`), and
    only when no field holds it, the first `word <sha>` anywhere in the header (`Base x  Head y`), any case. Prose
    before the field must not win: w1080f's header says "(reviewed head db7e19186 merged in) · head 5df3c56a3", and the
    unanchored first match read the predecessor's head as the report's (kb/Work PB3181; measured over the 360 reports
    on disk 2026-10-15: this rule changes that one and reads 17 the old one missed, 12 name no head)."""
    return (re.compile(r"(?im)(?:^|[·|])[ \t]*\**" + word + r"\b:?\**[ \t]*`?([0-9a-f]{7,40})\b"),
            re.compile(r"(?i)\b" + word + r"\b[:*`\s]*([0-9a-f]{7,40})\b"))


#: The header's HEAD (the branch tip the report describes: the branch survey admits a report only when this head, or a
#: head followed only by mechanical commits, is the branch's tip) and its BASE (the commit the branch was cut from: a
#: DISCHARGED report whose HEAD is its base produced no branch work, Report.finished).
HEAD, BASE = sha_field("head"), sha_field("base")


def header_sha(fields: tuple[re.Pattern[str], re.Pattern[str]], header: str) -> str | None:
    m = fields[0].search(header) or fields[1].search(header)
    return m.group(1) if m else None


# THE BRANCH REVIEW'S STAMP (kb/Work PB3027; DESIGN-test-build-ci.md section 3.17.2 step 2). The read-only reviewer of a
# DONE branch writes ONE line into its report's header, through `review_findings.py stamp` and never by hand:
#   **Review:** CLEAN <reviewer> <head sha>                              the branch at that head may land
#   **Review:** FINDINGS <n> <reviewer> <head sha> <findings file>        a finisher answers them first
# A header with no parsable stamp, or one naming another head than the branch's tip, is not reviewed.
REVIEW = re.compile(r"^\**Review:\**\s*(?P<verdict>CLEAN|FINDINGS)\s+(?:(?P<n>\d+)\s+)?(?P<reviewer>[^\s`*]+)\s+"
                    r"(?P<sha>[0-9a-f]{7,40})\b(?:\s+(?P<file>\S+))?", re.M)
FINISH_STATUSES = {"SPLIT", "NOT STARTED"}


@dataclasses.dataclass(frozen=True)
class Review:
    verdict: str             # CLEAN | FINDINGS
    findings: int
    reviewer: str
    sha: str                 # the head the reviewer reviewed
    file: str | None         # the findings file (FINDINGS only)

    def covers(self, tip: str | None) -> bool:
        """The stamp is about exactly this head: a commit made after the review is unreviewed work."""
        return bool(tip) and (tip.startswith(self.sha) or self.sha.startswith(tip))


def parse_review(header: str) -> Review | None:
    """The review stamp in a report's header (REVIEW), or None. The one reader: this planner (a FINDINGS stamp makes
    a finisher) and prune_worktrees.py (a DONE branch is WAITING TO LAND only under a CLEAN stamp at its tip)."""
    m = REVIEW.search(header)
    if not m or (m.group("verdict") == "FINDINGS" and not (m.group("n") and m.group("file"))):
        return None
    findings = m.group("verdict") == "FINDINGS"
    return Review(m.group("verdict"), int(m.group("n")) if findings else 0, m.group("reviewer"), m.group("sha"),
                  m.group("file") if findings else None)


@dataclasses.dataclass
class Report:
    path: pathlib.Path
    wave: int
    slug: str
    lead: str
    status: str
    notes: list[str]   # the notes the report ANSWERS: its lead, then its dispatch group's (answered_notes), never its prose's
    not_started: bool
    branch: str | None
    worktree: str | None
    head: str | None   # the commit the report describes: the header's HEAD, else the head its review stamp names (parse_report)
    review: Review | None = None
    base: str | None = None
    gate_run: str | None = None   # GATE-PENDING: the run directory of the gate its agent handed over (kb/Work PB3536)

    @property
    def finished(self) -> bool:
        """THE ONE "FINISHED REPORT" PREDICATE (kb/Work PB3181): DONE, or DISCHARGED with branch work (a HEAD past its
        base: the note's retirement and its DEVLOG entry), or GATE-PENDING whose handed gate is GREEN at its HEAD
        (`gate_state`, kb/Work PB3536). The rolling wave asks it through `--finished` before it reviews a return, and
        the branch survey (prune_worktrees.lifecycle) admits only such a report to the landing states. A SPLIT, BLOCKED
        or NOT STARTED report, a GATE-PENDING one whose gate is not GREEN, or a header with no status line, is
        unfinished."""
        return (self.status == "DONE"
                or (self.status == "DISCHARGED" and bool(self.head) and not same_commit(self.head, self.base))
                or (self.status == "GATE-PENDING" and self.gate_state == "GREEN"))

    @functools.cached_property
    def gate_state(self) -> str:
        """The gate a GATE-PENDING report's agent handed over at its turn cap (kb/Work PB3536), read from its run
        directory (`run_gate_legs.resolve_run`: a relative name is the report's WORKTREE's run, never the reader's
        checkout's) through the gate's ONE reader (`run_gate_legs.watch`, no wait): GREEN — its verdict record is GREEN
        (exit 0) for this report's HEAD and measured that commit's own tree, unchanged while it ran (the measured tree
        leaves out coord.NEVER_WORK, so the harness's edit of its tracked settings file is no difference); RUNNING — no
        verdict yet and the gate holds its worktree's gate lock (or has taken none yet); ENDED — any other end (RED, NOT
        RUN, BUSY, a gate that DIED, a GREEN of another head or tree), or no run named: a finisher's work. NONE for any other
        status."""
        if self.status != "GATE-PENDING":
            return "NONE"
        if not self.gate_run or not self.head:
            return "ENDED"
        import run_gate_legs  # noqa: PLC0415  (scripts/: the gate's own reader; heavy, so only for this status)
        run = run_gate_legs.resolve_run(self.gate_run, self.worktree)   # a relative name is the WORKTREE's run (R1-1)
        code, _ = run_gate_legs.watch(run, 0, run_gate_legs.gate_running(run))
        if code == run_gate_legs.AWAIT_PENDING_EXIT:
            return "RUNNING"
        rec = run_gate_legs.read_record(run) if code == 0 else None
        if not rec or not same_commit(str(rec.get("head") or ""), self.head):
            return "ENDED"
        committed = None
        # the commit is the report's worktree's; one since pruned leaves it in the object store this checkout shares
        for where in ([self.worktree] if self.worktree and pathlib.Path(self.worktree).is_dir() else []) + [str(REPO)]:
            p = subprocess.run(["git", "-C", where, "rev-parse", "--verify", "--quiet", f"{rec['head']}^{{tree}}"],
                               capture_output=True, text=True, encoding="utf-8", errors="replace")
            if p.returncode == 0 and p.stdout.strip():
                committed = p.stdout.strip()
                break
        return "GREEN" if committed and rec.get("tree") == rec.get("tree_end") == committed else "ENDED"


def same_commit(a: str | None, b: str | None) -> bool:
    """Two shas name one commit: equal, or one an abbreviation of the other (a report writes 9 or 12 characters)."""
    return bool(a) and bool(b) and (a.startswith(b) or b.startswith(a))


def header_lines(lines: list[str]) -> list[str]:
    """A report's HEADER: its lines above the first `## ` heading (a heading on the first line does not end it). The
    one definition this planner, the branch survey and review_findings.py stamp read."""
    return lines[:next((i for i, line in enumerate(lines) if i > 0 and line.startswith("## ")), len(lines))]


def status_line(header: list[str]) -> tuple[int, str] | None:
    """(index, status) of the header's STATUS LINE: the first of its first six lines that STATUS_WORD reads
    (`Status: DONE`, `**Status:** DONE · …`, or the bare word), or None when it has none. review_findings.py stamps the
    review right under it, and refuses a header without one (kb/Work PB3181 shape 3)."""
    return next(((i, s.group("s").upper()) for i, l in enumerate(header[:6]) if (s := STATUS_WORD.match(l.strip()))),
                None)


def parse_report(p: pathlib.Path) -> Report | None:
    m = coord.REPORT_NAME.match(p.name)
    if not m:
        return None
    header = header_lines(p.read_text(encoding="utf-8", errors="replace").splitlines())
    text = "\n".join(header)
    status = s[1] if (s := status_line(header)) else ""
    if not status and re.search(r"\bSPLIT\b", text):
        status = "SPLIT"
    # The branch is the header's own field, never a word of the review stamp (kb/Work PB3077): review_findings.py
    # stamps the review one line under Status, ABOVE the Branch field of an O11 header, and its reviewer label
    # `land-review-worktree-…-r1` was the header's first `worktree-…` word — a branch that does not exist, ABSENT,
    # so every reviewed branch read as landed and wave 1064's finishers were told to merge `worktree-…-r1`.
    unstamped = "\n".join(l for l in header if not REVIEW_LINE.match(l.strip()))
    b, w = BRANCH_FIELD.search(unstamped) or BRANCH.search(unstamped), WORKTREE.search(unstamped)
    review = parse_review(text)
    # THE COMMIT THE REPORT DESCRIBES (kb/Work PB3476): its header's HEAD field, else the head its review stamp names.
    # The reviewer stamps the report AT a head of its branch (review_findings.py stamp writes the head it reviewed), so
    # the stamp is the stronger statement of what the report covers. A header whose HEAD field gives no sha ("HEAD: see
    # the trailing commit") described no commit, and a branch reviewed CLEAN at its tip sat IN FLIGHT, then a DECISION,
    # outside the landing lane (w1136c, twice). One owner: the branch survey, the landing lane and the landing measure
    # all read this field.
    head = header_sha(HEAD, unstamped) or (review.sha if review else None)
    return Report(p, int(m.group("wave")), f"w{m.group('wave')}{m.group('letter')}", m.group("lead"), status,
                  [m.group("lead")],
                  bool(re.search(r"(?i)not\s+started", text)), b.group(1) if b else None, w.group(1) if w else None,
                  head, review, header_sha(BASE, unstamped),
                  (g.group(1) if (g := GATE_RUN.search(unstamped)) else None))


def report_order(r: Report) -> tuple[float, int, str]:
    """THE ONE ORDER OF FIX-LANE REPORTS, oldest first (kb/Work PB3625): the report's write time, then its wave number,
    then its file name. Every reader that takes "the newest report" (the planner's `newest`, the branch survey's
    `newest_report`, the landing check's `report_of`, the landing measure's chain) reads this order through
    `load_reports`. The wave number is a name, not a clock: two lanes number their waves differently (date-numbered
    20261024 against serial 1157), so ordering by it first made a superseded report outrank a later one. `next_wave`
    takes the highest wave number for a different job (a number nobody has used) and is right to ignore time."""
    return (r.path.stat().st_mtime, r.wave, r.path.name)


def load_reports(reports_dir: pathlib.Path | None, coord_dir: pathlib.Path | None = None) -> list[Report]:
    """The reports in `reports_dir`, oldest first: by WRITE TIME, then wave, then file name (`report_order`). The wave
    number is no order across lanes (kb/Work PB3625: the loop's waves are date-numbered, 20261024, the Mythos lane's
    serial, 1157, so every date-numbered report outranked every serial one and a superseded SPLIT report stayed a
    note's newest beside a later DONE one), so it only breaks a tie. The name makes the order TOTAL: two reports of
    ONE wave written within one file-system tick tie on the first two keys (reports of different waves written in one
    tick are ordered by the second), and a tie left to `glob` takes the directory's enumeration order, which is the name on NTFS and a hash order on ext4, so a note's
    newest report, the order finishers are planned in and which of two chained finishers is the successor differed
    between Windows and Linux (kb/Work PB3528's self-test, red only under the Linux gate). With `coord_dir`, each
    report's `notes` are completed from the dispatch ledger there (`answered_notes`): the planner passes it; the readers
    of a report's status and branch (the branch survey, the landing checks) have no use for the notes and read the lead
    alone."""
    if not reports_dir or not reports_dir.is_dir():
        return []
    groups = coord.ledger_groups(coord_dir) if coord_dir else []
    reps = [r for p in reports_dir.glob("w*-PB*-report.md") if (r := parse_report(p))]
    for r in reps:
        r.notes = answered_notes(r, groups)
    return sorted(reps, key=report_order)


def ledger_slug(g: dict[str, Any]) -> str:
    """A ledger group's report slug, `w<wave><letter>` (its report is `<slug>-<lead>-report.md`, coord.REPORT_NAME): the
    group's recorded `slug`, else the planner's own spelling."""
    return str(g.get("slug") or f"w{g['wave']}{g['letter'].lower()}").lower()


def answers(r: Report, g: dict[str, Any]) -> bool:
    """THE ONE "A DISPATCH RETURNED" PREDICATE (kb/Work PB3311, PB3152): the report is the group's OWN (its slug, the
    ledger group's `w<wave><letter>`) and was written at or after the group's `at`. Never a report that merely NAMES
    one of the group's notes: a branch reviewer stamps its `Review:` line into the reviewed report, which moves that
    report's mtime past every later dispatch of the same notes (PB3198: a review finisher's dispatch looked answered
    by the stamp on the very report it was dispatched to answer), and a report header mentions notes it does not own
    (PB3152). The time bound keeps an EARLIER dispatch's report of a reused slug (two sessions planning one wave
    number) from answering this one. Both readers of "answered" call it: held_by_dispatch (the notes' hold) and
    inflight_file_sets (the group's file set). A report whose file cannot be read answers nothing: the hold is the
    safe side of a double dispatch."""
    try:
        return r.slug.lower() == ledger_slug(g) and r.path.stat().st_mtime >= g.get("at", 0.0)
    except OSError:
        return False


def answered_notes(r: Report, groups: list[dict[str, Any]]) -> list[str]:
    """THE NOTES A REPORT ANSWERS (kb/Work PB3152, PB3473): its LEAD (the id in its file name) plus the notes of the
    dispatch-ledger group it `answers`, the notes its group was DISPATCHED with. Never the ids its header mentions: a
    header carries prose (a "not started" aside, a cross-reference, a merge note), and every note it named was taken as
    this report's, so a DONE report that only mentioned PB3337 put PB3337 in `awaiting landing` (wave 1135) and a
    FINDINGS report that mentioned PB3101 had its finisher planned under PB3101 (wave 1076). A report no ledger group
    knows (an attended branch's) answers its lead alone."""
    return list(dict.fromkeys([r.lead, *(n for g in groups if answers(r, g) for n in g.get("notes", []))]))


def age_minutes(*paths: pathlib.Path) -> float:
    """Minutes since the newest of `paths` changed (a missing path is ignored; none present = 0, i.e. fresh): a report's
    age here (report_index), a worktree's in the branch survey (prune_worktrees.survey)."""
    stamps = []
    for p in paths:
        try:
            stamps.append(p.stat().st_mtime)
        except OSError:
            pass
    return (time.time() - max(stamps)) / 60 if stamps else 0.0


@dataclasses.dataclass
class IndexedReport:
    """A report as the BRANCH SURVEY indexes it (report_index; prune_worktrees.lifecycle reads it): the fields Report
    parsed, plus the two facts the survey adds, whether its lead note is terminal and its age. It keeps the report's
    PATH, not only its name: the landing lane hands a row's report to the review lane, which reads the file itself
    (kb/Work PB4055)."""
    path: pathlib.Path              # the report file, <reports>/w<wave><letter>-PB<lead>-report.md
    status: str                    # DONE | SPLIT | DISCHARGED | BLOCKED | NOT STARTED | '' (parse_report)
    lead: str                       # its lead note
    lead_terminal: bool             # the lead note is landed or retired: the work reached main some other way
    age_min: float                  # minutes since the report was written
    header: str = dataclasses.field(default='', repr=False)
    review: Review | None = None    # the header's branch-review stamp (parse_review, kb/Work PB3027)
    head: str | None = None         # the header's HEAD: the branch tip the report describes (HEAD)
    finished: bool = False          # Report.finished: DONE, or DISCHARGED with a HEAD past its base

    @property
    def name(self) -> str:
        """The file name, w<wave><letter>-PB<lead>-report.md: what the survey's evidence prints."""
        return self.path.name


def report_index(reports_dir: pathlib.Path | None, terminal: Callable[[str], bool]) -> list[IndexedReport]:
    """Every fix-lane report, oldest first, through load_reports (coord.REPORT_NAME, the status line), never a fork of
    it: a suffixed name is invisible to the survey exactly as it is to the planner (kb/Work PB2602). The header (up to
    the first `## ` heading, header_lines, as parse_report reads it) is where a report names its branch and
    worktree."""
    if not reports_dir or not reports_dir.is_dir():
        return []
    out = []
    for r in load_reports(reports_dir):
        header = header_lines(r.path.read_text(encoding='utf-8', errors='replace').splitlines())
        out.append(IndexedReport(r.path, r.status, r.lead, terminal(r.lead), age_minutes(r.path), '\n'.join(header),
                                 r.review, r.head, r.finished))
    return out


#: The branch survey's MATCHING rule (newest_report): every name after a `branch` word, a field (`Branch: x`,
#: `**branch:** `x``) or not (`(branch x)`, prose). It is NOT the header's branch field (BRANCH_FIELD, read at a field
#: position only): measured 2026-10-25 over the 802 reports on disk, 206 headers name another branch set under it,
#: chiefly a successor's report naming its predecessor's branch, which this rule credits the report to as well.
BRANCH_MENTION = re.compile(r'(?i)\bbranch\b[\s:*`(]*([A-Za-z0-9_][\w.\-/]*[\w])')


def newest_report(reports: list[IndexedReport], branch: str | None, path: str | None) -> IndexedReport | None:
    """The newest report about this branch. A header with a `branch` mention (BRANCH_MENTION: `Branch: x`,
    `**branch:** `x``, `(branch x)`) is about exactly the branch(es) it names: a header that ALSO names another
    branch, e.g. as its base or its predecessor, is not about that one (2026-10-08: w1037r's report names landing-lease
    as its base). A header with no branch mention is about any branch it names as a whole word, or whose worktree
    directory it names (either slash)."""
    word = re.compile(r'(?<![\w.\-/\\])' + re.escape(branch) + r'(?![\w.\-])') if branch else None
    tree = re.compile(r'worktrees[\\/]+' + re.escape(pathlib.PurePath(path).name) + r'(?![\w.\-])') if path else None
    hit = None
    for rep in reports:
        named = set(BRANCH_MENTION.findall(rep.header))
        if (branch in named) if named else any(p and p.search(rep.header) for p in (word, tree)):
            hit = rep
    return hit
