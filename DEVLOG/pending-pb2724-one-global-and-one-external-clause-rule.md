## PB2724: the GLOBAL and EXTERNAL clauses are each one grammar rule, and the EXTERNAL AS phrase and GR5 default are each read once

**Why.** ISO §2023:13.18.22 (EXTERNAL) and §2023:13.18.27 (GLOBAL) are each one clause with one general format, which the data
description, file description, report description, screen description and constant entries print as the same bracketed
slot. The grammar spelled GLOBAL four times (`globalClause`, the GLOBAL arm of the fused FD rule `fileGlobalExternalClause`,
`reportGlobalClause`, and `IS? GLOBAL` inline in `constantEntryBody`) and EXTERNAL twice. Every reader paid for it:
`OoDefinitionRules.GlobalClauseSites` had one row per spelling with a per-type predicate, `ClauseCardinalities` had a
`KindOf` column that existed only to split the fused FD rule back into its two printed brackets, and the FD and the data
description entry each carried their own copy of the §2023:13.18.22.3 SR3 screen call and of the §2023:13.18.22.4 GR5
default. kb/Work PB511 already paid once for the fused rule: it hid the AS asymmetry between EXTERNAL and GLOBAL.

**The re-probe** (at 3b5bca46e4, main with the PB2627, PB5709 and PB3227 branches replayed): the four GLOBAL spellings and two
EXTERNAL spellings were all present. The two `ExternalizedName.Screen` calls with the same descriptor, tag, rule and
`rejectZeroLength` were at `FileControlBinder.FileSection.cs` (FD) and `DataBinder.cs` (BindEntry).

**The fix.** `fileDescriptionClause` references `externalClause` and `globalClause`, and `reportDescriptionClause` and
`constantEntryBody` reference `globalClause`. `fileGlobalExternalClause`, `reportGlobalClause`,
`ClauseCardinalities.KindOfFileDescriptionClause` and `ClauseList.KindOf` are deleted, and a clause list's element is a
`Type`. `GlobalClauseSites` keys on the host rule of the one `globalClause`. The screen description entry, which already
referenced `globalClause`, gets its own SR1 c) row; before, the data-description row matched it under the wrong entry name.
`ExternalizedName.AsLiteralOf` is the one SR3 reading of the EXTERNAL AS phrase and `ExternalClauseName.Of` (model layer) the
one GR5 default, used by the FD binder and by `DataItem.ExternalizedRecordName`. `VersionConformancePass.VisitGlobalClause`
(F.2 item 61) now checks that the clause's host is a data description clause, because the constant entry's GLOBAL reaches it too.

**Drift test.** `OoDefinitionRulesDriftTests.TheGlobalClauseSites_AreTheGrammarsGlobalClauseHosts` checks two things. First,
`globalClause` is the only rule that matches the GLOBAL token, apart from the USE statement's phrase. Second, the sites table
lists exactly the rules whose ATN calls `globalClause`. A second spelling, or a new host without a row, turns it red.

**Oracle.** `compare_oracle.py --base 3b5bca46e4`: 12 of 23581 cases differ, all in diagnostic text or column; no outcome
and no code changed. COBOL2604's list of admitted clauses now names the two printed brackets (`IS EXTERNAL clause, IS GLOBAL
clause`). COBOL1520 on a constant entry's GLOBAL is reported at the GLOBAL clause, as every other host already was.

**Rows.** The witnesses were re-sited from the deleted rules to their host rules: FMT-13.18.22.2, FMT-13.18.27.2,
1985-FMT-X.4.2.2, 1985-FMT-X.4.4.2 and 1985-FMT-X.4.5.2, with their prose restated. SR-13.18.22.3-3 and GR-13.18.22.4-5 gain
the two `ExternalizedName` members. Five more rows' prose was restated (GR-13.14.4-1, GR-13.18.22.4-5, GR-13.4.5.4-3,
SR-13.18.22.3-1, SR-13.18.22.3-3). GAP did not move: 2023 1 → 1, 1985 0 → 0. `closes_rows: []`.
