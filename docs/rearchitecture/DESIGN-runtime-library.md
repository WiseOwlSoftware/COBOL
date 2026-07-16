# DESIGN — Target Runtime Library Organization (`Cobol.Net.Runtime`)

Status: EXECUTED (PHASE-08) — §2's target design IS the as-built runtime; §4's migration steps 1–5 are DONE
(plus the `ExternalSwitches`→`SwitchStore` conversion the §5 hidden-static gate surfaced); step 6 (the G8
RootNamespace flip + real sub-namespaces) is the ONE remaining item, deferred to G8 Cut 3 behind the compiler's
`RuntimeApi` façade. §1 records the pre-P8 problems as the rationale; §6's former open questions are resolved
inline. Scope: the organization + API of the greenfield runtime `src/Cobol.Net.Runtime` that COBOL.NET-generated
C# programs call. Upholds the HARD INVARIANTS (typed-native only; spec-first; battery green throughout; singular
pattern; four-editions-in-one; JSON/XML out of scope).

Cross-dimension dependency: the *compiler-side* references to this runtime (~60 runtime members) are funnelled
through ONE typed `RuntimeApi` façade in the emitter (`CodeGen/Roslyn/RuntimeApi.cs`, a `nameof`-anchored static
class); the raw-string references still in `CodeGen/Emit/*` migrate onto it incrementally. Several renames/namespace
moves below are cheap because that façade exists; where a reference has not yet migrated onto it, the migration keeps
the emitted surface byte-stable (see Migration).

---

## 1. The pre-P8 problems this design fixed (the rationale record; all remedied by PHASE-08)

The pre-P8 runtime was a flat collection of **static facade classes** whose organization had drifted along three
axes (line references are to the pre-P8 tree):

### 1.1 Run-unit state is process-global ambient statics with an INCONSISTENT threading model (the prime smell)
Run-unit-lifetime state is spread across five unrelated static classes, each with its *own* threading assumption:

| State | Home | Threading model (current) |
|---|---|---|
| Program registry (names, instances, containment) | `Control/ProgramRegistry.cs` (static `Dictionary`/`List`) | plain process-global, "single-threaded in-process" assumed |
| Last-exception status, propagation slots, EC ambient gates | `Exceptions/ExceptionState.cs` (all `static … { get; private set; }`) | plain process-global |
| EXTERNAL data store | `ExceptionState.cs` → `ExternalStore` (static `Dictionary`) | plain process-global |
| `FUNCTION MODULE-NAME` call stack | `Control/CobolModule.cs` | **`[ThreadStatic]`** — the ONE type that is thread-local |
| File connector registries + GC-close queue | `IO/CobolFile.cs` (static `Dictionary` + `ConcurrentQueue`) | single-thread registries + a lock-free finalizer hand-off |
| ACCEPT clock / test seam | `IO/AcceptSource.cs` `public static Func<DateTime> Now { get; set; }` | process-global mutable |

`ProgramRegistry.Reset()` (ProgramRegistry.cs:359) is the de-facto run-unit boundary — it clears `ByPath`/`Order`,
`ExternalStore`, and `CobolModule`. But `ExceptionState`, `CobolFile.Init()`, and `AcceptSource.Now` reset through
*separate* entry points, so "start a clean run unit" has no single owner. Consequences: (a) two run units cannot
coexist in one process (forces the test harness to spawn `dotnet out.dll` per program — see the efficiency critique);
(b) the `[ThreadStatic]` vs plain-static split is a latent bug if a host ever thread-hops; (c) `AcceptSource.Now` and
`ExceptionState.ArgumentFunctionChecking`/`DataConversionChecking` are genuine process-global mutable seams.

### 1.2 The three file organizations duplicate the ISO §9.1.13 status machine + position state
`SequentialFile` (438), `RelativeFile` (487), `IndexedFile` (707) each independently re-implement: the two-character
FILE STATUS field + transition rules; the read-position guard pair `_lastReadUnsuccessful` /
`_prevOpWasSuccessfulRead` (verbatim in all three — SequentialFile.cs:31-32, IndexedFile.cs:47-48); `_mode` /
`_optionalAbsent` open-mode tracking; host-path resolution; and record framing (`KeyedFrames` is shared between
relative/indexed, but sequential re-does length-framing separately, SequentialFile.cs:42-48). The `CobolFile` static
facade then dispatches by *trying the sequential registry first and falling through to a parallel `Keyed*` fan-out*
(`CobolFile.cs:103-108,116,169,172,177` → `KeyedOpen/KeyedClose/KeyedStatus/KeyedLastReadLength/KeyedOpenModeOf`,
whose bodies live at the bottom of `IndexedFile.cs:570-707`). That is a second dispatch mechanism layered on the
type split — a singular-pattern violation.

### 1.3 Powers-of-ten recomputed by an identical multiply-loop in FOUR copies (efficiency critique, MEDIUM)
`CobolNum.Pow10`/`Pow10Wide` (CobolNum.cs:404-418), `CobolDec.Pow10` (CobolDec.cs:272), `CobolDate.Pow10`
(CobolDate.cs:154), `CobolIntrinsics.Pow10D`/`Pow10I` (CobolIntrinsics.cs:43-56) — every one is a
`for` loop recomputing a compile-time-constant table on every numeric store/rescale/format.

### 1.4 Value-library folder taxonomy is arbitrary; namespaces are incoherent
`Text/` holds value types (`CobolString`, `CobolBool`, `CobolClass`) while `Strings/` holds *statement runtime*
(`CobolStringOps` = STRING/UNSTRING, `CobolInspect` = INSPECT) — the split is by word, not by role. Namespaces are
mixed: `CobolIntrinsics`, `ProgramRegistry`, `NumProfile`, `CobolModule`, `ManagedPointer`, `ExternalStore` sit in the
**root** `CobolNet.Runtime`; `CobolNum`/`CobolDec` in `.Numeric` reference-only-by-string from the compiler;
`ExceptionState` in `.Exceptions`; files in `.IO`. There is no `.Control` namespace even though a `Control/` folder
exists. And the assembly is `Cobol.Net.Runtime` while the root namespace is still `CobolNet.Runtime` (a deliberate
G0→G7 deferral so generated `using CobolNet.Runtime;` is unaffected — csproj comment).

### 1.5 Minor API-coherence nicks
`NotImplemented`, `StopRun`, `MethodReturn`, `CobolInvokeArg`, `ResumeSignal`, `CobolFatalException` are one-purpose
control/signal types scattered between `Control/` and `Exceptions/`. `FileStatusCode` (the status constants) lives
inside `FileSupport.cs:74` next to the enums but is consumed by all three connectors.

---

## 2. Target design

### 2.0 Guiding principles
- **ONE owner for run-unit-lifetime state**: a `RunUnit` context object. Everything currently process-global that is
  *conceptually per-run-unit* moves onto it. Ambient access via `RunUnit.Current` (see §2.1) so the emitted surface
  need not thread a parameter.
- **ONE canonical mechanism per job** (singular pattern): one file-connector base + polymorphic dispatch (no
  `Keyed*` shim); one `Pow10` table; one clock; one status machine.
- **Role-based folders, not word-based**: *value library* (elementary/aggregate value operations) vs *verb runtime*
  (statement implementations) vs *IO connectors* vs *control/inter-program* vs *exceptions* vs *intrinsics*.
- **Typed-native invariant preserved**: the only character-image (`string`) boundaries remain the on-disk file edge
  (`FileConnector`), the Tier-B/EXTERNAL `StorageCell`, and the CALL-ABI adapters. No new byte substrate.

### 2.1 The `RunUnit` context (NEW) — the state-ownership + threading fix
```csharp
namespace Cobol.Net.Runtime.Control;

/// The single owner of all run-unit-lifetime state (ISO §14.6.1 run unit). Replaces the five independent
/// process-global static stores. One instance per run unit; the ambient current run unit is an AsyncLocal so a
/// host that thread-hops or runs run units concurrently each see their own (uniform threading model — no more
/// [ThreadStatic]-vs-plain-static split).
public sealed class RunUnit
{
    private static readonly AsyncLocal<RunUnit?> _current = new();
    public static RunUnit Current => _current.Value ?? throw new InvalidOperationException("no active run unit");

    public ProgramTable   Programs   { get; }   // was static ProgramRegistry
    public ExceptionState Exceptions { get; }   // was static ExceptionState
    public ExternalStore  External   { get; }   // was static ExternalStore
    public ModuleStack    Modules    { get; }   // was [ThreadStatic] CobolModule
    public FileRegistry   Files      { get; }   // was static CobolFile registries
    public IClock         Clock      { get; set; } = SystemClock.Instance;   // was AcceptSource.Now

    /// Establish an ambient run unit for the duration of `body` (the generated Main wrapper calls this once).
    public static void Run(Action<RunUnit> body)
    {
        var ru = new RunUnit();
        var prior = _current.Value; _current.Value = ru;
        try { body(ru); }
        finally { ru.Files.CloseAll(); _current.Value = prior; }
    }
}
```
Rationale: `AsyncLocal` (not `ThreadStatic`) because the correct scope is the *logical* run-unit activation, and it
subsumes `CobolModule`'s existing thread-locality while also being correct across `await`/thread-pool hops. Hot
facades cache `RunUnit.Current` in a local at entry to avoid repeated `AsyncLocal` reads.

**Emitted-surface compatibility (battery-green):** the existing static facades stay as *thin delegating shims* over
`RunUnit.Current` during the migration, so generated code (`ProgramRegistry.CallProgram(...)`,
`ExceptionState.Set(...)`, `CobolFile.Open(...)`, `CobolModule.Push(...)`) keeps compiling unchanged:
```csharp
public static class ProgramRegistry {                 // compat shim (deleted at G8 or kept — OPEN QUESTION)
    public static void CallProgram(string n, string c, CobolArg[] a, ManagedPointer? r,
        bool site = false, string ec = "EC-PROGRAM-NOT-FOUND") => RunUnit.Current.Programs.Call(n, c, a, r, site, ec);
    public static void Reset() => /* replaced by RunUnit.Run lifecycle */ ;
}
```

### 2.2 IO — one `FileConnector` base, three organization subclasses, one registry
```csharp
namespace Cobol.Net.Runtime.IO;

/// Shared control logic for every organization (ISO §9.1.13 status machine + §14.9.30/§14.9.35 read-position
/// state + OPEN/CLOSE/mode + host path). Organization-specific record SELECTION is abstract.
public abstract class FileConnector
{
    public string Status { get; protected set; } = FileStatusCode.Success;   // §9.1.13
    protected FileOpenMode Mode;
    protected bool OptionalAbsent, LastReadUnsuccessful, PrevOpWasSuccessfulRead;   // the shared position pair
    protected readonly string HostPath;
    public int  OpenModeView { get; protected set; } = -1;    // §14.9.49 GR6 USE-declarative mode scoping
    public int  LastReadLength { get; protected set; }

    public void Open(FileOpenMode mode);                       // shared preamble → OpenCore
    public void Close();                                       // shared → CloseCore
    protected abstract void OpenCore(FileOpenMode mode);
    protected abstract bool ReadNext(out string image);        // sequential retrieval
    protected abstract void WriteRecord(string image, int length);
    // Keyed subclasses add: ReadByKey / Start / Delete / Rewrite; sequential adds ADVANCING / LINAGE.
}

public sealed class SequentialConnector : FileConnector { … }  // was SequentialFile (incl. line- & record-seq, LINAGE)
public sealed class RelativeConnector   : FileConnector { … }  // was RelativeFile
public sealed class IndexedConnector    : FileConnector { … }  // was IndexedFile (record-list-as-truth design kept)

/// The per-run-unit connector registry + the GC-finalizer deferred-close queue. ONE lookup keyed by COBOL
/// file-name; dispatch is polymorphic on FileConnector — no Sequential-first/Keyed-fallthrough split.
public sealed class FileRegistry { … Register/Open/Close/Read/Write/Status/CloseAll … }
```
The static `CobolFile` facade (kept for the emitted surface) becomes a pure delegator to `RunUnit.Current.Files`.
The `Keyed*` static methods at `IndexedFile.cs:570-707` are **deleted**; their callers in `CobolFile.cs` collapse to a
single `Files[name]` polymorphic call. `RecordFraming` (today `KeyedFrames`) becomes the ONE framing helper used by all
three (sequential varying framing folds into it — same 4-byte LE prefix). File **sharing/locking**
(`CobolFile.Locks.cs` + the `FileSharing`/`FileLockMode` enums) becomes `IO/Sharing/` — a `PhysicalFileTable` owned by
`RunUnit` (today the sharing registry is another static in the Locks partial). `CobolSort`, `ReportWriter`,
`AcceptSource` (renamed `IClock`/`SystemClock`) stay in IO.

### 2.3 Values — the value library (numeric + text + tables), role-grouped
```
Values/                         namespace Cobol.Net.Runtime.Values
  Numeric/  CobolNum, CobolDec, CobolFloat, CobolEdit, CobolRounding, CobolSizeError, NumProfile, Pow10 (NEW)
  Text/     CobolString, CobolBool, CobolClass
  Tables/   CobolTable, CobolDynTable
```
`Pow10` (NEW, `Values/Numeric/Pow10.cs`) — the ONE power-of-ten source:
```csharp
internal static class Pow10 {
    private static readonly long[]   L = BuildLong();   // 10^0..10^18
    private static readonly Int128[] W = BuildWide();   // 10^0..10^38
    public static long   AsLong(int n) => L[n];         // callers already bound n ≤ 18 / ≤ 38 by design
    public static Int128 AsWide(int n) => W[n];
    public static double AsDouble(int n) => n < L.Length ? L[n] : Math.Pow(10, n);
}
```
`CobolNum.Pow10/Pow10Wide`, `CobolDec.Pow10`, `CobolDate.Pow10`, `CobolIntrinsics.Pow10I/Pow10D` all delegate to it,
then are deleted. Pure win, no behavior change (identical values, table-driven).

The numeric value types (`CobolNum` scaled long/Int128 kernel · `CobolDec` decimal128 · `CobolFloat` float/double ·
`CobolEdit` PIC editing) are **coherent as-is** — keep the type set and their internal design; only the folder path and
the `Pow10` dedup change. (The compiler-side "force everything through Int128 even when it fits in `long`" concern is a
*NumericRenderer/emitter* issue, out of scope for this dimension.)

### 2.4 Verbs — the statement runtime (moved out of the mislabeled `Strings/`)
```
Verbs/                          namespace Cobol.Net.Runtime.Verbs
  CobolStringOps  (STRING / UNSTRING)
  CobolInspect    (INSPECT)
```
These implement *statements*, not elementary values — role-grouped away from the `Values/Text/` value types they were
arbitrarily filed beside. (`CobolSort`/`ReportWriter` are verb-runtime too but stay in `IO/` because they are file/
report-connector-bound; noted, not moved.)

### 2.5 Control — run-unit + inter-program
```
Control/                        namespace Cobol.Net.Runtime.Control
  RunUnit (NEW) · ProgramTable (was ProgramRegistry) · ExternalStore · ModuleStack (was CobolModule)
  ManagedPointer / StorageCell / CellPointer · CobolArg / ICobolProgram / CobolArgAdapt
  CobolObject · CobolPtr · ExternalSwitches · CobolInvokeArg
  Signals/  StopRun · ProgramReturn · MethodReturn · ResumeSignal · NotImplemented
```
`ProgramRegistry` → **`ProgramTable`** (an instance owned by `RunUnit`; the name "Registry" is reserved for the
process-level nothing-here). The name-resolution / state-model / CANCEL / sibling-module-probe logic is ported
verbatim onto the instance. `ExternalStore` and `CobolModule`→`ModuleStack` move off statics onto `RunUnit`. The CALL-
ABI types (`ManagedPointer`, `CobolArg`, `ICobolProgram`, `CobolArgAdapt`, `StorageCell`) currently live *inside*
`ProgramRegistry.cs` (700 lines, one file) — split into their own files under `Control/`.

### 2.6 Exceptions — EC engine (instance-owned)
```
Exceptions/                     namespace Cobol.Net.Runtime.Exceptions
  ExceptionState (instance on RunUnit) · ExceptionCatalog (static, immutable table — stays static) · EcFunctions
  Signals moved to Control/Signals/ (CobolFatalException stays here — it is an EC condition carrier)
```
`ExceptionState` becomes an instance (all its `static … { get; private set; }` → instance members; the ambient gates
`ArgumentFunctionChecking`/`DataConversionChecking` become instance flags). `ExceptionCatalog` is an immutable lookup
table — legitimately static, kept. `EcFunctions` reads `RunUnit.Current.Exceptions`.

### 2.7 Intrinsics — keep the family split; only the `Pow10` dedup + clock change
`Intrinsics/CobolIntrinsics.{cs,Exact,Float,Text}` + `CobolDate` are cohesive (the deep-dive's runtime home). No
reorg beyond: `Pow10*` → `Values/Numeric/Pow10`; `CobolDate`'s `AcceptSource.Now` dependency → `RunUnit.Current.Clock`.

### 2.8 Namespace / assembly rename (G8)
Assembly is already `Cobol.Net.Runtime`. At G8, `RootNamespace` `CobolNet.Runtime` → **`Cobol.Net.Runtime`** and the
sub-namespaces above become real. This is a single coordinated change with the compiler's `RuntimeApi` façade (one
file emits the `using`s), so generated `using CobolNet.Runtime;` flips to `using Cobol.Net.Runtime.*;` in exactly one
place. Until G8 the root namespace stays `CobolNet.Runtime` and the reorg below is **folder-only** (namespaces
unchanged) to keep the emitted surface byte-stable.

---

## 3. Current → target module changes

| Action | From | To | Why |
|---|---|---|---|
| create | — | `Control/RunUnit.cs` | ONE owner of run-unit state; uniform `AsyncLocal` threading; single lifecycle boundary |
| create | — | `Values/Numeric/Pow10.cs` | ONE table-driven power-of-ten; kills 4 recompute-loop copies (efficiency MEDIUM) |
| create | — | `IO/FileConnector.cs` | shared §9.1.13 status machine + read-position state for all three organizations |
| create | — | `IO/FileRegistry.cs` | one polymorphic connector registry; deletes the `Keyed*` fallthrough dispatch |
| split | `Control/ProgramRegistry.cs` (700, holds registry + ManagedPointer + CobolArg + ICobolProgram + CobolArgAdapt + StorageCell + ExternalStore) | `Control/ProgramTable.cs`, `Control/ManagedPointer.cs`, `Control/CallAbi.cs` (CobolArg/ICobolProgram/CobolArgAdapt), `Control/StorageCell.cs`, `Control/ExternalStore.cs` | one concern per file; the ABI types are not "registry" |
| rename+refactor | `ProgramRegistry` (static) | `ProgramTable` (instance on `RunUnit`) | state ownership; enables concurrent run units; a static shim keeps the emitted surface |
| rename+refactor | `Exceptions/ExceptionState` (static) | `ExceptionState` (instance on `RunUnit`) | state ownership; ambient EC gates become run-unit-scoped |
| rename+refactor | `Control/CobolModule` (`[ThreadStatic]`) | `Control/ModuleStack` (instance on `RunUnit`) | remove the lone `[ThreadStatic]`; uniform threading |
| move+refactor | `ExternalStore` (static, in ProgramRegistry.cs) | `Control/ExternalStore.cs` (instance on `RunUnit`) | state ownership; own file |
| rename | `SequentialFile`/`RelativeFile`/`IndexedFile` | `SequentialConnector`/`RelativeConnector`/`IndexedConnector` (: `FileConnector`) | dedup the status/position machinery into the base |
| delete | `IndexedFile.cs:570-707` `Keyed*` static methods; `CobolFile` sequential-first/keyed-fallthrough | (polymorphic `FileRegistry` dispatch) | remove the second dispatch mechanism (singular pattern) |
| rename+move | `IO/AcceptSource` `static Func<DateTime> Now` | `IO/Clock.cs` `IClock`/`SystemClock`, on `RunUnit.Clock` | remove process-global test seam; injectable per run unit |
| move | `IO/CobolFile.Locks.cs` sharing registry (static) | `IO/Sharing/PhysicalFileTable.cs` (on `RunUnit`) | state ownership; cohesive sharing subsystem |
| move | `Strings/CobolStringOps.cs`, `Strings/CobolInspect.cs` | `Verbs/` | role-based: statement runtime ≠ value types; delete empty `Strings/` |
| move | `Numeric/*`, `Text/*`, `Tables/*` | `Values/Numeric/`, `Values/Text/`, `Values/Tables/` | one value-library home (folder-only pre-G8) |
| move | `FileStatusCode` (in `FileSupport.cs`) | `IO/FileStatus.cs` | consumed by all connectors; own file next to the base |
| merge | 4× `Pow10`/`Pow10Wide`/`Pow10I`/`Pow10D` | delegate to `Values/Numeric/Pow10` | one source of truth |
| merge | `KeyedFrames` (relative/indexed) + sequential varying framing | `IO/RecordFraming.cs` | one framing helper across organizations |
| move | `StopRun`, `MethodReturn`, `ProgramReturn`, `ResumeSignal`, `NotImplemented` | `Control/Signals/` | cohesive control-signal group |
| rename (G8) | RootNamespace `CobolNet.Runtime` | `Cobol.Net.Runtime` (+ real sub-namespaces) | assembly/namespace coherence; one coordinated flip with the emitter `RuntimeApi` façade |
| keep | `CobolNum`/`CobolDec`/`CobolFloat`/`CobolEdit`/`NumProfile`, `ExceptionCatalog`, `CobolIntrinsics.*`, `CobolDate` (type designs) | — | already coherent; only path/`Pow10`/clock touched |

---

## 4. Migration (keeping the 3256 conformance + 281 unit + NIST-353 battery green) — steps 1–5 EXECUTED (P8); step 6 = G8

Order the work so each step is independently green and the emitted-code surface is byte-stable until G8.
Steps 1–5 below are DONE (PHASE-08; battery green at every commit; zero compiler-side changes, so the emitted
C# is byte-identical by construction). Step 6 remains for G8. Additions beyond the plan, per the §5 hidden-
static gate: `ExternalSwitches` → instance `SwitchStore` on `RunUnit` (+ static shim) — switch scope is the run
unit (§12.3.7 GR4 NOTE 1); the only statics left are genuinely immutable (`ExceptionCatalog`, `Pow10`,
`SystemClock.Instance`). Naming as landed: the instance types are `ProgramTable`, `ExceptionEngine`,
`ExternalTable`, `ModuleStack`, `SwitchStore`, `FileRegistry` (+ `PhysicalFileTable` under `IO/Sharing/`);
every pre-P8 static name survives as the emitted-surface shim.

1. **`Pow10` dedup (safe, isolated).** Add `Values/Numeric/Pow10.cs`; repoint the 4 copies; delete them. Pure value
   identity — run the numeric unit tests + full conformance. No emitted-surface change.
2. **Folder moves, namespaces unchanged.** Physically move files into `Values/`, `Verbs/`, split
   `ProgramRegistry.cs` into per-type files, group `Control/Signals/`. Namespaces stay `CobolNet.Runtime[.X]` exactly
   as today, so nothing the compiler emits changes. Green by construction (rename/move only).
3. **`FileConnector` base extraction.** Introduce the abstract base + `FileRegistry`; make
   `Sequential/Relative/Indexed` derive from it, hoisting ONLY the provably-identical status/position/mode/host-path
   members. Keep organization-specific rules in overrides. Replace the `Keyed*` fallthrough in `CobolFile` with a
   polymorphic `Files[name]` call; delete the `Keyed*` shims. Guard: the RL/IX/SQ/relative NIST goldens + file unit
   tests are the exact regression net — run after each connector is migrated (one organization at a time).
4. **Introduce `RunUnit`; keep static facades as shims.** Create `RunUnit` owning instance `ProgramTable` /
   `ExceptionState` / `ExternalStore` / `ModuleStack` / `FileRegistry` / `Clock`. Convert the five static classes to
   thin delegators over `RunUnit.Current`. The generated `Main` wrapper's run-unit entry (today
   `ProgramRegistry.Reset(); … ; CobolFile.CloseAll()`) is replaced by `RunUnit.Run(ru => { … })` — a *one-line
   emitter change* in the run-unit driver, or (to defer any emitter change) have the first `ProgramRegistry.Reset()`
   lazily establish an ambient `RunUnit` and `CloseAll()` tear it down. Verify: the exact `Reset()` semantics
   (clears programs + external + module stack + files) must be reproduced by `RunUnit.Run`'s begin/end — assert with
   an inter-program (CALL/CANCEL/EXTERNAL) golden subset first, then full battery.
5. **Clock injection.** Replace `AcceptSource.Now` with `RunUnit.Clock`; the test seam that set `AcceptSource.Now`
   sets `ru.Clock` instead (update the ~handful of date/time conformance fixtures' harness hook). Green when the
   date/time goldens pass.
6. **G8 namespace flip.** Once the compiler routes runtime references through a single `RuntimeApi` façade, flip
   `RootNamespace` to `Cobol.Net.Runtime` and realize the sub-namespaces; update the façade's emitted `using`s in one
   place. Full battery + a from-clean regen. Decide static-shim retirement here (OPEN QUESTION).

Each step is a commit with its own DEVLOG entry and a guard-fast/greenfield-suite run (per the process rules).

---

## 5. Risks

- **`static → RunUnit` ambient is a broad change.** Mitigated by the delegating-shim strategy (emitted surface
  unchanged) and by reproducing `ProgramRegistry.Reset()` semantics *exactly* in `RunUnit.Run`. The
  inter-program/EXTERNAL goldens (IC-series, IC227A EXTERNAL-connector persistence) are the sharp regression edge.
- **`AsyncLocal` per-access cost.** Small but nonzero; hot facades (`CobolFile.*`, `ExceptionState` raise sites) cache
  `RunUnit.Current` in a local. Measure against the 20M-iteration arithmetic-loop benchmark used in the efficiency
  critique — the numeric hot path does not touch `RunUnit`, so the risk is confined to I/O/CALL/raise sites.
- **`FileConnector` base could paper over an intentional per-organization divergence.** The base must be the *true*
  common denominator; anything an organization tunes independently (e.g. sequential REWRITE-requires-prior-READ 43 vs
  indexed key rules) stays an override. Migrate one organization at a time behind its NIST goldens; do not "unify"
  a status transition that the three do differently.
- **Concurrent in-process run units expose latent shared statics** not yet moved (`ExceptionCatalog` is immutable so
  safe; verify no other hidden mutable static remains — a grep gate in CI). Until every run-unit static is on
  `RunUnit`, concurrency stays opt-in/off.
- **Namespace flip breaks generated code if not funnelled.** Strictly gated on the `RuntimeApi` façade existing;
  otherwise it is a corpus-wide emitted-`using` churn. Keep it as the last (G8) step.

---

## 6. Formerly-open questions — RESOLVED (as executed by PHASE-08)

1. **Concurrent in-process run units:** adopted for state-ownership HYGIENE; "one run unit per process" stays
   the supported contract. The `RunUnit`/`AsyncLocal` design enables concurrency later (the harness-throughput
   win), but it is opt-in/untested until every consumer is audited — no capability claim is made.
2. **Ambient mechanism:** `AsyncLocal<RunUnit>` (the recommendation), with a LAZY `Current` so the unchanged
   emitted driver (`ProgramRegistry.Reset(); …`) establishes the ambient unit implicitly. Threading `RunUnit`
   through the `ICobolProgram` ABI was rejected — it would change every generated entry point pre-G8.
3. **Static-facade lifetime:** kept pre-G8 as the emitted surface (byte-stability); the retire-vs-keep decision
   is G8 Cut 3's (with the namespace flip, where the `RuntimeApi` façade centralizes whichever spelling wins).
4. **Namespace granularity:** deferred to G8 with the flip itself (§2.8's sub-namespaced layout remains the
   plan of record; the folders already mirror it).
5. **`Values/` nesting:** ACCEPTED — `Values/{Numeric,Text,Tables}/` landed.
6. **Sequential connector split:** KEPT UNIFIED (line- + record-sequential + LINAGE in one
   `SequentialConnector`) — the shared position/framing state makes a split net-negative.
