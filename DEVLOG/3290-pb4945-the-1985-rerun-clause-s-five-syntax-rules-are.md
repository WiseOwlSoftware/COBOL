## Entry 3290 — 2026-11-07 18:34 PST — PB4945: the 1985 RERUN clause's five syntax rules are screened, and the obsolete I-O-CONTROL clauses resolve their names

kb/Work PB4945 (wave 20261917 group A). The X3.23-1985 RERUN clause was parsed and ignored. Since PB4919 its RECORDS
phrase had to name file-name-2, but none of §1985:VII.2.12.3's five syntax rules was checked. The re-probe on the base
build compiled every violation clean at `--std 85`: ON naming a relative file (SR1), END OF UNIT of an indexed file
(SR2), RECORDS or CLOCK-UNITS with no ON phrase or with ON file-name-1 (SR3), two RECORDS or two END OF REEL/UNIT phrases
naming one file (SR4 a, b), and two CLOCK-UNITS clauses (SR5). The sibling sweep found the same root three more times. The
clauses' operands were never resolved, so an undefined file-name-2, an undefined MULTIPLE FILE TAPE file-name and a
condition-name-1 that names no switch status also compiled clean.

`FileControlBinder.ScreenRerunClauses` now runs once after the whole I-O-CONTROL paragraph, because SR4 and SR5 compare
clauses. The ON word is file-name-1 when it names a visible file and implementor-name-1 otherwise. The new codes are
COBOL6529 (SR1, SR2), COBOL6530 (SR3, and GR2 f/g: the condition-name-1 forms both write an ON phrase) and COBOL6531 (SR4
a/b, SR5). `ResolveIoControlFile` resolves every RERUN and MULTIPLE FILE file-name, and condition-name-1 resolves against the
SPECIAL-NAMES switch statuses. Both report COBOL1639. Every rule is asked at every edition, as COBOL6496 is, because
`--permissive` accepts the removed clause from 2002 on. The 1985 SAME rules (§1985:VII.2.13.3) were already enforced through
their 2023 counterparts (COBOL2738), and the re-probe at 85 confirmed it.

Four existing programs wrote `RERUN ON <file> EVERY n RECORDS` or `CLOCK-UNITS`, which SR3 forbids: three in
`Ansi85AcceptanceTests`, plus the `rerun-removed-2002` constructs.json source. A fifth, `cases/_unassigned/rerun.cob`, did
the same. Each now names the implementor-name SYSRR. The constructs.json description also stated `n RECORDS [OF f]`, which
PB4919 had already corrected in the grammar.

Goldens: `cases/12.4.6-I-O-CONTROL/pb4945_rerun_seven_forms_85` (the seven GR2 forms, accepted at 85) and eleven
`pb4945-*` refusals, one per rule plus the undefined-name siblings, refused at every edition. No inventory row exists for
the 1985 clause yet (PB4886 builds it), so the note closes none. Self-review covered the rule text against the printed
1985 page, the arm of every phrase, the null-context guards after parse recovery, and the allocations (one pass, two small
sets per paragraph).

