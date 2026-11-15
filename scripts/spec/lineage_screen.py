#!/usr/bin/env python3
"""THE LINEAGE SCREEN: which candidate pairs of an older edition are FLAGGED, because their identical words are known to
govern something else in the older edition (kb/Work PB5589, PB5584).

    python scripts/selftests/spec/lineage_screen/test_screen.py   # its cases (the case layout, kb/Work PB5428)
    python scripts/spec/lineage_screen.py --write [YEAR]       # docs/rearchitecture/spec-lineage-screen-<year>.json
    python scripts/spec/lineage_screen.py --check [YEAR]       # stale, an unanswered flag, a lost fixture pair: RED
    python scripts/spec/lineage_screen.py --measure YEAR --labels 'GLOB'   # each trigger against labelled sample reads

DESIGN-spec-conformance-review.md section 10.5 is the design. A pair of the candidate pairing (`lineage.candidates`) has
the same construct, kind and words in two editions. That does NOT make it the same requirement: each edition's rule is
judged on its own edition's text, and every pair is decided by a READ (an agent reads the older rule in its own text and a
refuter attacks the decision; `audit_batch.py change-batch --edition YEAR --state DIR` writes only such decisions). Two
blind samples measured it: of 100 pairs this screen leaves unmarked, 13 were different and 3 doubtful.

What the screen does is FLAG the pairs whose words are known to reach further in the older edition than the words alone
show: a rule whose meaning rests on a clause of the older text no catalog row holds (a PENDING clause, such as
§2002:8.8.1.3.2's 32-digit standard arithmetic), on a term the older definitions clause defines, on a behaviour the compiler
switches by edition, on a band of formats, on a record entry the older edition lets a CD entry hold, or on rules it names
without stating. A flag is a question the read must answer: a change record for a flagged pair stands only when its
decision answers each flag by the trigger's name, with why the older rule still states that requirement (`next.answers`,
`unanswered`), which the writer and this module's `--check` refuse otherwise.

THE TRIGGERS ARE DATA: one table per older edition (`scripts/spec/data/lineage-screen-triggers-<year>.json`, `triggers`), each
row naming its `mechanism` (one of `MECHANISMS`, the only code), its parameters, its `source` and the `reason` its flag
gives. A new trigger an existing mechanism computes is a row; a new mechanism is one function in `MECHANISMS` and its
case in the test file. A row with `"measured-only": true` is computed and scored against the labels and flags nothing.

UNKNOWN FAILS CLOSED: a changed or removed pending clause the table does not judge flags every pair under its own clause
and every pair whose text names its title, and a dialect behaviour of the window the table places in no clause flags
every pair; each is printed by `--check` as owed, and neither turns the gate red. The REGRESSION FIXTURE
(`tests/version-matrix/lineage-screen-labels-<year>.json`) pins what the screen catches, never that it suffices: `--check`
fails when a DIFFERENT pair of a sample marked `caught` goes unflagged, and reports how many of the known misses (a
sample marked `known-misses`) the triggers now flag.
"""
from __future__ import annotations

import argparse
import dataclasses
import difflib
import glob
import html
import json
import pathlib
import re
import sys
from typing import Any, Callable, Iterable

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (THE region reader of each transcription)
import inventory_schema  # noqa: E402  (the editions, the repository root, the component-wise clause test)
import lineage  # noqa: E402  (the candidate pairing's file)
from inventory_schema import edition, section_matches  # noqa: E402
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

REPO = inventory_schema.REPO
#: The word-diff ratio at or above which a pending clause is `near` rather than `changed`.
NEAR = 0.90
#: The longest list of differing word runs the screen file keeps per clause (the rest are counted).
DIFF_RUNS = 25
#: The facts file the compiler writes (DocumentedFactsDriftTests): each dialect behaviour and the edition it changed in.
FACTS = "tests/doc-facts/compiler-facts.json"


def table_path(year: int) -> pathlib.Path:
    return REPO / "scripts" / "spec" / "data" / f"lineage-screen-triggers-{year}.json"


def screen_path(year: int) -> pathlib.Path:
    """The committed screen of the older edition `year`'s candidate pairs (`--write`)."""
    return REPO / "docs" / "rearchitecture" / f"spec-lineage-screen-{year}.json"


def committed(year: int) -> dict[str, Any] | None:
    """The committed screen of `year`, or None when the edition has no trigger table (then nothing is flagged)."""
    path = screen_path(year)
    if not table_path(year).exists():
        return None
    if not path.exists():
        raise SystemExit(f"lineage_screen: {year} has a trigger table and no committed screen: run --write {year}")
    return json.loads(path.read_text(encoding="utf-8"))


#: What is wrong with a decision's answer to one flag (`answer_problems`): no answer, an answer to the flag as it read
#: before the screen changed it (keyed by the flag's content, kb/Work PB5597, branch review R2-2 of PB5589), or an answer
#: to a trigger that does not flag the pair.
UNANSWERED, CHANGED_FLAG, STRAY = "unanswered", "flag changed", "stray answer"


def answer_problems(row: dict[str, Any], flags: dict[str, dict[str, str]]) -> list[tuple[str, str, str]]:
    """`(trigger, problem, message)` for each flag of the screen's `flags` (rule-id -> trigger -> detail) that the change
    record `row` (a store row or a batch record: `rule-id` and `next`) does not answer as it now reads. An answer is
    `{"flag": the detail it answered, "answer": why the decision holds}` (`lineage.answer_shaped`): one written for another
    detail of the same trigger answers another question."""
    nxt = row.get("next") or {}
    if not nxt.get("kind"):
        return []
    answers = nxt.get("answers") or {}
    asked = flags.get(row["rule-id"], {})
    out = []
    for name, detail in asked.items():
        got = answers.get(name)
        if not lineage.answer_shaped(got):
            out.append((name, UNANSWERED, f"{name}: {detail}"))
        elif got["flag"] != detail:
            out.append((name, CHANGED_FLAG, f"{name}: answered for \"{got['flag']}\", now \"{detail}\""))
    out += [(name, STRAY, f"{name}: answered, and it does not flag the pair") for name in sorted(set(answers) - set(asked))]
    return out


def unanswered(rows: Iterable[dict[str, Any]], screen: dict[str, Any] | None) -> list[str]:
    """THE WRITER'S REFUSAL (audit_batch.change_batch, record_verdicts.py): why each change record of `rows` may not be
    written against `screen`: its pair is flagged and its decision does not answer every flag as the flag now reads, or it
    answers a trigger that does not flag it (a question nobody asked). One message per refused record."""
    flags = (screen or {}).get("marks", {})
    out = []
    for r in rows:
        if probs := answer_problems(r, flags):
            out.append(f"{r['rule-id']}: a {r['next']['kind']} record whose decision does not answer the lineage screen's "
                       "flags as they now read (" + "; ".join(f"{p}: {m}" for _n, p, m in probs) + "): the read answers "
                       "each flag in next.answers, by trigger, with the flag's detail and why the older rule still states "
                       "that requirement")
    return out


def owed_records(rows: Iterable[dict[str, Any]], screen: dict[str, Any] | None) -> list[str]:
    """A STORED record the screen now asks a question it did not answer (kb/Work PB5597, branch review R2-1 of PB5589):
    the writer refused every unanswered flag when the record was written (`unanswered`, asked by change-batch and by
    record_verdicts.py), so a stored record's unanswered, changed or stray flag is a change of the screen since (a new
    behaviour, a new trigger, a judgment that moved). It is OWED a re-read, listed by `--check` as the table's owed
    judgments are, never a red gate: a compiler change that adds a behaviour must not turn every stored record red."""
    flags = (screen or {}).get("marks", {})
    return [f"{r['rule-id']}: owed a re-read (" + "; ".join(f"{p}: {m}" for _n, p, m in probs) + ")"
            for r in rows if (probs := answer_problems(r, flags))]


# -- the texts ------------------------------------------------------------------------------------------------------------

def diff_words(text: str) -> list[str]:
    """The words a clause diff compares: markup dropped, a clause number read as `#` (a renumbering is no change)."""
    t = re.sub(r"<[^>]+>", " ", text)
    t = re.sub(r"\b\d+(?:\.\d+)+\b", "#", t)
    return re.findall(r"[a-z0-9#'-]+", t.lower())


def term_words(text: str) -> str:
    """A text as the definition flags read it: lower case, every character but a letter, digit, space or hyphen a space."""
    return " ".join(re.sub(r"[^a-z0-9 -]", " ", text.lower()).split())


@dataclasses.dataclass(frozen=True)
class Clauses:
    """An edition's clauses: each clause number's title and own body (up to the next heading), in text order."""
    title: dict[str, str]
    own: dict[str, str]
    order: tuple[str, ...]

    @classmethod
    def of(cls, year: int) -> "Clauses":
        loaded = citation.regions(year)
        if loaded is None:
            raise SystemExit(f"lineage_screen: no transcription of {year} to read")
        lines, regs = loaded
        title: dict[str, str] = {}
        own: dict[str, str] = {}
        for loc, t, s, e in regs:
            if loc not in own:
                title[loc], own[loc] = t.strip(), "\n".join(lines[s + 1:e])
        return cls(title, own, tuple(own))

    def subtree(self, clause: str) -> str:
        return "\n".join(self.own[c] for c in self.order if c == clause or c.startswith(clause + "."))

    def compared(self, clause: str, other: "Clauses", counterpart: str) -> tuple[list[str], list[str]]:
        """The words of `clause` and of `other`'s `counterpart` that are compared: their own bodies, or both subtrees when
        either own body has no word (a heading whose text lives in its subclauses, as 2023's `9.1.13` over `9.1.13.1`)."""
        a, b = diff_words(self.own[clause]), diff_words(other.own[counterpart])
        if not a or not b:
            return diff_words(self.subtree(clause)), diff_words(other.subtree(counterpart))
        return a, b


def ratio(a: list[str], b: list[str]) -> float:
    return difflib.SequenceMatcher(None, a, b, autojunk=False).ratio()


def differing_runs(a: list[str], b: list[str]) -> tuple[list[dict[str, str]], int]:
    ops = [o for o in difflib.SequenceMatcher(None, a, b, autojunk=False).get_opcodes() if o[0] != "equal"]
    runs = [{"older": " ".join(a[i1:i2]), "newer": " ".join(b[j1:j2])} for _, i1, i2, j1, j2 in ops]
    return runs[:DIFF_RUNS], max(0, len(runs) - DIFF_RUNS)


# -- the pending-clause diff (P0) -----------------------------------------------------------------------------------------

IDENTICAL, NEAR_CLASS, CHANGED, REMOVED = "identical", "near", "changed", "removed"


def pending_diff(pending: Iterable[str], older: Clauses, newer: Clauses,
                 judged: dict[str, dict[str, Any]]) -> dict[str, dict[str, Any]]:
    """Each pending clause of the older edition against its newer counterpart: the table's judgment (`judged[c]
    ["counterpart"]`, null for none) where it names one, else the newer clause of the same title nearest in words."""
    by_title: dict[str, list[str]] = {}
    for c, t in newer.title.items():
        by_title.setdefault(t.lower(), []).append(c)
    out: dict[str, dict[str, Any]] = {}
    for c in pending:
        if c not in older.own:
            raise SystemExit(f"lineage_screen: the pending clause {c} is no heading of the older transcription")
        entry: dict[str, Any] = {"title": older.title[c]}
        if c in judged and "counterpart" in judged[c]:
            cp = judged[c]["counterpart"]
            if cp is not None and cp not in newer.own:
                raise SystemExit(f"lineage_screen: the judged counterpart {cp} of {c} is no clause of the newer text")
            entry["counterpart-by"] = "judgment"
        else:
            cands = by_title.get(older.title[c].lower(), [])
            cp = max(cands, key=lambda k: ratio(*older.compared(c, newer, k))) if cands else None
            entry["counterpart-by"] = "title"
        entry["counterpart"] = cp
        if cp is None:
            entry["class"] = REMOVED
        else:
            a, b = older.compared(c, newer, cp)
            r = ratio(a, b)
            entry["ratio"] = round(r, 3)
            entry["class"] = IDENTICAL if a == b else NEAR_CLASS if r >= NEAR else CHANGED
            if a != b:
                entry["differs"], more = differing_runs(a, b)
                if more:
                    entry["differs-more"] = more
        out[c] = entry
    return out


def marking(diff: dict[str, dict[str, Any]], judged: dict[str, dict[str, Any]]) -> list[str]:
    """The pending clauses whose dependents are flagged: every changed or removed one, and a near one the table judged."""
    return [c for c, d in diff.items() if d["class"] in (CHANGED, REMOVED) or (d["class"] == NEAR_CLASS and c in judged)]


# -- the definitions ------------------------------------------------------------------------------------------------------

#: A term's DOMAIN: 2002 prints it in parentheses after the term (`class (of a data item)`), 2023 in angle brackets on the
#: line under it (`<of a data item>`). A term is keyed by its words and its domain, `class | of a data item`, so the
#: homonyms of one word (2002 defines `class` three times) are compared sense by sense and never merged.
_DOMAIN_2002 = re.compile(r"^(.*?)\s*\(([^)]*)\)\s*$")
_DOMAIN_2023 = re.compile(r"^\\?<([^>]*?)\\?>\s*(.*)$")


def term_key(term: str, domain: str = "") -> str:
    return f"{term_words(term)} | {term_words(domain)}" if domain.strip() else term_words(term)


def base_of(key: str) -> str:
    """The words a rule uses to name a keyed term: the term without its domain."""
    return key.split(" | ", 1)[0]


def definitions_older(clauses: Clauses, clause: str) -> dict[str, str]:
    """An ISO 2002-style definitions clause: one line per entry, `<clause>.<n> <term>[ (<domain>)][; <synonym>]:
    <definition>`, each synonym a term of its own."""
    out: dict[str, str] = {}
    line = re.compile(rf"^{re.escape(clause)}\.\d+ ([^:]{{2,80}}): (.*)$")
    for c in clauses.order:
        if c == clause or c.startswith(clause + "."):
            for ln in clauses.own[c].splitlines():
                if m := line.match(ln.strip()):
                    for name in m.group(1).split(";"):
                        d = _DOMAIN_2002.match(name.strip())
                        out.setdefault(term_key(*(d.groups() if d else (name,))), term_words(m.group(2)))
    return out


def definitions_newer(clauses: Clauses, clause: str) -> dict[str, str]:
    """An ISO 2023-style definitions clause: a numbered entry per term, the term bold on its own line, its domain (if any)
    in angle brackets opening the next, the definition the lines after it (its notes included) up to the next bold term."""
    out: dict[str, str] = {}
    for c in clauses.order:
        if not c.startswith(clause + "."):
            continue
        term, domain, body = None, None, []
        for ln in clauses.own[c].splitlines() + ["**"]:
            if ln.startswith("**"):
                if term:
                    out.setdefault(term_key(term, domain or ""), term_words(" ".join(body)))
                m = re.match(r"^\*\*([^*]{2,60})\*\*\s*$", ln)
                term, domain, body = (m.group(1) if m else None), None, []
            elif term and ln.strip():
                if not body and domain is None and (d := _DOMAIN_2023.match(ln.strip())):
                    domain = d.group(1)
                    if d.group(2).strip():
                        body.append(d.group(2))
                else:
                    domain = domain if domain is not None else ""
                    body.append(ln)
    return out


def defined_in(term: str, clauses: Clauses, skip: str) -> str | None:
    """Where the newer text defines `term` outside its definitions clause `skip`: the clause titled with it, else the
    first clause whose body opens by defining it ("A <term> is ...")."""
    singular = {term, term + "s", term + "es"}
    for c in clauses.order:
        if not (c == skip or c.startswith(skip + ".")) and term_words(clauses.title[c]) in singular:
            return c
    opens = re.compile(rf"^(?:an? |the )?{re.escape(term)}s? (?:is|are) ")
    for c in clauses.order:
        if c == skip or c.startswith(skip + "."):
            continue
        first = term_words(re.sub(r"<[^>]+>", " ", clauses.own[c]))[:200]
        if opens.match(first):
            return c
    return None


def definition_map(older: Clauses, newer: Clauses, where: dict[str, str]) -> dict[str, Any]:
    d_old, d_new = definitions_older(older, where["older"]), definitions_newer(newer, where["newer"])
    changed = sorted(t for t in set(d_old) & set(d_new) if d_old[t] not in d_new[t] and d_new[t] not in d_old[t])
    dropped = sorted(set(d_old) - set(d_new))
    return {"older-terms": len(d_old), "newer-terms": len(d_new),
            "changed": {t: {"older": d_old[t], "newer": d_new[t]} for t in changed},
            "dropped": {t: {"older": d_old[t], "defined-in": defined_in(base_of(t), newer, where["newer"])} for t in dropped}}


# -- the screen -----------------------------------------------------------------------------------------------------------

@dataclasses.dataclass
class Screen:
    """Everything a mechanism reads for one older edition: its rules and the newer edition's, the candidate pairs, the
    judgment table, and (computed once, on first use) the two editions' clauses, the pending diff and the definitions."""
    year: int
    newer_year: int
    rules: dict[str, dict[str, Any]]
    newer_rules: dict[str, dict[str, Any]]
    pairs: list[dict[str, Any]]
    table: dict[str, Any]
    pending: list[str]
    clauses: Callable[[int], Clauses]
    transcription: Callable[[int], str]
    behaviours: list[dict[str, Any]]
    #: The regression fixture's labels, {rule-id: SAME | DIFFERENT} (`labels`).
    labels: dict[str, str] = dataclasses.field(default_factory=dict)
    _cache: dict[str, Any] = dataclasses.field(default_factory=dict)

    def once(self, key: str, make: Callable[[], Any]) -> Any:
        if key not in self._cache:
            self._cache[key] = make()
        return self._cache[key]

    @property
    def diff(self) -> dict[str, dict[str, Any]]:
        return self.once("diff", lambda: pending_diff(self.pending, self.clauses(self.year), self.clauses(self.newer_year),
                                                      self.table.get("pending", {})))

    @property
    def definitions(self) -> dict[str, Any]:
        return self.once("definitions", lambda: definition_map(self.clauses(self.year), self.clauses(self.newer_year),
                                                               self.table["definitions"]))

    @classmethod
    def of(cls, year: int, table: dict[str, Any] | None = None) -> "Screen":
        ed = edition(year)
        if ed.next_held is None:
            raise SystemExit(f"lineage_screen: {year} is the newest held edition; it has no candidate pairs to screen")
        loaded: dict[int, Clauses] = {}
        texts: dict[int, str] = {}

        def clauses(y: int) -> Clauses:
            if y not in loaded:
                loaded[y] = Clauses.of(y)
            return loaded[y]

        def transcription(y: int) -> str:
            if y not in texts:
                path = citation.transcription(y)
                if path is None:
                    raise SystemExit(f"lineage_screen: no transcription of {y} to read")
                texts[y] = probe_text(path.read_text(encoding="utf-8"))
            return texts[y]

        facts = json.loads((REPO / FACTS).read_text(encoding="utf-8"))["dialectBehaviors"]
        return cls(year, ed.next_held, {r["id"]: r for r in ed.rules()},
                   {r["id"]: r for r in edition(ed.next_held).rules()},
                   json.loads(lineage.candidates_path(year).read_text(encoding="utf-8"))["pairs"],
                   table if table is not None else load_table(year),
                   [p.split(":", 1)[1] for p in ed.outside().get("pending-clauses", [])], clauses, transcription, facts,
                   labels(year))


def load_table(year: int) -> dict[str, Any]:
    path = table_path(year)
    if not path.exists():
        raise SystemExit(f"lineage_screen: {year} has no trigger table ({path.relative_to(REPO)})")
    table = json.loads(path.read_text(encoding="utf-8"))
    triggers(table)   # refused at load: a trigger with no mechanism, no reason, or a control character in a parameter
    return table


def probe_text(text: str) -> str:
    """A text as `absent-from` searches it: markup, escapes and typographic quotes and dashes erased, white space one."""
    t = html.unescape(re.sub(r"<[^>]+>", " ", text)).replace("\\", "")
    t = t.translate(str.maketrans({"’": "'", "‘": "'", "“": '"', "”": '"', "–": "-", "—": "-"}))
    t = re.sub(r"^\s*(?:\d+\)|[a-z]\))\s*", "", t)
    return " ".join(t.split())


# -- the mechanisms: the only code. Each returns {rule-id: detail} over the screen's pairs. --------------------------------

Marks = dict[str, str]


def _pair_targets(s: Screen, p: dict[str, Any]) -> Marks:
    return {x["rule-id"]: f"{len(x['to'])} targets" for x in s.pairs if len(x["to"]) > p["max"]}


def _pair_match(s: Screen, p: dict[str, Any]) -> Marks:
    return {x["rule-id"]: x["match"] for x in s.pairs if x["match"] not in p["accept"]}


def _text_pattern(s: Screen, p: dict[str, Any]) -> Marks:
    rx = re.compile(p["pattern"], re.I if "i" in p.get("flags", "") else 0)
    out = {}
    for x in s.pairs:
        if found := rx.findall(s.rules[x["rule-id"]]["text"]):
            out[x["rule-id"]] = ", ".join(sorted(set(found)))
    return out


def _absent_from(s: Screen, p: dict[str, Any]) -> Marks:
    """The rule's text is not in the transcription of the edition `p["edition"]` (one the bridge spans)."""
    text = s.transcription(p["edition"])
    out = {}
    for x in s.pairs:
        r = s.rules[x["rule-id"]]
        if r["kind"] in p.get("except-kinds", []):
            continue
        if probe_text(r["text"]) not in text:
            out[x["rule-id"]] = f"its whole text is not in the {p['edition']} transcription"
    return out


def _definitions(s: Screen, p: dict[str, Any]) -> Marks:
    """The rule uses a term of the older definitions clause the newer one defines otherwise (`changed`) or not at all
    (`dropped`, with where the newer text now defines it, if anywhere)."""
    terms = s.definitions[p["which"]]
    use = [t for t in terms if len(base_of(t)) > p.get("min-length", 3)]
    out = {}
    for x in s.pairs:
        text = f" {term_words(s.rules[x['rule-id']]['text'])} "
        if hit := sorted(t for t in use if f" {base_of(t)} " in text):
            if p["which"] == "dropped":
                hit = [f"{t} (newer: {terms[t]['defined-in'] or 'undefined'})" for t in hit]
            out[x["rule-id"]] = "; ".join(hit)
    return out


def _pending_clause(s: Screen, p: dict[str, Any]) -> Marks:
    judged = s.table.get("pending", {})
    out: dict[str, list[str]] = {}
    for c in marking(s.diff, judged):
        j = judged.get(c) or undecided_judgment(c, s.diff[c]["title"])
        rxs = [re.compile(r, re.I) for r in j["invokes"]]
        tag = f"§{s.year}:{c} ({s.diff[c]['class']}" + (", undecided)" if c not in judged else ")")
        for x in s.pairs:
            r = s.rules[x["rule-id"]]
            if any(section_matches(g, r["section"]) for g in j["governs"]) or any(rx.search(r["text"]) for rx in rxs):
                out.setdefault(x["rule-id"], []).append(tag)
    return {k: ", ".join(v) for k, v in out.items()}


def undecided_judgment(clause: str, title: str) -> dict[str, Any]:
    """UNKNOWN FAILS CLOSED: what a changed or removed pending clause the table does not judge governs, until it does:
    every rule under the clause itself and every rule whose text names the clause's title."""
    return {"governs": [clause], "invokes": [rf"\b{re.escape(title)}\b"] if title.strip() else []}


def undecided(s: Screen) -> list[str]:
    """What the table owes: each changed or removed pending clause it does not judge, and each behaviour of the window
    it places in no clause, both flagged fail-closed meanwhile (`--check` prints them; neither is a red gate)."""
    judged, sections = s.table.get("pending", {}), s.table.get("behaviours", {})
    return ([f"pending clause §{s.year}:{c} ({d['class']}) has no judgment: it flags every pair under {c} and every "
             f"pair naming '{d['title']}' until pending.{c} states what it governs"
             for c, d in s.diff.items() if d["class"] in (CHANGED, REMOVED) and c not in judged]
            + [f"dialect behaviour {b['id']} (changed in {b['changedIn']}) is placed in no {s.year} clause: it flags every "
               f"pair until behaviours.{b['id']} names the {s.year} clauses it sits in"
               for b in behaviour_window(s) if b["id"] not in sections])


def behaviour_window(s: Screen) -> list[dict[str, Any]]:
    """The compiler's dialect behaviours that changed after the older edition and by the newer one."""
    return [b for b in s.behaviours if s.year < b["changedIn"] <= s.newer_year]


def _behaviour_switch(s: Screen, p: dict[str, Any]) -> Marks:
    sections = s.table.get("behaviours", {})
    out: dict[str, list[str]] = {}
    for b in behaviour_window(s):
        placed = sections.get(b["id"])
        for x in s.pairs:
            if placed is None or any(section_matches(g, s.rules[x["rule-id"]]["section"]) for g in placed["sections"]):
                out.setdefault(x["rule-id"], []).append(b["id"] + ("" if placed else " (unplaced)"))
    return {k: ", ".join(v) for k, v in out.items()}


def _listed(s: Screen, p: dict[str, Any]) -> Marks:
    rows = s.table.get(p["rows"], {})
    if unknown := sorted(set(rows) - set(s.rules)):
        raise SystemExit(f"lineage_screen: {p['rows']} names {unknown}, which the {s.year} catalog lacks")
    return {x["rule-id"]: rows[x["rule-id"]] for x in s.pairs if x["rule-id"] in rows}


def _field_differs(s: Screen, p: dict[str, Any]) -> Marks:
    """The pair's rule and a rule it pairs with differ in a catalog field (`band-formats`: the formats a rule's band rules)."""
    f = p["field"]
    out = {}
    for x in s.pairs:
        mine = s.rules[x["rule-id"]].get(f) or None          # an absent field and an empty one say the same
        theirs = [s.newer_rules[t].get(f) or None for t in x["to"]]
        if any(t != mine for t in theirs):
            out[x["rule-id"]] = f"{f} {mine} against {theirs}"
    return out


def construct_clause(rule: dict[str, Any], locator: str = "section") -> str:
    """The clause a rule belongs to as a construct's rule: its block's parent (`14.8.29` of the GR block `14.8.29.3` and of
    the format `14.8.29.1`)."""
    sec = rule[locator]
    return sec.rsplit(".", 1)[0] if "." in sec else sec


def _clause_identity(s: Screen, p: dict[str, Any]) -> Marks:
    """WHOLE-CLAUSE IDENTITY (measured only): a rule is marked unless every row of its clause (its general format and every
    rule) pairs with exactly one newer rule, all of one newer clause, and that clause has no row the pairing leaves out."""
    kinds = set(p.get("kinds") or ())
    by_pair = {x["rule-id"]: x for x in s.pairs}
    older: dict[str, list[str]] = {}
    for rid, r in s.rules.items():
        if not kinds or r["kind"] in kinds:
            older.setdefault(construct_clause(r), []).append(rid)
    newer: dict[str, set[str]] = {}
    for rid, r in s.newer_rules.items():
        if not kinds or r["kind"] in kinds:
            newer.setdefault(construct_clause(r), set()).add(rid)
    out = {}
    for c, rids in older.items():
        mine = [x for x in rids if x in by_pair]
        if not mine:
            continue
        why = ""
        if unpaired := [x for x in rids if x not in by_pair or len(by_pair[x]["to"]) != 1]:
            why = f"{len(unpaired)} of its {len(rids)} rows pair with no single newer rule (first {sorted(unpaired)[0]})"
        else:
            targets = [by_pair[x]["to"][0] for x in rids]
            homes = sorted({construct_clause(s.newer_rules[t]) for t in targets})
            if len(homes) != 1:
                why = f"its rows pair into {len(homes)} newer clauses ({', '.join(homes[:4])})"
            elif len(set(targets)) != len(targets):
                why = "two of its rows pair with one newer rule"
            elif extra := sorted(newer.get(homes[0], set()) - set(targets)):
                why = f"newer clause {homes[0]} has {len(extra)} row(s) no rule of it pairs with (first {extra[0]})"
        if why:
            for x in mine:
                out[x] = f"clause {c}: {why}"
    return out


def _construct_reach(s: Screen, p: dict[str, Any]) -> Marks:
    """The rule's construct is one a removed context can hold: a clause of `clauses`, or one the general formats of the
    constructs `formats-of` name (by `clause-pattern`), and its text does not confine it elsewhere (`confined`). 2002's
    communication section: the CD entry (13.14) and the clauses its formats name govern records 2023 has no section for."""
    reach = set(p["clauses"])
    named = re.compile(p["clause-pattern"])
    for r in s.rules.values():
        if r["kind"] == "FMT" and construct_clause(r) in p["formats-of"]:
            reach |= set(named.findall(r["text"]))
    confined = re.compile(p["confined"], re.I)
    return {x["rule-id"]: f"clause {construct_clause(s.rules[x['rule-id']])} is reached by {p['context']}"
            for x in s.pairs if construct_clause(s.rules[x["rule-id"]]) in reach
            and not confined.search(s.rules[x["rule-id"]]["text"])}


#: THE MECHANISMS, by the name a trigger row gives as its `mechanism`.
MECHANISMS: dict[str, Callable[[Screen, dict[str, Any]], Marks]] = {
    "pair-targets": _pair_targets, "pair-match": _pair_match, "text-pattern": _text_pattern,
    "absent-from": _absent_from, "definitions": _definitions, "pending-clause": _pending_clause,
    "behaviour-switch": _behaviour_switch, "listed": _listed, "field-differs": _field_differs,
    "clause-identity": _clause_identity, "construct-reach": _construct_reach}


def triggers(table: dict[str, Any]) -> list[dict[str, Any]]:
    rows = table["triggers"]
    names = [t["name"] for t in rows]
    if len(set(names)) != len(names):
        raise SystemExit(f"lineage_screen: the trigger table names a trigger twice ({names})")
    for t in rows:
        if t.get("mechanism") not in MECHANISMS:
            raise SystemExit(f"lineage_screen: trigger {t['name']} names the mechanism {t.get('mechanism')!r}, which is not "
                             f"one of {sorted(MECHANISMS)}")
        if not (t.get("reason") or "").strip():
            raise SystemExit(f"lineage_screen: trigger {t['name']} states no reason for the pairs it flags")
        if bad := control_characters(t.get("params", {})):
            raise SystemExit(f"lineage_screen: trigger {t['name']} holds a control character in {bad}: a JSON escape such "
                             "as \\b decodes to one (a backspace, which no rule text holds); write a pattern's \\b as "
                             "\\\\b in the JSON (kb/Work PB5597, branch review R2-3 of PB5589)")
    return rows


def control_characters(value: Any, where: str = "params") -> list[str]:
    """Each place in a trigger's parameters whose string holds a control character (U+0000 to U+001F, U+007F): none is
    meant, since no rule text a pattern searches holds one, so each is a JSON escape that decoded to the wrong thing."""
    if isinstance(value, str):
        bad = sorted({c for c in value if ord(c) < 32 or ord(c) == 127})
        return [f"{where} ({', '.join(f'U+{ord(c):04X}' for c in bad)})"] if bad else []
    if isinstance(value, dict):
        return [w for k, v in value.items() for w in control_characters(v, f"{where}.{k}")]
    if isinstance(value, list):
        return [w for i, v in enumerate(value) for w in control_characters(v, f"{where}[{i}]")]
    return []


def flagging(trigger: dict[str, Any]) -> bool:
    """Does the trigger flag the pairs it marks? A row with `"measured-only": true` is computed, committed and scored
    against the labels and flags nothing (whole-clause identity, which marks 83 of the first sample's 84 SAME pairs)."""
    return not trigger.get("measured-only", False)


def text_marks(s: Screen) -> dict[str, dict[str, str]]:
    """Every trigger's marks: {trigger name: {rule-id: detail}}."""
    return {t["name"]: MECHANISMS[t["mechanism"]](s, t.get("params", {})) for t in triggers(s.table)}


def screen_document(s: Screen) -> dict[str, Any]:
    marks = text_marks(s)
    barring = {t["name"] for t in triggers(s.table) if flagging(t)}
    by_rule: dict[str, dict[str, str]] = {}
    measured: dict[str, dict[str, str]] = {}
    for name, m in marks.items():
        for rid, detail in m.items():
            (by_rule if name in barring else measured).setdefault(rid, {})[name] = detail
    diff = s.diff
    return {"_comment": [
        f"THE LINEAGE SCREEN of the {s.year} candidate pairs (lineage_screen.py, kb/Work PB5589; DESIGN-spec-conformance-",
        "review.md section 10.5). It FLAGS pairs; an unflagged pair is not thereby the same requirement, and every pair is",
        "decided by a read. `pending`: each pending clause against its counterpart (class, ratio, the differing word runs);",
        "`definitions`: the terms whose definition differs or is not given in the other edition's definitions clause;",
        "`marks`: each flagged pair, by trigger, with its detail: a change record for it answers each flag (next.answers),",
        "or audit_batch.py change-batch and lineage_screen.py --check refuse it. `measured`: the marks of the",
        "measured-only triggers. `labels`: the regression fixture's result. `owed`: the judgments the table owes.",
        f"Regenerate: python scripts/spec/lineage_screen.py --write {s.year}; --check fails when it is stale."],
        "edition": s.year, "next": s.newer_year,
        "triggers": [{**{k: t.get(k) for k in ("name", "mechanism", "source")}, "flags": flagging(t)}
                     for t in triggers(s.table)],
        "counts": {"pairs": len(s.pairs), "marked": len(by_rule), **{n: len(m) for n, m in marks.items()},
                   "pending": {k: sum(1 for d in diff.values() if d["class"] == k)
                               for k in (IDENTICAL, NEAR_CLASS, CHANGED, REMOVED)}},
        "pending": diff, "definitions": s.definitions,
        "labels": label_findings(s, marks), "owed": undecided(s),
        "marks": {rid: by_rule[rid] for rid in sorted(by_rule)},
        "measured": {rid: measured[rid] for rid in sorted(measured)}}


#: THE REGRESSION FIXTURE (kb/Work PB5589): samples of pairs read blind and whole, each labelled SAME, DIFFERENT or
#: DOUBTFUL by the read that decided it, each sample with its EXPECTATION. `caught`: every DIFFERENT pair must stay flagged
#: (the screen's triggers were built from them). `known-misses`: a later sample's DIFFERENT and DOUBTFUL pairs, which the
#: screen was not built to catch: `--check` reports how many are flagged, a measure that grows when a trigger row is added,
#: and fails on none of them. The fixture pins what the screen catches, never that it suffices.
CAUGHT, KNOWN_MISSES = "caught", "known-misses"


def labels_path(year: int) -> pathlib.Path:
    return REPO / "tests" / "version-matrix" / f"lineage-screen-labels-{year}.json"


def labels(year: int) -> list[dict[str, str]]:
    """The fixture's cases of `year`, each `{rule-id, verdict, expect}`, every sample in file order."""
    path = labels_path(year)
    if not path.exists():
        return []
    out = []
    for sample in json.loads(path.read_text(encoding="utf-8"))["samples"]:
        if sample["expectation"] not in (CAUGHT, KNOWN_MISSES):
            raise SystemExit(f"lineage_screen: sample {sample['name']} expects {sample['expectation']!r}, not one of "
                             f"{[CAUGHT, KNOWN_MISSES]}")
        out += [{"rule-id": c["rule-id"], "verdict": c["verdict"], "expect": sample["expectation"]} for c in sample["cases"]]
    return out


def label_findings(s: Screen, marks: dict[str, dict[str, str]], got: list[dict[str, str]] | None = None) -> dict[str, Any]:
    lab = s.labels if got is None else got
    barring = {t["name"] for t in triggers(s.table) if flagging(t)}
    flagged = {c["rule-id"] for c in lab if any(n in barring and c["rule-id"] in m for n, m in marks.items())}
    misses = [c for c in lab if c["expect"] == KNOWN_MISSES and c["verdict"] != "SAME"]
    return {"cases": len(lab),
            "different-unflagged": sorted(c["rule-id"] for c in lab
                                          if c["expect"] == CAUGHT and c["verdict"] == "DIFFERENT" and c["rule-id"] not in flagged),
            "known-misses-flagged": f"{sum(1 for c in misses if c['rule-id'] in flagged)} of {len(misses)}",
            "same-flagged": sorted({c["rule-id"] for c in lab if c["verdict"] == "SAME" and c["rule-id"] in flagged}),
            "by-trigger": {n: {v: sum(1 for c in lab if c["verdict"] == v and c["rule-id"] in m)
                               for v in ("DIFFERENT", "DOUBTFUL", "SAME")} for n, m in marks.items()}}


def table_errors(s: Screen) -> list[str]:
    """Why the judgment table no longer fits the texts: a judgment of a clause that is identical or not pending, a
    judgment with no why, a behaviour placed that did not change in the window, or a `caught` fixture pair unflagged. (What
    the table does not yet judge is `undecided`: flagged fail-closed, printed, never red.)"""
    errs = []
    judged = s.table.get("pending", {})
    for c, d in s.diff.items():
        if d["class"] == IDENTICAL and c in judged:
            errs.append(f"pending.{c} judges a clause identical to its counterpart: withdraw the judgment")
    for c in sorted(set(judged) - set(s.diff)):
        errs.append(f"pending.{c} judges a clause the {s.year} catalog does not hold pending")
    for c, j in judged.items():
        if not (j.get("why") or "").strip():
            errs.append(f"pending.{c} states no why")
    if s.labels and (missed := label_findings(s, text_marks(s))["different-unflagged"]):
        errs.append(f"the regression fixture {labels_path(s.year).relative_to(REPO)}: DIFFERENT pair(s) {missed} of a "
                    "`caught` sample are flagged by no trigger (a trigger that caught them was lost or narrowed)")
    window = {b["id"] for b in behaviour_window(s)}
    sections = s.table.get("behaviours", {})
    errs += [f"behaviours.{b} names a behaviour that did not change between {s.year} and {s.newer_year}"
             for b in sorted(set(sections) - window)]
    return errs


# -- the command line -----------------------------------------------------------------------------------------------------

def screened_years() -> list[int]:
    return [y for y in inventory_schema.HELD if table_path(y).exists()]


def measure(year: int, pattern: str, among: str | None) -> int:
    """Each trigger against labelled sample reads (`cases` with `rule-id` and `verdict`): how many
    of each label it marks, and, with `among` (a JSON list of rule ids), how many of those it marks."""
    labels: dict[str, str] = {}
    for f in sorted(glob.glob(pattern)):
        for c in json.loads(pathlib.Path(f).read_text(encoding="utf-8"))["cases"]:
            labels[c["rule-id"]] = c["verdict"]
    if not labels:
        raise SystemExit(f"--measure: no labelled case matched {pattern}")
    pool = set(json.loads(pathlib.Path(among).read_text(encoding="utf-8"))) if among else None
    s = Screen.of(year)
    marks = text_marks(s)
    tally = {v: sum(1 for x in labels.values() if x == v) for v in sorted(set(labels.values()))}
    print(f"labelled {len(labels)} {tally}" + (f"; pool {len(pool)}" if pool is not None else ""))
    union: set[str] = set()
    barring = {t["name"] for t in triggers(s.table) if flagging(t)}
    for name, m in marks.items():
        hit = {v: sum(1 for r, x in labels.items() if x == v and r in m) for v in tally}
        if name in barring:
            union |= set(m)
        print(f"  {name:22s} marks {len(m):5d}" + (f"  pool {len(pool & set(m)):4d}" if pool is not None else "")
              + "  " + "  ".join(f"{v} {hit[v]}/{tally[v]}" for v in tally))
    print("  union of the flagging   " + "  ".join(f"{v} {sum(1 for r, x in labels.items() if x == v and r in union)}/{tally[v]}"
                                             for v in tally) + (f"  pool {len(pool & union)}" if pool is not None else ""))
    return 0


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(description="The lineage screen of an older edition's candidate pairs (kb/Work PB5589).")
    ap.add_argument("--write", type=int, nargs="?", const=0, metavar="YEAR")
    ap.add_argument("--check", type=int, nargs="?", const=0, metavar="YEAR",
                    help="fail when the committed screen is stale, the judgment table no longer fits, a stored record "
                         "leaves a flag unanswered or a caught fixture pair is unflagged; with no YEAR, "
                         "every held edition with a trigger table: the gate's LINEAGE SCREEN audit")
    ap.add_argument("--measure", type=int, metavar="YEAR")
    ap.add_argument("--labels", help="with --measure: a glob of labelled sample reads")
    ap.add_argument("--among", help="with --measure: a JSON list of rule ids to count the marks within")
    a = ap.parse_args(argv)
    if a.measure:
        return measure(a.measure, a.labels or "", a.among)
    if a.write is None and a.check is None:
        ap.print_help()
        return 2
    year = a.write if a.write is not None else a.check
    years = [year] if year else screened_years()
    red = []
    for y in years:
        s = Screen.of(y)
        doc = screen_document(s)
        text = json.dumps(doc, indent=1, ensure_ascii=False) + "\n"
        path = screen_path(y)
        errs = table_errors(s)
        owed_stored: list[str] = []
        if a.check is not None:
            import inventory_view  # noqa: PLC0415  (the stored records of the edition)
            owed_stored = owed_records(inventory_view.inventory(y), doc)
        print(f"{y}: pairs {len(s.pairs)}, flagged {doc['counts']['marked']}; pending {doc['counts']['pending']}; "
              f"fixture: caught pairs unflagged {len(doc['labels']['different-unflagged'])}, known misses flagged "
              f"{doc['labels']['known-misses-flagged']}")
        for o in doc["owed"]:
            print(f"  · owed: {o}")
        for o in owed_stored:
            print(f"  · owed (a stored record): {o}")
        for e in errs:
            print(f"  ⛔ {e}")
        if a.check is not None:
            current = path.exists() and path.read_text(encoding="utf-8") == text
            print(f"{'✓ current' if current else '⛔ STALE'}: {path.relative_to(REPO)}")
            if errs or not current:
                red.append(y)
            continue
        if errs:
            red.append(y)
            continue
        path.write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {path.relative_to(REPO)}")
    if a.check is not None:
        print(f"=== LINEAGE SCREEN: {'GREEN' if not red else f'RED ({red}; fix the table, then lineage_screen.py --write)'} ===")
    return 1 if red else 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
