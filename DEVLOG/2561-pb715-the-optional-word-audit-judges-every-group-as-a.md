## Entry 2561 — 2026-10-25 12:58 PDT — PB715: the optional-word audit judges every group as a spelling of its own, and fails on a construct it cannot anchor

kb/Work PB715 (wave 20261501 C). `scripts/spec/audit_grammar_optional_words.py` finds grammar rules that demand a
word the printed general format leaves un-underlined (§8.3.2.4.3, "may be specified at the user's option with no
effect on the semantics of the format"). It had two blind spots.

**Re-probe.** On the old code the note's fixture `CLASS cobolWord IS? classValueSet (FOR (ALPHANUMERIC | NATIONAL))?
(IN cobolWord)?` produced the demand set `{CLASS}`. `scan_alternative` had marked every token of an optional group
omittable, so FOR was never a candidate. The six siblings PB695 found by hand are already relaxed on this tree, so
the full run printed 6 candidates, none of them in that class. Measuring the second blindness (from the
registrar's extension) found 20 anchored formats with optional words that anchored no rule and still ran clean.
Ten of them have a parser-token keyword: USE, USAGE, ORGANIZATION, SIGN, PICTURE, FORMAT, COLLATING, FILE STATUS,
INTERFACE-ID and METHOD-ID. Each had its own cause. USE's DEBUGGING arm disqualified the whole rule. USAGE,
ORGANIZATION and SIGN write the keyword only inside an optional group. PICTURE's token is PIC. FORMAT is a word
`measure` skips. INTERFACE-ID and METHOD-ID are paragraphs inside a definition rule.

**Fix.** Each rule is parsed into a tree (`parse_body`), and ONE function, `demanded`, judges each alternative and
each `( … )` group at any depth. A word is demanded unless one of three things holds: its alternative is a name
alternative; a sibling spells the same ordered sequence without it; or the unit is omittable and the alternative is
that word alone. The third case replaces the old `optional_only` special case: an optional group and an
optional-only rule are the same thing, and `readInto : INTO dataReference` used as `readInto?` now demands INTO,
which the old code had excused. `examine` changes how constructs are anchored:
- it tests per alternative;
- it finds the keyword's token by spelling as well as by name;
- it anchors a rule that names the keyword only optionally;
- for a paragraph printed inside a definition, it falls back to the words of the enclosing format.
A format with optional words that no rule anchors now fails the run. A keyword that no parser token spells is
counted and named. A new `--self-test` (found by `self_tests.py`, so every gate and CI runs it) tests each arm on
the blind shape and on its repaired twin. Disabling the group judgement or the enclosing anchor turns it red.

**Result.** 322 formats; 101 anchored by name, 2 through an enclosing format, 10 with no parser token (nine
directives and the compilation group), 0 unexamined; 11 candidates. These are the 6 from before (none disappeared)
plus 5 the blindness hid, each confirmed on the rendered page: PAGE `[ LIMIT IS | LIMITS ARE ]` (p468), VALUE
`[ IN alphabet-name-1 ]` (p546) and RECEIVE `CONTINUE AFTER { … SECONDS | … }` (p732). They are grammar defects
outside this note, returned as leads. The comments that described the old blindness as current were rewritten
(CobolData.g4, CobolOO.g4, OptionalWordSubsetDriftTests, DESIGN-frontend-grammar.md).

**Row.** SR-8.3.2.4.2-1 was re-verdicted PARTIAL with current evidence (GAP 41 → 41), and its claim moved to
kb/Work PB2024. That note owns the inverse audit (an underlined word made optional), which is the row's open half.

**Self-review.** Architecture: one demand test at both levels, with no second scanner. Duplication: the old
`alternatives`/`scan_alternative`/`required_tokens` were deleted, not wrapped. Performance: the enclosing-format
words are measured only when a construct needs them, and a full run still takes about 6 s.

**Gate.** `build-local -Priority BelowNormal` on this head, leg 1 with its Linux half; the verdict line is in the
wave report `w20261501c-PB715-report.md`.

