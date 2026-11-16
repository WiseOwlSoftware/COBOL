## Entry 3743 — 2026-11-16 02:11 PST — Seven mechanisms of the report section binder: a CODE record, a multiple LINE, index CONTROL, SUM ROUNDED, GROUP INDICATE, 1985 citations and a shared name

Wave 20262063, group B (kb/Work PB5788, PB5651, PB5774, PB5644, PB5806, PB5627, PB5775). Each note was re-probed on the base (bc2127f96c) and reproduced.

**PB5788.** A report file with `CODE "AB"` and `RECORD CONTAINS 2` wrote `ABW`: `ResolveReports` gave the line `Math.Max(1, record - code)` positions. A record exactly the code's size is the code alone; a
record shorter than the code cannot hold it and is refused COBOL7441 (§1985:XIII.3.6.4 GR1, §2023:13.18.12.4 GR1). Goldens at 85 and 2002+, and a refusal at all four editions.

**PB5651.** The SR10 nesting check counted a multiple LINE clause's repetitions as an OCCURS clause, refusing an OCCURS DEPENDING entry under a multiple-operand LINE (COBOL2021) at 2002 and at 2023.
`ReportOccursSpec.FromMultipleLine` marks them and the check reads the written OCCURS frames. The note's repro becomes golden `g2src04_source_suppressed_before_printed`; row `2002-GR-13.16.51.3-4`
goes DIVERGES to CONFORMS.

**PB5774.** At 85 an index data item in CONTROL was refused on §2023:13.18.60.3 SR10 though §1985:XIII.3.7.4 GR3b provides for it. Lifting the refusal alone compiled a report that never broke: an index item has
a zero-digit profile, so the character-image control key was the same empty string for every value. The emitter now keys an index CONTROL on its value; the golden prints `G1 CF G2 CF DONE`.

**PB5644.** SUM ... ROUNDED without a COLUMN clause is a 2014 rule (§2014:13.18.54.2 SR3); §2002:13.16.52.2 has none. `EditionInfo.SumRoundedRequiresColumn` guards it. Golden `g2sum06`; the `pb883` refusal now lists 2014 and 2023.

**PB5806.** At 85 GROUP INDICATE without a COLUMN clause compiled (kb/Work PB5243 had read Table 1 as the whole rule). §1985:XIII.3.13.3 SR1 and §1985:III.2 need a printable item, so it is refused; PB5243's golden lost its `G3` entry.

**PB5627.** At 85 the OCCURS bans of the key clauses, FILE STATUS and LINAGE, the RELATIVE KEY item rules, the FILE STATUS item rule and the ambiguity verdict cited 2023 rules. `ClauseOperandRules1985` holds the 1985 designations
(`Rule1985`), five `FileControlKeyRules.Rules1985` rows replace their 2023 rows, and the sum-counter ambiguity draws one verdict instead of two. The two 1985 drift tests accept chapter IV's headings and read the new designations.

**PB5775.** A report group or entry name and a same-spelled data item were counted in two separate lookups and never together, so GENERATE, USE BEFORE REPORTING, SUM UPON, SOURCE and an ordinary MOVE compiled over the shared name.
`DataBinder.QualifiedCandidates` and `NameResolution.ResolveUnqualified` count a report entry of the same spelling and source element with the data items, and `ReportGroupResolution.Resolve` counts the data items with the
report groups; the qualified spelling `DA OF R-1` is the way out. Rows `FMT-14.9.16.2` and `FMT-14.9.49.2` go PARTIAL to CONFORMS.
Main re-recorded five 2023 rows PARTIAL for PB5775 and PB5788 while this was in review (the 1985 code reads); the same fix closes them, so `SR-13.18.53.3-4`, `SR-14.9.49.3-9`, `SR-13.18.16.3-2`, `SR-14.9.16.3-1` and `GR-13.18.12.4-2` go back to CONFORMS, and a CONTROL homonym (`pb5775-control-homonym`, and its 85 twin) witnesses the CONTROL arm.

COBOL7441 used; COBOL7442 and COBOL7443 returned unused. Gate: see the landing record.

