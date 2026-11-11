#!/usr/bin/env python3
"""audit_batch.py - THE Message Batches arm of the edition audit (kb/Work PB4886; DESIGN-spec-conformance-review.md §10).

The 1985 audit is the 2023 audit run with an edition parameter. Its independent JUDGMENTS go to a model through the Message
Batches API in eight passes. The first five are DETECTIONS; the last three DECIDE each rule's change record against the next
held edition (kb/Work PB4995, PB5008), and the fleet decides only what they leave open:

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
          requires, which the fleet compiles and runs under --std=1985 and checks against the rule text before any use;
  adjudicate  one request per DISPUTED or OWN-ROW rule: its candidates and both readers' answers; the answer is the rule's
          change record, KEPT (with the 2023 rules it carries into), CHANGED (with the rules and the words that differ) or
          REMOVED (the 2023 text drops it), or OPEN when the texts do not decide it;
  refute  the independent refuter of a decision that names no rule of the next edition (REMOVED): one request each, which
          states the strongest reading on which the requirement survives first (UPHELD, REFUTED, UNSURE);
  recheck the independent check of a decision that names the next edition's rules (KEPT, CHANGED), framed to DISPROVE it
          (kb/Work PB5008): one request per decision, at the highest effort, with the whole 1985 clause and the 1985 rules
          most like it (another rule may already allow what the decision calls new), and every rule of every 2023 clause
          that holds the feature or a decided rule (the record may omit the rule that states the requirement); the answer
          is the change record the rechecker finds, and it upholds the decision only when it is the same record.
          A decision its checker (`checker`) upholds, and no other check disputes, is the rule's change record
          (`dispositions`' `change`, which `change-batch` writes); an OPEN or disputed one is CONTESTED and goes to a fleet
          adjudicator, never to a vote.

A ninth pass serves the 1985 transcription, not the change records (kb/Work PB4984):

  misprints one request per 1985 catalog rule as it now reads, with the 2023 rules its map answer paired it with (none
          for a NONE, or a rule the map never saw, which is read for its own sense alone): a proofreader of the misprints
          a spelling check cannot see, a real word printed for another (MISPRINT, with the printed words, the intended
          words and their context, all checked against the rule's text, or CLEAN). Each is a LEAD (`misprint-leads`),
          which a reader of the rendered page confirms before the Errata lists it.

    python scripts/spec/audit_batch.py plan    --pass route  --state DIR [--catalog F] [--catalog-2023 F]
    python scripts/spec/audit_batch.py plan    --pass map    --state DIR [--catalog F] [--catalog-2023 F] [--candidates 15]
                                                                                   # once every route item is answered
    python scripts/spec/audit_batch.py plan    --pass claims --state DIR --claims FILE
    python scripts/spec/audit_batch.py plan    --pass verify --state DIR          # once every map item is answered
    python scripts/spec/audit_batch.py plan    --pass drafts --state DIR          # once every verify item is answered
    python scripts/spec/audit_batch.py plan    --pass adjudicate --state DIR      # once every verify item is answered
    python scripts/spec/audit_batch.py plan    --pass refute --state DIR          # once every adjudicate item is answered
    python scripts/spec/audit_batch.py plan    --pass recheck --state DIR [--catalog F] [--catalog-2023 F] [--near-1985 10]
                                                                                   # once every adjudicate item is answered
    python scripts/spec/audit_batch.py plan    --pass misprints --state DIR [--catalog F]  # once every map item is answered
    python scripts/spec/audit_batch.py submit  --pass P --state DIR --api-key-file KEY [--api-account A] --max-usd N --canary 20
    python scripts/spec/audit_batch.py submit  --pass P --state DIR --api-key-file KEY --max-usd N      # the rest
    python scripts/spec/audit_batch.py status  --pass P --state DIR [--api-key-file KEY]
    python scripts/spec/audit_batch.py collect --pass P --state DIR --api-key-file KEY
    python scripts/spec/audit_batch.py retry   --pass P --state DIR --api-key-file KEY --max-usd N
    python scripts/spec/audit_batch.py withdraw --pass P --state DIR             # each stored answer the shape now
                                                                                   # refuses, to raw/; retry asks again
    python scripts/spec/audit_batch.py seed    --state NEW --upstream DIR --decisions BATCH [--inherit route,map,verify]
                                                                                   # a run checking a batch's decisions:
                                                                                   # then plan its recheck and refute
    python scripts/spec/audit_batch.py results --state DIR                        # DIR/results.json: each rule's disposition
    python scripts/spec/audit_batch.py misprint-leads --state DIR                 # DIR/misprint-leads.json: each misprint
                                                                                   # the misprints pass reports
    python scripts/spec/audit_batch.py change-batch --state DIR --out FILE        # every DECIDED change record (KEPT,
                                                                                   # CHANGED, REMOVED; record_verdicts.py
                                                                                   # --edition 1985) no adjudication
                                                                                   # outranks
    python scripts/spec/test_audit_batch.py                                       # its self-test: a fake client, no API call

RESTARTABLE. A pass lives in `DIR/<pass>/`: `items.json` (written by `plan`: custom id -> the item, every input its prompt
needs, so a prompt is rebuilt from the run alone; a re-plan keeps it when it is the same, GROWS it when the new plan keeps
every planned item unchanged and adds others, and is refused otherwise: `write_plan`), `state.json` (message_batches.BatchRun, THE restartable batch state
transcribe_pdf.py shares: an intent before every create, the batch after, a crash adopted on the next run) and
`answers/<custom id>.json`, each written once, atomically, and moved to `raw/` only by `withdraw`, when the pass's answer
shape has grown past it. A reply that does not parse into the pass's answer shape is kept in
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

THE KEY is read from `--api-key-file` (api_key.read_api_key; `--api-account` chooses among labeled keys; each batch records the
account it billed, never the key, and a pass may move to another account once its batches are collected:
message_batches.BatchRun.load_key). No self-test opens a real key file.
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
from inventory_schema import ChangeRecord, edition, load_schema  # noqa: E402  (the 1985 edition and its change record)
from inventory_store import inventory_dir, read_store  # noqa: E402  (the 1985 store, whose adjudications outrank the run)
from message_batches import (CACHE_MIN_SHARERS, DEFAULT_MODEL, BatchRun, cached_system, cached_text,  # noqa: E402
                             make_client, reply_text, usd, write_atomic)  # noqa: E402  (THE batch state)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/
from api_key import ACCOUNT_HELP  # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

TOOL = "audit_batch"
#: A 1985 region quoted to a claims request is cut at this many characters (a claim names a paragraph, not a section).
REGION_CHARS = 12000
#: Characters per input token, for the estimate before a canary has measured one.
CHARS_PER_TOKEN = 3.5

SAME, DIFFERS, NONE = "SAME", "DIFFERS", "NONE"
#: A mapped 1985 rule's disposition (`dispositions`, the run's results.json): CARRIES is a SAME the verify pass upheld, whose
#: decided change is the chained (KEPT) change record (kb/Work PB4993); the others are the adjudicate pass's, or in flight.
CARRIES, OWN_ROW, DISPUTED, UNVERIFIED, OPEN = "CARRIES", "OWN-ROW", "DISPUTED", "UNVERIFIED", "OPEN"
#: The dispositions the adjudicate pass decides: a refuted or unsure SAME, and a DIFFERS or NONE answer (kb/Work PB4995).
ADJUDICATED = (DISPUTED, OWN_ROW)
#: The adjudicator's answer when the texts do not decide a rule's change record. Its other answers are the change record's
#: kinds, the schema's `next.kinds` (`change_kinds`): no code here spells a kind.
UNDECIDED = "OPEN"


def change_kinds() -> ChangeRecord:
    """The change record's shape, the schema's `next` (read when asked: the schema loads lazily, kb/Work PB4993)."""
    return load_schema().next

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
Below is the list of every clause of ISO/IEC 1989:2023 that has rules, each with its number and subject, and then one
clause of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993): its subject, its module and its first rules. Name
every 2023 clause whose rules state the same language feature as this 1985 clause, however it is now named or organized: a 1985
module's copy of a clause (the OPEN statement of the indexed I-O module) and the one 2023 clause that now holds it, a clause
renamed, a 1985 clause whose rules 2023 spreads over several clauses (a general concept and the statement that uses it).
- ROUTED: one or more 2023 clauses state this feature; name them, the closest first;
- NONE: no 2023 clause states it (a deleted feature, such as the communication module).
The JSON object: {"verdict": "ROUTED|NONE", "clauses_2023": ["clause number", ...], "reason": "at most 200 characters"}.
"clauses_2023" names only clause numbers of the list, at most 8, and is empty for NONE.""",
    "map": COMMON + """
Compare one rule of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993) with candidate rules of ISO/IEC 1989:2023:
first the candidates from the 2023 clauses that hold the 1985 rule's feature, then the 1985 rule, then more candidates
found by its wording.
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
A program the compiler must refuse violates the rule on exactly one line and nothing else: give that line's number (counting
the program's first line as 1) and a control, the same program with that line made conforming, which the compiler must
accept, so that a refusal is tied to the rule and never to another error.
The JSON object: {"program": "the source text, lines separated by \\n", "expected": {"outcome": "refused|output",
"output": ["each DISPLAYed line", ...], "line": the violating line's number, "control": "the conforming program"},
"basis": "the words of the rule that decide the outcome, at most 200 characters"}; "output" is given for output only,
"line" and "control" for refused only.""",
    "adjudicate": COMMON + """
Decide how ISO/IEC 1989:2023 carries one rule of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993). You get
candidate 2023 rules, the 1985 rule, and two earlier readers' answers, which disagree or found a difference: judge the texts
yourself, and use the readers only as leads.
- KEPT: one or more candidates state the same requirement in substance: the same programs are accepted or refused, with the
  same effect. Wording, numbering and terms for the same thing ("must" and "shall", "nonnumeric literal" and "alphanumeric
  literal") are no difference, and neither is the 1985 level-2 box.
  Compare the two on the programs the 1985 edition ADMITS (its general formats and syntax rules): a case 2023 adds that
  no 1985 program can write (new syntax, a new operand class) does not make this rule CHANGED, and a 2023 rule about
  such syntax is not a candidate to name (kb/Work PB5021). The 1985 general format or syntax rule that excludes the
  syntax is the one that is CHANGED.
- CHANGED: candidates cover the same subject but the requirement differs in substance (a case added or removed, a result
  changed, made undefined or implementor-defined); name them and quote the words that differ.
- REMOVED: the 2023 standard drops the feature or the requirement (a deleted module, statement, clause or phrase), and the
  candidates show it: one covers the subject and no longer states the requirement, or none covers a feature 2023 lacks.
- OPEN: the texts below do not let you decide (the counterpart may lie outside the candidates).
The JSON object: {"verdict": "KEPT|CHANGED|REMOVED|OPEN", "rules_2023": ["candidate id", ...], "differs": {"1985": "the 1985
words", "2023": "the 2023 words, or what removed it"}, "reason": "at most 200 characters"}. "rules_2023" names only candidate
ids, is non-empty for KEPT and CHANGED and empty for REMOVED and OPEN; "differs" is empty for KEPT and OPEN.""",
    "refute": COMMON + """
An adjudicator decided that ISO/IEC 1989:2023 drops the ANSI X3.23-1985 rule below (REMOVED): no 2023 rule states its
requirement. Try to REFUTE that decision from the texts. FIRST write the strongest reading on which the requirement
survives in 2023: the candidate below that comes closest to stating it, in other words, under another name or in another
clause. THEN judge whether that reading holds.
- UPHELD: even the strongest reading fails: no candidate states the requirement;
- REFUTED: a candidate states it (quote it);
- UNSURE: the texts do not let you decide.
The JSON object: {"counter_reading": "the strongest reading, at most 200 characters", "verdict": "UPHELD|REFUTED|UNSURE",
"reason": "at most 200 characters", "differs": {"1985": "...", "2023": "..."}}; "differs" is empty unless REFUTED.""",
    "recheck": COMMON + """
An adjudicator decided how ISO/IEC 1989:2023 carries the ANSI X3.23-1985 rule below. An audit of such decisions found one
in four wrong, in three ways, so try to DISPROVE this one. Work through all three, quoting both texts:
(a) Search the other 1985 rules below for one that bears on the same element: a 1985 rule elsewhere may already allow or
    require what the decision calls a change, so that there is none.
(b) Find EVERY 2023 rule below that states any part of the 1985 requirement: a general rule of the clause, a definition
    restated in another clause, a general rule over a whole class of statements, not only the rules the decision names.
    Name a rule that states part of the requirement; never one that only shares its subject.
(c) Say whether a program the 1985 rule makes the compiler refuse is, in 2023, accepted and given a run-time exception
    condition instead (or the reverse): that is a change.
Then give the change record the texts support, whether or not it is the adjudicator's:
- KEPT: the 2023 rules state the same requirement in substance: the same programs are accepted or refused, with the same
  effect. Wording, numbering and terms for the same thing are no difference, and neither is the 1985 level-2 box.
  Compare the two on the programs the 1985 edition ADMITS (its general formats and syntax rules): a case 2023 adds that
  no 1985 program can write does not make this rule CHANGED, and a 2023 rule about such syntax is not one to name
  (kb/Work PB5021); the 1985 general format or syntax rule that excludes the syntax is the one that is CHANGED.
- CHANGED: they cover the same subject with a different requirement (a case added or removed, a result changed, made
  undefined or implementor-defined, a refusal made an exception condition); quote the words that differ.
- REMOVED: the 2023 standard drops the feature or the requirement, and no rule below states it.
- OPEN: the texts below do not let you decide.
The JSON object: {"verdict": "KEPT|CHANGED|REMOVED|OPEN", "rules_2023": ["the id of every 2023 rule below that states any
part of the requirement"], "rules_1985": ["the id of every other 1985 rule below that bears on the same element"],
"exception": {"1985": "the words that make it a refusal", "2023": "the words that make it an exception condition"},
"differs": {"1985": "the 1985 words", "2023": "the 2023 words, or what removed it"}, "reason": "at most 300 characters"}.
"rules_2023" names only ids shown below, is non-empty for KEPT and CHANGED and empty for REMOVED and OPEN; "rules_1985"
names only 1985 ids shown below, and is empty when no other rule bears on it; "exception" is empty unless (c) found one;
"differs" is empty for KEPT and OPEN.""",
    "misprints": COMMON + """
Proofread one rule of ANSI X3.23-1985 (as amended by X3.23a-1989 and X3.23b-1993) for MISPRINTS: a word printed in error
that is itself a real word, so a spelling check cannot see it ("form" for "from", "of" for "or", "than" for "then", "date"
for "data", "and" for "any", "identifier-1" for "identifier-2", a dropped "not"). Read the 1985 sentence for its own sense;
where 2023 rules an earlier reader paired with it are given below, a word that differs from them is a lead, never proof.
A requirement the two editions state differently on purpose (a case added or removed, a term renamed, such as "nonnumeric
literal" and "alphanumeric literal") is NO misprint, and neither is a non-word: report only a real word the sentence's own
sense shows was meant to be another.
- MISPRINT: one or more words read as a misprint of the word the sense requires;
- CLEAN: none does.
The JSON object: {"verdict": "MISPRINT|CLEAN", "misprints": [{"printed": "the misprinted word or words, exactly as the 1985
rule prints them", "intended": "the words the sense requires", "context": "the 1985 rule's words around it, exactly as
printed, at most 120 characters"}], "reason": "at most 200 characters"}; "misprints" is empty exactly when CLEAN.""",
}
#: The output tokens of one reply, for the estimate before a canary has measured them (thinking included).
#: The recheck's is its canary's measured mean (22,300 over 20 replies on state-route, kb/Work PB4995).
EST_OUTPUT_TOKENS = {"route": 1200, "map": 1500, "verify": 1500, "claims": 1200, "drafts": 2500, "adjudicate": 2500,
                     "refute": 2000, "recheck": 22000, "misprints": 1200}
EFFORT = {"route": "high", "map": "medium", "verify": "high", "claims": "medium", "drafts": "medium", "adjudicate": "high",
          "refute": "high", "recheck": "max", "misprints": "medium"}
#: The most output tokens one reply may spend, thinking included: the recheck thinks at the highest effort over whole
#: clauses, and a reply cut off at the limit is no answer (4 of its canary's 20 replies stopped at 32,000, kb/Work PB4995).
MAX_OUTPUT_TOKENS = {**dict.fromkeys(EFFORT, 8000), "recheck": 64000}
ROUTED = "ROUTED"
#: The most 2023 clauses one route answer may name: a 1985 clause spread over more than this is no single feature.
MAX_ROUTES = 8
VERDICTS = {"route": {ROUTED, NONE}, "map": {SAME, DIFFERS, NONE}, "verify": {"UPHELD", "REFUTED", "UNSURE"},
            "claims": {"CONFIRMED", "REFUTED", "NOT-ADDRESSED"}, "refute": {"UPHELD", "REFUTED", "UNSURE"},
            "misprints": {"MISPRINT", "CLEAN"}}


#: The passes whose answer is a change record: the change record's kinds, and UNDECIDED.
RECORDING = ("adjudicate", "recheck")


def verdicts(pass_name: str) -> set[str]:
    """The verdicts a pass's answer may give: a recording pass's are the change record's kinds, and UNDECIDED."""
    return {*change_kinds().kinds, UNDECIDED} if pass_name in RECORDING else VERDICTS[pass_name]
#: The passes whose one item is a map item's rule with its candidates: their requests share the map's cached clause block.
WITH_CANDIDATES = ("map", "adjudicate", "refute")
#: The passes that check an adjudication: each decided kind has ONE checker (`checker`), and any check that answered
#: and disputes the decision makes it contested.
CHECKS = ("refute", "recheck")


def checker(kind: Any) -> str:
    """The pass that checks a decided adjudication of `kind` (a `ChangeRecord` kind): the recheck when it names rules of
    the next edition (KEPT, CHANGED: kb/Work PB5008 found the refuter upheld every one of the 8 wrong ones it sampled),
    the refuter when it names none (REMOVED). Read from the kind's own `to` flag, the schema's: no kind is spelled."""
    return "recheck" if kind.to else "refute"


def upholds(pass_name: str, decided: dict[str, Any], answer: dict[str, Any]) -> bool:
    """Does check `pass_name`'s answer uphold the adjudication `decided`? The refuter's when it found nothing; the recheck's
    when the record it found is the same record: the same kind into the same 2023 rules (a rule it adds is one the
    decision omits, kb/Work PB5008's commonest error)."""
    if pass_name == "refute":
        return answer["verdict"] == "UPHELD"
    return answer["verdict"] == decided["verdict"] and set(answer["rules_2023"]) == set(decided["rules_2023"])

WORD = re.compile(r"[a-z][a-z0-9-]{2,}")
#: Words too common in both standards to say which rule is which.
STOPWORDS = frozenset("""the and that this with for are any each which from shall must may not its into than then when
where data item items specified clause statement statements rule rules general syntax format following used same value
values one two all being been have has such only also other there their them they will""".split())


# -- the pass's directory --------------------------------------------------------------------------------------------------

#: A run seeded from another (`seed`) names that run and the passes it reads from it; every other pass is its own.
UPSTREAM = "upstream.json"
#: The passes a seeded run may read from its upstream run: the inputs of an adjudication, which nothing in the seeded run
#: writes (the drafts pass is not one: `run_drafts.py` writes beside its answers).
INHERITABLE = ("route", "map", "verify", "claims")
#: A seeded run's own pass whose answers are the decisions it checks, recorded by `seed`, never asked of the model.
SEEDED = "adjudicate"


def upstream_of(state: pathlib.Path) -> dict[str, Any] | None:
    """The seeded run's `upstream.json` ({run, passes}), or None for a run of its own."""
    up = state / UPSTREAM
    return json.loads(up.read_text(encoding="utf-8")) if up.exists() else None


def pass_dir(state: pathlib.Path, name: str) -> pathlib.Path:
    """The directory of `state`'s pass `name`: its own, or its upstream run's when `state` inherits that pass (`seed`)."""
    u = upstream_of(state)
    return pass_dir(pathlib.Path(u["run"]), name) if u and name in u["passes"] else state / name


class Pass:
    """`DIR/<name>/`: items.json, state.json, answers/, raw/. In a seeded run an inherited pass is its upstream run's and
    the seeded pass holds the decisions `seed` recorded: both are read only."""

    def __init__(self, state: pathlib.Path, name: str):
        self.name, self.dir = name, pass_dir(state, name)
        self.inherited = self.dir != state / name
        self.seeded = name == SEEDED and upstream_of(state) is not None
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
    """Write the pass's items once. A plan that exists is kept when it is the same, GROWN when the new plan keeps every
    planned item unchanged and adds others (the drafts of the rules the adjudication made CHANGED or REMOVED, kb/Work
    PB4995), and refused otherwise: a run's answers belong to its items, so no planned item may change or go."""
    planned = {custom_id(p.name, it["id"]): it for it in items}
    if len(planned) != len(items):
        raise SystemExit(f"{TOOL}: the {p.name} plan names an item twice")
    if p.items_file.exists():
        held = json.loads(p.items_file.read_text(encoding="utf-8"))
        if any(planned.get(cid) != it for cid, it in held.items()):
            raise SystemExit(f"{TOOL}: {p.items_file} holds another plan; a new plan needs a new --state")
        if len(planned) == len(held):
            print(f"{p.name}: the plan of {len(planned)} item(s) exists and is unchanged", flush=True)
            return 0
        write_atomic(p.items_file, json.dumps(planned, indent=1, ensure_ascii=False))
        print(f"{p.name}: the plan of {len(held)} item(s) grew by {len(planned) - len(held)}", flush=True)
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


def bag_of(rule: dict[str, Any]) -> set[str]:
    """A rule's words, its subject's marked apart (they weigh twice in `Wording`)."""
    return words(rule.get("text", "")) | {f"s:{w}" for w in words(rule.get("subject", ""))}


class Wording:
    """The rules of one catalog ranked by how like another rule they are in wording: the inverse-document-frequency weight
    of the words they share (the subject's words count twice), normalized by the length of the rule, a rule of the same
    kind preferred. The map pass ranks the 2023 rules by it (`candidates`), the recheck the 1985 rules (`plan_recheck`)."""

    def __init__(self, rules: list[dict[str, Any]]):
        self.rules = rules
        self.bags = [bag_of(r) for r in rules]
        self.index: dict[str, list[int]] = collections.defaultdict(list)
        for i, bag in enumerate(self.bags):
            for w in bag:
                self.index[w].append(i)
        self.idf = {w: math.log(len(rules) / len(post)) for w, post in self.index.items()}

    def nearest(self, rule: dict[str, Any], k: int, skip: set[str]) -> list[str]:
        """The ids of the k rules most like `rule`, the most alike first, none of `skip`."""
        score: dict[int, float] = collections.defaultdict(float)
        for w in bag_of(rule):
            for i in self.index.get(w, ()):
                score[i] += self.idf[w] * (2.0 if w.startswith("s:") else 1.0)
        for i in score:
            score[i] /= math.sqrt(len(self.bags[i]) + 1)
            if self.rules[i]["kind"] == rule["kind"]:
                score[i] *= 1.5
        ranked = sorted(score, key=lambda j: (-score[j], self.rules[j]["id"]))
        return [self.rules[i]["id"] for i in ranked if self.rules[i]["id"] not in skip][:k]


def subject_clauses(rules_2023: list[dict[str, Any]]) -> dict[str, list[str]]:
    """Each heading (`subject_key`) -> the 2023 clauses that carry it, in catalog order."""
    out: dict[str, list[str]] = collections.defaultdict(list)
    for r in rules_2023:
        c = clause_of(r["section"])
        if c not in out[subject_key(r.get("subject"))]:
            out[subject_key(r.get("subject"))].append(c)
    return out


def feature_clauses(rule: dict[str, Any], routed: dict[str, list[str]], by_subject: dict[str, list[str]]) -> list[str]:
    """The 2023 clauses that hold a 1985 rule's feature: those its clause was routed to (closest first), then those that
    share its heading."""
    return list(dict.fromkeys(routed.get(clause_key_1985(rule), []) + by_subject.get(subject_key(rule.get("subject")), [])))


def candidates(rules_1985: list[dict[str, Any]], rules_2023: list[dict[str, Any]], k: int,
               routed: dict[str, list[str]]) -> dict[str, Candidates]:
    """The 2023 rules that may state each 1985 rule, ids in order (kb/Work PB4971), from the strongest signal down:
    1. every 2023 rule of the SAME KIND in the clauses the rule's clause was routed to (`routed`, the route pass, closest
       clause first) or shares a heading with (`subject_key`), in catalog order (`feature_clauses`);
    2. then the k rules most like it by wording that step 1 did not name (`Wording`).
    Step 1 finds the counterpart that wording misses (another clause's name, a module's copy); step 2 finds a rule 2023
    moved to a clause of another subject, and a counterpart of another kind."""
    by_clause_kind: dict[tuple[str, str], list[str]] = collections.defaultdict(list)
    for r in rules_2023:
        by_clause_kind[(clause_of(r["section"]), r["kind"])].append(r["id"])
    by_subject, wording = subject_clauses(rules_2023), Wording(rules_2023)
    out: dict[str, Candidates] = {}
    for r in rules_1985:
        clauses = feature_clauses(r, routed, by_subject)
        first = list(dict.fromkeys(i for c in clauses for i in by_clause_kind.get((c, r["kind"]), [])))
        out[r["id"]] = Candidates(first, wording.nearest(r, k, set(first)))
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
    return [{"id": r["id"], "rule": quoted_rule(r), "clause_candidates": quoted_2023(by_id, near[r["id"]].clause),
             "word_candidates": quoted_2023(by_id, near[r["id"]].worded)}
            for r in rules]


def quoted_rule(rule: dict[str, Any]) -> dict[str, Any]:
    """A 1985 rule as a request quotes it (`rule_block`)."""
    quoted = {f: rule.get(f) for f in ("citation", "module", "level", "box", "subject", "kind", "text")}
    if (provisional := rule.get("provisional", "none")) != "none":
        quoted["provisional"] = provisional   # a reconstruction's text (kb/Work PB4952): every verdict on it says so
    return quoted


def quoted_2023(by_id: dict[str, dict[str, Any]], ids: Iterable[str]) -> list[dict[str, Any]]:
    """2023 rules as a request quotes them (`candidate_block`)."""
    return [{"id": c, "section": by_id[c].get("section"), "subject": by_id[c].get("subject", ""),
             "kind": by_id[c]["kind"], "text": by_id[c].get("text", "")} for c in ids]


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


def plan_misprints(catalog: pathlib.Path, catalog_2023: pathlib.Path, state: pathlib.Path) -> list[dict[str, Any]]:
    """Every rule of the 1985 catalog AS IT NOW READS (the transcription it proofreads, repaired since the map was
    planned), with the 2023 rules the map answer named as its counterparts (SAME or DIFFERS), or none (NONE, or a rule
    the map never saw): the reader of the misprints a spelling check cannot see, a real word printed for another
    (kb/Work PB4984). A rule with no counterpart is read for its own sense alone."""
    m = Pass(state, "map")
    items, answers = m.items(), m.answers()
    if missing := [cid for cid in items if cid not in answers]:
        raise SystemExit(f"{TOOL}: {len(missing)} map item(s) are unanswered; collect and retry the map pass first")
    named = {}
    for cid, it in items.items():
        chosen = set(answers[cid]["answer"]["rules_2023"])
        named[it["id"]] = [c for c in map_candidates(it) if c["id"] in chosen]
    rules, _ = load_catalogs(catalog, catalog_2023)    # every quoted citation one in the one format
    return [{"id": r["id"], "rule": quoted_rule(r), "counterparts": named.get(r["id"], [])} for r in rules]


def misprint_leads(state: pathlib.Path) -> list[dict[str, Any]]:
    """Each misprint the misprints pass reports, with its rule's citation and whether a 2023 counterpart was read beside
    it: a LEAD for a reader of the rendered page, never a correction (the Errata of specs/ANSI_COBOL_1985.md lists only
    what the page shows, kb/Work PB4972)."""
    p = Pass(state, "misprints")
    items = {it["id"]: it for it in p.items().values()}
    return [{"rule-id": a["id"], "citation": items[a["id"]]["rule"]["citation"],
             "paired": bool(items[a["id"]]["counterparts"]), **m}
            for a in p.answers().values() for m in a["answer"]["misprints"]]


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


def answers_by_id(state: pathlib.Path, name: str) -> dict[str, dict[str, Any]]:
    """A pass's answers keyed by item id ({} for a pass not planned)."""
    p = Pass(state, name)
    return {a["id"]: a["answer"] for a in p.answers().values()} if p.items_file.exists() else {}


def change_of(adjudicated: dict[str, Any]) -> dict[str, Any]:
    """An upheld adjudication as its change record's decided part (`change_records` adds the edition, the bridge and the source):
    its kind, the 2023 rules it carries into, and its reason, the words that differ before the adjudicator's why."""
    differs = adjudicated.get("differs") or {}
    words = "; ".join(f'{ed}: "{differs[ed]}"' for ed in ("1985", "2023") if differs.get(ed))   # the words, as written
    reason = (f"{words}. {adjudicated['reason']}" if words else adjudicated["reason"]).strip()
    return {"kind": adjudicated["verdict"], "to": adjudicated["rules_2023"], **({"reason": reason} if reason else {}),
            **({"source": adjudicated["source"]} if "source" in adjudicated else {})}   # a seeded decision's own


def dispositions(state: pathlib.Path) -> dict[str, dict[str, Any]]:
    """Each mapped 1985 rule's disposition: CARRIES (SAME, upheld: the 2023 verdict carries over), OWN-ROW (DIFFERS or NONE: a
    1985 row of its own), DISPUTED (SAME, refuted or unsure: a fleet adjudicator decides), UNVERIFIED (SAME, no verify answer
    yet) or OPEN (no map answer yet).

    A rule's `change` is its DECIDED change record against 2023 (kb/Work PB4995): KEPT for CARRIES, and for a DISPUTED or
    OWN-ROW rule the adjudicate pass's answer once the check its kind takes (`checker`: the recheck for KEPT and CHANGED,
    the refuter for REMOVED, kb/Work PB5008) UPHELD it and no other check that answered disputes it. A DISPUTED or OWN-ROW
    rule whose adjudication is OPEN, or that a check disputes, is `contested` (a fleet adjudicator decides it); one whose
    check has not answered yet has neither. Each check's verdict is the row's under the check's name."""
    m = Pass(state, "map")
    verify, adjudicate = answers_by_id(state, "verify"), answers_by_id(state, "adjudicate")
    checks = {n: answers_by_id(state, n) for n in CHECKS}
    kinds = change_kinds().kinds
    answers = m.answers()
    out = {}
    for cid, it in m.items().items():
        a = answers.get(cid, {}).get("answer")
        row: dict[str, Any] = {"citation": it["rule"]["citation"], "map": a and a["verdict"],
                               "rules_2023": a["rules_2023"] if a else [], "verify": None}
        if a is None:
            row["disposition"] = OPEN
        elif a["verdict"] != SAME:
            row["disposition"] = OWN_ROW
        else:
            got = verify.get(it["id"])
            row["verify"] = got and got["verdict"]
            row["disposition"] = UNVERIFIED if got is None else CARRIES if got["verdict"] == "UPHELD" else DISPUTED
        if row["disposition"] == CARRIES:
            row["change"] = {"kind": change_kinds().chained.name, "to": row["rules_2023"]}   # the same requirement: no reason
        elif (adj := adjudicate.get(it["id"])) is not None:
            row["adjudicate"] = adj["verdict"]
            said = {n: got for n, by_id in checks.items() if (got := by_id.get(it["id"])) is not None}
            row.update({n: a["verdict"] for n, a in said.items()})
            upheld = {n: upholds(n, adj, a) for n, a in said.items()}
            kind = kinds.get(adj["verdict"])
            if kind is None or not all(upheld.values()):      # OPEN, or a check disputes it
                row["contested"] = True
            elif upheld.get(checker(kind)):
                row["change"] = change_of(adj)
        out[it["id"]] = row
    return out


def change_records(disp: dict[str, dict[str, Any]], source: str,
                   held: dict[str, dict[str, Any]] | None = None) -> list[dict[str, Any]]:
    """Every DECIDED change record of the run (each rule's `change`: the chained kind, KEPT, for a CARRIES rule, and the
    adjudicate pass's decision once its check upheld it) as record_verdicts records of the 1985 edition's change record
    (the schema's `next`): into the 2023 rules it names, its reason only when it states one (one stored form,
    lineage.next_errors), the run as its source, or a seeded decision's own (kb/Work PB4993, PB4995;
    DESIGN-spec-conformance-review.md section 10.2). A contested or unadjudicated rule is the fleet's, and not in it; nor
    is a rule whose record in the store (`held`, by rule id) another source decided: a fleet adjudication outranks the
    audit's detection, so re-running the batch never reverts one (kb/Work PB5008)."""
    ed = edition(1985)
    held = held or {}
    return [{"rule-id": rid, "next": {"kind": c["kind"], "edition": ed.next_held, "to": c["to"], "bridge": ed.bridged,
                                      **({"reason": c["reason"]} if c.get("reason") else {}), "source": own}}
            for rid, d in sorted(disp.items())
            if (c := d.get("change")) and (own := c.get("source", source))
            and (held.get(rid, {}).get("next") or {}).get("source", own) == own]


def final_dispositions(state: pathlib.Path) -> dict[str, dict[str, Any]]:
    """`dispositions`, refused while a rule has none yet: the drafts and the adjudication are planned from final ones."""
    disp = dispositions(state)
    if waiting := [i for i, d in disp.items() if d["disposition"] in (OPEN, UNVERIFIED)]:
        raise SystemExit(f"{TOOL}: {len(waiting)} rule(s) have no final disposition yet; finish the map and verify passes")
    return disp


def plan_drafts(state: pathlib.Path) -> list[dict[str, Any]]:
    """Each rule that may hold a verdict of its own at its edition: an OWN-ROW rule, and a rule any recording pass's
    answer (the adjudication's, the recheck's) reads as anything but the chained kind: CHANGED, REMOVED, or OPEN, which
    the fleet decides (a DISPUTED SAME can be any; kb/Work PB4995). Read from the ANSWERS, never from the upheld
    `change`: answers are only ever added, so the set only grows, and the plan planned again after the adjudication, the
    recheck or a later check grows by the new ones (`write_plan`) and never loses a planned draft because a check now
    withholds or disputes its decision (review w20261935a R1-1)."""
    disp = final_dispositions(state)
    items = Pass(state, "map").items()
    chained = change_kinds().chained.name
    read = [answers_by_id(state, n) for n in RECORDING]
    return [{"id": it["id"], "rule": it["rule"]} for it in items.values()
            if disp[it["id"]]["disposition"] == OWN_ROW
            or any((got := by_id.get(it["id"])) is not None and got["verdict"] != chained for by_id in read)]


def plan_adjudicate(state: pathlib.Path) -> list[dict[str, Any]]:
    """Each DISPUTED or OWN-ROW rule with its map item's candidates and both readers' answers (the map's, and the verify
    pass's for a SAME)."""
    disp = final_dispositions(state)
    m = Pass(state, "map")
    verify, answers = answers_by_id(state, "verify"), answers_by_id(state, "map")
    return [{**it, "readers": {"map": answers[it["id"]], "verify": verify.get(it["id"])}}
            for it in m.items().values() if disp[it["id"]]["disposition"] in ADJUDICATED]


def checked(state: pathlib.Path, pass_name: str) -> list[tuple[dict[str, Any], dict[str, Any]]]:
    """Each adjudicate item with its decision, for the decisions check `pass_name` takes (`checker`); refused while an
    adjudicate item is unanswered."""
    adj = Pass(state, "adjudicate")
    items, answers = adj.items(), adj.answers()
    if missing := [cid for cid in items if cid not in answers]:
        raise SystemExit(f"{TOOL}: {len(missing)} adjudicate item(s) are unanswered; collect and retry that pass first")
    kinds = change_kinds().kinds
    return [(it, answers[cid]["answer"]) for cid, it in items.items()
            if (kind := kinds.get(answers[cid]["answer"]["verdict"])) is not None and checker(kind) == pass_name]


def plan_refute(state: pathlib.Path) -> list[dict[str, Any]]:
    """Each decision that names no 2023 rule (REMOVED), with the rule and every candidate the adjudicator saw: the refuter
    looks for the candidate that still states the requirement."""
    return [{"id": it["id"], "rule": it["rule"], "clause_candidates": it["clause_candidates"],
             "word_candidates": it["word_candidates"], "adjudication": decided}
            for it, decided in checked(state, "refute")]


def plan_recheck(catalog: pathlib.Path, catalog_2023: pathlib.Path, near_1985: int,
                 state: pathlib.Path) -> list[dict[str, Any]]:
    """Each decision that names 2023 rules (KEPT, CHANGED) with the texts its three questions need (kb/Work PB5008):
    - its 1985 CLAUSE, every rule of it, and the `near_1985` other 1985 rules most like it by wording (`Wording`): another
      1985 rule may already allow what the decision calls new;
    - every rule, of every kind, of each 2023 clause that holds its feature (`feature_clauses`) and of each clause a decided
      rule lies in, then the map's candidates by wording not among them: the record may omit the rule that states the
      requirement (a clause's general rules, a definition restated elsewhere, a general rule over a class of statements).
    The 1985 clause and the 2023 feature clauses are the same for every rule of that 1985 clause: they are the request's
    cached group (`prompt_parts`)."""
    rules, rules_2023 = load_catalogs(catalog, catalog_2023)
    routed, by_subject, wording = routes(state), subject_clauses(rules_2023), Wording(rules)
    by_id_85, by_id_23 = {r["id"]: r for r in rules}, {r["id"]: r for r in rules_2023}
    clause_85: dict[str, list[dict[str, Any]]] = collections.defaultdict(list)
    for r in rules:
        clause_85[clause_key_1985(r)].append(r)
    clause_23: dict[str, list[str]] = collections.defaultdict(list)
    for r in rules_2023:
        clause_23[clause_of(r["section"])].append(r["id"])
    out = []
    for it, decided in checked(state, "recheck"):
        r = by_id_85[it["id"]]
        own_clause = clause_85[clause_key_1985(r)]
        feature = feature_clauses(r, routed, by_subject)
        decided_clauses = [c for c in dict.fromkeys(clause_of(by_id_23[t]["section"]) for t in decided["rules_2023"])
                           if c not in feature]
        shown = {i for c in feature + decided_clauses for i in clause_23[c]}
        out.append({"id": it["id"], "rule": it["rule"],
                    "clause_1985": [dict(quoted_rule(x), id=x["id"]) for x in own_clause],
                    "near_1985": [dict(quoted_rule(by_id_85[i]), id=i)
                                  for i in wording.nearest(r, near_1985, {x["id"] for x in own_clause})],
                    "feature_2023": quoted_2023(by_id_23, (i for c in feature for i in clause_23[c])),
                    "decided_2023": quoted_2023(by_id_23, (i for c in decided_clauses for i in clause_23[c])),
                    "word_candidates": [c for c in it["word_candidates"] if c["id"] not in shown],
                    "adjudication": decided})
    return out


def recheck_rules_2023(item: dict[str, Any]) -> list[dict[str, Any]]:
    """Every 2023 rule a recheck request shows."""
    return item["feature_2023"] + item["decided_2023"] + item["word_candidates"]


def recheck_rules_1985(item: dict[str, Any]) -> list[dict[str, Any]]:
    """Every 1985 rule a recheck request shows besides its own."""
    return [x for x in item["clause_1985"] if x["id"] != item["id"]] + item["near_1985"]


# -- the requests ---------------------------------------------------------------------------------------------------------

def rule_block(rule: dict[str, Any]) -> str:
    level = "" if rule.get("level") is None else f", level {rule['level']}"
    provisional = ("" if rule.get("provisional", "none") == "none" else
                   f"; PROVISIONAL ({rule['provisional']}): the transcription's reconstruction of a printed page the scan "
                   f"lacks, not the standard's words")
    return (f"THE 1985 RULE ({rule['citation']}, {rule.get('module') or 'no module'} module{level}; "
            f"subject: {rule.get('subject') or '-'}{provisional}):\n{rule['text']}")


def candidate_block(cands: list[dict[str, Any]]) -> str:
    return "\n\n".join(f"[{c['id']}] (§2023:{c['section']}, {c['kind']}; subject: {c['subject'] or '-'}):\n{c['text']}"
                       for c in cands)


def rules_1985_block(rules: list[dict[str, Any]]) -> str:
    return "\n\n".join(f"[{r['id']}] ({r['citation']}; subject: {r.get('subject') or '-'}):\n{r['text']}" for r in rules)


def prompt_parts(pass_name: str, item: dict[str, Any]) -> tuple[str, str, str]:
    """A request's text in its three cache layers (Anthropic's notice of 2026-11-08), most shared first:
    (what every request of the pass shares: its cached `system`; what a GROUP of requests shares: a cached first user
    block, or "" where the pass has no group; what is the item's own). The route pass's every request shares the list of
    every 2023 clause; a map request shares its clause's candidates with the other rules of that clause and kind."""
    shared, group = PROMPTS[pass_name], ""
    if pass_name in WITH_CANDIDATES:
        group = (f"THE 2023 CANDIDATES FROM THE CLAUSES THAT HOLD THE 1985 RULE'S FEATURE:\n"
                 f"{candidate_block(item['clause_candidates']) or '(none)'}")
        own = (f"{rule_block(item['rule'])}\n\nTHE 2023 CANDIDATES BY WORDING:\n"
               f"{candidate_block(item['word_candidates']) or '(none)'}")
        if pass_name == "adjudicate":
            own += "".join(f"\n\nTHE {name.upper()} READER'S ANSWER:\n{json.dumps(said, ensure_ascii=False)}"
                           for name, said in item["readers"].items() if said)
        elif pass_name == "refute":
            own += f"\n\nTHE ADJUDICATOR'S DECISION:\n{json.dumps(item['adjudication'], ensure_ascii=False)}"
    elif pass_name == "recheck":
        group = (f"THE WHOLE 1985 CLAUSE:\n{rules_1985_block(item['clause_1985'])}\n\n"
                 f"EVERY RULE OF THE 2023 CLAUSES THAT HOLD THE 1985 RULE'S FEATURE:\n"
                 f"{candidate_block(item['feature_2023']) or '(none)'}")
        own = (f"[{item['id']}] {rule_block(item['rule'])}\n\n"
               f"THE OTHER 1985 RULES MOST LIKE IT BY WORDING:\n{rules_1985_block(item['near_1985']) or '(none)'}\n\n"
               f"EVERY RULE OF THE OTHER 2023 CLAUSES THE DECISION NAMES:\n{candidate_block(item['decided_2023']) or '(none)'}"
               f"\n\nTHE 2023 RULES MOST LIKE IT BY WORDING:\n{candidate_block(item['word_candidates']) or '(none)'}\n\n"
               f"THE ADJUDICATOR'S DECISION:\n{json.dumps(item['adjudication'], ensure_ascii=False)}")
    elif pass_name == "verify":
        own = f"{rule_block(item['rule'])}\n\nTHE 2023 RULES JUDGED SAME:\n{candidate_block(item['candidates']) or '(none)'}"
    elif pass_name == "misprints":
        own = (f"{rule_block(item['rule'])}\n\nTHE 2023 RULES AN EARLIER READER PAIRED WITH IT:\n"
               f"{candidate_block(item['counterparts']) or '(none: read the 1985 rule for its own sense)'}")
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


def shared_groups(pass_name: str, items: Iterable[dict[str, Any]]) -> set[str]:
    """The group blocks (`prompt_parts`' middle layer) at least CACHE_MIN_SHARERS of the pass's items share: the ones
    worth caching. A clause whose rules are fewer than that sends its block uncached (review w20261922c R1-3)."""
    counts = collections.Counter(prompt_parts(pass_name, it)[1] for it in items)
    return {g for g, n in counts.items() if g and n >= CACHE_MIN_SHARERS}


def request(pass_name: str, cid: str, item: dict[str, Any], model: str, shapes: list[re.Pattern[str]],
            cached_groups: set[str]) -> dict[str, Any]:
    shared, group, own = prompt_parts(pass_name, item)
    if any(p.search(part) for p in shapes for part in (shared, group, own)):
        raise SystemExit(f"{TOOL}: the {pass_name} request for {item['id']} carries a per-copy stamp shape; nothing submitted")
    return {"custom_id": cid, "params": {"model": model, "max_tokens": MAX_OUTPUT_TOKENS[pass_name],
                                         "output_config": {"effort": EFFORT[pass_name]},
                                         "system": cached_system(shared),
                                         "messages": [{"role": "user", "content": (
                                             [cached_text(group) if group in cached_groups else {"type": "text", "text": group}]
                                             * bool(group) + [{"type": "text", "text": own}])}]}}


# -- the answers ----------------------------------------------------------------------------------------------------------

def parse_answer(pass_name: str, item: dict[str, Any], text: str) -> tuple[dict[str, Any] | None, str]:
    """The reply as the pass's answer, or None and why it is not one. The answer is the ONE JSON value that starts at the
    reply's first `{`, read to its own end (`raw_decode`), so a fence, prose or a stray brace after it is never part of it:
    cutting at the reply's LAST `}` failed a complete answer that a model followed with an extra `}` (PB4995's drafts). A
    reply that opens a second object after it (a self-correction) holds two answers, and neither is taken (review
    w20261932a R1-1)."""
    body = re.sub(r"\A\s*```[a-z]*\s*|\s*```\s*\Z", "", text)
    try:
        obj, end = json.JSONDecoder().raw_decode(body, body.index("{"))
    except ValueError:
        return None, "unparsed"
    if "{" in body[end:]:
        return None, "two answers"
    if not isinstance(obj, dict):
        return None, "not an object"
    if pass_name == "drafts":
        exp = obj.get("expected")
        ok = (isinstance(obj.get("program"), str) and obj["program"].strip() and isinstance(exp, dict)
              and exp.get("outcome") in ("refused", "output")
              and (exp["outcome"] == "refused" or isinstance(exp.get("output"), list)))
        if not ok:
            return None, "not a draft"
        if exp["outcome"] == "refused":   # what the harness's flagged-test judge needs (run_drafts.py; run.py Run.flagged)
            line, control = exp.get("line"), exp.get("control")
            if not (type(line) is int and 1 <= line <= len(obj["program"].splitlines())):
                return None, "a refused draft names the line of its program that violates the rule"
            if not (isinstance(control, str) and control.strip() and control.strip() != obj["program"].strip()):
                return None, "a refused draft carries its control, the program with that line made conforming"
        return obj, ""
    if obj.get("verdict") not in verdicts(pass_name):
        return None, f"verdict {obj.get('verdict')!r}"
    if pass_name == "route":
        ids = obj.get("clauses_2023")
        if not isinstance(ids, list) or not set(ids) <= {c["id"] for c in item["clauses_2023"]}:
            return None, "clauses_2023 names a clause that is not in the list"
        if (obj["verdict"] == NONE) != (not ids) or len(ids) > MAX_ROUTES:
            return None, f"clauses_2023 must be empty exactly when the verdict is NONE, and name at most {MAX_ROUTES}"
    if pass_name in ("map", *RECORDING):
        ids = obj.get("rules_2023")
        known = {c["id"] for c in (recheck_rules_2023(item) if pass_name == "recheck" else map_candidates(item))}
        if not isinstance(ids, list) or not set(ids) <= known:
            return None, "rules_2023 names a rule that is no candidate"
        kind = change_kinds().kinds.get(obj["verdict"]) if pass_name in RECORDING else None
        names = obj["verdict"] == SAME or obj["verdict"] == DIFFERS or bool(kind and kind.to)
        if names == (not ids):
            return None, "rules_2023 must be empty exactly when the verdict names no counterpart"
        if kind and kind.reason and not str(obj.get("reason") or "").strip():
            return None, f"a {obj['verdict']} decision states its reason"   # the change record's (lineage.next_errors)
    if pass_name == "recheck":
        others, exception = obj.get("rules_1985"), obj.get("exception")
        if not isinstance(others, list) or not set(others) <= {x["id"] for x in recheck_rules_1985(item)}:
            return None, "rules_1985 names a rule that is not another 1985 rule shown"
        if not isinstance(exception, dict):
            return None, "exception is an object, empty unless a refusal became an exception condition"
        kind = change_kinds().kinds.get(obj["verdict"])
        if kind and kind.chained and any(str(v or "").strip() for v in exception.values()):
            return None, f"a refusal that became an exception condition is a change, never {obj['verdict']}"
    if pass_name == "misprints":
        why = misprints_error(item["rule"]["text"], obj)
        if why:
            return None, why
    if pass_name == "claims" and obj["verdict"] != "NOT-ADDRESSED":
        found, why = cited(obj)
        if found is None:
            return None, why
        obj["citation"] = citation.render(found)
    return obj, ""


def spaced(text: str) -> str:
    """`text` as a reply quotes it: no markup tag (the catalog's underlines), no `\\` escape, no level-2 box side, and every
    run of white space one space."""
    return " ".join(re.sub(r"<[^>]+>", "", text).replace("\\", "").replace(spec_pdf.BOX_SIDE, " ").split())


def misprints_error(rule_text: str, obj: dict[str, Any]) -> str:
    """Why a misprints answer is none, or "": its list is empty exactly when CLEAN, and each misprint quotes words the
    1985 rule prints (`printed`, whole words, inside its `context`, itself the rule's own words) and names other words
    (`intended`). A lead whose words the rule lacks points at nothing on the page, so it is a reply to retry."""
    found = obj.get("misprints")
    if not isinstance(found, list) or (obj["verdict"] == "CLEAN") != (not found):
        return "misprints must be a list, empty exactly when the verdict is CLEAN"
    text = spaced(rule_text)
    for m in found:
        if not isinstance(m, dict) or not all(isinstance(m.get(f), str) and m[f].strip()
                                              for f in ("printed", "intended", "context")):
            return "a misprint gives its printed, intended and context words"
        printed, context = spaced(m["printed"]), spaced(m["context"])
        if spaced(m["intended"]) == printed:
            return "a misprint's intended words differ from its printed words"
        if context not in text or not re.search(rf"(?<![\w-]){re.escape(printed)}(?![\w-])", context):
            return "a misprint's context is the rule's own words, and holds its printed words whole"
    return ""


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


def withdraw(p: Pass) -> list[str]:
    """Withdraw each stored answer that the pass's answer shape (`parse_answer`) now refuses, and return their custom ids.
    An answer stored before the shape asked for more (a refused draft's violating line and control, review w20261931a
    R1-1) is one no reader that trusts the shape can judge. Its file moves to `raw/` as `<custom id>.withdrawn[-n].json`,
    the record of what was answered, and its item is unanswered again, so `retry` asks for it under the current prompt
    (kb/Work PB4995). The shape stays the one rule of what an answer is: no reader carries an arm for an older one."""
    items = p.items()
    gone = []
    for cid, stored in p.answers().items():
        if parse_answer(p.name, items[cid], json.dumps(stored["answer"], ensure_ascii=False))[0] is not None:
            continue
        p.raw_dir.mkdir(parents=True, exist_ok=True)
        kept, n = p.raw_dir / f"{cid}.withdrawn.json", 1
        while kept.exists():
            n += 1
            kept = p.raw_dir / f"{cid}.withdrawn-{n}.json"
        p.answer_file(cid).replace(kept)
        gone.append(cid)
    return gone


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
    # Priced as the batch caches it (review w20261922c R1-4): a cached layer is written by the first request of the
    # sample that sends it and read by the rest; an uncached one is input every time.
    groups, seen, total = shared_groups(p.name, items.values()), set(), 0.0
    for c in chosen:
        shared, group, own = prompt_parts(p.name, items[c])
        plain, write, read = len(own), 0, 0
        for part, cached in ((shared, True), (group, group in groups)):
            if not cached:
                plain += len(part)
            elif part in seen:
                read += len(part)
            else:
                write += len(part)
                seen.add(part)
        total += usd(st["model"], int(plain / CHARS_PER_TOKEN), EST_OUTPUT_TOKENS[p.name],
                     int(write / CHARS_PER_TOKEN), int(read / CHARS_PER_TOKEN))
    return total / len(chosen), "estimated before a canary"


def guard(p: Pass, st: dict[str, Any], todo: list[str], max_usd: float) -> None:
    per, how = per_request_usd(p, st, todo)
    spent = p.batches.cost_so_far(st)
    in_flight = len(p.batches.in_flight(st))
    estimate = spent + per * (in_flight + len(todo))
    print(f"{p.name}: {len(todo)} request(s) to submit; ${per:.4f} per request ({how}); spent ${spent:.2f}, in flight "
          f"{in_flight}; total ${estimate:.2f} against --max-usd {max_usd:.2f}", flush=True)
    if estimate > max_usd:
        raise SystemExit(f"{TOOL}: the estimate ${estimate:.2f} exceeds --max-usd {max_usd}; nothing submitted")


def seed(state: pathlib.Path, upstream: pathlib.Path, decisions: pathlib.Path, inherit: list[str]) -> int:
    """Start the run `state` to check a record_verdicts batch's decisions (kb/Work PB4995: the fleet's decisions of the
    contested records, checked again before they stand). It reads the passes `inherit` names from `upstream`, and its
    own adjudicate pass is the upstream items of the decided rules, each ANSWERED by its record (the kind, the 2023 rules,
    the reason, and the record's source): its recheck and refute passes then plan from them as from any adjudication,
    and `results` contests each decision a check disputes."""
    if state.exists() and any(state.iterdir()):
        raise SystemExit(f"{TOOL}: {state} is not empty: a seeded run starts empty")
    if stray := sorted(set(inherit) - set(INHERITABLE)):
        raise SystemExit(f"{TOOL}: a seeded run inherits only {', '.join(INHERITABLE)}, not {', '.join(stray)}")
    if not (upstream / SEEDED).is_dir():
        raise SystemExit(f"{TOOL}: {upstream} has no {SEEDED} pass whose items the decisions answer")
    records = json.loads(decisions.read_text(encoding="utf-8"))["records"]
    up_items = {it["id"]: (cid, it) for cid, it in Pass(upstream, SEEDED).items().items()}
    if missing := [r["rule-id"] for r in records if r["rule-id"] not in up_items]:
        raise SystemExit(f"{TOOL}: {len(missing)} decided rule(s) have no {SEEDED} item in {upstream}: {missing[:5]}")
    if twice := sorted(k for k, n in collections.Counter(r["rule-id"] for r in records).items() if n > 1):
        raise SystemExit(f"{TOOL}: {decisions} decides a rule twice: {twice[:5]}")
    state.mkdir(parents=True, exist_ok=True)       # every refusal is above: nothing is written until all are passed
    write_atomic(state / UPSTREAM, json.dumps({"run": str(upstream.resolve()), "passes": inherit}, indent=1) + "\n")
    p = Pass(state, SEEDED)
    write_plan(p, [up_items[r["rule-id"]][1] for r in records])
    p.answers_dir.mkdir(parents=True, exist_ok=True)
    for r in records:
        nxt = r["next"]
        write_atomic(p.answer_file(up_items[r["rule-id"]][0]), json.dumps(
            {"id": r["rule-id"], "answer": {"verdict": nxt["kind"], "rules_2023": nxt["to"], "differs": {},
                                            "reason": nxt.get("reason", ""), "source": nxt["source"]}},
            indent=1, ensure_ascii=False))
    kinds = collections.Counter(r["next"]["kind"] for r in records)
    print(f"seed: {state} holds {len(records)} decision(s) {dict(sorted(kinds.items()))} from {decisions}; it reads "
          f"{', '.join(inherit)} from {upstream}", flush=True)
    return 0


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
    pl.add_argument("--catalog", default=str(OUT_1985), help="the 1985 rule catalog (route, map, recheck, misprints)")
    pl.add_argument("--catalog-2023", default=str(OUT), help="the 2023 rule catalog (route, map, recheck, misprints)")
    pl.add_argument("--candidates", type=int, default=15,
                    help="the 2023 rules by wording per 1985 rule, after those of its routed clauses (map)")
    pl.add_argument("--near-1985", type=int, default=10,
                    help="the other 1985 rules by wording per decision, beside its own clause's (recheck)")
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
    common(sub.add_parser("withdraw", help="move each stored answer the pass's answer shape now refuses to raw/, so "
                                           "retry asks again"), key=False)
    common(sub.add_parser("results", help="write DIR/results.json: each mapped rule's disposition and decided change"),
           key=False, pass_arg=False)
    common(sub.add_parser("misprint-leads", help="write DIR/misprint-leads.json: each misprint the misprints pass "
                                                 "reports, a lead for a reader of the rendered page"),
           key=False, pass_arg=False)
    sd = sub.add_parser("seed", help="start a run whose adjudicate pass holds a record_verdicts batch's decisions, to check "
                                     "them with its own recheck and refute passes")
    common(sd, key=False, pass_arg=False)
    sd.add_argument("--upstream", required=True, help="the run whose adjudicate items the decisions answer")
    sd.add_argument("--decisions", required=True, help="the record_verdicts batch file of the decisions to check")
    sd.add_argument("--inherit", default="route,map,verify",
                    help="the passes the new run reads from the upstream run, comma-separated (default: route,map,verify)")
    cb = sub.add_parser("change-batch", help="write every decided change record as a record_verdicts batch")
    common(cb, key=False, pass_arg=False)
    cb.add_argument("--out", required=True, help="the batch file to write")
    cb.add_argument("--source", help="each record's next.source (default: audit-1985/<the state directory's name>)")
    cb.add_argument("--repo", default=str(spec_pdf.REPO),
                    help="the repository whose 1985 store's adjudications outrank the run's records")
    return ap


def print_status(p: Pass, st: dict[str, Any]) -> None:
    items, answers = p.items(), p.answers()
    tally = collections.Counter(a["answer"].get("verdict", "draft") for a in answers.values())
    bad = sum(len(b.get("bad", [])) for b in st["batches"])
    print(f"{p.name}: items {len(items)}; answered {len(answers)}; in flight {len(p.batches.in_flight(st))}; bad replies {bad}; "
          f"cost so far ${p.batches.cost_so_far(st):.2f}; verdicts {dict(sorted(tally.items()))}", flush=True)
    if st.get("batches"):
        print(p.batches.billing_line(st), flush=True)


def main(argv: list[str] | None = None, client_factory: Callable[[str], Any] = make_client) -> int:
    a = build_parser().parse_args(argv)
    state = pathlib.Path(a.state)
    if a.cmd == "results":
        disp = dispositions(state)
        write_atomic(state / "results.json", json.dumps(disp, indent=1, ensure_ascii=False))
        decided = collections.Counter(d["change"]["kind"] for d in disp.values() if "change" in d)
        print(f"results: {dict(sorted(collections.Counter(d['disposition'] for d in disp.values()).items()))}; decided "
              f"{dict(sorted(decided.items()))}; contested {sum(1 for d in disp.values() if d.get('contested'))} "
              f"-> {state / 'results.json'}", flush=True)
        return 0
    if a.cmd == "change-batch":
        held = read_store(inventory_dir(edition(1985), pathlib.Path(a.repo)))
        records = change_records(dispositions(state), a.source or f"audit-1985/{state.name}", held)
        out = pathlib.Path(a.out)
        out.parent.mkdir(parents=True, exist_ok=True)
        write_atomic(out, json.dumps({"batch": f"lineage-change:{state.name}", "records": records}, indent=1,
                                     ensure_ascii=False) + "\n")
        kinds = collections.Counter(r["next"]["kind"] for r in records)
        print(f"change-batch: {len(records)} change record(s) {dict(sorted(kinds.items()))} -> {out}\n"
              f"  validate: python scripts/spec/record_verdicts.py --edition 1985 --dry-run {out}", flush=True)
        return 0
    if a.cmd == "misprint-leads":
        leads = misprint_leads(state)
        write_atomic(state / "misprint-leads.json", json.dumps(leads, indent=1, ensure_ascii=False))
        print(f"misprint-leads: {len(leads)} lead(s) in {len({x['rule-id'] for x in leads})} rule(s), "
              f"{sum(1 for x in leads if not x['paired'])} read without a 2023 counterpart -> "
              f"{state / 'misprint-leads.json'}", flush=True)
        return 0
    if a.cmd == "seed":
        return seed(state, pathlib.Path(a.upstream), pathlib.Path(a.decisions), a.inherit.split(","))
    p = Pass(state, a.pass_name)
    if p.inherited and a.cmd != "status":
        raise SystemExit(f"{TOOL}: {state} reads its {p.name} pass from {p.dir.parent} ({UPSTREAM}): {a.cmd} it there")
    if p.seeded and a.cmd != "status":
        raise SystemExit(f"{TOOL}: {state}'s {p.name} pass holds the decisions `seed` recorded, which no model answers")
    if a.cmd == "plan":
        if a.pass_name == "claims" and not a.claims:
            raise SystemExit(f"{TOOL}: plan --pass claims needs --claims")
        planners: dict[str, Callable[[], list[dict[str, Any]]]] = {
            "route": lambda: plan_route(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023)),
            "map": lambda: plan_map(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023), a.candidates, state),
            "verify": lambda: plan_verify(state),
            "claims": lambda: plan_claims(pathlib.Path(a.claims)),
            "drafts": lambda: plan_drafts(state),
            "adjudicate": lambda: plan_adjudicate(state),
            "refute": lambda: plan_refute(state),
            "recheck": lambda: plan_recheck(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023), a.near_1985, state),
            "misprints": lambda: plan_misprints(pathlib.Path(a.catalog), pathlib.Path(a.catalog_2023), state),
        }
        return write_plan(p, planners[a.pass_name]())
    if a.cmd == "withdraw":
        gone = withdraw(p)
        print(f"{p.name}: withdrew {len(gone)} answer(s) the answer shape refuses; {len(p.unanswered())} item(s) "
              f"unanswered (retry asks for them)", flush=True)
        return 0
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
        groups = shared_groups(p.name, items.values())
        reqs = [request(p.name, cid, items[cid], st["model"], shapes, groups) for cid in todo]   # every request checked first
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
