# Report Writer CONTROL-break + SUM engine — implementation design (WS-SPEC #6.4/#6.5)

Worked out 2026-06-05 (DEVLOG 349, after RW sub-stages #6.1-#6.3). This is the headline Report Writer feature:
CONTROL break detection + CONTROL HEADING/FOOTING presentation (#6.4), then SUM accumulators (#6.5). Implement
directly on main, guard-gated. Symbol model is already populated: `ReportSymbol.ControlFields` (major→minor,
"FINAL" first if present), `ReportGroupSymbol.ControlField` (the CH/CF group's control name or "FINAL"),
GroupKind ControlHeading/ControlFooting (SemanticBuilder.cs:936-937, 984-985, 1040-1042).

## Slot generalization (do FIRST — refactor, no behavior change; verify the 8 RW tests stay green)
`ReportWriterRuntime.ReportContext`: replace `int[] GroupLines` / `List<FieldPlan>?[] GroupFields` (fixed 4) with
`Dictionary<int, AutoGroupPlan> Groups` where `AutoGroupPlan { int Line; List<FieldPlan> Fields = []; }`.
Slots: PH=0, PF=1, RH=2, RF=3; `ControlHeadingSlot(L)=4+2L`, `ControlFootingSlot(L)=5+2L` (L = control level,
0 = most major). Update RegisterAutoGroup (`Groups[slot]=new AutoGroupPlan{Line=...}`), RegisterAutoField /
RegisterAutoDataField (`Groups.TryGetValue(slot, out var p)` then `p.Fields.Add`), and PresentAutoGroup
(`if (Groups.TryGetValue(slot, out var p)) PresentGroupPlan(ctx,p);` — extract the existing body to
`PresentGroupPlan(ctx, AutoGroupPlan)`). Remove the `slot<0||slot>3` guards. This reuses the ENTIRE existing
registration + IR (IrReportRegisterDataField) + emitter + lowering path for CH/CF — only the engine below is new.

## Runtime state
```
class ControlInfo { bool IsFinal; byte[]? Area; int Offset; int Size; byte[]? Prior; } // Prior null until 1st GENERATE
ReportContext.List<ControlInfo> Controls;  // major→minor; FINAL (if any) is index 0
```
`RegisterControl(string report, bool isFinal, byte[] area, int offset, int size)` → `Controls.Add(new ControlInfo{
IsFinal=isFinal, Area=isFinal?null:area, Offset=offset, Size=size})`. Helpers: `SnapshotControl` (copy Area[Off..Off+Size],
empty for FINAL), `RestoreControl` (write bytes back into Area — used for the CF prior-value dance), `BytesEqual`.

## The engine
`ProcessDetailControls(ctx)` — call from **EmitGroup AFTER the page-start (RH/PH) block, BEFORE the detail write**
(ordering: RH, PH, CH, detail — §14.9.16.4 GR4). n=Controls.Count; return if 0.
- **First detail** (`Controls[0].Prior == null`): snapshot all controls into Prior; present every CONTROL HEADING
  major→minor (`for i in 0..n-1: PresentAutoGroup(ctx, ControlHeadingSlot(i))`); return.
- **Subsequent**: breakLevel = the most-major NON-FINAL level whose Snapshot != Prior (scan i=0..n-1, skip IsFinal,
  break on first inequality); if none, return. On a break at L:
  1. save current = Snapshot(all); RestoreControl(all, their Prior)  ← so CF SOURCE shows the ENDING group's values
  2. present CONTROL FOOTINGs minor→L: `for i=n-1 downto L: PresentAutoGroup(ctx, ControlFootingSlot(i))` (§13.18.57 GR6e1)
     — for #6.5, each CF that prints a SUM counter prints it, then resets counters at level i.
  3. RestoreControl(all, current)
  4. present CONTROL HEADINGs L→minor: `for i=L to n-1: PresentAutoGroup(ctx, ControlHeadingSlot(i))` (GR6c2)
  5. Prior = Snapshot(all)

`ProcessTerminateControls(ctx)` — call from **TerminateReport BEFORE the REPORT FOOTING**. If nothing generated
(`Controls.Count==0 || Controls[0].Prior==null`) return. Present every CONTROL FOOTING minor→major
(`for i=n-1 downto 0: PresentAutoGroup(ctx, ControlFootingSlot(i))`) — the final break (§13.18.16.4 GR5); control
items already hold the last group's values, so no restore.

## Lowering (FileIoLowerer.LowerInitiate)
- Extend the GroupKind→slot switch with ControlHeading→`4+2*level`, ControlFooting→`5+2*level`, where
  `level = report.ControlFields.FindIndex(case-insensitive == g.ControlField)`; skip if level<0. (Reuses the
  existing RegisterAutoGroup/RegisterAutoField/IrReportRegisterDataField field-emission loop unchanged.)
- After the group loop, register the controls in order: `foreach cf in report.ControlFields`: if "FINAL" →
  `IrReportRegisterControl(report.Name, isFinal:true, null)`; else resolve `cf` to a location → `IrReportRegister
  Control(report.Name, false, loc)`.

## New IR + emitter
`IrReportRegisterControl { string ReportName; bool IsFinal; IrLocation? Source; }`. Emit: Ldstr ReportName;
Ldc_I4 (isFinal?1:0); if Source!=null EmitLocationArgs(Source) else (Ldnull, Ldc_I4_0, Ldc_I4_0);
Call RegisterControl(string,bool,byte[],int,int). Add the CilEmitter dispatch case (next to IrReportRegisterDataField).

## #6.4 test (no SUM): single-level break, CF shows the control value via SOURCE
`RD … CONTROL IS WS-DEPT`; `01 DET TYPE DETAIL LINE PLUS 1. 03 COL 1 PIC X SOURCE WS-DEPT.`;
`01 CF-D TYPE CONTROL FOOTING WS-DEPT LINE PLUS 1. 03 COL 1 PIC X(3) VALUE "CF-". 03 COL 5 PIC X SOURCE WS-DEPT.`
Generate DEPT A, A, B. Expected order: A, A, "CF-? A" (break A→B, prior A restored), B, "CF-? B" (TERMINATE).
Assert via IndexOf order: detail A < CF-A < detail B < CF-B, and the CF after A shows "A" (prior-restore works).

## #6.5 SUM (next): a CF field `… PIC 9(4) SUM WS-AMT`
- New FieldPlan kind 4 = SUM counter (keyed by a counter id). ReportContext: `Dictionary<string,decimal> SumCounters`.
- Register: a report entry with a SUM clause → a named counter (entry name); CF SUM field = kind 4 referencing it.
- LowerGenerate emits `AddToSum(report, counterId, addendLocation)` for the detail's SUM addends (§13.18.54.4 GR7).
- PresentGroupPlan kind 4: format SumCounters[id] to the field PIC, place; the counter resets at end-of-group /
  at its RESET control level (GR2) — reset in ProcessDetailControls step 2 at the CF's level.
- Decimal accumulation; numeric-edited SUM fields reuse the eventual numeric-edit-in-report-fields path.

## Notes / risks
- Positioning: CF/CH present at their LINE value via PresentGroupPlan (adv = Line - LineCounter, clamp ≥1). For
  the simple no-overflow tests this is fine; the full page-fit interaction of CF/CH with FIRST DETAIL/overflow is
  a later refinement. Keep tests single-page.
- RW101A-104A baselines use only DETAIL/PH/PF — no CONTROL — so the engine isn't entered for them; guard must stay
  green (the Dictionary refactor is the only thing touching their path — verify the 8 RW spec tests first).
- FINAL: never breaks mid-report (skipped in the break scan); FINAL CH at first GENERATE, FINAL CF at TERMINATE.
