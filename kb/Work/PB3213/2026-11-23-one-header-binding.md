# PB3213, 2026-11-23: one procedure division header binding for every arm (wave 20262147 group B)

**Re-probe (base a23ed881c2 plus the w20262139d replay).** Structural; the sites had moved since the finding (the DataBinder
extract): the program/function arm is `LinkageBinder.CallBindLinkage` with `LinkageBinder.AnyLengthValidateUnit`, the method arm
`OoDataBinder.BindMethodData`. Both still threaded the BY REFERENCE / BY VALUE / OPTIONAL modes line for line, each reported
COBOL0888 at two sites, each screened BY VALUE and resident formals, and the ANY LENGTH sweep was written twice with its
COBOL1542 messages paraphrased per arm.

**Fixed.** `ProcedureHeaderBinder` (`src/WiseOwl.Cobol.Compiler/Binding/ProcedureHeaderBinder.cs`):
- `Bind` threads the using-phrase modes (§2023:14.2.3 GR4), identifies each operand (§2023:14.2.2 SR1; COBOL0888 from one
  site for USING and one for RETURNING), asks `ProcedureHeaderScreen` (SR1/SR2/SR5/SR6) and decides carrier residency
  (`LinkageBinder.FormalIsCarrierResident`), returning `ProcedureHeader` (the `HeaderFormal`s and the RETURNING item). The
  ADDRESS OF scan is asked only when the header has formals (now for the method arm too, kb/Work PB4302's rule).
- `SweepAnyLength` is the one §2023:13.18.2.3 SR2-SR4 sweep; the per-arm input is `ProcedureHeaderOwner` (outermost,
  contained, function, class data, method, property method).
- Each arm only names its carrier: the program arm builds `LinkageFormal` (class member carrier), the method arm `OoFormal`
  (method-local carrier). The carrier/area-pointer name and the area-formal rule are written once
  (`FormalCarrier.AreaPointerName`, `FormalCarrier.IsAreaFormal`), asked by `LinkageFormal.IsArea` and `OoFormal.IsArea`.

The two formal records stay two: their consumers (the program emitter's `CobolArg` carrier fields, the method ABI's
`ParamName`/`AreaParam`/`OmittedFlag`) differ by arm, and the shared facts now come from one `HeaderFormal`.

**Message changes (explained, no test pins them).** A method LOCAL-STORAGE ANY LENGTH item now reads the SR2 text the program
arm used ("only in an elementary level 1 entry in the LINKAGE SECTION"); the contained-program and method SR3 messages read
"in the procedure division header" (no "in a contained program" / "method's"). Codes unchanged.

Evidence: `compare_oracle.py --base aed6a7e6ef --no-build` → `=== ARCH-ORACLE: IDENTICAL — 23455 cases ===`.
`ByValueFormalScreenDriftTests` now pins that both arms call `ProcedureHeaderBinder.Bind` and the binder is the one
`ByValueClass` asker. Inventory: SR-13.18.2.3-2/-3/-4 and 2002-SR-13.16.2.2-2 re-sited from `#AnyLengthValidateUnit` to
`ProcedureHeaderBinder.cs#SweepAnyLength` (record_verdicts; GAP unchanged).
