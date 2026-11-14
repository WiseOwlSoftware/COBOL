## PB5254: at COBOL-85 a PERFORM in a USE BEFORE REPORTING procedure may not reach a GENERATE, INITIATE or TERMINATE

§1985:XIII.4.9.3 SR3 has two sentences. The first, that GENERATE, INITIATE and TERMINATE "must not appear in a paragraph within a
USE BEFORE REPORTING procedure", was always refused (COBOL2378). The second, "A PERFORM statement in a USE BEFORE REPORTING
procedure must not have GENERATE, INITIATE, or TERMINATE statements in its range", had no site: at `--std 85` a declarative
paragraph that PERFORMed a paragraph of another declarative section holding a GENERATE compiled and ran, and the performed
GENERATE silently did nothing (the run-time EC-FLOW-REPORT of §2023:14.9.49.4 GR10, kb/Work PB326). I re-probed the note's program
on this build: exit 0 at 85 and at 2002 before the fix.

**Fix.** `VersionConformancePass.GateBeforeReportingPerformRange`, run below 2002 beside `GateReleaseReturnRange`, asks the bound
program: for each out-of-line PERFORM written in a USE BEFORE REPORTING section it takes the static range from
`ProcedureReach.Closure` (§1985:VI.6.21.4 GR11, transitively through PERFORMs the range holds; GO TO not followed) and reports the
first GENERATE, INITIATE or TERMINATE in it at the PERFORM, COBOL7234. A report statement in a USE BEFORE REPORTING paragraph is
the first sentence's and is skipped, so nothing is reported twice. 2002 and later keep only the first sentence (`cite.py
--edition 2002 --find`), so the program still compiles there. A section range, a THRU range and a transitive PERFORM are refused;
a PERFORM of a paragraph with no report statement runs.

**Goldens.** `pb5254-perform-range-generate-85` and `pb5254-perform-range-transitive-terminate-85`, refused at 85. The existing
`14.9.49-USE/pb326_flow_report_unconditional` was an 85 golden whose program is now (correctly) refused there; it is accepted from
2002 with the same `.out`.

**Row.** `1985-SR-XIII.4.9.3-3` PARTIAL to CONFORMS (1985 GAP 172 to 171). `docs/REPORT_WRITER_DESIGN.md` states the 85
compile-time half beside the run-time rule.
