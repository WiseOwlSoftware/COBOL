# PHASE 08 — Runtime Library Reorg: RunUnit context, FileConnector/FileRegistry, Pow10, role-based folders

- **Phase:** P8
- **Track:** rearchitecture
- **Risk:** MEDIUM
- **Depends on:** P0 (migration safety net — characterization/oracle bake, corpus consolidation, cached Roslyn refs).
  P8 is otherwise INDEPENDENT of the compiler rearchitecture (P1–P7) and may run in parallel with P5–P7. It touches
  ONLY `src/Cobol.Net.Runtime` + one one-line seam in the emitter's run-unit driver.
- **Design source:** `docs/rearchitecture/DESIGN-runtime-library.md` (decision-complete; this phase executes its §4
  migration). Cross-check `docs/COBOLNET_DESIGN.md` §17 (namespace/G8 deferral).

## Goal (one paragraph)
Unify all run-unit-lifetime state under ONE `RunUnit` context (owning `ProgramTable`, `ExceptionState`,
`ExternalStore`, `ModuleStack`, `FileRegistry`, `Clock`), ambient via `AsyncLocal`, with the existing static facades
(`ProgramRegistry`/`ExceptionState`/`CobolFile`/`CobolModule`/`AcceptSource`) kept as **thin delegating shims** so NO
generated code changes. Collapse the three duplicated file-organization machines (`SequentialFile`/`RelativeFile`/
`IndexedFile`) behind ONE abstract `FileConnector` base + a polymorphic `FileRegistry`, deleting the `Keyed*` static
fallthrough dispatch and unifying record framing into one `RecordFraming`. Single-source powers-of-ten into one
table-driven `Pow10`, deleting the (six) recompute-loop copies. Split the 700-line `ProgramRegistry.cs` into one file
per concern. Role-group the folders (`Values/`, `Verbs/`, `IO/`, `Control/`, `Exceptions/`, `Intrinsics/`) with
**namespaces UNCHANGED pre-G8**. The RootNamespace flip to `Cobol.Net.Runtime` + real sub-namespaces is OUT OF SCOPE
(deferred to G8 Cut 3, behind the compiler's `RuntimeApi` façade).

## Exit criteria
1. ONE run-unit reset owner (`RunUnit.Run` begin/end reproduces the old `ProgramRegistry.Reset()` + `CobolFile.Init()`
   + `CobolFile.CloseAll()` semantics exactly).
2. ONE polymorphic file-connector dispatch (`FileRegistry` over `FileConnector`); the `Keyed*` static methods in
   `IndexedFile.cs` and the sequential-first/keyed fallthrough in `CobolFile.cs` are DELETED.
3. `Pow10` single-sourced; the six recompute copies delegate then are deleted.
4. RL / IX / SQ / IC NIST goldens + the numeric unit tests + the full greenfield conformance battery are GREEN.
5. Emitted C# is BYTE-STABLE (no `RuntimeApi`-façade change; the emitter's run-unit driver changes by AT MOST one
   line, and only if Step 9 elects the emitter-side `RunUnit.Run` form — the default keeps it byte-stable via the
   lazy-ambient shim).

## STATUS
`NOT STARTED`

> The executing session updates this line to `IN PROGRESS @ step N` after each step and `DONE` at phase end. Also
> keep `resume-prompt.md`'s STATE banner and a `DEVLOG.md` entry per commit boundary (process rules).

---

## 1. Preconditions / how to resume

Before starting, confirm the battery is green from a clean state (this is the neutrality baseline every step is
measured against):

```bash
# Build the greenfield stack (runtime + compiler + CLI).
dotnet build CobolSharp.sln -v quiet

# Greenfield conformance + unit (the primary net for this phase — it compiles COBOL through the greenfield
# compiler and links THIS runtime dll):
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --verbosity quiet

# The full legacy NIST guard (regression backstop; runs the LEGACY CLI over the NIST corpus — proves the
# differential oracle still matches). This is unaffected by runtime-only changes but MUST stay green:
bash scripts/guard-fast.sh
```

Record the baseline pass counts (expected ≈ **2028 conformance + 213 unit + NIST 353 MATCH**; exact numbers may have
advanced — read `resume-prompt.md`). If any is red BEFORE you start, STOP and reconcile — do not begin P8 on a red
battery.

**Resuming mid-phase:** each step below is an independent commit boundary. `git log --oneline` shows which steps
landed (commit messages are prefixed `feat(cobolnet): Phase 8 — <step>`). Re-run the battery, find the last green
commit, and continue from the next step. No step leaves the tree un-buildable if committed.

---

## 2. Rationale — the problems this phase fixes (grounded in the survey + as-built code)

### 2.1 Run-unit state is five process-global stores with an INCONSISTENT threading model (prime smell)
Run-unit-lifetime state is scattered with no single reset owner and a split threading model:
- `Control/ProgramRegistry.cs` — static `ByPath`/`Order`/`ProbedModules` dictionaries; `Reset()` (line 359) is the
  de-facto run-unit boundary. It ALSO clears `ExternalStore` and `CobolModule` (lines 364–365) — so three subsystems'
  reset is entangled in one method.
- `Exceptions/ExceptionState.cs` — every member is `static … { get; private set; }`; the ambient EC gates
  `ArgumentFunctionChecking` (line 177) / `DataConversionChecking` (line 199) are process-global mutable.
- `ExternalStore` — a static `Dictionary<string,StorageCell>` living INSIDE `ProgramRegistry.cs` (lines 309–325).
- `Control/CobolModule.cs` — the ONE `[ThreadStatic]` store (line 24) — inconsistent with everything else.
- `IO/CobolFile.cs` — static `Files`/`Locked` + a `ConcurrentQueue` GC hand-off; `Init()` (line 26) resets the file
  registries through a SEPARATE entry point from `ProgramRegistry.Reset()`.
- `IO/AcceptSource.cs` — `public static Func<DateTime> Now { get; set; }` (line 26): a process-global mutable clock.

Consequences: no single "start a clean run unit" owner (three separate reset entry points, wired together only by
`ProgramRegistry.Reset` reaching sideways into `ExternalStore.Reset()`/`CobolModule.Reset()`); two run units cannot
coexist in one process; the `[ThreadStatic]`-vs-plain-static split is a latent thread-hop bug.

### 2.2 Three file organizations duplicate the ISO §9.1.13 status machine + read-position pair
`SequentialFile` (`SequentialFile.cs:16`), `RelativeFile` (`RelativeFile.cs:90`), `IndexedFile` (`IndexedFile.cs:17`)
each independently re-declare the read-position guard pair `_lastReadUnsuccessful` / `_prevOpWasSuccessfulRead`
(SequentialFile.cs:31–32, RelativeFile.cs:108–109, IndexedFile.cs:47–48), plus `_optionalAbsent`, `Status`, `Mode`,
host-path, and open/close preambles. The `CobolFile` static facade dispatches by **trying the sequential `Files`
registry first and falling through to a parallel `Keyed*` fan-out** (`CobolFile.cs:103-108,116,169,172,177`) whose
bodies (`KeyedOpen/KeyedClose/KeyedStatus/KeyedLastReadLength/KeyedOpenModeOf/KeyedInit/KeyedDrop/KeyedCloseAll`) live
at `IndexedFile.cs:657-707`, over the separate `RelativeFiles`/`IndexedFiles` registries (`IndexedFile.cs:512-513`).
That is a SECOND dispatch mechanism layered on the type split — a singular-pattern violation.

### 2.3 Powers-of-ten recomputed by an identical multiply-loop in SIX copies (efficiency, MEDIUM)
- `CobolNum.Pow10(int)` long (CobolNum.cs:404) + `CobolNum.Pow10Wide(int)` Int128 (CobolNum.cs:413)
- `CobolDec.Pow10(int)` Int128 (CobolDec.cs:272)
- `CobolDate.Pow10(int)` Int128 (CobolDate.cs:154)
- `CobolIntrinsics.Pow10D(int)` double (CobolIntrinsics.cs:43) + `CobolIntrinsics.Pow10I(int)` Int128
  (CobolIntrinsics.cs:51)
- `CobolFloat.Pow10(int)` double (CobolFloat.cs:83)

Every one is a `for` loop recomputing a compile-time-constant table on each numeric store/rescale/format. (The
DESIGN says "four" — the as-built count is six once `CobolNum.Pow10` and `CobolFloat.Pow10` are included. Dedup all
six.)

### 2.4 Value-library folder taxonomy is word-based, not role-based
`Text/` holds VALUE types (`CobolString`/`CobolBool`/`CobolClass`) while `Strings/` holds STATEMENT runtime
(`CobolStringOps` = STRING/UNSTRING, `CobolInspect` = INSPECT). `Numeric/`/`Text/`/`Tables/` are three peer value
folders with no umbrella. One-purpose signal/control types (`StopRun`, `MethodReturn`, `NotImplemented`,
`ResumeSignal`) are scattered between `Control/` and `Exceptions/`.

### 2.5 `ProgramRegistry.cs` is a 700-line grab-bag
It holds the registry PLUS the entire CALL-ABI (`ManagedPointer`, `ManagedPointer<T>`, `CellPointer`, `StorageCell`,
`CobolArg`, `ICobolProgram`, `CobolArgAdapt`), `ExternalStore`, `ProgramReturn`, and `CobolCallException`. The ABI
types are not "registry" concerns.

---

## 3. Target end-state (what exists when P8 is DONE)

Folder layout of `src/Cobol.Net.Runtime` (namespaces UNCHANGED — still `CobolNet.Runtime[.Exceptions|.IO]` exactly as
today; only physical folders + file names change; the RootNamespace flip is G8):

```
Values/
  Numeric/   CobolNum.cs, CobolDec.cs, CobolFloat.cs, CobolEdit.cs, CobolRounding.cs, CobolSizeError.cs,
             NumProfile.cs, Pow10.cs (NEW)
  Text/      CobolString.cs, CobolBool.cs, CobolClass.cs
  Tables/    CobolTable.cs, CobolDynTable.cs
Verbs/       CobolStringOps.cs (was Strings/), CobolInspect.cs (was Strings/)
IO/
  FileConnector.cs (NEW abstract base), FileRegistry.cs (NEW), RecordFraming.cs (NEW),
  SequentialConnector.cs (was SequentialFile), RelativeConnector.cs (was RelativeFile),
  IndexedConnector.cs (was IndexedFile), CobolFile.cs (thin static facade → RunUnit.Current.Files),
  FileStatus.cs (FileStatusCode, moved out of FileSupport.cs), FileSupport.cs (residual enums),
  CobolSort.cs, ReportWriter.cs, Clock.cs (NEW: IClock/SystemClock, was AcceptSource.Now), AcceptSource.cs (facade),
  Sharing/ PhysicalFileTable.cs (was CobolFile.Locks.cs sharing registry)
Control/
  RunUnit.cs (NEW), ProgramTable.cs (was ProgramRegistry, instance), ExternalStore.cs (own file, instance),
  ModuleStack.cs (was CobolModule, instance), ManagedPointer.cs, CellPointer.cs, StorageCell.cs,
  CallAbi.cs (CobolArg/ICobolProgram/CobolArgAdapt/CobolPassMode), CobolObject.cs, CobolPtr.cs,
  ExternalSwitches.cs, CobolInvokeArg.cs,
  ProgramRegistry.cs (thin STATIC SHIM → RunUnit.Current.Programs; kept for emitted surface),
  Signals/ StopRun.cs, ProgramReturn.cs, MethodReturn.cs, ResumeSignal.cs, NotImplemented.cs
Exceptions/
  ExceptionState.cs (instance on RunUnit + a static SHIM → RunUnit.Current.Exceptions),
  ExceptionCatalog.cs (immutable — stays static), EcFunctions.cs, CobolFatalException.cs
Intrinsics/
  CobolIntrinsics.{cs,Exact,Float,Text}.cs, CobolDate.cs   (only Pow10-dedup + clock change)
```

Key new/changed types & signatures:

- `Control/RunUnit.cs` — `public sealed class RunUnit` with instance `ProgramTable Programs`,
  `ExceptionState Exceptions`, `ExternalStore External`, `ModuleStack Modules`, `FileRegistry Files`,
  `IClock Clock`. Static `RunUnit Current` (ambient `AsyncLocal<RunUnit?>`), and a lifecycle entry:
  ```csharp
  public static RunUnit Current { get; }              // lazily establishes an ambient run unit if none (see Step 8)
  public static void Run(Action<RunUnit> body);       // begin/end owner; finally → Files.CloseAll()
  internal static RunUnit? TryCurrent { get; }
  ```
- `Control/ProgramTable.cs` — `public sealed class ProgramTable` — the verbatim port of today's `ProgramRegistry`
  bodies (`Register`/`RunMain`/`CallProgram`/`Cancel`/`ResolveVisible`/`ProbeSiblingModule`/…) as INSTANCE methods
  over instance `ByPath`/`Order`/`ProbedModules`, holding refs to its owning `RunUnit`'s `External` + `Modules`.
- `Control/ProgramRegistry.cs` — `public static class ProgramRegistry` — a thin shim: every method forwards to
  `RunUnit.Current.Programs.X(...)`; `Reset()` maps to the run-unit lifecycle (see Step 8).
- `Exceptions/ExceptionState.cs` — the class becomes INSTANCE (all members instance); a sibling
  `public static class ExceptionState` shim (same file or a `.Shim.cs`) forwards the emitted static surface to
  `RunUnit.Current.Exceptions`. (Naming: the instance type may be `ExceptionState` and the shim a nested/static
  partner; if a name clash is awkward, name the instance `ExceptionEngine` and keep `ExceptionState` as the static
  shim — SEE Step 7 note.)
- `Control/ModuleStack.cs` — `public sealed class ModuleStack` (instance, was `[ThreadStatic] CobolModule`) with a
  `public static class CobolModule` shim forwarding to `RunUnit.Current.Modules`.
- `Control/ExternalStore.cs` — `public sealed class ExternalStore` (instance) + a `public static class ExternalStore`
  shim? — NO: `ExternalStore` is referenced by name from GENERATED code? Verify (Step 6 grep). If emitted, keep a
  static shim; if only referenced by `ProgramTable`/`StorageCell`, make it a plain instance with no shim.
- `IO/FileConnector.cs` — `public abstract class FileConnector` with the shared `Status`/`Mode`/`OptionalAbsent`/
  `LastReadUnsuccessful`/`PrevOpWasSuccessfulRead`/`HostPath`/`OpenModeView`/`LastReadLength` + shared
  `Open`/`Close` preambles; abstract `OpenCore`/`ReadNext`/`WriteRecord`; keyed subclasses add `ReadByKey`/`Start`/
  `Delete`/`Rewrite`.
- `IO/FileRegistry.cs` — `public sealed class FileRegistry` — instance registry keyed by COBOL file-name over
  `FileConnector`; owns `Register`/`Open`/`Close`/`Read`/`Write`/`Status`/`LastReadLength`/`OpenModeOf`/`CloseAll` +
  the GC deferred-close queue + the per-object `MintInstanceKey`/`CloseAndDrop`. ONE polymorphic dispatch.
- `IO/CobolFile.cs` — the static facade stays (emitted surface) but every method forwards to
  `RunUnit.Current.Files.X(...)`; NO `Keyed*` methods.
- `Values/Numeric/Pow10.cs` — `internal static class Pow10 { long AsLong(int); Int128 AsWide(int); double AsDouble(int); }`.
- `IO/Clock.cs` — `public interface IClock { DateTime Now(); }` + `public sealed class SystemClock : IClock` (default;
  consults `COBOLNET_CLOCK`). `AcceptSource` keeps its emitted static methods (`Date()`/`Time()`/`Device(int)`/…) but
  reads `RunUnit.Current.Clock.Now()`.

---

## 4. STEP-BY-STEP

> Ordering follows DESIGN §4: safest/most-isolated first (`Pow10`), then folder moves (pure rename), then the
> `FileConnector` extraction (one organization at a time behind its NIST goldens), then `RunUnit` + shims, then the
> clock. The namespace flip is explicitly NOT in this phase.

> At EVERY commit boundary run the three-suite battery (§1 commands). "Battery green" below means all three green
> with baseline counts.

---

### Step 1 — `Pow10` single-source (safe, isolated)
**Files:**
- CREATE `src/Cobol.Net.Runtime/Numeric/Pow10.cs` (folder is still `Numeric/` at this step — it moves to
  `Values/Numeric/` in Step 2; do the dedup first so Step 2 is a pure move):
  ```csharp
  namespace CobolNet.Runtime;   // unchanged namespace (root, matches CobolNum's current ns)
  internal static class Pow10
  {
      private static readonly long[]   L = Build(19, (a) => a);          // 10^0 .. 10^18 (long-safe)
      private static readonly Int128[] W = BuildWide(39);                // 10^0 .. 10^38
      public static long   AsLong(int n)   => L[n];
      public static Int128 AsWide(int n)   => W[n];
      public static double AsDouble(int n) => n >= 0 && n < L.Length ? L[n] : Math.Pow(10, n);
      // Build helpers compute the tables ONCE at type init (identical values to the deleted loops).
  }
  ```
  Match the EXACT return types the callers need: `CobolNum` needs `long` (Pow10) and `Int128` (Pow10Wide);
  `CobolDec`/`CobolDate`/`CobolIntrinsics.Pow10I` need `Int128`; `CobolIntrinsics.Pow10D`/`CobolFloat.Pow10` need
  `double`.
- EDIT `Numeric/CobolNum.cs`: delete `Pow10` (line 404) and `Pow10Wide` (line 413) bodies; replace the internal
  `Pow10Wide` with `internal static Int128 Pow10Wide(int n) => Pow10.AsWide(n);` (keep the wrapper name — it is
  `internal` and referenced by `CobolEdit.cs:272`), and repoint the local `Pow10` call sites to `Pow10.AsLong`. (Or
  delete `Pow10` if it has no callers — grep first: `CobolNum.Pow10(` usage.)
- EDIT `Numeric/CobolDec.cs`: delete `Pow10` (line 272); replace all `Pow10(x)` with `Pow10.AsWide(x)`.
- EDIT `Intrinsics/CobolDate.cs`: delete `Pow10` (line 154); replace `Pow10(x)` with `Pow10.AsWide(x)`.
- EDIT `Intrinsics/CobolIntrinsics.cs`: delete `Pow10D` (line 43) and `Pow10I` (line 51); replace with
  `Pow10.AsDouble`/`Pow10.AsWide` at their call sites (CobolIntrinsics.cs:36; .Exact.cs:38,39,45,52,225,269).
- EDIT `Numeric/CobolFloat.cs`: delete `Pow10` (line 83); replace `Pow10(x)` with `Pow10.AsDouble(x)`
  (CobolFloat.cs:36,78).

**Why:** kills six recompute-loop copies (§2.3); pure value identity, no behavior change.

**Verify:**
```bash
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --verbosity quiet          # numeric kernels
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet
```
Expected: identical pass counts to baseline. (The numeric/arithmetic/intrinsic/date differential tests exercise every
`Pow10` path.)

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 1 — single-source Pow10, delete 6 recompute-loop copies`

---

### Step 2 — Folder moves + `ProgramRegistry.cs` split (pure rename; namespaces UNCHANGED)
This step is behavior-neutral by construction: only physical file locations and file names change. Namespaces stay
`CobolNet.Runtime`, `CobolNet.Runtime.IO`, `CobolNet.Runtime.Exceptions` EXACTLY as today, so nothing the compiler
emits or the runtime exposes changes. Use `git mv` so history follows.

**2a — Value library under `Values/`:**
- `git mv Numeric/ Values/Numeric/` (includes `Pow10.cs` from Step 1) — move `CobolNum/CobolDec/CobolFloat/CobolEdit/
  CobolRounding/CobolSizeError/NumProfile/Pow10`.
- `git mv Text/ Values/Text/` — `CobolString/CobolBool/CobolClass`.
- `git mv Tables/ Values/Tables/` — `CobolTable/CobolDynTable`.

**2b — Statement runtime under `Verbs/`:**
- `git mv Strings/CobolStringOps.cs Verbs/CobolStringOps.cs`
- `git mv Strings/CobolInspect.cs Verbs/CobolInspect.cs`
- delete the now-empty `Strings/`.

**2c — Split `Control/ProgramRegistry.cs` (700 lines) by concern** (still all namespace `CobolNet.Runtime`; do NOT yet
change static→instance — that is Step 8; this is a PURE file split of the existing static/POCO types):
- `Control/ManagedPointer.cs` ← `ManagedPointer` (abstract) + `ManagedPointer<T>` + `NullManagedPointer`.
- `Control/CellPointer.cs` ← `CellPointer`.
- `Control/StorageCell.cs` ← `StorageCell`.
- `Control/CallAbi.cs` ← `CobolPassMode` enum + `CobolArg` + `ICobolProgram` + `CobolArgAdapt`.
- `Control/ExternalStore.cs` ← `ExternalStore` (STILL static at this step).
- `Control/Signals/ProgramReturn.cs` ← `ProgramReturn`; keep `CobolCallException` next to `ProgramTable`/registry
  (it is a registry-machinery failure) — put it in the leftover `Control/ProgramRegistry.cs`.
- The leftover `Control/ProgramRegistry.cs` keeps `ProgramRegistry` (static, unchanged) + `CobolCallException`.

**2d — Group control signals under `Control/Signals/`:**
- `git mv Control/StopRun.cs Control/Signals/StopRun.cs`
- `git mv Control/MethodReturn.cs Control/Signals/MethodReturn.cs`
- `git mv Control/NotImplemented.cs Control/Signals/NotImplemented.cs`
- `git mv Exceptions/ResumeSignal.cs Control/Signals/ResumeSignal.cs`
  (`ProgramReturn.cs` already placed in 2c. `CobolFatalException` STAYS in `Exceptions/` — it is an EC condition
  carrier, per DESIGN §2.6.)

**2e — `FileStatus` out of `FileSupport`:**
- Move `FileStatusCode` from `IO/FileSupport.cs` into a new `IO/FileStatus.cs` (same namespace). Leave the residual
  enums (`FileOpenMode`/`FileSharing`/…) in `FileSupport.cs`.

**Why:** role-based cohesion (§2.4, §2.5); one concern per file. Namespaces unchanged ⇒ green by construction.

**Verify:** `dotnet build CobolSharp.sln -v quiet` compiles clean, then the full three-suite battery. Because
namespaces did not change, expect byte-identical emitted C# and identical pass counts.

> IMPORTANT: `.csproj` uses SDK-style globbing (`**/*.cs`), so moved files are picked up automatically. Confirm no
> `<Compile Include>` hard-codes an old path (grep the csproj). Also delete stale `obj/`/`bin/` if a move confuses
> incremental build (`dotnet clean`).

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 2 — role-based runtime folders + split ProgramRegistry.cs (namespaces unchanged)`

---

### Step 3 — `RecordFraming` helper (extract the shared framing)
**Files:**
- CREATE `IO/RecordFraming.cs` — `internal static class RecordFraming` holding the ONE 4-byte little-endian
  length-prefix framing used by (a) `KeyedFrames` (shared relative/indexed varying framing) and (b) the sequential
  varying framing at `SequentialFile.cs:42-48` / the read path at `SequentialFile.cs:360`. Expose
  `WriteFrame(...)` / `ReadFrame(...)` / `FrameLength(...)` matching the exact byte layout the three connectors
  currently produce (verify byte-for-byte against `KeyedFrames` — the goldens depend on the on-disk layout).
- EDIT `SequentialFile.cs`, `RelativeFile.cs`, `IndexedFile.cs` (and wherever `KeyedFrames` lives) to call
  `RecordFraming`; delete the local copies.

**Why:** DESIGN §2.2 — one framing helper across organizations; prerequisite for the `FileConnector` base so the base
can own framing once.

**Verify:** the file-I/O goldens are the exact net (they read/write the framed on-disk records):
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet \
  --filter "FullyQualifiedName~FileIo|FullyQualifiedName~KeyedIo|FullyQualifiedName~InterProgramFile"
```
then the full battery + `bash scripts/guard-fast.sh` (the RL/IX/SQ NIST chains). Byte-identical on-disk framing is the
pass condition.

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 3 — unify record framing into IO/RecordFraming`

---

### Step 4 — `FileConnector` base + connectors (rename, hoist SHARED members only)
Do this WITHOUT changing dispatch yet (that is Step 5). Introduce the base and make the three connectors derive from
it, hoisting ONLY the provably-identical members.

**Files:**
- CREATE `IO/FileConnector.cs` — `public abstract class FileConnector` (namespace `CobolNet.Runtime.IO`). Hoist the
  members that are byte-identical across all three: `Status` (get; protected set), `Mode`/`OptionalAbsent`,
  `LastReadUnsuccessful` (`_lastReadUnsuccessful`), `PrevOpWasSuccessfulRead` (`_prevOpWasSuccessfulRead`), `HostPath`,
  `OpenModeView`, `LastReadLength`, `IsOpen`, `SetStatus`, and the shared OPEN/CLOSE preamble
  (`Open(FileOpenMode)` → `OpenCore`, `Close()` → `CloseCore`). Declare abstract: `OpenCore(FileOpenMode)`,
  `ReadNext(out string)` (or the org-specific read entry). Keyed subclasses ADD `ReadByKey`/`Start`/`Delete`/
  `Rewrite`/`ReadPrevious`.
- RENAME `IO/SequentialFile.cs` → `IO/SequentialConnector.cs`; class `SequentialFile` → `SequentialConnector :
  FileConnector`. Keep line-sequential + record-sequential + LINAGE unified in the one class (DESIGN §6 recommends
  NOT splitting — shared position/framing state). Remove the now-inherited fields; keep org-specific overrides
  (e.g. the sequential REWRITE-requires-prior-READ '43' gate at SequentialFile.cs:411).
- RENAME `IO/RelativeFile.cs` → `IO/RelativeConnector.cs`; `RelativeFile` → `RelativeConnector : FileConnector`.
- RENAME `IO/IndexedFile.cs` → `IO/IndexedConnector.cs`; `IndexedFile` → `IndexedConnector : FileConnector`. NOTE:
  this file ALSO contains the `partial class CobolFile` keyed-routing section (`RelativeFiles`/`IndexedFiles`
  registries + `Keyed*` methods + the public keyed verbs `RewriteKeyed`/`DeleteRecord`/`ReadKeyed*`/`Start*`/
  `DeleteFile`). Move that `partial class CobolFile` section OUT into `IO/CobolFile.Keyed.cs` for now (it will be
  deleted/absorbed in Step 5). Keep the connector class in `IndexedConnector.cs`.

> ⚠ MIGRATE ONE ORGANIZATION AT A TIME. Hoist Sequential first, run the SQ goldens; then Relative (RL goldens); then
> Indexed (IX goldens). Do NOT "unify" a status transition the three implement differently (the base is the TRUE
> common denominator; org-specific rules stay overrides — DESIGN §5 risk).

**Why:** DESIGN §2.2 — dedup the §9.1.13 status machine + read-position pair into one base.

**Verify (after EACH connector):**
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet \
  --filter "FullyQualifiedName~FileIo|FullyQualifiedName~KeyedIo|FullyQualifiedName~Linage"
bash scripts/guard-fast.sh    # SQ/RL/IX/OBSQ NIST chains
```
Expected: unchanged. The connectors still register into the SAME `Files`/`RelativeFiles`/`IndexedFiles` dictionaries
via the SAME `Keyed*` routing (unchanged this step) — only the class internals were refactored onto the base.

**COMMIT BOUNDARY** (one per connector is fine) — `feat(cobolnet): Phase 8 step 4 — FileConnector base; Sequential/Relative/Indexed connectors`

---

### Step 5 — `FileRegistry` polymorphic dispatch; DELETE the `Keyed*` fallthrough
Now collapse the two-registry, sequential-first/keyed-fallthrough dispatch into ONE polymorphic registry.

**Files:**
- CREATE `IO/FileRegistry.cs` — `public sealed class FileRegistry` (INSTANCE; namespace `CobolNet.Runtime.IO`). ONE
  `Dictionary<string, FileConnector>` keyed by COBOL file-name. Port the bodies of `CobolFile.Register`/`Open`/`Close`/
  `Write`/`WriteAdvancing`/`Read`/`Rewrite`/`Status`/`LastReadLength`/`OpenModeOf`/`AtEnd`/`Failed`/`CloseAll`/
  `SetLinage`/`LinageCounter`/`EndOfPage`/`CloseReelUnit` + the keyed verbs (`RewriteKeyed`/`DeleteRecord`/
  `ReadKeyed[Next|Previous]`/`ReadKeyed`/`StartRelative`/`StartIndexed`/`StartFirstLast`/`DeleteFile`) as INSTANCE
  methods. `Register` constructs the RIGHT `FileConnector` subclass (Sequential/Relative/Indexed) and stores it under
  the name; every verb becomes `if (_files.TryGetValue(name, out var c)) c.Xxx(...)` — polymorphic, NO
  sequential-first probe, NO `Keyed*`. Fold in the GC deferred-close queue (`_pendingObjectClose`) +
  `MintInstanceKey`/`CloseAndDrop`/`EnqueueInstanceClose` + `_instSeq`. Fold the sharing hooks (`LocksInit`/
  `IsSharingActive`/`SharedOpen*`/`SharedClose`) — move the sharing REGISTRY (`ConnectorShares`/`Physical` from
  `CobolFile.Locks.cs`) into `IO/Sharing/PhysicalFileTable.cs` as an instance owned by the `FileRegistry` (or by
  `RunUnit`); `FileRegistry` calls into it.
- REWRITE `IO/CobolFile.cs` — the static facade becomes a PURE delegator. For THIS step (RunUnit not yet introduced),
  hold a single `private static readonly FileRegistry _reg = new();` and forward every method to `_reg.X(...)`.
  `Init()` → `_reg.Reset()` (a new method that clears + drains). DELETE all `Keyed*` calls; DELETE
  `IO/CobolFile.Keyed.cs` (the moved keyed-routing partial from Step 4) and the separate `RelativeFiles`/
  `IndexedFiles` dictionaries — the ONE `_files` dictionary in `FileRegistry` replaces them.
- DELETE `CobolFile.Locks.cs`'s static sharing registry (moved to `PhysicalFileTable`); keep the `FileSharing`/
  `FileLockMode`/retry enums where the connectors expect them.

**Why:** DESIGN §2.2 — remove the SECOND dispatch mechanism (singular pattern); one registry, polymorphic on
`FileConnector`.

**Verify:** the FULL file battery is the sharp edge here (this is where a dispatch bug shows):
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --verbosity quiet
bash scripts/guard-fast.sh    # RL/IX/SQ/IC + OBSQ chains, sharing/lock tests
```
Pay special attention to: the EXTERNAL-connector persistence path (`::EXT::` key band — IC227A: a later describer
must NOT clobber the live connector; `CobolFile.cs:84-85` / `IndexedFile.cs:522,533`), the per-object instance
connectors (M2-OO-1i two-instances golden), and the GC deferred-close ordering.

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 5 — one polymorphic FileRegistry; delete the Keyed* fallthrough dispatch`

---

### Step 6 — Grep audit: which shim types are named by GENERATED code
Before making state instance-owned, enumerate EXACTLY which runtime members the emitter names by string, so every one
keeps a static shim (else generated code breaks at Roslyn time). This is a read-only audit that pins Step 7/8's shim
surface.

```bash
# Every runtime name the compiler emits as text:
grep -rEn "ProgramRegistry\.|ExceptionState\.|CobolFile\.|CobolModule\.|ExternalStore\.|AcceptSource\.|EcFunctions\." \
  src/Cobol.Net.Compiler/CodeGen/ src/Cobol.Net.Compiler/Binding/
```
Record the set. Known emitted surfaces (from the survey): `ProgramRegistry.{Register,Reset,RunMain,CallProgram,
Cancel}`, `CobolFile.{Init,Register,Open*,Close*,Write*,Read,Rewrite,Status,...,CloseAll,MintInstanceKey}`,
`CobolModule` (via `ProgramRegistry`/`FUNCTION MODULE-NAME` → check `EcFunctions`/intrinsic emit),
`ExceptionState.{Set,SetIo,SetObject,Clear,SetPropagating*,TakePropagated*,ArgumentFunctionChecking,
DataConversionChecking,ArgumentError,DataConversionError,...}`, `AcceptSource.{Date,DateYYYYMMDD,Day,DayYYYYDDD,Time,
DayOfWeek,Device}`. `ExternalStore` — check whether the emitter names it directly or only `ProgramTable`/`StorageCell`
reference it (determines whether it needs a shim).

**No commit** (audit only; fold the finding into Step 7/8 notes).

---

### Step 7 — `ExceptionState` + `ModuleStack` + `ExternalStore` → instance (with static shims)
Convert the three simplest stores to instance form FIRST (they have no cross-dependency on `ProgramTable`'s resolve
logic), each keeping a static shim over `RunUnit.Current`. `RunUnit` itself lands in Step 8, so at THIS step introduce
a minimal `RunUnit` shell holding just these three instances (or stage them as instances reachable via a temporary
ambient holder). Recommended: land `RunUnit` shell here (owning `Exceptions`/`Modules`/`External`) and expand it in
Step 8.

**Files:**
- `Exceptions/ExceptionState.cs` — make every member INSTANCE (the `static … { get; private set; }` → instance
  auto-props; the ambient gates `ArgumentFunctionChecking`/`DataConversionChecking` → instance; the propagation slots
  `_propagated`/`_propagatedObject` → instance fields). Add a `public static class ExceptionState`... — but the type
  name collides. RESOLUTION: rename the INSTANCE class to `ExceptionEngine` and keep `public static class
  ExceptionState` as the shim forwarding every emitted member (`Set`/`SetIo`/`SetObject`/`Clear`/`SetPropagating*`/
  `TakePropagated*`/`ArgumentFunctionChecking`/`DataConversionChecking`/`ArgumentError`/`DataConversionError`/
  `LastName`/`LastFatal`/`LastFile`/`LastIoStatus`/`LastLocation`/`LastStatement`/`ExceptionObject`/`ObjectSentinel`)
  to `RunUnit.Current.Exceptions.X`. Keep the file/type XML docs.
- `Control/CobolModule.cs` → split: `Control/ModuleStack.cs` = `public sealed class ModuleStack` (instance, drop
  `[ThreadStatic]`, the `_stack`/`Stack`/`PushMain`/`Push`/`Pop`/`Reset`/`Name` bodies become instance);
  `Control/CobolModule.cs` = `public static class CobolModule` shim forwarding to `RunUnit.Current.Modules`.
- `Control/ExternalStore.cs` — make `ExternalStore` an INSTANCE class (`Cell(name,initialImage)`/`Reset()` instance).
  Add a static shim ONLY if Step 6 found it emitted by name; otherwise no shim (referenced solely by
  `ProgramTable`/`StorageCell`, which will hold a `RunUnit` ref).
- `Control/RunUnit.cs` (SHELL) — `public sealed class RunUnit` with `ExceptionEngine Exceptions { get; } = new();`,
  `ModuleStack Modules { get; } = new();`, `ExternalStore External { get; } = new();`, and the `AsyncLocal<RunUnit?>`
  ambient + `Current` (lazily creating an ambient run unit if none — so pre-Step-8 code paths that touch
  `ExceptionState`/`CobolModule` outside `RunUnit.Run` still work). Expand in Step 8.

**Why:** state ownership; remove the lone `[ThreadStatic]`; ambient EC gates become run-unit-scoped (§2.1).

**Verify:** the EC + inter-program + MODULE-NAME nets:
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet \
  --filter "FullyQualifiedName~Exception|FullyQualifiedName~Call|FullyQualifiedName~InterProgram|FullyQualifiedName~Intrinsic"
```
then FULL battery + `bash scripts/guard-fast.sh`. The EXCEPTION-* function goldens + IC-series are the edge.

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 7 — ExceptionState/ModuleStack/ExternalStore instance-owned on RunUnit (static shims)`

---

### Step 8 — `ProgramTable` + `RunUnit` lifecycle owner (the run-unit reset unification)
Promote the registry to an instance and make `RunUnit.Run` the ONE lifecycle boundary that reproduces the old
`Reset()` + file `Init()` + `CloseAll()` semantics.

**Files:**
- `Control/ProgramTable.cs` — `public sealed class ProgramTable` = the verbatim port of `ProgramRegistry`'s bodies as
  INSTANCE methods over instance `ByPath`/`Order`/`ProbedModules`. Replace its sideways reaches:
  `ExternalStore.Reset()` → `_owner.External.Reset()`; `CobolModule.Push/Pop/Reset` → `_owner.Modules.X`;
  `ExceptionState.TakePropagated`/`Set` → `_owner.Exceptions.X`. Hold `RunUnit _owner` (ctor-injected). `Reset()`'s
  body (clear ByPath/Order/Probed + External + Modules) becomes the run-unit BEGIN.
- `Control/RunUnit.cs` (EXPAND) — add `ProgramTable Programs { get; }` (ctor: `Programs = new ProgramTable(this)`),
  `FileRegistry Files { get; } = new()`, `IClock Clock { get; set; } = SystemClock.Instance` (Clock wired in Step 9).
  Implement:
  ```csharp
  public static void Run(Action<RunUnit> body)
  {
      var ru = new RunUnit();
      var prior = _current.Value; _current.Value = ru;
      try { body(ru); }
      finally { ru.Files.CloseAll(); _current.Value = prior; }
  }
  ```
  `Current` lazily establishes an ambient run unit if `_current.Value` is null (so the delegating shims work even
  when the emitter has NOT switched to `RunUnit.Run` — keeps the emitted surface byte-stable; see below).
- `Control/ProgramRegistry.cs` — the static shim. `Register`/`RunMain`/`CallProgram`/`Cancel` → `RunUnit.Current.
  Programs.X`. `Reset()` → establish/clear the ambient run unit: `RunUnit.ResetCurrent()` (clears Programs+External+
  Modules on the ambient instance) — this reproduces EXACTLY the old `ProgramRegistry.Reset()` (which cleared
  ByPath/Order/Probed + ExternalStore + CobolModule).
- `IO/CobolFile.cs` — flip its `private static readonly FileRegistry _reg` (Step 5) to forward to
  `RunUnit.Current.Files` instead, so the file registry is the run-unit's. `Init()` → `RunUnit.Current.Files.Reset()`.

**Emitted-surface decision (byte-stability):** the generated `Program.Main` (emitter `CSharpEmitter.Call.cs:740-753`)
today emits `ProgramRegistry.Reset(); CobolFile.Init(); __CobolModule.Register(); try { ProgramRegistry.RunMain(...);
} catch(StopRun){} ... finally { CobolFile.CloseAll(); }`. Two options — pick DEFAULT unless the owner wants the
cleaner form:
- **DEFAULT (byte-stable, zero emitter change):** keep the emitted text identical. `ProgramRegistry.Reset()` lazily
  establishes the ambient `RunUnit` (via `RunUnit.Current`), clears it, and `CobolFile.Init()`/`CloseAll()` operate on
  `RunUnit.Current.Files`. The run unit is torn down implicitly at process exit. NO emitter change ⇒ emitted C# is
  byte-identical (exit-criterion 5 satisfied trivially).
- **OPTIONAL (one-line emitter change):** wrap the body in `RunUnit.Run(ru => { ... })`. This is cleaner but changes
  the emitted `.g.cs` (a reviewed gate-3 re-baseline). DEFER to G8 unless the owner asks — it is NOT required for P8's
  exit criteria.

**Why:** DESIGN §2.1 / exit-criterion 1 — ONE reset owner; enables concurrent in-process run units (opt-in).

**Verify — reproduce `Reset()` semantics EXACTLY.** First a targeted inter-program subset, then full:
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet \
  --filter "FullyQualifiedName~Call|FullyQualifiedName~InterProgram|FullyQualifiedName~Cancel|FullyQualifiedName~External"
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --verbosity quiet
bash scripts/guard-fast.sh
```
The IC-series (CALL/CANCEL/EXTERNAL, especially IC227A EXTERNAL persistence + RECURSIVE/INITIAL state) is the sharp
regression edge — a `Reset()`-semantics mismatch shows there first.

Also run a direct CLI smoke on a CALL program to confirm the ambient run unit establishes correctly:
```bash
dotnet src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.dll <a-CALL-fixture>.cob --std 2002 -o /tmp/o.dll --run
```

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 8 — RunUnit lifecycle owner + ProgramTable; ProgramRegistry becomes a shim`

---

### Step 9 — Clock injection (`IClock`/`SystemClock` replaces `AcceptSource.Now`)
**Files:**
- CREATE `IO/Clock.cs` — `public interface IClock { DateTime Now(); }` + `public sealed class SystemClock : IClock`
  whose `Now()` consults `COBOLNET_CLOCK` (the existing cross-process pin) then falls back to `DateTime.Now` — i.e.
  the current `AcceptSource.DefaultNow` body, moved here. `public static readonly SystemClock Instance = new();`.
- EDIT `Control/RunUnit.cs` — `public IClock Clock { get; set; } = SystemClock.Instance;`.
- EDIT `IO/AcceptSource.cs` — KEEP the emitted static methods (`Date`/`DateYYYYMMDD`/`Day`/`DayYYYYDDD`/`Time`/
  `DayOfWeek`/`Device`) — they are the emitted surface (`CSharpEmitter.Accept.cs:43,54,65,76,90-95`). Change their
  internal `Now()` reads to `RunUnit.Current.Clock.Now()`. DELETE the `public static Func<DateTime> Now` seam.
- Test seam: the conformance clock pin is the `COBOLNET_CLOCK` env var (`AcceptDifferentialTests.cs:15`) — cross
  process, unchanged (SystemClock still reads it). No in-process `AcceptSource.Now` assignment exists (grep confirmed
  only doc/emit references), so NO test change is needed. If a future in-process test wants a fixed clock it sets
  `RunUnit.Current.Clock = new FixedClock(...)`.

**Why:** DESIGN §2.7 — remove the process-global mutable clock seam; injectable per run unit.

**Verify:**
```bash
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet \
  --filter "FullyQualifiedName~Accept|FullyQualifiedName~Date|FullyQualifiedName~Intrinsic"
```
then FULL battery. The DATE/TIME/date-intrinsic goldens (which pin `COBOLNET_CLOCK`) are the net.

**COMMIT BOUNDARY** — `feat(cobolnet): Phase 8 step 9 — IClock/SystemClock on RunUnit replaces AcceptSource.Now`

---

### Step 10 — Doc + index sync (close-out)
- Update `docs/COBOLNET_DESIGN.md` §17 (runtime layout) to the new folder topology; note the RootNamespace flip is
  still deferred to G8.
- Update `docs/DOC_INDEX.md` if any doc's subject changed; mark `DESIGN-runtime-library.md` §4 migration as EXECUTED
  (steps 1–9), leaving the G8 namespace flip (its step 6) as the remaining item.
- Flip this file's STATUS line to `DONE`; update `resume-prompt.md`'s STATE banner.
- Add the final DEVLOG entry.

**COMMIT BOUNDARY** — `docs(cobolnet): Phase 8 — runtime reorg complete; sync design + index`

---

## 5. Verification (phase end)

Run the FULL battery from a clean build and confirm baseline-or-better counts + zero diffs:
```bash
dotnet clean CobolSharp.sln
dotnet build CobolSharp.sln -v quiet
dotnet test tests/Cobol.Net.Tests.Conformance/Cobol.Net.Tests.Conformance.csproj --verbosity quiet   # ≈2028
dotnet test tests/Cobol.Net.Tests.Unit/Cobol.Net.Tests.Unit.csproj --verbosity quiet                 # ≈213
bash scripts/guard-fast.sh                                                                            # NIST 353 MATCH
```

**Byte-exact / neutrality checks:**
- Emitted C# byte-stability: pick 3–4 representative fixtures (a CALL/CANCEL program, an INDEXED-file program, an
  ACCEPT DATE program, a numeric-heavy program), compile with `--std 2002 -o out.dll` BEFORE Step 1 (stash the
  `.g.cs`) and AFTER Step 9; `diff` the two `.g.cs`. They MUST be byte-identical (the DEFAULT emitter path makes no
  emitter change). If they differ, an unintended emitter/`RuntimeApi` change crept in — investigate before declaring
  DONE.
- On-disk file layout: the RL/IX/SQ goldens compare produced data files; a framing regression (Step 3/4/5) surfaces
  as a golden diff — those must be clean.
- Hidden-mutable-static gate (concurrency hygiene): grep the runtime for any REMAINING mutable process-global static
  that should be on `RunUnit`:
  ```bash
  grep -rEn "static\s+(readonly\s+)?(Dictionary|List|HashSet|int|bool|string|Func)" src/Cobol.Net.Runtime/ \
    | grep -viE "const |readonly .* = |ExceptionCatalog|Pow10|SystemClock.Instance|RecordSize"
  ```
  Everything surviving must be genuinely immutable (`ExceptionCatalog` table, `Pow10` tables, `SystemClock.Instance`)
  — any mutable run-unit state still static is a miss.

---

## 6. Rollback / resumability

- Each step is an independent, buildable, green commit. To roll back a bad step: `git revert <commit>` (or reset to
  the prior commit) — no step depends on a LATER step's file existing.
- **Highest-risk steps: 5 and 8.** Step 5 (delete the `Keyed*` fallthrough) and Step 8 (`Reset()` → `RunUnit`
  lifecycle) are where behavior can drift. Mitigation: both are verified by the IC/RL/IX NIST goldens BEFORE the
  commit; if a golden reddens, the diff points at the exact program.
- **`AsyncLocal` cost:** confined to I/O/CALL/raise sites (the numeric hot path never touches `RunUnit`). Hot facades
  (`CobolFile.*`, `ExceptionState` raise sites) should cache `RunUnit.Current` in a local at method entry if a
  micro-benchmark shows regression; the arithmetic-loop benchmark from the efficiency critique is unaffected.
- **`FileConnector` over-unification:** if hoisting a member reddens one organization's goldens, that member is NOT
  the true common denominator — push it back down as an override. Migrate one organization at a time (Step 4) so the
  blast radius is one suite.
- **Shim surface miss:** if generated code fails to Roslyn-compile after Step 7/8 (a named member lost its static
  shim), Step 6's grep is the checklist — add the missing shim method. The failure is loud (Roslyn compile error in
  the conformance harness), not silent.
- Interrupted mid-step: the working tree of an incomplete step will not build; `git stash` or finish the step. Never
  commit a non-building tree. If unsure where you were, `git log --oneline | grep "Phase 8"` shows the last landed
  step; re-run §1 baseline; continue.

---

## 7. ISO feature work in this phase

**None (this is a pure rearchitecture phase — no new ISO surface).** No spec sections gain support; no editions
change; no new conformance goldens are ADDED. The phase PRESERVES existing spec behavior, anchored by these already-
present spec citations in the touched code (verify they still hold, do not re-derive):
- ISO §9.1.13 (FILE STATUS machine) + §14.9.30/§14.9.35 (read-position state) — owned by `FileConnector` after Step 4.
- ISO §14.6.1 (run unit) / §14.9.5 (CANCEL) / §8.4.6.3 (program-name scope) / §8.6.7 (EXTERNAL sharing, §13.18.22) —
  the `RunUnit`/`ProgramTable`/`ExternalStore` semantics ported verbatim in Steps 7–8; IC227A (EXTERNAL persistence)
  is the golden.
- ISO §15.65 (FUNCTION MODULE-NAME) — `ModuleStack` (Step 7); the MODULE-NAME intrinsic goldens are the net.
- ISO §14.9.1.4 GR7–GR12 (ACCEPT DATE/DAY/TIME) — `AcceptSource` over `IClock` (Step 9); the ACCEPT-temporal goldens
  pin `COBOLNET_CLOCK`.

The regression net (RL/IX/SQ/IC NIST goldens + the EXCEPTION-*/ACCEPT/date-intrinsic conformance tests + the numeric
unit tests) is exactly the set that VERIFIES this phase preserved spec behavior. If any new gap is discovered while
refactoring (e.g. a status transition the three connectors did inconsistently that the spec says should be uniform),
DO NOT fix it silently under cover of the reorg — file it as its own spec-cited change with its own golden, per the
process rules (behavior changes are never smuggled into a rearchitecture commit).
```
