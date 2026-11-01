# DESIGN — User documentation, publication and distribution

> **Status:** design, owner decisions recorded in `kb/Work/R50.md` (2026-09-26). The work that implements this doc
> is tracked ONLY in `kb/Work/` (notes PB1610–PB1614, filed from §7) — this doc never carries a worklist.

## §1 The obligation

ISO/IEC 1989:2023 §4.2.16 (`cite.py --check 4.2.16` OK): *"An implementation shall satisfy the user documentation
requirements specified in 4.2.3, 4.2.4, 4.2.5, 4.2.6, 4.2.10, 4.2.12, and 4.2.13 by specification in at least one
form of documentation."* Any form qualifies — manuals, on-line documentation, help screens — and a requirement may be
met by reference to other documents.

| Clause | Subject | Current source of truth |
|---|---|---|
| §4.2.3 | Interaction with non-COBOL runtime modules | .NET interop: `INTERPROGRAM_DESIGN.md`, R48 (STOP RUN under a .NET host) |
| §4.2.4 | Interaction between COBOL implementations | not yet written as user text (migration chapter) |
| §4.2.5 | Implementor-defined language elements | `docs/CONFORMANCE.md` §7 (Annex A.1 register, 182/182) + §3 determinations |
| §4.2.6 | Processor-dependent language elements | `docs/CONFORMANCE.md` §2 (Annex A.3) + §4 documented non-support |
| §4.2.10 | Nonstandard extensions | `docs/CONFORMANCE.md`, `GnuCOBOL extensions.md`, dialect gating |
| §4.2.12 / §4.2.13 | Archaic / obsolete elements | `constructs.json` + `VERSION_CHANGE_REFERENCE.md` (edition matrix) |

The facts exist; what is missing is a USER-facing presentation, versioned with the compiler.

## §2 Decisions (R50)

1. **Docs-as-code, not a wiki.** The §4.2.16 documentation describes ONE compiler version, so it is versioned,
   reviewed and CI-checked with the code (the rustc book / Reference, CPython `Doc/`, GnuCOBOL's Texinfo manual,
   dotnet/docs all do this). A wiki is unversioned, unreviewed and cannot be generated.
2. **Astro Starlight**, hosted on **wiseowlsoftware.com** (the existing Astro site, which carries Demeanor's docs).
3. **The home page bifurcates** into two products; **Demeanor remains primary**.
4. **NuGet distribution under the `WiseOwl.` prefix**, as Demeanor ships (`WiseOwl.Demeanor`, `WiseOwl.Demeanor.MSBuild`).

## §3 Source layout and publication

**What a reader sees:** ordinary web pages at `https://wiseowlsoftware.com/cobol/docs/` — browse, search and link
like any documentation site, nothing to download or install. A version picker switches between releases;
`/cobol/docs/latest/` is the default. Everything below is BUILD plumbing that readers never see.

- **Source:** `docs/manual/` in THIS repo, Starlight-compatible Markdown/MDX (frontmatter `title`, `description`,
  `sidebar`). Hand-written chapters live there; GENERATED chapters are written by generators into
  `docs/manual/reference/` at build time and are never hand-edited (a header comment says so).
- **Bundle (a build input, never a user download):** the release pipeline produces `docs-bundle-<version>.zip` (hand-written + generated pages, plus an
  `index.json` with version and commit) as a release artifact. `main` publishes a rolling `next` bundle.
- **Site:** the wiseowlsoftware.com repo mounts Starlight at `/cobol/docs/` and its BUILD (CI, at deploy time) fetches the bundles into
  `src/content/docs/cobol/<version>/`; `/cobol/docs/latest/` aliases the newest release. The site repo owns theme,
  navigation and the bifurcated home page; this repo owns the content. Neither copies the other's files by hand.
- **Why a bundle, not a submodule or a copy:** the site builds any number of versions without checking out old
  compiler trees, and the conformance statement a user reads is byte-for-byte the one that shipped with their version.

## §4 Content map

**Hand-written** (`docs/manual/`):
- *Install* (`install.md`) — prerequisites (.NET 10 runtime) and verifying the install. It builds the compiler from source
  until kb/Work PB1613 publishes the packages; that change replaces the build steps with
  `dotnet tool install -g WiseOwl.COBOL` and the page doubles as the package README (§6).
- *Getting started* (`getting-started.md`) — hello world; compile, run, the produced assembly, reading a diagnostic.
- *User guide* (`user-guide/`, one page each) — `command-line.md`: the `cobol` command, options, output files, exit
  statuses; `editions.md`: `--std 85|2002|2014|2023`, `--permissive`, archaic/obsolete warnings, `--flag-extensions`;
  `source-format.md`: reference format (fixed / free / `>>SOURCE`), COPY libraries and search; `run-units.md`: run
  units and STOP RUN under a .NET host (R48); `locale-and-collation.md` (A.4.9); `exceptions.md`; `files.md`: the basics
  of file handling (the *File types* chapter below is the full account). `command-line.md`'s option table is
  superseded by the generated command-line reference (kb/Work PB1612) when that lands; the page then keeps the
  concepts and links the reference.
- *File types* (owner 2026-10-17, kb/Work PB3352) — the implementation and support of each file kind, per
  organization: sequential, line sequential, relative, indexed, sort-merge and report files. For each it gives the
  access modes and statements per edition, the on-disk format, the limits (DOC-A.1-107 and the other DOC-A.1 rows),
  foreign files ('39'), the Latin-1 no-CODE-SET rule of R47 and the status codes. It also covers sharing and record
  locking, APPLY COMMIT / COMMIT / ROLLBACK and the crash guarantee, the performance a user can plan by, and the SORT
  work files' memory limit and location. Its prerequisite, kb/Work PB3330's keyed store engine, has landed.
- *.NET interoperability* (§4.2.3, `dotnet-interop.md`) — calling COBOL from C#, what crosses the boundary, what a call
  throws, trimmed/AOT applications; and the statement that COBOL cannot call non-COBOL code (DOC-A.1-13, -15, -141).
  The "Separately compiled units and the repository" chapter joins it as its own page when kb/Work PB2103 lands.
- *Migrating* (§4.2.4, `migrating.md`) — from IBM Enterprise COBOL, Micro Focus and GnuCOBOL: what is standard, what is a
  dialect extension, what WiseOwl COBOL rejects and why; it carries the §4.2.4 statement that no interaction with other
  COBOL implementations' runtime elements is supported.
- *Release notes* (`release-notes.md`) — per version, derived from the DEVLOG entries between release tags (edited, not
  dumped); the page states the convention until a release exists.

Every behavioral statement on a hand-written page was run on the compiler it describes and cites its clause
(`cite.py --check`) or its CONFORMANCE.md determination. A page links its siblings with relative URLs that end in a slash
(`../editions/`), so a bundle mounted at any version prefix keeps its links; it never links a file outside `docs/manual/`.

**Generated** (`docs/manual/reference/`), each by one generator with a drift test:
- *Conformance statement* (§4.2.5, §4.2.6, §4.2.10) — from `docs/CONFORMANCE.md` §2–§5, §7 and the traceability
  inventory: the implementor-defined register, processor-dependent dispositions, optional-module (A.4) claims,
  documented non-support with its warning codes, and the limits.
- *Edition matrix* (§4.2.12, §4.2.13) — from `constructs.json` + `VERSION_CHANGE_REFERENCE.md`: per construct, the
  introducing edition, archaic/obsolete status, and what `--std` does with it.
- *Command-line reference* — from `CliOptions` (one source for `--help` and the page).
- *Diagnostics reference* — from the diagnostic catalog: one entry per `COBOL####` code with severity, the rule it
  enforces (§ citation) and a minimal example.
- *Runtime API reference* — from the runtime assembly's XML documentation comments.

## §5 Drift tests (what keeps "generated" true)

One drift test per generator, in the Conformance or Unit assembly: regenerate into a temp directory and compare with
`docs/manual/reference/`; a new diagnostic code, CLI option, `constructs.json` row or CONFORMANCE.md determination
without its page is RED. Every citation a generated page prints passes `cite.py --check` (the generator runs it).

## §6 Distribution (proposal until PB1613 lands)

| Package | Content | Notes |
|---|---|---|
| `WiseOwl.COBOL` | the compiler as a .NET global tool, command `cobol` | `PackAsTool`, `ToolCommandName=cobol` |
| `WiseOwl.COBOL.Runtime` | the runtime library compiled programs reference | versioned in lockstep with the compiler |
| `WiseOwl.COBOL.MSBuild` | optional: compile `.cob` items inside a .csproj | mirrors `WiseOwl.Demeanor.MSBuild`; decide in PB1613 |

**The tool ships the ReadyToRun runtime (owner decision 2026-10-07, kb/Work PB2528; DESIGN-test-build-ci §3.16).**
The `WiseOwl.COBOL` package is the CLI's publish output, and that output already carries
`ReadyToRun/<rid>/WiseOwl.Cobol.Runtime.dll` for every platform of `CobolRuntimeReadyToRunIdentifiers` (`win-x64`,
`linux-x64`), the same images the gates' programs run. The `cobol` command deploys the one for the platform it runs
on beside every compiled program, and the portable runtime on any other platform. The CLI's publish fails if any
image is missing (`CobolAssertReadyToRunRuntimePublished`), so the pack PB1613 adds needs no step of its own for
them; which further platforms the package targets (`osx-arm64`, `linux-arm64`, `win-arm64`) is PB1613's choice, and
each is one entry in that property. `WiseOwl.COBOL.Runtime`, for programs referenced from a .csproj, is a portable
library package; a project that publishes with `PublishReadyToRun` precompiles it for its own platform.

License metadata (settled, kb/Work PB3146): `PackageLicenseFile` `LICENSE`, set once in `Directory.Build.props`, which
also packs the repository's `LICENSE` at the root of every package whose project is packable. Not a
`PackageLicenseExpression`: nuget.org accepts an expression only for an OSI- or FSF-approved license, and the SPDX id
`BUSL-1.1` would drop the license's parameters (the Additional Use Grant, the Change Date and the Change License).
Package README = the Install + Getting started pages.

## §7 Work items

Filed as `kb/Work` notes (rule 8 — no list here): **PB1610** manual scaffold + hand-written chapters · **PB1611**
conformance-statement and edition-matrix generators + drift tests · **PB1612** CLI and diagnostics reference
generators + drift tests · **PB1613** NuGet packaging and release pipeline (including the docs bundle) ·
**PB1614** wiseowlsoftware.com: Starlight integration at `/cobol/docs/` and the two-product home page.
