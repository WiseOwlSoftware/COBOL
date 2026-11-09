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

Self-review: one reader of the change-record kinds (the schema's `chained` flag, no kind spelled in either module), the
citation parser reused, the word lists held as citations rather than an edition branch, a rule designator read by its prefix (`GR25e-f` is GR25), the views read once per audit, the planted self-test citations real locators of the 1985 text.
Gate: `=== BUILD-LOCAL GATE: GREEN — Conformance 22,713/22,713 · Unit 40,567/40,567 · Characterization 37/37 cases ran (skipped 0)` (run 000001-20261109T014218Z, at the first checkpoint; the branch head is re-gated after the self-review fixes).
