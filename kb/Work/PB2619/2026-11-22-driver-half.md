## 2026-11-22 — wave 20262135 (C): the driver half landed; PB2619 is closed

**Re-probe (pin 2061bdb7bb):** the CLI half (2026-11-21, wave 20262133) stood: `--std`'s spellings, error text, help
default and listing default read `EditionInfo.All`, `EditionInfo.Latest` and `EditionInfo.RequiresSourceListing`. What
remained was the driver boundary: `CompilerDriver.Options` carried `int DialectLevel = 2023` and `bool Permissive = false`,
the driver's own default edition, and `CliOptions` carried the same two loose values.

**Landed:** `CompilerDriver.Options` carries a REQUIRED `Editions.EditionInfo Edition` (its second positional parameter) in
place of `DialectLevel` and `Permissive`, so the driver has no default edition and a year outside `EditionInfo.All` is
unrepresentable past the composition root. `CliOptions` carries the same value, built once by `EditionInfo.Of(std,
permissive)` in `Program.BuildParser`'s resolver; check-batch builds it the same way. Every caller changed in the same
change (rule 4): 149 construction sites in the compiler, the CLI and the tests, rewritten mechanically from the build's
own error list (a positional or named `DialectLevel`/`Permissive` became `EditionInfo.Of(level[, permissive])`; an
omitted one, the old default, became `EditionInfo.Latest`), plus the conformance runner's `std` build-step option (`with
{ Edition = … }`). `CompilerDriverTests.CompilerDriver_DefaultsToLatestEdition` is deleted: the driver has no default to
pin, and the CLI default it described is pinned by `CliParserTests` (`Resolve("a.cob").Edition.Year == 2023`).

The listing's options section keeps its two rows, `--std` and `--permissive`, now named by the property paths
`Edition.Year` and `Edition.Permissive` (`OptionsSectionBuilder.EditionYear`, `EditionPermissive`), and
`OptionsSectionDriftTests` expands an `EditionInfo` property into those two rows, so "every option has exactly one row"
stays true. `CompileStateDriftTests` maps `Options.Edition` to both `CompileState.Edition` and `CompileState.Permissive`.

**Sites:** `src/WiseOwl.Cobol.Compiler/CompilerDriver.cs` (`CompilerDriver.Options`, `CompilerDriver.Compile`,
`CompilerDriver.NewFrontend`, `CompilerDriver.CompileCore`), `src/WiseOwl.Cobol.Compiler/OptionsSectionBuilder.cs`
(`OptionsSectionBuilder.Table`, `Defaults`, `EditionYear`, `EditionPermissive`),
`src/WiseOwl.Cobol.Compiler/ExpansionAssemblies.cs` (the expansion's own compilation), `src/WiseOwl.Cobol.Cli/CliOptions.cs`
(`CliOptions.Edition`), `src/WiseOwl.Cobol.Cli/Program.cs` (`Program.BuildParser`'s `Resolve`, `Program.CheckBatchVerdict`,
`Program.RunCompile`), `src/WiseOwl.Cobol.Compiler/Binding/Model/SkeletonRecord.cs` and
`src/WiseOwl.Cobol.Runtime/Repository/CobolSignatureAttributes.cs` (doc comments), and every test that builds options.

**Left, a sibling of the same shape (not this note's target):** `Frontend.Frontend` (`DialectLevel { get; init; } = 85`,
`Permissive`) and `Binding.EditionContext(int dialectLevel, bool permissive)` still take the edition as two loose values,
and the front end defaults to 85 on its own; the driver now hands both of them `options.Edition`'s parts.
