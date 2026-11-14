# DESIGN — Full Implementation ↔ Specification Conformance Review (P14 Step-0, spec-first)

> **STATUS: DESIGN — decision-complete methodology + plan.** This is the exhaustive, spec-anchored conformance review
> that is the PHASE-14 definition of DONE (owner decision D13: commercial-quality, 100% CONFORMING to ISO/IEC
> 1989:2023 per §2023:4.2.1 with correct support for 1985/2002/2014). It re-grounds the P14 Step-0 "traceability
> inventory" as **spec-first and exhaustive** — the spec text is the ORACLE, not a differential. Spec-first is the
> only going priority (memory `feedback_spec_is_the_oracle`).

## 0. Why this exists — the lesson that forced it

The historical test strategy is entirely **differential + happy-path**: NIST-85 goldens, the GnuCOBOL compile
accept/reject differential, and our own conformance corpus whose "expected" outputs were largely derived from the
legacy byte-engine. **None of these is a spec oracle**, and a differential is structurally BLIND to a spec-violation
that both implementations share — and many defects were *ported from* the legacy, so the differential and the bug came
from the same place. Two spec-first audits (design-doc `wf_480d50f5`, code `wf_4ce42db6`) found **~44 candidate
conformance bugs** those checks never could. But those audits were **bounded SAMPLING** — 14 behavioral areas, one
agent each, a handful of rules apiece — not exhaustive coverage. 100% conformance (D13) requires an EXHAUSTIVE,
traceable review: every normative rule → its implementation → a verified verdict → a spec-derived test.

## 1. Definition of DONE

Every normative ISO/IEC 1989:2023 rule — and its 1985/2002/2014 applicability — has:
- **(a)** a located implementation (a traceability link into `src/WiseOwl.Cobol.*`), OR a recorded **owner
  documented-non-support decision** (optional modules only, per §2023:4.2.16);
- **(b)** a conformance **verdict VERIFIED against the spec TEXT** (not against the legacy or GnuCOBOL);
- **(c)** a **spec-derived test** that covers it — the expected value COMPUTED FROM THE SPEC, never copied from an
  implementation oracle; **OR, only for a rule that carries no observable obligation, the owner-signed
  DERIVATION of §1.1 below.**

The traceability inventory at **zero unresolved GAP** = P14 DONE (D13). This supersedes differential-based confidence.

### 1.1 The DERIVATION — the (c) alternative for a rule with no observable obligation (owner, `kb/Work/PB386`, 2026-09-03)

⛔ **This is the ONE widening of §1 an agent did not make, and the bounds are the owner's, not a paraphrase.**
Some normative rules impose no obligation a conforming program can observe: the standard declares the result
*undefined* (§2023:4.4 2): *"Situations in which the results of executing a statement are explicitly undefined or
unpredictable are identified in A.2, Undefined language element list. A COBOL run unit that allows these
situations to happen is a conforming run unit"* — `cite.py --check 4.4` OK); or the rule's antecedent cannot be
populated in this implementation; or its consequent cannot be told apart from another rule's. For such a rule
§1(c) is not merely unmet, it is **unmeetable**: any golden written for it would pin an IMPLEMENTATION CHOICE as
though it were the standard's answer, which is exactly what the schema's `spec-derived` clause exists to prevent.
The measured population before the door opened was **eight rows** — every CONFORMS-but-untested row on the tree
except `DOC-A.1-93`, which is a real defect (`kb/Work/PB383`).

A **derivation** is a determination recorded in `docs/CONFORMANCE.md` §8, keyed by the inventory `rule-id`,
naming ONE of five arms and signed by a dated decision. It stands in place of §1(c) **and of nothing else**: (a) and
(b) are paid in full, and a `kind: DOC` row still pays its register anchor and its observability cost.

**Every edition's rows** (`kb/Work/PB5374`). §8 holds rows of every held edition, keyed `DRV-<rule-id>`; a rule-id
of an older edition carries its year (`DRV-1985-GR-XIV.3.6.4-5`), and rule-ids are unique across the catalogs, so
the id names the edition. Each row is checked against its OWN edition's catalog. The checkers read every held
edition's store (`audit_derivations.py --check`, `SpecTraceabilityInventoryDriftTests.EveryDerivation_StandsUnderItsOwnArm`),
and a §8 row that no row of a held edition claims is refused: a derivation on a row of an edition a checker did not
open would otherwise be skipped, never checked.

**Who signs** (`kb/Work/PB5373`, 2026-11-12). A `Signed` cell is a key `<signer>: <date>` of the schema's
`signatures` map, and its signer is a key of `signers`. The owner signed every row through 2026-10 (`owner:`); from
PB5373 on, the session that decided the rows signs them (`claude:`), each such key mapped to the `kind: decision`
note that read the rows (`audit_derivations.py --check` holds both). The owner's signature was the independent read
of an ARGUED arm; for a `claude:` signature that read is a `cobol-refuter` pass that tried to write the witness the
derivation says cannot exist: the row's `Refuted` cell names the refuter's record file, and the checker requires a
record of the row's rule-id in it reading UPHELD. An overturned derivation becomes a test. The MECHANICAL arms
(`undefined-A.2`, `undefined-in-rule`) need no refuter: the checker is the reader. The owner's rows are unchanged.

| arm | what it claims | how it is CHECKED |
|---|---|---|
| `undefined-A.2` | the standard itself lists this rule as explicitly undefined | **MECHANICAL.** The `Names` cell reads `A.2 item <n>`; the checker resolves Annex A.2 item *n*'s own trailing citation (`(14.9.5, CANCEL statement, General rule 11)`) against the rule catalog and requires **this row's `rule-id` to be among the ids it resolves to**. Naming an item that does not cover the row is refused. |
| `unpopulatable-antecedent` | the antecedent is false for every state this implementation can reach | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must STATE the closed set that makes it unreachable (≥20 characters — a dash or an `n/a` is refused). The argument itself has a reader, and the owner's signature is that reader. |
| `indistinguishable-consequent` | the consequent is byte-identical to another rule's, so no conforming program can distinguish them | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must be a `rule-id` that EXISTS in the catalog and is not this row's own. |
| `definitional` | the rule DEFINES a property of the user's program (§2023:4.4's conforming run unit) and obliges the implementation to nothing — owner decision `kb/Work/R43` item 4, extending `PB386` | **REVIEWED ARGUMENT, shape-checked.** The `Names` cell must open `defines` and state the property and whose it is (≥20 characters after the word). A rule whose subject is the implementation or the implementor is never this arm. |
| `undefined-in-rule` | the rule's OWN text declares the result undefined or unpredictable — how an edition with no Annex A.2 says it (the 1985 standard); `kb/Work/PB5373` class 1 | **MECHANICAL.** The `Names` cell is a quotation in double quotes, of four or more words, containing `undefined` or `unpredictable`; the checker requires it to lie inside the row's own catalog text in its edition (each whitespace run compared as one space). A row an Annex A.2 item resolves to is refused here: it keeps `undefined-A.2`, so one fact has one arm. |

**What is REFUSED, by the writer and by the gate.** Each of these is a bound the owner set, encoded rather than
described:

1. a derivation on a row that already carries a **spec-derived `test-ref`** — the row demonstrably HAS an
   observable obligation, so the premise of the whole mechanism is false for it;
2. a derivation on a row whose **verdict does not resolve** — a derivation explains why no test can exist, not
   why a DIVERGES is acceptable;
3. a derivation on a row of a kind that **obliges a register determination which `docs/CONFORMANCE.md` does not
   carry** — the documentation obligation is not yet stated, so there is nothing for the derivation to be about;
4. an `undefined-A.2` arm naming an item that **does not resolve to this row**;
5. a **missing or unknown signature**: the `Signed` cell must be a key of the schema's `signatures` map, one
   key per dated decision that signed rows (`owner: 2026-09-03`, PB386 and the rows signed under it through
   2026-09-25; `owner: 2026-10-08`, the PB1636 batch; `claude: 2026-11-12`, PB5373). A new batch signing adds its key
   in the change that writes its rows, so the cell records WHEN its argument was read;
6. a `derivation` field that is not the anchor **COMPUTED** from the row's own rule-id, or for which
   `docs/CONFORMANCE.md` §8 carries **no row**;
7. an `undefined-in-rule` arm whose quotation is **not in the row's own catalog text**, or on a row an Annex A.2
   item resolves to;
8. an ARGUED arm signed by a signer whose `refuter` is set with **no UPHELD refuter record** of the row.

⚖ **How this coexists with `PB280` Q2 ("no" for anchor-only DOC rows), which it does NOT reverse.** A DOC row
asks the implementor to STATE a choice, so "there is nothing to observe" is unfalsifiable there and cannot close
a row — that answer stands. `DOC-A.1-19` is admitted for the opposite reason: its determination **is** stated,
in §7, and it names a greenfield site; only its WITNESS is impossible, because §2023:14.9.5.4 GR10's branch is
reachable only through the GR2 locate step this runtime never performs, so the effect is byte-identical to
GR7's mandated no-op. **The distinction is encoded by ORDER, not by a special case**: `state_for` charges the
kind's anchor and observability costs FIRST and consults the derivation only where a spec-derived `test-ref`
would have been consulted. A DOC row whose only location is its §7 anchor therefore still computes `GAP`, with
a derivation or without one — refusal 3 above adds the second half, that a DOC row §7 does not document cannot
carry a derivation at all.

## 2. The unit of review — the normative rule

The spec expresses requirements as: **Syntax Rules (SR) + General Rules (GR)** per statement (§2023:14.9.x) and clause
(§11/§12/§13.x); **general formats** (the grammar); **§8 concepts** (classes/categories §2023:8.5, conditions §2023:8.8.4,
reference/subscript/ref-mod §2023:8.4, standard conversions §2023:8.5.1, arithmetic/boolean expressions §2023:8.8); **§15 intrinsic**
definitions; **Annex A** required-documented-behavior; **Annex E** edition deltas; **Annex F** obsolete/archaic. Each
rule = `(rule-id, §-anchor, subject, kind, edition-applicability, text)`. The complete rule set is the coverage
**DENOMINATOR** — the thing the audits never enumerated. The standard also states rules OUTSIDE its numbered lists, as
prose paragraphs and tables; the unit for those is the printed paragraph or table unit, decided in "Phase A, continued"
of section 3 of this document (kb/Work PB479, decision PB5448).

## 3. The three phases

### Phase A — build the spec-rule catalog (the denominator)
Extract EVERY normative rule from `specs/ISO_COBOL_2023.md` into a structured, machine-checkable catalog. The spec's SR/GR
are numbered under "Syntax rules" / "General rules" headings per section, so extraction is tractable (parse by
§-section). **Output:** `docs/rearchitecture/spec-rule-catalog.json` — one entry per rule:
`{ id, section, subject, kind: SR|GR|format|concept|intrinsic|doc-behavior, editions, text }`.
**THE RULE IS THE UNIT** (owner decision 2026-08-08, landed by kb/Work PB55): one entry per TOP-LEVEL numbered rule
of a rule block; every sub-item — lettered, or a numbered list nested under one, at any depth — is its parent rule's
text, addressed by citation path (`9) c) 2.`), and verifying the rule means verifying every sub-item, in that one
row. A block may also print SEVERAL lists at its own margin, each under its own prose lead-in (§2023:14.7.5, §2023:9.3.6):
those are rules in their own right and take sub-list ids `<kind>-<section>-L<sublist>.<n>`. The transcription's
indentation cannot tell a flattened nested list from such a peer list, so the difference is MEASURED on the PDF
(`scripts/spec/measure_sublist_levels.py`, recorded in `scripts/spec/data/sublist-levels.json`) and the extractor's
`--check` fails on a sub-list the record does not hold at level 0. A **completeness
critic** cross-checks the catalog against the spec's TOC + section numbering, because an OMITTED rule is silent false
confidence. Orchestrated: fan out over the §-clauses; loop-until-dry.

**A general-format (FMT) row is a format the clause PRINTS, not a heading that says "General format(s)"** (kb/Work
PB1536): a candidate heading yields a row only when its body carries a diagram or the "Format N" labels §2023:5.2.1 gives a
multi-format construct. A candidate with neither is a PARENT clause about formats (§5.2 itself — skipped and printed)
or a leaf whose diagram the transcription lost (a loud `--check` failure: restore the `<pre>`/`<u>` block from the
rendered page). `CatalogCoverageDriftTests.EveryFmtRow_CarriesADiagramOrAFormatLabel` holds the bar over the written
catalog.

### Phase A, continued — the rows of unnumbered text (kb/Work PB479, decision PB5448, 2026-11-12)

**What the numbered scan cannot see.** The extractor lifts a row from a numbered marker under a rule-block heading (or
a clause the content-keyed manifest admits). The standard also states rules in three other places, and today none
of them has a row unless a clause was admitted by hand (the 18 `shape: paragraph` rows of §2023:9.1.16 to 9.1.18,
kb/Work PB284):

1. **a prose clause** with no rule block. §2023:14.5.1 defines the conditional statement ("Any statement with a
   conditional phrase that is not terminated by its explicit scope terminator is a conditional statement"), §2023:10.4,
   §2023:14.6.2.2 and §2023:8.4.2.1 ("The same name shall not be used both as the name of an external record") are
   such clauses, and the 2023 carriers of fourteen 1985 rules the lineage holds UNDECIDED (kb/Work PB4995);
2. **the margin of a rule block**: a lead-in before the first rule is DROPPED, and a paragraph after a rule is APPENDED
   to that rule's text. Appending is right when the page prints the paragraph at the rule's hanging indent (a
   continuation of the rule) and wrong when it prints it at the block's own margin: §2023:14.8.2.2's sentence "If either
   the formal parameter or the argument is a variable length group" is margin text (measured below) and sits inside the
   row `SR-14.8.2.2-2`, so `cite.py --check` answers "it lies in SR2" and kb/Work PB222's ordinal guess has a second
   form;
3. **a table**: §2023:14.5.1 Table 12 (which statements carry which conditional phrase), §2023:14.6.13.1.6 Table 13
   (the exception names), and the precedence matrices of §2023:13.18.40.6, which the extractor declares known-empty
   ("the normative content IS the two precedence matrices").

**The measurement** (kb/Work PB479 step 1; `python E:/COBOL-coord/scratch/pb479/measure.py`, `analyse.py`,
`pdf_levels.py`, `firstpass.py`, outside the repository at design time, and the design review's probes in
`E:/COBOL-coord/scratch/pb479/review/`; step PB5449 makes the extractor's own coverage report reproduce every figure).
The criterion was fixed before counting:

- *Deontic sentence*: outside a NOTE, an EXAMPLE, a general format, a quotation and a code example, the sentence carries
  a requirement or permission form. 2023 defines its verbal forms in its Introduction ('shall' a requirement, 'may' a
  permission, 'should' a recommendation, 'can' a possibility; front matter, which `cite.py` has no locator for):
  counted are `shall`, `may` and the equivalents `is/are (not) permitted`, `allowed`, `required`. 1985 defines no
  verbal forms; its syntax rules say how a statement "must be written" (§1985:IV.3.1), so `must` is added.
- *Normative region*: 2023 clauses 6 to 16 and the normative Annexes A to C; clauses 1 to 3 are front matter and
  terms, and clauses 4 and 5 are the FRAME (the conformance clause enters by direct citation, section 10.1 of this
  document; §2023:5.3 defines the rule kinds). 1985 Sections IV, VI to XVI and Amendment 1's Section A; Section II
  is explanatory, the counterpart of 2023's informative Annex D ("In this section each of these features is
  discussed", §1985:II.1).
- *In no row*: the sentence's words (after `cite.norm`) lie in no catalog row's text of its clause.
- A table counts by its body rows; a paragraph by its sentences, and its paragraph separately.

| 2023 | sentences | paragraphs | clauses |
|---|---|---|---|
| deontic, normative, in no row (the HITS) | 432 | 344 | 223 |
| indicative (no deontic form), normative, in no row | 2,533 | 1,430 | (737 clauses hold either) |
| of these, Annex B and C character data | | 263 | 5 |
| prose-clause paragraphs (no rule block) | | 1,577 (278 deontic) | 634 |
| of these, numbered items of a prose clause (Annex A.2 66, A.3 46, A.4.2 27, A.4.11 25, ...) | | 226 | |
| lead-in paragraphs of a rule block (dropped) | | 179 (66 deontic) | |
| margin paragraphs glued into a rule's row (PDF level 0) | | 121 (47 deontic) | in 57 rows |
| tables outside rule blocks (prose clauses) | | 46 tables, 721 lines (about 675 body rows) | |
| tables inside rule blocks / inside general formats | | 36 / 10 | |
| frame clauses 4 and 5, deontic in no row (outside by decision) | 63 | | |

| 1985 | sentences | paragraphs | clauses |
|---|---|---|---|
| deontic, normative, in no row (the HITS) | 238 | 186 | 135 |
| indicative, normative, in no row | 1,296 | 685 | (502 clauses hold either) |
| prose paragraphs / lead-in paragraphs | | 767 (153 deontic) / 98 (31 deontic) | 446 |
| Section I, II, III deontic in no row (outside by decision) | 31, 142, 50 | | |

The glued count is a measurement of the printed page: each unnumbered column-0 paragraph that follows a numbered rule
was found on the PDF (`measure_sublist_levels._visual_lines`, the page's own margin, 17.5 pt a level): of 502, 122 print
at level 0 (one is a group label, so 121 paragraphs), 279 at the rule's indent or deeper (true continuations), 16 at
more than one level and 85 not found. The method was calibrated on the 180 lead-ins, which print at the margin by
definition: 164 measure level 0, 14 are not found, 2 are ambiguous, none measures deeper. The 57 rows that hold the
121 are 55 CONFORMS with a `test-ref`, 1 CONFORMS closed on a derivation (`RV-15.4.1-4`, section 1.1 of this
document) and 1 DOCUMENTED-NON-SUPPORT. The 1985 transcription keeps no indentation, so its glued margin text is
unmeasured; `cite.py` reads an unlabelled 1985 paragraph as the rule's above it.

**The samples.** First by sentence (seed 479, 30 hits and 30 non-hits per edition, judged by hand: an obligation
constrains valid source or the effect of compilation or execution; an overview a rule restates, a cross-reference, an
example and a 'may' of possibility are not; `E:/COBOL-coord/scratch/pb479/out/sample-judgments.json`):

| | hits that are obligations (precision) | non-hits that are obligations (missed) |
|---|---|---|
| 2023 | 22 of 30 (73 %); every false hit is a 'may' of possibility or a cross-reference | 15 of 30: 8 lines of Annex B/C character data, 7 general-rule semantics ("The type of this function is numeric", §2023:15.89.1; the precedence of logical operators) |
| 1985 | 23 of 30 (77 %) | 9 of 30 (function types, the initial open mode, the area A rule for level 01) |

Then by PARAGRAPH, the unit this design mints (a paragraph is a row when any of its sentences is, so a sentence rate
understates it). 2023 (the design review, seed 1112, 20 paragraphs in no row, `review/mysample.txt`): 5 are Annex B
character data; of the other 15, 7 are rules, 2 are lead-ins and 6 are not rules. 1985 (seed 1985, 20 paragraphs,
`review/sample1985para.py`, judged in `out/sample-1985-para.txt`): 3 are general-format or example text the measuring
script failed to fence and 1 is an item of the colon paragraph above it, so 16 are units, and 8 of them are rules.
With n of 15 and 16, each paragraph rate is within about 22 to 25 points. The verb criterion flags 6 of the 2023
paragraph sample, all of them rules or lead-ins, and finds 5 of its 7 rules: general rules "define or clarify the
meaning" of an element (§2023:5.3.3) and are mostly written in the indicative, so no textual signal defines the rows.
Of the five 2023 carriers of the fourteen held 1985 rules, §2023:8.4.2.1 and §2023:10.4 carry a deontic form;
§2023:14.5.1's definitions, its Table 12 and §2023:14.5.2 carry none (14.5.1's one hit is a 'may' of possibility
inside its dash list), and §2023:14.6.2.2's active-state rules are indicative (its hit is the permission of recursion
beside them).

**What the tree already shows** (kb/Work PB204's case): the clause of 143 of the 223 2023 hit clauses (304 hit
sentences) is cited `§2023:<clause>` both in `src/` and in a test tree. Of the 1,774 2023 paragraphs in no row, 293 are
quoted (eight consecutive words) by a CONFORMS record with a `test-ref`, but only 19 by a record of a row in their own
clause, and the best such record quotes 90 % or more of the paragraph for only 23 (less than a quarter for 196;
`review/fp_quality.py`): a quotation shows where the code may be, never that the paragraph is witnessed. For 1985, 18
hit clauses are cited in `src/` and a test tree, and 78 of the 871 paragraphs are quoted by a CONFORMS 1985 record
with a `test-ref`. These figures ORDER the adjudication of the new rows (step PB5455) and seed its candidate sites;
they close nothing.

**THE DECISION (kb/Work PB5448).**

1. **THE PARAGRAPH IS THE UNIT**, as the rule is for numbered text (section 3, "THE RULE IS THE UNIT"). A *margin
   paragraph* is a printed paragraph at its clause's margin that is not a numbered rule: a prose clause's paragraphs, a
   rule block's lead-ins, and a paragraph after a rule that the page prints at the block's margin. A sentence is not
   the unit: a sentence-level recogniser would put about one false row in four into the denominator and miss most
   general rules (the samples above), and a sentence of a paragraph is cited, verdicted and tested with its paragraph
   exactly as a sub-item is with its rule. The segmentation, the same for every edition:
   - **Where a paragraph ends.** Blank lines, and page furniture (1985's page markers and running headers; the 2023
     text has none), separate candidate paragraphs. A sentence ends in `.`, `?` or `!`, optionally followed by closing
     brackets or quotes (§2023:8.4.2.1's second paragraph ends "(See 8.4.6, Scope of names.)"; 64 lines of the 2023
     text end ".)"). A candidate CONTINUES the unit before it when that unit does not end a sentence and the candidate
     opens in lowercase or with a continuation word of a closed list (`and`, `or`, `nor`, `but`, `then`, `where`,
     `unless`, `otherwise`): the 2023 text breaks 42 sentences across a plain blank line this way, none across
     furniture, among them §2023:14.9.35.4's "... TO record-name-1" / "or" / "according to the rules ..." and the
     "'PICTURE N(length)'" / "where length is the length of the literal" pairs of §2023:13.15.3 and 13.16.3
     (`review/tails.py`, a lower bound). The 42 are the join's test population, kept as `--self-test` data. A candidate
     after an unterminated unit that opens any other way does not join, and the coverage report lists it for reading
     on the rendered page.
   - **Lists.** A unit ending in a colon owns the dash, lettered or numbered items that follow it, and a unit after the
     list that continues it by the rule above belongs to the colon unit, not to the last item: §2023:14.7.5's "If the
     size error condition exists and a SIZE ERROR phrase is not specified:" and, after its five items, "and processing
     proceeds as specified in 14.6.13.1.3, Fatal exception conditions" are one unit. The printed level decides where
     the measurement exists (the closing tail of §2023:14.7.5 prints at level 0, the lead's level, and the margin-level
     data of point 4 covers every unlabelled paragraph after a list item, numbered or not). When the items are numbered
     rules with rows, the colon unit's words before the list are also each item's `lead` field (outside `text`, so no
     digest moves).
   - **Numbered items of a prose clause** (226 in 2023, mostly Annex A.2, A.3 and A.4) are units keyed by their
     PRINTED number, id `<kind>-<clause>-<n>` (the form `DOC-A.1-<n>` already has): the standard and Annex A.2's own
     resolver (`extract_annex_a2.py`) cite them by that number, and an item owns its lettered sub-items as a rule does.
   - **Not units at all**: a NOTE or EXAMPLE (it stays inside the unit it follows), a general format, and code. Text of
     these kinds that the transcription failed to mark (an unmarked NOTE, a format line outside its fence) is a
     transcription defect: it is repaired as the page prints it (`scripts/render-spec-page.py <page>`) in the landing of
     the edition's mint, never disposed as if it were prose. The three such paragraphs of the 1985 sample were the
     measuring script's own fencing, not the transcription's.
   - **Tables**, by kind, each kind proposed by a shape test on the transcription and confirmed by the table's
     disposition: an *entries* table (each body row states a determination about the subject its first cell names;
     the first column's cells are distinct names: Table 12, Table 13) has one unit per body row; a *matrix* (both
     axes are categories and the cells take a few values such as Yes and No; the first header cell is a corner: Table
     16, the two matrices of §2023:13.18.40.6) has one unit per body row line, its subject against every column; a
     *data* table (values a rule consumes: Table 1's character repertoire) is one unit. A table whose text already lies
     in a row's text, or that a rule's text names ("table 16 ... specifies the validity of the move",
     §2023:14.9.25.3 SR10), is that row's content: the segmentation records it `held-by <rule-id>`, it is never minted
     again, and the audit refuses a table held in two places. Of the 36 tables inside rule blocks, those no row holds
     (the two matrices of §2023:13.18.40.6, a clause with no numbered rule) are units by their kind. **Table 12** is
     an entries table of 50 body rows, one unit per statement (`GR-14.5.1-T12.<r>`, keyed by the statement's name): the
     21 rows that name a conditional phrase and PERFORM's (a scope terminator alone) are expected rows, a cell holding
     two phrases (DELETE, IF, READ, SEARCH, WRITE) staying one row because the statement is the unit; the 28 rows with
     both cells empty (ALLOCATE, CANCEL, CLOSE, ...) are expected to be `restates` of their statement's general-format
     row, checked mechanically (that row holds no conditional phrase and no `END-` terminator) and refuted. The
     fourteen held 1985 records (step PB5454) name the Table 12 rows of their statements and §2023:14.5.1's paragraph
     rows as their 2023 targets.
2. **The ids.** A margin paragraph's row is `<kind>-<clause>-p<n>` (2023, PB284's form, unchanged) and
   `<year>-<kind>-<locator>-p<n>` (1985, 2002), where `n` is the paragraph's position among ALL the clause's margin
   paragraphs, counted from its heading, whether or not each has a row: the number is the text's, so deciding one
   paragraph never moves another's id. Numbered rules are not counted, so a rule inserted or removed moves no `p`. A
   table unit is `<kind>-<clause>-T<t>.<r>` (the printed table number, the body row's position, header rows not
   counted), a table that is one unit is `<kind>-<clause>-T<t>`, and a clause of pure data that is one unit is
   `<kind>-<clause>-C`; a 1985 table, unnumbered, takes its position in its paragraph for `t`. A numbered item of a
   prose clause is `<kind>-<clause>-<n>` (point 1). None of `-p`, `-T`, `-C` can collide with `-<n>` or `-L<m>.<n>`,
   and a prose clause has no numbered rule for an item's `-<n>` to collide with. The kind is SR or GR by §2023:5.3.2 and
   §2023:5.3.3, AR or RV inside an intrinsic function's clause, since "Except for intrinsic functions, rules are
   categorized as syntax rules and general rules" (§2023:5.3.1), and DOC for an Annex A item whose obligation is a
   statement in the implementor's documentation, as A.1's are: the extractor's paragraph-admission check, which refuses
   everything but SR and GR today, takes the kinds from the clause.
3. **Which units are rows is an ADJUDICATION, recorded as data; finding them is MECHANICAL.** The content-keyed
   manifest's `paragraph-blocks` grows from "admitted clauses" into the DISPOSITION of every unit of the normative
   region. It stays keyed by clause (so `audit_catalog_coverage.py`'s reading of its keys as clause labels stays true),
   each clause holding its units; the positional `kinds` map (`9.1.16`'s `{"6": "SR"}`) is removed and the 18 PB284 rows'
   kinds become per-unit dispositions in the same change (rule 4: one key, not two).
   - **The key** of a unit is its clause, the digest of the `cite.norm` form of its WHOLE text (a table unit: its
     table number and the digest of its normalized cells), and, for units of one clause whose digests are equal, their
     order (0 for the first). A prefix key would not do: the first 100 normalized characters collide on 17 keys
     covering 41 margin paragraphs (§2023:14.9.39.4's "the execution of the set statement is unsuccessful and" four
     times), and an edit after the prefix would leave an old disposition in force. The margin-level data of point 4 is
     keyed the same way.
   - **A transcription repair that changes a unit's text** changes its digest: the old disposition then matches no
     unit and the unit has none, and the audit fails on both, naming them side by side, until a person decides again
     (with a fresh refuter record where the disposition needs one). An old disposition is never carried to changed
     text.
   - **The dispositions.** A kind (`SR`, `GR`, `AR`, `RV`, `DOC`): a ROW. `restates` with the rule ids that state the
     same requirement: no row; each id must exist. `not-a-rule` with a class from the closed vocabulary below: no row.
     `pending` until decided: outside the denominator, and counted, so every reader that prints the edition's rule count
     prints the pending count beside it as a lower bound (`inventory_view.lower_bound`, 2002's mechanism, section 10.4
     of this document). There is no other way out of the denominator.
   - **A unit that states or defines anything normative is a ROW** (rule 4: one mechanism per question). If it obliges
     the implementation to nothing observable (it defines a property of the user's program), it closes through the
     `definitional` arm of section 1.1 of this document, signed and refuter-upheld as kb/Work PB5373 requires, never by
     leaving the denominator. "The type of this function is numeric" (§2023:15.62.1) is a row with a test, as the
     function's type is observable.
   - **`not-a-rule` is for text with no normative content of its own**, from a closed vocabulary, each class with a
     mechanical test the audit runs:
     - `lead-in`: the unit ends in a colon (or is a label of at most ten words with no sentence stop), the list or
       table after it completes its sentence, the items carry its words (`lead`) and no tail follows the list (a lead
       with a tail is a unit like any other, point 1);
     - `cross-reference`: every sentence matches the closed pointer grammar ("See <locator>[, <title>].",
       "<subject> is/are specified/described/defined in <locator>[, <title>].") and every locator resolves
       (`cite.py`) to a clause, rule or table whose units have dispositions;
     - `data`: the unit holds no sentence (list or table lines of literal values: characters, code points, names)
       and a row's text names its locator, so the row whose obligation consumes it carries it; data no row names is a
       row itself (`-T<t>` or `-C`).
     An overview a numbered rule states again is `restates`, never `not-a-rule`.
   - **Every `restates` and every `not-a-rule` names an UPHELD refuter record**: a second agent read the unit and tried
     to state the obligation it imposes that the named rules (or nothing) do not; the record carries that attempt, why
     it fails, the refuter, the date and the digest of the text it read. The form is the one kb/Work PB5374 builds for
     the refuter record of an argued derivation (one form for every refuter record a checker reads; whichever of
     PB5374 and PB5449 lands first builds it). The RULE CATALOGS audit refuses a disposition without its record, a
     record that is not UPHELD, and a record whose digest is not the unit's; and it holds, per class, that the number
     of `not-a-rule` dispositions equals the number of UPHELD records keyed to them. The same question one level up,
     the manifests' existing block disposition `not-rules` (a whole numbered list kept out of the denominator with a
     free-text reason: 4 in 2023, 37 in 1985), takes the same record: its entries are refuted in steps PB5450 (2023)
     and PB5452 (1985), and the audit refuses one without it.
   - **Two guards refuse a `not-a-rule`** whose class test passed (a `lead-in` excepted: its words are carried by its
     list's rows). The edition's verbal forms, held beside the manifest as data and widened by the misses the review
     found ('shall', 'may', 1985's 'must', 'is/are (not) permitted/allowed/required', "the type of this function is",
     "the function type is", "is undefined", "is unpredictable", "implementor-defined", "is treated as", "is
     equivalent to"; the 2023 text holds 76 function-type sentences, none in a row today). And the SHAPE guard: the
     opening five normalized words of every sentence of every row of the edition, numbered and paragraph rows alike,
     derived from the catalog at every audit; a `not-a-rule` unit any of whose sentences opens with one of them is
     refused. The shape set grows with every row minted, so once "the type of this function is" is a row in one
     function's clause, a `not-a-rule` on the same shape in another is refused without anyone adding it; a drift test
     plants that case and expects the refusal.
   Why a list and not a recogniser: no textual signal separates the rows (precision 73 to 77 % and a paragraph recall
   near 70 % at best, the samples above); the list is of decisions about measured text, held to the text by its digests
   and the drift checks of point 4, so the NEXT unnumbered obligation is found automatically and only its disposition
   is a person's.
4. **The mechanical half.** The segmentation of point 1 (`prose_paragraphs` generalised to a clause's units, inside
   rule blocks too); the printed level of every unlabelled paragraph after a numbered rule or a list item, MEASURED on
   the PDF and committed as data (`scripts/spec/data/margin-levels-<year>.json`, keyed as in point 3,
   `measure_sublist_levels.py`'s pattern: CI holds the transcription to it without the PDF; a paragraph the
   measurement cannot find is read on the rendered page and recorded with the page); a paragraph at level 0 that has a
   disposition leaves the rule it follows and becomes its own unit (while it is `pending` it stays in that rule's
   text, so the rule's words never leave the denominator undecided); the RULE CATALOGS audit fails on a unit with no
   disposition entry (`pending` is an entry), on a disposition whose key no unit has, on the refusals of point 3, and on
   a table held in two places; and a regeneration that MOVES an adjudicated row's id (a transcription repair that
   splits, joins or re-levels a paragraph, or adds a table row) writes the move batch for `record_verdicts.py` itself,
   a record moving only when its `rule-digest` matches (the renumbering rule of section 10.2 of this document), and the
   audit refuses a catalog whose moves are not applied.
5. **Citation.** `cite.py --check` takes the designators `p<n>` (`§2023:14.8.2.2 p3`), `Table <t> row <r>`
   (`§2023:14.5.1 Table 12 row 2`) and a prose clause's item number; the existing `Table <t>` is the whole table. Their
   extent is the catalog's reading (`rule_book`), as for every rule. A quotation inside a unit that has no row yet
   answers `OK §<clause> (unnumbered prose, pending)` and never names the rule above it: that is kb/Work PB222's
   repair. A glued paragraph stays inside its rule's extent until the 2023 mint carves it (point 7), so the "it lies in
   GR13" and "it lies in SR2" answers disappear in that landing, together with the stored citations that rely on them.
6. **Every held edition, one mechanism.** 2002 runs the ISO extractor unchanged; 1985's reader
   (`rule_catalog_1985.py`) takes the same segmentation, dispositions and level data (a 1985 unit also carries its
   `box`). 1985's margin levels need the 1985 PDF measurement before its rule-block margins can be decided at all.
   A 2002 unit whose words are a 2023 unit's takes the 2023 disposition as its `candidate`, the 2002 manifest's
   existing field; a candidate is decided like any disposition, refuter record included.
7. **Existing ids and chains do not move; a carved row inherits nothing.** The 18 PB284 rows are reproduced byte for
   byte (their clauses have no dash list, no table and no ".)" ending; a drift test holds it). A numbered row from
   which a margin paragraph is carved keeps its id and loses the carved text: the carve is ONE mechanical operation of
   `record_verdicts.py` (`split`), which checks that the old text is the new text plus the carved paragraphs in order.
   - **The carved row is minted OPEN**, with no record. Its record was decided against text that held the paragraph,
     but not FOR it: `GR-14.7.5-L3.5`'s text holds the NOT SIZE ERROR paragraph while its notes and tests are about
     ROUNDED-mode truncation, and `GR-8.8.4.2.1-13`'s carved alphabetic and national-group comparison sentences are
     witnessed by nothing. It closes only by its own adjudication (a reader judges the paragraph against the code and
     against the parent's test and says whether that test witnesses THIS paragraph; a refuter attacks the closing
     verdict, as for any row) and its own `test-ref` (the parent's test only where the adjudication shows it exercises
     the paragraph). A derivation is never copied: a carved row of `RV-15.4.1-4` needs its own argument and its own
     signature under kb/Work PB5373.
   - **The parent keeps its record only through a recheck.** Its remaining text is part of what the record was
     decided against, but its test may have witnessed only the carved text. The split re-stamps the parent's
     `rule-digest` only when the recheck (the three questions of section 10.3 of this document, asked of the remaining
     text) and its refuter uphold the verdict and the `test-ref`, or for `RV-15.4.1-4` the derivation's argument; a
     parent that fails opens and goes to adjudication with the carved rows.
   - **The chains and citations follow the text.** The split re-stamps `next.digests` of the 1985 change records naming
     a parent (67 records name 23 of the 57; none of 2002) unless the record's `reason` quotes the carved words, which
     sends that record to adjudication instead; and every stored citation of a parent whose quotation now lies in a
     carved row (record notes and reasons, code comments, tests, docs: 76 designator citations of the parents at
     design time, `review/parents.py`, and the 1985 reasons such as `1985-GR-VI.6.3.1.1.2-L2.1`'s quotation of
     §2023:8.8.4.2.1 GR13) is rewritten to the carved row's designator in the same landing, so the citation audit
     (`audit_code_citations.py`) stays green. No other existing row's text changes.
8. **The neighbours.** kb/Work PB5148's 2023 half (the §2023:8.4.2.1 uniqueness sentences) is a paragraph row of this
   mechanism; the three 1985 rows of its 1985 half exist in the catalog today, and the one of them with no store
   record, `1985-SR-X.4.6.3-2`, is recorded by step PB5454 among the fourteen held records. kb/Work PB5319 (general
   format rows: a bold `Format n` label uncounted, text cut at 4,000 characters) is not this mechanism, since a general
   format is already a row; it shares only the regeneration, so it lands in the 2023 mint (PB5451) and the FMT digests
   move once. Seven FMT rows' digests move (`FMT-14.9.39.2`'s text is exactly 4,000 characters today), and about 42
   1985 change records name six of them (`FMT-10.6.1` 12, `FMT-12.4.5.1` 17, `FMT-12.3.7.2` 3, `FMT-14.9.12.2` 3,
   `FMT-14.9.28.2` 4, `FMT-14.9.39.2` 3); PB5451 re-stamps their `next.digests`, sending to adjudication each whose
   `reason` quotes the restored tail.

**The consequence, in numbers.** Each edition's denominator is corrected ONCE, in one landing per edition, and every
row minted is OPEN that day: nothing closes at minting. These are estimates from the paragraph samples (95 %
intervals); the exact count is the output of the classification step (PB5450 for 2023), which first draws a small
stratified sample of the tables by kind:

| | rows today | GAP today | rows minted (estimate) | closed at minting | open the day they are minted |
|---|---|---|---|---|---|
| 2023 | 4,214 | 0 | paragraphs about 705 (375 to 1,055: 1,511 non-data paragraphs × 7/15); carved margin paragraphs up to 121 (measured); table rows unmeasured (about 250 to 450; between 0 and about 675 body rows of the prose-clause tables, plus the rule-block tables no row holds); at most 5 data clauses. About 500 to 1,850 in all | 0 | every minted row, about 500 to 1,850, plus any of the 57 parents the recheck opens |
| 1985 | 2,274 | 318 | paragraphs about 350 (195 to 500: 871 × 16/20 × 8/16); tables and glued margin text unmeasured | 0 (a 1985 row closes through its change record, section 10.2 of this document) | 318 plus every minted row: about 670 (515 to 820), plus the unmeasured |
| 2002 | 3,298 | 3,298 | measured when its 89 pending clauses are adjudicated | 0 | every row, as today |

**The order** (kb/Work notes, cluster PB479):

| step | note | what | effect on the denominator |
|---|---|---|---|
| 1 | PB5449 | the mechanism: segmentation, ids, the dispositions schema and its refuter records, level data for 2023, the audit's checks, the move batch, `split` and the recheck, `cite.py` designators and the pending answer, the lower bound | none: every unit `pending`; every catalog's `rules` array byte-identical, only its `outside-denominator` block gaining the pending units; 2023 reads "4,214 rules (a lower bound: N units pending)" |
| 2 | PB5450 | the 2023 dispositions, each `restates` and `not-a-rule` refuted, by clause; the table sample first | none (a branch) |
| 3 | PB5451 | the 2023 mint: dispositions + regeneration + carve + parent rechecks + citation re-pointing + PB5319, ONE landing | the 2023 correction, every new row open |
| 4 | PB5454 | record the fourteen held 1985 records (records-85p's batch, `to` filled) and re-check the round-2 records against their prose carriers | none (1985 records) |
| 5 | PB5455 | adjudicate the open 2023 rows, carved rows included | GAP down |
| 6 | PB5452 | 1985: margin levels on the 1985 PDF, dispositions, mint, ONE landing | the 1985 correction |
| 7 | PB5453 | the 1985 change records of the new 1985 rows (map, verify, adjudicate passes) | 1985 GAP down |
| 8 | PB5456 | 2002: the same, with its pending clauses | the 2002 correction |

Steps 6 and 8 need only step 1, step 7 needs steps 3 and 6, and steps 4 and 5 need only step 3.

**Attacked.** Run on the current `prose_paragraphs` (the code PB284 landed), on the clauses the dispositions will
admit, and once more by an adversarial review of the design (2026-11-12, kb/Work PB479's dated file of that day):

- *A row for a non-obligation*: §2023:14.6.2.2 splits into six paragraphs and the fifth is "— a STOP statement — in a
  called program, ..." alone, the list cut from its lead "An instance of a program remains active until the execution
  of one of the following" (the colon ends a sentence by `SENTENCE_END`); §2023:14.5.1's dash list and its ninth
  paragraph (Table 12's caption with the whole table as text) are the same shape, and the closing tail of
  §2023:14.7.5's size-error list would be a fragment row. Repaired in point 1 (a colon unit owns its list and its
  closing tail; a table is never paragraph text).
- *A missed obligation*: §2023:8.4.2.1 splits into two paragraphs, not three: the second ends "(See 8.4.6, Scope of
  names.)", `SENTENCE_END` does not accept a closing parenthesis after the period, and the third (kb/Work PB5148's
  uniqueness sentences) is joined to it. Repaired in point 1 (a sentence may end in closing brackets). A join keyed on
  page furniture would have had nothing to key on in 2023 and cut the 42 sentences broken across a blank line; the
  join is the textual test of point 1. At the criterion level, §2023:14.5.2 ("The conditional phrases are specified in
  Table 12") and §2023:15.89.1 carry no deontic form, so a completeness check keyed on verbs would never ask about
  them: hence point 3 disposes EVERY unit, and the verbs and shapes only refuse a `not-a-rule`.
- *A cheap way out of the denominator*: a free `not-a-rule` reason such as "definition" would remove a row more cheaply
  than the derivation it competes with, and the 76 function-type sentences carry no verb a guard would see. Repaired in
  point 3 (a normative unit is a row and closes through section 1.1; the closed vocabulary with its tests; the
  refuter record; the shape guard).
- *A cheap way to close*: copying a parent's verdict and test to a carved row, or closing a new row because some
  record quotes a few of its words, would close rows no test witnesses. Repaired in point 7 (a carved row inherits
  nothing; a parent keeps its record only through a recheck) and in "What the tree already shows" (quotations order
  the work; they close nothing).
- *A transcription repair under a row*: a reconciliation that splits a paragraph the transcription had joined, or
  indents a continuation paragraph, renumbers every later `p` in its clause, and a stored record would sit on the wrong
  paragraph: its digest refuses it (kb/Work PB698), the change records naming it go STALE by `next.digests`, and a
  disposition keyed by number would type the wrong paragraph. Repaired in point 3 (dispositions keyed by the digest of
  the whole text; a changed text returns its unit to undecided, loudly) and point 4 (the regeneration writes the move
  batch; a record moves only with its digest).

### Phase B — map + verify (the numerator)
For each rule (batched by statement/clause), an agent: **(1)** reads the rule + general format; **(2)** LOCATES the
implementing code (binder/emitter/runtime) — the traceability link; **(3)** VERIFIES conformance across the applicable
editions and adversarial edge cases; **(4)** assigns a **verdict** — `CONFORMS | DIVERGES | PARTIAL | NOT-IMPLEMENTED
| DOCUMENTED-NON-SUPPORT | NEEDS-OWNER-DECISION`; **(5)** records the covering spec-derived test, or flags
`test-needed`. Results accumulate into the **traceability inventory** (§4), incremental and RESUMABLE (a rule's
verdict persists across sessions). Orchestration: fan out by clause; completeness critics surface un-verdicted rules;
candidate verdicts are agent-surfaced ⇒ **re-verified** before they are trusted.

### Phase C — close the gaps
- **DIVERGES** → the **§24 fix-queue** (`PHASE-13-plan-vs-spec-review.md`): spec-first fix + spec-derived golden.
  (V54–V59 + `CODE-SPEC-AUDIT.md` CA1–CA38 are the FIRST INSTALLMENT of this queue.)
- **NOT-IMPLEMENTED** → implement to spec, OR an **owner** documented-non-support decision (optional modules only).
- **CONFORMS-but-untested** → write the spec-derived golden (so coverage is DURABLE, not merely asserted).
- Inventory at **zero GAP** = DONE.

## 4. The traceability inventory (SSOT) — schema

**An edition's inventory is a VIEW: its catalog's rules joined with its store's adjudications, each rule's state
computed** (owner 2026-11-08: "We must eliminate redundant code and data. The new implementation should be as if we
originally architected it"; kb/Work PB4992). Each fact has one home:

| fact | its one home |
|---|---|
| the rule: id, citation, section or locator, kind, ordinal, subject, module, level, box, text, status (normal, archaic or obsolete, read from the text's own lists) | the edition's catalog (`extract_rule_catalog.py --edition`), regenerated from the text |
| the adjudication: verdict, code-location, test-ref, derivation, notes, and for an older edition the change record `next` (§10.2) | the edition's store, one file per adjudicated rule |
| the text the adjudication was decided against | the store file's `rule-digest`, compared with the catalog's text on every read |
| the state (OK or GAP) | nowhere: computed by the view, never stored |
| the edition band of a rule | derived from the lineage (§10.2); the store keeps only what no held text decides yet |

**The store.** One JSON file per adjudicated rule, `tests/version-matrix/inventory/<rule-id>.json` for 2023 and
`tests/version-matrix/inventory-<edition>/<rule-id>.json` for an older edition (kb/Work PB4148, PB4991), written only
through `scripts/spec/record_verdicts.py` (§8). A rule nobody has adjudicated has no file: the catalog is the
denominator, and an unadjudicated rule is a GAP by the view. It sits beside `constructs/` and the rest of the
machine-read test data rather than under `docs/`, because its reader is the battery, not a person. **Why a file per
row:** the merge queue merges with plain git, so two pull requests that each add a row beside the same neighbour of one
catalogue file conflict there, and the one 4,214-row file was the second cause of landing holds (PB4148). Two landings that close
different rows touch different files and merge with git's plain merge; a conflict means two branches changed the SAME
row. A file is named by its rule id, nothing else lives in the directory, and two names differing only in case are
refused (`RegistryShapeDriftTests`).

**The view, ONE PER LANGUAGE.** `scripts/spec/inventory_view.py` (`inventory(edition)` over a working tree, its root
the repository's or another worktree's, and `inventory_at(rev, edition)` for a commit: every file the state rule reads,
the store, the catalog, the schema, `docs/CONFORMANCE.md`'s registers and the Annex A.2 list, read as that one tree has
it, `inventory_schema.Tree`) and
`tests/_shared/TraceabilityInventory.cs` join the catalog and the store; the state is computed by the one state rule
(`inventory_schema.Schema.state_for`, and for an older edition the chain, §10.2) and, on the C# side, by its twin
(`SpecTraceabilityInventoryDriftTests.DerivedState`), which `EveryRuleState_AgreesWithThePythonView` holds to the Python
view's state of every rule (`inventory_view.py --states`). Every reader reads the view; none opens a store file or a
catalog to rebuild it. The store (`inventory_store.py`) is the files only, and the view is a second module because it
is a second job. The view refuses: a store file of a rule the catalog no longer has, a store file holding a field no
store file holds (a catalog field or a state copied in), and a store file whose `rule-digest` is not the catalog's text
(an adjudication decided against other text, kb/Work PB698; `record_verdicts.py` accepts that one only for a rule its
batch re-adjudicates). The store refuses a stray or misnamed file and two names that differ only in case. The refusals
are said once, in Python: `inventory_view.py --check` is the gate's INVENTORY VIEW audit, in every lane, and no C# test
re-checks them; the C# tests hold the C# view to the Python one. The view's order is the spec's (section or locator,
ordinal, id). The editions are DATA, `inventory-schema.json`'s `editions.of`, read by both languages: each edition's
year, its band spelling and, for a held edition, its catalog, locator, store directory, legacy single file and the
`adjudicated` fields its store holds (beside `store.keys`). No code names an edition's year: adding an edition is an
entry there (`lineage.py --self-test` adds a 2027 and watches the order, the bridges and the store follow).

| store field | meaning |
|---|---|
| `rule-id` | the catalog id; the file's name |
| `rule-digest` | the catalog text the adjudication was decided against (`inventory_view.rule_digest`'s squash) |
| `verdict` | one of the six defined in `inventory-schema.json` — the vocabulary lives THERE, not here, so it cannot drift between this doc and the tools |
| `code-location` | the traceability link: `path` or `path#Symbol` (see the correction below), or `""` if not located |
| `test-ref` | the spec-derived test covering it, in a form that RESOLVES on disk (§8), or `""` |
| `derivation` | §1.1's owner-signed alternative to `test-ref`, for a rule with no observable obligation: the register anchor `docs/CONFORMANCE.md#DRV-<rule-id>`, **COMPUTED from the row's own rule-id**, or `""` |
| `editions` | (2023 only) the band within the editions no held text decides yet: a run from the introducing edition through 2023 among 2002 and 2014. Whether the rule exists at 85 is the lineage's (§10.2): the `85` token leaves this field when the 1985 change records are complete and a check finds the stored and the derived 85 agree (kb/Work PB4996) |
| `next` | (an older edition only) the change record against the next held edition (§10.2) |
| `notes` | the fix-queue item id for a DIVERGES; the owner decision for a non-support |

`scripts/session-probe.ps1` reports each edition's live GAP count from the view — the **burn-down metric** for P14.

> **⛔ DESIGN CORRECTION, superseding the `file:line` this table used to specify.** `code-location` is a **path,
> optionally plus a `#Symbol`** — never a line number. A line number rots on the next edit to the file, so 3,790
> rows of them would hold the battery gate permanently red for reasons having nothing to do with conformance, and
> a gate that cries wolf is a gate nobody reads. A symbol survives refactoring, moves with the code it names, and
> fails only when the thing it points at actually stops existing — which is the one event worth failing on.

> **⚙ THE `code-location` OF A `kind: DOC` ROW (Annex A.1), and why it has a second form.** A DOC row is not a
> behavioural rule: it is an obligation to SPECIFY and to DOCUMENT an implementor-defined element (§2023:4.2.5), and a
> binder symbol alone cannot discharge that — a DETERMINATION in `docs/CONFORMANCE.md` §7 can. So a DOC row's
> `code-location` is **its §7 register anchor, plus every greenfield implementing site**:
> `docs/CONFORMANCE.md#DOC-A.1-<n>; src/WiseOwl.Cobol.…#Symbol[; …]`.
>
> **The anchor is COMPUTED from the row's own rule-id, never chosen** —
> `inventory-schema.json` → `kinds.DOC.anchor-template` expands `{rule-id}`, which is why §7's item key is the
> rule-id `DOC-A.1-<n>` and not the bare item number. Two things follow. A determination filed under a
> neighbouring item cannot be *spelled*, which is the structural answer to `kb/Work/A11` (the §2023:15.3.3.2
> determination sat under item 87, whose obligation is FORMATTED-CURRENT-DATE's accuracy). And a bare number
> could never have been an anchor at all: the gate resolves a `#Symbol` by a word search over the file, so `#7` —
> which five live rows carried, all `state: OK` — matched the digit 7 anywhere in a 790-line document.
> `code-location.anchored-files` now holds each such file to its real anchor spaces (`DOC-A.1-<n>` for §7,
> `4.2.16` for §1, `A.4.<n>` for §5) and forbids citing one bare.
>
> **A DOC row closes exactly as every other row does**: its own anchor, at least one site matching
> `kinds.DOC.implementation-pattern` (`^src/WiseOwl\.Cobol\.` — the same greenfield predicate `audit_annex_a1.py`'s
> source sweep applies), and a spec-derived `test-ref`. ⛔ There is **no policy arm**: a row whose only location
> is its §7 anchor stays a GAP. **The owner answered that question `no` on 2026-09-02 (`kb/Work/PB280` Q2)** —
> §1(a) is not widened — on the ground that the claim such an arm would rest on is unfalsifiable: greenfield
> source cites just **13 of the 222** items in `implementor item N` comments, so for the other 209 "there is
> nothing in the compiler to observe" would be contradicted by nothing *and confirmed by nothing*
> (`feedback_verdict_evidence_invariant`). An item with genuinely nothing to observe is discharged by an
> explicit owner decline per item, or stays open. The §7 row's machine-readable **`Pinned by`** cell names the
> same spec-derived test(s), and `audit_annex_a1.py` holds the two in agreement — the determination and its
> evidence are one artifact, which is the half the computed anchor does not cover.
>
> ⚠ **The anchor is owed by the rows with a determination to point at — and the REGISTER decides which those
> are, not the verdict.** CONFORMS, PARTIAL and DIVERGES each assert something about what §7 says, so each owes
> its own row unconditionally. `DOCUMENTED-NON-SUPPORT` has **two grounds on a DOC row**, and they differ exactly
> here:
>
> | ground | §7 row | anchor | how it closes |
> |---|---|---|---|
> | the conditioning facility is not implemented, so A.1's preamble **withdraws** the item ("the item is not required if the optional or processor-dependent feature is not implemented") | must **not** exist | not owed | its module's **witness** alone (items 84, 85, 173, 86) |
> | the element is **optional and not provided** — `kb/Work/PB280` Q1, owner, 2026-09-02 | **exists**: stating the non-provision *is* the determination | owed, like CONFORMS | anchor + a site + a witness (items 127, 206) |
>
> `kinds.DOC.anchor-exempt-verdicts` therefore names the verdicts that **may** be exempt, and one predicate per
> language (`Schema.anchor_obliged` / `AnchorObliged`) asks the register, over one parser per language
> (`inventory_schema.section7_rows` / `tests/_shared/ConformanceRegister.cs`). `audit_annex_a1.py` applies the
> same test — the DNS selectors' rows *minus the ones §7 documents* (`unreachable_items`, emitted as `--json`'s
> `unreachable`) — so the writer, the drift gate, the register gate and the REMAINING counter answer alike.
> ⛔ The first half was found only by landing the A.1 lane and the derived-verdict lane onto one tree
> (2026-09-02): the A.1 lane wrote the anchor rule where every DOC row was a determination, the derived lane then
> stamped four DOC rows `DOCUMENTED-NON-SUPPORT`, and the writer refused the whole batch. ⛔ The second half was
> wrong from the moment PB280 Q1 was answered, and silently: keyed on the verdict, items 127 and 206 would have
> been excused their own determination, `check_pins` would have stopped holding their witness in agreement with
> §7's `Pinned by` at the moment the witness landed, and the audit would have printed "⊘ CANNOT ARISE" about an
> element this register documents in full — dropping REMAINING by two for no work done.

## 5. Execution model — two concurrent lanes, five row-shape lanes (owner decision PB278, 2026-09-01)

Workflow-orchestrated (owner: max parallelism for disjoint work). **Fan out by §-clause** (disjoint units).
**Incremental accumulation** into the inventory — resumable across sessions; the GAP count is the burn-down. **Loop-
until-dry + completeness critics** so nothing is silently uncovered. Every Phase-C fix lands with its **spec-derived
golden** and a comprehensive gate (tiered testing, `feedback_tiered_gates`). This is many sessions of work; it is
paced by the inventory GAP, not big-banged.

**⚖ Adjudication and fixing are TWO LANES THAT RUN CONCURRENTLY** (`kb/Work/PB278`, replacing the 2026-08-09
"backlog to zero before adjudication" order). The measurement behind it: of 3,636 GAP rows on 2026-09-01, 3,361 had
never been adjudicated while thirteen per-wave review fleets (~1,000 agents, ~4B cache-read tokens in three days)
had moved the GAP by 27 rows. The two lanes never write the same thing: the adjudication lane reads a `git
worktree` pinned at a commit and writes verdict batch files; the fix lane edits the main tree.

### 5.1 The lanes, ordered by rows closed per token

| lane | rows (2026-09-01) | unit of work | what closes a row |
|---|---|---|---|
| **Golden** | CONFORMS rows with an empty `test-ref` (139) + DOCUMENTED-NON-SUPPORT rows without a witness (18) | one writer agent per subject family (~10–14 rows), one refuter per file checking ONLY that each expected value is derivable from the cited rule text | a spec-derived golden (or witness) registered in the manifest; no compiler change |
| **Derived verdict** | rules made unreachable by an A.4 module the owner has declared *Not claimed* (`CONFORMANCE.md` §5) | one `derived-verdicts` selector per module in `inventory-schema.json` (DATA — the PB198 mechanism) + one witness negative test per module | the selector + the witness; the drift test holds the population |
| **Adjudication** | never-adjudicated rules (3,361) | `phase_b_batch.py` batches of ≤20 rules per agent, **dossier-fed** (§5.2), refuters read only the CONFORMS / DNS candidates | a verified verdict; CONFORMS rows then go to the golden lane, defective rows to `kb/Work/` |
| **Fix** | defective rows (PARTIAL / NOT-IMPLEMENTED / DIVERGES) via their `kb/Work` notes | clusters by MECHANISM, not by note; the proven loop (implementer → self-review → fleet → director → lander) with the fleet sized in §5.3 | the fix + golden + re-verdict in one change set; one comprehensive battery per cluster batch |
| **A.1 documentation** | Annex A.1 DOC rows (222; 49 now carry a verdict, 45 §7 determinations exist) | the schema's `kinds.DOC` defines what a DOC row costs (§4); then batches by statement family | a §7 determination as `code-location` + a witness golden as `test-ref` — **except** a DOC row an A.4 module the owner declined has **withdrawn**, which belongs to the **Derived verdict** lane instead and closes on that module's witness alone (A.1's preamble makes the item not required, so there is no determination to file: §4, §8.1; four rows — items 84, 85, 173, 86 — as of 2026-09-02). ⚠ A DOC row the derived lane stamps because the element is **optional and not provided** (PB280 Q1) is NOT that case: it has a determination, so it stays on this lane's evidence rules in full (items 127, 206) |

⛔ **THE ADJUDICATION LANE IS VERDICT-ONLY, AND THE GOLDEN LANE STAYS A SEPARATE LANE** (owner decision,
2026-09-04, `kb/Work/PB468` Q1). It was put to the owner as a bare question — *may the agent that assigns a row's
verdict also author that row's witness, given a mechanically checked pre-run predicted expectation and an
independent blind replicate?* — and the answer was **no: keep the lanes separate.** The proposal it declines is
recorded whole, with the measurement that motivated it, in
`docs/rearchitecture/evidence/PROCESS-REVIEW-2026-09-04.md`; that evidence is frozen and is **the analysis, not
this document's process.**

The reason is **independent derivation of the evidence.** An agent that has located and read the implementing code
has already formed its expectation *from that code*; a witness the same agent then authors is checked against that
one derivation, and a pre-run predicted expectation or a blind replicate checks the *record* of it rather than
supplying a second one. Two lanes each read the standard for themselves and produce **two** derivations, and the
second one demonstrably earns its cost: every adjudication batch so far has overturned some CONFORMS verdicts, and
always downward. §1's definition of DONE is therefore unchanged, and so is the price it implies — the same rule is
read twice, once to verdict it and once to witness it. That cost is accepted as the price of the guarantee; it is
not waste, and it is not to be removed by merging the lanes. The measured consequence, recorded so nobody
rediscovers it as a surprise: an adjudication batch on its own moves the GAP only ~8–11 %, because its CONFORMS
rows wait for the golden lane.

The same decision left **`kb/Work/PB468` open with six further questions** the owner has not yet been asked — the
implementer cap, table tests closing many rows, effort tiers, the already-OK "shall" rows with positive-only
evidence, Report Writer, and extending PB386's derivation arm. Three of those would change §1 or §5 if answered
yes; none of them is in force.

**Batch order in the adjudication lane** alternates a *defect-rich* batch — §14 statement groups, grouped by
mechanism (the I/O statements together, the string statements together) so the notes it produces already cluster —
with a *closure-rich* batch (§13, then §12, §11, §8, §7, the small clauses; §13/§12 adjudicate ~96% CONFORMS where
§14 adjudicates 64%). The lane stays one batch ahead of the fix lane: the GAP moves every battery and the fix lane
never starves.

### 5.2 The dossier — agents verify, they do not search

Phase-B batch 8 agents spent ~120 turns each, most of them discovering what they were to verify. The batch
generator therefore hands each agent, beside the rule texts: the `.g4` rules for the construct, the binder /
emitter / runtime files that cite the clause, every test and golden that cites it, and the `CONFORMANCE.md`
determinations for it. Discovery is a grep; verification is the agent's job.

### 5.3 The fleet is sized to the cluster, and it runs once

A fleet per wave at ~100 agents was the largest token sink relative to burn-down, and five of the last eleven
findings were defects the wave itself introduced. So: the implementer runs the four review lenses as a **self-review
checklist before** any fleet; the fleet is **~40 agents** (four lenses, two refuters per finding) and runs **once
per cluster**; implementer agents receive the apply-ready contract, not a discovery brief, with a turn budget.
Refute stages on anything that CLOSES a row are never dropped (§9).

Two mechanical enablers, each its own change set: the A13 collection split (landed 2026-09-02 as partitions — the
Conformance leg fell 721 → 600 s, not the ~10× predicted, because the box saturates at 24 physical cores; the next
lever is a persistent compile host), and the pinned-worktree convention that lets the adjudication lane read while the
fix lane builds.

**⛔ How every lane is RUN is the owner's standing instruction of 2026-09-02, and its operational SSOT is the
`workstream` skill (`.claude/skills/workstream/SKILL.md`, with the brief and workflow templates):** checkpoint to disk
(WIP commits + `STATUS.md` per worktree; one JSON line per rule per workflow stage), replace a killed agent with a
fresh one reading the checkpoint, keep to the concurrency budget (one lander + ≤3 implementers + one four-wide
read-only chunk), land finished work first with one lander on main at a time, allocate ids centrally. The fleet
sizing above is a consequence of that budget, not a separate rule.

## 6. Governance / relationship to existing artifacts

- **This IS P14 Step-0** (the traceability inventory), re-grounded spec-first + exhaustive — supersedes any
  feature/legacy-checklist framing of Step-0.
- **Divergences → §24** (`PHASE-13-plan-vs-spec-review.md`) — the verified defect/fix-queue SSOT.
- **Editions → the VERSION TEST MATRIX** (`VERSION_CHANGE_REFERENCE.md` + `VERSION_TEST_MATRIX_DESIGN.md`) — the
  (construct × edition) dimension; introduction/continuity/behavior invariants.
- **Seed / first installment:** `DESIGN-SPEC-RECONCILIATION.md` (design-doc audit, 54 conflicts) + `CODE-SPEC-AUDIT.md`
  (code audit, CA1–CA38). The full review is their exhaustive completion.
- **Spec-first discipline:** `feedback_spec_is_the_oracle`, `feedback_spec_is_the_oracle`, `feedback_spec_scopes_not_tests`
  (implement/verify COMPLETELY to the spec — tests verify, never scope).

## 7. Risks + discipline

- **Catalog exhaustiveness** — a missing rule is silent false confidence; the completeness critic + TOC cross-check
  guard it. The denominator's completeness is itself reviewed.
- **Verdicts must be spec-derived**, never differential — that is the entire point; a "matches the legacy" verdict is
  invalid.
- **Candidate verdicts are agent-surfaced** ⇒ VERIFIED before any code change (the verify-then-fix loop already in
  flight for the ~44).
- **Scale** — incremental/resumable inventory; the GAP burn-down is the metric; do NOT attempt it in one pass.
- **Optional-module non-support is an OWNER decision** (D13), recorded in `CONFORMANCE.md`, never an agent's call.

## 8. HOW A VERDICT IS RECORDED — the Phase-B mechanism

An inventory row is a claim, and nothing writes one but this mechanism: the store holds adjudications only (§4),
so the one way a verdict, a witness or a change record enters it is a checked batch. That is not a convenience. **The GAP count is the definition of v1.0** (D13), so a row is a claim about completion, and
an unaudited claim about completion is a self-report. The mechanism below exists to make each of the three claims
in §1 — located, verified, tested — cost what it is supposed to cost.

**Nobody hand-edits the inventory.** A reviewer or a Phase-B agent emits a **batch file** of verdict records;
`scripts/spec/record_verdicts.py` merges it.

```
python scripts/spec/record_verdicts.py --dry-run batch.json     # validate + report the GAP delta, write nothing
python scripts/spec/record_verdicts.py batch.json [more.json]   # merge
dotnet test tests/WiseOwl.Cobol.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

A record sets only the adjudicated fields (plus the two retirement control fields below) and is keyed by `rule-id`:

```json
{"rule-id": "AR-15.7-1", "verdict": "CONFORMS",
 "code-location": "src/WiseOwl.Cobol.Compiler/Binder/Intrinsics/IntrinsicSignatures.cs#Abs",
 "test-ref": "conformance:2023/intrinsic_abs; unit:IntrinsicArgumentRuleTests.Abs_RejectsAlphanumeric",
 "editions": "85,2002,2014,2023", "notes": ""}
```

**Witnesses MERGE; statements are restated (kb/Work PB959).** `code-location` and `test-ref` are sets of
witnesses (`inventory_schema.WITNESS_FIELDS`): the writer UNIONS a record's values with the row's, so a record that
names one new test adds it and a record that omits the field keeps what the row had. `verdict`, `editions`,
`notes` and `derivation` are single statements (`inventory_schema.STATEMENT_FIELDS`): a record that names one
restates it and a record that omits one leaves it as the row has it (kb/Work PB3795, PB3375; until then an omitted
`notes` was written as empty and four landings erased 25 rows' forensic prose with every gate green). A statement
never goes from saying something to saying nothing (`inventory_schema.statement_losses`, asked by the writer and by
the auditor: writing a field empty over a non-empty one is refused, and the `derivation` of a row that can no longer
stand on it, one that now carries a spec-derived `test-ref` or whose verdict no longer resolves, is the one excuse:
`Schema.derivation_superseded`, the first two refusals of `Derivation.refusals`; a record re-adjudicating a
derivation-closed rule to a defect verdict writes `"derivation": ""` to withdraw it), and a record that changes the verdict must restate `notes`, whose prose argues the old
verdict. Three refinements, each measured:

- **A witness leaves a row only by a named, reasoned retirement** — `"retire-witnesses": "<ref>; <ref>"` plus
  `"retire-reason"`. Each ref must be on the row; the writer drops it and appends `retired-witness: <ref>
  (<reason>)` to the row's `notes`, which is the durable mark the gate below reads. Any other loss is refused.
- **A re-adjudication re-sites `code-location`.** A record that CHANGES the verdict and names a code-location
  replaces that field (`inventory_schema.resites`): a PARTIAL row's code-location names the defect site, and a
  union would present it as implementation evidence for the CONFORMS record that closes it. The inventory's own
  history showed this is the dominant legitimate removal (1,105 of 1,714). `test-ref` is never re-sited.
- **A witness-only record** — `{"rule-id": …, "test-ref": …}`, no verdict and no statement field — adds
  witnesses to an adjudicated row and restates nothing, so it is safe to re-apply on a merged tree after another
  batch re-adjudicated the same row.

**The count is gated, not only the resolution.** `SpecTraceabilityInventoryDriftTests` checks that each surviving
reference RESOLVES and deliberately not how many there are, so until PB959 a batch that subtracted valid evidence
left every gate green. `scripts/spec/audit_witness_loss.py --check` (run by `build-local.{sh,ps1}` and
`battery.sh`) compares the inventory with the merge-base with main and is RED on any witness lost without a
retirement mark or a re-site, and on any statement (`notes` prose, `derivation`, `verdict`, `editions`) that went
from non-empty to empty; `--history` replays the file's git history under the merge rule and `--restore-batch`
emits the batch owing back what the old overwrite silently removed (witness-only records for the witnesses, full
records restating the row's own verdict for the erased `notes`).

**A symbol must be in the code, and the prose is read too (kb/Work PB762).** One predicate,
`SpecTraceabilityInventoryDriftTests.SymbolResolves`, decides whether a `#Symbol` still exists, for a `code-location`
and for a `path#Symbol` spelling in a row's `notes`: the symbol must be in the file's CODE (a C# file's tokens, so a
comment, a doc comment and a disabled region do not count but a string literal does; a grammar file's text without its
comments; any other file's whole text), and in a C# file a dotted symbol is a qualified name, so the member must be
DECLARED in that type of the file (a Roslyn walk of its declarations; a member of another type of the same file does not
resolve it, kb/Work PB3278) while a simple name need only be used in the code. The old whole-body word search let `FileRegistry.cs#ReadLockGovern` resolve on three
CONFORMS rows for as long as a comment narrated the method's removal, and left `notes`, the field a reader opens to
learn why a row has its verdict, unread: a rename re-pointed the field the gate checks and five rows then
contradicted themselves, a dated erratum stapled beneath prose that still described the dead method. The prose is
`notes` up to the first `retired-witness:` mark (the schema's `notes.retired-witness-mark`, read by both engines); a
mark is history by construction and may name a symbol that is gone. The writer's half is a shape rule decidable from
the record and the row (`inventory_schema.retired_symbols_left_in_prose`): once a witness is retired its symbol may not
appear in the prose unless the row still cites it at another file, so the history of the change goes in the
retirement's reason and the prose describes the code as it is. What no resolver can read is a BARE identifier in old
prose: it also narrates history and the standard's own words, so a stale bare name is caught only at the next
retirement, and the rows that predate the rule are a measured residue (kb/Work PB762).

**The five parts, and why each is where it is.**

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` | the verdict vocabulary, each verdict's `resolves` flag and required evidence, the legal editions, the `code-location` pattern, the `test-ref` forms, and **`kinds`** — the PER-KIND evidence rules (§4) |
| the Python loader | `scripts/spec/inventory_schema.py` | parsing that schema, deriving `state`, and the ATOMIC inventory write |
| the writer | `scripts/spec/record_verdicts.py` | validating a batch's SHAPE and merging it all-or-nothing — witnesses by union (PB959) |
| the witness-count gate | `scripts/spec/audit_witness_loss.py` | that no row LOSES a witness without a retirement or a re-site (PB959) |
| the battery gate | `tests/WiseOwl.Cobol.Tests.RepoDrift/SpecTraceabilityInventoryDriftTests.cs` | REFERENTIAL integrity, continuously |
| the register audit | `scripts/spec/audit_annex_a1.py`, a gate audit (ANNEX A.1 REGISTER) | that `docs/CONFORMANCE.md` §7 is internally sound and agrees with the inventory |

- **`kinds` is orthogonal to `verdicts`, and that is the point.** `verdicts` says what a VERDICT costs; `kinds`
  says what a ROW OF THIS KIND costs. Before it existed the schema described only one kind of claim — a
  behavioural rule located in a binder and pinned by a golden — so all 222 Annex A.1 rows sat verdict-less while
  §7 already carried 39 determinations, a third of that work invisible to the burn-down. Adding a kind is an
  edit to the JSON; the two evaluators read it, and neither carries a copy.

- **The vocabulary is data, in one file.** Three consumers read it — the row builder, the writer and the C# gate —
  and none carries a copy. Adding a verdict is an edit to the JSON and nothing else. This is CLAUDE.md rule 5
  applied where drift is most likely, and the DA wave's lesson (`feedback_one_rule_one_place`) applied before the
  fact rather than after it.
- **Shape at record time, reference at battery time — with NO overlap.** The writer checks that the rule-id is
  real, the verdict is in the vocabulary, the required fields are filled, the editions are legal and each
  reference parses. It does **not** check that a `code-location` symbol exists or that a `test-ref` names a real
  test. That belongs to the gate, because it has to keep holding as the tree changes underneath a row recorded
  sessions ago, and implementing it in both places would be the duplication this whole design is trying to avoid.
- **All-or-nothing, then an atomic replace.** Every record across every named batch validates before any of them
  merges, and the write goes through a temp file. A half-applied batch is the worst outcome available: the rows
  that DID land look exactly like reviewed work, and nothing afterwards can tell them apart.
- **`state` is recomputed, never believed.** Both the Python side and the gate derive it from the verdict and its
  evidence. Closing a GAP by hand-editing a string fails the battery.
- **⛔ ONLY A SPEC-DERIVED TEST CLOSES A ROW — §1(c), enforced rather than trusted.** Each `test-ref` form carries
  `spec-derived`: `conformance` / `unit` / `conformance-test` yes; **`nist` and `characterization` no**, because a
  NIST CCVS golden and a characterization snapshot are regression NETS (CLAUDE.md rule 1) and a differential is
  structurally blind to a violation both implementations share. An xUnit form *can* be spec-derived, so the form
  alone cannot decide it — `disqualifying-method-patterns` additionally strikes any test whose method name says
  otherwise (`*_MatchesLegacy`, `*_MatchesGnuCobol`, `*_MatchesOracle`). A non-qualifying ref is still worth
  recording as corroboration; it simply cannot be what closes the row.
- **`requires` is NOT "what closes the row".** `requires` is the evidence a RECORD needs to be well-formed —
  evidence for the VERDICT. Closing additionally needs the spec-derived test. Keeping them apart is what makes
  **CONFORMS-but-untested** expressible, the Phase-C category §3 names: the rule verified against the code, no
  test yet pinning it, row still a GAP and one golden from done. Folding the test into `requires` would force
  every such row to be misrecorded as PARTIAL — or closed on whatever test happened to touch the function.

> **⚠ BOTH OF THE ABOVE WERE LEARNED FROM THE FIRST BATCH, NOT DESIGNED IN.** It returned 19 CONFORMS rows
> carrying a covering test, and **7 rested on nothing but a NIST golden or a `*_MatchesLegacy` differential** —
> they would have moved the v1.0 burn-down on exactly the evidence this document's §1(c) rules out. The cause was
> upstream of the agents: the fan-out prompt said which forms RESOLVE and never that a cover must be spec-derived,
> and the schema listed `nist:` with nothing marking it differential. Any future fan-out prompt must state the
> spec-derived requirement explicitly. `session-probe` had the same blind spot in the same direction — it counted
> "test-needed" as an EMPTY `test-ref` rather than as a row that did not close, under-reporting 11 as 4.

### 8.1 The DERIVED verdict — one determination, applied by a predicate rather than by a list

Some verdicts are not adjudicated per row at all. When the owner declines a whole optional module, every rule the
module conditions becomes unreachable *at once*, and there is nothing left to decide about any single one of them.
Recording that by hand produced the failure kb/Work PB198 measured: sixteen rules on one question carrying **four
different verdicts** — NOT-IMPLEMENTED (9), blank (2), NEEDS-OWNER-DECISION (4) and CONFORMS (1).

**Two shapes qualify, and the second one is newer than the mechanism's own description of itself.** Until
2026-09-02 "the decision makes the rules unreachable" was written down as if it were the definition; PB280 Q1 is
the counter-example. Its rows are perfectly **reachable** — an A.1 documentation obligation for an element a user
can write today — and what is common to all of them is the **adjudication**: the owner answered, once, a question
each of up to 30 rows asks in the same words ("an A.1-OPTIONAL element §7 records as *Not provided.* closes as
DOCUMENTED-NON-SUPPORT"). A selector of the second shape wants reading twice as carefully, because its rows are
live surface; the protection is that its arms turn on **what the owner wrote** — the requirement class the
standard states and the determination this repository filed — never on an agent's reading of a rule's text.

So the module's scope is written **once, as DATA**, in `inventory-schema.json` under `derived-verdicts`, and
**both engines read it**: `DerivedSelector` in `scripts/spec/inventory_schema.py` (driven by
`scripts/spec/derive_verdict_batch.py`, which emits the batch) and `DerivedVerdictDriftTests.Select` (which holds
the population every build). Neither carries a selector. ⚠ That claim was *aspirational* from PB198 until
2026-09-02 — no Python consumer of `derived-verdicts` existed, so the PB198 batch was hand-made and the schema's
own `$comment` asserted a second reader that was not there.

**A selector is `arms`: a disjunction of conjunctions.** Within an arm every field present must hold; a rule is
selected when any arm holds and no entry-level `excludes-patterns` matches its text. The fields exist because the
standard scopes a module in more than one way and each way was, at some point, the one a text-only predicate
missed:

| field | the scoping mechanism it reads | the module that needed it |
|---|---|---|
| `sections` | the rule's own CLAUSE — §2023:8.8.1.4 is *titled* for the declined mode and its rules never repeat the phrase | A.4.3, A.4.8, A.4.14, PB198 |
| `pattern` | the rule's TEXT — the facility's functionality stated from inside a mandatory clause | PB198, A.4.14 |
| both, as an AND-gate | a term that is STATEMENT-LOCAL (`file-name-1` means the FD's own subject in §2023:13.4.5.4) or an OPERAND NAME (§2023:12.3.7's `data-name-1` IS the CURSOR operand) | A.4.13, A.4.2 |
| `xref-sections` + `kinds` | an Annex A.1 documentation obligation citing a declined clause, keyed on the same clause numbers as the clause arm | A.4.8, A.4.14 |
| `excludes-kinds`, per arm | a general format (kind FMT) is evidence about a CLAUSE, never one of its formats | A.4.2, A.4.3 |
| `requirement` | the A.1 **requirement class the standard itself states** ("This item is optional."), carried on the catalog row — A.1's preamble writes its licences per class | PB280 Q1 |
| `determination-prefix` | what **`docs/CONFORMANCE.md` §7 says** about the element, read off the register row keyed by the rule's own id — which is exactly the anchor `kinds.DOC.anchor-template` computes | PB280 Q1 |

Two properties belong to the **matcher**, not to any selector, because a convention each author must remember is
a convention one author will forget. Clause prefixes match **component-wise** — §2023:13.18.30 is not inside §2023:13.18.3,
and a raw `StartsWith` had the screen selector taking 543 rules instead of 156, flipping 32 CONFORMS rows to
non-support. And the text is **hyphen-normalised** before matching, because 29 catalog rules spell an operand name
with U+2011 where the rest use ASCII `-`, which silently *under*-selects — the direction that leaves the drift
test green.

⛔ **`determination-prefix` is the first arm field that reads an artifact outside the catalog, and it has to be:
a DOC row's implementation IS its determination.** Three consequences, each load-bearing. The §7 table therefore
has **one parser per language** — `inventory_schema.section7_rows` and `tests/_shared/ConformanceRegister.cs` —
because three readers now need it (the register audit, `anchor_obliged`, this arm). An item with **no** §7 row is
never selected: a missing determination is not a negative one. And the match is on the cell's **opening** words
with markdown emphasis stripped, never `contains` — item 127's determination discusses what is *not* provided
three sentences into its body, and the opening words are the register's answer to A.1's question.

⛔ **THE `requirement` AXIS IS STILL NOT FALSIFIABLE AGAINST TODAY'S DOCUMENT; the `determination` axis
BECAME so on 2026-09-09.** When this landed, all 30 A.1-optional items and all 47 §7 rows agreed — the only
"Not provided." determinations were on optional items, and the only optional items with a determination said
"Not provided." — so a predicate that had dropped `requirement` altogether, or matched *any* determination,
selected the same two rows and left `DerivedVerdictDriftTests` green. ⭐ Re-measured 2026-09-09 the second half
is no longer true: §7 now carries **58** rows, and **5** of A.1's 30 optional items have a §7 row (10, 127,
150, 184, 206) of which only **2** open "Not provided." — item 206 left the set when owner decision Q30
(`kb/Work/PB592`) found that §2023:13.18.60.4 GR12's SIGNED minimum bands are OPEN at the bottom and the element is
PROVIDED. A predicate that matched *any* determination now selects five rows and this file's own live
assertion (every selected row's cell opens "Not provided.") turns RED. The `requirement` axis is still blind
live, because every "Not provided." row remains on an optional item. That is precisely the
shape `feedback_green_gates_arent_evidence` names, so each axis is driven against a **fabricated** catalog and a
**fabricated** register by `python scripts/spec/derive_verdict_batch.py --self-test`, which
`TheSelectorEngine_ProvesEveryAxisCanFail` shells every build and which asserts the **case names**, because a
shrinking self-test still exits 0. The axis that most needs it is `requirement`: a **required** item whose §7 row
said "Not provided." is a conformance *defect*, and stamping it DOCUMENTED-NON-SUPPORT would document
non-conformance as if the owner had licensed it.

**Three properties keep a derived verdict honest, and they are what distinguish it from a shortcut:**

1. **It may never overwrite an adjudication.** A selected row may be blank, or may already carry the selector's
   own verdict (agreement — the reassuring direction). A row carrying CONFORMS, PARTIAL or DIVERGES means the
   determination and a human reading of the code disagree, so `derive_verdict_batch.py` **refuses the whole
   batch** rather than reporting a rewrite the way an adjudication batch legitimately does.
2. **One row, one determination.** Two selectors claiming the same rule would leave it carrying whichever batch
   ran last; the generator refuses it and `NoTwoSelectors_ClaimTheSameRow` asserts it against the data.
3. **It does not close the row.** DOCUMENTED-NON-SUPPORT resolves, but §1(c) still demands a spec-derived test
   proving the documented posture is what *actually happens* — and for a declined module that is the module's
   **witness**: a negative case showing the construct is refused, by name. The 2026-09-02 A.4 landing stamped
   **308 rows** and moved the GAP by **zero**, which is the correct and expected outcome; the PB280 Q1 landing
   stamped **2** and moved it by zero for the same reason (their witness debt was `kb/Work/PB373`, DISCHARGED
   2026-09-09 — item 127 on `conformance:cases/A.1-Implementor-defined-language-element-list/pb373_object_computer_name_inert`, and item 206 by leaving the
   selector altogether when owner decision Q30 / `kb/Work/PB592` found the element IS provided, closing CONFORMS
   on `conformance:cases/A.1-Implementor-defined-language-element-list/pb592_binary_usage_range_bounds`).

**The printed FORMAT band is a catalog field, and a selector arm reads it** (kb/Work PB284). The standard scopes a
rule three ways — by clause, by words, and by the group label printed above it ("FORMAT 2", "SEQUENTIAL FILES",
"ALL FORMATS"). `extract_rule_catalog.py` carries the third as `band` (the label as printed) and `band-formats` (the
format numbers it names) on every SR/GR row printed under one, and no longer lets the label leak into the text
before it. The `formats` arm field selects a rule whose `band-formats` is a non-empty SUBSET of the arm's numbers —
solely, because a rule under "FORMATS 1 AND 2" also holds for format 1 — and is only legal beside `sections`, since a
format number is its clause's own (PRESENT WHEN's FORMAT 2 is not ACCEPT's). The operand arm stays: §2023:14.9.1.3 prints no
band at all and SR-14.9.1.3-5 is scoped by the names `identifier-3` / `identifier-4`; neither mechanism subsumes the
other. The eight rows this reached (five in `validate-only`, three in `screen-handling-only`) are the ones
the old `$format-band-residue` block recorded as unreachable.

**Prose concept clauses are rows too** (kb/Work PB284 part 3, PB479). A clause whose rules are running prose with no
numbered marker (§2023:9.1.16 record locking, §2023:9.1.17, §2023:9.1.18.x commit and rollback) was invisible to the numbered scan
and to every completeness guard. The manifest's `paragraph-blocks` key admits such a clause, with its rule kind and
the reason, and the extractor emits one row per normative paragraph (`GR-9.1.16-p1`, field `shape: paragraph`).
`audit_catalog_coverage.py` holds each admitted clause to the same standard as a numbered one: the rows' text is the
clause's text, sentence for sentence.

**What the gate actually enforces** (eleven facts, and it is proven able to fail — see below): the inventory covers
the catalog exactly, no row contradicts its catalog entry, every verdict is in the vocabulary, every stored `state`
equals the derived one, every verdict carries its required evidence, every edition name is real, every
`code-location` names a file that exists and a symbol still in it, and every `test-ref` resolves —
`conformance:<edition>/<case>` and `nist:`/`characterization:` to a `.cob` on disk, `unit:<Class>.<Method>` and
`conformance-test:<Class>.<Method>` to a method really declared in that class. Plus the two `kinds` facts (§4):
every kind-anchored row whose VERDICT claims a determination carries the anchor its OWN rule-id computes (§4's
`anchor-exempt-verdicts` carve-out), and every fragment on an `anchored-files` path is a legal anchor of that
file's space — an anchored file being cited bare included.

**And the register audit runs every build.** `audit_annex_a1.py --check --json` was wired into nothing until
2026-09-02 — not `battery.sh`, not the CI workflow, not `build-local.*` — so the A.1 register's own correctness
was enforced when a human remembered. it is now a gate audit (`audit_annex_a1.py --check`, kb/Work PB4453), run in every lane, following
the precedent of a Unit class that shells a Python gate, which puts it in the per-commit wave-local
gate, battery phase 1 and both CI unit jobs at once. It asserts its POPULATION (a run that filed nothing is not a
pass), that every §7 row is filed under the item A.1 names, that no `DOC-A.1-<n>` token occurs outside a row key,
that each row's `Pinned by` agrees with its inventory row's spec-derived `test-ref`, that every verdicted DOC row
has a determination, and that the script's own `--self-test` still names every case it drives.

> **⚠ A gate observed only passing is indistinguishable from a gate that inspects nothing**
> (`feedback_green_gates_arent_evidence`), and this one will spend most of its life green over rows nobody has
> touched. Two things answer that. `TheseChecks_ActuallyFail_OnAFabricatedInventory` drives every check above
> against rows built to break it — one defect class at a time, plus positive controls so the failures cannot come
> from a checker that rejects everything — and it runs on every build. And the gate was confirmed against the
> REAL artifact once, by corrupting the inventory (then one file, `traceability-inventory.json`) in place: a row hand-promoted to `OK` and a
> CONFORMS row citing a file and a golden that do not exist turned exactly three tests red, then green again on
> restore.

### 8.2 The DERIVATION — how §1.1's evidence kind is recorded and checked

§1.1 says WHAT a derivation is and what it may not do; this says where the parts live. **The reference shape is
the DOC row's, reused rather than paralleled**: an anchor COMPUTED from the row's own rule-id, carried on the
row, cross-checked against a register PARSED by one reader per language.

| part | file | owns |
|---|---|---|
| the rules, as DATA | `tests/version-matrix/inventory-schema.json` → `derivation` | the record field, the anchor template, the §8 heading, the owner signatures (one per dated owner decision), and the four arms with their `names-pattern` + `check` |
| the register | `docs/CONFORMANCE.md` **§8** | the determinations themselves — one row per closed rule, keyed `DRV-<rule-id>` |
| the register parser | `inventory_schema.register_section` / `tests/_shared/ConformanceRegister.cs` | ONE markdown-table reader per language, now taking the HEADING and stopping at the next `## ` — §7 and §8 are two calls, not two parsers |
| the undefined-element list | `tests/version-matrix/annex-a2-undefined.json`, generated by `scripts/spec/extract_annex_a2.py` | Annex A.2's 66 items, each item's own trailing citation, and the catalog `rule-id`s that citation RESOLVES to |
| the state rule | `Schema.state_for` / `DerivedState` | the same clause, in both engines, in the same change set (`kb/Work/PB315`'s lesson) |
| the writer | `record_verdicts.validate` | the six refusals of §1.1, at record time |
| the gates | `SpecTraceabilityInventoryDriftTests` · `AnnexA2UndefinedListDriftTests` | that every derivation row still holds, and that the A.2 artifact still equals the spec |

- **The A.2 list is a GENERATED ARTIFACT, not a parse at check time, and that is the shape that makes the next
  A.2 row automatic.** The extraction lives in exactly one place — a Python script that reads
  `specs/ISO_COBOL_2023.md` and resolves each item's citation against `spec-rule-catalog.json` — and both engines then
  read the resolved `rule-ids` as data. Parsing the spec markdown in C# as well would be the second parser this
  design spends its whole length avoiding, and re-parsing 66 items per row would put a 1.3 MB read on the path
  `EveryRuleState_AgreesWithThePythonView` walks 4,311 times. `extract_annex_a2.py --check` regenerates and diffs, so
  the artifact cannot go stale silently; `AnnexA2UndefinedListDriftTests` runs it every build, the
  gate-audit precedent of `audit_annex_a1.py` (kb/Work PB4453).
- **Adding an A.2-arm row costs one register row.** The item→rule-id resolution is mechanical, so the next rule
  the standard declares undefined needs no edit to any list — only the determination and its `A.2 item <n>`.
- **⛔ §7's `DOC-A.1-<n>` token invariant had to learn the second register.** `audit_annex_a1.py` asserts the
  COUNT of each `DOC-A.1-<n>` token in `CONFORMANCE.md` equals the number of §7 rows keyed by it — a check that
  exists because an inventory row resolves its anchor by a word search over the WHOLE file, so a prose mention
  would otherwise read as a filed determination. A §8 row keyed `DRV-DOC-A.1-19` is a second, legitimate
  occurrence. The invariant is therefore stated over BOTH registers (`§7 rows keyed T` + `§8 rows keyed DRV-T`),
  and `check_pins` — which is what actually holds a DOC row's evidence in agreement with §7 — was widened at the
  same time from "closes on a spec-derived test-ref" to "closes", so the guard that matters did not weaken.

## 9. RUNNING A PHASE-B BATCH — the fan-out, and what its prompt MUST carry

A batch is: pick the next contiguous clause block → generate one input file per subject → fan out one agent per
subject → hand each result to an INDEPENDENT agent told to overturn it → merge → gate.

```
python scripts/spec/phase_b_batch.py 15.32-15.44     # writes scratchpad/phase-b/in-<slug>.json, prints the slugs
#   … each file carries the subject's DOSSIER; --no-dossier omits it, --include-adjudicated is NOT for a batch …
#   … run the workflow (one adjudicate agent + one refute agent per slug) …
python scripts/spec/record_verdicts.py --dry-run scratchpad/phase-b/out-*.json
python scripts/spec/record_verdicts.py scratchpad/phase-b/out-*.json
dotnet test tests/WiseOwl.Cobol.Tests.Unit --filter "FullyQualifiedName~SpecTraceabilityInventory"
```

`phase_b_batch.py` excludes rules that already carry a verdict, so a re-run after a partial batch is safe.

**Every input file also carries a DOSSIER — the discovery, pre-computed, so the agent VERIFIES instead of
SEARCHING.** Batch 8 (2026-09-01) cost 574M cache-read tokens over 39 agents for 224 rows, and most of each
agent's ~120 turns went on the same three greps — where is the grammar rule, where is the binder and emitter,
where are the tests — a search with one answer per subject that was being bought once per agent. The dossier
computes it once per subject and pastes it into each of that subject's part files: the ANTLR rules (matched on
the rule name, on the clause its comment block cites, and on the construct keyword), every `src/WiseOwl.Cobol.*` file
citing the subject's clause with the citing lines, the same for `tests/` including the `.cob` goldens, the
`docs/CONFORMANCE.md` determinations, the `kb/Work/` notes whose `spec_refs` or `inventory_rows` fall inside the
subject **with their `status`** — which is how §8 item 8's "do not re-report a landed fix" gets answered without
anybody hand-writing the list it forbids — and the citing `docs/DIAGNOSTICS.md` rows. Nothing is hand-mapped:
the clause set comes from the catalog rows plus their construct root, and the keyword from the catalog's own
`subject` field, because a hand-kept construct→file table would be a sixth work register (CLAUDE.md rule 8) whose
staleness would be invisible — a dossier missing an entry looks exactly like a construct that has none.
⛔ **That is also the dossier's limit, and every file repeats it to the agent in `how-to-read`: it is a selector,
so it is evidence about what it RETURNED and none about what it dropped.** An empty list means "this derivation
found none", never "the compiler has none" — 8 of the catalog's 599 subjects are prose with no keyword to search
with at all, and code implementing a rule without citing its clause is invisible to it. A NOT-IMPLEMENTED verdict
still needs the agent's own search. The generator prints a per-subject count table, and a row of zeros is the
signal that that one agent starts cold.

### ⛔ The refute stage is not optional, and the evidence is now three batches deep

Every batch's refute stage overturned verdicts, and **every overturn was a DOWNGRADE** — the pass only ever
removes confidence. It has also found defects the adjudicator looked straight at:

- **PB5** (the quantizer saturating at 9.2 × 10⁹) — the adjudicator had sampled only small receivers, where the
  clamp cannot bite. The refuter read the implementation instead of sampling outputs.
- **PB7** (every zero-argument intrinsic unreachable in the keyword-omitted form) — invisible to output-sampling,
  because the program COMPILES and fails at run time.

So: **an all-CONFORMS report is a red flag, not a good result**, and that sentence belongs in the refuter's prompt.

### What every batch prompt must carry — each line paid for by a defect

1. **The spec is the only oracle**, and the legacy / GnuCOBOL / existing goldens are regression nets.
2. **Validate every citation** with `cite.py --check`. Give the concrete precedents: the fabricated
   "§2023:12.3.7 GR7 k3 … distinct ascending" that came out of *our own design doc*, and the one-level-short shape
   (§2023:13.18.60 where the rule is in §2023:13.18.60.4).
3. **Only a SPEC-DERIVED test closes a row.** Name `nist:`, `characterization:` and any `*MatchesLegacy` method
   as non-qualifying, and say that CONFORMS-but-untested is a legitimate, expected outcome. The first batch
   offered seven rows that would otherwise have closed on a differential.
4. **Editions are never derived from `IntrinsicCatalog`'s own row** — that is deriving the answer from the code
   under review, and it produced a wrong window in batch 1.
5. **CONFORMS costs something**: code not located ⇒ NOT-IMPLEMENTED; an edge unverified ⇒ PARTIAL; a rule with
   two halves where one was verified ⇒ PARTIAL, always.
6. **`DOCUMENTED-NON-SUPPORT` is never an agent's to choose** (owner decision, D13).
7. **`code-location` is `path#Symbol`, never a line number.**
8. **The fixes that have already landed**, so agents do not re-report them — and so they know that a function
   ABSENT from `IntrinsicArgumentRules.Verified` is genuinely unscreened, because that table is deliberately
   partial and grows as this review adjudicates each clause.
   ⛔ **THE LIST IS NOT WRITTEN HERE AND MUST NOT BE.** It read "PB1–PB7" while PB8–PB11 and PB13 had all landed,
   then "PB1–PB11 and PB13" while a dozen more had — a hand-maintained roster inside the very doc that warns
   against one, which is what CLAUDE.md rule 8 forbids. **Generate it when writing the batch prompt:**
   `python scripts/spec/work.py stats`, or `status: landed` over `kb/Work/`. A stale roster is worse than none:
   it tells an agent a fixed defect is open and the batch is spent re-reporting it.
   ⚠ Also tell agents that `Verified` now screens by CLASS SETS, not one class per operand — a figurative
   constant is admissible wherever ANY class it can present is admissible (§2023:8.3.3.6.4 GR4; fix-queue PB48) — so
   "the screen accepts ZERO here AND there" is correct behaviour, not a missing rule.
9. **Read-only, and no `dotnet build`/`dotnet test`** — other work is usually in flight.

### ⚠ Reading the results

- **Wait for the workflow's completion notification.** Output files exist from stage one and are REWRITTEN by
  stage two; reading early published a wrong GAP number once, and because every overturn is a downgrade, an early
  read biases the result *upward*.
- **Read the notification's `<failures>` block before its results.** One run reported "completed" with 5 of 12
  refuters dead on API 529, leaving unrefuted stage-one output in their files. `resumeFromRunId` re-runs only the
  failures.
- **Cluster before triaging.** The finding count is not the defect count: batch 1's 42 open rows were three root
  causes for 31 of them; batch 2's 41 were 34 of a single one.

## 10. THE EDITION PARAMETER — the 1985 audit (kb/Work PB4886)

`--std=1985` is audited against ANSI X3.23-1985 itself (`specs/ANSI_COBOL_1985.md`, with Amendments 1 and 2), not against
the reconstructed `VERSION_CHANGE_REFERENCE.md` and the NIST CCVS85 suite alone. **It is this audit with an edition
parameter, not a second audit:** the same three phases (§3), the same definition of done (§1), the same inventory store (one directory per edition, §10.2) and
its one recording path (§4, §8), and the same rule that an omitted rule is silent false confidence (§7). Only the reader of
the standard's structure, the row key and the change record between adjacent editions are per edition, and each is below. The 2002
edition (kb/Work PB4813) is ISO-structured, so it shares 2023's reader: its catalog and its inventory exist (§10.4,
kb/Work PB5256), its change records do not yet.

### 10.1 The 1985 rule catalog and its key

`python scripts/spec/extract_rule_catalog.py --edition 1985` reads the 1985 document in its own structure
(`scripts/spec/rule_catalog_1985.py`, self-test on planted text):

- **The locator is a Roman section and that section's own paragraph number** (`§1985:VI.5.7.3`), because paragraph numbers
  restart in each section; Amendment 1's Section A (the Intrinsic Function module) is cited `§1989:A.2.5.3`. The readers
  of that structure are `scripts/spec/citation.py`'s (`SECTION_85`, `PARAGRAPH_85`, `FOLIO_85`, `AMENDMENT_85`), so the
  catalog and `cite.py --check` cannot disagree about what a paragraph is.
- **A rule block** is a paragraph headed Syntax Rules (SR), General Rules (GR), Arguments (AR) or Returned Values (RV),
  headed with `#` or, where the transcription did not head it, a plain line opening with its number; a **rule** is a
  column-0 `(n)`, and its lettered `a.` items and their `1)` items are its text (THE RULE IS THE UNIT, §3). A General
  Format paragraph is an FMT row. The page-break furniture (the folio, the page's running header) is never rule text.
- **The row key** is `<year>-<kind>-<locator>-<n>` (`1985-SR-VI.5.7.3-1`; a list restarted inside a block `-L<k>.<n>`, an
  FMT row `1985-FMT-VI.5.8.2`), with its citation (`§1985:VI.5.7.3 SR1`). A CITATION NAMES ONE RULE (kb/Work PB4969): a rule of a
  restarted list is cited by its list, `GRL2.1` for the id's `-L2.1` (`citation.rule_designator`, the one writer; the format's
  `SUBLIST_RULE`), so `§1985:IV.4.2.2.2.3 GR1` is the first list's rule 1 alone, and the extractor's `--check` fails on two
  rows that share a citation.
  The 2023 catalog carries no `citation` field. The year prefix names the edition (or amendment) the
  rule is from, in the 1985 store (§10.2).
- **The conformance requirements are the FRAME, not rows** (kb/Work PB4968). Paragraph 1.5 of Section I (§1985:I.1.5: what a
  conforming implementation must support, the subsets, the optional modules, the obsolete elements of §1985:I.1.5.2.3) is
  unnumbered prose, and the 2023 audit treats its conformance clause the same way: §2023:4.2 enters the definition of DONE
  (§1) and docs/CONFORMANCE.md by direct citation, and only its numbered lists (§2023:4.2.10) are catalog rows. So the 1985
  conformance statement (kb/Work PB4885) cites §1985:I.1.5 and its sub-paragraphs directly as its frame, each requirement
  answered in docs/CONFORMANCE.md, and the catalog gains no row.
- **Unnumbered text has rows here too** (kb/Work PB479, decision PB5448; "Phase A, continued" of section 3 of this
  document). A 1985 margin paragraph or table row that its disposition admits is `<year>-<kind>-<locator>-p<n>` or
  `-T<t>.<r>`, cited `§1985:<locator> p<n>`; the measured 1985 pool is 238 deontic sentences (186 paragraphs, 135
  clauses) and 1,296 indicative ones in no row, of which a paragraph sample makes about 350 rows (95 %: 195 to 500),
  each minted open and closed through its change record. Because the 1985 transcription keeps no indentation, an unlabelled
  paragraph after a rule is part of that rule until the 1985 PDF's margin levels (`margin-levels-1985.json`, step
  PB5452) say it prints at the block's margin. Section II (Concepts) stays outside the denominator, as 2023's
  informative Annex D does.
- **Module and level.** Sections VI to XVI are the eleven modules and Section A the twelfth. Every module but Sort-Merge,
  Report Writer and Intrinsic Function has two levels (§1985:I.1.2; Amendment 1 adds the third one-level module), and the
  standard boxes every specification unique to level 2. A row's `box` is `none`, `part` or `whole` (the prose box is the
  transcription's one markup, `rule_catalog_1985.BOX_SPAN`; a general format's is FIGURE-STYLE.md rule 9's double line),
  and its `level` is 2 only when the whole rule is boxed in a two-level module. A `part` row is a level-1 rule with a
  level-2 span, verified at both levels. A box in a one-level module is a finding. The high subset (§1985:I.1.5.1) is
  every row of the required modules at its module's highest level.
- **The amendment.** The amended 1985 is a different language level for edition gating, so a row carries its `document`
  (the base standard, or Amendment 1 for Section A) and `amended_by`: Amendment 1's Addendum 1 and Amendment 2 are
  tables of changes, a row per change keyed by the printed page it begins on and a paragraph and rule, and a change that
  names a rule, or a general format the catalog holds as an FMT row (`rule_catalog_1985.record_format_change`), records
  its amendment there. The base text is read AS AMENDED: both amendments are applied in place in
  `specs/ANSI_COBOL_1985.md` (owner 2026-11-09, kb/Work PB5045), so a rule an amendment adds or rewrites is read from
  the body where the amendment puts it, with the number the amended text gives it, and its row is the base text's
  (`§1985:<locator> <rule>`); the change row records `amended_by` on it and adds no row of its own (§10.2, "One
  home for an amended rule").
- **Completeness** is held as for 2023: a rule-shaped heading no kind resolves, a recognised block that yields no rule,
  an ordinal that neither continues nor restarts its list, a box in a one-level module, an amendment change naming a rule
  the catalog cannot find (read from the change's directive, never from the new text it carries), a line of a change
  table that opens with a page cell in neither row shape (`CHANGE_UNREAD`: a change no reader would count), an adjudication the
  transcription no longer has, a duplicate id, a rule of an admitted paragraph
  its kind map does not name, and a row whose citation `cite.py --check` would refuse (`rule_catalog_1985.cite_check`,
  `citation.validate` on every row) are each reported, and `--check` fails on them. The paragraphs that DEFINE the rule
  kinds (§1985:IV.3) are outside the denominator, as §2023:5.3 is. **The kind table is ONE**: `extract_rule_catalog.KINDS`
  types both editions' headings, its "Editing Rules" (GR) and "Precedence Rules" (SR) included, the same PICTURE rules
  §2023:5.3.3 and §2023:5.3.2 type, and `extract_rule_catalog.RULE_SHAPED` is the one test of a rule-shaped heading.
- **The content-keyed scan is the 2023 arm's, with the edition as its parameter** (§7, kb/Work PB29). A guard keyed on
  what a block is CALLED cannot find one called something else, so the reader measures EVERY paragraph whose heading names
  no rule block, in every section and appendix (`extract_rule_catalog.ordinal_shape`, the one measure of both editions:
  ordinals, lists, longest run), and compares the result with the committed manifest
  `docs/rearchitecture/spec-unharvested-rule-blocks-1985.json` (`UNHARVESTED_OF[1985]`, the 2023 manifest's shape, keyed
  by citation because the base text's Appendix A and Amendment 1's Section A both have an `A.2.1`). A paragraph measured
  and undeclared, declared and no longer measured, or with a moved ordinal count is a drift (`unharvested_report`, one
  report for both editions), and `--check` fails on it. Each paragraph's disposition is an adjudication by §1985:IV.3.1
  and IV.3.2, read with the 2023 manifest's disposition of the same text: `rules` ADMITS it as a rule block of its `kind`
  (`admitted_kinds`: one kind, a per-item map, a per-sub-list map whose entry may itself be a per-item map, as
  VI.6.3.1.1.2 needs), and `not-rules` records why its list states no requirement (a worked example, an index, a
  technique the text calls one, a glossary entry, the history and the element lists of the appendices, a verbatim
  restatement of a module paragraph that is admitted). The blocks whose heading names rules that KINDS does not type
  ("Standard Alignment Rules", "Formation and Evaluation Rules", the Report Writer's counter and presentation rules,
  IV.4.2.1 Separators and VI.5.15.6) are entries of the same manifest: there is no second adjudication table in the code.
  `EXCLUDED_BLOCKS` (IV.3, VI.6.4, XIII.3.10) and `KNOWN_EMPTY_BLOCKS` (VI.5.9.6) stay, as in the 2023 arm. The Index and
  the appendices open no rule block: a rule-block heading there is reported.
- **The scan's own defects.** The scan prints folios X-11 and X-12 twice (PDF pages 527-528); the transcription keeps
  both copies under `citation.SCAN_REPEAT_85`'s comment, and the catalog reads no rule from the repeat.
- **A reconstruction fills a scan gap, and its rows are provisional** (owner 2026-11-09, kb/Work PB4952). The scan lacks
  printed pages XIII-30, XIII-31 (paragraph 3.10.7, the page heading group presentation rules), XIII-54 (the SUM clause's
  general rules after 9 a) and XIII-55 (the TYPE clause's heading, format and syntax rules 1 to 4). Until the printed
  pages are obtained, each gap holds a RECONSTRUCTION: editorial text between a `cite.RECONSTRUCTED` note, which names
  the folios, the sources and the confidence, and a `cite.RECONSTRUCTED_END` note. The catalog reads it as text, so its
  headings open their blocks and a rule continues across it as across a page break (the TYPE clause's printed syntax rules
  5 to 7 on folio XIII-56 are `§1985:XIII.3.20.3 SR5` to `SR7`), and every row carries `provisional`: `none`, `part`
  (SUM GR9, whose items b and c are reconstructed) or `whole`, with the folio it reconstructs. The mark travels to the
  inventory row (`inventory_view.py --check` counts them), to the audit's requests (`audit_batch.quoted_rule` and
  `rule_block`), and to `cite.py --check`, which marks a quotation it verifies inside a reconstruction; the figure sweep
  draws no format from one (`sweep_figures.drawn_figures`). `verify_publishable.check_reconstructions` (the SPEC
  CORRECTIONS 1985 audit) accepts the reconstructions as editorial and holds them to the folios: each is closed and
  sourced, and the folios it names are exactly those the transcription's printed folios skip where it stands, so a gap
  nobody filled is refused and so is a reconstruction whose printed page has since been transcribed. That replacement
  changes the rows' text, and the store records adjudicated against the reconstruction are refused by their
  `rule-digest` until they are decided again against the printed text.

The catalog is written to `docs/rearchitecture/spec-rule-catalog-1985.json` and held by the gate's RULE CATALOGS
audit (`--held --check`, every held edition; for 1985 no finding, and the committed catalog is this run's). On the reconciled file it counts
2,274 rows from 669 blocks (SR 660, GR 1,256, AR 65, RV 80, FMT 213), 12 of them provisional, and no finding; the
content-keyed scan measures 89 paragraphs (777 ordinals, an upper bound), of which the manifest admits 52 (255 rows) and
records 37 as not rules.

### 10.2 Rule lineage: one rule list per edition, one change record per adjacent pair (owner 2026-11-08, kb/Work PB4991)

The owner's model (2026-11-08): "separate lists for each version's rules. 1985 sets the original aka base set of them.
Then 2002 can do one of three things for each 1985 rule": keep it (it applies to 2002 and is in effect a 2002 rule),
deprecate it (it will be removed later), remove it, and it can define rules new to 2002, "and similarly for later
versions". The mechanism below was approved the same day; it replaces the earlier plan of 1985 rows that carried a 2023
verdict by reference into the one 2023 store.

**One rule list per edition, from that edition's own text.** Each edition has its catalog (§10.1 for 1985, §3 for 2023;
2002 and 2014 when transcribed, kb/Work PB4813), and its inventory is the view of §4 over a store of its own,
`tests/version-matrix/inventory/` for 2023 and `tests/version-matrix/inventory-<edition>/` for each earlier one
(`inventory_store.py` and the view `inventory_view.py`, each of which takes the edition; `inventory_schema.Edition`,
read from the schema's `editions.of`, carries what differs: the catalog, the key that locates a rule, the store's
directory and fields). A reader names the edition it reads, or reads the newest held one.

**One home for an amended rule (owner 2026-11-09, kb/Work PB5045).** The 1985 list is the AMENDED third standard:
`specs/ANSI_COBOL_1985.md` keeps the base text current with Amendment 1's Addendum 1 and Amendment 2 applied in place,
each change attributed by a `CORRECTED` flag and an Errata entry naming the amendment, its change row and its page,
while both amendments stay transcribed in full under their headings as the record of what they changed. So an amended
rule has ONE home, the body: a rule an amendment adds ("add the following as new general rule 4") or renumbers is a
1985 row at the place and number the amended text gives it, and the amendment's change row only records `amended_by`
on that row (read from the change's directive, as now); it adds no row of its own, cited by the amendment's page, beside it. A renumbering the
amendments make (Addendum 1: Section IV 4.3.8.3 to 4.3.8.5 become 4.3.8.4 to 4.3.8.6 behind the new 4.3.8.3
Function-Identifier, Section VI 1.3.5 and 1.3.6 become 1.3.6 and 1.3.7; Amendment 2: the INSPECT, RECORD and WRITE
general rules it renumbers) is the numbering 1985 citations use, and every citation, change record and row id of a moved
rule moves with it in the same change, through `record_verdicts.py --edition 1985`, never an alias. Section A (the
Intrinsic Function module) is the exception to "in the body" that carries rules: Amendment 1 inserts it after Section
XVI, and the transcription holds it once, where Amendment 1 prints it, its rules cited `§1989:A` (§10.1) with Amendment
2's changes applied there. Two insertions of Amendment 2 that run over many pages and carry no rule are held the same
way, once where Amendment 2 prints them, a flag at the place they take in the body: paragraph 2.2B of Appendix B of
Section XVII (pages B-30 to B-46, the Errata's C332) and the replacement index (pages B-49 to B-64, C337, whose row also
deletes the base index's entries). `rule_catalog_1985.resolve_change`'s ADD path finds the body's row (of Section A for
an Amendment 1 page) at the number the amended text gives it and records `amended_by` on it; it adds no row, so the
catalog carries no `§1993` row and no page citation. The store records the renumberings and deletions moved through
`record_verdicts.py --edition 1985`, whose WITHDRAWAL record (`{"rule-id", "withdraw"}`) takes a record whose rule is gone
or whose id now names another rule out of the store. A record moves to its rule's new id only when the rule's text there
is the text the record was decided against (the same `rule-digest`); a record whose rule an amendment or a misprint
correction rewrote is withdrawn, never carried onto the new text, and its row waits to be decided again against the
amended text (kb/Work PB698's drift refusal; PB5045's branch review, R1-2).

**Change is recorded once, between ADJACENT editions only** (85 to 2002, 2002 to 2014, 2014 to 2023, and 2023 to 2027),
never between every pair. Each rule of the older edition carries one classification against the next held edition, in a
`next` field on its own row (so the record shares the row's file and adds no register):

| kind | meaning | what the compiler owes |
|---|---|---|
| KEPT | the next edition states the same requirement (`to`: its rule or rules) | one verdict and one test, good at both editions |
| CHANGED | same subject, a different requirement | its own expected behaviour at each side |
| REMOVED | no counterpart | the older edition accepts it; the newer refuses it (syntax) or does not do it (semantics) |

**The two rules are compared on the programs the OLDER edition admits (decided 2026-11-10, kb/Work PB5021).** A
program is admitted by the older edition's general formats and syntax rules. A case the newer edition adds that no
such program can write (new syntax, a new operand class such as national or boolean, a phrase such as INSPECT's
BACKWARD) does not make the older RULE CHANGED: its record stays KEPT, and the difference is recorded once, on the
general format or the syntax rule of the older edition that excludes the syntax, which is CHANGED and owes the older
compiler's refusal. A refusal the older edition makes of a program its own formats admit, which the newer edition
lifts (or the reverse, or a refusal made an exception condition), IS a change of that rule. So every edition gate of
the compiler rests on a CHANGED format or syntax-rule record, and a general rule is CHANGED only when a program both
editions accept behaves differently. A `to` list names only rules that state the older requirement, in whole or in
part; a newer rule about syntax the older edition lacks is never a KEPT record's target, whatever subject it shares
(PB4996's band adjudication found 18 such targets: 16 records lost the target and stayed KEPT, and two, whose only
target it was, were re-decided CHANGED and keep it as the same-subject successor).

A rule of the newer edition that no older row's `to` names is NEW; that is derived, never written. A `next` stores the
rule ids it links, its `reason` (the words that differ, or what deleted the feature) and its `source` (the audit run or
the adjudication); its evidence is the citation of the older rule and of each `to` rule, read from their catalogs,
never stored a second time, and each one `cite.py --check`s against its own text. The record's shape is DATA, the
schema's `next` (kb/Work PB4993): the keys it may carry, the fields that state a verdict, and each kind's flags (does it
name `to` rules, must it state its `reason`, is its verdict its chain's: `chained`, KEPT alone), read as
`inventory_schema.ChangeRecord` by `lineage.next_errors` and `lineage.state_of`; no code spells a kind. The one writer
is `record_verdicts.py --edition <older edition>`.

**Deprecation is not a fourth kind.** The standard itself marks an element obsolete or archaic in an edition
(§1985:I.1.5.2.3 requires a conforming implementation to support the obsolete elements of what it claims), so `status`
is a fact of that edition's rule, read from its text into its catalog: KEPT into a row whose status is obsolete, then REMOVED at the next edition, is the
deprecate-then-remove lifecycle. A catalog row's `status`, in every edition's catalog, is `normal`, or one clause per
standing naming the list entries that confer it (`obsolete: C.1(n)`, `archaic: F.1(n)`; two standings join with `; `).
The authority is the edition's own enumerated lists: 1985's obsolete list (§1985:C.1, 18 entries); 2023's archaic list
(§2023:F.1, 2 entries, which §2023:4.2.12 defines) and obsolete list (§2023:F.2, 5 entries, which §2023:4.2.13
defines). They are held once as data per edition (`docs/rearchitecture/spec-element-standing-<edition>.json`,
`extract_rule_catalog.STANDING_OF`): per list its standing, its locator and its remark sections; per entry its number,
the terms that name the element in a rule, the sections that specify it, a section's own extra terms (VALUE's VALID and
INVALID phrases), and a SHARED section's numbered formats that are wholly the element (`section-formats`: the EXIT
statement's Format 2 (program), the data description entry's Format 4 (validation), PRESENT WHEN's Format 2
(validation), VALUE's Format 5 (content-validation-entry)), whose band's every syntax and general rule is marked, by the
rule's `band-formats`, whether its text names a term or not; the entry's name is the list's own text. Both catalog arms apply them through one call (`element_standing.apply`). The
marking (`scripts/spec/element_standing.py`) is decided per SECTION: an OWN section, whose heading names one of the
entry's terms (THE ALTER STATEMENT, THE RERUN CLAUSE, the Debug and Segmentation modules; 2023's FLAG-02 directive,
standard-binary arithmetic, the VALIDATE statement and the clauses that exist only for it), marks every rule; a SHARED
one (an I-O-CONTROL paragraph beside SAME, a file description entry, VI 3.2 beside PROGRAM-ID, the OPEN, STOP, GO TO,
DISABLE and ENABLE statements; 2023's EXIT, IF, SEARCH and MOVE statements, the ARITHMETIC clause, the data description
entry and the PRESENT WHEN, VALUE and VARYING clauses) marks only the syntax and general rules that name a term, never
its general format, whose later fate is its own CHANGED record. OWN or SHARED is derived from the heading, never held.
The drift checks, which the RULE CATALOGS audit fails on, hold the data to the text, per list: the
entries are exactly the list's items, every section is a locator of the text, every sentence that gives an element the
list's standing ("is an obsolete element", "is an archaic feature") falls in a section some entry names (or a remark
section the data names: the 1985 level tables' Z notation, a 2023 definition's note, Annex A's lists, Annex D's
concepts), every sentence giving a NAMED format the standing ("The Program format of the EXIT statement is an archaic
feature", "The validation format of the data description is an obsolete feature") names a format an entry declares, a
declared format is a numbered format of its section, a format's number is never a term (it misses the band's rules that
do not repeat it), every entry marks a rule (counted by the marking itself, so a shared general format naming a term does not
count; 1985 entry 1, double character substitution, is prose outside any rule block and entry 4, MEMORY SIZE, is named
only in the shared OBJECT-COMPUTER format, so both are declared `no-rule`, checked both ways), and no shared section's
format and no rule outside its entries' sections is marked. 1985: 143 rules obsolete. 2023: 13 archaic and 106
obsolete. A later edition's lists are one more data file read by the same code.

**A verdict is held once per chain.** A run of KEPT links is one chain; its verdict, code location and test live on the
chain's NEWEST row, and an older row in it holds none (the schema refuses them there), so 2023's adjudicated work serves
every 1985 rule that carries into it without a copy. The older row's state is the chain's, evaluated at its own edition:
the chain's test must also be placed at that edition (the conformance suite's placement, DESIGN-conformance-suite.md).
CHANGED and REMOVED rows, and NEW ones, carry verdicts of their own. The fleet's work is therefore only what changed.
Both languages compute it: Python's `lineage.state_of` and the C# view's `StateAt` in the drift test over
`TraceabilityInventory.RowsOf`, held to each other for every held edition by
`EveryOlderEditionsRuleState_AgreesWithThePythonView`. The session probe and the ledger report each held edition's rules and
GAP side by side (`inventory_view.edition_views`); the ledger's trend series stays the newest edition's GAP.

**The audit fills the change records** (§10.3). Its passes are detections. A SAME that the verify pass upholds is
recorded as KEPT through the one recording path (`record_verdicts.py --edition 1985`, a batch the run itself writes from
its dispositions, `audit_batch.py change-batch`, as the 2023 derived batches are written by `derive_verdict_batch.py`);
a DISPUTED rule, and a DIFFERS or NONE answer (a CHANGED or REMOVED candidate), is decided by the `adjudicate` pass and
checked by one independent pass per decided kind (§10.3, kb/Work PB4995, PB5008): the `recheck` for a KEPT or CHANGED
decision, the `refute` pass for a REMOVED one. An upheld decision is recorded through the same batch, and only what they
leave contested goes to a fleet adjudicator, whose decision is recorded through `record_verdicts.py --edition 1985` with
itself as the `source` and outranks the run's: the run's batch never writes over it. The 1985 audit's decided rules are
recorded (759 upheld SAME rules, kb/Work PB4993, and the upheld adjudications, kb/Work PB4995; 7 of them corrected by
the sample audit of kb/Work PB5008). A record states its `reason` or omits the
key: an empty reason is not stored (`lineage.next_errors`). A CHANGED or REMOVED rule then takes a verdict of its own
under `--std=<edition>`, its witness a draft from the drafts pass that `run_drafts.py` ran and the fleet checked against
the rule text. A checked draft becomes a conformance test through one script, `place_witnesses.py` (kb/Work PB4995 item
3; DESIGN-conformance-suite.md §3.5): it judges each again on the build, files it in the rule's own 1985 clause and
writes a WITNESS-ONLY batch (`{"rule-id", "test-ref"}`) for every placed rule whose verdict is its own. The verdict
itself stays the adjudicator's: CONFORMS requires the `code-location` a reader of the compiler names (the schema's
`verdicts`), which no script can derive from a passing test, so a witnessed rule stays a GAP until it is read.

**Everything else is derived from the chains, never written by hand:** each edition's GAP count (the session probe and
the ledger report one per edition), the conformance statement of each edition (kb/Work PB4885 for 1985), the conformance
suite's edition bands (a band is a chain's span: DESIGN-conformance-suite.md §3.5), the drift test that the
compiler's edition gates (`constructs/` windows, the grammar's version predicates) agree with the lineage's edges,
and **each diagnostic's citation at each edition** (kb/Work PB4950, the owner on 2026-11-08: "a way for a
compilation error to accurately describe the edition + rule violated"). A diagnostic cites one rule; walking that rule's
chain gives the rule of the compilation's edition (a 2023 SR read back along KEPT and CHANGED links to its 1985 rule),
and a chain that does not reach that edition gives the gate's own message from its edge: the construct is NEW at a later
edition, or REMOVED after an earlier one, each with the citation that proves it. The per-edition citations are generated
from the chains into the compiler's diagnostic data and checked by a drift test, never kept by hand.

**The band at an older edition is the chains' (kb/Work PB4996).** A rule of the newest edition exists at an older held
edition exactly when a KEPT record of that edition carries into it, and a bridged record carries it through every edition
the bridge spans (`lineage.spanned`); a CHANGED record proves no band, since the newer rule's requirement is not the
older one's (the older rule holds its own verdict). That rule has one definition, `lineage.chain_bands`, which maps each
rule a KEPT chain reaches to the positions it proves; the conformance suite's band reader (S5, kb/Work PB4683) takes the
positions below the newest edition from it. Until the stored `85` token and the derived one agree it stays stored, and
the view measures the distance (`lineage.band_drift`, built on `chain_bands`, printed by `inventory_view.py --check` and
listed by `--band-drift`): rules a KEPT chain reaches whose stored band lacks 85 or an edition the bridge spans
(`unstored`), rules whose stored band names 85 that no KEPT chain reaches (`unproven`), and the 1985 rules not yet
classified, while any of which remains `unproven` is provisional. The first set is a wrong band or a wrong KEPT record,
never decided by a script: each is adjudicated, through `record_verdicts.py` (the band) or `record_verdicts.py --edition
1985` (the record), by the fleet adjudication of slice 4 (kb/Work PB4995) and the refutation of the audit's upheld
records (kb/Work PB5008). When all three are empty the `85` token leaves the stored `editions` and every reader takes
the band at 85 from `chain_bands`; until then the token is the only 85 position the 1026 unproven rules have, and
deleting it would place them nowhere.

**The edition gates rest on the lineage's edges (kb/Work PB4996).** `scripts/spec/edition_gates.py --check` (the gate's
EDITION GATES audit) reads every gate, a `constructs/` window and a version predicate of the grammar (citing what its
rule's comment cites), and holds each edge the held editions span to the rules its citation names. An edge between two
held editions, a START or an END, rests on a cited rule that changes between them: a rule of the newer one that is NEW or
that a CHANGED record names, or a rule of the older one whose record is REMOVED or CHANGED. Either side carries either
edge: a start is a construct the newer text adds (a new rule, or an alternative added to a format the older text has,
the common case) or a restriction it lifts (the 1985 rule "Identifier-1 must not be reference modified", REMOVED); an
end is a construct it drops or a restriction it adds. What a kind means is the schema's `next` (`chained`), never
spelled in code. **A citation proves an edge only when every rule it names changes** (branch review R1-1): a clause that
gained anything after 1985 holds changing rules beside KEPT ones, so a gate citing the whole clause would pass whichever
of its constructs it moved. A gate names the SR or GR its edge rests on. **A general format proves only a whole
diagram's edge** (branch review R2-1): the catalog holds one FMT rule per clause (a general-format subclause, or `Format
n`, names it), every format and alternative of the statement in it, so a CHANGED one would prove any construct of the
statement; a NEW newer FMT rule (a new statement) proves a start, an older one REMOVED an end, and nothing else. A
construct the standard states only in its diagram, with no SR or GR of its own (END-ACCEPT, the optional IDENTIFICATION
DIVISION header, USE without PROCEDURE: 12 windows at this slice), has no edge the lineage proves, and is held open in
`edition_gates.FORMAT_ONLY` until the catalog numbers each format's alternatives; a new window resting on a general format
alone is refused. An SR or GR has no direction in the lineage (a NEW rule adds a construct or a restriction), so an
inverted window citing one is the version matrix's to catch, not this audit's. A gate whose cited rules are all KEPT contradicts its chain, and the audit refuses it until the gate or the record is
corrected (two records were, by this slice: 1985-GR-VII.2.6.3-2 KEPT into line sequential organization, and the 1985
FILLER rules KEPT into 2023's wider entry-name clause); so is a gate citing nothing of either edition, one whose every
citation mixes changing rules with KEPT ones, or one resting on an unclassified rule. Three
gates lie outside the rules: a word's reservation (a gate citing only the word lists, §2023:8.9 and §2023:8.10, whose
edges `reserved-words.json` and `context-sensitive-words.json` carry), an edge specified in prose the catalog holds no
rule of (the 1985 continuation of lines, §1985:IV.7.2.2; the 2023 floating indicators, §2023:6.2.3.1), which the
CITATIONS audit still holds to the text, and a window wholly between two held editions, which waits for the edition
between them. A finding a known compiler defect holds open is listed in the audit (`HELD_OPEN`, checked both ways) with
the note that fixes it.

**A missing edition is bridged, provisionally.** Until the 2002 and 2014 texts are transcribed, the 1985 rows' `next`
points straight at 2023 and is marked `bridge`. When 2002 is held, its two adjacent records replace the bridge, and
composing them must give the bridge's answer: a cross-check of both audits, held by a drift test, at no extra cost.
Holding an edition and linking it are two steps (kb/Work PB5256): 2002 is HELD (its catalog, its store, its rows and its
GAP count) but not yet LINKED (the schema's `linked: false`, `inventory_schema.Edition.linked`), so the lineage still
bridges over it exactly as over an edition not held: 1985's records name 2023, 2002's own records name 2023 over 2014,
and the EDITION GATES audit spans no edge at 2002. The adjudication that writes the 1985 to 2002 records sets `linked`
in the same change, and from then on `next_held` routes 1985's records into 2002 (both languages read the flag:
`editions_of` and `TraceabilityInventory.Editions`).

**Why it stays maintainable.** A new edition costs one rule list, one adjacent change record and the adjudication of what
changed; every change carries a checked citation in both texts; and the drift tests hold that every older rule is
classified exactly once and every newer rule is reached or NEW.

**The slices** (kb/Work PB4992 to PB4997, in this order):
1. the inventory as the view of §4, in both languages: a store per edition holding adjudication only (no catalog
   copy, no stored state, no file for an unadjudicated rule), every reader moved onto the view, and the rebuild's
   seeding retired into the view's own checks;
2. the change record `next` and its recording path, the catalog's `status`, and the derived batch that records the
   audit's upheld SAME answers as KEPT;
3. chain state in the view, in both languages, and the GAP count per edition in the probe and the ledger;
4. the fleet adjudication of the disputed and own-row rules against `--std=1985`;
5. the derived bands (`lineage.chain_bands`, which the conformance suite's S5 reads), the `85` token leaving the
   stored `editions` once the stored and the derived 85 agree (the band drift measures how far apart they are, and
   slice 4's adjudication closes it), and the edition-gate drift test (the EDITION GATES audit);
6. the 1985 conformance statement (kb/Work PB4885).

### 10.3 The Message Batches arm — `scripts/spec/audit_batch.py`

The independent JUDGMENTS go to Opus 5.5 through the Message Batches API (owner 2026-11-07, kb/Work PB4886), in passes,
each a DETECTION a fleet agent turns into a verdict, never a verdict itself:

| pass | one request per | answer |
|---|---|---|
| `route` | 1985 clause (the paragraph that holds its rule blocks), with its subject, module and first three rules, and the list of every 2023 clause that has rules | ROUTED with the 2023 clauses that state the same feature (at most 8, closest first), or NONE |
| `map` | 1985 catalog rule, with its candidates (below); planned only once every route item is answered | SAME, DIFFERS or NONE, the 2023 ids and the differing words |
| `verify` | SAME answer | UPHELD, REFUTED or UNSURE: the adversarial pass tries to refute it |
| `claims` | 1985 claim of `VERSION_CHANGE_REFERENCE.md` (a JSON list of claims the fleet prepares, each with its 1985 locator) | CONFIRMED, REFUTED or NOT-ADDRESSED, with the 1985 `locator` and `rule` (null where no numbered rule decides) as fields of their own |
| `drafts` | rule whose disposition is a row of its own, and rule an adjudicate or recheck answer reads as CHANGED, REMOVED or OPEN (read from the answers, which only grow, so a re-plan only grows the plan) | a minimal program and the outcome the rule requires (for a refusal, the violating line and a conforming control), which `scripts/spec/run_drafts.py` compiles and runs under `--std=1985` and the fleet checks against the rule text before any use |
| `adjudicate` | DISPUTED or OWN-ROW rule, with its map candidates and both readers' answers (kb/Work PB4995) | its change record: KEPT or CHANGED (the 2023 candidates it carries into, and for CHANGED the differing words) or REMOVED (2023 drops it), or OPEN when the texts do not decide |
| `refute` | REMOVED adjudication, with every candidate the adjudicator saw | the strongest reading on which the requirement survives, first; then UPHELD, REFUTED or UNSURE |
| `recheck` | KEPT or CHANGED adjudication (kb/Work PB5008), at the highest effort, with its whole 1985 clause and the 1985 rules most like it by wording, every rule of every kind of the 2023 clauses that hold its feature and of each clause a decided rule lies in, and the map's worded candidates beyond them | framed to DISPROVE the decision: (a) another 1985 rule bearing on the element, (b) every 2023 rule that states any part of the requirement, (c) a refusal made a run-time exception condition; then the change record the texts support, with `rules_1985` and `exception` |

A ninth pass serves the 1985 TRANSCRIPTION, not the change records (kb/Work PB4984):

| pass | one request per | answer |
|---|---|---|
| `misprints` | rule of the 1985 catalog as it now reads (repairs since the map included), with the 2023 rules its map answer named (SAME or DIFFERS), or none (NONE, or a rule the map never saw: read for its own sense alone) | MISPRINT, with each misprint's printed words, the words the sense requires and the rule's words around them, or CLEAN |

It finds what no spelling scan can: a misprint that makes another real word (`form` for `from`). The answer shape holds
each misprint to the rule's text (`audit_batch.misprints_error`: the context is the rule's own words once tags, escapes,
box sides and line breaks are dropped, and it holds the printed words whole), so a lead always points at words the page
prints. `misprint-leads` writes `DIR/misprint-leads.json`, each lead with its rule's citation and whether a counterpart was
read beside it. A lead is never a correction: a reader confirms it on the rendered page before the Errata of
`specs/ANSI_COBOL_1985.md` lists it (kb/Work PB4972's convention). A confirmed lead is corrected when the page, the
rule's own sense, the same paragraph or a later revision shows it is wrong, an operand included: the transcription is
the most accurate version of the specification and never keeps incorrect text because the page printed it (owner
2026-11-09, kb/Work PB5045); a wording that is the standard's deliberate requirement stays as printed, and the Errata
says why. A misprint an amendment corrects is applied as the amendment's change, attributed to it. The pass reads catalog rules only; the prose outside
the catalog, and the index, stay unread by it; the catalog's 210 general-format (FMT) rows are planned with every other rule and so are read, and the Errata says so.

**A rule's candidates are drawn from the strongest signal down** (`audit_batch.candidates`, kb/Work PB4971, owner
2026-11-08: "Use the strongest appropriate signals"):
1. every 2023 rule of the SAME KIND in the clauses its clause was ROUTED to, or shares a heading with (`subject_key`:
   `THE START STATEMENT` and `START statement` are one), in catalog order and uncapped;
2. then the k rules most like it by wording that step 1 did not name (the inverse-document-frequency weight of the words of
   their headings and text, the same kind preferred; 15 by default).
Wording alone was measured on the first 1985 run (k = 15): 464 NONE answers, 217 of them giving "no candidate addresses
it" rather than a deleted feature; a heading alone matches no 2023 clause for 598 of the 2,253 rules, because 1985 repeats
a clause per module and names clauses differently. The route pass is the signal that reads the clause as a whole, one
request per clause (247 for 1985), so it is cheap; the heading and the wording stay as the routes' complement.

`results` writes each mapped rule's disposition (CARRIES, OWN-ROW, DISPUTED, or still open) and its DECIDED change
(`audit_batch.dispositions`, the one derivation): KEPT for CARRIES, and the adjudicate pass's answer once the check its
kind takes (`audit_batch.checker`, read from the kind's `to` flag: the recheck when it names 2023 rules, the refuter when
it names none) UPHELD it and no other check that answered disputes it. The recheck upholds a decision only when the
record it finds is the same one, the same kind into the same set of 2023 rules (`audit_batch.upholds`): a rule it adds
is one the decision omits. An OPEN or disputed adjudication is `contested`, a fleet adjudicator's (`cobol-adjudicator`),
never a vote. `python scripts/spec/audit_batch.py change-batch` turns every decided change into a `record_verdicts.py --edition
1985` batch (`change_records`, the one writer), leaving out each rule whose stored record another source (a fleet
adjudication) decided.

**The fleet's decisions take the same checks** (kb/Work PB4995, owner 2026-11-09). `audit_batch.py seed --state NEW
--upstream DIR --decisions BATCH` starts a run whose own adjudicate pass is the upstream items of the rules a
`record_verdicts` batch decides, each answered by its record (kind, 2023 rules, reason, and the record's source), and
which reads the route, map and verify passes from the upstream run (`upstream.json`, `audit_batch.pass_dir`; only
those inputs, `INHERITABLE`, may be inherited). Both the inherited passes and the seeded adjudicate pass are read only:
no model is asked for a recorded decision. Its recheck and refute passes then plan from the fleet's decisions exactly
as from the adjudicate pass's, its `results` contests each one a check disputes, for a `cobol-adjudicator` again, and its
`change-batch` keeps each upheld decision's own source, so the fleet's record still outranks the audit's.

**Why KEPT and CHANGED take the recheck** (kb/Work PB5008, 2026-11-08). The refute pass upheld 1,345 of the first 1,371
decisions it read, and a sample audit by `cobol-adjudicator`s from both texts found 5 of 18 upheld CHANGED and 3 of 15
upheld KEPT decisions wrong, every one of them upheld by the refuter (12 REMOVED and 15 map + verify KEPT records: no
error). The refuter, the same model at the same effort shown the same candidates, measured agreement, not accuracy. The
errors fell in three modes, which the recheck's three questions name: a 1985 rule elsewhere already allows what the
record calls new; the record omits the 2023 rule that states the requirement (the commonest: a definition restated in
another clause, a group-parameter rule, the general rule of §2023:14.7.7 over every arithmetic statement); and a
compile-time refusal that 2023 turned into a run-time exception condition (§2023:14.9.39.4 GR2). So the recheck reads
whole clauses at the highest effort, and the refuter keeps REMOVED, its prompt stating the strongest counter-reading
before it judges. `python scripts/spec/run_drafts.py --state DIR` compiles and runs each draft at `--std=1985` through the
conformance harness (`conformance-suite/harness/run.py`'s `Run` and the WiseOwl adapter, the one judge of a program's
outcome): an output draft is an accepted test (`Run.accept`), and a refused draft a FLAGGED test (`Run.flagged`), so a
refusal counts only when it is the rule's: the draft names the line that violates the rule and carries a control (the
program with that line made conforming), the control must compile and the diagnostic must be AT that line. Every stored
draft names both, since the drafts pass's answer shape requires them; the 291 refused drafts written before it did were
withdrawn (`audit_batch.py withdraw`) and drafted again (kb/Work PB4995). A draft that COPYs carries its library texts
as data, `files` (`{"<text-name>.cpy": text}`, or `<library-name>/<text-name>.cpy` for a text in a named library): one
shape of a draft, `audit_batch.py draft_problem`, checks every pass's draft, and `run_drafts.tests_of` writes the texts
beside the program and declares them on the test and its control, so the harness stages them for both (kb/Work PB5067).
It writes `DIR/drafts/runs.json`: MATCH,
MISMATCH or NO VERDICT per rule,
naming the compiler binary (its `--version` and the digest of its assemblies, never a checkout's HEAD). A MATCH is a candidate witness, placed in the conformance suite only after the fleet checks the draft
against the rule text; a MISMATCH is a defect or a suspect draft for the fleet to tell apart. The tool's rules:

- **Restartable**, on the ONE batch state the transcriber shares (`scripts/spec/message_batches.py`, `BatchRun`): an
  intent written before every create and the batch after, a crash between them adopted from the server's list on the next
  run, the usage of every billed reply recorded under its batch and custom id (a retried item's earlier reply stays in the
  spend `--max-usd` checks). A pass's items are planned once (`items.json`, every input its prompts need);
  each answer is written once and never overwritten; a reply outside the pass's answer shape is kept in `raw/` and its item
  retried. When the shape grows (a refused draft's violating line and control), `withdraw` moves each stored answer the
  shape now refuses to `raw/` (`<custom id>.withdrawn.json`) and `retry` asks for it again, so the shape stays the one rule
  of what an answer is and no reader keeps an arm for an older one.
- **A canary before each full submit**: `submit --canary N` first, and a full submit is refused until that pass holds a
  collected canary with a valid answer. **`--max-usd` on every submit and retry**, against spent plus in-flight plus this
  submit, priced by the canary's measured cost per request. The map pass on the unreconciled file estimates about $0.017
  a request, about $34 for the pass, before a canary measures it. Until a canary is proven, `retry` resubmits that
  canary's own unanswered items, as the canary, and nothing else (kb/Work PB4930).
- **A request is built in cache layers, most shared first** (kb/Work PB4998, after Anthropic's notice of 2026-11-08 that
  this account's direct API traffic had a low prompt-cache hit rate): the pass's instructions are the request's cached
  `system` (`message_batches.cached_system`), a group's shared part is its cached first user block
  (`message_batches.cached_text`), and the item's own part comes last (`audit_batch.prompt_parts`). The route pass's group is
  every request (the list of every 2023 clause, about 7,000 tokens); a map request's is the clause's candidates, which every
  rule of that 1985 clause and kind shares (`Candidates.clause`), so the rules of one clause read it once, measured on the
  re-run at 47 % of the map pass's input. The TTL is an hour (`message_batches.CACHE_TTL`), because a batch runs for
  longer than five minutes and its cache hits are best-effort; `usd` prices the cache writes (twice the input price) and
  reads (a twentieth on Opus 5.5) the replies report (`message_batches.usage_of`). A prefix below the model's minimum (512
  tokens on Opus 5.5: each pass's instructions alone) caches nothing and costs nothing.
- **One citation shape** (kb/Work PB4886): many 1985 paragraphs a reader cites are a Function, a General Format or prose
  with no numbered rule, so an answer names its `locator` and its `rule` as two fields (`audit_batch.CITATION_FIELDS`, the
  prompt's words), read through `citation.py` and validated against the 1985 transcription. The catalog citation every
  pass quotes for its rule must parse too (`plan --pass map` refuses a catalog whose row does not); its rule kinds are
  `citation.RULE_KINDS` (SR, GR, AR, RV), the kinds both catalogs write.
- **Public text only**: a prompt carries the tracked transcriptions' text, never a page image; a request whose text
  matches any per-copy stamp shape (`verify_publishable`) is refused before any batch.
- **The key** comes through `scripts/api_key.py` with `--api-account`; each batch record in a pass's `state.json` names
  the account that created it, never the key. A batch belongs to that account, so while one is uncollected (or a
  create's intent is unreconciled) every command bills its account; once every batch is collected a pass may move to
  another account, so one pass can spend two accounts' credit, and `status` prints each account's spend
  (`message_batches.BatchRun.load_key`, `pinned_account`, `billing_line`; kb/Work PB4995). The self-test (`scripts/spec/test_audit_batch.py`) uses a fake client (`scripts/spec/fake_batches.py`,
  the transcriber's self-test's too) and opens no key file.

The passes run after the reconciled 1985 file (PB4850) and its catalog exist; the verdicts stay with the fleet (§5, §8).

### 10.4 The 2002 catalog, its inventory and its lineage seed (kb/Work PB5256)

**One ISO reader, a table of structure facts.** ISO/IEC 1989:2002 is built as 2023 is (numbered clauses, a general format
and Syntax and General rules per construct, Argument and Returned value rules per intrinsic function), and measured on
2026-11-11 the 2023 extraction read the 2002 text unchanged into 2,990 rows and 13 findings, each a structure fact or a
defect of the transcription. So `python scripts/spec/extract_rule_catalog.py --edition 2002` is the 2023 reader with the
edition as its parameter: `extract` is the one extraction and `ISO` (`IsoEdition`) holds what differs. For 2002 that is
Annex A (normative, the communications facility), whose clauses carry rule blocks as the body's do; the
implementor-defined list in B.1 (2023: A.1); §2002:5.2, the taxonomy clause, outside the denominator, and its four
sub-clauses that define the kinds; §2002:15.3 and §2002:13.16.38.5 known empty (their rules sit in §2002:15.3.1, and the
precedence rules are Tables 11 and 12); a table of contents that prints the clauses to the third level, each of which must
be a heading of the transcription with the TOC's title; and the row form 1985's rows have: the year in the id
(`2002-SR-14.8.25.2-1`), the `citation` (`§2002:14.8.25.2 SR1`) and the PDF `page`. The 1985 reader stays its own
(`rule_catalog_1985.py`): its structure is not ISO's, and it already shares the kind table, the heading rules, the
content-keyed scan and the standing lists. The 2023 and 1985 catalogs regenerate byte-identical through the restructure.

**What it reads.** 3,290 rows from 548 blocks: SR 1,168, GR 1,361, AR 142, RV 142, DOC 184 (B.1), FMT 293; 117 of them
Annex A's. The standing lists are `spec-element-standing-2002.json` (§2002:G.1, five archaic elements, and §2002:G.2, three
obsolete ones; `element_standing.LIST_ITEMS` reads 2002's body-type item): 11 rules archaic, 129 obsolete (Annex A whole).
Every row's citation names a region of the transcription (`citation.validate`, in the extractor), no two numbered rules
share one, and a sample of 284 citations over 236 clauses (every twelfth numbered rule, every tenth format and
implementor-defined item, each quoted by the first words of its printed line) passes `cite.py --check`, the rule
ordinal included.

**What it cannot read is a finding, never a guess.** Each is a `Finding` (its kind, clause, line and PDF page); the kinds are
`FINDING_KINDS`, counted in the summary. 2023's transcription is reconciled, so `--check` fails on any finding; 2002's is
not (kb/Work PB4813), so its findings are written into its catalog (`findings`) for the reconciliation and `--check` fails
when they change. On 2026-11-11 there are six: a table cell of §2002:15.16.2 ("00 through 99.") that the text layer wrapped
onto a line of its own (kept in RV1's text, never a row: an ordinal no list explains is no longer filed as a guessed row in
any edition), a nested item of §2002:14.8.1.3 GR25 printed at column 0 after a page break (folded into GR25), and four
rule-shaped headings no kind types (§2002:8.8.1.3.1.3 "Rounding rules", §2002:8.8.4.1.4.2, 8.8.4.1.5.2 and 8.8.4.1.6.2
"Rules"), which wait for the content-keyed manifest's adjudication.

**Unnumbered text** takes the dispositions of "Phase A, continued" of section 3 of this document (kb/Work PB479, step
PB5456): a 2002 unit whose words are a 2023 unit's carries that unit's disposition as its `candidate`, decided like any
disposition (its refuter record included), and its pending units join the lower bound below.

**The content-keyed manifest** (`spec-unharvested-rule-blocks-2002.json`) measures 92 clauses (395 ordinals). None is word
for word a clause of the 2023 manifest, so none carried a disposition: every one is `pending`, and its rules are outside
the 2002 denominator until it is adjudicated by §2002:5.2.1 and 5.2.2. Each carries a `candidate` (71 of them: the 2023
manifest clause of the same title nearest in text, its disposition and kind) as the adjudicator's starting point. Until
then the 2002 rule count is a lower bound: the catalog records what it leaves out (`outside-denominator`: the pending
clauses and the untyped headings), and every reader that prints the count prints it beside it
(`inventory_view.lower_bound`: the session probe's `invent` line, the ledger), so a 2002 count at zero GAP cannot read
as a complete denominator (branch review R1-5).

**The stability rule.** An id is its clause, its kind and its printed number (`IsoEdition.rule_id`), so regenerating the
catalog after the reconciliation corrects a page keeps every id whose clause and number did not change and changes only
the corrected rule's text; a rule the correction inserts renumbers the rules after it in its own block, as the printed
numbers do. A record adjudicated against changed text is refused by its `rule-digest` (§10.2). The extractor's
`--self-test` plants a page, applies a correction and holds this. A CHANGE RECORD names its `to` rules by id, so it
also carries each target's `rule-digest` when it was decided (`next.digests`, stamped by `record_verdicts.py`; 1985's
1,937 records were stamped against the 2023 texts they were decided on, unchanged since): a target whose text has
moved under its id makes the record STALE by name (`lineage.stale_targets`), and the INVENTORY VIEW audit fails on it.
The reviewer's scenario, one rule inserted into READ's general rules, moves the text under 27 ids, and a record into
each is named STALE (branch review R1-4). And an edition is LINKED only once its transcription is reconciled
(`inventory_schema.link_errors`, asked by the EDITION GATES audit, against `verify_publishable.reconciled`: the
reconciliation ledger records every page of the PDF, `spec_pdf.EDITIONS[...].pages`), so no older edition's records
can name 2002 ids before the reconciliation stops moving them. The extractor's `IsoEdition.reconciled` is the same fact.

**The inventory** is `tests/version-matrix/inventory-2002/`, the 1985 store's layout and fields (`verdict`, `code-location`,
`test-ref`, `derivation`, `notes`, `next`), held by the schema's `editions.of` and read by the same view, store and
recording path (`inventory_view.py`, `inventory_store.py`, `record_verdicts.py --edition 2002`): no 2002 script. It holds no
file (a store holds adjudication only), so every row is open: 3,290 rules, 3,290 GAP, printed by the session probe's
`invent` line and the ledger beside 1985's and 2023's, which iterate `inventory_schema.HELD`. 2002 is held but not linked
(§10.2): no change record names it yet. The gate's RULE CATALOGS, LINEAGE CANDIDATES and CATALOG COVERAGE audits (and
CI's) read the held editions from the schema, so a newly held edition needs no new audit line (branch review R1-2, R1-7).

**The lineage seed.** `python scripts/spec/lineage.py --candidates` writes `spec-lineage-candidates-<year>.json` for every
older held edition: each rule paired with the next linked edition's rules of the same construct (`subject`), kind and
text, `exact` or `normalized`, no judgment and no record. NORMALIZATION ERASES TYPOGRAPHY ONLY (`lineage.pairing_text`):
a Markdown escape, typographic quotes and dashes, the ellipsis glyph (read as `...`), a list item's delimiter (`1.` and
`1)`), white space and the transcription's own markup (a Markdown hard break, read as a line break; a character entity
such as `&emsp;` or `&lt;`, read as its character); never a bracket, a brace, a choice bar, an ellipsis, an underline, a word, a letter's case or a
number, so a general format's notation is part of its key (the first seed, by `cite.norm`'s words only, paired 2002's
`{character-1} ...` with 2023's `[character-1] ...`, a change of meaning: branch review R1-3). On 2026-11-11, 2002 to
2023: 1,771 paired (1,730 exact, 41 normalized, 2 with two candidates), 1,519 unpaired; 1985 to 2023: none paired
(2,274 unpaired: no 1985 rule is word for word a 2023 rule). The adjudication of the 2002 change records starts there, and the 1985 to 2002 records after it link 2002.
