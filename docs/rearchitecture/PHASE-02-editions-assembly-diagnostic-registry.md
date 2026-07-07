# PHASE 02 — `Cobol.Net.Editions` leaf assembly + first-class diagnostic registry

- **Phase:** P2
- **Track:** rearchitecture
- **Risk:** MEDIUM
- **Depends on:** P1 (mechanical namespace rename `CobolSharp.Compiler.* → CobolNet.*` +
  `CobolNet.Frontend.Generated`; dead-grammar / JSON-XML removal). This phase assumes P1 has already
  renamed the frontend namespaces and the generated-parser package. See **Precondition check** below —
  if P1 is NOT done, the file paths still hold but the `namespace`/`using` lines you edit will read
  `CobolSharp.Compiler.*`; adapt accordingly (do NOT do the rename here — that is P1).
- **Related design (READ FIRST):**
  - `docs/rearchitecture/DESIGN-edition-framework.md` (the SSOT for this phase; §2 target, §3 module
    map, §4 migration order, §5 risks, §6 open questions).
  - `docs/rearchitecture/DESIGN-frontend-grammar.md` (predicate-stamping + `Cobol.Net.Editions` boundary).
  - `docs/rearchitecture/DESIGN-test-build-ci.md` (the first-class diagnostic-code registry, 0899 split,
    drift tests, `docs/DIAGNOSTICS.md`).
  - `docs/COBOLNET_DESIGN.md` §16 (G0–G8), `docs/VERSION_TEST_MATRIX_DESIGN.md`, `docs/VERSION_CHANGE_REFERENCE.md`.

## Goal (one paragraph)

Give the edition machinery and all ~163 diagnostic codes ONE home visible to **both** `Cobol.Net.Frontend`
and `Cobol.Net.Compiler`, ending the reverse-engineering and severity-policy duplication that fragments
the four-compilers-in-one framework. Concretely: stand up a new lowest-layer assembly
`src/Cobol.Net.Editions`, referenced by both Frontend and Compiler; move `ConstructRegistry` /
`ConstructDialectStatus`, `ReservedWords[.Table]`, and `EditionCodes` into it; split the overloaded
`EditionContext` into an immutable `EditionInfo` value + a structured `IDiagnosticSink`/`EditionDiagnostic`,
kept byte-stable behind an adapter so all ~290 call sites compile untouched; make **one**
`EditionSeverityPolicy` the single strict/permissive decision consumed by the binder, the validator, and
both frontend preprocessor gates; **generate** `ConstructRegistry.Entries` + `Constructs.*` id-consts from
`constructs.json`; add a first-class `DiagnosticDescriptors` registry (code → {stable id, ISO §, severity,
template, edition, suppress-key}) that splits the ~44-site `COBOLNET0899` catch-all and disambiguates reused
codes (e.g. `1533`), generates `docs/DIAGNOSTICS.md`, and is guarded by drift tests; **delete
`EditionGateHints`**, replacing its reverse-engineering table with forward `{Gate(edition, GateId)}?`
predicate stamping on `CobolParserCoreBase`; and route the five inline pre-band gates
(`0816`/`0803`/`0810`/`0811`/`0882`) through registry rows with their pinned codes.

## Exit criteria (phase is DONE when ALL hold)

1. Full battery green: greenfield conformance (2028+) + unit (213+) + the FULL legacy guard (NIST **353
   MATCH**), zero regressions, diagnostic goldens byte-stable.
2. `Cobol.Net.Editions` exists and is referenced by **both** `Cobol.Net.Frontend` and `Cobol.Net.Compiler`
   (verify: both `.csproj` have the `ProjectReference`).
3. `EditionGateHints.cs` is **deleted**; there is no duplicate edition-diagnostic origin (the parse-layer
   `COBOLNET0900` now comes from `ConstructRegistry.Check` via predicate stamping, not a signature table).
4. `constructs.json`, `reserved-words.json`, and the diagnostic descriptors are bound to code by **drift
   tests** (`ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, new `DiagnosticRegistryDriftTests`).
5. The ~290 `EditionContext` call sites are green via the adapter (or migrated — the adapter may remain at
   phase end; retiring it is optional and can slip to P7).
6. The five inline gates are registry rows (pinned codes preserved) and appear in the version matrix
   (each has a positive+negative fixture in `constructs.json`; `VersionMatrixTests` green).
7. `docs/DIAGNOSTICS.md` is generated from the descriptor catalogue and is in sync (drift test green);
   `DOC_INDEX.md` has its row.

**OUT of scope (do NOT do here):** the version-gating *behavior* audit + VCR generation +
`VersionBehaviorMatrixTests` (P3); `EditionValidator` removal-gate *waves* / new construct gates (P3);
any binder/emitter god-class decomposition (P6/P7). Renaming `EditionContext` to its final name and
retiring the adapter across all 290 sites is *optional* here and may defer to P7.

## STATUS

`NOT STARTED`

> The executing session MUST update this line as it works: `IN PROGRESS @ step N` while working, then
> `DONE` when every exit criterion holds. Record the DEVLOG entry numbers you allocate next to each commit.

---

## 1. Precondition check (run before step 1)

```bash
# P1 must be landed. Confirm the frontend namespaces are already CobolNet.Frontend.*:
grep -rl "namespace CobolSharp.Compiler" src/Cobol.Net.Frontend/ | head        # expect: NO hits (or only Generated/ if P1 partial)
grep -n "namespace CobolNet.Frontend.Generated" src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs  # expect a hit after P1
# Baseline the battery so you can prove neutrality later:
dotnet build CobolSharp.sln -c Debug
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug
bash scripts/guard-fast.sh     # NIST 353 MATCH (Windows: run under WSL, or use scripts/guard.ps1 if present)
```

If P1 is NOT landed, the code below still applies but you will read/write `CobolSharp.Compiler.*`
namespaces in the frontend files; keep those spellings and let P1 do the rename. **Do not rename in P2.**

---

## 2. Rationale — the problems this phase fixes (grounded in AS-BUILT code)

Assembly layering today: `Runtime → Frontend → Compiler → Cli`. **`Frontend` has no project reference
to `Compiler`.** The canonical construct registry lives *above* the frontend, so the frontend physically
cannot see it. Every duplication below is a symptom of that one fact.

| # | Problem | Evidence (file:line) |
|---|---------|----------------------|
| P1 | Registry invisible to the frontend → the parse layer re-encodes edition metadata. `EditionGateHints` hand-copies `(Display, IntroducedIn, Citation, RowId)` for ~30 constructs that already exist as `ConstructDialectStatus` rows. | `src/Cobol.Net.Frontend/Parsing/EditionGateHints.cs:35-63` vs `src/Cobol.Net.Compiler/Validation/ConstructDialectStatus.cs:60-168` |
| P2 | Severity policy reimplemented in the frontend twice more (`if(permissive) Warning else Error`) instead of the one `EditionContext.Removed` seam. | `src/Cobol.Net.Frontend/Preprocessor/ReferenceFormatProcessor.cs:120-148` (inner `EditionGates`); `CopyProcessor.cs` |
| P3 | `EditionGateHints` reverse-engineers what the `{isXXXX()}?` gate already knew — ~30 empirical `(token, rule-stack, lookahead)` signatures, with a documented residue (`inlineMethodInvocation`, `SET…TO objref`). | `EditionGateHints.cs:72-175` |
| P4 | Two parallel mechanisms for removed/introduction gating. Five gates stay inline with pinned codes and **never enter the version matrix**: `0816` END-ACCEPT, `0803` ROUNDED MODE, `0810` ALTER, `0811` bare GO TO, `0882` CALL ON OVERFLOW. | `StatementBinder.Accept.cs:44`, `StatementBinder.cs:1228`, `StatementBinder.AlterSwitches.cs:118,136`, `StatementBinder.Call.cs:175` |
| P5 | `DialectLevel` is triple-sourced: `CobolParserCoreBase.DialectLevel {get;set;}=85`, `Frontend.DialectLevel`, `EditionContext.DialectLevel` — kept equal by convention. | `CobolParserCoreBase.cs:17`, `EditionContext.cs:29` |
| P6 | `EditionContext` is really `EditionInfo + DiagnosticSink`: immutable facts (`DialectLevel/Permissive/MaxDigits`) **plus** two `public List<string>` stringly-typed accumulators (`$"error {code}: {msg}"`). 290 call sites. | `EditionContext.cs:26-84` |
| P7 | Edition metadata lives in 3+ representations, drift-protected in only 2 (`constructs.json`↔`Entries`, `reserved-words.json`↔`ReservedWords.Table`). `EditionGateHints`, the preprocessor gates, and the inline gates are unbound. | — |
| P8 | Diagnostics are stringly-typed on the compiler side: `EditionContext.Error(code, msg)` string-concats; the `COBOLNET0899` catch-all is emitted from ~44 distinct sites; codes are reused across rules (e.g. `1533`). No descriptor registry, no `docs/DIAGNOSTICS.md`, no `--suppress` addressability. | `grep COBOLNET0899 src/Cobol.Net.Compiler` = 65 hits / 13 files; `EditionContext.cs:52,56` |

**What is already good and MUST be preserved:** `ConstructRegistry.Check` is the ONE gating funnel;
`EditionContext.Removed` is the ONE severity seam; the `constructs.json`/`reserved-words.json` drift tests;
the `ConstructAvailability` verdict shape; the `pending`-row scheduling; the conservative "only
high-confidence reserved words reject" policy; the rich ISO §-citation on every row. The `Check` body is
copied **verbatim** in the migration so no diagnostic text changes.

---

## 3. Target end-state for this phase (concrete file/type inventory)

New assembly **`src/Cobol.Net.Editions/`** (`AssemblyName Cobol.Net.Editions`, `RootNamespace
CobolNet.Editions`), depending only on the ANTLR runtime **iff** the gate helper needs it (Open Q3 — the
recommendation is **dependency-free**; keep the `GateId → construct-id` mapping in the frontend and pass
primitives across). Referenced by both `Cobol.Net.Frontend` and `Cobol.Net.Compiler`.

```
src/Cobol.Net.Editions/
  Cobol.Net.Editions.csproj
  EditionInfo.cs                immutable value: Year, Permissive, MaxDigits, Has(introducedIn)
  EditionSeverity.cs            enum { Error, Warning }
  EditionSeverityPolicy.cs      static For(ConstructAvailability, EditionInfo) -> EditionSeverity
  EditionDiagnostic.cs          readonly record struct (Code, Severity, ConstructId, Message, Where, Citation, Location?)
  IDiagnosticSink.cs            void Report(in EditionDiagnostic)
  EditionCodes.cs               (MOVED) COBOLNET0900-0903 band + pinned 08xx consts
  ConstructDialectStatus.cs     (MOVED) verdict record + ConstructAvailability enum + StatusAt
  ConstructRegistry.cs          hand-written Check() funnel (partial) — now (EditionInfo, IDiagnosticSink, id, where)
  ConstructRegistry.g.cs        (GENERATED from constructs.json) Entries + _byId + Find
  Constructs.g.cs               (GENERATED) const string per constructs.json row id
  ReservedWordEntry.cs          (MOVED)
  ReservedWords.cs              (MOVED) ReservedWords + ReservedWordSet
  ReservedWords.Table.g.cs      (GENERATED, as today — the gen-reserved-words.ps1 output, renamed .g.cs)
  Diagnostics/
    DiagnosticSeverity.cs       (shared severity — reconcile with the frontend's copy; see step 9)
    DiagnosticDescriptor.cs     record (Code, Id, Severity, IsoSection, Template, IntroducedEdition?, SuppressKey)
    DiagnosticDescriptors.cs    hand-written partial: the compiler-side catalogue (edition band + the ex-0899 splits + disambiguated reuses)
  Gating/
    GateId.cs                   (GENERATED) enum, one member per introduction-gated construct → its constructs.json id

src/Cobol.Net.Editions.SourceGen/            (only if Open Q1 chooses the source generator)
  Cobol.Net.Editions.SourceGen.csproj        (netstandard2.0 analyzer)
  ConstructRegistryGenerator.cs              reads constructs.json (AdditionalFiles) -> Entries + Constructs + GateId
```

Changes to existing files:

- `src/Cobol.Net.Compiler/Binding/EditionContext.cs` → becomes a **thin adapter** wrapping an
  `EditionInfo` + a `List<string>`-backed `IDiagnosticSink`, exposing the *unchanged* public surface
  (`DialectLevel`, `Permissive`, `Diagnostics`, `Warnings`, `HasErrors`, `MaxDigits`, `Error`, `Warning`,
  `Removed`, `CheckDigitCapacity`). All 290 call sites compile untouched.
- `src/Cobol.Net.Compiler/Validation/{ConstructDialectStatus,EditionCodes,ReservedWords,ReservedWords.Table}.cs`
  → **deleted** (moved to `Editions`); `EditionValidator.cs` keeps its position, now `using CobolNet.Editions;`.
- `src/Cobol.Net.Frontend/Parsing/EditionGateHints.cs` → **deleted**.
- `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs` → gains `EditionInfo Edition`,
  `Gate(int, GateId)`, `LastRejectedGate`, `LastRejectedTokenIndex`; `is85/2002/2014/2023` delegate to
  `Edition.Has`; `DialectLevel` becomes a shim over `Edition.Year` (kept for P1-compat, single-sourced).
- `src/Cobol.Net.Frontend/Parsing/CobolErrorStrategy.cs` → reads `parser.LastRejectedGate` and calls
  `ConstructRegistry.Check` (now frontend-visible) to build the `COBOLNET0900` message.
- `src/Cobol.Net.Frontend/Preprocessor/{ReferenceFormatProcessor,CopyProcessor}.cs` → the inner severity
  gates call `EditionSeverityPolicy.For` + `ConstructRegistry.Check` over a shared sink; delete the local
  `if(permissive)` tests.
- The five inline gate sites → `ConstructRegistry.Check(edition, sink, Constructs.X, where)` with rows
  added to `constructs.json` (pinned codes in a new `diagnosticCode` field).
- `tests/version-matrix/constructs.json` → enriched with `display`/`citation`/`diagnosticCode` fields the
  generator needs, plus the five new inline-gate rows with positive+negative fixtures.
- `docs/DIAGNOSTICS.md` (new, generated), `docs/DOC_INDEX.md` (row added),
  `docs/rearchitecture/DESIGN-edition-framework.md` (status banner updated to reflect what landed).

---

## 4. STEP-BY-STEP

> **Discipline reminders (from `CLAUDE.md` / memories):** run the battery after **every** change; commit at
> each COMMIT BOUNDARY only with the battery green; a shared `.g4` change requires the **FULL legacy guard**
> in the same change set (`feedback_legacy_suite_on_shared_corpus`, `feedback_autonomous_grammar_nist`);
> every commit gets a DEVLOG entry (`feedback_devlog_per_commit`), newest-first, real timestamp; commit
> messages are forensically detailed; push every checkpoint (`feedback_fully_autonomous_push`). The `Check`
> body must be copied **verbatim** so behavior stays byte-stable (`feedback_diff_is_a_bug`).

### Step 1 — Create the `Cobol.Net.Editions` assembly (empty, referenced)

**Files:** create `src/Cobol.Net.Editions/Cobol.Net.Editions.csproj`; edit `CobolSharp.sln`,
`src/Cobol.Net.Frontend/Cobol.Net.Frontend.csproj`, `src/Cobol.Net.Compiler/Cobol.Net.Compiler.csproj`.

**Change:**
- `.csproj` skeleton:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <RootNamespace>CobolNet.Editions</RootNamespace>
      <AssemblyName>Cobol.Net.Editions</AssemblyName>
    </PropertyGroup>
    <ItemGroup>
      <InternalsVisibleTo Include="Cobol.Net.Tests.Unit" />
    </ItemGroup>
    <!-- Q3: dependency-free is the recommendation. Add the ANTLR ref ONLY if the gate helper needs it. -->
  </Project>
  ```
- Add the project to `CobolSharp.sln` (new GUID; place under the existing `src` solution folder
  `{827E0CD3-B72D-47B6-A68D-7590B98EB39B}`; add `Debug|Any CPU` build config lines and the nested-project
  mapping, mirroring `Cobol.Net.Compiler`'s entries). Easiest reliable path:
  `dotnet sln CobolSharp.sln add src/Cobol.Net.Editions/Cobol.Net.Editions.csproj`.
- Add `<ProjectReference Include="..\Cobol.Net.Editions\Cobol.Net.Editions.csproj" />` to **both**
  `Cobol.Net.Frontend.csproj` and `Cobol.Net.Compiler.csproj`. Editions is BELOW Frontend, so Editions must
  NOT reference Frontend/Compiler (dependency direction: `Editions ← Frontend ← Compiler ← Cli`).
- Add one placeholder file `src/Cobol.Net.Editions/EditionInfo.cs` (see step 2) so the assembly is non-empty.

**Why:** the single root-cause fix (P1/P2/P7) — a shared low layer both Frontend and Compiler can see.

**Verify:** `dotnet build CobolSharp.sln -c Debug` succeeds; there is no reference cycle
(`dotnet build src/Cobol.Net.Editions/Cobol.Net.Editions.csproj` builds standalone).

**COMMIT BOUNDARY** — `feat(cobolnet): P2.1 — stand up the Cobol.Net.Editions leaf assembly (referenced by Frontend + Compiler) (DEVLOG N)`

---

### Step 2 — Add the new value/policy/sink types (no moves yet)

**Files:** create in `src/Cobol.Net.Editions/`: `EditionInfo.cs`, `EditionSeverity.cs`,
`EditionDiagnostic.cs`, `IDiagnosticSink.cs`, `EditionSeverityPolicy.cs`.

**Change (target shapes — copy from `DESIGN-edition-framework.md` §2.2–2.5):**
- `EditionInfo` — `public readonly record struct EditionInfo(int Year, bool Permissive = false)` with
  `static EditionInfo Latest = new(2023)`, `static Of(int, bool)`, `int MaxDigits => Year < 2002 ? 18 : 31`,
  `bool Has(int introducedIn) => Year >= introducedIn`, and a `Validate(year)` guard that throws on a year
  not in `{85,2002,2014,2023}` (defends against a `default(EditionInfo)` with `Year==0` — risk R5). Port the
  `MaxDigits` doc-comment verbatim from `EditionContext.cs:47-49` (ISO §8.3.1.2).
- `EditionSeverity` — `enum { Error, Warning }`.
- `EditionDiagnostic` — `readonly record struct EditionDiagnostic(string Code, EditionSeverity Severity,
  string ConstructId, string Message, string Where, string Citation, SourceLocation? Location = null)`. Use
  the frontend's existing `CobolNet.Frontend.Common.SourceLocation` **only if** Editions may reference it;
  since Editions is *below* Frontend it cannot — so define a minimal `CobolNet.Editions.SourceSpan`
  (path,line,col) here (or leave `Location` off for now and add it when the diagnostics unify in P4/P7).
  Recommendation: omit `Location` in P2 to avoid a premature shared-location type; the adapter’s
  `List<string>` sink does not need it.
- `IDiagnosticSink` — `void Report(in EditionDiagnostic d);`.
- `EditionSeverityPolicy` — the ONE policy:
  ```csharp
  public static EditionSeverity For(ConstructAvailability verdict, EditionInfo ed) => verdict switch {
      ConstructAvailability.NotYetIntroduced => EditionSeverity.Error,
      ConstructAvailability.Removed          => ed.Permissive ? EditionSeverity.Warning : EditionSeverity.Error,
      ConstructAvailability.Obsolete         => EditionSeverity.Warning,
      _                                      => EditionSeverity.Warning,
  };
  ```
  This is exactly the decision `EditionContext.Removed` (`EditionContext.cs:66-70`) and the obsolete/intro
  arms in `ConstructRegistry.Check` (`ConstructDialectStatus.cs:186-201`) make today.

> `ConstructAvailability` is referenced here but still lives in `Validation/` until step 3. To compile step
> 2 standalone, TEMPORARILY define `ConstructAvailability` in Editions now and have step 3 delete the
> `Validation/` copy — OR fold steps 2+3 into one commit. Folding is cleaner; the doc keeps them separate
> for reviewability.

**Why:** the immutable edition value (fixes P5) + the structured sink and single policy (fixes P2/P4/P6).

**Verify:** `dotnet build src/Cobol.Net.Editions -c Debug`; add a tiny unit fact in
`tests/Cobol.Net.Tests.Unit/` asserting `EditionSeverityPolicy.For(Removed, Of(2023, permissive:false)) ==
Error` and `== Warning` when permissive, and `For(Obsolete, …) == Warning`. Run the Unit suite.

**COMMIT BOUNDARY** — `feat(cobolnet): P2.2 — EditionInfo + EditionSeverity(Policy) + IDiagnosticSink/EditionDiagnostic (DEVLOG N)`

---

### Step 3 — Move `EditionCodes`, `ConstructDialectStatus`/`ConstructRegistry`, `ReservedWords[.Table]` into Editions

**Files:**
- MOVE `src/Cobol.Net.Compiler/Validation/EditionCodes.cs` → `src/Cobol.Net.Editions/EditionCodes.cs`
  (`namespace CobolNet.Editions`).
- MOVE `src/Cobol.Net.Compiler/Validation/ConstructDialectStatus.cs` →
  `src/Cobol.Net.Editions/ConstructDialectStatus.cs` (the record + `ConstructAvailability` + `StatusAt`) and
  `src/Cobol.Net.Editions/ConstructRegistry.cs` (the `Entries` list — still hand-written this step — +
  `Find` + `Check`). Split the one source file into two (verdict record vs registry) as in the target.
- MOVE `src/Cobol.Net.Compiler/Validation/ReservedWords.cs` and `ReservedWords.Table.cs` →
  `src/Cobol.Net.Editions/ReservedWords.cs` and `ReservedWords.Table.cs` (keep `.Table` hand-generated for
  now; rename to `.g.cs` in step 5 when the generator owns it). Move `ReservedWordEntry` with them.

**Change — the ONE behavioral edit in this step: `ConstructRegistry.Check` becomes sink-based.** Replace its
signature `Check(EditionContext, id, where)` with the layer-neutral
`Check(EditionInfo ed, IDiagnosticSink sink, string id, string where)` and translate the body verbatim (the
switch in `ConstructDialectStatus.cs:186-202`) into `sink.Report(new EditionDiagnostic(...))` calls, with
the severity from `EditionSeverityPolicy.For`. Keep the message strings **character-for-character** identical
(they are asserted by `EditionGateDiagnosticTests` / goldens). Add a back-compat overload
`Check(EditionContext, id, where)` that forwards to the new one using the adapter’s `EditionInfo`+sink
(added in step 4) — but since `EditionContext` still lives in Compiler and Editions can’t see it, put that
overload as an **extension method in Compiler** (`ConstructRegistryCompat.Check(this EditionContext, id,
where)`), OR simply update all `ConstructRegistry.Check(_edition, …)` call sites to pass
`_edition.Edition, _edition.Sink` once the adapter exists (step 4). Recommended: keep the extension shim so
this step’s call sites (`EditionValidator.cs` ×25, `PicInfo.cs` ×12, ~8 StatementBinder partials) stay
untouched until step 4 wires the adapter.

- Update `namespace`s to `CobolNet.Editions`; add `using CobolNet.Editions;` to every Compiler file that
  referenced `CobolNet.Validation` for these types (`EditionValidator.cs`, `PicInfo.cs`, the StatementBinder
  partials, `DataBinder*.cs`, `OoClassTable.cs`, `OptionsBinder`, `TurnState`). `grep -rl "CobolNet.Validation"
  src/Cobol.Net.Compiler` to find them; most only need the `using` swapped (the type names are unchanged).
- Move the drift tests’ target namespace: `tests/Cobol.Net.Tests.Unit/ConstructRegistryDriftTests.cs` and
  `ReservedWordsDriftTests.cs` update their `using` to `CobolNet.Editions`.

**Why:** the registry/reserved-words/codes now sit **below** the frontend (fixes the root cause of
P1/P2/P7). Behavior is byte-stable — only the `Check` plumbing changed, body verbatim.

**Verify:**
```bash
dotnet build CobolSharp.sln -c Debug
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug     # ConstructRegistryDriftTests + ReservedWordsDriftTests green
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug
bash scripts/guard-fast.sh   # NIST 353 MATCH — no diagnostic text changed
```

**COMMIT BOUNDARY** — `refactor(cobolnet): P2.3 — move ConstructRegistry/ReservedWords/EditionCodes into Cobol.Net.Editions; Check is now sink-based (body verbatim) (DEVLOG N)`

---

### Step 4 — Split `EditionContext` into `EditionInfo` + sink, behind a byte-stable adapter

**Files:** rewrite `src/Cobol.Net.Compiler/Binding/EditionContext.cs`.

**Change:** keep the class name and the entire public surface, but back it with `EditionInfo` + a
`List<string>`-backed `IDiagnosticSink`:
```csharp
namespace CobolNet.Binding;
using CobolNet.Editions;

public sealed class EditionContext : IDiagnosticSink   // still the same public API the 290 sites use
{
    public EditionInfo Edition { get; }                // the single edition value (fixes P5)
    public IDiagnosticSink Sink => this;               // handed to ConstructRegistry.Check

    private readonly List<string> _diagnostics = [];
    private readonly List<string> _warnings = [];

    public EditionContext(int dialectLevel, bool permissive = false)
        => Edition = EditionInfo.Of(dialectLevel, permissive);

    public int DialectLevel => Edition.Year;
    public bool Permissive => Edition.Permissive;
    public int MaxDigits => Edition.MaxDigits;
    public List<string> Diagnostics => _diagnostics;   // UNCHANGED public shape (adapter keeps 290 sites green)
    public List<string> Warnings => _warnings;
    public bool HasErrors => _diagnostics.Count > 0;

    public void Error(string code, string message)   => _diagnostics.Add($"error {code}: {message}");
    public void Warning(string code, string message) => _warnings.Add($"warning {code}: {message}");
    public void Removed(string code, string message) { if (Permissive) Warning(code, message); else Error(code, message); }
    public void CheckDigitCapacity(int digits, string what) { /* verbatim from the current file:74-83 */ }

    // The IDiagnosticSink bridge: structured -> the legacy string channels (identical text).
    public void Report(in EditionDiagnostic d)
    {
        if (d.Severity == EditionSeverity.Error) Error(d.Code, d.Message);
        else Warning(d.Code, d.Message);
    }
}
```
Then update the `ConstructRegistry.Check` call sites (or the compat extension from step 3) to pass
`edition.Edition, edition.Sink`. The frontend-visible `Check` now takes `(EditionInfo, IDiagnosticSink, …)`;
the compiler passes the adapter as the sink so the emitted text is identical.

> The `Report` bridge reconstructs the exact `"error {code}: {message}"` / `"warning …"` strings the current
> `Error/Warning` produce, so `EditionContext.Diagnostics`/`Warnings` are byte-identical to today. This is
> the crux of the neutrality guarantee — verify it with a golden diff (step 4 verify).

**Why:** ends the "edition = diagnostic sink" mislabel and single-sources `DialectLevel` (P5/P6), while the
adapter keeps all ~290 call sites compiling and byte-stable (risk R2).

**Verify:** full battery (as step 3). Additionally, pick one program that trips a removed-construct gate and
confirm identical stderr before/after:
```bash
# ALTER at 2002 -> COBOLNET0810 error (strict); the message must be byte-identical to pre-refactor.
printf 'IDENTIFICATION DIVISION.\nPROGRAM-ID. A.\nPROCEDURE DIVISION.\nP1. GO TO P2.\nP2. ALTER P1 TO PROCEED TO P2. STOP RUN.\n' > /e/tmp/alter.cob
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /e/tmp/alter.cob --std 2002 -o /e/tmp/a.dll 2>&1 | grep COBOLNET0810
```

**COMMIT BOUNDARY** — `refactor(cobolnet): P2.4 — EditionContext = EditionInfo + IDiagnosticSink adapter (290 sites byte-stable; single DialectLevel source) (DEVLOG N)`

---

### Step 5 — Generate `ConstructRegistry.Entries` + `Constructs.*` + `GateId` from `constructs.json`

**Decision (Open Q1):** default = **MSBuild pre-build PowerShell script** mirroring the trusted
`scripts/gen-reserved-words.ps1` / ANTLR-regen pattern (portable on both OSes, failed regen fails the build,
"generated is a build output" discipline). The Roslyn incremental source generator
(`src/Cobol.Net.Editions.SourceGen/`) is the cleaner alternative — pick it only if the owner confirms Q1.
This step is written for the **script** path; if you choose the generator, the outputs are identical, only
the build hook differs.

**Files:**
- Enrich `tests/version-matrix/constructs.json`: add to every row the fields the current C# `Entries` carries
  but the JSON does not — `display` (the human name in diagnostics), `diagnosticCode` (the pinned code or
  `"COBOLNET0900"`/`0901`/`0902`/`0903`), and `citation` (the ISO §/VCR string). Source these **verbatim**
  from `ConstructDialectStatus.cs:60-168` so nothing changes. Also add `obsoleteIn` where a row has it (the
  MOVE/QUOTE rows, EXIT PROGRAM, NEXT SENTENCE). The JSON already has `introducedIn`/`removedIn`.
- Create `scripts/gen-constructs.ps1`: reads `tests/version-matrix/constructs.json`, emits
  `src/Cobol.Net.Editions/ConstructRegistry.g.cs` (the `Entries` list, one `new(...)` per row, in file
  order), `src/Cobol.Net.Editions/Constructs.g.cs` (`public const string <PascalId> = "<id>";` per row), and
  `src/Cobol.Net.Editions/Gating/GateId.cs` (an `enum GateId` with one member per row whose `removedIn` is
  null and `introducedIn > 85` — i.e. the introduction-gated set the parser stamps). Rename the reserved-word
  table output `ReservedWords.Table.cs` → `ReservedWords.Table.g.cs` and have `gen-reserved-words.ps1` (or a
  merged `gen-editions.ps1`) own it.
- Wire an MSBuild `BeforeTargets="CoreCompile"` target in `Cobol.Net.Editions.csproj` that runs
  `pwsh gen-constructs.ps1` with `Inputs="tests/version-matrix/constructs.json"
  Outputs="ConstructRegistry.g.cs;Constructs.g.cs;Gating/GateId.cs"`, exactly like the frontend’s
  `EnsureGeneratedFiles` target (`Cobol.Net.Frontend.csproj:42-64`). `.gitignore` the three `*.g.cs`
  (build output) OR commit them and add a drift test — follow the repo’s existing choice for
  `ReservedWords.Table`. Delete the hand-written `Entries` array from `ConstructRegistry.cs` (leave only the
  `Check`/`Find`/`Require` funnel as the hand-written partial).
- Repurpose `ConstructRegistryDriftTests`: it no longer needs to assert json↔registry (same source now);
  instead assert (a) generator output parses and every `Constructs.*` const is referenced somewhere, and (b)
  every `constructs.json` row round-trips into an `Entries` row with equal fields.

**Why:** `constructs.json` is the single source; ids become compile-checked consts; the hand-written 110-row
array is deleted (fixes P7). `GateId` is the strongly-typed identity the parser will stamp (step 7).

**Verify:**
```bash
pwsh scripts/gen-constructs.ps1        # regenerates cleanly; nonzero exit fails the build
dotnet build CobolSharp.sln -c Debug   # generated Entries compile; no missing rows
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug   # ConstructRegistryDriftTests green
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug  # VersionMatrixTests green
```

**COMMIT BOUNDARY** — `feat(cobolnet): P2.5 — generate ConstructRegistry.Entries + Constructs ids + GateId from constructs.json (single source) (DEVLOG N)`

---

### Step 6 — Introduce `Constructs.*` id-consts at all call sites; fold the five inline gates into the registry

**Files:** `tests/version-matrix/constructs.json`; `EditionValidator.cs`; the five inline-gate sites:
`StatementBinder.Accept.cs:44`, `StatementBinder.cs:1228`, `StatementBinder.AlterSwitches.cs:118,136`,
`StatementBinder.Call.cs:175`.

**Change:**
1. Mechanical find/replace: turn magic-string ids into `Constructs.<PascalId>` (e.g.
   `ConstructRegistry.Check(_edition, Constructs.LabelRecordsRemoved2002, "…")`). This is pure ergonomics but
   makes an unregistered id a **compile** error.
2. Add five `constructs.json` rows with pinned `diagnosticCode` (so the generator carries them):
   - `end-accept-2002` — introducedIn 2002, code `COBOLNET0816`, cite ISO §14.9.1.
   - `rounded-mode-is-2014` — **already a row** (`ConstructDialectStatus.cs:74`) but currently gated inline
     with `COBOLNET0803`; reconcile: set that row’s `diagnosticCode` to `COBOLNET0803` and route the inline
     site through it. (The existing row uses `EditionCodes.Introduction`; changing to the pinned `0803` keeps
     the *emitted* code because the inline site already emits `0803` — verify the golden.)
   - `alter-removed-2002` — 85→removed 2002, code `COBOLNET0810`.
   - `bare-goto-removed-2002` — 85→removed 2002, code `COBOLNET0811`.
   - `call-on-overflow-removed-2023` — 85→removed 2023, code `COBOLNET0882`.
   Each row needs a positive fixture (a program using it) and the `expectDiagnostic` (below-introduction /
   at-removal) so it enters `VersionMatrixTests`. For ALTER/bare-GO-TO the negative case is `--std 85` (legal)
   and the positive is `--std 2002` (removed). For CALL ON OVERFLOW: legal ≤2014, removed 2023.
3. Replace each inline `data.Edition.Error(...)` / `data.Edition.Removed(...)` with
   `ConstructRegistry.Check(data.Edition.Edition, data.Edition.Sink, Constructs.X, where)`. **Keep the pinned
   code**: because the row’s `diagnosticCode` is the pinned `08xx`/`0882`, `Check` emits the identical code.
   The message text WILL change (it now uses `Check`’s uniform template `"{Display} was removed in COBOL-{r}
   …"`). This is the one place message text changes — update the affected goldens/assertions
   (`CallExceptionPhraseEditionTests`, any END-ACCEPT/ROUNDED-MODE/ALTER assertions) in the SAME commit, and
   confirm the CODE is unchanged. If preserving exact legacy wording matters for a golden, keep the row’s
   `display`/`citation` chosen so the rendered string matches; otherwise re-baseline the assertion with the
   new uniform text (a reviewed diagnostic-golden change — gate 2).

**Why:** kills the parallel gating mechanism (P4); the five gates now enter the version matrix and are
drift-bound.

**Verify:**
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug   # VersionMatrixTests + CallExceptionPhraseEditionTests green (re-baselined)
bash scripts/guard-fast.sh
# spot-check the pinned codes are unchanged:
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /e/tmp/alter.cob --std 2002 -o /e/tmp/a.dll 2>&1 | grep COBOLNET0810
```

**COMMIT BOUNDARY** — `refactor(cobolnet): P2.6 — Constructs.* id-consts everywhere; fold the 5 inline gates (0816/0803/0810/0811/0882) into registry rows (pinned codes; now in the version matrix) (DEVLOG N)`

---

### Step 7 — Predicate stamping on `CobolParserCoreBase`; wire `CobolErrorStrategy`; run in parallel with `EditionGateHints`

**Files:** `src/Cobol.Net.Frontend/Parsing/CobolParserCoreBase.cs`,
`src/Cobol.Net.Frontend/Parsing/CobolErrorStrategy.cs`, the grammar fragments under
`src/Cobol.Net.Frontend/Grammar/Core/*.g4` and `Grammar/CobolParserCore.g4`.

**Change (do this incrementally — one `.g4` fragment at a time, FULL legacy guard each):**
1. Add to `CobolParserCoreBase`:
   ```csharp
   public EditionInfo Edition { get; set; } = EditionInfo.Latest;      // single DialectLevel source
   public int DialectLevel { get => Edition.Year; set => Edition = EditionInfo.Of(value, Edition.Permissive); } // P1-compat shim
   internal GateId? LastRejectedGate { get; private set; }
   internal int LastRejectedTokenIndex { get; private set; } = -1;
   protected bool is85()   => Edition.Has(85);
   protected bool is2002() => Edition.Has(2002);
   protected bool is2014() => Edition.Has(2014);
   protected bool is2023() => Edition.Has(2023);
   protected bool Gate(int introducedIn, GateId id)
   {
       if (Edition.Year >= introducedIn) return true;
       int idx = CurrentToken?.TokenIndex ?? -1;
       if (idx >= LastRejectedTokenIndex) { LastRejectedGate = id; LastRejectedTokenIndex = idx; }
       return false;
   }
   ```
   Reset `LastRejectedGate`/`LastRejectedTokenIndex` at the start of each parse (both the SLL and LL passes;
   `Frontend.cs` `Reset()`s the parser between passes — hook the reset there or in a parser-entry override).
   If Editions is dependency-free (Q3) and thus `GateId` lives in Editions but the parser cannot map it to a
   construct id, keep the `GateId → constructs.json id` lookup in the frontend (a small generated dictionary)
   and have `Gate` take the enum only.
2. Migrate the introduction predicates fragment-by-fragment: `{is2002()}?` → `{Gate(2002, GateId.Invoke)}?`
   etc., one `.g4` at a time. After EACH fragment: regenerate the parser (`GenerateIfNewer.ps1` runs on
   build), build, run `guard-fast.sh` (FULL legacy guard — a shared `.g4` change requires it).
3. Wire `CobolErrorStrategy.GuessCobolIntent` (`CobolErrorStrategy.cs:113`): when the parser is a
   `CobolParserCoreBase core` with `core.LastRejectedGate is { } g` at/after the offending token, and the
   construct’s introduction edition (from the frontend `GateId`→row map / `ConstructRegistry.Require`) is
   above `core.Edition.Year`, build the `COBOLNET0900` message via `ConstructRegistry.Check` into a local sink
   and use its text — replacing the `EditionGateHints.Recognize` call at `CobolErrorStrategy.cs:113`.
   **Keep the `EditionGateHints.Recognize` call ALSO for one commit**, comparing the two message paths in the
   test corpus (see verify) — do not delete the table yet.

**Why:** forward gate identity replaces the reverse-engineering table (fixes P1/P3); one diagnostic origin.

**Verify (the go/no-go for deletion):**
```bash
# EditionGateDiagnosticTests is the corpus that pins the parse-layer 0900 messages per construct per edition.
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter EditionGateDiagnostic -c Debug
bash scripts/guard-fast.sh    # every shared-.g4 change: FULL legacy guard MATCH
# Manual spot-checks (below-introduction => COBOLNET0900 naming the construct):
printf 'IDENTIFICATION DIVISION.\nPROGRAM-ID. I.\nPROCEDURE DIVISION.\nP. INVOKE SELF "m".\n' > /e/tmp/inv.cob
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll /e/tmp/inv.cob --std 85 -o /e/tmp/i.dll 2>&1 | grep COBOLNET0900
```
Expected: identical `EditionGateDiagnosticTests` results from the new path as the old (the stamped construct
matches the signature-derived one for every corpus case).

**COMMIT BOUNDARY** — `feat(cobolnet): P2.7 — forward {Gate(edition, GateId)}? predicate stamping; CobolErrorStrategy reads LastRejectedGate (parallel with EditionGateHints for one commit) (DEVLOG N)`

---

### Step 8 — Delete `EditionGateHints`

**Files:** delete `src/Cobol.Net.Frontend/Parsing/EditionGateHints.cs`; remove the residual
`EditionGateHints.Recognize` call and the vendor JSON/XML `COBOL0313` branch from
`CobolErrorStrategy.cs:113-117` (the JSON/XML dispatch was removed by P1 per DESIGN-frontend-grammar; if any
`COBOL0313` mapping must survive for vendor statements, re-home it as a tiny explicit token check in the
error strategy, NOT a resurrected signature table).

**Why:** eliminate the reverse-engineering table and the duplicate edition-diagnostic origin (exit
criterion 3).

**Verify:** `dotnet build` (no dangling references); `EditionGateDiagnosticTests` green; full battery +
`guard-fast.sh` MATCH.

> **Rollback note:** if step-7’s parallel run shows ANY corpus divergence, do NOT proceed to step 8. Keep a
> *thin* registry-driven recognizer as a fallback (risk R1 mitigation): drive its metadata from
> `ConstructRegistry` (delete the duplicated `(Display, IntroducedIn, Citation, RowId)` fields, keep only the
> token-signature → `GateId` map). This still removes the duplication while retaining the heuristic.

**COMMIT BOUNDARY** — `refactor(cobolnet): P2.8 — delete EditionGateHints; the parse-layer COBOLNET0900 has ONE origin (ConstructRegistry.Check via predicate stamping) (DEVLOG N)`

---

### Step 9 — Route the frontend preprocessor gates through `EditionSeverityPolicy`

**Files:** `src/Cobol.Net.Frontend/Preprocessor/ReferenceFormatProcessor.cs` (inner `EditionGates`,
`:120-148`), `src/Cobol.Net.Frontend/Preprocessor/CopyProcessor.cs` (the COPY REPLACING non-pseudo-text gate).

**Change:** the inner gates today inline `if (permissive) ReportWarning else ReportError` with a hard-coded
code (`ReferenceFormatProcessor.cs:144-147`). Replace the severity decision with
`EditionSeverityPolicy.For(ConstructAvailability.Removed, Edition)` (and `.Obsolete` for the col-7 obsolete
gate) and emit via the shared `DiagnosticBag` mapping `EditionSeverity.Error/Warning` → `ReportError/ReportWarning`.
Keep the emitted code (`COBOLNET0902`/`0903`) and message text identical. The metadata stays
registry-canonical (the rows `fixed-form-word-continuation-removed-2023`, `col7-continuation-obsolete-2023`,
`copy-replacing-non-pseudo-text-removed-2023` already exist in `constructs.json` /
`ConstructDialectStatus.cs:149-151`). If you can pass the shared registry down to the preprocessor, prefer
`ConstructRegistry.Check` directly over a per-site `EditionSeverityPolicy.For`; the emit site stays in the
frontend (only the column-aware pass sees the col-7 indicator).

**Why:** delete the two remaining copies of the strict/permissive policy (P2) — one policy for binder +
validator + both preprocessor gates.

**Verify:** the reference-format / COPY edition tests (grep for the tests exercising `COBOLNET0902`/`0903`
under `--std 2023`); full battery + `guard-fast.sh`.

**COMMIT BOUNDARY** — `refactor(cobolnet): P2.9 — preprocessor edition gates consume the ONE EditionSeverityPolicy (no local if(permissive)) (DEVLOG N)`

---

### Step 10 — First-class diagnostic-descriptor registry + split the `COBOLNET0899` catch-all + disambiguate reused codes

**Files:** create `src/Cobol.Net.Editions/Diagnostics/DiagnosticDescriptor.cs`,
`Diagnostics/DiagnosticDescriptors.cs`, `Diagnostics/DiagnosticSeverity.cs`; edit the ~44 `COBOLNET0899`
emit sites (`grep -rn "COBOLNET0899" src/Cobol.Net.Compiler` → 13 files) and the reused-code sites (e.g.
`1533`); create `scripts/gen-diagnostics-doc.ps1` and `docs/DIAGNOSTICS.md`; add
`tests/Cobol.Net.Tests.Unit/DiagnosticRegistryDriftTests.cs`.

**Change:**
- `DiagnosticDescriptor` record:
  `record DiagnosticDescriptor(string Code, string Id, DiagnosticSeverity Severity, string IsoSection,
  string Template, int? IntroducedEdition = null, string? SuppressKey = null)`. `Id` is a stable kebab-case
  slug (survives code renumbering); `SuppressKey` defaults to `Code` (Open decision from
  DESIGN-test-build-ci: per-code with optional family key).
- `DiagnosticDescriptors` — a hand-written `static partial class` catalogue (the compiler-side codes:
  the edition band `COBOLNET0900-0903` + the pinned `08xx`/`0882` + the *split* of `COBOLNET0899` into
  one-code-one-rule descriptors + the disambiguated reuses). For the edition band, reference the codes in
  `EditionCodes.cs` so there is ONE definition of the `0900-0903` strings. Reconcile with the frontend’s
  existing `DiagnosticDescriptors` (`src/Cobol.Net.Frontend/Diagnostics/DiagnosticDescriptors.cs`): the
  frontend already has a large descriptor catalogue (CBL*, COBOL*, `COBOLNET0900`, `COBOL0313`). The
  end-to-end unification (moving the frontend’s `DiagnosticDescriptor`/`DiagnosticBag` down into Editions as
  the ONE model — DESIGN-frontend-grammar) is a LARGER move; in P2, **only** unify the edition band
  (`COBOLNET0900-0903`, `0313`) so it has one home in Editions and the frontend consumes it, and leave the
  frontend’s parse-layer CBL*/COBOL* descriptors in place. Note the full merge as a P4/P7 follow-on in the
  design doc.
- **Split `COBOLNET0899`:** it is a "recognized-not-implemented" catch-all emitted from ~44 sites (national
  edited, external float, DEBUG registers, OO property stubs, sort/report residue, …). Give each distinct
  deferred feature its own descriptor with a stable `Id` and a `SuppressKey` in a shared "unimplemented"
  family, keyed off a tracked list. Keep the `0899` **code** where a golden pins it, but make each site carry
  a distinct descriptor so snapshots/`--suppress`/the version matrix can address them. (Do NOT change the
  emitted text where a golden asserts it — re-baseline only with review.)
- **Disambiguate reused codes** (e.g. `1533` appears for more than one rule per the dossier): assign each
  rule its own descriptor; if the emitted code must stay for goldens, differentiate by `Id`.
- `scripts/gen-diagnostics-doc.ps1` renders `docs/DIAGNOSTICS.md` (a table of Code | Id | Severity | ISO § |
  Template | Edition | SuppressKey) from the descriptor catalogue. `DiagnosticRegistryDriftTests` asserts
  (a) every descriptor has a unique `Id`, (b) `docs/DIAGNOSTICS.md` matches the catalogue (regenerate-and-diff),
  (c) no bare `COBOLNET####` string literal is emitted where a descriptor exists (a grep-style test over
  `src/Cobol.Net.Compiler` for the split codes), (d) the split 0899 sites each reference a distinct descriptor.

**Why:** replaces stringly-typed codes with a structured, documented, suppressible registry; makes the
44-site `0899` catch-all and reused codes addressable (P8); satisfies exit criteria 4 and 7.

> **Scope guard:** do NOT attempt to convert all 163 codes to descriptors in P2 if it destabilizes goldens.
> The REQUIRED deliverable is: the edition band unified in Editions, the `0899` split, the reused-code
> disambiguation, `docs/DIAGNOSTICS.md`, and the drift test. A broader "every code → descriptor + `sink.Report`"
> migration (DESIGN-test-build-ci’s 161-bare-code cleanup) may be staged across sub-commits here or deferred
> to P7; land what is stable and record the remainder as tracked follow-on.

**Verify:**
```bash
pwsh scripts/gen-diagnostics-doc.ps1                       # regenerates docs/DIAGNOSTICS.md
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug     # DiagnosticRegistryDriftTests green
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug
bash scripts/guard-fast.sh
```

**COMMIT BOUNDARY** — `feat(cobolnet): P2.10 — first-class DiagnosticDescriptors registry; split the COBOLNET0899 catch-all + disambiguate reused codes; generate docs/DIAGNOSTICS.md; drift-tested (DEVLOG N)`

---

### Step 11 — Docs sync + design-banner update

**Files:** `docs/DOC_INDEX.md` (add rows for `docs/DIAGNOSTICS.md` and note `Cobol.Net.Editions`);
`docs/rearchitecture/DESIGN-edition-framework.md` (flip the status banner: record what P2 landed, and mark
Q1/Q2/Q3/Q5/Q6 as resolved-by-implementation where applicable — e.g. Q5: the compiler-side collector stayed
`EditionContext`-named behind the adapter; Q6: the general registry’s edition band landed here, the broader
cleanup is P7); `resume-prompt.md` STATE banner (P2 done, next = P3); `CLAUDE.md` if the doc map changed.

**Why:** `feedback_follow_design_docs_and_spec` / `feedback_plan_updates` — keep the deep-dives current in
the same change set; a future session resumes from accurate docs.

**Verify:** `docs/DOC_INDEX.md` lints (if a doc-index drift test exists, run it); prose review.

**COMMIT BOUNDARY** — `docs(cobolnet): P2.11 — sync DOC_INDEX + DESIGN-edition-framework banner + resume-prompt for P2 completion (DEVLOG N)`

---

### Step 12 (OPTIONAL — may defer to P7) — retire the `EditionContext` adapter

Only if time permits and the battery stays green. Migrate the ~290 sites from `EditionContext.Error/Warning/
Removed` to `EditionInfo` + `IDiagnosticSink` in independently-testable slices (per binder partial / per
emitter partial), deleting the adapter last and renaming the compiler-side collector to its final name (Q5:
`DiagnosticSink`/`CompileDiagnostics`). This is the largest mechanical churn and is **not** an exit
criterion for P2 — the adapter satisfies criterion 5 on its own.

---

## 5. Verification (run at phase end)

```bash
# 1. Build the whole solution clean (regenerates the ANTLR parser + the constructs.json-derived registry).
dotnet build CobolSharp.sln -c Debug

# 2. Greenfield conformance (2028+ expected) + unit (213+ expected).
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -c Debug
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -c Debug

# 3. FULL legacy guard — NIST 353 MATCH (the byte-exact differential net). Windows: run under WSL.
bash scripts/guard-fast.sh

# 4. Edition-specific suites explicitly:
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --filter "EditionGateDiagnostic|VersionMatrix|ReservedWordPosition" -c Debug
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter "ConstructRegistryDrift|ReservedWordsDrift|DiagnosticRegistryDrift" -c Debug
```

**Behavior-neutrality checks (the byte-exact gate):**
- The `EditionContext.Diagnostics`/`Warnings` string shape (`"error {code}: {msg}"` / `"warning …"`) is
  **byte-identical** before/after (the adapter’s `Report` bridge). Diff a couple of gate programs’ stderr
  (INVOKE@85 → `COBOLNET0900`; ALTER@2002 → `COBOLNET0810`; CALL ON OVERFLOW@2023 → `COBOLNET0882`;
  col-7 continuation@2023 → `COBOLNET0902`).
- `EditionGateDiagnosticTests` results are unchanged by the predicate-stamping switch (step 7’s parallel run
  is the proof; step 8 must keep it green).
- The five folded inline gates emit the **same code** (message text may re-baseline with review — a gate-2
  diagnostic-golden change, allowed when the code is stable and reviewed).

**Exit-criteria checklist:** re-verify all 7 items in §"Exit criteria". Update **STATUS → DONE** only when
every box is checked and the battery is green.

---

## 6. Rollback / resumability

- **Resume point:** the STATUS line records `IN PROGRESS @ step N`. Each step is a COMMIT BOUNDARY, so
  `git log --oneline` shows the last completed step; resume at N+1. Steps 1–5 are pure relocation/generation
  and cannot regress runtime behavior (the adapter + verbatim `Check` body guarantee it).
- **Highest-risk steps are last and gated:** step 7 (predicate stamping) runs the new path **in parallel**
  with `EditionGateHints` for one commit; step 8 (deletion) proceeds ONLY if `EditionGateDiagnosticTests` is
  identical. If it diverges, stop at step 7’s fallback (registry-driven thin recognizer) — the duplication is
  still removed. This is risk **R1** (ANTLR speculative predicate evaluation): the `>= LastRejectedTokenIndex`
  furthest-token-wins + reset-per-parse makes the furthest rejection win, matching the premise
  `EditionGateHints` already relies on.
- **R2 (290-site churn):** the adapter (step 4) makes the split zero-behavior-risk; the 290 sites never
  change in P2. Retiring the adapter (step 12) is optional and per-slice reversible.
- **R3 (generator toolchain):** the MSBuild-script path (Q1 default) mirrors the trusted
  `gen-reserved-words.ps1` — a failed regen fails the build (no stale fallback). java+pwsh are already build
  prerequisites (ANTLR). If the source generator is chosen instead, ensure it degrades to the script on CI.
- **R5 (`default(EditionInfo)` = `Year 0`):** `EditionInfo.Of`/`Validate` throw on an invalid year; the
  parser/driver default to `EditionInfo.Latest` **explicitly**, never `default`.
- **If the battery goes red at any boundary:** `git revert` the last commit (each is self-contained) and
  re-attempt the step in smaller slices (e.g. step 7 per `.g4` fragment; step 10 per 0899-site family).

---

## 7. ISO feature work in this phase

This is a **rearchitecture** phase — no NEW ISO semantics are implemented; the version-gating *behavior*
audit and validator waves are explicitly P3. However, folding the five inline gates into the registry (step
6) requires each to enter the version matrix with fixtures, which touches these spec sections and editions:

| Gate | ISO § | Edition change | Fixtures to add to `constructs.json` |
|------|-------|----------------|--------------------------------------|
| END-ACCEPT (`COBOLNET0816`) | §14.9.1 (ACCEPT general formats) | introduced 2002 | positive: `--std 85` rejects; negative: `--std 2002` accepts |
| ROUNDED MODE IS (`COBOLNET0803`) | §14.7.4 | introduced 2014 | `--std 2002` rejects; `--std 2014` accepts |
| ALTER (`COBOLNET0810`) | §14.9.17 (no altered GO TO) | removed 2002 (obsolete '85) | `--std 85` accepts; `--std 2002` rejects |
| bare GO TO (`COBOLNET0811`) | §14.9.17 (procedure-name-1 required) | removed 2002 | `--std 85` accepts; `--std 2002` rejects |
| CALL … ON OVERFLOW (`COBOLNET0882`) | §14.9.4 / Annex E.2 item 1c | removed 2023 | `--std 2014` accepts; `--std 2023` rejects |

Each row carries `expectDiagnostic` for the failing edition; `VersionMatrixTests` computes accept/reject
from `introducedIn`/`removedIn` and asserts the pinned `diagnosticCode` surfaces. No golden output files are
added beyond the matrix fixtures; the existing `EditionGateDiagnosticTests` / `CallExceptionPhraseEditionTests`
assert the diagnostic text (re-baselined with review where step 6 changes wording, code unchanged).

Conformance tests to keep green (they encode the four-editions-in-one invariants):
`VersionMatrixTests`, `EditionGateDiagnosticTests`, `ReservedWordPositionConformanceTests`,
`ConstructRegistryDriftTests`, `ReservedWordsDriftTests`, and the new `DiagnosticRegistryDriftTests`.
