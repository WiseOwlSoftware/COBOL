## Entry 3835 — 2026-11-18 00:34 PST — PB4950: every diagnostic at --std 85 cites the 1985 rule; the citation ratchet is gone

The owner decided on 2026-11-07 that a diagnostic cites the rule of the edition the program is compiled under. The net that
asks it, `EditionCitationNet`, was laid as a ratchet of 298 codes and 1638 diagnostic lines that cited only a 2002, 2014 or
2023 rule at `--std 85`. Eight slices swept it in parallel, each in its own branch from slice 1, and this branch merged them.

- **Each site cites through `EditionInfo.Cite(y1985, later, y2002?, y2014?)`**, which takes each text once, and a rule with no 1985 counterpart records why in its descriptor's
  `No1985Rule`. Where 1985 states no numbered rule, the message cites the 1985 general format that prints the clause, or the
  1985 text that states the requirement elsewhere (the level-77 data-name in `§1985:VI.5.2.1` and `§1985:X.4.1.1`, the split `==`
  in `§1985:IV.7.2.2`, the sort file in `§1985:II.2.2.2.1.1`). A rule of a construct COBOL-2002 introduced says so with the edition
  gate's words.
- **One edition read in the parser.** The slices had added six members to `CobolParserCoreBase` for one question. The merge kept
  `EditionInForce` and deleted the rest.
- **One file-control row mechanism.** A 1985 file-control row replaces only the 2023 rows of its id under the same diagnostic
  (`FileControlKeyRule.Replaces`), so a sort-merge arm that shares a 2023 rule id keeps its row and its code.
- **One decider, enforced by the build.** A Roslyn analyzer (`src/WiseOwl.Cobol.Analyzers`, loaded by every `src` project)
  computes the edition-derived values of each real compilation as a fixed point, aliases included, and makes any other choice
  of words by edition a build error (WOCA0001). A derived member another assembly reads carries `[EditionDerived]` (WOCA0002),
  so the derivation crosses projects. That covers public members, and internal ones when the assembly grants
  `InternalsVisibleTo` to another product. Three merge reds came from a test that rebuilt the compilation by hand; the
  analyzer replaced it. Every quotation a COBOL-85 message carries was checked against the 1985 rule it cites, over the
  rendered diagnostics of every refusal at 85.
- **The analyzer misses no tree, no project, no alias and no build, and cannot be silenced.**
  - It reads generated files (the ANTLR parser, the `*.Table.cs` files) like any other.
  - It compares every symbol as the member it stands for, an extension method or a member of a generic type included. A read
    through an interface, abstract or virtual member is derived from its implementations, but only up to members of our own
    assemblies, never a framework member such as `object.GetHashCode` or `IReadOnlyCollection<T>.Count`.
  - `AnalyzerWiringDriftTests` has MSBuild evaluate every src project in Debug and Release. Every project but the analyzer
    itself, which is named by its path, must load the analyzer and must leave analyzers on.
  - Both rules are `NotConfigurable`, so NoWarn, a lowered severity, `.editorconfig`, `.globalconfig` and `#pragma` cannot turn
    them off. A `[SuppressMessage]` whose checkId names a rule fails the build with a `#error` (WOCA0003) that the analyzers' generator writes.
    A compiler error is the one refusal no suppression attribute can reach: an analyzer diagnostic refusing the attribute was
    silenced by a suppression of its own id.
  - The analyzer is built under one configuration per build. Every reference names its configuration, and a nested build such
    as the runtime's per-platform ReadyToRun publish builds no references. This ends a CS2012 race and a Release build that was
    never incremental. `AnalyzerBuildsOnceDriftTests` builds the runtime in Release twice to hold it, in the code lane's gate.
  - The held-output target that keeps a language server's lock from failing a build is defined once, for every src Roslyn
    component.
- **The net is a plain rule.** The ratchet file is deleted. Any later-edition-only citation at 85 fails, unless it is an edition
  gate or its descriptor records `No1985Rule` (docs/VERSION_TEST_MATRIX_DESIGN.md P2.3a).

kb/Work PB4950 is landed for 1985. The 2002 and 2014 directions are kb/Work PB5922.

The runtime's `MoveValidity` keeps one form of each rule check, the one that takes the citations
(`Table16Refusal`, `BinaryWidthRefusal`, `FigurativeRefusal` with `MoveCitations`). The run time passes
`MoveCitations.Iso2023` explicitly. The two-argument overloads that forwarded to it are gone. Removing them changes the
shipped public surface, so the runtime major is now 31 (`scripts/abi_pin.py bump --runtime-major`).

