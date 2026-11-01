## PB4230: a section owns its paragraphs in the grammar's shape, so the LL pass is linear in a section's paragraphs

**The mechanism.** `procedureDivision` read `declarativePart* sentence* procedureUnit*`, with `procedureUnit :
sectionDefinition | paragraphDefinition`. A paragraph after a section header could stay in the section's
`paragraphDefinition*` loop or close it and be a division-level `procedureUnit`. Both derivations accept every token to
the next section header or the end of the division. SLL prediction resolved the conflict at two tokens, but the LL pass,
which every program whose SLL pass bails takes (a syntax error anywhere, or a legal `SET p TO NULL` until PB4231 lands),
ran full-context prediction to that point at every paragraph header. The cost was quadratic in a section's paragraphs.

**Re-probe (Debug `cobol.exe`, wall time).** `null_sec_400.cob`, a legal program with one 400-paragraph section and one
`SET W-P TO NULL`: 112,412 ms, rc 0. CCVS DB201A (`--nist DB201A`, two COBOL5506s in its DECLARATIVES): 58,272 ms.
`null_nosec_400` (the same paragraphs, no section): 3,563 ms.

**Fix.** ISO §14.4.2: "A section consists of a section header followed by zero, one, or more successive paragraphs. A
section ends immediately before the next section or at the end of the procedure division" (cite-checked). The grammar
now writes that as its shape: `declarativePart* sentence* paragraphDefinition* sectionDefinition*`, with `procedureUnit`
deleted. After a section's last paragraph, the only continuations are another section header, an end marker, a nested
source unit or the end of input, and those differ from `paragraphName DOT` within the header's own tokens. Leading
sentences and paragraphs still parse as the division's own children, so PB1146's COBOL2797 refusals fire where they
did. The three readers changed with the rule: `ProcedureTableBuilder.CollectNondeclaratives` walks the division's
paragraphs and then its sections (source order); `ProcedureFormatPass.VisitProcedureDivision` reads the two lists
directly; `PrototypeUnitRules.ScreenBody` (§10.6.2 SR4 f) asks both. DESIGN-frontend-grammar §3.14 states the shape and
why the one-loop form is excluded.

**After.** null_sec_400: 6,801 ms, same output, and a byte-identical `.dll`. DB201A: 1,380 ms, same diagnostics.
null_nosec_400: 3,215 ms. The review's refuter had already shown the reshape tree-neutral (procedureUnit spliced out)
and diagnostic-neutral on all 6,201 corpus files.

**One diagnostic changed wording, by design.** A paragraph or section header written after a statement with no
period, inside a section, drew COBOL0307 as a failed prediction ("cannot parse construct near 'P2'. A period may be
missing ..."), because the ambiguous loop's prediction looked past the header. It now draws the COBOL0307 the same
fault draws in a division without sections ("unexpected 'P2'. A period may be missing at the end of the previous
sentence."): still one report, the same code, at the same token. `ParseRecoveryHintTruthTests` rows PBHT31-36 and 38
now pin that wording, and PBHT37 (the header after an open IF ... ELSE IF ... END-IF) still pins the failed-prediction
path, which is unchanged. `PrototypeUnitRulesTests.Sr4f_ProcedureStatements_AreRefused` gained a section row and a
leading-sentence row, so each body element §10.6.2 SR4 f) now asks about separately has its own witness.

**Guard.** `ProcedureDivisionShapeDriftTests` parses one program, with DECLARATIVES, leading paragraphs, two sections and
a nested program, in LL mode under ANTLR's profiler at 3 and at 60 paragraphs per section. No decision of the
procedure-division structure rules may report an ambiguity, look further than one qualified section header, or look
further at 60 paragraphs than at 3. On the old shape it fails with 120 ambiguity reports at `sectionDefinition`. The
cost model over every structural loop stays PB4253's.

**Golden.** `14.2-Procedure-division-structure/pb4230_section_owns_its_paragraphs` (accept 85): `PERFORM A-S` runs A-S's
unnamed sentence, A-1 and COMMON-P and stops before B-S (0111). COMMON-P is defined in both sections and is referenced
qualified from MAIN-S (§8.4.2.2.3 SR7): `COMMON-P OF B-S` gives 1000 and `COMMON-P IN A-S` gives 0100. The expected
values come from §14.4.2 and §14.9.28.4 GR4. The inventory row FMT-14.2.1 stays CONFORMS and gains the golden and the
drift test as witnesses (record_verdicts batch `w20261795a-pb4230-section-owns-paragraphs`, GAP 19 to 19).

**Gate.** Run 000001-20261101T044521Z on db751f279 was RED on the seven ParseRecoveryHintTruthTests rows above (fixed in
2c5fba4ac). Run 000002-20261101T050807Z on 2c5fba4ac: every case passed (Conformance 20,375/20,375, Unit 39,097/39,097,
Characterization 36/36), and the verdict is `=== BUILD-LOCAL GATE: RED — Unit POPULATION NOT EXACT` on the four
astral-character theory names alone, which is kb/Work PB4338 (open, outside this group): the grammar change makes the
plan hold the whole of tier 1, which is the plan PB4338 bites.
