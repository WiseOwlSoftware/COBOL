#!/usr/bin/env python3
"""audit_batch.py - THE Message Batches arm of the edition audit (kb/Work PB4886; DESIGN-spec-conformance-review.md §10).

The 1985 audit is the 2023 audit run with an edition parameter. Its independent JUDGMENTS go to a model through the Message
Batches API in five passes, each a first pass whose answers are DETECTIONS (a verdict stays with the fleet):

  route   one request per 1985 clause (the paragraph that holds its rule blocks): its subject, module and first rules, and
          the list of every 2023 clause; the answer is ROUTED, with the 2023 clauses that state the same feature (a 1985
          clause may become several, and a module's copy of a clause one), or NONE (kb/Work PB4971);
  map     one request per 1985 catalog rule: its text and the 2023 rules that may state it (`candidates`: every rule of the
          same kind in the clauses its clause was routed to or shares a heading with, then the rules most like it by
          wording); the answer is
          SAME (the 2023 verdict carries over), DIFFERS (a 1985 row of its own) or NONE (no 2023 counterpart), with the
          2023 rule ids and the differing words;
  verify  the adversarial pass: one request per SAME answer, which tries to REFUTE it (UPHELD, REFUTED, UNSURE);
  claims  one request per 1985 claim of docs/VERSION_CHANGE_REFERENCE.md (a JSON list the fleet prepares, each claim with
          the 1985 locator it is about): CONFIRMED, REFUTED or NOT-ADDRESSED, with the `§1985:` citation;
  drafts  one request per rule whose disposition is a row of its own: a minimal program and the outcome the 1985 rule
          requires, which the fleet compiles and runs under --std=1985 and checks against the rule text before any use.

    python scripts/spec/audit_batch.py plan    --pass route  --state DIR [--catalog F] [--catalog-2023 F]
    python scripts/spec/audit_batch.py plan    --pass map    --state DIR [--catalog F] [--catalog-2023 F] [--candidates 15]
                                                                                   # once every route item is answered
    python scripts/spec/audit_batch.py plan    --pass claims --state DIR --claims FILE
    python scripts/spec/audit_batch.py plan    --pass verify --state DIR          # once every map item is answered
    python scripts/spec/audit_batch.py plan    --pass drafts --state DIR          # once every verify item is answered
    python scripts/spec/audit_batch.py submit  --pass P --state DIR --api-key-file KEY [--api-account A] --max-usd N --canary 20
    python scripts/spec/audit_batch.py submit  --pass P --state DIR --api-key-file KEY --max-usd N      # the rest
    python scripts/spec/audit_batch.py status  --pass P --state DIR [--api-key-file KEY]
    python scripts/spec/audit_batch.py collect --pass P --state DIR --api-key-file KEY
    python scripts/spec/audit_batch.py retry   --pass P --state DIR --api-key-file KEY --max-usd N
    python scripts/spec/audit_batch.py results --state DIR                        # DIR/results.json: each rule's disposition
    python scripts/spec/test_audit_batch.py                                       # its self-test: a fake client, no API call

RESTARTABLE. A pass lives in `DIR/<pass>/`: `items.json` (written once by `plan`: custom id -> the item, every input its
prompt needs, so a prompt is rebuilt from the run alone), `state.json` (message_batches.BatchRun, THE restartable batch state
transcribe_pdf.py shares: an intent before every create, the batch after, a crash adopted on the next run) and
`answers/<custom id>.json`, each written once, atomically. A reply that does not parse into the pass's answer shape is kept in
`raw/` and its item stays unanswered, so `retry` resubmits it. Every command can be re-run at any time.

A CANARY BEFORE EACH FULL SUBMIT. `submit --canary N` sends the first N unanswered items as a batch of their own; a submit
without `--canary` is refused until that pass has a collected canary with at least one valid answer, and its estimate then
uses the canary's MEASURED cost per request. Until then `retry` resubmits the canary's own unanswered items, as the canary
(kb/Work PB4930). `--max-usd` is required on every submit and retry, and refuses one whose estimate (spent, plus in flight,
plus this submit, per request) exceeds it.

ONE CITATION SHAPE (kb/Work PB4886). An answer that cites the 1985 text names its `locator` and its `rule` as fields of
their own (CITATION_FIELDS, the prompt's words), read by `cited` through citation.py and validated against the 1985
transcription; a 1985 rule a pass quotes carries its catalog citation, which `plan --pass map` parses first.

PUBLIC TEXT ONLY. A prompt carries the tracked transcriptions' text (through the catalogs and specs/), never a page image and
never a per-copy stamp: a request whose text matches any stamp shape (verify_publishable) is refused before any batch.

THE KEY is read from `--api-key-file` (api_key.read_api_key; `--api-account` chooses among labeled keys and the state keeps the
account it first billed, never the key). No self-test opens a real key file.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import math
import pathlib
import re
import sys
from typing import Any, Callable, Iterable, NamedTuple

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import citation  # noqa: E402  (THE citation format and the 1985 regions, kb/Work PB4887)
import spec_pdf  # noqa: E402  (the one edition table, kb/Work PB4815)
import verify_publishable  # noqa: E402  (the stamp shapes)
from extract_rule_catalog import OUT, OUT_1985  # noqa: E402  (the 2023 and the 1985 catalogs)
from message_batches import DEFAULT_MODEL, BatchRun, cached_system, cached_text, make_client, reply_text, usd, write_atomic  # noqa: E402  (THE batch state)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/
from api_key import ACCOUNT_HELP  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "audit_batch"
MAX_OUTPUT_TOKENS = 8000
#: A 1985 region quoted to a claims request is cut at this many characters (a claim names a paragraph, not a section).
REGION_CHARS = 12000
#: Characters per input token, for the estimate before a canary has measured one.
CHARS_PER_TOKEN = 3.5

SAME, DIFFERS, NONE = "SAME", "DIFFERS", "NONE"

#: The preamble of every pass. Its box sentence names the transcription's OWN level-2 markup (spec_pdf.BOX_SIDE for prose,
#: FIGURE-STYLE.md rule 9's double lines for a general format; TRANSCRIPTION-STATE.md "The 1985 transcription", kb/Work
#: PB4850), so a reader sees in the quoted text the level-2 box the standard prints (§1985:I.1.3).
COMMON = f"""You are one independent reader in a conformance audit of a COBOL compiler. Judge from the standards' text below
only; do not rely on memory of other editions or implementations. Text between two {spec_pdf.BOX_SIDE} marks in 1985 text, and a
general format drawn in double lines (╔═╗ ║ ╚═╝), is printed inside a box: a specification unique to level 2 of its module.
Quote at most 120 characters per field. Answer with ONLY one JSON object, no prose before or after it."""

#: THE ONE SHAPE OF A 1985 CITATION IN AN ANSWER (kb/Work PB4886). Many 1985 regions are a Function, a General Format or a
#: prose paragraph with no numbered rule, and the claims canary's three right answers each wrote that paragraph's heading
#: word where the rule goes (`§1985:VII.3.5.1 Function`), which is no citation. So the locator and the rule are fields of
#: their own: a prompt asks for them in these words, and `cited` reads them through citation.py, the one format.
CITATION_FIELDS = ('"locator": "the 1985 locator alone: a Roman section and its own paragraph number, for example VI.5.3.2, '
                   'or an appendix paragraph, for example C.1; no section sign, no edition and no heading word", "rule": '
                   '"SR n or GR n when one numbered syntax or general rule decides, for example GR3; otherwise null"')

PROMPTS: dict[str, str] = {
    "route": COMMON + """
Below is one clause of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993): its subject, its module and its first
rules, and then the list of every clause of ISO/IEC 1989:2023 that has rules, each with its number and subject. Name every
2023 clause whose rules state the same language feature as this 1985 clause, however it is now named or organized: a 1985
module's copy of a clause (the OPEN statement of the indexed I-O module) and the one 2023 clause that now holds it, a clause
renamed, a 1985 clause whose rules 2023 spreads over several clauses (a general concept and the statement that uses it).
- ROUTED: one or more 2023 clauses state this feature; name them, the closest first;
- NONE: no 2023 clause states it (a deleted feature, such as the communication module).
The JSON object: {"verdict": "ROUTED|NONE", "clauses_2023": ["clause number", ...], "reason": "at most 200 characters"}.
"clauses_2023" names only clause numbers of the list, at most 8, and is empty for NONE.""",
    "map": COMMON + """
Compare one rule of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993) with candidate rules of ISO/IEC 1989:2023.
- SAME: one or more candidates state the same requirement in substance: the same programs are accepted or refused, and with
  the same effect. Different wording, numbering or terms for the same thing ("must" and "shall", "nonnumeric literal" and
  "alphanumeric literal") are not a difference.
- DIFFERS: a candidate covers the same subject but the requirement differs in substance (a case added or removed, a result
  changed, made undefined or implementor-defined).
- NONE: no candidate covers this rule's subject (a deleted or obsolete feature, or one the candidates miss).
The JSON object: {"verdict": "SAME|DIFFERS|NONE", "rules_2023": ["candidate id", ...], "differs": {"1985": "the 1985
words", "2023": "the 2023 words"}, "reason": "at most 200 characters"}. "rules_2023" names only candidate ids, and is empty
for NONE; "differs" is empty for SAME and NONE.""",
    "verify": COMMON + """
A first reader judged that the ISO/IEC 1989:2023 rules below state the SAME requirement as the ANSI X3.23-1985 rule. Try to
REFUTE that judgment: look for a case, a phrase, an exception, a default or a result that one text has and the other does not.
- UPHELD: you found no difference in substance;
- REFUTED: you found one (quote it);
- UNSURE: the texts do not let you decide.
The JSON object: {"verdict": "UPHELD|REFUTED|UNSURE", "reason": "at most 200 characters", "differs": {"1985": "...",
"2023": "..."}}; "differs" is empty unless REFUTED.""",
    "claims": COMMON + """
A reference document makes the claim below about ANSI X3.23-1985. Check it against the 1985 text below, which is the part of
the standard the claim is about.
- CONFIRMED: the text says what the claim says;
- REFUTED: the text says otherwise (quote it);
- NOT-ADDRESSED: the text does not decide the claim.
The JSON object: {"verdict": "CONFIRMED|REFUTED|NOT-ADDRESSED", """ + CITATION_FIELDS + """, "quote": "the deciding
words", "reason": "at most 200 characters"}; "locator" names the paragraph that holds the deciding words, and is required
unless NOT-ADDRESSED.""",
    "drafts": COMMON + """
Write a minimal ANSI X3.23-1985 COBOL program (fixed reference format: sequence area in columns 1-6, indicator in column 7,
area A from column 8) that exercises the 1985 rule below, and state the outcome the rule requires: either the compiler must
refuse the program, or the program must run and DISPLAY the exact lines given. Use only language the 1985 standard defines.
The JSON object: {"program": "the source text, lines separated by \\n", "expected": {"outcome": "refused|output",
"output": ["each DISPLAYed line", ...]}, "basis": "the words of the rule that decide the outcome, at most 200 characters"}.""",
}
#: The output tokens of one reply, for the estimate before a canary has measured them (thinking included).
EST_OUTPUT_TOKENS = {"route": 1200, "map": 1500, "verify": 1500, "claims": 1200, "drafts": 2500}
EFFORT = {"route": "high", "map": "medium", "verify": "high", "claims": "medium", "drafts": "medium"}
ROUTED = "ROUTED"
#: The most 2023 clauses one route answer may name: a 1985 clause spread over more than this is no single feature.
MAX_ROUTES = 8
VERDICTS = {"route": {ROUTED, NONE}, "map": {SAME, DIFFERS, NONE}, "verify": {"UPHELD", "REFUTED", "UNSURE"},
            "claims": {"CONFIRMED", "REFUTED", "NOT-ADDRESSED"}}

WORD = re.compile(r"[a-z][a-z0-9-]{2,}")
#: Words too common in both standards to say which rule is which.
STOPWORDS = frozenset("""the and that this with for are any each which from shall must may not its into than then when
where data item items specified clause statement statements rule rules general syntax format following used same value
values one two all being been have has such only also other there their them they will""".split())


# -- the pass's directory --------------------------------------------------------------------------------------------------

class Pass:
    """`DIR/<name>/`: items.json, state.json, answers/, raw/."""

    def __init__(self, state: pathlib.Path, name: str):
        self.name, self.dir = name, state / name
        self.items_file, self.answers_dir, self.raw_dir = self.dir / "items.json", self.dir / "answers", self.dir / "raw"
        self.batches: BatchRun[str] = BatchRun(self.dir / "state.json", "items", str, str, TOOL)

    def items(self) -> dict[str, dict[str, Any]]:
        if not self.items_file.exists():
            raise SystemExit(f"{TOOL}: the {self.name} pass has no plan: run `plan --pass {self.name}` first")
        return json.loads(self.items_file.read_text(encoding="utf-8"))

    def answer_file(self, cid: str) -> pathlib.Path:
        return self.answers_dir / f"{cid}.json"

    def answers(self) -> dict[str, dict[str, Any]]:
        """custom id -> {"id": item id, "answer": the parsed answer}."""
        if not self.answers_dir.exists():
            return {}
        return {p.stem: json.loads(p.read_text(encoding="utf-8")) for p in sorted(self.answers_dir.glob("*.json"))}

    def unanswered(self) -> list[str]:
        return [cid for cid in self.items() if not self.answer_file(cid).exists()]


def custom_id(pass_name: str, item_id: str) -> str:
    """A custom id the API accepts (letters, digits, `_` and `-`, at most 64), the same for the same item every time."""
    return f"{pass_name}-{hashlib.sha256(item_id.encode()).hexdigest()[:24]}"


def write_plan(p: Pass, items: list[dict[str, Any]]) -> int:
    """Write the pass's items once. A plan that exists is kept when it is the same, and refused when it is not: a run's
    answers belong to its items."""
    planned = {custom_id(p.name, it["id"]): it for it in items}
    if len(planned) != len(items):
        raise SystemExit(f"{TOOL}: the {p.name} plan names an item twice")
    if p.items_file.exists():
        if json.loads(p.items_file.read_text(encoding="utf-8")) != planned:
            raise SystemExit(f"{TOOL}: {p.items_file} holds another plan; a new plan needs a new --state")
        print(f"{p.name}: the plan of {len(planned)} item(s) exists and is unchanged", flush=True)
        return 0
    p.dir.mkdir(parents=True, exist_ok=True)
    write_atomic(p.items_file, json.dumps(planned, indent=1, ensure_ascii=False))
    print(f"{p.name}: planned {len(planned)} item(s)", flush=True)
    return 0


# -- the plans ------------------------------------------------------------------------------------------------------------

def words(text: str) -> set[str]:
    return {w for w in WORD.findall(re.sub(r"<[^>]+>", " ", text.lower())) if w not in STOPWORDS}


def clause_of(block: str) -> str:
    """The clause that holds a rule block: its parent paragraph (`VI.6.18.4` -> `VI.6.18`, `14.9.21.4` -> `14.9.21`)."""
    return block.rsplit(".", 1)[0]


def clause_key_1985(rule: dict[str, Any]) -> str:
    """A 1985 rule's clause, keyed with its document: Amendment 1's Section A numbers its paragraphs anew."""
    return f"{rule['document']} {clause_of(rule['locator'])}"


def subject_key(subject: str | None) -> str:
    """A clause heading as words alone: `THE START STATEMENT` and `START statement` are one key."""
    return " ".join(w for w in re.findall(r"[a-z0-9]+", (subject or "").lower()) if w != "the")


def clauses_2023(rules_2023: list[dict[str, Any]]) -> list[dict[str, str]]:
    """Every 2023 clause that has catalog rules, in catalog order: its number and its subject."""
    seen: dict[str, str] = {}
    for r in rules_2023:
        seen.setdefault(clause_of(r["section"]), r.get("subject", ""))
    return [{"id": c, "subject": s} for c, s in seen.items()]


#: The rules of a 1985 clause a route request quotes, and the characters of each.
ROUTE_SAMPLE, ROUTE_SAMPLE_CHARS = 3, 400


def plan_route(catalog: pathlib.Path, catalog_2023: pathlib.Path) -> list[dict[str, Any]]:
    rules, rules_2023 = load_catalogs(catalog, catalog_2023)
    listed = clauses_2023(rules_2023)
    by_clause: dict[str, list[dict[str, Any]]] = collections.defaultdict(list)
    for r in rules:
        by_clause[clause_key_1985(r)].append(r)
    return [{"id": key, "clause": {"locator": clause_of(rs[0]["locator"]), "document": rs[0]["document"],
                                   "module": rs[0].get("module"), "subject": rs[0].get("subject", ""), "rules": len(rs),
                                   "sample": [f"{r['citation']}: {r['text'][:ROUTE_SAMPLE_CHARS]}" for r in rs[:ROUTE_SAMPLE]]},
             "clauses_2023": listed}
            for key, rs in by_clause.items()]


def routes(state: pathlib.Path) -> dict[str, list[str]]:
    """Each 1985 clause key -> the 2023 clauses its route answer names. Every route item must be answered: the map pass's
    candidates are built from them, so a map planned before the routes are in would be planned without its strongest signal."""
    r = Pass(state, "route")
    items, answers = r.items(), r.answers()
    if missing := [cid for cid in items if cid not in answers]:
        raise SystemExit(f"{TOOL}: {len(missing)} route item(s) are unanswered; collect and retry the route pass first")
    return {answers[cid]["id"]: answers[cid]["answer"]["clauses_2023"] for cid in items}


class Candidates(NamedTuple):
    """A 1985 rule's 2023 candidates, in the two parts `candidates` draws them from: its CLAUSE's (every rule of its kind
    in the clauses its clause was routed to or shares a heading with), which every rule of that clause and kind shares,
    and its own by WORDING. The clause part is a request's cached block, so the rules of one clause read it once."""
    clause: list[str]
    worded: list[str]

    @property
    def all(self) -> list[str]:
        return self.clause + self.worded


def candidates(rules_1985: list[dict[str, Any]], rules_2023: list[dict[str, Any]], k: int,
               routed: dict[str, list[str]]) -> dict[str, Candidates]:
    """The 2023 rules that may state each 1985 rule, ids in order (kb/Work PB4971), from the strongest signal down:
    1. every 2023 rule of the SAME KIND in the clauses the rule's clause was routed to (`routed`, the route pass, closest
       clause first) or shares a heading with (`subject_key`), in catalog order;
    2. then the k rules most like it by wording that step 1 did not name: the inverse-document-frequency weight of the words
       they share (the subject's words count twice), a rule of the same kind preferred.
    Step 1 finds the counterpart that wording misses (another clause's name, a module's copy); step 2 finds a rule 2023
    moved to a clause of another subject, and a counterpart of another kind."""
    by_clause_kind: dict[tuple[str, str], list[str]] = collections.defaultdict(list)
    clause_by_subject: dict[str, list[str]] = collections.defaultdict(list)
    for r in rules_2023:
        c = clause_of(r["section"])
        by_clause_kind[(c, r["kind"])].append(r["id"])
        if c not in clause_by_subject[subject_key(r.get("subject"))]:
            clause_by_subject[subject_key(r.get("subject"))].append(c)
    index: dict[str, list[int]] = collections.defaultdict(list)
    bags = []
    for i, r in enumerate(rules_2023):
        bag = words(r.get("text", "")) | {f"s:{w}" for w in words(r.get("subject", ""))}
        bags.append(bag)
        for w in bag:
            index[w].append(i)
    idf = {w: math.log(len(rules_2023) / len(post)) for w, post in index.items()}
    out: dict[str, Candidates] = {}
    for r in rules_1985:
        mine = words(r.get("text", "")) | {f"s:{w}" for w in words(r.get("subject", ""))}
        score: dict[int, float] = collections.defaultdict(float)
        for w in mine:
            for i in index.get(w, ()):
                score[i] += idf[w] * (2.0 if w.startswith("s:") else 1.0)
        for i in score:
            score[i] /= math.sqrt(len(bags[i]) + 1)
            if rules_2023[i]["kind"] == r["kind"]:
                score[i] *= 1.5
        clauses = list(dict.fromkeys(routed.get(clause_key_1985(r), []) + clause_by_subject.get(subject_key(r.get("subject")), [])))
        first = list(dict.fromkeys(i for c in clauses for i in by_clause_kind.get((c, r["kind"]), [])))
        named = set(first)
        worded = [rules_2023[i]["id"] for i in sorted(score, key=lambda j: (-score[j], rules_2023[j]["id"]))
                  if rules_2023[i]["id"] not in named][:k]
        out[r["id"]] = Candidates(first, worded)
    return out


def load_catalogs(catalog: pathlib.Path, catalog_2023: pathlib.Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    """The 1985 and the 2023 catalog rules; a 1985 rule whose citation is no citation in the one format is no plan."""
    for path in (catalog, catalog_2023):
        if not path.exists():
            raise SystemExit(f"{TOOL}: no catalog at {path} (python scripts/spec/extract_rule_catalog.py --edition ...)")
    rules = json.loads(catalog.read_text(encoding="utf-8"))["rules"]
    for r in rules:   # every pass quotes the rule's citation (rule_block): it is one in the one format, or no plan
        try:
            citation.parse(r["citation"])
        except citation.CitationError as e:
            raise SystemExit(f"{TOOL}: catalog rule {r['id']}: its citation {r['citation']!r} is none: {e}") from None
    return rules, json.loads(catalog_2023.read_text(encoding="utf-8"))["rules"]


def plan_map(catalog: pathlib.Path, catalog_2023: pathlib.Path, k: int, state: pathlib.Path) -> list[dict[str, Any]]:
    rules, rules_2023 = load_catalogs(catalog, catalog_2023)
    by_id = {r["id"]: r for r in rules_2023}
    routed = routes(state)
    # The routes must be THESE catalogs' (review R1-1): a rule whose clause the route pass never saw would be planned
    # without its strongest signal, and a routed clause the 2023 catalog lacks would name no rule.
    if unseen := sorted({clause_key_1985(r) for r in rules} - set(routed)):
        raise SystemExit(f"{TOOL}: {len(unseen)} 1985 clause(s) of this catalog have no route item (e.g. {unseen[0]}): "
                         "the route pass was planned from another catalog; plan the run again from one catalog")
    if stale := sorted({c for to in routed.values() for c in to} - {c["id"] for c in clauses_2023(rules_2023)}):
        raise SystemExit(f"{TOOL}: the route answers name 2023 clause(s) this 2023 catalog lacks (e.g. {stale[0]}): the "
                         "route pass was planned from another 2023 catalog")
    near = candidates(rules, rules_2023, k, routed)

    def quoted(ids: list[str]) -> list[dict[str, Any]]:
        return [{"id": c, "section": by_id[c].get("section"), "subject": by_id[c].get("subject", ""),
                 "kind": by_id[c]["kind"], "text": by_id[c].get("text", "")} for c in ids]
    return [{"id": r["id"], "rule": {f: r.get(f) for f in ("citation", "module", "level", "box", "subject", "kind", "text")},
             "clause_candidates": quoted(near[r["id"]].clause), "word_candidates": quoted(near[r["id"]].worded)}
            for r in rules]


def map_candidates(item: dict[str, Any]) -> list[dict[str, Any]]:
    """Every candidate of a map item: its clause's, then its own by wording."""
    return item["clause_candidates"] + item["word_candidates"]


def plan_verify(state: pathlib.Path) -> list[dict[str, Any]]:
    m = Pass(state, "map")
    items, answers = m.items(), m.answers()
    if missing := [cid for cid in items if cid not in answers]:
        raise SystemExit(f"{TOOL}: {len(missing)} map item(s) are unanswered; collect and retry the map pass first")
    out = []
    for cid, it in items.items():
        a = answers[cid]["answer"]
        if a["verdict"] == SAME:
            chosen = set(a["rules_2023"])
            out.append({"id": it["id"], "rule": it["rule"],
                        "candidates": [c for c in map_candidates(it) if c["id"] in chosen]})
    return out


def plan_claims(claims_file: pathlib.Path, repo: pathlib.Path = spec_pdf.REPO) -> list[dict[str, Any]]:
    """Each claim with the 1985 text of the locator it names (citation.regions: every region the locator owns)."""
    claims = json.loads(claims_file.read_text(encoding="utf-8"))
    loaded = citation.regions(1985, repo)
    if loaded is None:
        raise SystemExit(f"{TOOL}: no 1985 transcription to quote ({spec_pdf.markdown(1985, repo)})")
    lines, regions = loaded
    out = []
    for c in claims:
        spans = [(s, e) for loc, _t, s, e in regions if loc == c["locator"]]
        if not spans:
            raise SystemExit(f"{TOOL}: claim {c['id']}: the 1985 transcription has no {c['locator']}")
        text = "\n\n".join("\n".join(lines[s:e]) for s, e in spans)[:REGION_CHARS]
        out.append({"id": c["id"], "claim": c["claim"], "locator": c["locator"], "text": text})
    return out


def dispositions(state: pathlib.Path) -> dict[str, dict[str, Any]]:
    """Each mapped 1985 rule's disposition: CARRIES (SAME, upheld: the 2023 verdict carries over), OWN-ROW (DIFFERS or NONE: a
    1985 row of its own), DISPUTED (SAME, refuted or unsure: a fleet adjudicator decides), UNVERIFIED (SAME, no verify answer
    yet) or OPEN (no map answer yet)."""
    m, v = Pass(state, "map"), Pass(state, "verify")
    verify = {a["id"]: a["answer"] for a in v.answers().values()} if v.items_file.exists() else {}
    answers = m.answers()
    out = {}
    for cid, it in m.items().items():
        a = answers.get(cid, {}).get("answer")
        row: dict[str, Any] = {"citation": it["rule"]["citation"], "map": a and a["verdict"],
                               "rules_2023": a["rules_2023"] if a else [], "verify": None}
        if a is None:
            row["disposition"] = "OPEN"
        elif a["verdict"] != SAME:
            row["disposition"] = "OWN-ROW"
        else:
            got = verify.get(it["id"])
            row["verify"] = got and got["verdict"]
            row["disposition"] = "UNVERIFIED" if got is None else "CARRIES" if got["verdict"] == "UPHELD" else "DISPUTED"
        out[it["id"]] = row
    return out


def plan_drafts(state: pathlib.Path) -> list[dict[str, Any]]:
    disp = dispositions(state)
    if waiting := [i for i, d in disp.items() if d["disposition"] in ("OPEN", "UNVERIFIED")]:
        raise SystemExit(f"{TOOL}: {len(waiting)} rule(s) have no final disposition yet; finish the map and verify passes")
    items = Pass(state, "map").items()
    return [{"id": it["id"], "rule": it["rule"]} for it in items.values() if disp[it["id"]]["disposition"] == "OWN-ROW"]


# -- the requests ---------------------------------------------------------------------------------------------------------

def rule_block(rule: dict[str, Any]) -> str:
    level = "" if rule.get("level") is None else f", level {rule['level']}"
    return (f"THE 1985 RULE ({rule['citation']}, {rule.get('module') or 'no module'} module{level}; "
            f"subject: {rule.get('subject') or '-'}):\n{rule['text']}")


def candidate_block(cands: list[dict[str, Any]]) -> str:
    return "\n\n".join(f"[{c['id']}] (§2023:{c['section']}, {c['kind']}; subject: {c['subject'] or '-'}):\n{c['text']}"
                       for c in cands)


def prompt_parts(pass_name: str, item: dict[str, Any]) -> tuple[str, str, str]:
    """A request's text in its three cache layers (Anthropic's notice of 2026-11-08), most shared first:
    (what every request of the pass shares: its cached `system`; what a GROUP of requests shares: a cached first user
    block, or "" where the pass has no group; what is the item's own). The route pass's every request shares the list of
    every 2023 clause; a map request shares its clause's candidates with the other rules of that clause and kind."""
    shared, group = PROMPTS[pass_name], ""
    if pass_name == "map":
        group = f"THE 2023 CANDIDATES OF ITS CLAUSE:\n{candidate_block(item['clause_candidates']) or '(none)'}"
        own = (f"{rule_block(item['rule'])}\n\nTHE 2023 CANDIDATES BY WORDING:\n"
               f"{candidate_block(item['word_candidates']) or '(none)'}")
    elif pass_name == "verify":
        own = f"{rule_block(item['rule'])}\n\nTHE 2023 RULES JUDGED SAME:\n{candidate_block(item['candidates']) or '(none)'}"
    elif pass_name == "route":
        c = item["clause"]
        shared += "\n\nTHE 2023 CLAUSES:\n" + "\n".join(f"{x['id']} {x['subject'] or '-'}" for x in item["clauses_2023"])
        own = (f"THE 1985 CLAUSE ({c['document']} {c['locator']}, {c['module'] or 'no module'} module; subject: "
               f"{c['subject'] or '-'}; {c['rules']} rule(s), the first of them):\n" + "\n".join(c["sample"]))
    elif pass_name == "claims":
        own = f"THE CLAIM ({item['id']}):\n{item['claim']}\n\nTHE 1985 TEXT (§1985:{item['locator']}):\n{item['text']}"
    else:
        own = rule_block(item["rule"])
    return shared, group, own


def prompt_text(pass_name: str, item: dict[str, Any]) -> str:
    """The whole text the model reads for `item`, its layers in order."""
    return "\n\n".join(p for p in prompt_parts(pass_name, item) if p)


def stamp_shapes() -> list[re.Pattern[str]]:
    """Every per-copy stamp shape a prompt must not carry: every edition's and the tree's (verify_publishable)."""
    shapes = [s for e in sorted(verify_publishable.FORBIDDEN) for s in verify_publishable.FORBIDDEN[e]]
    return [re.compile(p) for p, _ in dict.fromkeys(shapes + verify_publishable.tree_shapes())]


def request(pass_name: str, cid: str, item: dict[str, Any], model: str, shapes: list[re.Pattern[str]]) -> dict[str, Any]:
    shared, group, own = prompt_parts(pass_name, item)
    if any(p.search(part) for p in shapes for part in (shared, group, own)):
        raise SystemExit(f"{TOOL}: the {pass_name} request for {item['id']} carries a per-copy stamp shape; nothing submitted")
    return {"custom_id": cid, "params": {"model": model, "max_tokens": MAX_OUTPUT_TOKENS,
                                         "output_config": {"effort": EFFORT[pass_name]},
                                         "system": cached_system(shared),
                                         "messages": [{"role": "user", "content": [cached_text(group)] * bool(group)
                                                       + [{"type": "text", "text": own}]}]}}


# -- the answers ----------------------------------------------------------------------------------------------------------

def parse_answer(pass_name: str, item: dict[str, Any], text: str) -> tuple[dict[str, Any] | None, str]:
    """The reply as the pass's answer, or None and why it is not one."""
    body = re.sub(r"\A\s*```[a-z]*\s*|\s*```\s*\Z", "", text)
    try:
        obj = json.loads(body[body.index("{"):body.rindex("}") + 1])
    except ValueError:
        return None, "unparsed"
    if not isinstance(obj, dict):
        return None, "not an object"
    if pass_name == "drafts":
        exp = obj.get("expected")
        ok = (isinstance(obj.get("program"), str) and obj["program"].strip() and isinstance(exp, dict)
              and exp.get("outcome") in ("refused", "output")
              and (exp["outcome"] == "refused" or isinstance(exp.get("output"), list)))
        return (obj, "") if ok else (None, "not a draft")
    if obj.get("verdict") not in VERDICTS[pass_name]:
        return None, f"verdict {obj.get('verdict')!r}"
    if pass_name == "route":
        ids = obj.get("clauses_2023")
        if not isinstance(ids, list) or not set(ids) <= {c["id"] for c in item["clauses_2023"]}:
            return None, "clauses_2023 names a clause that is not in the list"
        if (obj["verdict"] == NONE) != (not ids) or len(ids) > MAX_ROUTES:
            return None, f"clauses_2023 must be empty exactly when the verdict is NONE, and name at most {MAX_ROUTES}"
    if pass_name == "map":
        ids = obj.get("rules_2023")
        known = {c["id"] for c in map_candidates(item)}
        if not isinstance(ids, list) or not set(ids) <= known:
            return None, "rules_2023 names a rule that is no candidate"
        if (obj["verdict"] == NONE) != (not ids):
            return None, "rules_2023 must be empty exactly when the verdict is NONE"
    if pass_name == "claims" and obj["verdict"] != "NOT-ADDRESSED":
        found, why = cited(obj)
        if found is None:
            return None, why
        obj["citation"] = citation.render(found)
    return obj, ""


def cited(obj: dict[str, Any]) -> tuple[citation.Citation | None, str]:
    """The 1985 citation an answer's `locator` and `rule` fields name (CITATION_FIELDS), validated against the 1985
    transcription, or None and why not: a locator the text lacks is a reply to retry, never a citation."""
    locator, rule = obj.get("locator"), obj.get("rule")
    if not isinstance(locator, str) or not (rule is None or isinstance(rule, str)):
        return None, "citation: the locator must be text and the rule text or null"
    try:
        found = citation.parse(f"§1985:{locator.strip()}" + (f" {rule.strip()}" if rule and rule.strip() else ""))
    except citation.CitationError as e:
        return None, f"citation: {e}"
    if (missing := citation.validate(found)) is not None:
        return None, f"citation: {missing}"
    return found, ""


def collect(p: Pass, st: dict[str, Any], client: Any) -> None:
    items = p.items()

    def take(cid: str, message: Any, _batch: dict[str, Any]) -> str | None:
        if p.answer_file(cid).exists():
            return None                      # answered by an earlier batch: kept, never overwritten
        text = reply_text(message)
        answer, why = parse_answer(p.name, items[cid], text)
        if answer is None:
            p.raw_dir.mkdir(parents=True, exist_ok=True)
            write_atomic(p.raw_dir / f"{cid}.txt", text)
            return why
        p.answers_dir.mkdir(parents=True, exist_ok=True)
        write_atomic(p.answer_file(cid), json.dumps({"id": items[cid]["id"], "answer": answer}, indent=1, ensure_ascii=False))
        return None
    p.batches.collect(st, client, take)


# -- cost -----------------------------------------------------------------------------------------------------------------

def per_request_usd(p: Pass, st: dict[str, Any], sample: Iterable[str]) -> tuple[float, str]:
    """The cost of one request: the mean MEASURED over the pass's replies, or, before any, an estimate from the prompt length
    of `sample` and the pass's expected output."""
    usage = st.get("usage", {})
    if usage:
        return p.batches.cost_so_far(st) / len(usage), f"measured over {len(usage)}"
    items = p.items()
    chosen = list(sample)[:50] or list(items)[:50]
    if not chosen:
        return 0.0, "nothing to estimate"
    chars = sum(len(prompt_text(p.name, items[c])) for c in chosen) / len(chosen)
    return usd(st["model"], int(chars / CHARS_PER_TOKEN), EST_OUTPUT_TOKENS[p.name]), "estimated before a canary"


def guard(p: Pass, st: dict[str, Any], todo: list[str], max_usd: float) -> None:
    per, how = per_request_usd(p, st, todo)
    spent = p.batches.cost_so_far(st)
    in_flight = len(p.batches.in_flight(st))
    estimate = spent + per * (in_flight + len(todo))
    print(f"{p.name}: {len(todo)} request(s) to submit; ${per:.4f} per request ({how}); spent ${spent:.2f}, in flight "
          f"{in_flight}; total ${estimate:.2f} against --max-usd {max_usd:.2f}", flush=True)
    if estimate > max_usd:
        raise SystemExit(f"{TOOL}: the estimate ${estimate:.2f} exceeds --max-usd {max_usd}; nothing submitted")


def canary_proven(p: Pass, st: dict[str, Any]) -> bool:
    """A collected canary batch of this pass holds at least one valid answer."""
    return any(b.get("canary") and b.get("collected") and any(p.answer_file(c).exists() for c in b["items"])
               for b in st["batches"])


# -- command line ---------------------------------------------------------------------------------------------------------

def build_parser() -> argparse.ArgumentParser:
    ap = argparse.ArgumentParser(description="The edition audit's Message Batches passes (restartable).")
    sub = ap.add_subparsers(dest="cmd", required=True)

    def common(p: argparse.ArgumentParser, key: bool = True, pass_arg: bool = True) -> None:
        p.add_argument("--state", required=True, help="the run's directory (one subdirectory per pass)")
        if pass_arg:
            p.add_argument("--pass", dest="pass_name", required=True, choices=sorted(PROMPTS))
        if key:
            p.add_argument("--api-key-file", help="the Anthropic API key file: one key, or one labeled key per account (api_key.py)")
            p.add_argument("--api-account", help=ACCOUNT_HELP + "; the pass's state.json records it, and a later command keeps it")

    pl = sub.add_parser("plan", help="write a pass's items (once)")
    common(pl, key=False)
    pl.add_argument("--catalog", default=str(OUT_1985), help="the 1985 rule catalog (route, map)")
    pl.add_argument("--catalog-2023", default=str(OUT), help="the 2023 rule catalog (route, map)")
    pl.add_argument("--candidates", type=int, default=15,
                    help="the 2023 rules by wording per 1985 rule, after those of its routed clauses (map)")
    pl.add_argument("--claims", help="the VERSION_CHANGE_REFERENCE claims, a JSON list of {id, claim, locator} (claims)")
    for name, helptext in (("submit", "submit the unanswered items not in any batch"),
                           ("retry", "resubmit the items no answer covers and no uncollected batch holds")):
        s = sub.add_parser(name, help=helptext)
        common(s)
        s.add_argument("--max-usd", type=float, required=True, help="refuse a submit whose estimate exceeds this")
        s.add_argument("--model", default=DEFAULT_MODEL)
        if name == "submit":
            s.add_argument("--canary", type=int, metavar="N", help="submit only the first N, as the pass's canary")
    common(sub.add_parser("status", help="items, answers, verdicts and cost; with a key, each batch's state too"))
    common(sub.add_parser("collect", help="store the answers of every ended batch"))
    common(sub.add_parser("results", help="write DIR/results.json: each mapped rule's disposition"), key=False, pass_arg=False)
    return ap


def print_status(p: Pass, st: dict[str, Any]) -> None:
    items, answers = p.items(), p.answers()
    tally = collections.Counter(a["answer"].get("verdict", "draft") for a in answers.values())
    bad = sum(len(b.get("bad", [])) for b in st["batches"])
    print(f"{p.name}: items {len(items)}; answered {len(answers)}; in flight {len(p.batches.in_flight(st))}; bad replies {bad}; "
          f"cost so far ${p.batches.cost_so_far(st):.2f}; verdicts {dict(sorted(tally.items()))}", flush=True)
    if st.get("batches"):
        print(f"batches billed to the account {st.get('api_account') or '(one unlabeled key)'}", flush=True)


def main(argv: list[str] | None = None, client_factory: Callable[[str], Any] = make_client) -> int:
    a = build_parser().parse_args(argv)
    state = pathlib.Path(a.state)
    if a.cmd == "results":
        disp = dispositions(state)
        write_atomic(state / "results.json", json.dumps(disp, indent=1, ensure_ascii=False))
        print(f"results: {dict(sorted(collections.Counter(d['disposition'] for d in disp.values()).items()))} "
              f"-> {state / 'results.json'}", flush=True)
        return 0
    p = Pass(state, a.pass_name)
    if a.cmd == "plan":
        if a.pass_name == "claims" and not a.claims:
            raise SystemExit(f"{TOOL}: plan --pass claims needs --claims")
        planners: dict[str, Callable[[], list[dict[str, Any]]]] = {
            "route": lambda: plan_route(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023)),
            "map": lambda: plan_map(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023), a.candidates, state),
            "verify": lambda: plan_verify(state),
            "claims": lambda: plan_claims(pathlib.Path(a.claims)),
            "drafts": lambda: plan_drafts(state),
        }
        return write_plan(p, planners[a.pass_name]())
    st = p.batches.load(model=getattr(a, "model", DEFAULT_MODEL))
    if a.api_account and not a.api_key_file:
        raise SystemExit(f"{TOOL}: --api-account names which key of --api-key-file to bill; give the key file")
    if a.cmd == "status" and not a.api_key_file:
        print_status(p, st)
        return 0
    if not a.api_key_file:
        raise SystemExit(f"{TOOL}: {a.cmd} calls the API and needs --api-key-file")
    items = p.items()
    client = client_factory(p.batches.load_key(a.api_key_file, a.api_account, st).key)
    p.batches.adopt_pending(st, client)
    shapes = stamp_shapes()
    if a.cmd in ("submit", "retry"):
        held = set(p.batches.in_flight(st)) if a.cmd == "retry" else p.batches.covered(st)
        todo = [cid for cid in p.unanswered() if cid not in held]
        proven = canary_proven(p, st)
        if a.cmd == "submit":
            canary = a.canary is not None          # a canary is a submit's first N
            if canary:
                todo = todo[:a.canary]
        else:
            # A retry before a proven canary resubmits the canary's own unanswered items, AS the canary, and nothing
            # else: the never-submitted items wait for the full submit the proven canary allows (kb/Work PB4930).
            canary = not proven
            if canary:
                tried = {c for b in st["batches"] if b.get("canary") for c in b["items"]}
                todo = [cid for cid in todo if cid in tried]
                if not todo:
                    raise SystemExit(f"{TOOL}: the {p.name} pass has no canary item to retry (none is unanswered "
                                     "outside a running batch): submit --canary N first")
        if not canary and not proven:
            raise SystemExit(f"{TOOL}: the {p.name} pass has no collected canary with a valid answer: submit --canary N "
                             "first, collect it, and read its answers and cost")
        guard(p, st, todo, a.max_usd)
        reqs = [request(p.name, cid, items[cid], st["model"], shapes) for cid in todo]   # every request checked first
        p.batches.submit(st, client, reqs, canary=canary)
        print_status(p, st)
    elif a.cmd == "status":
        for b in st["batches"]:
            print(b["id"], client.messages.batches.retrieve(b["id"]).processing_status, flush=True)
        print_status(p, st)
    elif a.cmd == "collect":
        collect(p, st, client)
        print_status(p, st)
    return 0


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
