## Filed the leads of eight fix-lane reports as PB4151-PB4163

The lead-filing clerk (units/enqueue.md step 5) turned the new defects and the non-blocking review leads of eight finished fix-lane reports into thirteen `kb/Work/` notes and one extension. No code changed.

- PB4151 (defect, TRIVIAL): `RequiredImperativeStatementDriftTests.TheFormat3Statement_WithEveryBodyWritten_IsAccepted` asserts `ok || no COBOL0001`, so another refusal code or a run-time failure passes. From the PB3948 report, R1-1.
- PB4152 (defect, TRIVIAL): `CompileTimeExpressionEvaluator.IsRefusedName`'s summary sits inside `SoleDataRef`'s doc comment. From the PB3429 report, R1-1 (its `DataBinder.WrittenText` forwarder lead is already held by PB2733).
- PB4153 (defect, MINOR, misleads_diagnosis): SELF and SUPER at COBOL-85 draw the 2002 OO rules, not the undefined reference. From the PB1906 report, new defect 1.
- PB4154 (analysis, MINOR): `dref.cobolWord()` is the qualifier in the LINAGE-/LINE-/PAGE-COUNTER alternatives and about 40 sites should ask `ReferenceResolver.WrittenWordOf`. From the PB1906 report, new defect 2.
- PB4155 (defect, MINOR, misleads_diagnosis): `CALL ... USING NULL` at COBOL-85 draws the Format-2 wording; PB1673 recorded it as a lead and no open note held it. From the PB1906 report, item 3, and PB1673.
- PB4156 (defect, MINOR, misleads_diagnosis): `VersionConformancePass` skips the extended-letter gate for the whole word below 2002, and nothing pins the pass half of PB4130's fix. From the PB4130 report, R1-1 and R1-2.
- PB4157 (analysis, MINOR): `CompilationInputs.LocateFile` returns two path strings for one file, so a circular COPY is missed by one level. PB4158 (analysis, MINOR): its entry-spelling directory listing is an ambient read the compiled-program cache and depfile do not record. PB4159 (defect, TRIVIAL): its wildcard comment's macOS claim is false. From the PB1680 report, R1-1, R1-2 and R1-3.
- PB4160 (defect, MINOR, misleads_diagnosis): about forty non-SET messages open `VERB ... PHRASE`; one note with a generalized drift test. PB4161 (defect, TRIVIAL): the reworded COBOL1666 SET LOCALE message is pinned by no test. From the PB4121 report, the sweep and R1-1.
- PB4162 (defect, MAJOR, wrong_answer): a table(ALL) function argument is never held for GR2's order and has no recorded operand (LENGTH 4 against 8). PB4163 (defect, TRIVIAL): `HeldFunctionArgumentDescriptionTests` cites §14.6.13.1.3, which does not raise EC-ARGUMENT-FUNCTION. From the PB4124 report, new defect 1 and R2-1.
- PB4025 extended: the PB1906 report's carried-over inert-watch flake is a second sighting of PB4025's.

Skipped: PB4146's R1-1 and R1-2 are already held by PB4149 (open); PB1906's `LENGTH OF V (1)` item is held by PB2867 (landed: `LENGTH OF V (1)` measuring V itself stays the SR4 cycle); PB4126 (R1-2 answered fixed), PB4092 (an observation), PB851, PB4096 carry no lead (PB4096's partition-bound lead rests on PB4146, which is not yet landed: its pull request #844 is queued).
