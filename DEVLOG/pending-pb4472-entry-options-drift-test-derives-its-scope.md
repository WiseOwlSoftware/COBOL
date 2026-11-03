## PB4472: the entry-options drift test derives the entry binder from the tree and proves it reads every partial

**Mechanism.** `EntryOptionsReaderDriftTests` (PB4464) claims the entry binder never reads the unit's OPTIONS slice
(`State.Configuration.Options`), because ISO §11.9.4 GR1 makes a METHOD's own OPTIONS paragraph govern its entries. Review lead R1-1
found it scanned `DataBinder.cs` alone though the entry binder is the partial class `DataBinder` across 19 more files; the
PB4464 successor merge already widened it to `DataBinder*.cs` plus `LinkageBinder.cs` and `ConstantEntryBinder.cs`.

**Re-probe.** No file in that set reads the slice today (the test passes on the merged tree). Two weaknesses remained: the two
non-partial binders were a hand list (DESIGN-architecture-review 8.3 (1): derive, do not name), and the check was the literal text
`Configuration.Options`, so `var c = State.Configuration; c.Options.X` or a bare `Options.X` in a partial passed it.

**Fix.** The scan set is now every top-level `Binding/*.cs` declaring `partial class DataBinder` or a class built over a `DataBinder`
primary-constructor parameter (excluding `OoDataBinder.cs`, where `EntryOptions` is defined, asserted by the second fact); the test also
asserts every `DataBinder*.cs` file by name is in it. A mention is any whole-word `Options` in comment-stripped code, however spelled; the
single legal one is the unit binder writing its own slice (`configuration.Options = ...`, `DataBinder.cs`). A Theory
(`ASlicePlantedInAnyPartial_IsReported`) runs the reader function over seven planted and legal lines.

**Witness.** A read planted in a real partial (`DataBinder.SignClause.cs`, `State.Configuration.Options.FloatBinaryEndianness`) turned the
fact red, naming `DataBinder.SignClause.cs`; reverted.

**Register.** PB4472 landed, `closes_rows: []` (process-only drift test).
