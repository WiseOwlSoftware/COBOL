# PHASE 16 — The Second Codegen Backend (direct CIL via Mono.Cecil) + the Executable Seam-Proof

- **Phase:** P16
- **Track:** rearchitecture (dual-backend capability — the elevated `project_dual_backend_goal`)
- **Risk:** MEDIUM (Milestone 0, the seam-proof) → HIGH (the full Cecil backend build-out). **Additive throughout —
  Roslyn stays the default backend; the battery is never gated on CIL.**
- **Depends on:**
  - **P6** (`BindPipeline` → immutable `BoundCompilation`; `SymbolTable`; the L3/L4 node fields de-C#'d per
    `DESIGN-backend-abstraction.md §5`) — the seam must carry a **neutral** tree.
  - **P7** (materialized `ICodeGenBackend` Step 1; structural `Place`/`AccessSegment` Step 11; the exhaustive
    `IBound*Visitor<T>` interfaces Step 6; `RuntimeAbi`+`NameMangler`; **Step 13 the backend-contract test +
    seam-proof** — see the split note below).
  - Design: **`DESIGN-backend-abstraction.md`** (the contract, the interface, the Cecil recommendation, the
    `RuntimeAbi`/`CilRuntimeApi` split), `DESIGN-codegen-backend.md` (§2.2/§2.3/§3), `docs/COBOLNET_DESIGN.md`
    §1.1 (no shared lowered IR; `--backend roslyn|cil`).
- **Goal (one paragraph):** Deliver the dual-backend capability the owner directive requires: a real second
  `ICodeGenBackend` implementation that consumes the SAME immutable `BoundCompilation` and emits a runnable assembly
  **without touching the frontend, binder, or bound tree**. Begin with an **early, cheap seam-proof** (a `NullBackend`
  + a tiny in-box `System.Reflection.Emit` `DisplayBackend`, DISPLAY-only) that proves `ICodeGenBackend` is real and
  the IR carries no C# — runnable the moment `Place` goes structural, so neutrality can never silently rot. Then build
  the production `CilBackend` (Mono.Cecil, in the isolated `Cobol.Net.Backend.Cil` assembly) feature-by-feature —
  numerics, moves, control flow / the PERFORM dispatcher, files, OO, EC — each sub-step verified by running the SAME
  golden corpus through `--backend cil` and byte-comparing stdout to the Roslyn backend (a backend-equivalence
  harness). Wire `--backend {roslyn|cil}` in the CLI and run CI on BOTH backends over a defined, growing subset.
- **Exit criteria:** `--backend cil` produces a runnable `.dll` whose stdout/stderr/exit-code is **byte-identical to
  the Roslyn backend** across a defined corpus subset (grown to full at phase end); the backend-contract neutrality
  test (`DESIGN-backend-abstraction.md §6`) is green (a non-C# backend consumes the tree by construction); CI runs the
  conformance corpus on both backends; Roslyn remains the default and every pre-existing battery item stays green.
- **STATUS:** `NOT STARTED`
  > The executing session updates this line to `IN PROGRESS @ milestone N / step M` after each step and `DONE` at
  > phase end. Keep the last green commit hash here so an interrupted session can resume precisely.

> **Where Milestone 0 slots in the 0..15 sequence (recommendation).** The seam-proof must run **right after
> PHASE-07** so the neutral tree is proven by a second consumer before phases 08–15 evolve it. **Recommendation: land
> Milestone 0 as PHASE-07 Step 13** (`DESIGN-backend-abstraction.md §5` adds exactly that one-line addition to P7),
> not as a late P16 milestone — pulling it forward is the whole anti-rot mechanism. This PHASE-16 file **owns
> Milestone 0's full spec** and restates it as its entry milestone for two reasons: (1) if the owner prefers to keep
> P7 lean, Milestone 0 can execute here as a standalone slice immediately after P7 with the same content; (2) the
> production CilBackend (Milestones 1+) depends on Milestone 0's `BackendFactory` plumbing and equivalence-harness
> skeleton regardless. Its only dependencies are P7 Step 1 (seam) + Step 11 (structural `Place`) — **not** the full
> CIL backend.

---

## 1. Preconditions & how to resume

Before starting, confirm P6/P7 landed the neutral seam (grep to verify — all must exist):

```bash
cd E:/CobolSharp
grep -rln "interface ICodeGenBackend"     src/Cobol.Net.Compiler/CodeGen   # P7 Step 1 seam
grep -rln "record BoundCompilation"        src/Cobol.Net.Compiler/Binding   # P6 immutable result
grep -rln "record AccessPath"              src/Cobol.Net.Compiler/Binding   # P7 Step 11 structural Place
grep -rln "class RuntimeAbi"               src/Cobol.Net.Compiler           # neutral runtime catalogue (DESIGN-backend-abstraction §2.3)
grep -rln "class NameMangler"              src/Cobol.Net.Compiler/Model     # shared naming service (§2.4)
# The bound tree must be neutral: these must return NOTHING (L3/L4 de-C#'d in P6):
grep -rn  "string TablePath\|string IndexField\|string CsName\|string SendingPath" \
          src/Cobol.Net.Compiler/Binding/Bound src/Cobol.Net.Compiler/Binding/Place.cs
grep -rn  "Read()\|Write(" src/Cobol.Net.Compiler/Binding/Place.cs         # must be gone (P7 Step 11)
```

If the seam or the structural `Place` is missing, STOP and finish P6/P7 first. If the last two greps return hits, the
**bound tree is not yet neutral** — the CIL backend cannot consume it; finish P6's L3/L4 de-C#-ing
(`DESIGN-backend-abstraction.md §1.3`) first.

**The battery (run at every commit boundary; must stay green):**

```bash
# 1. Greenfield conformance (~2003 cases). Run with BOTH backends from Milestone 3 on (§ CLI matrix).
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet
# 2. Greenfield unit (~213).
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet
# 3. Backend-contract neutrality test (DESIGN-backend-abstraction §6) — MUST be green from Milestone 0.
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --filter Category=BackendContract -v quiet
# 4. Backend-equivalence harness (NEW this phase) — Roslyn vs Cil stdout byte-compare over the enabled subset.
dotnet test tests/Cobol.Net.Tests.BackendEquivalence/Cobol.Net.Tests.BackendEquivalence.csproj -v quiet
# 5. FULL legacy differential guard — only if a SHARED .g4 is touched (this phase touches NONE) + once at phase end.
bash scripts/guard-fast.sh
```

**Behavioral probe (prebuilt CLI, both backends):**

```bash
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <source.cob> --std 2002 -o /tmp/r.dll --backend roslyn --run
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <source.cob> --std 2002 -o /tmp/c.dll --backend cil    --run
diff <(dotnet /tmp/r.dll) <(dotnet /tmp/c.dll) && echo "BYTE-IDENTICAL"
```

**Resuming mid-phase:** every step is an independent COMMIT BOUNDARY leaving the battery green. To resume, read the
STATUS line, `git log --oneline | grep "P16"` for the last landed sub-commit, continue at the next. No step leaves the
tree non-compiling at its boundary. Milestones 3–6 are multi-sub-commit; each carries its own resumability note (§6).

---

## 2. Rationale — the problem this phase fixes

The rearchitecture plan names the dual-backend seam (`DESIGN-codegen-backend.md §2.2`) and makes `Place` structural
(P7 Step 11), but **no phase actually builds or even seam-proves a second backend**. Consequences the plan leaves
open:

1. **The seam is untested by a real consumer.** `ICodeGenBackend` with one implementation is an interface, not a
   proven abstraction — nothing forces the tree to be genuinely neutral. A single C# string can creep back into a
   node (as `BoundSetCapacity.TablePath` `BoundTree.cs:493` and `BoundSearch` `BoundTree.cs:508-511` show it already
   did) and nothing fails until someone tries to write the second backend, years later.
2. **The owner-emphasized goal has no delivery vehicle.** `project_dual_backend_goal` is a durable directive ("a
   direct CIL/IL backend must be droppable in later WITHOUT touching the frontend, binder, or bound tree"). The plan
   must contain the phase that delivers it and the milestone that proves it early.
3. **Neutrality rots silently without a live second consumer.** `DESIGN-codegen-backend.md §6 R5` foresaw this and
   proposed a reflection test; this phase makes the guarantee **executable** — an actual non-C# backend consumes the
   tree (Milestone 0), so residual C# is caught by construction, not just by a reflection heuristic.

This phase closes all three: an early seam-proof (dependency-free), the production Cecil backend, an equivalence
harness, and CI on both backends — additive, Roslyn-default, battery-green throughout.

---

## 3. Target end-state for this phase (concrete)

When P16 is DONE, these exist with these responsibilities (grounded in `DESIGN-backend-abstraction.md §2–§4`):

**Seam-proof (Milestone 0 — in-box, dependency-free)**
- `CodeGen/NullBackend.cs` — `NullBackend : ICodeGenBackend` — consumes `BoundCompilation`, produces a
  `BackendArtifact(Success:true, …, AssemblyPath:null)` doing nothing; proves the driver→seam plumbing.
- `CodeGen/DisplayBackend.cs` — `DisplayBackend : ICodeGenBackend` over **`System.Reflection.Emit`
  `PersistedAssemblyBuilder`** (.NET 10 in-box) — lowers ONLY `BoundProgram`s whose statements are `BoundDisplay` of
  literal/field operands + `BoundStop`, emitting `Console.WriteLine` IL; every other node → a loud `NotSupported`.
  The dependency-free proof that a non-C# backend consumes the neutral tree.
- The backend-contract test's **assertion 3** (`DESIGN-backend-abstraction.md §6`) round-trips a DISPLAY-only
  `BoundCompilation` through Roslyn AND `DisplayBackend`, asserting byte-identical stdout.

**Production CIL backend (Milestones 1–6 — Mono.Cecil, isolated assembly)**
- assembly `src/Cobol.Net.Backend.Cil/` (refs `Cobol.Net.Compiler`, `Cobol.Net.Runtime`, `Mono.Cecil`) — the ONLY
  place the Cecil dependency lives (`DESIGN-backend-abstraction.md §3.2`).
- `CilBackend.cs` — `CilBackend : ICodeGenBackend`; drives `CilProgramEmitter` per unit; writes the PE + portable PDB
  + `.runtimeconfig.json`.
- `CilProgramEmitter.cs` — one program/class/interface unit → its `TypeDefinition` + the `__Dispatch` method + entry
  wrapper (the same shape `CSharpEmitter` emits, in IL).
- `CilStatementEmitter : IBoundStatementVisitor<CilFlow>`, `CilExpressionEmitter : IBoundExprVisitor<CilVal>`,
  `CilConditionEmitter : IBoundConditionVisitor<CilBranch>`, `CilBoolEmitter`, `CilPlaceLower` (structural `Place` →
  ldfld/stfld/ldelema/call), `CilDispatcher` (the PC `while(true)switch` lowered to a branch table),
  `CilRuntimeApi` (Cecil `MethodReference`s over the shared `RuntimeAbi`).
- The visitor implementations INHERIT exhaustiveness from the source-generated interfaces (P7 Step 6) — a new bound
  node is a compile error in the CIL backend too.

**Wiring & tests**
- `Cli/CliOptions.cs` + `CompilerDriver.Options` (`CompilerDriver.cs:34`): `BackendId Backend = BackendId.Roslyn`;
  `Cli/Program.cs`: `--backend {roslyn|cil}` option; `CompilerDriver.Compile` resolves via
  `BackendFactory.For(options.Backend, cilPlugin)`.
- `tests/Cobol.Net.Tests.BackendEquivalence/` — the harness (§ Milestone 2); a per-program `[Theory]` over the
  enabled corpus subset, byte-comparing `dotnet r.dll` vs `dotnet c.dll`.
- CI: the conformance job runs the enabled subset under `--backend cil` in addition to `--backend roslyn`.

---

## 4. STEP-BY-STEP

> Ordering principle: **seam-proof first (cheap, in-box), then the Cecil backend feature-by-feature, each gated by the
> equivalence harness.** Every milestone step is a COMMIT BOUNDARY; run battery items 1–4 at every boundary (item 5
> only at phase end). The CIL backend is additive: a not-yet-implemented node lowers to a loud `NotSupported`, and the
> equivalence harness only enables a program once every node it uses is implemented — so the battery is never red for
> "CIL doesn't do X yet".

### Milestone 0 — the seam-proof (recommended: execute as PHASE-07 Step 13)

#### Step 0.1 — `NullBackend` + `BackendFactory` plumbing

- **Files:** create `CodeGen/NullBackend.cs`; edit `CodeGen/ICodeGenBackend.cs` (`BackendFactory`), `CompilerDriver.cs`.
- **Change:** `NullBackend : ICodeGenBackend` returns `BackendArtifact(true, [], null, null)`. Add a hidden
  `--backend null` (test-only) so `CompilerDriver` can route a `BoundCompilation` to it. Proves the P6→P7 seam
  actually carries a `BoundCompilation` end-to-end with zero C# rendering.
- **Verify:** `cobol x.cob --backend null` returns success and writes nothing; battery 1+2+3 green.
- **COMMIT:** `P16 step0.1: NullBackend + BackendFactory routing (seam carries BoundCompilation end-to-end)`

#### Step 0.2 — in-box `DisplayBackend` (Reflection.Emit, DISPLAY-only)

- **Files:** create `CodeGen/DisplayBackend.cs`.
- **Change:** `DisplayBackend : ICodeGenBackend` over `System.Reflection.Emit.PersistedAssemblyBuilder` (.NET 10
  in-box — no external dependency, `DESIGN-backend-abstraction.md §3.3`). Walk `BoundCompilation`; for a
  `BoundProgram` whose statements are `BoundDisplay` (of `BoundStringLiteral`/`BoundNumericLiteral`/simple
  `BoundFieldOperand`) and `BoundStop`, emit a `Main` that `call`s `Console.WriteLine`/`Console.Write`; every other
  node throws `NotSupportedException` at emit (loud, never silent). Save a runnable `.dll` + `.runtimeconfig.json`.
- **Why:** the dependency-free proof that a **non-C#** backend consumes the neutral tree. If any node still exposed a
  C# string, this backend could not be written — that is the point.
- **Verify:** `cobol hello.cob --backend display -o /tmp/h.dll --run` prints the same as `--backend roslyn`;
  battery 1+2+3 green.
- **COMMIT:** `P16 step0.2: in-box Reflection.Emit DisplayBackend (DISPLAY-only) — seam proven by a non-C# consumer`

#### Step 0.3 — the backend-contract test, assertion 3 (two real consumers)

- **Files:** create `tests/Cobol.Net.Tests.Unit/BackendContract/` fixtures (assertions 1–2 land in P7 Step 11; this
  adds assertion 3, `DESIGN-backend-abstraction.md §6`).
- **Change:** a `[Category("BackendContract")]` test builds a fixed DISPLAY-only `BoundCompilation` and runs it
  through `RoslynBackend` AND `DisplayBackend`, asserting byte-identical stdout. Now a residual C# string in any node
  used by that program breaks the build.
- **Verify:** battery 1+2+3 green; the test is a hard CI gate.
- **COMMIT:** `P16 step0.3: backend-contract test assertion 3 (Roslyn≡Display on a neutral DISPLAY program)`

> **Milestone 0 exit:** the seam is real, and neutrality is proven by a second consumer + an executable test — the
> anti-rot guarantee. Everything below is the production backend and is **out of P7**; it can proceed whenever the CIL
> backend is scheduled without ever un-proving the seam.

### Milestone 1 — the `Cobol.Net.Backend.Cil` assembly + a Cecil DISPLAY-only backend

#### Step 1.1 — create the isolated assembly + `CilBackend` skeleton

- **Files:** create `src/Cobol.Net.Backend.Cil/Cobol.Net.Backend.Cil.csproj` (refs `Cobol.Net.Compiler`,
  `Cobol.Net.Runtime`, `Mono.Cecil` NuGet); `CilBackend.cs`; add the project to the solution and to the CLI's plug
  wiring (`BackendFactory.For(BackendId.Cil, new CilBackend())`).
- **Change:** `CilBackend : ICodeGenBackend` with a `ModuleDefinition` targeting `net10.0`, a `Program`
  `TypeDefinition`, an empty `Main`; writes the PE + a portable PDB + `.runtimeconfig.json` (reuse the Roslyn
  `WriteRuntimeConfig` JSON shape `RoslynBackend.cs:89-104`, factored into a shared helper). No statements yet — a
  non-DISPLAY program lowers to a loud `NotSupported`.
- **Why:** stands up the isolated Cecil assembly (default path stays Cecil-free, `DESIGN-backend-abstraction.md §3.2`)
  and the PE/PDB writer.
- **Verify:** the solution builds; `cobol hello.cob --backend cil` produces a loadable (empty-Main) assembly;
  battery 1+2+3 green (CIL not yet in the equivalence subset).
- **COMMIT:** `P16 step1.1: Cobol.Net.Backend.Cil assembly + CilBackend skeleton (PE + portable PDB writer)`

#### Step 1.2 — `CilRuntimeApi` over the shared `RuntimeAbi`; `CilPlaceLower` (read); DISPLAY

- **Files:** create `CilRuntimeApi.cs`, `CilPlaceLower.cs`, `CilStatementEmitter.cs` (partial — DISPLAY + STOP),
  `CilExpressionEmitter.cs` (partial — field/literal read).
- **Change:** `CilRuntimeApi` imports `MethodReference`s from `RuntimeAbi` (`DESIGN-backend-abstraction.md §2.3`).
  `CilPlaceLower.Read(Place)` lowers a `MemberPlace` `AccessPath` to `ldsfld`/`ldfld`/`ldelem` (the SENDING side).
  `CilStatementEmitter` implements `Visit(BoundDisplay)`/`Visit(BoundStop)` → `Console.Write*` + the runtime image
  calls (`CobolNum.FormatDisplay` via `CilRuntimeApi`). Names come from the shared `NameMangler`.
- **Verify:** DISPLAY of a group/field/numeric matches Roslyn byte-for-byte (probe below); battery 1+2+3 green.
  ```bash
  diff <(dotnet /tmp/r.dll) <(dotnet /tmp/c.dll)   # for a DISPLAY-of-fields program
  ```
- **COMMIT:** `P16 step1.2: CilRuntimeApi + CilPlaceLower read + DISPLAY/STOP (byte-matches Roslyn)`

### Milestone 2 — the backend-equivalence harness (the growth engine)

- **Files:** create `tests/Cobol.Net.Tests.BackendEquivalence/BackendEquivalenceTests.cs` + an
  `enabled-cil-corpus.txt` manifest (starts with the DISPLAY-only programs).
- **Change:** a `[Theory]` over the manifest: for each program, `CompilerDriver.Compile` with `Backend=Roslyn` → run
  → capture stdout/stderr/exit; same with `Backend=Cil`; assert **byte-identical** all three. A program enters the
  manifest ONLY when every bound-node kind it uses is implemented in the CIL backend (so the harness is always green;
  it grows as milestones land). Provide a `scripts/cil-corpus-add.sh <glob>` helper that trial-runs candidates and
  reports which pass, to grow the manifest safely.
- **Why:** the single mechanism that keeps CIL provably equal to Roslyn as features land; it is the CIL backend's
  "differential oracle" (`DESIGN-codegen-backend.md §2` bonus — the two backends cross-check each other).
- **Verify:** the harness is green over the DISPLAY-only manifest; battery 1+2+3+4 green.
- **COMMIT:** `P16 step2: backend-equivalence harness (Roslyn≡Cil stdout) + growable enabled-corpus manifest`

### Milestone 3 — numerics + moves (multi-sub-commit)

- **Files:** extend `CilExpressionEmitter` (full `IBoundExprVisitor<CilVal>`: `BoundBinary`/`BoundNegate`/
  `BoundPower`/`BoundNumLiteral` scaled-integer lowering per SSOT §1.2 #2 — `long`/`Int128`), `CilPlaceLower.Write`,
  `CilStatementEmitter` MOVE + arithmetic (`BoundMove` reading `MoveKind`+`StorageForm` from the node — P7 Step 7;
  `BoundAddTo`/…/`BoundCompute` with `ReceiverContext` scale/rounding lowered to IL).
- **Change:** lower the scaled-integer numeric pipeline to IL (unscaled value in a native int; scale is compile-time
  metadata — the CIL emitter aligns scales exactly as the Roslyn `NumX` render does, over the SAME `RuntimeAbi`
  numeric members). MOVE per `MoveKind` (group/elementary/edited/figurative-fill/ref-mod-slice) — a pure switch on
  the node, no re-classification (P7 Step 7 already moved that onto the node). Add the newly-covered programs to the
  equivalence manifest.
- **Verify:** equivalence harness green over the numerics+moves subset; probe COMPUTE/divide/ROUNDED/edited-MOVE
  programs byte-identical.
- **COMMIT (per sub-group):** `P16 step3X: CIL <numerics|moves|arithmetic> lowering; grow equivalence corpus`

### Milestone 4 — control flow + the PERFORM dispatcher (multi-sub-commit) — the HIGH-risk core

- **Files:** create `CilDispatcher.cs`; extend `CilStatementEmitter` (`BoundIf`, `BoundInlinePerform`,
  `BoundOutOfLinePerform`, `BoundGoTo`, `BoundGoToDepending`, `BoundExitParagraph`/`Perform`, `BoundEvaluate`,
  `BoundNextSentence`, `BoundSearch`, SET index/capacity, ALTER).
- **Change:** lower the single PC dispatcher (SSOT §1.2 #3; the Roslyn `while(true)switch`
  `CSharpEmitter.cs:88-120`) to an **IL branch table** — this is the CIL backend's **private** structure→branch
  lowering (SSOT §1.1: NOT a shared phase). `GO TO` sets `__pc` + branches to the dispatch head; an out-of-line
  PERFORM is a recursive bounded `__Dispatch(start,end)` IL call; inline PERFORM/EVALUATE lower to loops/branch
  chains. `BoundSearch`/`BoundSetCapacity` consume the **symbol/`Place`** the node now carries (P6 L3 de-C#-ing),
  not a path string.
- **Why:** the dispatcher is the backbone every non-trivial program uses; getting it byte-equal is the make-or-break
  of the backend.
- **Verify:** equivalence harness green over control-flow programs (PERFORM THRU, VARYING/AFTER, GO TO DEPENDING,
  EVALUATE, SEARCH ALL). This is where the harness earns its keep — byte-identical stdout on branch-heavy programs.
- **COMMIT (per sub-group):** `P16 step4X: CIL <dispatcher|PERFORM|EVALUATE|SEARCH> branch lowering; grow corpus`

### Milestone 5 — file I/O (multi-sub-commit)

- **Files:** extend `CilStatementEmitter` (`BoundOpen`/`Close`/`Read`/`Write`/`Rewrite`, keyed I/O, SORT/MERGE,
  Report Writer verbs, `BoundUnlock`), reusing the SAME `Cobol.Net.Runtime.IO` façade the Roslyn backend calls (via
  `CilRuntimeApi` over `RuntimeAbi`). File-connector keys come from the neutral `FileModel` (the emit-side
  qualification `CSharpEmitter.Call.cs:138-147` is a P6/driver concern, not a node string).
- **Change:** lower OPEN/READ/WRITE/CLOSE + FILE STATUS + USE declaratives to IL calls into the runtime file system;
  no new runtime code (the runtime is shared). Grow the manifest with the SQ/RL/IX corpus families.
- **Verify:** equivalence harness green over the file-I/O subset (compare stdout AND any produced data files
  byte-for-byte).
- **COMMIT (per sub-group):** `P16 step5X: CIL <sequential|keyed|sort|report-writer> I/O lowering; grow corpus`

### Milestone 6 — OO + the EC exception model (multi-sub-commit)

- **Files:** extend `CilProgramEmitter` (class/factory/interface `TypeDefinition`s, `__CobolInvoke` dispatch),
  `CilStatementEmitter` (`BoundInvoke*`, `BoundSetObjectRef`, `BoundRaise`/`Resume`/`RaiseObject`/`Raising`,
  `BoundEcChecked` → IL try/finally, pointers/`ALLOCATE`/`FREE`).
- **Change:** lower the OO type model (the method names via the shared `NameMangler`, the `OoMethodSymbol` the node
  carries after L4 removal — not `BoundMethod.CsName`) and the EC machinery (try/finally + the runtime
  `ExceptionState`/`__EcDispatch` calls via `RuntimeAbi`). This is the last feature family; at its end the manifest
  can grow toward the full corpus.
- **Verify:** equivalence harness green over the OO + EC subset (IC/OBSQ families, `>>TURN`, RAISE/RESUME, USE F3).
- **COMMIT (per sub-group):** `P16 step6X: CIL <OO dispatch|EC model|pointers> lowering; grow corpus`

### Milestone 7 — CLI + CI on both backends

- **Files:** `Cli/CliOptions.cs`, `Cli/Program.cs`, `CompilerDriver.cs`; CI workflow.
- **Change:** add `--backend {roslyn|cil}` (default `roslyn`) to the System.CommandLine surface; thread
  `BackendId Backend` through `CompilerDriver.Options` (`CompilerDriver.cs:34`); `CompilerDriver.Compile` resolves the
  backend via `BackendFactory.For(options.Backend, cilPlugin)`. In CI, add a matrix leg that runs the **enabled CIL
  subset** of the conformance corpus under `--backend cil` (byte-comparing to the golden AND to the Roslyn output via
  the equivalence harness). Roslyn remains the default and the full-corpus authority.
- **Verify:** `cobol … --backend cil` and `--backend roslyn` both work from the shipped CLI; CI green on both legs.
- **COMMIT:** `P16 step7: --backend {roslyn|cil} CLI wiring + CI matrix on both backends`

---

## 5. Verification (phase end)

Run the COMPLETE battery and confirm all green + neutral + equivalent:

```bash
cd E:/CobolSharp
dotnet build Cobol.Net.sln -v quiet                                                   # all projects incl. Backend.Cil
dotnet test  tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj -v quiet     # Roslyn: ~2003 green, 0 diffs
dotnet test  tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj -v quiet                   # incl. BackendContract
dotnet test  tests/Cobol.Net.Tests.BackendEquivalence/Cobol.Net.Tests.BackendEquivalence.csproj -v quiet  # Roslyn≡Cil subset
bash scripts/guard-fast.sh                                                             # legacy guard (untouched — no .g4 change)
```

Exit checks (all must pass):

1. **Byte-equivalence over the defined subset.** The equivalence manifest covers the target subset (grown from
   DISPLAY-only to the full feature families in Milestones 3–6); every entry's Roslyn and Cil stdout/stderr/exit are
   byte-identical. The subset's size and the growth-to-full plan are recorded in the manifest header.
2. **The neutrality contract is green (proven by construction).** The backend-contract test
   (`DESIGN-backend-abstraction.md §6`) passes: no `Place`/`Bound*` node exposes a `string`-returning render member
   or a raw-C#-identifier field, AND a non-C# backend (`DisplayBackend`, and now `CilBackend`) consumes the tree — the
   executable proof that the IR carries no C#.
3. **Roslyn is untouched as default.** `--backend roslyn` is the default; every pre-existing battery item is green;
   the Cecil dependency exists ONLY in `Cobol.Net.Backend.Cil` (grep: `Mono.Cecil` appears in no other csproj).
4. **A missing node is a compile error in BOTH backends.** Add a throwaway `sealed record BoundProbe : BoundStatement;`
   → `dotnet build` fails in `StatementEmitter` AND `CilStatementEmitter` (the source-generated exhaustiveness, P7
   Step 6, inherited by the CIL visitor). Remove the probe; do not commit it.

---

## 6. Rollback / resumability

- **Every step is an independent, battery-green commit.** Resume: read STATUS, `git log --oneline | grep "P16"` for
  the last landed sub-commit, continue at the next.
- **Milestone 0 is the anti-rot floor.** Even if Milestones 1–6 are deferred indefinitely, the seam-proof
  (`NullBackend` + `DisplayBackend`) + the contract test keep neutrality enforced with NO Cecil dependency. Reverting
  the Cecil milestones never un-proves the seam.
- **CIL is additive — a partial backend never breaks the battery.** An unimplemented node lowers to a loud
  `NotSupported`, and the equivalence manifest only enables a program once every node it uses is implemented. So a
  half-built CIL backend leaves the manifest smaller, never red.
- **Milestone 3–6 internal resume:** each is per-feature-family sub-commits; the enabled-corpus manifest records
  exactly which families are live, so the resume point is "the first family not yet in the manifest".
- **Risks & mitigations** (from `DESIGN-backend-abstraction.md §7`):
  - **R — CIL control-flow parity (HIGH, Milestone 4).** The private dispatcher branch lowering must match the
    Roslyn `while(true)switch` byte-for-byte in behavior. Mitigation: the equivalence harness on branch-heavy
    programs; land Milestone 4 in small sub-commits, one construct at a time (`feedback_iterate_one_at_a_time`).
  - **R — `RuntimeAbi` overload identity (MEDIUM).** A Cecil `MethodReference` import must pick the exact overload.
    Mitigation: `RuntimeMember` carries a parameter-shape key; `CilRuntimeApi` throws loudly on an ambiguous import.
  - **R — PDB / debug-info scope (LOW for correctness).** Portable-PDB sequence points are a quality goal, not a
    correctness one; the equivalence harness compares runtime behavior, not debug info. Defer full source-line PDBs
    to a follow-on if needed (`DESIGN-backend-abstraction.md §7` Open Q2).
  - **R — neutral tree regressions from later phases.** Any phase after P7 that adds a bound node with a C# string
    field fails the backend-contract test (§ Verification #2) — caught in CI, not years later.

---

## 7. ISO feature work in this phase

**None.** P16 is a **backend-additive** phase — it adds NO ISO construct and changes NO observable output of the
default (Roslyn) backend. The CIL backend's entire correctness criterion is *byte-equality with the Roslyn backend*
over the enabled corpus subset; it introduces no new semantics, no new goldens (it re-uses the existing conformance
goldens through the equivalence harness), and touches no `.g4` (no legacy-guard exposure). The four owner-locked
invariants (SSOT §1.2) and "no shared lowered IR" (§1.1) are upheld: the CIL backend does its structure→branch
lowering **privately**, and the bound tree it consumes is the SAME neutral tree the Roslyn backend consumes.

---

## Appendix A — file/line anchors (AS-IS, for the executing session)

| Concern | AS-IS location |
|---|---|
| Seam absent — `RoslynBackend` static string→dll | `CodeGen/RoslynBackend.cs:13,24`; uncached refs `:73-83` |
| Binding fused into codegen (must be P6-extracted) | `CodeGen/CSharpEmitter.cs:38-40` → `CallEmitRunUnit` `CSharpEmitter.Call.cs:88-147` |
| `Place.Read()/Write()` C# strings (P7 Step 11 removes) | `Binding/Place.cs:22-25` + every subtype |
| C#-path node fields (P6 L3/L4 de-C#, `DESIGN-backend-abstraction §1.3`) | `BoundTree.cs:32` (`BoundMethod.CsName`), `:109` (`BoundIndexRef.IndexField`), `:471` (`SetIndexTarget.IndexField`), `:493` (`BoundSetCapacity.TablePath`), `:508-511` (`BoundSearch`), `Place.cs:58,176` |
| Roslyn `WriteRuntimeConfig` JSON (share with CIL) | `CodeGen/RoslynBackend.cs:89-104` |
| The PC dispatcher (Roslyn `while(true)switch` → CIL branch table) | `CodeGen/CSharpEmitter.cs:88-120` |
| Exhaustive visitor interfaces (CIL visitors inherit exhaustiveness) | P7 Step 6 (`Binding/Bound/BoundTree.cs` `[BoundNode]`) |
| `RuntimeAbi` catalogue / `RoslynRuntimeApi` (CIL adds `CilRuntimeApi`) | `DESIGN-backend-abstraction.md §2.3`; runtime members `grep -hoE "Cobol[A-Za-z]+\." src/Cobol.Net.Compiler/CodeGen` |
| CLI options (add `--backend`) | `Cli/CliOptions.cs`, `Cli/Program.cs`, `CompilerDriver.Options` `CompilerDriver.cs:34` |
