## Entry 4073 — 2026-11-27 17:18 PST — NumericDisplayForm moves to compiler.model; validation asks typed file-name and report-name questions (PB6172, PB6173)

Wave 20262172, group A (fix lane, cluster PB1754). The group's five notes are five mechanisms. This branch finishes the two
layering moves the 9e65164eb census found, PB6172 and PB6173, and leaves PB6169, PB3231 and PB2622 for dispatches of their own.

**PB6172.** Re-probe: `grep NumericDisplayForm src` found the enum declared in `Binding/NumericDisplayForm.cs`
(compiler.binding). It was read by `AcceptDisplayEmitter`, `EmitCore` and `OperandText` (code generation), by
`DataBindResult` (compiler.model) and by the CLI. The enum and its parser `NumericDisplayFormOption` move to
`Binding/Model/NumericDisplayForm.cs`, namespace `WiseOwl.Cobol.Binding.Model`, beside the OPTIONS types that PB2402 moved.
Every reader changes in the same change. After the move the CLI names nothing in compiler.binding, so the edges file's
`cli → compiler.binding` allowance (which existed only for this enum) becomes `cli → compiler.model`. Both are allowed by the
same composition-root rule, and DESIGN-architecture-review §8.1 "The file as measured" now says so. The edges ratchet
deleted the three walk pairs that named `Binding.NumericDisplayForm`.

**PB6173.** Re-probe: `FlagConformancePass.cs` had `using WiseOwl.Cobol.Symbols; // NameClass` and called
`Symbols.Identify(name, NameClass.File, …)` and `Identify(report, NameClass.Report, …)`. `SymbolTable` gains the typed
questions `IdentifyFile` and `IdentifyReport`. The sibling sweep (`grep "NameClass\.File\b|NameClass\.Report\b"` over
Binding and Validation) found 23 callers in 14 files (the flag pass and 13 binder files) that asked `Identify` for those two classes and then matched the symbol
as `FileModel` or `ReportModel`. All of them now ask the typed question, so each question has one spelling. The flag pass
names no compiler.symbols type, and six binders drop a `using WiseOwl.Cobol.Symbols` they no longer need.

**The census.** Both layering pairs were seen only by the census, because an enum constant compiles to an integer and the
IL walk does not see it. So a census-only row ends only when a census is recorded in the landing (the PB4319 shape). This
branch records the census of its code head, and the ratchet, keyed to that census, lowers `CodeGen → Binding` from 70 to 69,
`CodeGen.Emit → Binding` from 25 to 23 and `Binding.Model → Binding` from 16 to 15, and deletes the `Validation → Symbols` row.
The remaining use above the 68 PB6172 expected does not come from this enum.

No inventory row is involved (behaviour-neutral layering), so nothing is added to the conformance suite. The full gate is
this change's proof.

