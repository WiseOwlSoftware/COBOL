## PB2619: CompilerDriver.Options and CliOptions carry a validated EditionInfo; the driver has no default edition

Wave 20262135, group C (kb/Work PB1754 cluster). The group's eleven notes proved to be eleven mechanisms. This branch
finishes the first, kb/Work PB2619, and returns the rest for later agents (SPLIT at the note boundary).

**Re-probe (pin 2061bdb7bb).** The CLI half had landed in wave 20262133: `--std`'s spellings, its error text, its help
default and the listing default read `EditionInfo.All`, `EditionInfo.Latest` and `EditionInfo.RequiresSourceListing`.
What remained was the driver boundary. `CompilerDriver.Options` carried `int DialectLevel = 2023` and `bool Permissive`,
a third copy of the default edition, and `CliOptions` carried the same two loose values. `cobol --std 1999` is refused
by the validator (`--std must be one of 85, 2002, 2014, 2023 (got 1999).`, probed on this build), so the CLI was already
safe. The library entry point was not: it took any int.

**Fix.** `CompilerDriver.Options` now takes a REQUIRED `EditionInfo Edition` as its second positional parameter, in place
of `DialectLevel` and `Permissive`. `CliOptions.Edition` is built once by `EditionInfo.Of(std, permissive)` where the
command line is resolved, and check-batch builds it the same way. Every caller changed in this change. A script read the
build's error list and rewrote 149 construction sites in the compiler, the CLI and the tests. An omitted level (the old
default) became `EditionInfo.Latest`. The conformance runner's `std` build-step option now sets `Edition` through `with`.
`CompilerDriverTests.CompilerDriver_DefaultsToLatestEdition` is deleted, because the driver has no default left to pin.
`CliParserTests` pins the CLI default. The listing's options section keeps its `--std` and `--permissive` rows, keyed by
the property paths `Edition.Year` and `Edition.Permissive`. `OptionsSectionDriftTests` and `CompileStateDriftTests` now
read the edition as one property with two parts. The first gate (run 000001-20261122T175058Z) was RED on 28,902
Conformance cases, every one the same refusal: the compiled-program cache key (`CompiledProgramCache.KeyValue`, kb/Work
PB985) renders each option by its type and throws on a type it has no rendering for, and `EditionInfo` was new to it. The
key now renders an edition as its year and its permissive flag, and `EveryOption_ChangesTheKey` flips each of the two
parts on its own, so dropping either from the key fails that test. Design docs updated: VERSION_TEST_MATRIX_DESIGN (decision #2's default
now lives in the CLI), VALIDATION_DESIGN, INTRINSICS_DESIGN and DESIGN-external-repository.

**Gate.** Run 000002-20261122T175921Z at a804dddc0f: `=== BUILD-LOCAL GATE: GREEN — Conformance 29,080/29,080 ran · Unit
up to date (run 000001-20261122T175058Z, 42,684 cases) · RepoDrift 230/230 ran · Characterization up to date (37 cases)`.

**Neutral.** `cobol --std 2023 --run` on a DISPLAY program prints `HELLO`, exit 0. The 85 refusal of a sentence before
the first paragraph is unchanged (COBOL0900). The focused unit classes (CompileStateDriftTests, OptionsSectionDriftTests,
CliParserTests, CompilerDriverTests, ExternalRepositoryExpansionTests, ReferenceOccurrenceDriftTests) pass: 525 of 525.

**Left.** `Frontend.Frontend` (`DialectLevel = 85` by default) and `EditionContext(int, bool)` still take the edition as two
loose values. They are siblings of the same shape and appear in the report as a lead.
