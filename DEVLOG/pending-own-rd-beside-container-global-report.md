## A contained program's own RD of a container's GLOBAL report-name keeps the container report's groups and sum counters

kb/Work PB6277. A contained program that declared its own `RD R1` while its container declared `RD R1 GLOBAL` lost every
sum counter and report group of the container's report: `SymbolTable.InheritReport` returned at the failed
`_visibleReports.TryAdd` before `ReportScope.Inherit` and the sum-counter loop, so `TOT` (a counter of the container's
control footing) was refused COBOL1639 "not defined" in the contained program, though by ISO §2023:8.4.6.2.1 GR1/GR2 and
§2023:13.18.27.4 GR1 it identifies exactly one item. Re-probed on this tree before the fix: `DISPLAY TOT` and `TOT OF R1` both
refused COBOL1639.

Fix: `InheritReport` always inherits the report (scope entry, every sum counter); the `TryAdd` only decides whether the
spelling names the nearer report, so a reference to the report-name itself still names the program's own R1 (GR3a) while
the container's groups and counters stay reachable and are ranked by `NearestInScope`. The method is now `void` (its one
caller ignored the result). `docs/REPORT_WRITER_DESIGN.md` states the rule.

Probes: a container whose report summed 10 read `TOT` = 010 in the contained program; `ADD 1 TO TOT OF R1` then a
contained `GENERATE` of the container's detail group gave 016, and the container's control footing printed 016.

Golden: `conformance-suite/cases/8.4.6.2-Local-and-global-names/pb6277-own-report-name-beside-container-global-report-sum-counter`
(accepted at 85, 2002, 2014 and 2023; run at each by probe: `I TOT=04 / I TOTCFG=04 / O TOT=04`). No refused-below
program: the rule begins at COBOL-85 (§1985:X.1.3.8.2). The sibling arms (own FD, own data/condition/index names against
a GLOBAL report) already keep the report visible through `_hiddenReportNames`. Registry: closes_rows [] with reason, no row
changed verdict.
