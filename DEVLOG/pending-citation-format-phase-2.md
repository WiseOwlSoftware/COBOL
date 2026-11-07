## The tree's citations carry their edition: the one-time rewrite, the hand repairs, and the bare form refused (PB4887 phase 2)

kb/Work PB4887's phase 2, landed alone after the operator lane drained (main d170858339). Phase 1 (entry 3238) gave the
repository one citation format, `§<edition>:<locator>[ <rule>]`, and one parser, `scripts/spec/citation.py`, and kept
the old bare form readable as 2023 through `BARE_MEANS_2023`. This landing rewrites the tree and removes that reading.

**The rewrite.** `rewrite_citations.py --dry-run` first learned to LIST the 2023 clause numbers it leaves bare beside a
document's name (PB4887 new defect 2: a count alone had hidden review R1-1's genuine citations); that run listed 1,415,
each a design document's own section on inspection. `--apply` then rewrote 94,513 bare citations of ISO/IEC 1989:2023
in 6,310 files (a second run rewrote 0) and converted 376 free-text citations of another edition by elimination. The
commit holds the rewrite and nothing else.

**The hand repairs.** The 248 items `--apply` listed (a 2023 clause beside the words COBOL-2002 or 2014, a 1985
paragraph several sections have) were each read against the cited clause's 2023 heading. Where the line's topic is the
clause the number names, it is `§2023:` (the repository holds no 2002 or 2014 transcription, so 2023 is the only
edition `cite.py` can check). Where the number is another edition's own: CALL is 14.8.4 in the 2002 standard (2023's
14.8.4 is "External items"), so `ISO/IEC 1989:2002 §2002:14.8.4.1`; IntrinsicBinder's "§15.55 / §15.76" are both 2002's.
The 2009 committee draft is no edition, so its numbers lost the `§`. Two items are a document's own section, which
the rewrite's screen order (other edition before own heading) had sent to hand repair. The sibling sweep found a class
the screen never saw: "ISO 2002 §8.9" names no COBOL-2002 or 1989:2002, so the 2002 standard's own reserved-word list
had become `§2023:8.9`; it is `§2002:8.9` and `§2002:8.10` in 16 files (reserved-words-2002.json transcribes the 2002
text's 8.9).

**The generators.** Their string literals are the text of committed outputs the rewrite changed, so a regeneration
would have restored the bare form: gen-reserved-words.ps1, gen-cobol-words.ps1, gen-constructs.ps1 and gen_annex_bc.py
now emit the edition form, and ReservedWords.Table.cs, ContextSensitiveWords.Table.cs, CobolWords.g4,
CobolLexerWordSet.g.cs, ConstructRegistry.g.cs and CobolNames.Table.cs regenerate byte-equal (reserved-words.json and
context-sensitive-words.json, which the rewrite left out, now match their tables). constructs.json held eight citations
as the JSON escape for the section sign, invisible to a literal match; they are rewritten too.

**The transition removed.** `BARE_MEANS_2023` is gone with every line that read it: `MARK_2023` is `§2023:` alone,
`_WHOLE_BARE` and the self-tests' transition toggles are deleted, `scan` reads a bare `§N` only when asked, and `parse`
refuses one with its reason ("'§13.18.35.4 GR1' carries no edition: write §<edition>:<locator>[ <rule>], the edition
mandatory (§2023:13.18.35.4 ...)"), planted in the self-test. `audit_code_citations.py` BARE is unconditional, and a
mistyped bare number the standard lacks is still a PHANTOM. `rewrite_citations.py` is deleted. The doc audits'
expected texts, the conformance register and every `cite.py --check <clause>` instruction in the briefs and skills now
use the edition form.

**Review leads.** R1-3: `where.py` prints `FAIL: <the parser's reason>` for a malformed or bare citation, as `cite.py`
does. R1-2: `audit_code_citations.py`'s docstring and comments state the logic they explain. R2-1: entry 3238 and the
`citation.py` comment called the old screen's TOTAL (2,658) its genuine part; the whole-name screen keeps 1,400 of them
as document sections, so about 1,258 were genuine citations. The tooling's full-line comments are rewritten; a blind
pass had turned a conformance-suite design's own §3.1-§3.3 and a review design's §5.5 into 2023 definitions, the R1-2
shape, and those were put back. Its docstrings and string literals are left (data its logic reads).

**What the gate found.** The textual rewrite cannot see a citation the compiler COMPOSES: `$"ISO §{Clause} SR{rule}"`
in EcRaiseSite, ChoiceIndicators, AcceptDisplayBinder, IntrinsicBinder, SetBinder, ScreenFacility, ClosedFormatPass,
ClosedFormats and ConditionalCompilationProcessor emitted the bare form into COBOL0710/0717 and their siblings, and
33 cases (suite refusals, the RAISING-context tests, nine drift-test classes that read citations by pattern or compose
the one they expect) went red. Every interpolation is `§2023:{…}` now (`git grep -nP '§\{' -- 'src/*.cs'` is empty) and
the tests' patterns read the edition form; two `TrimStart('§')` would have produced `§2023:2023:`. The parser then
read `§2023:{` and a test's `§2023:(` as malformed citations: an interpolation hole and a regex group after the colon
are templates like `§2023:<clause>`, and the format says so. Every golden, snapshot and version-matrix file the
branch changed (1,964) differs from main only in citation text, checked by normalizing the edition token away (the
register's lead fields are cut at a fixed length, so their tails shift).

**What the branch review found (R1-1).** The rewrite had also turned a repository document's OWN section numbers into
2023 clauses wherever the document-name screen did not know the name: "[canonical: SSOT §14.1]" in STRING_OPS_DESIGN
means docs/DESIGN.md's §14.1 (ONE lvalue model: `Place`), and a reader sent to ISO 14.1 is sent to the wrong text with
a well-formed citation no audit can flag. The screen lacked `SSOT`, a `PLAN-` identifier, "doc" and "scout brief",
could not step over an XML-doc `</c>` or an en-dash range (`§8.1–§8.7`), and the own-heading reader missed a
`### §3.1` heading. All are in the screen now, each with a planted self-test case, and the 39 lines it now reads as a
document's section went back to the bare form. Where the line itself named no document, the name was written in: the
architecture-review campaign's 210 notes and code comments (471 sites citing that design's §8.1-§8.7, §5.1-§5.6 and
§4.1) say `DESIGN-architecture-review §8.3`, kb/Work PB2118 says `design §8.3`, and the hand cases name
DESIGN-external-repository, PLAN-bindtime-gating-migration and DESIGN.md §15.5. The lead R1-2 is fixed too: the 2009
committee draft's numbers in PB3919, PB3920, PB3923 and PB3600 are `clause N`, not an edition's.

**Checks.** audit_code_citations.py --check and --check-all: 0 findings over 12,797 files; audit_doc_citations.py
GREEN; citation.py, cite.py, audit_code_citations.py and doc_audits.py self-tests PASS; doc_audits.py --check GREEN;
verify_publishable.py PUBLISHABLE; generate.py --check GREEN; semgrep verify PASS (no count rose). The gate:
`=== BUILD-LOCAL GATE: GREEN — Conformance 22,463/22,463 · Unit 40,427/40,427 · Characterization 37/37 cases ran
(skipped 0) …` (run 000004-20261107T060203Z).
