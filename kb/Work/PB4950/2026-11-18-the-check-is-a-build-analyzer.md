**2026-11-18: the one-decider check moves from a test into the build, as a Roslyn analyzer (gate red, run 000005).**

- **The red.** Main b4ed00ffc9 was merged in, and the gate went red (run 000005-20261118T114637Z): "WiseOwl.Cobol.Listing does not
  bind as the build binds it" (CS0534/CS7036/CS0117 in `Json/ListingJsonContext.cs`). Listing uses the System.Text.Json source
  generator, which the SDK supplies, and the test's hand-built compilation lacked its output. This was the third red from one
  cause: the test re-created the build (paths, then projects, now SDK generators), and every change to how main builds broke the copy.
- **Decision (the coordinator's ruling, rule 5): stop rebuilding the compilation.**
  - `src/WiseOwl.Cobol.Analyzers` is a new netstandard2.0 Roslyn component. It holds `EditionCiteAnalyzer` (WOCA0001 and WOCA0002,
    both errors).
  - `src/Directory.Build.props` (new; it imports the root props) loads it as an analyzer into every src project that is not a
    Roslyn component. The check therefore runs on the compilation MSBuild really builds.
  - Why a new project: no in-repo analyzer project existed. The only Roslyn component is `WiseOwl.Cobol.Compiler.SourceGen`, the
    compiler's bound-tree generator. Loading it everywhere would run that generator in every project.
  - The fixed-point logic moved unchanged, in a different shape: semantic-model actions collect the declarations and candidate
    choices, and the compilation-end action closes the fixed point and reports.
  - The seeds now come from `EditionInfo`'s symbol, not from reflection. The edition list is a constant, which
    `EditionCiteAnalyzerTests` pins to `EditionInfo.All`.
- **Decision: cross-project derivation travels by attribute, not by a seed list.**
  - An analyzer sees one compilation, and a referenced assembly has no bodies to derive from.
  - Every derived member another assembly can read must carry `[EditionDerived]` (WOCA0002). That means public members, and
    protected members of a public type; the products grant `InternalsVisibleTo` only to tests.
  - In the reading compilation, a marked member from metadata is derived.
  - The rule that requires the mark is the rule that computes the need for it, so the marked set cannot drift. A seed list would
    be a hand-kept list beside the code.
  - Seeds (`DialectLevel`, `Std`, `EditionInfo`'s own) need no mark.
  - The attribute is `WiseOwl.Cobol.Runtime.EditionDerivedAttribute`, in the runtime, the one assembly every product project
    that reads an edition references. It is listed in `PublicAPI.Unshipped.txt`.
  - The build marked 7 members, with no WOCA0001 anywhere:
    - `CobolNames.HasEditionSpelling`
    - `CobolWordRule.MaxLength`
    - `CobolParserCoreBase.is85`, `is2002`, `is2014`, `is2023` and `setLocaleAhead`
    - `EditionContext.MaxDigits`
- **Deleted.** `BuildSourceCompilations`, `DiscoverSrcProjects`, `RunSourceGenerators`, `TestRepo.MsBuildPath` (it had no other
  user), `TestRepoMsBuildPathTests`, and the drift_rules resolver category added for that helper.
  - `EditionCiteOneDeciderDriftTests` is now `EditionCiteAnalyzerTests` (Unit). Its planted sources run through
    `CompilationWithAnalyzers`: every shape and alias (the same 13 lines as before), the cross-assembly mark (WOCA0002 on an
    unmarked public and on a protected derived member; WOCA0001 on a string chosen on a marked member read from metadata), the
    edition list, and the error severity.
- **Proof.**
  - A src file planted in the Compiler, with a direct ternary and one on `CobolNames.HasEditionSpelling` (cross-assembly, by the
    mark), failed `dotnet build WiseOwl.Cobol.sln` with two WOCA0001 errors. It was then deleted.
  - The whole solution builds with 0 warnings and 0 errors.
- **Build cost.** Full rebuilds (`--no-incremental`), alternating: with the analyzer 11.3 s and 12.4 s; without it 12.1 s and
  19.5 s. That is inside the noise.
  - `-p:ReportAnalyzer=true` gives the analyzer's own time per project: Compiler 0.33 s (of 4.0 s for all analyzers), Runtime
    0.17 to 0.41 s (of about 30 s, mostly the public-API analyzer), Listing 0.23 s, Frontend 0.06 s, Editions 0.01 s, Cli
    0.001 s.
- **Other drift.**
  - `architecture-edges.json` excludes the analyzer as it excludes SourceGen: a build-time component that references no product.
  - The arch census was re-recorded at the committed head.
  - `CobolNameFoldDriftTests`: the analyzer's one case-insensitive compare, of a C# identifier, now carries its reason.
- **Sibling found and fixed here: the held-output target (kb/Work PB3674).**
  - A language server that loads a Roslyn component from `bin` holds the file, and the next build fails. Only SourceGen carried
    `MoveAsideHeldOutput`, and the guard asked each new component to copy it. The analyzer project would have been the second copy.
  - The target now lives once in `src/Directory.Build.targets`, conditioned on `IsRoslynComponent`. It is a targets file, so the
    condition sees the project's own property. SourceGen's copy is deleted.
  - `HeldAnalyzerOutputDriftTests` now requires three things: every Roslyn component lies under `src/`, the shared target is
    there, and no project file defines it.
  - `scripts/test_held_analyzer_output.py` stages src's props and targets beside the copied project and is GREEN: a real process
    held the output while it was rebuilt.
- **Census growth admitted by hand.** The census was re-recorded at ad187e16d6 for the new project. It measured
  `Binding.Bound -> Frontend.Generated` at 27 uses against 26. The extra use is this branch's: StatementBinder's USE placement
  refusal reads `USE BEFORE` at 85 to cite the 1985 rule of its module (XIII.4.9.3 SR1 or VII.4.6.3 SR1). It was admitted in
  `architecture-edges.json`, in the change that recorded the census. Its remover is unchanged: PB2347 moves StatementBinder out
  of `Bound/`. The edges file was then ratcheted to the new census key.
