## Entry 2808 — 2026-10-30 14:11 PDT — PB4121: the three SetBinder "no receiving operand" guards stop opening with an elided SET, and the drift test sees BoundRejected.Report

**Mechanism.** `SetDiagnosticNamesReceiversDriftTests.ElidedEcho` keyed on `Error(` only, so a `BoundRejected.Report(ctx.Edition, code, "SET …")` site was invisible to it; that blind spot is how `PtrBinder`'s message survived to PB1314 and three `SetBinder` guards survived after it.

**Re-probe.** The guards (`BindSetFunctionAddress`, `BindSetEntry`, `BindSetProgramAddress`) cannot be reached from source: all three grammar rules print `setReceivingIdentifier+`, so `SET TO ENTRY 'X'` stops at `COBOL0001`. They are defensive, so the drift test is their witness and no conformance golden exists for them.

**Fix.** The messages now name the statement as written (`SET TO ENTRY with no receiving operand`), the form `PtrBinder.BindSetUpDown` already uses. The regex now matches `Error(` and `Report(` forms and takes the optional leading edition argument; `ThePatternThisTestKeysOn_FiresOnThePB388Defect` gained the Report-form defect and its repaired twin, and was seen RED with one message reverted (`SetBinder.cs:1011`). Sibling fixed: `BindSetLocale`'s duplicate-category message elided the earlier categories it held (`SET LOCALE … LC_TIME`); it now reads `SET LOCALE LC_TIME LC_TIME: …`.

**Not fixed.** About forty messages in other binders open `VERB … PHRASE` (CALL, READ, WRITE, START, INSPECT, OCCURS DEPENDING ON, CODE-SET). They elide a phrase, not the receiver list, which is a separate mechanism; reported as a lead.

**Rows.** None claimed or closed (`closes_rows: []`, message quality).

