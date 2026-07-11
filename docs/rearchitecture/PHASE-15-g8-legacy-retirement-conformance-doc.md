# PHASE 15 — G8 legacy retirement (three cuts) + §4.2.16 conformance documentation + runtime namespace flip + D10 SUBSCRIPT-mode removal

- **Phase:** P15
- **Track:** cleanup (+ the one relocated rearchitecture sub-track, D10)
- **Risk:** MEDIUM for the cleanup cuts (irreversible deletions); **HIGH** for the D10 sub-track (a shared-grammar +
  ~250-line binder-parser rewrite) — kept isolated as its own post-Cut-2 sub-track (§"CUT 2.5") so it cannot destabilize
  the legacy-retirement cuts.
- **Depends on:** P14 (matrix closure + in-repo greenfield guard + the one-time equivalence proof). Transitively also
  P0 (oracle bake-out → committed goldens; `corpus.tsv`; `DifferentialBakeTool`), P1 (mechanical namespace rename
  `CobolSharp.Compiler.* → CobolNet.*` already landed in the greenfield tree), P7/P8 (the `RuntimeApi` typed façade
  is the single compiler↔runtime ABI surface — its existence is what makes the runtime namespace flip a one-file
  change), and P9–P13 (M2/M3/M4 ISO feature completion — the conformance document can only claim what is actually
  implemented).
- **SSOT alignment:** `docs/rearchitecture/DESIGN-test-build-ci.md` §3.4/§3.7/§3.8/§5 (Phase G8), `DESIGN-runtime-library.md`
  §2.8 + §4 step 6, `DESIGN-module-topology.md` §10, `docs/COBOLNET_DESIGN.md` §16 (G8), and the ISO spec
  `specs/ISO_COBOL.md` §4.2 + Annexes A.1/A.3/A.4/F.1/F.2.

## GOAL (one paragraph)
Sever the frozen legacy byte-engine oracle from the build and test graph, delete `src/CobolSharp.*` entirely, flip
the runtime `RootNamespace` from `CobolNet.Runtime` to `Cobol.Net.Runtime` (routed through the single `RuntimeApi`
façade so the emitted `using` changes in exactly one place), and publish the complete ISO/IEC 1989:2023 §4.2.16 user
/ conformance documentation set (the implementor-defined element list A.1, the processor-dependent claims-and-absences
A.3, the optional-element claims A.4 including the A.4.10/11/12 rows, nonstandard extensions and any added reserved
words §4.2.10, archaic §4.2.12 and obsolete §4.2.13 identification, and the §4.2.3/§4.2.4 interaction statements). All
deletions here are irreversible and are gated on the Phase-14 equivalence proof having been green; the legacy source
is preserved at an annotated git tag with a WSL reproduction recipe before it is deleted.

> **⛔ RELOCATED SUB-TRACK — D10 (SUBSCRIPT-mode removal), moved here from PHASE 04 (2026-07-10, DEVLOG 748).** The owner's
> D10 ruling (master §6 D10) — FULLY REMOVE the lexer `SUBSCRIPT` mode + the binder subscript re-parse, replacing the
> flat `SUB_*` stream + the ~250-line hand-rolled C# re-parsers with interpreted grammar rules — could not land inside
> PHASE 04's byte-neutral window: the FROZEN legacy compiler consumes `SUB_*`/`SubscriptEntryContext` (`ExpressionBinder.
> BindSubscriptEntry`), so the machinery cannot leave the SHARED grammar until the legacy tree is deleted. That deletion
> is **PHASE 15 Cut 2** — so PHASE 15, immediately AFTER Cut 2, is the first place D10 is realistically doable. It runs as
> the isolated **§"CUT 2.5"** sub-track (after the legacy delete, before the Cut-3 namespace flip). The decision-complete
> DESIGN is `DESIGN-frontend-grammar.md §9` (incl. the §9.4 ISO §8.3.5 space-separator decision the executing session must
> resolve first). This is a rearchitecture task riding the cleanup phase because that is where its blocker clears.

## EXIT CRITERIA (phase is DONE when ALL hold)
0. **(D10 sub-track) The SUBSCRIPT lexer mode + the flat `SUB_*` stream + the hand-rolled C# subscript re-parsers are
   REMOVED**, replaced by interpreted grammar rules per `DESIGN-frontend-grammar.md §9` (retaining only the minimal
   spec-compelled WS mechanism per §9.4); the greenfield battery + `guard.ps1` stay green; a subscript/ref-mod/space-
   separated-args/nested-FUNCTION corpus (the §9.5 D10.1 set) is green. (Sequenced after Cut 2; see §"CUT 2.5".)
1. **Grep-clean of legacy references:** no `src/CobolSharp.*` project, no `ProjectReference` to a `CobolSharp.*`
   project anywhere, no `CobolSharp.Compiler`/`CobolSharp.Runtime` type reference in any `src/Cobol.Net.*` or
   `tests/Cobol.Net.*` file, no `CobolSharp.sln` entry for a legacy project, and no `scripts/guard*.sh` /
   `compliance.sh` / `nist-batch.sh` / `run-suite.sh` in the tree.
2. **One greenfield guard exits 0:** the greenfield-only battery (below) is green from a clean checkout on Windows and
   Linux; a single authoritative guard command (`scripts/guard.ps1`, cross-platform) returns exit 0.
3. **The §4.2.16 conformance document is published:** `docs/CONFORMANCE.md` exists, is complete per the section map in
   §7 of this doc, is linked from `README.md` and `docs/DOC_INDEX.md`, and every claim cites a spec § and (where a
   behavioral claim) a passing conformance test / golden.
4. **Runtime namespace flipped with emitted code green:** `RootNamespace` is `Cobol.Net.Runtime`; the `Generated/`
   regenerates clean; a representative program compiles, its `.g.cs` shows the new `using`, and it runs byte-identically
   to before the flip.
5. **DOC_INDEX reconciled:** `docs/DOC_INDEX.md` has no rows pointing at deleted docs/scripts, has rows for
   `CONFORMANCE.md` and this phase doc, and the count/preamble is updated.

## STATUS
`NOT STARTED`
<!-- The executing session updates this line to `IN PROGRESS @ step N` and finally `DONE`.
     Keep the per-step checkboxes in §4 current so an interrupted session can resume exactly. -->

---

## 1. Preconditions to VERIFY before starting (do not skip)
A future session must confirm the world is in the expected shape, because P15 is destructive. Run these and abort if
any fails — the missing precondition belongs to an earlier phase, not here.

```bash
# P14 done: the greenfield guard + equivalence proof exist and are green.
ls tests/nist/corpus.tsv                                  # P0 corpus manifest must exist
ls scripts/guard.ps1                                      # P0/P14 cross-platform greenfield guard must exist
ls tests/differential/**/*.out 2>/dev/null | head         # P0/R1 baked goldens must exist (severs the oracle)
# P1 done: the greenfield tree is already renamed off the legacy namespace.
grep -rl "namespace CobolSharp.Compiler" src/Cobol.Net.* ; echo "expect: NO hits"
# P7/P8 done: the RuntimeApi façade is the single runtime ABI surface.
ls src/Cobol.Net.Compiler/CodeGen/**/RuntimeApi.cs 2>/dev/null || find src/Cobol.Net.Compiler -name RuntimeApi.cs
```

Expected: `corpus.tsv`, `guard.ps1`, the baked `*.out` goldens, and `RuntimeApi.cs` all exist; the greenfield tree has
**zero** `namespace CobolSharp.Compiler` declarations (P1 renamed them all). If `RuntimeApi.cs` does not exist, step 9
(the namespace flip) cannot be done as a one-file change — STOP and finish P7/P8 first.

**Baseline the battery once, green, before any change** (this is the number every later step must reproduce):

```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
pwsh scripts/guard.ps1        # greenfield authoritative guard (P0/P14); exit 0
```
Record the pass counts (conformance ~2028+, unit ~213+; NIST corpus all green). Any later step that changes these
counts is a regression to investigate before the commit boundary.

---

## 2. Rationale — the problems this phase fixes
The AS-IS dossier and the sibling DESIGN docs identify the load-bearing weaknesses this phase closes:

1. **The differential net is coupled to code that gets deleted.** `tests/Cobol.Net.Tests.Conformance` still
   `ProjectReference`s the legacy engine (`Cobol.Net.Tests.Conformance.csproj:34-35` → `CobolSharp.Compiler.csproj`
   + `CobolSharp.Runtime.csproj`), and `CompilerUnderTest.cs` opens with
   `using LegacyCompilation = CobolSharp.Compiler.Compilation; using LegacyState = CobolSharp.Runtime.ProgramState;`
   plus a `LegacyCompiler : ICompilerUnderTest`. ~60 `*DifferentialTests.cs` assert `cobolnet == legacy` at runtime.
   DESIGN-test-build-ci §"Risks" #1: *"The net evaporates at G8."* P0/R1 already **baked** the legacy stdout into
   committed goldens (`tests/differential/**/*.out`) and rewrote those tests to golden comparison, so by P15 the net
   is self-standing — this phase only **removes the now-dead legacy edge**.
2. **The CI authoritative gate is a Linux-only bash loop over the frozen engine.** `.github/workflows/build-and-test.yml`
   job `guard` runs `scripts/guard-fast.sh`, which builds `src/CobolSharp.CLI` and runs the legacy `~350` NIST loop.
   That is the *legacy* gate; the greenfield gate is a separate job. Post-bake it is pure redundant insurance and must
   be retired so the greenfield in-process NIST run (P0's `corpus.tsv`-driven `NistDifferentialTests`) becomes the
   whole regression, identical on both OSes (DESIGN-test-build-ci §3.8, closes frontend smell "NIST is Linux-only").
3. **Seven guard scripts and two legacy test projects exist only to run/parallelize the legacy NIST loop.**
   `scripts/{guard.sh,guard-fast.sh,guard-run-group.sh,guard-verify.sh,compliance.sh,nist-batch.sh,run-suite.sh}` and
   `tests/CobolSharp.Tests.{Unit,Integration}` are all legacy-only (DESIGN-test-build-ci §4, "delete (G8)"). They are
   dead once NIST runs in-process.
4. **The runtime carries a deferred rename.** `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` has
   `AssemblyName=Cobol.Net.Runtime` but `RootNamespace=CobolNet.Runtime`, and every runtime file declares
   `namespace CobolNet.Runtime;` — an assembly/namespace incoherence deliberately deferred to G8 to keep the emitted
   `using CobolNet.Runtime;` byte-stable through G0–G7 (csproj banner; DESIGN-runtime-library §2.8). With the
   `RuntimeApi` façade now owning the emitted `using`s, the flip is a one-file emitted change.
5. **No published conformance documentation.** ISO §4.2.16 *requires* an implementation to document its
   implementor-defined (§4.2.5 / A.1), processor-dependent (§4.2.6 / A.3), optional (§4.2.7 / A.4), nonstandard
   (§4.2.10), archaic (§4.2.12 / F.1) and obsolete (§4.2.13 / F.2) elements and its non-COBOL / cross-implementation
   interaction (§4.2.3 / §4.2.4). A "commercial-quality, full-ISO compiler" (the North Star) cannot *claim* conformance
   without this artifact. It is authored here because only now (post P9–P13) is the feature set frozen enough to be
   accurately documented.

---

## 3. Target end-state (concrete — what exists when P15 is DONE)
Files/dirs **deleted:**
- `src/CobolSharp.Compiler/`, `src/CobolSharp.Runtime/`, `src/CobolSharp.CLI/` (the entire byte engine + legacy CLI).
- `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/`.
- `scripts/guard.sh`, `scripts/guard-fast.sh`, `scripts/guard-run-group.sh`, `scripts/guard-verify.sh`,
  `scripts/compliance.sh`, `scripts/nist-batch.sh`, `scripts/run-suite.sh`.
- `tests/Cobol.Net.Tests.Conformance/` legacy pieces: the `LegacyCompiler` class + `ICompilerUnderTest.Legacy*` and the
  two `ProjectReference`s to `CobolSharp.*` (`CompilerUnderTest.cs`, the `.csproj`).
- The `legacy-oracle` (currently named `guard`) CI job in `.github/workflows/build-and-test.yml`.
- Any `InternalsVisibleTo Include="CobolSharp.Tests.Unit"` in greenfield csprojs (e.g.
  `Cobol.Net.Frontend.csproj:18`).

Files/dirs **changed:**
- `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj`: `RootNamespace` → `Cobol.Net.Runtime`; csproj banner updated
  (no longer "renamed at G8; stays CobolNet.Runtime through G0-G7").
- Every `src/Cobol.Net.Runtime/**/*.cs`: `namespace CobolNet.Runtime[.X]` → `namespace Cobol.Net.Runtime[.X]`.
- The `RuntimeApi` façade (`src/Cobol.Net.Compiler/CodeGen/.../RuntimeApi.cs`): the ONE place the emitted `using`
  namespace(s) is produced flips to `Cobol.Net.Runtime`. Every runtime member reference in generated code follows.
- `CobolSharp.sln`: legacy project entries removed; solution builds only the `Cobol.Net.*` projects + the two
  greenfield test projects.
- `.github/workflows/build-and-test.yml`: reduced to an OS-matrix `build-test` job (build `-warnaserror`; conformance +
  unit `--no-build`) + the `version-sweep` (INV-1) job. No legacy job.
- `docs/DOC_INDEX.md`: rows for deleted docs/scripts removed; rows for `CONFORMANCE.md` and this phase doc added.

Files/dirs **created:**
- `docs/CONFORMANCE.md` — the complete ISO §4.2.16 conformance / user-documentation set.
- A git **annotated tag** `legacy-byte-engine-final` at the commit just before Cut 2, with the WSL run recipe in its
  tag message (and a short `docs/rearchitecture/LEGACY-ARCHIVE.md` note recording the tag + recipe for discoverability).

Invariants still upheld: typed-native data only; the full greenfield battery green; four-editions-in-one; JSON/XML
absent.

---

## 4. STEP-BY-STEP (numbered, ordered, resumable)
> Convention: each step is a small, independently-green change. `[ ]`/`[x]` checkboxes track resumability — an
> interrupted session reads the last `[x]` and the STATUS line and resumes at the next `[ ]`. Commit boundaries are
> called out; keep the battery green at each. Ordering is deliberate: **Cut 1 (test graph) → Cut 2 (delete engine) →
> Cut 3 (namespace flip) → conformance doc**, so the destructive engine delete happens only after the test graph no
> longer references it, and the namespace flip happens against a tree that no longer builds the legacy stack.

### CUT 1 — Drop legacy from the build & test graph
The engine files still exist on disk after Cut 1; only their edges into the greenfield build/test/CI graph are cut.

- [ ] **Step 1 — Sever the conformance project's legacy `ProjectReference`s and delete `LegacyCompiler`.**
  - Files: `tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj` (remove lines 34-35, the
    `CobolSharp.Compiler`/`CobolSharp.Runtime` `ProjectReference`s); `tests/Cobol.Net.Tests.Conformance/CompilerUnderTest.cs`
    (delete `using LegacyCompilation = …`, `using LegacyState = …`, the `LegacyCompiler` class, and the `Legacy*`
    members/branches of `ICompilerUnderTest`; keep `CobolNetCompiler` + `CutRunner`). Any remaining `DifferentialHarness`
    smoke that constructs `LegacyCompiler` is repointed to golden comparison or removed.
  - Why: this `ProjectReference` pair is the ONLY compile-time dependency of the greenfield tree on the legacy engine
    (verified: `src/Cobol.Net.*` csprojs reference only `Cobol.Net.*` + Antlr). P0/R1 already baked the goldens and
    rewrote the differential tests to `AssertMatchesGolden`, so nothing behavioral is lost.
  - Verify:
    ```bash
    grep -rn "CobolSharp" tests/Cobol.Net.Tests.Conformance/ ; echo "expect: NO hits"
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    ```
    Expected: no `CobolSharp` hits; conformance battery pass count == the §1 baseline (the ~60 differential tests now
    read committed `*.out` goldens, not a live legacy run).
  - **COMMIT BOUNDARY.** Suggested message:
    `refactor(cobolnet): P15 Cut 1a — sever conformance project's legacy oracle ProjectReference (goldens are self-standing)`

- [ ] **Step 2 — Point CI's authoritative gate at the greenfield guard; delete the legacy `guard` job.**
  - File: `.github/workflows/build-and-test.yml`. Delete the `guard` job (runs `scripts/guard-fast.sh` over the legacy
    engine). Confirm `greenfield-tests` (conformance+unit) and `inv1-sweep` remain and are green. Collapse
    `windows-build-test` into an OS-matrix `build-test` per DESIGN-test-build-ci §3.8 (matrix `[ubuntu-latest,
    windows-latest]`; `dotnet build CobolSharp.sln -warnaserror` — but see step 6, the sln still contains legacy at
    this point, so for THIS step keep building the greenfield test projects explicitly; the sln slim-down in step 6
    lets a later edit switch to a whole-sln build). Remove `InternalsVisibleTo` for `CobolSharp.Tests.Unit` from
    greenfield csprojs so nothing references the legacy test assembly name.
  - Why: post-bake the legacy job is redundant insurance; the greenfield in-process NIST run is the whole regression
    (DESIGN-test-build-ci §3.8: post-G8 `build-test` becomes the whole gate, identical on both OSes).
  - Verify: push to a branch and confirm the workflow runs only the greenfield jobs and is green; locally
    `pwsh scripts/guard.ps1` exits 0.
  - **COMMIT BOUNDARY.** Suggested message:
    `ci(cobolnet): P15 Cut 1b — retire the legacy guard-fast.sh job; greenfield in-process NIST is the authoritative gate`

- [ ] **Step 3 — Sever `tools/DifferentialBakeTool`'s legacy dependency.**
  - File: `tools/DifferentialBakeTool` (created in P0). The bake is a one-time operation that has already run; the tool
    must no longer reference `CobolSharp.*`. Either (a) delete the tool outright (the goldens are committed and the
    bake never needs re-running against legacy — recommended), or (b) if kept as a re-bake maintenance utility, strip
    the `CobolSharp.*` `ProjectReference` and gate the legacy code path out. Prefer (a).
  - Why: DESIGN-test-build-ci §"Cut 1 … sever tools/DifferentialBakeTool's legacy dependency". A dangling legacy
    reference here would block Cut 2.
  - Verify: `grep -rn "CobolSharp" tools/ ; echo "expect: NO hits"`; `dotnet build CobolSharp.sln` still succeeds
    (legacy projects still present but now referenced only by themselves).
  - **COMMIT BOUNDARY.** Suggested message: `chore(cobolnet): P15 Cut 1c — remove DifferentialBakeTool legacy oracle dependency`

- [ ] **Step 4 — Delete the legacy guard scripts.**
  - Files: `scripts/guard.sh`, `scripts/guard-fast.sh`, `scripts/guard-run-group.sh`, `scripts/guard-verify.sh`,
    `scripts/compliance.sh`, `scripts/nist-batch.sh`, `scripts/run-suite.sh`. KEEP `scripts/guard.ps1` (greenfield
    authoritative), `scripts/version-continuity-sweep.sh` (INV-1, greenfield CLI), `scripts/gen-reserved-words.ps1`
    (codegen), and any P4 `gen-cobol-words.ps1` / P3 `gen-vcr.ps1`.
  - Why: every deleted script exists solely to run or parallelize the legacy NIST loop or legacy dashboards
    (DESIGN-test-build-ci §4). `version-continuity-sweep.sh` drives the **greenfield** `cobol check-batch`, so it stays.
  - Verify:
    ```bash
    ls scripts/  # confirm only the kept scripts remain
    grep -rn "guard.sh\|guard-fast\|compliance.sh\|nist-batch\|run-suite" .github/ docs/ scripts/ ; echo "expect: NO live references"
    ```
  - **COMMIT BOUNDARY.** Suggested message: `chore(cobolnet): P15 Cut 1d — delete the 7 legacy guard/NIST bash scripts (NIST now runs in-process)`

### CUT 2 — Delete the byte engine
At the start of Cut 2, NOTHING in the greenfield build/test/CI graph references `CobolSharp.*` (Cut 1 proved it). The
only remaining references are the legacy projects referencing each other, and the two legacy test projects.

- [ ] **Step 5 — Tag & archive the legacy engine BEFORE deleting it.**
  - Create an annotated tag at HEAD (which still contains the engine) with a WSL reproduction recipe in the message, so
    the frozen oracle is recoverable forever:
    ```bash
    git tag -a legacy-byte-engine-final -m "Final commit containing the frozen CobolSharp.* byte-engine oracle (pre-G8 Cut 2).
    To run the legacy engine for a differential spot-check: check out this tag, then on WSL/Linux:
      dotnet build src/CobolSharp.CLI/CobolSharp.CLI.csproj
      bash scripts/guard.sh   # (this tag still has the guard scripts)
    The greenfield goldens under tests/differential/**/*.out and tests/nist/valid/*.txt were baked from this engine."
    git push origin legacy-byte-engine-final
    ```
  - Also write `docs/rearchitecture/LEGACY-ARCHIVE.md` (short): the tag name, the recipe, and the note that the engine
    is intentionally absent from `main` post-P15. Add a `DOC_INDEX.md` row for it.
  - Why: DESIGN scope: *"preserved at a git tag with a WSL run recipe"*. Deletion is irreversible; the tag is the
    rollback.
  - Verify: `git tag -l legacy-byte-engine-final` lists it; `git show legacy-byte-engine-final --stat | head` shows the
    engine present at the tag.
  - **COMMIT BOUNDARY.** Suggested message: `docs(cobolnet): P15 Cut 2a — archive the legacy byte engine at tag legacy-byte-engine-final + WSL recipe`

- [ ] **Step 6 — Remove legacy projects from the solution and delete the legacy trees.**
  - Files: remove from `CobolSharp.sln` the entries for `src\CobolSharp.Compiler`, `src\CobolSharp.Runtime`,
    `src\CobolSharp.CLI`, `tests\CobolSharp.Tests.Unit`, `tests\CobolSharp.Tests.Integration` (use
    `dotnet sln CobolSharp.sln remove <path>` for each). Then delete the directories:
    `src/CobolSharp.Compiler/`, `src/CobolSharp.Runtime/`, `src/CobolSharp.CLI/`,
    `tests/CobolSharp.Tests.Unit/`, `tests/CobolSharp.Tests.Integration/`.
  - Note the solution FILE is named `CobolSharp.sln`. Renaming the solution file to `Cobol.Net.sln` is a nicety but is
    a wider ripple (CI + docs reference it). Recommendation: **keep the filename `CobolSharp.sln` in P15** to avoid
    churn, and record the optional rename as a follow-on in the post-G8 architectural review (owner decision 11, out of
    scope here). If renamed, do it as its own step + `git mv` and update every `CobolSharp.sln` reference in CI/docs.
  - Why: DESIGN-module-topology §10 / COBOLNET_DESIGN §16 G8. Cut 2 is the actual removal of the byte substrate the
    PIVOT mandated never to fall back to.
  - Verify:
    ```bash
    ls src/ ; echo "expect: only Cobol.Net.* dirs"
    grep -rn "CobolSharp\.\(Compiler\|Runtime\|CLI\)" --include=*.csproj --include=*.sln --include=*.cs \
        src tests tools ; echo "expect: NO hits"
    dotnet build CobolSharp.sln            # builds only Cobol.Net.* now
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
    pwsh scripts/guard.ps1                 # exit 0
    ```
    Expected: build succeeds with no legacy project; grep clean; battery pass counts == §1 baseline.
  - **COMMIT BOUNDARY.** Suggested message:
    `feat(cobolnet)!: P15 Cut 2b — DELETE the src/CobolSharp.* byte engine + legacy test suites (G8; archived at tag)`

- [ ] **Step 7 — Grep-clean sweep for legacy residue.**
  - Search the whole repo (excluding `bin/`, `obj/`, `.git/`) for stale references and fix each: doc comments naming
    "the legacy CobolSharp.Compiler assembly" (notably `src/Cobol.Net.Frontend/Pipeline/Frontend.cs:16` banner —
    correct it to state the frontend is self-contained; P4/P1 own the code rename but the banner text may still be
    stale), `resume-prompt.md` / `CLAUDE.md` "legacy oracle" live-state mentions, and any `docs/` reference to the
    deleted scripts.
  - Verify:
    ```bash
    grep -rn "CobolSharp" --include=*.cs --include=*.csproj --include=*.md --include=*.yml \
        --include=*.ps1 --include=*.sh . | grep -v "legacy-byte-engine-final\|LEGACY-ARCHIVE\|CobolSharp.sln"
    ```
    Expected: the only surviving `CobolSharp` mentions are the intentional archive references (tag name, `LEGACY-ARCHIVE.md`,
    the `CobolSharp.sln` filename if kept) and historical DEVLOG entries (DEVLOG is an append-only ledger — do NOT
    rewrite history; leaving past entries is correct).
  - **COMMIT BOUNDARY** (if any fixes were needed). Suggested message:
    `docs(cobolnet): P15 Cut 2c — scrub stale legacy-oracle references from banners/live docs`

### CUT 2.5 — D10: SUBSCRIPT-mode removal (the relocated PHASE-04 owner-override sub-track)
Sequenced HERE, after Cut 2, because Cut 2 deleted `src/CobolSharp.*` — so `SUB_*`/`CobolParserCore.SubscriptEntryContext`
are no longer consumed by any legacy code and the SUBSCRIPT machinery can finally leave the SHARED grammar. This is a
HIGH-risk rearchitecture task; keep it a self-contained sub-track that does not touch the Cut-1/2/3 cleanup work.
Execute it FROM `docs/rearchitecture/DESIGN-frontend-grammar.md §9` (the decision-complete design), staged §9.5
D10.1–D10.5, battery-green at every commit boundary.

- [ ] **Step D10.1 — resolve §9.4 + land the before-corpus.** Answer the §9.4 space-separator decision (recommended
  Option A — preserve ISO §8.3.5 space-separated subscript/argument lists via a scoped WS mechanism; Option B narrows the
  language and is a spec violation). Add the NEW conformance/characterization corpus (§9.5 D10.1: multi-subscript
  space/comma lists, relative offsets `I+1` vs `I + 1`, signed literals `+1`/`-15.6`/`-.5`, ref-mod `(a:b)`/`(a:)`,
  qualified subscripts, nested FUNCTION args, string/national/boolean args, `table(ALL)`) captured GREEN first. **COMMIT.**
- [ ] **Step D10.2 — converge ref-mod** onto the DEFAULT-mode `refModPart`; delete the ref-mod branch of the binder's
  `InterpretSubscripts`. **COMMIT.**
- [ ] **Step D10.3 — interpreted subscript grammar rule** (per §9.4's answer) + rewrite `ReferenceResolver`'s subscript
  interpreters (`HasDepth0Colon`/`InterpretSubscripts`/`SplitSubscriptTokens`/`RenderSegment`/`ResolveSubscriptName`)
  over real `arithmeticExpression`/`subscript` nodes. **COMMIT.**
- [ ] **Step D10.4 — reunify `functionCall` onto `argumentList`** + rewrite the ~250-line `StatementBinder.Intrinsics.cs`
  recursive-descent `SUB_*` parser over real `argument` nodes; migrate `Udf`/`Emitter` (`SplitSubscriptTokens` hand-offs).
  The dominant-cost step. **COMMIT.**
- [ ] **Step D10.5 — delete the SUBSCRIPT-mode block** + the `LPAREN` mode-entry action + `PreviousTokenCouldBeDataName`
  + the now-dead structured `subscriptList/subscriptEntry/subscriptQualification/relativeOffset` rules (legacy is gone,
  so their `SubscriptEntryContext` consumer is gone); reconcile the PHASE-04 Group-A `cobol-words.json` drift test (the
  `subscriptTrigger` column goes dead — regenerate + adjust the drift assertion). **COMMIT.**
- **Verify (each step):** greenfield battery + `guard.ps1` + INV-1-strong; the D10.1 corpus green; token equivalence is
  NOT the metric (tokens change by design — prove OUTPUT/behavior equivalence). Exit criterion 0 holds when D10.5 lands.

### CUT 3 — Runtime namespace flip (`CobolNet.Runtime` → `Cobol.Net.Runtime`)
This is the only step here that changes emitted code; it is a coordinated flip made trivial by the `RuntimeApi` façade.

- [ ] **Step 8 — Flip the runtime library's own namespace.**
  - Files: `src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` — set `<RootNamespace>Cobol.Net.Runtime</RootNamespace>`
    and rewrite the banner (drop "renamed from CobolNet.Runtime at G0 … stays CobolNet.Runtime through G0-G7"; state the
    assembly and root namespace are now coherent). Then rewrite every `namespace CobolNet.Runtime` declaration across
    `src/Cobol.Net.Runtime/**/*.cs` to `namespace Cobol.Net.Runtime` (preserving any sub-namespace suffix, e.g.
    `CobolNet.Runtime.IO` → `Cobol.Net.Runtime.IO`). This is a mechanical find/replace of the exact token
    `namespace CobolNet.Runtime` → `namespace Cobol.Net.Runtime` plus internal `using CobolNet.Runtime…` → `using
    Cobol.Net.Runtime…` within the runtime project itself.
    - Sub-namespaces: DESIGN-runtime-library §2.8 says "realize the sub-namespaces" at this flip (`.Values/.IO/.Control/
      .Exceptions/.Intrinsics/.Verbs`). This is an OPEN QUESTION in the design (flat vs sub-namespaced). **Recommended
      for P15: flip the root token only (keep whatever sub-namespace structure P8's folder reorg left in place).**
      Deepening the namespace tree beyond the mechanical root flip is optional and, if done, must be reflected in the
      façade's emitted `using` set (step 9). Do the minimal, provably-one-using flip first; treat sub-namespace
      deepening as a follow-on only if the façade already emits fully-qualified member names.
  - Why: DESIGN-runtime-library §2.8 / §4 step 6. Assembly/namespace coherence; the deliberate deferral ends here.
  - Verify: `dotnet build src/Cobol.Net.Runtime/Cobol.Net.Runtime.csproj` succeeds; `grep -rn "namespace CobolNet.Runtime"
    src/Cobol.Net.Runtime ; echo "expect: NO hits"`. (The COMPILER will not build yet — it still emits/uses the old
    namespace until step 9. Do steps 8 and 9 in ONE commit.)

- [ ] **Step 9 — Flip the emitted `using` (one place) + any compiler-side runtime type references.**
  - Files: the `RuntimeApi` façade (`src/Cobol.Net.Compiler/CodeGen/.../RuntimeApi.cs`) — the single place that
    produces the generated program's `using CobolNet.Runtime;` (and every `Cobol*` runtime member name). Flip its
    emitted namespace constant(s) to `Cobol.Net.Runtime`. Then fix any direct compile-time references to runtime types
    inside the compiler (e.g. `using CobolNet.Runtime;` in binder/emitter files that call `ExceptionCatalog`,
    `CobolEdit.MaskCapacity`, `CobolDate.*`, `RoundingModes`, etc.) — these are `using` swaps to `Cobol.Net.Runtime`.
    Find them with `grep -rn "CobolNet.Runtime" src/Cobol.Net.Compiler`.
  - Why: DESIGN-runtime-library §2.8: "one file emits the `using`s, so generated `using CobolNet.Runtime;` flips to
    `using Cobol.Net.Runtime;` in exactly one place." The façade (P7) is precisely what makes this a one-emitted-change
    flip instead of corpus-wide churn.
  - Verify:
    ```bash
    dotnet build CobolSharp.sln
    # Compile a representative program and inspect the emitted .g.cs + run it.
    cat > /tmp/p15.cob <<'EOF'
    IDENTIFICATION DIVISION.
    PROGRAM-ID. P15NS.
    DATA DIVISION.
    WORKING-STORAGE SECTION.
    01 WS-N PIC 9(4) VALUE 42.
    PROCEDURE DIVISION.
    MAIN.
        DISPLAY "N=" WS-N.
        STOP RUN.
    EOF
    dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /tmp/p15.cob --std 2023 -o /tmp/p15.dll --run
    grep -n "using Cobol.Net.Runtime\|using CobolNet.Runtime" /tmp/p15.g.cs 2>/dev/null || \
        grep -rn "using .*Runtime" /tmp  # confirm the .g.cs shows `using Cobol.Net.Runtime`
    ```
    Expected: builds clean; program prints `N=0042`; the emitted `.g.cs` shows `using Cobol.Net.Runtime` and **no**
    `using CobolNet.Runtime`; the runtime DLL deployed alongside is `Cobol.Net.Runtime.dll` (assembly name unchanged —
    only the namespace moved, so no deployment path changes).
  - Then the full battery:
    ```bash
    dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj
    dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj
    pwsh scripts/guard.ps1
    ```
    Expected: pass counts == §1 baseline (behavior-neutral: a namespace is a compile-time name only — runtime behavior
    and emitted-output bytes are identical). If the characterization/emit-snapshot gate (P0) flags the `.g.cs` `using`
    line as changed, that is the ONE expected, reviewed re-baseline — re-baseline the snapshots in THIS commit with a
    DEVLOG note ("emit change = runtime namespace flip; gate-1 output goldens prove behavior-neutral").
  - **COMMIT BOUNDARY** (steps 8 + 9 together — the tree does not build between them). Suggested message:
    `refactor(cobolnet)!: P15 Cut 3 — flip runtime RootNamespace CobolNet.Runtime → Cobol.Net.Runtime via the RuntimeApi façade (one emitted-using change)`

- [ ] **Step 10 — Regenerate `Generated/` from clean and confirm.**
  - `Generated/` is a build output (gitignored); a from-clean build must regenerate it without error and the emitted
    parser must be consistent with the flipped namespaces (the frontend's generated namespace is `CobolNet.Frontend.Generated`
    per P1/P4 — unaffected by the runtime flip, but re-verify a cold build).
  - Verify:
    ```bash
    dotnet clean CobolSharp.sln
    rm -rf src/Cobol.Net.Frontend/Generated
    dotnet build CobolSharp.sln           # regenerates Generated/, builds green
    pwsh scripts/guard.ps1                 # exit 0
    ```
  - No commit needed if nothing tracked changed (Generated/ is untracked). If the regen surfaced a tracked drift, fix
    and commit.

### Conformance documentation + final doc pass

- [ ] **Step 11 — Author `docs/CONFORMANCE.md` (the §4.2.16 set).** See §7 for the required section map, sourcing, and
  the derive-don't-guess method. This is a large writing task; treat it as its own commit(s). Every claim cites a spec
  § and, where behavioral, a passing conformance test / golden. Cross-link from `README.md`.
  - Verify: the §7 checklist is fully satisfied; `grep -c "§" docs/CONFORMANCE.md` shows dense citation; the "claimed
    optional features" table matches what actually compiles+runs (spot-check ≥1 program per claimed A.4 subsection).
  - **COMMIT BOUNDARY.** Suggested message: `docs(cobolnet): P15 — publish the ISO §4.2.16 conformance / user documentation set (CONFORMANCE.md)`

- [ ] **Step 12 — Final DOC_INDEX + doc reconciliation.**
  - File: `docs/DOC_INDEX.md`. Remove rows for the deleted byte-engine architecture guides (already deleted at the
    PIVOT, but confirm no dangling rows), the deleted guard scripts (if indexed), and any doc that referenced the
    legacy engine as live. Add rows for `CONFORMANCE.md`, `LEGACY-ARCHIVE.md`, and this phase doc
    (`PHASE-15-…md`). Update the preamble count. Also update `resume-prompt.md`'s STATE banner and `CLAUDE.md`'s PIVOT
    STATE line to record G8 complete (legacy severed; runtime namespace flipped; conformance doc published).
  - Verify:
    ```bash
    # every doc referenced in DOC_INDEX exists; no dangling links
    grep -oE "docs/[A-Za-z0-9_./-]+\.md|[A-Za-z0-9_-]+\.md" docs/DOC_INDEX.md | sort -u | \
        while read f; do [ -e "docs/$f" ] || [ -e "$f" ] || echo "MISSING: $f"; done
    ```
    Expected: no `MISSING:` lines.
  - **COMMIT BOUNDARY.** Suggested message:
    `docs(cobolnet): P15 — reconcile DOC_INDEX + live-state banners for G8 completion`

- [ ] **Step 13 — Final exit-criteria gate.** Run §5 in full; confirm all five EXIT CRITERIA hold; set STATUS to `DONE`.

---

## 5. Verification (full battery at phase end)
Run all of these from a clean checkout on BOTH Windows and Linux (WSL) — cross-platform parity is now the whole gate:

```bash
dotnet clean CobolSharp.sln && dotnet build CobolSharp.sln            # green, warnings-as-errors in Release
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj   # == §1 baseline (~2028+)
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj                 # == §1 baseline (~213+)
pwsh scripts/guard.ps1                                                # exit 0 (greenfield NIST corpus all green)
bash scripts/version-continuity-sweep.sh | grep -q BREAKS && echo FAIL || echo "INV-1 OK"
```

Behavior-neutrality / byte-exact checks specific to P15:
- **Namespace flip is behavior-neutral:** the NIST golden set (`tests/nist/valid/*.txt`) and the differential goldens
  (`tests/differential/**/*.out`) must match byte-for-byte before and after Cut 3 — a namespace rename cannot change
  program output. The ONLY permitted diff is the emitted `.g.cs` `using` line (the P0 emit-snapshot gate re-baseline in
  step 9).
- **Legacy-delete is behavior-neutral:** Cut 1/Cut 2 change no `src/Cobol.Net.*` production code, so the battery counts
  must be identical to §1. A count change means an accidental deletion of something the greenfield tree needed —
  investigate before committing.
- **Grep gates (exit criterion 1):** the three grep sweeps in steps 6/7 return no live `CobolSharp.*` hits.

Equivalence-proof note: the authoritative equivalence proof (greenfield == legacy over the whole corpus) was produced
in P14 and is the gate that authorizes P15's irreversible deletes. P15 does not re-run it (legacy is being deleted);
it relies on P14's green result + the committed goldens as the frozen record of that equivalence.

---

## 6. Rollback / resumability
- **Resuming mid-phase:** read the STATUS line and the last `[x]` checkbox. Each step is its own commit boundary, so
  `git log --oneline | grep "P15"` shows exactly how far the phase got. Resume at the next `[ ]`.
- **Rollback of a namespace flip (Cut 3):** it is a mechanical rename; `git revert` the Cut-3 commit restores
  `CobolNet.Runtime` cleanly. Low risk.
- **Rollback of the engine delete (Cut 2):** the engine is at tag `legacy-byte-engine-final`. To restore, `git checkout
  legacy-byte-engine-final -- src/CobolSharp.Compiler src/CobolSharp.Runtime src/CobolSharp.CLI tests/CobolSharp.Tests.Unit
  tests/CobolSharp.Tests.Integration scripts/guard.sh` and re-add the sln entries. This is the documented recovery
  path; deletion is irreversible only in the sense that `main` no longer carries it.
- **Risks & mitigations:**
  - *Risk:* a hidden greenfield dependency on a legacy type surfaces only after Cut 2. *Mitigation:* the Cut-1 grep
    gate (step 1 verify) proves the compile-time graph is already legacy-free BEFORE Cut 2, so Cut 2 cannot break the
    build. If it does, a `using` alias to a `CobolSharp.*` type was hiding in a non-referenced-project file — restore
    from the tag, port the type into `Cobol.Net.*`, retry.
  - *Risk:* the namespace flip is more than one emitted place (the `RuntimeApi` façade is incomplete). *Mitigation:*
    step 9's `.g.cs` inspection catches a stray `using CobolNet.Runtime` immediately; if found, that member bypasses
    the façade — route it through `RuntimeApi` first (a P7 gap), then flip. Do NOT hand-flip generated strings scattered
    across the emitter; fix the façade.
  - *Risk:* a re-baselined emit snapshot masks a real behavioral change. *Mitigation:* gate-1 output goldens
    (NIST + differential `*.out`) are the authority; they must be untouched. Only the `using` line may move.
  - *Risk:* CI still gates on a deleted script/job. *Mitigation:* step 2 edits `build-and-test.yml` before Cut 2; push
    to a branch and confirm the workflow is green with only greenfield jobs before merging Cut 2.

---

## 7. ISO conformance documentation — `docs/CONFORMANCE.md` (spec sourcing + section map)
The ONLY ISO feature *work* in P15 is authoring the mandatory user documentation (§4.2.16). No new language features
are implemented here (they landed in P9–P13). The document is the artifact that lets the compiler *claim* conformance.

### 7.1 What §4.2.16 requires
`specs/ISO_COBOL.md` §4.2.16 (line 2539): "An implementation shall satisfy the user documentation requirements
specified in **4.2.3, 4.2.4, 4.2.5, 4.2.6, 4.2.10, 4.2.12, and 4.2.13** by specification in at least one form of
documentation." Documentation may reference other documents. So the required set is:

| ISO § | Requirement | Source in this repo |
|---|---|---|
| §4.2.3 (line 2402) | Non-COBOL runtime-module interaction: document languages/implementations supported (or state none). | Runtime is typed-native .NET; document .NET interop stance (CALL to non-COBOL: supported/none). |
| §4.2.4 (line 2407) | Interaction between COBOL implementations: document implementations supported (or none). | Cross-vendor object/file interchange stance. |
| §4.2.5 (line 2422) → **A.1** (line 39232) | Implementor-defined language elements: document each supported; at minimum the ones A.1 marks *required*; document those A.1 marks as *requiring user documentation*. ~100 rows. | Derive from A.1 rows × what the compiler actually does (collating, default USAGE, currency, coded char set, line delimiter, arithmetic limits, etc.). |
| §4.2.6 (line 2431) → **A.3** (line 40052) | Processor-dependent elements: document those for which support is CLAIMED **and** the ABSENCE of those not supported (§4.2.6 last ¶: "The absence of processor-dependent elements … shall be specified"). | A.3 rows × claim/absence + `--std` gating. |
| §4.2.7 (line 2440) → **A.4** (line 40229) | Optional elements: identify those claimed; if partial, list supported vs not-supported parts. Includes **A.4.10 Object orientation**, **A.4.11 Report Writer**, **A.4.12 RESUME statement** (and A.4.2 screen, A.4.3 commit/rollback, A.4.4 dynamic-capacity tables, A.4.5 dynamic length, A.4.6 extended letters, A.4.7 sharing/locking, etc.). | A.4 subsection rows × implemented?/edition. |
| §4.2.10 (line 2466) | Nonstandard extensions claimed **and any reserved words added** for them. | The compiler's extension list (if any) + added reserved words from `reserved-words.json`. |
| §4.2.12 (line 2496) → **F.1** | Archaic elements present in the implementation. | F.1 rows × supported. |
| §4.2.13 (line 2505) → **F.2** | Obsolete elements present in the implementation. | F.2 rows × supported. |

Also include (good practice + referenced by the required sections): §4.2.2 the warning mechanism (`--std`, permissive/
strict), §4.2.8 reserved words recognized (§8.9), §4.2.9 standard extensions, §4.2.15 limits (max digits, table sizes),
§4.2.17 character substitution.

### 7.2 Sourcing method — DERIVE, do not hand-guess (owner rule: cite the §)
The canonical machine-readable inputs already exist; the document must be generated/validated from them, not written
from memory:
- `tests/version-matrix/constructs.json` (+ P3's generated `docs/VERSION_CHANGE_REFERENCE.md`) — which constructs are
  introduced/removed/available at each edition. Every A.1/A.3/A.4/F.1/F.2 claim maps to construct rows here.
- `reserved-words.json` / `ReservedWords.Table` — for §4.2.8 recognized words and §4.2.10 added reserved words.
- The passing conformance tests + NIST goldens — a claim of "supported" MUST be backed by a green test. For each
  claimed A.4 subsection, cite ≥1 passing program (e.g. A.4.10 OO → `OoSpineTests`; A.4.4 dynamic-capacity tables →
  `OccursDynamicGuardTests`; A.4.11 Report Writer → `ReportWriterConformanceTests`; A.4.7 sharing/locking → the file
  I/O locking tests).
- `docs/ISO2023_CONFORMANCE_PLAN.md` — the M3/M4 pending list; anything still pending is documented as **not supported**
  (honest absence), not silently claimed. §4.2.6/§4.2.7 explicitly require documenting non-support.

Recommended: extend an existing generator (P3's `scripts/gen-vcr.ps1`) to emit the A.1/A.3/A.4/F.1/F.2 claim tables
from `constructs.json` + harness results so the conformance doc is regenerable and cannot silently drift from the
implementation (mirrors the `gen-reserved-words.ps1` discipline). A hand-written narrative wraps the generated tables.

### 7.3 `docs/CONFORMANCE.md` section map (author to this outline)
1. **Title / scope / edition.** "ISO/IEC 1989:2023 conformance statement for COBOL.NET (cobol). Default `--std 2023`;
   supported editions 85/2002/2014/2023." Note conformance is claimed per the edition selected by `--std`.
2. **§4.2.2 Warning mechanism.** How to invoke conformance/extension/archaic/obsolete/nonstandard warnings (`--std`,
   `--permissive`/strict, the diagnostic codes band). Reference the diagnostic registry / `docs/DIAGNOSTICS.md`.
3. **§4.2.3 Non-COBOL interaction.** State the supported interop (typed-native .NET; CALL semantics) or explicit none.
4. **§4.2.4 COBOL-implementation interaction.** State supported cross-implementation interchange or explicit none.
5. **§4.2.5 / A.1 Implementor-defined elements.** The full A.1 table (row → the implementor's definition → cite the
   compiler behavior/spec §). Mark required rows. This is the ~100-item core.
6. **§4.2.6 / A.3 Processor-dependent elements.** Table of claimed (with the syntax/functionality subset if a standard
   extension) AND a table of **absences** (explicitly not supported).
7. **§4.2.7 / A.4 Optional elements.** Per A.4 subsection: claimed? fully/partially? at which editions? backing test.
   MUST include A.4.10 (OO), A.4.11 (Report Writer), A.4.12 (RESUME) rows.
8. **§4.2.9 Standard extensions / §4.2.10 Nonstandard extensions.** The extension list + any reserved words added
   (§4.2.10 last ¶ mandates specifying added reserved words).
9. **§4.2.12 / F.1 Archaic** and **§4.2.13 / F.2 Obsolete** elements present in the implementation.
10. **§4.2.15 Limits.** Max numeric digits (standard/extended), table dimensions, nesting, etc.
11. **§4.2.17 Character substitution & §8.1.3 repertoire.** The coded character set (UTF-16 native), any substitutions.
12. **References.** The spec, the version matrix, the diagnostics doc, the test corpus.

### 7.4 Conformance tests / goldens to add in P15
No new *language* goldens (features are frozen). Add instead a **documentation-integrity test** so the conformance
claims cannot rot:
- `tests/Cobol.Net.Tests.Conformance/ConformanceDocDriftTests.cs` — asserts every A.4 subsection the doc marks
  "supported" has a named passing test, and every construct the doc marks "not supported" is `pending`/absent in
  `constructs.json`. This binds `CONFORMANCE.md` to the harness the same way `CorpusManifestTests` binds `corpus.tsv`
  (DESIGN-test-build-ci §3.5). Green = the doc's claims match reality.

---

## 8. Commit / DEVLOG discipline (per project rules)
Every commit boundary above gets a DEVLOG entry at the TOP of `DEVLOG.md` (descending; real `date "+%Y-%m-%d %H:%M %Z"`
stamp; `## Entry NNN — … — Title`), referencing "P15 / G8". Commit messages are forensic and end with the mandated
Co-Authored-By / Claude-Session trailers. Push every checkpoint (fully-autonomous rule). After the phase, update
`resume-prompt.md` STATE + `CLAUDE.md` PIVOT STATE to "G8 COMPLETE".
