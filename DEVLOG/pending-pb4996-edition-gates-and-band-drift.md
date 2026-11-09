## PB4996: the compiler's edition gates are held to the rule lineage, and the band drift is measured

Lineage slice 5 (kb/Work PB4996; DESIGN-spec-conformance-review.md §10.2). Two mechanisms.

**The EDITION GATES audit.** `scripts/spec/edition_gates.py --check` runs in every gate lane and in CI's audits job. It
reads all 307 `constructs.json` windows and the grammar's 5 `{isNNNN()}?` predicates, and holds every edge the held
editions (1985 and 2023, bridged) span to the rules the gate's citation names. The drift test is an audit because the
lineage's rules live in Python and the C# view only joins (PB4992): a Unit test would need a second copy of them. The design
said a gate rests on a NEW or REMOVED edge. The first run showed that 229 of the windows rest on a CHANGED one: an
alternative added to, or dropped from, a format 1985 has. The design now says so: a start rests on a NEW or CHANGED rule,
an end on a REMOVED or CHANGED 1985 rule, and a gate whose cited rules are all KEPT contradicts its chain.

The first run found 48 gates. 46 cited nothing the lineage could read: bare clause numbers (`ISO 13.18.1`) or, on a
removal, no 1985 rule at all. 40 `constructs.json` citations now name their rules; each removal cites the 1985 rule whose
record removes or changes it (LABEL RECORDS §1985:VII.3.6.2, ALTER §1985:VI.6.7.2, CALL ON OVERFLOW §1985:X.5.2.2, ...),
and the generated registry was regenerated. Two were contradictions, and both were wrong 1985 records, re-adjudicated
through `record_verdicts.py --edition 1985`: `1985-GR-VII.2.6.3-2` was KEPT into line sequential organization (it is
record sequential's; line sequential is NEW at 2023), and the 1985 FILLER rules were KEPT into 2023's entry-name clause,
which also names report and screen items (now CHANGED). A word's reservation (citing only §2023:8.9 or §2023:8.10) is the
word lists', and an edge specified in prose the catalog holds no rule of (§1985:IV.7.2.2) rests on its citation, which the
CITATIONS audit checks.

One finding is held open, checked both ways: `remarks-removed-2002` accepts the REMARKS paragraph at `--std 85`, and ANSI
X3.23-1985 deleted it (§1985:A, item 8, "The deletion of NOTE and REMARKS in favor of a general comment capability"). That
answers kb/Work PB1802's open item 2; the fix changes the compiler's behaviour and is filed from the wave's report.

**The band drift.** `lineage.band_drift` derives the band at 85 from the chains (a rule is at 85 exactly when a KEPT
record carries into it, a bridged one through 2002 and 2014 too; a CHANGED record proves no band) and measures the stored
bands against it. `inventory_view.py --check` prints the three counts and `--band-drift` lists them: 96 rules a KEPT chain
reaches lack 85 in their stored band, 1026 name 85 with no KEPT chain behind it, and 149 1985 rules are unclassified. The
96 mix wrong bands (the 1989 intrinsic function rules stored `2002,2014,2023`) and wrong KEPT records (CONTINUE AFTER's
rules KEPT from "The CONTINUE statement has no effect"). A script decides neither, so the stored `85` token stays until
the sets are adjudicated and empty. PB4996 stays open for that step.

**Branch review round 1.** The reviewer found the audit too lenient: a whole-clause citation proved an edge when ANY rule
under the clause changed, so a window wrongly starting CALL ... USING at 2002 and citing `§2023:14.9.4` passed on BY
VALUE's rules (58 of the proven gates rested on such breadth). A citation now proves an edge only when every rule it
names changes; a `Format n` names the clause's FMT rule; and either edition's rule carries either edge, so a start can
rest on a 1985 restriction the newer text deleted (UNSTRING's "Identifier-1 must not be reference modified", REMOVED).
The rule refused 27 windows, each re-cited to the rule its edge rests on: the general format's subclause where the
construct is one of its alternatives (START FIRST/LAST, STOP RUN WITH STATUS, the INITIALIZE phrases, the LOCK phrases),
else the SR or GR (`§2023:13.18.40.3 SR14` for the 19-digit picture, `§2023:14.9.4.3 SR21` for BY VALUE,
`§2023:13.18.55.3 SR1` for SYNCHRONIZED on a group), each `cite.py --check`ed. The construct windows are now read in years
through the schema, as the grammar's predicates already were. The reviewer also found the band rule had no reader-facing
definition, only the drift: `lineage.chain_bands` is now its one definition (each rule a KEPT chain reaches and the
positions it proves), `band_drift` is built on it, and S5's band reader (PB4683) takes the 85 position from it. The
stored `85` stays: it is the only 85 position the 1026 unproven rules have until PB4995's fleet adjudication and PB5008's
refutation close the drift, so PB4996 is now blocked by both.

**Branch review round 2.** The round-1 rule moved the breadth one level down: the catalog holds ONE general-format rule
per clause, every format and alternative of the statement, and 111 of the 297 proven gates rested on one CHANGED FMT rule
alone, so `§2023:14.9.4 Format 1` still proved CALL ... USING at 2002 (the reviewer's probe). A general format now proves
only a whole diagram's edge: a NEW newer FMT rule a start, an older one REMOVED an end (`diagram_bound`), which also gives
its proof a direction (lead R2-2; an SR or GR has none in the lineage, now said in the docstring). The rule refused 56
gates; 44 windows now name the SR or GR their construct rests on (READ PREVIOUS `§2023:14.9.30.4 GR20`, CALL RETURNING
`§2023:14.9.4.4 GR4`, the MEMORY SIZE end `§1985:XVI.2.2.4 GR2`, DISPLAY's signed literal `§1985:VI.6.10.3 SR2` REMOVED,
...), the PERFORM LOCATION predicate cites `§2023:14.9.28.4 GR14`, and the 12 constructs the standard states only in a
diagram (END-ACCEPT, END-DISPLAY, the optional IDENTIFICATION DIVISION header, USE without PROCEDURE or AFTER, ...) are
held open in `FORMAT_ONLY`: the lineage cannot prove them until the catalog numbers each format's alternatives, and a new
window resting on a general format alone is refused. For two of them a 2023 general rule describes the phrase but is
rightly KEPT (ORGANIZATION RECORD SEQUENTIAL, arithmetic-expression subscripts): the semantics are 1985's, only the
syntax is new, and that is the diagram's.

Self-review: one reader of the change-record kinds (the schema's `chained` flag, no kind spelled in either module), the
citation parser reused, the word lists held as citations rather than an edition branch, a rule designator read by its prefix (`GR25e-f` is GR25), the views read once per audit, the planted self-test citations real locators of the 1985 text.
Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,713/22,713 · Unit 40,567/40,567 · Characterization 37/37 cases ran (skipped 0)` (run 000001-20261109T014218Z, at the first checkpoint; the branch head is re-gated after the self-review fixes).
