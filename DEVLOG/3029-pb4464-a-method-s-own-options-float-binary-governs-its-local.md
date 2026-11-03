## Entry 3029 — 2026-11-03 04:43 PST — PB4464: a method's own OPTIONS FLOAT-BINARY governs its LOCAL-STORAGE items

Wave 20261841, group A. kb/Work PB4464 suspected, unprobed, that a method's own `OPTIONS. FLOAT-BINARY DEFAULT IS HIGH-ORDER-RIGHT.`
did not reach the endianness of its `FLOAT-BINARY-32` items.

**Re-probe.** On a build of main with the PB2296/PB2352 decomposition merged in (the DataBinder extract of #1045, taken from the
resolved merge of a sibling lane), a class with no OPTIONS and a method `M1` with the clause and a `FLOAT-BINARY-32` local VALUE 1.5,
moved to `PIC X(4)`: FUNCTION ORD of bytes 1 and 4 printed `64 1` (big-endian). ISO §11.9.8.3 SR3 (checked with cite.py) implies
HIGH-ORDER-RIGHT for the method's items, and §13.18.60.4 GR19 b) makes that little-endian, `1 64`. Reproduced: a silent wrong byte
order. The sibling method with no OPTIONS printed `64 1` correctly.

**Root cause.** `DataBinder.BindEntry` read `State.Configuration.Options.FloatBinaryEndianness`, the class half's slice, for every
entry. `OoDataBinder.BindMethodData` bound the method's own model (`OoMethodSymbol.MethodOptions`, folded over the class's per
§11.9.4 GR1) but only the statement binder (`ProcedureBindState.Options`) and the emitter (`EmitContext.Options`) ever read it. The data
phase, which describes the method's LOCAL-STORAGE and LINKAGE entries, had no reader.

**Fix.** `OoDataBinder.EntryOptions` (`BindingMethod?.MethodOptions ?? Options`) is the data phase's one answer for an OPTIONS fact an
entry's description reads; `BindEntry` takes the endianness from it. `BindingMethod` was already the cursor naming the method whose data
division is binding, so no new scope was added; the design (DESIGN-architecture-review §8.3 (1)) and the `ConfigurationSlice` summary say
so. `EntryOptionsReaderDriftTests` pins that the entry binder never reads the unit's slice directly.

**Sweep.** Every consumer of an `OptionsModel` field in `src/`: `Arithmetic`, `DefaultRounding` and `IntermediateRounding` are read by
statement binding and emission through the scoped overrides; `Initialize` is read by `StorageFormPass` (already `MethodOptions ??`) and
`InitialStateBackground` (emission scope); `FloatBinaryEndianness` at `BindEntry` was the only data-phase reader.

**Goldens.** `conformance-suite/cases/11.9.8-FLOAT-BINARY/pb4464_method_options_float_binary` (accept 2014, 2023; free form): a method that
overrides the class's clause both ways, one that inherits it, one with an OPTIONS paragraph without the clause, an item that writes its
own endianness-phrase under the method's clause, and the object's own data read from an overriding method (it keeps the class's
clause). Every clause is written, so no leg rests on SR1's implementor choice. Its refusal below 2014 is
`pb4464_method_options_float_binary_below_2014` (COBOL0900 at 2002). The witness-only inventory batch adds the case and the code site to
SR-11.9.8.3-2, SR-11.9.8.3-3 and GR-11.9.4-1; no row changes verdict (all CONFORMED), so `closes_rows` is empty with its reason.

**Merge onto the extracted DataBinder (wave 20261845).** The branch was built on the pre-#1054 integration branch; merging main (the
DataBinder extract, DEVLOG 3024) conflicted in `OoDataBinder.cs` and `PtrBindBasedAndAddressablesPass.cs` only, where main had renamed the
method definition accessor to `m.Ctx`; main's structure was kept around the `EntryOptions` property. The branch's own stamped entry (3015)
is folded into main's 3024 and is not carried. The review's lead R1-1 is fixed: `EntryOptionsReaderDriftTests` scanned only
`DataBinder.cs`, and now scans every `DataBinder*.cs` partial plus `LinkageBinder.cs` and `ConstantEntryBinder.cs`. The merged build
prints the golden's six expected lines (`cobol --source-format free --run`).

