## kb/Work PB4453: no compiler test reads a Markdown file, so a document edit is the inert lane

A one-word edit to any file under docs/ ran the build and the three whole populations, because some thirty Unit and Conformance classes read Markdown.
The test phase's median over the last 61 gates was 12.7 minutes, and the tooling lane takes about 40 seconds.
The owner's direction on 2026-11-02 was "There is no compelling reason a compiler change needs to read an markdown note", so PB4453 became total: `docs/` and every `*.md` are INERT in `scripts/inert_lane.py`, and the Unit, Conformance and Characterization assemblies open none.

**Re-probe.** The first pass made `docs/` and `**/*.md` inert and ran the three assemblies under the inotify watch on WSL (`bash scripts/linux-gate.sh --legs unit,characterization,conformance --nice`).
All 60,000 tests passed, and the watch reported 167 opened paths.
Almost all were one test walking every doc (`SortRuntimeDocShapeDriftTests`), so the specific readers came from a static scan of `tests/` as well.
The watch does not cover the repository root, so `README.md` was found only by that scan.
A second run found `docs/CONFORMANCE.md` still opened, by `conformance-suite/harness/generate.py --out`, which a Conformance test runs once per process.
A third still found it opened once, and the watch cannot say by whom, so an `strace -f -e trace=openat,execve` run of the Unit assembly named the thread: it was in the test host itself.
The reader was `SpecTraceabilityInventoryDriftTests` resolving the 346 inventory anchors of the form `docs/CONFORMANCE.md#DOC-A.1-n` by reading the file.
That check is now `scripts/docchecks/inventory.py`, and `tests/doc-facts/inventory-anchor-kinds.json` names the document kinds the audit resolves so that the Unit test resolves every other kind.
The fourth Linux run was GREEN, `inert-watch` included: `=== INERT WATCH: GREEN — no inert or tooling-lane path was opened (105 director(ies) watched; command exit 0) ===` over 39,476 Unit, 21,019 Conformance and 37 Characterization tests.

**The fix has three shapes, one per kind of reader.**
- A check over the Markdown alone became the `DOC AUDITS` gate audit, `python scripts/doc_audits.py --check`, with one module per subject under `scripts/docchecks/`.
  Each check is a pure function of the text it judges, and each module's self-test plants every violation and requires the finding back.
  The 28 checks cover:
  - the CONFORMANCE.md registers (§2 covers every Annex A.3 item and cites no placeholder clause, and every code it names exists in src/);
  - the README's Status section against §2 and §5, and the conformance suite's profile against them;
  - VERSION_CHANGE_REFERENCE.md: gate anchors against `constructs.json`, spec citations against the standard, no spec line number, and the generated status index;
  - the user manual: frontmatter, links and headings, and the command-line page;
  - the boolean-store determination, carried word for word in three places;
  - the `CobolSort` calls the design docs write;
  - the untyped host routes in user documents;
  - NUL bytes in every tracked Markdown file.
- A check of a document against a fact the compiler holds reads `tests/doc-facts/compiler-facts.json`.
  It holds the dynamic-length maximum, the implementor-defined exception-names, the dialect-behavior registry, the nonstandard-extension ids, the implementor-names, the allocated diagnostic codes, the command line's options and exit statuses, the `CobolSort` members and the `OperandPositions` rows.
  The Unit test `DocumentedFactsDriftTests` writes it from the compiler (`WISEOWL_COBOL_WRITE_DOC_FACTS=1`) and holds it equal there.
- A datum a test needs that a document also states became an extract the audit holds fresh:
  - `tests/version-matrix/conformance-register.json` carries the §7 keys with the determination's lead, the §8 derivations and the codes §2 and §5 name.
    `ConformanceRegister.cs` reads it, so the C# twin of the Markdown parser is gone and the Python parser is the only one.
  - `tests/doc-facts/manual-programs/*.cob` holds the programs the manual prints, and `UserManualDriftTests` compiles them.
  - The exit-status examples, the declined exception-name sites and the untyped-route pattern are small data files the tests read and the audit holds the documents to.

**What was deleted or moved.** Deleted:
- `AnnexA3RegisterDriftTests`, `ReadmeDeclinedSummaryDriftTests`, `ConformanceSuiteProfileDriftTests`;
- `BooleanStoreDeterminationDriftTests`, `SortRuntimeDocShapeDriftTests`, `VcrDriftTests`;
- `AnnexA1RegisterDriftTests` and `DerivationRegisterDriftTests`, which ran Python audits to read CONFORMANCE.md.

`audit_annex_a1.py --check` and `audit_derivations.py --check` are now gate audits, and `audit_annex_a1.py` gained the orphan-verdict check and population assertions the Unit test held.
`r2_subsystems.py --check` is the `ARCH SUBSYSTEMS` audit.
`scripts/gen-vcr.ps1` is replaced by `doc_audits.py --write-vcr`.
In the tests that keep an inventory witness name, the Markdown arm moved out and the compiler arm stayed: `DynamicLengthTests`, `ReturnedValueMaximumTests`, `EcImplementorNamesDocumentationDriftTests`, `NonstandardExtensionRegisterDriftTests`, `DialectBehaviorRegistryDriftTests`, `ImplementorNamesDriftTests` and `FunctionDeviceFormDriftTests`.
`NulByteDriftTests` dropped `.md` from its kinds.
`generate.py --out` writes the manifests alone, and `--report` adds the completeness report, which is the only output that reads the §7 and §8 registers.

**The pin.** `inert_lane.py check`, an audit in every lane, holds every compiler test source to three rules:
- it spells no Markdown path (the ISO text and the gitignored `DIAGNOSTICS.md` view a test writes are the two named names);
- it calls no `TestRepo.Docs()` walk;
- it declares no `TreeScope` without `Kinds`.

It also reports a TOOLING_READS or CODE_READS entry that an inert entry covers, because `classify` asks `is_inert` first and such an entry is dead.
Six such entries were removed.
`csharp_parts` is a small C# lexer for this check, with self-test arms for regular, verbatim, interpolated and raw strings, comments and character literals.

**The one decision left to the owner.** `specs/ISO_COBOL.md` is a Markdown file that about 45 Unit and Conformance classes read as the oracle.
It is a READ entry, so a change to the spec still runs the whole population.
The other READ entries are the rule catalog, the architecture edges and the census records under docs/, which are JSON.
Moving the spec tables into audits would be its own campaign, and it is reported rather than taken.

**Gate.** The first gate was RED on one test, `SpecTraceabilityInventoryDriftTests.TheseChecks_ActuallyFail_OnAFabricatedInventory`.
Its fixture anchors into a `.py` file, and the first version of the kinds file listed the kinds the Unit test resolves instead of the document kinds the audit resolves, so the fixture's anchor stopped being checked.
The kinds file now names the document kinds, and the Unit test resolves every other kind.
The second gate (run `000002-20261103T080338Z`, the previously red class first) printed `=== BUILD-LOCAL GATE: GREEN — Conformance 21,019/21,019 · Unit 39,443/39,443 · Characterization 37/37 cases ran (skipped 0) ===`.
The landing's merge group is the first run of the merged tree, Linux and Release, as always.

**Self-review.** Architecture: one mechanism per job, a facts file for what the compiler holds, an extract for what a test needs from a document, an audit for the rest; no C# text in a bound node was touched.
Full code: each ported rule was read against the C# it replaces, and the module self-tests plant every violation; the rendered-views audit then found four classes still carrying the `Sweep` trait that no longer sweep, and the strace probe found the last reader.
Performance: the audit takes about 7 seconds, mostly the ISO text normalized for the VCR citations and the `src/` code scan, and runs beside the build.
Duplication: the C# twin of the register parser is gone, the untyped-route pattern and the document kinds each live in one data file read by both languages.

**Docs.**
- DESIGN-test-build-ci §3.18 and the lane text.
- CLAUDE.md's Testing paragraph.
- The gate skill.
- DOC_INDEX.
- The `inert_lane.py` docstring.
- Every document that named a deleted test.
