# DESIGN — Target Runtime Library Organization (`WiseOwl.Cobol.Runtime`)

Status: EXECUTED (PHASE-08) — §2's target design IS the as-built runtime; §4's migration steps 1–5 are DONE
(plus the `ExternalSwitches`→`SwitchStore` conversion the §5 hidden-static gate surfaced); step 6 (the G8
RootNamespace flip + real sub-namespaces) is the ONE remaining item, deferred to G8 Cut 3 behind the compiler's
`RuntimeApi` façade. §1 records the pre-P8 problems as the rationale; §6's former open questions are resolved
inline. Scope: the organization + API of the greenfield runtime `src/WiseOwl.Cobol.Runtime` that WiseOwl COBOL-generated
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
**root** `WiseOwl.Cobol.Runtime`; `CobolNum`/`CobolDec` in `.Numeric` reference-only-by-string from the compiler;
`ExceptionState` in `.Exceptions`; files in `.IO`. There is no `.Control` namespace even though a `Control/` folder
exists. And the assembly is `WiseOwl.Cobol.Runtime` while the root namespace is still `WiseOwl.Cobol.Runtime` (a deliberate
G0→G7 deferral so generated `using WiseOwl.Cobol.Runtime;` is unaffected — csproj comment).

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
namespace WiseOwl.Cobol.Runtime.Control;

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
    public RandomSequence Random     { get; }   // was static CobolIntrinsics._random (kb/Work PB307, §15.75.3 r4)
    public SortFileTable  SortFiles  { get; }   // was static CobolSort.Files (kb/Work PB1570, §14.6.1 + §14.9.40.4 GR10)

    /// A host's run unit for the duration of `body`; the generated Main begins its own with RunUnit.Begin().
    public static void Run(Action<RunUnit> body)
    {
        var prior = _current.Value;
        var ru = StartAfter(prior);   // a NEW object; only HostConfiguration carries over
        _current.Value = ru;
        try { body(ru); }
        finally { ru.Terminate(); _current.Value = prior; }   // the ONE §14.6.11 epilogue (RunMain calls it too)
    }
}
```
**A new run unit is a new `RunUnit` object — on every path.** `RunUnit.Begin()` (the emitted driver's
`ProgramRegistry.Reset()`) and `RunUnit.Run` both construct one, so every member — program table, EXTERNAL store,
MODULE-NAME stack, files, switches, locale, RANDOM sequence, sort-merge file stores, report flow, exception status, the per-class FACTORY
OBJECTS (`RunUnit.FactoryObject<F>()`, ISO §9.3.14.2: created before the first reference "by a run unit") and the
termination status — is fresh by construction. Only the declared HOST CONFIGURATION (`RunUnit.HostConfiguration`:
the clock seam and the object-time debug switch) is carried from the ambient run unit being replaced. The earlier
shape reset a HAND LIST of members on the one ambient object, and the switch, locale and report-flow state (and the
process-static factory singletons) survived into the next run unit (kb/Work PB1069);
`RunUnitStateDriftTests.EveryMember_IsFreshInTheNextRunUnit` now reflects over every field instead of a list.

**Static storage is ADOPTED by the run unit (kb/Work PB1069).** The one run-unit state a new object cannot make fresh
is STATIC data on a generated type: a RECURSIVE program's or a FUNCTION-ID's WORKING-STORAGE (§13.5.4 GR1) and a
class's METHOD WORKING-STORAGE (OO deep-dive D3). Each such unit emits `__ResetStatics`, and the run unit adopts it
(`RunUnit.AdoptStaticStorage`, once per reset): a program at `ProgramTable.Register`, a class when the run unit
creates its factory object (`RunUnit.FactoryObject` → the generated override of `CobolObject.__AdoptRunUnitStorage`,
which adopts both halves' resets and calls base, so every superclass adopts its own). Adoption runs the reset —
§14.6.2.3.2 1), the initial state at the first activation in a run unit — and `RunUnit.Terminate`, the ONE
termination epilogue `ProgramTable.RunMain` and `RunUnit.Run` both call, closes every file (§14.6.11 2) and runs every
adopted reset again, so a static POINTER, OBJECT REFERENCE or dynamic item releases what it held (§14.6.11 3, 4, 6).

**No run-unit state in a static — enforced.** `RunUnitStateDriftTests.NoWritableStatic_OutsideTheDocumentedProcessStores`
enumerates every writable static field in `WiseOwl.Cobol.Runtime` and fails on one not documented there as
PROCESS-lifetime (console encoding, collation-subsystem configuration, the per-thread overwritten-before-use
subscript scratch cell). The RANDOM sequence was the sixth process-global store the consolidation missed (kb/Work
PB307).

**The run unit's thread and the activation resource check (kb/Work PB2659).** Every COBOL activation — a CALL, a
user-defined function reference, an INVOKE or inline method invocation — is a chain of .NET frames on the thread that
runs the run unit, so the depth a RECURSIVE program reaches (§8.6.6) is that thread's stack. `Control/ActivationStack`
owns both halves of the rule:

- **The thread.** `ProgramTable.RunMain` runs the main program and the §14.6.11 epilogue on a thread of its own
  (`ActivationStack.RunOnRunUnitThread`) whose stack is `RunUnitThreadStackBytes` = 384 MiB of reserved address
  space, committed only as deep as the run unit goes. The thread is transparent: the ambient `RunUnit` flows to it
  with the execution context, it takes the caller's culture, and whatever escapes it (the `StopRun` unwind the
  generated `Main` catches, or a defect) is rethrown on the calling thread. Before it, the main program ran on the
  host's default stack (about 1 MB for a Windows main thread) and a self-CALLing program died of a CLR stack overflow at a
  depth of about 250 — no exception condition, no §14.6.11 CLOSE.
- **The check.** §14.9.4.4 GR3c, §8.4.3.2.4 GR6c and §14.9.23.4 GR7b give "the resources necessary to execute the
  program are not available" a defined outcome, and Annex A.1 items 14, 89 and 102 let the implementor say which
  resources are checked. The one checked here is the stack: `ActivationStack.IsAvailable` asks the .NET runtime's own
  margin (`RuntimeHelpers.TryEnsureSufficientExecutionStack`, 128 KB on 64-bit, valid on any thread) and, on the run
  unit's own thread, a larger `ActivationReserveBytes` (1 MiB) measured from the thread's start, so the deepest
  activation keeps room for its own statements and for the handler of the activation that fails. `ProgramTable.CallProgram`
  asks it first (before GR3d–f; a CALL and a function activation raise EC-PROGRAM-RESOURCES through the CALL
  carrier), and the emitted head of every method body asks it through `ActivationStack.RequireForMethod`
  (EC-OO-METHOD, before GR7 d). A PROPERTY clause's synthesized accessor executes no statement and is not checked.
- **No catch-and-rethrow on the activation path.** A catch block runs on top of the stack the exception was thrown
  from, so a `throw;` inside it starts a nested dispatch there. The GR3i boundary mark in `CallProgram` was such a
  rethrow, and a condition crossing N boundaries stacked N dispatches: the EC-PROGRAM-RESOURCES of an exhausted
  recursion overflowed the stack again while unwinding. The mark is an exception FILTER (`MarkBoundaryCrossing`),
  which runs in the dispatch's first pass in the same innermost-first order and declines the condition.

The size is chosen from measurement, not guessed. The target is the depth GnuCOBOL 3.2 reaches on its default 8 MB
stack with a minimal self-CALLing program — 74,687 activations, after which it dies of SIGSEGV with no message —
reached by the same program under its most expensive frame shape here. Measured per activation (deep recursion runs
mostly in the JIT's first-tier frames) and the depth at 384 MiB:

| program | KB per activation | depth at 384 MiB (Windows) | (Linux) |
|---|---|---|---|
| minimal RECURSIVE self-CALL | 1.5 | 256,126 | 143,146 |
| the same under `>>TURN EC-ALL CHECKING ON` | 3.9 | 104,585 | 92,137 |
| twenty statements and LOCAL-STORAGE | 2.5 | 156,190 | 139,121 |
| the same under `>>TURN EC-ALL CHECKING ON` | 12.4 | 32,566 | 66,029 |

(The Linux column ran the IL runtime without ReadyToRun code under WSL; fully optimized code — tiered compilation
off — costs 1.2 to 4.3 KB per activation for the same four programs.) A recursion through PERFORM is not an
activation and is not checked: §14.9.28.4 GR2 makes it undefined, and its NOTE 1 names the stack overflow.

Rationale: `AsyncLocal` (not `ThreadStatic`) because the correct scope is the *logical* run-unit activation, and it
subsumes `CobolModule`'s existing thread-locality while also being correct across `await`/thread-pool hops. Hot
facades cache `RunUnit.Current` in a local at entry to avoid repeated `AsyncLocal` reads.

**Emitted-surface compatibility (battery-green):** the existing static facades stay as *thin delegating shims* over
`RunUnit.Current` during the migration, so generated code (`ProgramRegistry.CallProgram(...)`,
`ExceptionState.Set(...)`, `CobolFile.Open(...)`, `CobolModule.Name(...)`) keeps compiling unchanged:
```csharp
public static class ProgramRegistry {                 // compat shim (deleted at G8 or kept — OPEN QUESTION)
    public static void CallProgram(string n, string c, CobolArg[] a, ManagedPointer? r,
        bool site = false, string ec = "EC-PROGRAM-NOT-FOUND") => RunUnit.Current.Programs.Call(n, c, a, r, site, ec);
    public static void Reset() => /* replaced by RunUnit.Run lifecycle */ ;
}
```

### 2.2 IO — one `FileConnector` base, three organization subclasses, one registry
```csharp
namespace WiseOwl.Cobol.Runtime.IO;

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

    public string Open(FileOpenMode mode);                     // shared preamble → OpenCore; returns the status
    public string Close();                                     // shared → CloseCore
    // The READ preconditions, written ONCE for every organization (kb/Work PB336). Order IS the rule inside
    // SequentialReadGuard: §14.9.30.4 GR2 '47', then GR21's '46' poison, then §9.1.13.4 1c's '10' — which is
    // "first time" only BECAUSE its own arm arms the poison. Random side: §9.1.13.5 3b's '23', never '46'.
    // The guards are PURE. A READ or START entry never assigns Status itself: a failure returns through
    // Unsuccessful, which applies §14.9.30.4 GR18 / §14.9.41.4 GR7 through the virtual InvalidateFilePosition
    // (kb/Work PB359); a success through ReadSucceeded or StartPositioned.
    protected string? ReadOpenModeGuard();
    protected string? SequentialReadGuard();
    protected string? RandomReadAbsentOptionalGuard();
    protected string? StartOpenModeGuard();
    protected string Unsuccessful(string status);             // InvalidateFilePosition, then the status
    protected string ReadSucceeded(string status);            // re-arms the '43' gate, clears the '46' poison
    protected string StartPositioned();                       // '00', clears the '46' poison
    protected virtual void InvalidateFilePosition();          // keyed connectors also clear their FPI validity bit
    protected abstract string OpenCore(FileOpenMode mode, FilePresence presence);   // takes the ONE presence probe
    //   (Open also answers GR3/'37', GR10/'39' and GR16's write capability/'37' before OpenCore is reached)
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
**One presence probe, three answers (`HostFile.Probe` → `FilePresence`, in `IO/FileSupport.cs`).** "Is the
physical file there?" is asked in exactly ONE place in the runtime, and its answer has **three** states —
`Absent` · `Present` · `Unauthorized` — because the standard prices two of them differently: §9.1.13.6 item 5
sets '35' when *"the physical file is not present"*, while §14.9.27.4 GR3 sets '37' when *"the file … is present
and insufficient authority exists to open the file"*. `File.Exists` cannot express that — it swallows every
access error and answers `false` — so it is **banned** everywhere under `Runtime/IO` (drift-guarded by
`HostFileProbeDriftTests`, which also bans `FileInfo.Exists` and the raw `File.GetAttributes` the probe is
built from). `FileConnector.Open` takes the probe once per OPEN, answers GR3 there and hands the result to
`OpenCore(mode, presence)`, so an organization body sees only the two-state Table-18 question it is entitled to
and no two probes in one statement can disagree; `FileRegistry.DeleteFile` takes the same probe for §14.9.10.4
GR14/GR16. GR3's short-circuit deliberately **excludes OUTPUT**: GR18 makes OUTPUT a creation that never
consults presence, and a directory the process may write but not list legitimately accepts a new file, so an
OUTPUT authority failure comes from the creating stream and the shared `catch` instead. (kb/Work PB323 — the
OPEN arm of the sweep PB140 had done only for DELETE FILE.)

**One write-capability probe, asked in the OPEN contract (`HostFile.PermitsWrite`, same file).** Presence is not
capability: a read-only file is `Present` and perfectly observable, and §14.9.27.4 GR16 still requires '37' —
*"If the I-O phrase is specified, the file shall support the input and output statements that are permitted for
the organization of that file when opened in the I-O mode"* — as does §9.1.13.6 item 6 a) 1. for EXTEND and
OUTPUT. **Neither rule names an organization**, and Table 20 is why they cannot: REWRITE sits under the I-O
column for sequential, random *and* dynamic access. So `FileConnector.Open` asks the question ONCE, for
I-O/EXTEND/OUTPUT on a `Present` file, immediately before `OpenCore` — the second half of the same contract
that already owns GR3 and GR10 — and an organization added later inherits the answer instead of re-earning it.
The probe is a real write open (`FileMode.Open` + `FileAccess.Write` + `FileShare.ReadWrite`, nothing written),
because a read-only attribute, a Unix mode bit and a deny-write ACE are three mechanisms with one consequence
and only the host can rank them; opening without writing leaves content and last-write time untouched, which is
what GR25 (*"the file is not affected"*) requires of the open that is about to fail. Only
`UnauthorizedAccessException` is an answer — every other `IOException` propagates to `FileConnector.Open`'s
ONE catch, which asks the host ONE question first: `HostFile.IsSharingRefusal` (Windows
`ERROR_SHARING_VIOLATION`/`ERROR_LOCK_VIOLATION`, Unix `EWOULDBLOCK` from .NET's `flock`). A sharing refusal is
§9.1.13.9 1)'s '61' — another run unit's connector holds the file, and the host said so — and only the residue
is §9.1.13.6 1)'s '30', *"no further information is available"* (kb/Work PB860: every organization answered
'30'). DELETE FILE asks the same question of the host before it deletes (`HostFile.IsHeldByAnother`, an
exclusive read request, inside the RETRY loop) and answers §9.1.13.9 2)'s '62', because a Unix unlink never
consults open handles and would otherwise destroy a file another run unit holds open. INPUT is excluded and the exclusion is load-bearing: a read-only file supports everything
Table 20 permits in the input mode, and item 6 a) 3.'s read-capability question is already answered eagerly by
the sequential reader and the keyed `Attach()`. `HostFileProbeDriftTests` bans `PermitsWrite` outside
`FileConnector` and pins the call count at one. (kb/Work PB328 — the sequential arm asked the question by
accident, since its I-O and EXTEND streams *are* write opens, while the relative and indexed arms only read
their store: the same read-only file answered '37' sequentially and '00' keyed, then '00' for READ and REWRITE,
and the loss surfaced as a '30' at CLOSE on a byte-identical file. The two-arm dispatch again, one arm fixed.)

**One host-path OPEN, five named roles (`HostFile.OpenConnectorStream` / `HostFile.OpenConnectorWriteStream` / `HostFile.OpenConnectorStore` / `HostFile.OpenAuxiliary` / `HostFile.CreateSortWork`, same file).**
The third question `HostFile` owns is *"what may every OTHER handle on this physical file do while this one is
open?"*, and it is not a per-call-site decision: §9.1.15 puts the in-run-unit gate on the file connectors —
*"Before access to a shared physical file is allowed through an OPEN statement, the sharing mode and the open
mode of that OPEN statement shall be allowed by all other file connectors that are currently associated with
the physical file"*, arbitrated by §14.9.27.4's Table 19 — never on the operating environment's handle.
(⛔ That sentence is §9.1.15's, not §14.9.27.4's: this paragraph carried the wrong clause number from the
PB713 landing until PB740's, an INHERITED citation, `cite.py --check`ed on both spellings.) The share mode is
therefore SPENT here and DERIVED in `FileLockPosture` (below).
`OpenConnectorStream(path, mode, access, share, options)` is a sequential connector's own long-lived read or
read-write stream, and `OpenConnectorWriteStream(path, mode, share)` its write stream; BOTH are UNBUFFERED in every
posture, because the connector above keeps exactly one buffer — its `StreamReader`'s or `StreamWriter`'s — and can
invalidate or release only the one it owns (kb/Work PB753, below). **That one buffer IS the connector's input-output
areas** (ISO §12.4.5.14.3 GR1, kb/Work PB643): `FileConnector.InputOutputAreas` (the RESERVE clause's integer-1,
registered by `CobolFile.RegisterReserve`, else `HostFile.ImplementorInputOutputAreas` = 1) × `HostFile.InputOutputAreaBytes`
(4,096), sized in ONE place, `HostFile.InputOutputAreaBuffer`. A connector whose file lock admits no
other connector (`SHARING WITH NO OTHER`) releases its records to the medium as its areas fill; one whose file lock
admits a reader or a writer (`AdmitsAnotherRunUnit`: READ ONLY or ALL OTHER) releases at every WRITE / REWRITE
(`SequentialConnector.ReleaseRecord`, kb/Work PB1587: GR12 puts the record in the physical file at the statement, and
§9.1.15 2) admits the INPUT connector beside a READ ONLY writer), so the posture decides WHEN the areas are emptied,
never WHETHER they exist (before PB643 the areas were the host handle's 4,096-byte buffer in one posture and the reader's default buffer
in the other, so RESERVE had no one place to land). `OpenConnectorStore(path, mode, access, share)` is a
RELATIVE or INDEXED connector's long-lived store handle (kb/Work PB771, below), UNBUFFERED: the store is read and
written positionally, one 4,096-byte page per call (kb/Work PB2660), and every page passes through the run unit's
page cache for the physical file, whose capacity the OPEN that attaches the connector raises to at least the
connector's areas, one page each (`KeyedConnector.Attach` → `KeyedStore.AttachHandle`, 2.2.1.13; docs/CONFORMANCE.md
DOC-A.1-164). `OpenAuxiliary(path, mode, access)` is a short-lived
bookkeeping handle over a path a connector may already hold — the shared `OPEN EXTEND` write-base measurement,
the §14.9.27.4 GR10 store-header read and the varying framing's parse check — and is **always**
`FileShare.ReadWrite`, because a handle's share mode has to admit the access every outstanding handle already
holds or the host refuses it. Every one of those runs BEFORE the connector's own handle for that OPEN exists,
which is what makes the permissiveness free: this role is never the handle a COBOL statement is served
through and never stands in for a file lock. **A keyed store's pages are not on this list** (kb/Work PB771): they
travel only through the connector's own `OpenConnectorStore` handle, one page per positional call (2.2.1.8). `SharedExtendOpenDriftTests` bans every other host-path open under `Runtime/IO`
and pins both roles to this file. (kb/Work PB713 — a sharing-active `OPEN EXTEND` measured its write-ordinal
base from a SECOND handle on the path it had just opened for WRITE: `File.ReadLines` for the line-sequential
framing, a three-argument `FileStream` for the varying one, both `FileShare.Read`, both refused. The refusal ran
from `FileRegistry.SharedOpenAttempt` — *after* `FileConnector.Open` returned, outside its try — so it left the
run unit as an unhandled `IOException` naming "another process" that was the program itself, where §14.9.27.4
GR1 admits only an I-O status. Fixed the way GR3 and GR16 were: the measurement moved INTO
`SequentialConnector.OpenCore`, before the writer exists, where the '30' mapping already lives, and the
duplicate `HostFile.Probe` it carried against PB323's one-probe rule went with it. The fixed-width framing read
`FileInfo.Length`, took no handle, and passed — one dispatch, three arms, one arm tested.)

**The third role, and the rule it serves: a release is positioned and numbered from the SHARED MEDIUM, at the
moment of the release.** `OpenConnectorWriteStream(path, mode, share)` is a connector's own write handle — `Create`
for `OPEN OUTPUT`, `Append` for `OPEN EXTEND` — and it exists because
.NET has no atomic append: `FileMode.Append` seeks to the end ONCE, at open, and every later write goes at that
stream's own position. For the connector holding the only writable handle there is that is right, and it keeps the
plain handle byte for byte. When the posture admits ANOTHER WRITER it is wrong, and silently: ISO §14.9.51.4 GR19 says of two
connectors extending one shared file that *"the added records follow the records present in the physical file
when it was opened, but are otherwise in an undefined order"* — only their ORDER is undefined — and both
connectors anchored at the same offset, so the later flush wrote over the earlier record while both WRITEs
reported '00'. Such a connector therefore gets `SharedAppendStream`, which repositions at the physical end before
every write, over an UNBUFFERED handle; and `SequentialConnector.ReleaseRecord` — the ONE place a sequential
record is released, reached by all three write arms — flushes it there, because GR12 (*"The successful execution
of a WRITE statement releases a logical record to the operating environment"*) is what makes the '00' a promise
and because a record still in this connector's buffer is one the other connector cannot see, count, or avoid
overwriting. **The predicate is the POSTURE, not a clause** (kb/Work PB740): a handle whose §9.1.15 file lock
admits a writer takes the repositioning stream and flushes each release, which since kb/Work PB322 means a
connector declared `SHARING WITH ALL OTHER` (the only mode Table 19 admits a writing sibling beside); and a handle
whose lock admits a READER (`SHARING WITH READ ONLY` too, kb/Work PB1587) flushes each release over its plain stream.
Keying it on
"this SELECT wrote a clause" would have left a pair that shares a file without either having written one —
writing through two independent buffers.

The same sentence settles the record's IDENTITY, which is the half that shows up in §9.1.16 (*"While locked by a
given file connector, a record is not accessible to another file connector in the same or a different run
unit"*). The ordinal is minted from `PhysicalFileTable.State.ReleasedOrdinal` — one mint per physical file,
seeded by a shared `OPEN EXTEND` from the records already there and reset by a shared `OPEN OUTPUT` — rather than
from a per-connector base plus a per-connector count, which had both connectors calling their first appended
record ordinal 2. `FileConnector.Physical` is how a connector reaches that state and EVERY connector holds it —
Table 19 arbitrates every open over it, and since PB740 two clause-less connectors may share one physical file,
so a rule about the MEDIUM cannot be gated on a clause. `SharedPhysical` is the §9.1.16 RECORD-LOCKING view of
it, non-null only for a connector whose current OPEN can SET record locks (`FileRegistry.RecordLocksSetUnder`: a
LOCK MODE clause under a sharing mode other than NO OTHER, §12.4.5.9.4 GR1 and GR3), because the ordinal *is* the
lock identity: a SHARING clause alone (GR1 a), a SHARING or RETRY phrase on the OPEN (GR1 b) 1.) and a clause-less
connector (GR1 b) 2., this compiler's determination: none) set no record lock and so have none to identify
(kb/Work PB3372). The same predicate answers `LocksEffective` at every I-O statement. Both are written by ONE call, `AssociatePhysical`, immediately before the
OPEN body runs — the boolean that used to sit beside the state is still gone, and the view is derived rather
than stored (kb/Work PB740 removed the conflation with the OS share mode; PB753 split the medium question from
the locking one without adding a second answer).

**The §9.1.15 FILE LOCK is derived, in one place: `FileLockPosture` (`IO/Sharing/`), applied by
`FileRegistry.SharedOpenAttempt`.** §9.1.15 addresses two audiences with one sharing mode and they need two
mechanisms. Inside the run unit — *"Multiple paths of access may exist in the same runtime element, contained
elements, separate runtime elements within the same run unit, or runtime elements in different run units"* — the
gate is Table 19, already run; outside it, *"The successful opening of a file establishes a file lock for the
applicable sharing rules, thereby preventing other run units from opening that file with incompatible sharing
rules"*, and the host's share mode is that lock. It cannot be both, because a share mode names no requester: it
cannot admit this run unit's second connector while refusing a foreign process. So:

- the BASE is §9.1.15's three rules read literally — `NO OTHER` ⇒ `FileShare.None` (*"exclusive access"*),
  `READ ONLY` ⇒ `FileShare.Read` (*"restricts concurrent access … to input mode"*), `ALL OTHER` ⇒
  `FileShare.ReadWrite` (*"allows concurrent access … specifying input, I-O, or extend mode"*);
- a connector with no SHARING specification holds one of those three modes too, given by its open mode
  (`FileRegistry.ImplementorDefaultSharing`, kb/Work PB322, Annex A.1 items 77 and 131): `OPEN INPUT` is `READ
  ONLY` (⇒ `FileShare.Read`) and every other mode is `NO OTHER` (⇒ `FileShare.None`), so a clause-less updater
  protects its physical file from other PROCESSES exactly as `SHARING WITH NO OTHER` does;
- the registry hands the posture down ONCE per OPEN, ahead of the handle (`c.HostSharing = sharing`; `HostShare` is derived from it
  in `SharedOpenAttempt`), and nothing re-derives it while the connector is open. **EVERY organization holds a
  long-lived host handle carrying its posture** — §9.1.15 3) names none, so all three owe the lock (kb/Work
  PB771). `KeyedConnector` is where RELATIVE and INDEXED hold theirs: `TakeFileLock` on each `OpenCore` arm that
  has a physical file and `ReleaseFileLock` in the CLOSE and on an unsuccessful OPEN. ⛔ **It is the SAME handle
  the store travels through** — the keyed store engine's pages are read and written positionally through it
  (`Store.RunUnitPageMedium` → `Store.HandlePageMedium`, 2.2.1.8) — because a
  second handle for a page read or write would ask for access the connector's own `FileShare.None` forbids,
  which is kb/Work PB713's defect re-opened by its own cure. The handle's ACCESS is `FileConnector.HostAccess`
  (every organization opens its handle, `FileConnector.FileLock`, with it and spells none of its own; kb/Work PB2758),
  not the open mode's floor, because the keyed store engine reads the physical file before it writes it in every
  writable mode (the adoption rule's meta pages, 2.2.1.6 R6; §14.9.51.4 GR29 a)'s release number is a fact of the
  records already there).

There is NO widening and NO handle rebuild. Every pair of the three modes that Table 19 admits already has
mutually admissible base postures — `FileLockPostureDriftTests.EveryNormalOpenCellHasMutuallyAdmissiblePostures`
proves it over every *Normal open* cell and every access a handle may take — because a `READ ONLY` connector
admits only input-mode siblings and an `ALL OTHER` one admits every access. (While the implementor default was
UNDETERMINED the base of a clause-less connector was unknown, so its posture was widened to admit whichever sibling
Table 19 admitted and the handle was rebuilt at its logical offset by a virtual `Reposture`; kb/Work PB322
determined the default and deleted that machinery, `SyncHostPostures`, `FileLockPosture.For` and both
`Reposture` overrides with it. History: kb/Work PB740 — one boolean, "did this SELECT write a SHARING or LOCK MODE
clause", was spent on both audiences and answered each backwards: two clause-less connectors Table 19 PERMITTED to
share one file were refused by the host, the second OPEN answering '30' — a status no Table 19 row and no
§9.1.13.9 item produces — and its mirror, measured across processes, had a `SHARING WITH NO OTHER` connector
admit a foreign append to a file the program had declared exclusive.)

**The whole of the above is the COBOL-2002 FILE SHARING FACILITY, and a COBOL-85 program has none of it (kb/Work PB670, owner
2026-10-23, option (i); `DialectBehavior.FileSharingFacility`, VCR row 7.39).** File sharing and record locking is an optional
module (Annex A.4.7), §A.4.1 makes its rules and its I-O status values optional with it, and its clauses are 2002 introductions, so
at `--std 85` (`FileConnector.Edition`, baked into the connector at registration) `FileConnector.SharingFacilityInEffect` is false and
the runtime asks the question in ONE place per job: `SharedOpenAttempt` does not run the Table 19 loop, and the connector's default
mode (`ImplementorDefaultSharing(mode, edition)`) is ALL OTHER — §9.1.15's "a mode completely specified by the implementor", standing for
the one that restricts nobody, whose host share mode (`FileShare.ReadWrite`) admits every sibling handle of the run unit in every open
mode; `FileConnector.Open` takes no `RunUnitFileLock` (a column lock would refuse an in-process sibling like any other holder: the
OFD and handle locks do not know a process's own descriptors), and a host refusal by a process outside the run unit answers `'30'`
(§9.1.13.6 item 1), because `'61'` is a status the 1985 edition does not define. Every pair of opens is therefore a normal open, and
the engines meet the states Table 19 used to keep out: a keyed `OPEN OUTPUT` beside connectors that hold the file creates the new store
and `StoreTable.AttachCreated(…, arbitrated: false)` makes it the path's (the holders keep the store they have; what they find in the file
is the engine's own word, an at-end `'10'` for a created file with no records, §14.9.27.4 GR18). From 2002 the guard of PB754 stays loud
(`arbitrated: true` throws on a creation beside a holder). **The ALL OTHER default is a statement about what the host handle ADMITS,
not about what a lone connector DOES (review R1-1 of PB670).** A connector pays for a sibling only while one is open: from 2002 the
file lock fixed at OPEN decides it (`FileLockPosture.AdmitsAnotherRunUnit`), and at 85 `FileConnector.OtherConnectorsMayShare` also
asks whether another connector of the run unit is open on the physical file (`PhysicalFileTable.State.Open`). Where none is, a
sequential writer keeps its records in its input-output areas until the area fills or the CLOSE (GR12 releases to the operating
environment, as the 2002 clause-less OUTPUT does), and a reader compares only the release generation, with no re-anchor per frame.
A sibling that OPENs later is owed what was already written: `SharedOpenAttempt` asks every connector open on the file to
`ReleaseBufferedRecords` before the incoming body runs, and a host refusal met there is held by the writer and reported by its own
next WRITE or CLOSE (`'34'` or `'30'`), never by the opener. No run unit is promised another's records at 85 (A.4.1).

**The §9.1.15 FILE LOCK AGAINST OTHER RUN UNITS is Table 19 itself: `RunUnitFileLock` (`IO/Sharing/`), taken by
`FileConnector.Open` (kb/Work PB833).** The share mode above is what a process that takes part in no protocol
meets, and it cannot be the whole of §9.1.15's *"preventing other run units from opening that file with
incompatible sharing rules"*, for two measured reasons: .NET reaches `FileShare` on Unix through ONE advisory
`flock` whose only states are `LOCK_EX` (`FileShare.None`) and `LOCK_SH` (every other value), so rule 2's
*"restricts concurrent access … to input mode"* — admit a reader, refuse a writer — has no expression in it; and
on EVERY host a share mode refuses an open by its ACCESS, so it cannot refuse the truncation of an `OPEN OUTPUT`
when a sibling's posture admits a writer (measured on Windows before PB833: a second run unit's `OPEN OUTPUT`
answered `00` against a file the first held open `SHARING WITH ALL OTHER`, and emptied it, where Table 19 prints
*Unsuccessful open* in every OUTPUT cell). The remedy is not to widen the posture to `FileShare.None`, which
refuses the reader rule 2 admits; it is a second expression of the same table:

- **Publish, then test (`RunUnitFileLock.Take`).** The OPEN publishes its Table 19 COLUMN
  (`Table19.Column(sharing, mode)`) as a SHARED region lock on one byte of the physical file at offset
  2^62 + the column's wire number (the five `ExistingSharingColumn`s; the numbers are a contract between run units
  and are spelled out in the class, not derived from an ordinal), and then asks whether any OTHER description holds
  a column its request ROW refuses (`Table19.Cell(row, column) == UnsuccessfulOpen` over the five columns). A
  refusal is §9.1.13.9 1)'s `61`, the lock is given back, and §14.9.27.4 GR25's *"the file is not affected"*
  holds because the test precedes `OpenCore`'s body on a file that exists. Publishing FIRST is what makes two
  simultaneous OPENs unable to pass each other; it needs a lock the asker's OWN description does not conflict
  with, because the classes that refuse their own kind (NO OTHER, an OUTPUT) would otherwise refuse themselves.
  The table is read, never restated: the cross-run-unit verdict and the in-run-unit verdict are ONE lookup.
- **The host primitive is `OfdRegionLocks` (`IO/Sharing/`): Linux open-file-description locks** (`fcntl`
  `F_OFD_SETLK`/`F_OFD_GETLK`, kernel 3.15; x86-64 and arm64). Classic `fcntl` locks and `FileStream.Lock` are
  keyed on the PROCESS and are dropped when ANY descriptor the process holds on the file closes, which this runtime
  does constantly for its own bookkeeping handles; an OFD lock belongs to the one descriptor that took it, conflicts
  with every other descriptor (this process's or another's) and never reports the asker's own. The descriptor is
  made with `open(2)` directly (`O_CLOEXEC`), not through `FileStream`, because .NET would take a `LOCK_SH` `flock`
  on it that a sibling's own `FileShare.None` handle would then be refused by. The lock is unlocked explicitly
  before the descriptor closes, so a `fork` holding a copy of it until its `exec` cannot make a CLOSEd lock outlive
  the CLOSE.
- **OUTPUT asks the host first where the share modes are the lock (`FileConnector.TakeRunUnitFileLock`,
  `HostFile.ShareModesAreMandatory` — Windows, pinned by the measured `HostCapability.Sharing`).** An OUTPUT open on
  a file that exists first requests `FileShare.None` (`HostFile.IsHeldByAnother`) — refused by every outstanding
  handle there is, which is what Table 19's OUTPUT rows mean — before the body truncates anything. This is what
  closes the Windows gap, where there is no region-lock expression and `FileShare` carries every other cell. A host
  with the region lock does not ask: the lock refuses an OUTPUT like any other cell, and a read open of a FIFO waits
  for a writer that is the program's own next statement.
- **Capability is measured.** `OfdRegionLocks.Open` answers null — and `Take` answers `Unavailable` — for a host
  that cannot hold the lock: an ABI it was not written for (the commands and `struct flock` are the Linux 64-bit
  x86-64/arm64 values), no `libc` (Windows, where `FileShare` is mandatory and per-access already), `EINVAL` from
  the kernel (no OFD locks), `ENOLCK`/`EOPNOTSUPP` from a filesystem without byte-range locks — and a path that is
  not a REGULAR file (read from the descriptor with `statx`, opened `O_NONBLOCK` so a FIFO answers at once): the lock
  lives on the inode, so a device node such as `/dev/null` would arbitrate every run unit on the machine that assigns
  it. An unavailable lock
  leaves the connector the file lock its `FileShare` gives, nothing more.
- **One policy, three hosts (`IColumnLockHost`, kb/Work PB2484, PB3482).** `RunUnitFileLock.Take` is the POLICY — the
  column an OPEN publishes (`Table19.Column`), the columns its row refuses (`Table19.Cell`), the wire number of each
  (`RunUnitFileLock.WireNumber`) — and speaks to the host of the process through `IColumnLockHost.Establish`,
  chosen ONCE (`RunUnitFileLock.HostOfThisProcess`): `OfdColumnLockHost` where `OfdRegionLocks.Available` (Linux
  x86-64/arm64), `HandleColumnLockHost` where the share modes are mandatory (Windows,
  `HostFile.ShareModesAreMandatory`), `SidecarColumnLockHost` everywhere else. All three publish BEFORE they test and
  never count the asker's own publication, which is the whole of the race-freedom argument. Adding a host is one class
  and one line in the choice. A hold can be REPLACED by a new request (`Establish`'s `supersedes`;
  `RunUnitFileLock.Amend`, `Revert`): the new column is published on the same hold (Linux: a second byte of the one
  description; sidecar: a second holder file; Windows: a second byte through the holder's own handle) while the old
  stays published, the test ignores both, and the old goes only when the request is admitted — the connector's re-OPEN
  after a CLOSE under commit control, whose kept lock must never be free (PB3483, 2.2.1.10 As built (3));
  `RunUnitFileLockAmendTests` observes it on every host of the machine with a racing run unit sent in at the entry of
  every establishment.
- **The Windows host: a column is a byte RANGE (`HandleColumnLockHost`, `WindowsByteRangeLocks`, kb/Work PB3482).**
  Windows locks belong to a HANDLE and conflict with every other handle, the asker's own included, so one shared byte
  per column (Linux) cannot answer "does any holder OTHER than me hold this column" about a column the asker holds
  (READ ONLY beside a READ ONLY connector that writes, every OUTPUT). A holder opens a READ handle of its own
  (`HostFile.OpenColumnHolder`: share `ReadWrite`, so it refuses nobody the connector's handle admits) and publishes by
  holding exclusively ONE byte, chosen at random, of its column's 2^32-byte range (`HostRegionLocks.ColumnRangeStart`,
  from 2^62 + 2^61 + 2^60, past the liveness region); it tests another column by a shared request over the whole range
  (released at once, conflicting only with an exclusive hold, so testers never exclude each other) and its own column
  by two shared requests either side of its own byte. A collision with a holder or a tester's instant costs a retry.
  There is no cap on holders. **The owed-writes byte (kb/Work PB3499).** A holder whose connector is an INPUT opener with a
  handle open for WRITING (the re-OPEN of a CLOSE under commit control while its transaction stands, 2.2.1.10 As built
  (3); `FileConnector.OwesWrites`, `KeyedConnector.HandleAccess`) holds exclusively ONE byte more, of a sixth range of the
  same width right past the columns' (`HostRegionLocks.OwedWritesRangeStart`), published in the SAME `Establish` request as
  its column (`owesWrites`; both `RunUnitFileLock.Take` and `Amend` carry it, and `Revert` publishes the superseded state
  again). The column is taken BEFORE the OPEN body, so the connector says it from what the CLOSE kept
  (`WillTakeWritableHandle`: the one expression `Lock` opens the handle by), so a kept column is replaced by one that already
  owes, never an instant before; where the column is taken after the body (a CLOSE under SHARING WITH NO OTHER kept a
  `FileShare.None` handle that publishes no column, `KeptHandleExcludesEveryOpen`) the handle the body took says it
  (`FileLock.CanWrite`). It is tested as a column's range is, its own byte left out
  (`RunUnitFileLock.AnotherOwesWrites`). It is the one fact a share mode needs to yield to the right writer, and it
  replaced a proxy (PB3499: "does any other INPUT opener hold a column") that an unrelated ALL OTHER reader satisfied, so
  a `SHARING WITH READ ONLY` OPEN beside a program outside the protocol that holds the file for writing answered '00' with
  a reader open and '61' without one; the other two hosts have no share mode to yield and publish nothing for it.
  `SHARING WITH NO OTHER` publishes nothing (`Unavailable`): its own handle is
  `FileShare.None`, which refuses the holder's read handle and every other open, so the share mode already says all
  its column would; and a holder that meets a `FileShare.None` handle is `Refused` (every request row refuses the
  first column). A path that is not a disk file, is absent or is refused is `Unavailable`. Two consequences of the
  holder being a HANDLE: a re-OPEN under `SHARING WITH NO OTHER` that replaces a kept lock tests the NO OTHER row through
  the kept holder and, admitted, gives the holder handle way to the connector's `FileShare.None` one (a hold that
  publishes nothing; `Revert` publishes the kept column again through a new handle), the one instant this host leaves,
  the share-mode window of the kept handle (2.2.1.10 As built (3)); and the OUTPUT probe beside a kept holder is a READ
  request that admits only readers (`HostFile.IsWrittenOrExcludedByAnother`, `FileConnector.ProbeHeldByAnother`), which
  the holder admits and every writer or reader-excluding handle refuses, while the OUTPUT rows' columns refuse every other
  run unit's connector. Witnesses: `HandleColumnLockHostDriftTests` (every pair, release, the superseding requests, the
  race, many holders), `RunUnitFileLockAmendTests` and `RunUnitFileLockDriftTests` through the host of the machine.
- **The lock-file primitive (`SidecarLockFiles`) and its column policy (`SidecarColumnLockHost`).** macOS has no host
  primitive for the Linux bytes: its classic `fcntl` locks are process-owned, its open-file-description commands are
  private, and `fcntl` is variadic, which Apple's arm64 calling convention passes differently from a fixed P/Invoke
  signature — so the class makes NO native call. A holder's lock is a FILE — `.wolk-<16 hex digits>/<column>-<guid>`
  in a directory BESIDE the physical file (the digits: SHA-256 of the file's name under `HostFile.PhysicalFileKey`,
  the case rule that `HostFile.PhysicalFileComparer` also reads) — held open through `HostFile.OpenLockHolder` (an
  EXCLUSIVE `flock`: `FileShare.None` on Unix, `FileShare.Delete` alone on Windows, where share modes are mandatory
  and the open file must still be renamed) and tested by `HostFile.ProbeLockFile` (a read open under a share mode
  that admits every other reader and writer — a SHARED `flock` — refused only while a holder lives; the kernel drops
  the lock with the descriptor, so a killed run unit holds nothing). ⛔ HOLDERS EXCLUSIVE, PROBES SHARED, so two
  testers sweeping one dead file never read each other as a holder (kb/Work PB2917: exclusive probes made the second
  tester's OPEN '61' for a connector that did not exist). The probe answers four states — `Live` (the sharing
  refusal), `Dead` (opened freely: the caller retires it, below), `Gone`, and `Unobservable` (the process may not open it,
  or it is not a file): an unobservable holder is neither free nor swept, and a request row that refuses its column
  is refused. A lock file PER HOLDER and not per column, because a request row can refuse its own column and a
  `flock` cannot say "anybody but me" about a file the asker holds: the tester skips only its own file. A holder
  creates and locks `pending-<guid>`, verifies the lock is in force (a host that does not refuse the probe — a
  network file system, `DOTNET_SYSTEM_IO_DISABLEFILELOCKING` — is `Unavailable`; a pending file a tester reaped in the
  meantime is retried, not `Unavailable`) and RENAMES it to its final name, so the testers, which read only final
  names, never see a half-published holder; a file they can open freely belongs to a dead run unit and is deleted. The
  CLOSE takes the file's name BEFORE closing the descriptor (a `fork` for `CALL "SYSTEM"` holds a copy of the description
  until its `exec`; a name nobody can see cannot refuse a successor) and the directory with its last file. ⛔ NO NAME A PROBER READS IS DELETED IN PLACE (`SidecarLockFiles.Retire`, train 1056b): where share modes are mandatory a file deleted while another handle has it open stays listed, delete-pending, and every open of it is "access denied", the unobservable holder's answer, which no bound on asking again tells apart; so a lock file there is renamed to `gone-<guid>` (a wire value no tester probes) before it is deleted, and "access denied" is always an answer. ⛔ THE SAME HOLDS OF THE LOCK DIRECTORY (`SidecarLockFiles.RetireDirectory`, kb/Work PB3109): the last holder's CLOSE removes the empty directory, and where share modes are mandatory the directory passes through delete-pending under its own name for as long as any other handle on it is open, so a publisher's `CreateNew` inside it and a tester's listing of it were "access denied" — read as a directory this user may not write, a waiting region hold answered `Unavailable` and the store mutex went untaken (11 of 3,000 two-endpoint rounds under CPU load). So there the empty directory is renamed to `.wolk-gone-<guid>` first and only that name is deleted; a directory with any entry, or one the host will not move while a holder's file is open, stays standing, and a publisher that resolved the old name finds it gone and publishes again (the residue of a kill between the rename and the delete is one empty `.wolk-gone-` directory). Where an `rmdir` is atomic the same race meets a publisher one step earlier (kb/Work PB3313): its `mkdir` answers `EEXIST` because a sibling had just made the directory, the sibling's CLOSE removes it before .NET's own existence check, and .NET throws the `EEXIST` — so `HostFile.CreateLockDirectory` reads `EEXIST` with no FILE at the name as that race and returns (`HostFile.IsClosingSiblingRace`), as .NET on Windows does: with nothing left at the name the publication into the missing directory publishes again, and with a directory there again (a sibling's next publication made it after the CLOSE) the directory is another holder's (14 of 24,000 waiting two-endpoint rounds under CPU load on Linux answered `Unavailable` before it; the directory-again arm is branch review R3-1, asked directly by `RegionLocksDriftTests.ALockDirectoryEexist_IsTheClosingSiblingRace_UnlessAFileHoldsTheName`). ⛔ A DIRECTORY IN THE MIDDLE OF ITS `rmdir` IS NOT YET GONE (kb/Work PB3314): the kernel marks the inode dead and the name stays resolvable until the remover finishes unlinking it, so for as long as a preempted remover is off the CPU (measured up to 17 ms, 240 consecutive refusals, 8 processes beside 24 CPU burners) the directory answers `stat` and an empty listing while every creation inside it answers `ENOENT`; eight immediate tries in `SidecarLockFiles.Publish` all fell inside that window (1 waiting hold in about 500,000 answered `Unavailable`), so a lost race that repeats is paced by TIME instead — the first retry is immediate, every later one sleeps a jittered span up to twice the longest before it, to 128 ms (`SidecarLockFiles.RetryPauseCeiling`, 12 attempts, about 0.6 s at the ceiling), spent only when the race does not end. A directory
  the process may not write is `Unavailable`, never an exception — except that a user who may not WRITE the lock
  directory still LOOKS: a live or unobservable conflicting holder refuses it. A user who may enter the lock directory but not LIST it cannot look: every holder is unobservable to it, so a request row that refuses any column is refused (the train 1056b review: the other user of a `0711` parent, whose mode the lock directory copies). ⛔ THE CROSS-USER MODEL: the lock
  directory is created with the permissions of the physical file's directory (`HostFile.CreateLockDirectory`) and a
  holder's file takes the directory's read and write bits and always its owner's own (`HostFile.OpenLockHolder`: the holder probes its own file before publishing it), not the first holder's umask,
  so a second user of a group-writable shared directory can publish into it and read the first user's files (macOS
  gives a new directory the group of its parent). The determination is `docs/CONFORMANCE.md` DOC-A.1-75.
- **The region locks on the same primitive (`SidecarRegionLocks`, kb/Work PB2936).** The record locks and the store
  mutex of a shared keyed file (below) are held through a `RegionLocks` endpoint made by the host of the process
  (`IRegionLockHost`, `HostRegionLocks.HostOfThisProcess`): `HandleRegionLocks` where the lock belongs to the
  connector's own handle (Windows `LockFileEx`, Linux OFD), `SidecarRegionLocks` everywhere else. A held byte is a
  lock file in the SAME lock directory, `r<byte as 16 hex digits>-<S|X>-<guid>` (⛔ a wire value), published and
  probed exactly as a column file is (`SidecarLockFiles.Publish`, `Observe`): an exclusive hold is refused by any
  other live holder of the byte, a shared one by an exclusive holder, the asker skips only its own file, a waiting
  hold retries after a jittered pause, and an unobservable holder (or a lock directory the asker may enter but not list) is `Held` to a non-waiting hold and `Unavailable`
  to a waiting one (it cannot wait for a release it cannot see). Classic `fcntl` record locks cannot stand in: they
  belong to the process, and any second connector's CLOSE in the same run unit would drop the first's. A witness can
  run any connector on the lock-file host on any machine (`HostRegionLocks.Forcing`), so `RegionLocksDriftTests` and
  the cross-run-unit scenarios hold macOS's expression to the same contract as Windows's and Linux's.
- **Guards.** `RunUnitFileLockDriftTests` opens a file in one `FileRegistry` and then in a second — a second
  registry shares nothing with the first, neither the `PhysicalFileTable` nor a handle — for every sharing spelling
  × open mode × sharing spelling × open mode × organization (768 pairs) and asserts the second OPEN is `61` exactly
  where `Table19.Conflicts` refuses and a success-family status where it permits; it runs on Windows and Linux,
  through the host of the machine. `SidecarColumnLockHostDriftTests` holds the lock-file host to the same table over
  every (sharing × open mode) pair directly (`RunUnitFileLock.Take(host, …)`), plus the simultaneous-open race, the
  sweep of a dead holder's leftovers, the wire names and the case rule — it is plain managed code, so it runs on
  EVERY host (Windows, Linux and macOS) and needs no Mac to witness the macOS path. `RunUnitProcessBoundaryTests`
  (kb/Work PB2485) is the witness with TWO REAL PROCESSES, which a second registry cannot be: compiled COBOL programs
  hold a file while another process asks for it. A refused OPEN is `61` in every open mode and a refused DELETE FILE
  `62`, each through a FILE STATUS item and a USE declarative alone, both, and neither, and through a RETRY that the
  holder outlasts (and a `RETRY FOREVER` that is released by the holder's CLOSE); the lock comes back with the holder's
  CLOSE while the holder lives, with its run unit's implicit CLOSE, with an abnormal end (a call of a missing program)
  and with a kill that runs nothing; and a child process started while the lock is held does not keep it past the
  CLOSE (the `O_CLOEXEC` guard of `OfdRegionLocks.Open`; the handle's non-inheritance on Windows). It runs on Windows
  and Linux, through the host of the machine; the macOS lock-file host is held to the same table by the other witnesses.

**⛔ DETERMINATION (Annex A.1 item 75, `docs/CONFORMANCE.md` DOC-A.1-75) — WHO IS BOUND.** §9.1.15 binds other RUN
UNITS, and every WiseOwl COBOL run unit is arbitrated by Table 19 on every host: by the region lock (Linux
x86-64/arm64), by the byte-range columns of `HandleColumnLockHost` together with the connector's share mode (Windows,
with the OUTPUT probe above), by the lock files (everywhere else). A process that takes part in no protocol meets only
the share mode of the connector's own handle, whose strength is the host's:

- **On Windows** the share modes are mandatory and per-access: rules 1–3 are enforced as written against every
  process on the machine, with one yielding posture (kb/Work PB3482). A share mode refuses by ACCESS, and an INPUT
  opener can owe writes (2.2.1.10), so `SHARING WITH READ ONLY`'s `FileShare.Read` refused a request Table 19 admits
  beside it. A connector whose first attempt the host refused with its Table 19 column HELD (the table admitted it
  against every WiseOwl COBOL run unit) AND another run unit's connector OWES WRITES (an INPUT opener holding a handle
  open for writing, published with its column: `RunUnitFileLock.AnotherOwesWrites`, kb/Work PB3499) is OPENed again with
  `FileLockPosture.WhenOwedWritesAreAdmitted` — `FileShare.ReadWrite` — and the columns, not the share mode, keep out
  every request Table 19 refuses. **Determination:** a program outside the protocol that holds the file for writing
  refuses a READ ONLY request '61' whether or not an INPUT opener that owes no writes is open beside it (an unrelated
  ALL OTHER reader is no reason for the share mode to yield, and its presence changes no answer); and a READ ONLY
  connector's handle admits an outside writer only when its second attempt was made, because a share mode cannot tell
  the owing connector's writer from an outside one. That is the one consequence for an outside program that A.1 item
  75 leaves to the implementor: such a connector's handle stays `FileShare.ReadWrite` for the rest of the OPEN.
- **On Linux** the share mode is one advisory `flock`: rule 1 (`FileShare.None` ⇒ `LOCK_EX`) excludes an outside
  reader and writer that also take `flock`, and rule 2's refusal of an outside writer exists only against a run unit
  of this runtime (the region lock). A program that opens the file without locking is neither stopped nor
  detected. `HostCapability.Sharing` (tests/_shared) MEASURES the share-mode semantics and
  `FileLockPostureDriftTests.TheHostsShareModeSemanticsAreTheDocumentedOnes` asserts the measurement against this
  paragraph, so if the host moves, the gate goes red naming this section rather than an assertion being adjusted.
- **On macOS** (and every Unix that is not Linux on x86-64/arm64) the file lock is the LOCK FILE PER HOLDER of
  `SidecarColumnLockHost` (kb/Work PB2484, the bullet "The lock-file primitive" above), so every WiseOwl COBOL run unit
  is arbitrated by Table 19 there too; an outside program meets the share mode only, as on Linux. The classic
  `fcntl` locks macOS offers are process-owned and cannot be used for the reason above.


**The rule generalizes, and that is why it is written here rather than in the sequential connector.** Each
organization spells it in its own terms and each must read the shared medium at the release, never a base
captured at OPEN:

- sequential — the medium is the file, and the position is its current end (GR19);
- relative — the medium is the shared `RelativeStore`, and the number is `Highest + 1` at the write. GR29 a) says
  so twice: *"one greater than the highest relative record number existing in the physical file"*, and *"If the
  physical file is shared and the open mode is extend, the record numbers are not necessarily consecutive.
  Otherwise, they are consecutive"* — one expression yields consecutive numbers when nothing else is releasing
  and the ascending-with-gaps sequence GR31 describes when something is. `RelativeConnector`'s captured
  `_seqNext` is gone; the store's map is exposed read-only and every mutation goes through `Put`/`Remove`/`Clear`,
  which maintain `Highest`, so the high-water mark has no second way to go stale;
- indexed — the medium is the shared `IndexedStore`, whose `NextOrdinal` was already the shared mint. Its GR38
  high key is per-connector on purpose: the rule says *"when it was opened through that file connector"*.

`SharedExtendWriteDriftTests` measures the whole (organization × framing × spelling) matrix, the exclusive
control, the print-control write arm, the store's high-water invariant and a static ban on `FileMode.Append`
outside `HostFile`; it was proved red on the pre-fix behaviour at 17 of 27. (kb/Work PB739.)

**And the READ side of the same sentence: what a READ delivers is read from the shared medium AT THE READ.**
§14.9.35.4 GR4 gives REWRITE the identical words GR12 gives WRITE — *"The successful execution of the REWRITE
statement releases a logical record to the operating environment"* — and §14.9.30.4 GR21 c)/d) say what the
other connector then owes: the record selected is *"the first existing record **in the physical file** whose
relative key number is greater than the file position indicator if NEXT is specified or implied"*, and that
record *"is made available in the record area associated with file-name-1"*. The physical file, not a snapshot
of what it said when a buffer was filled. The keyed organizations satisfy this by construction — their medium is
the run unit's ONE store for the file (`StoreTable`, 2.2.1.7), so a sibling's change is visible instantly — and the sequential connector,
whose medium is the file system, needed the rule stating in two places at once:

- `PhysicalFileTable.State.ReleaseGeneration` counts the releases that have REACHED the physical file (both
  verbs; `SequentialConnector.NoteRelease` is the one place it moves, and it advances the releasing connector's
  own watermark in the same breath, because a sequential REWRITE targets a record at or before its own file
  position indicator and cannot invalidate its own read-ahead). `NextFrame` — the ONE physical framing walk —
  compares it before every frame and re-anchors the reader at the logical offset, discarding the read-ahead.
  A sibling OPEN OUTPUT's truncation moves it too (`OpenCore`'s OUTPUT arm calls `NoteRelease`): from COBOL-2002
  Table 19 refuses an OUTPUT beside an open connector, but at COBOL-85 the sharing facility is not in effect and the
  truncation is admitted, so an open reader's next READ re-anchors and meets the empty file (the at end condition).
- and the OS handle carries NO buffer of its own when the posture admits another writer, because
  `StreamReader.DiscardBufferedData` reaches only the reader's own characters: with the generation in place and
  a 4096-byte `FileStream` underneath, a sibling's REWRITE was STILL invisible, since `FileStream.Seek` reuses
  its read buffer whenever the target offset lands inside it. One managed buffer between the connector and the
  medium is the most that leaves GR21 c) true.

`SharedReadCoherenceDriftTests` measures the matrix — every framing, a buffer-crossing and a sub-buffer record
count, reader-first and writer-first, the clause-less pair — plus the controls (the sibling APPEND, a lone I-O
connector rewriting its own records, the two keyed organizations) and the two structural facts; each half was
proved red by injection at 25 of 47.

⛔ **A probe of this rule must have SIBLINGS open at once, and only `SHARING WITH ALL OTHER` admits a rewriting or
appending sibling** (kb/Work PB322: a connector with no SHARING specification is `READ ONLY` for `OPEN INPUT` and
`NO OTHER` otherwise, and Table 19 admits neither beside a writer). The matrix therefore runs its sibling shapes
over the `ALL OTHER` clause only, and the clause-less spellings over the shapes with no sibling (the lone I-O
connector rewriting its own records). (Before PB322 a clause-less pair could share, and the probe also had to be
built against the handle REBUILD a widening forced, which discarded a stale read-ahead as a side effect and
rescued a probe that filled its buffer before the sibling's OPEN and read after its CLOSE.) (kb/Work PB753 — a
reader that had taken record 1 went on serving its snapshot after a sibling REWROTE record 3 and reported '00', so a
concurrent read/update pass computed from data the same run unit had already replaced, silently. The invalidation
rule was already written down in `SeekToRecord`'s doc comment — for the connector's OWN seek, never for a sibling's
write.)

**And ACROSS RUN UNITS: the FILE is the truth, and every run unit's store follows it one page at a time (kb/Work
PB2660, PB3335; 2.2.1.7).** §9.1.15 3) admits concurrent writers in other run units — *"The sharing with all other
mode allows concurrent access to a physical file through other file connectors specifying input, I-O, or extend
mode"*, and *"Multiple paths of access may exist … or runtime elements in different run units"* — and §9.1.16 binds
them to each other's record locks: *"While locked by a given file connector, a record is not accessible to another
file connector in the same or a different run unit, except by the execution of a READ statement with the IGNORING LOCK
phrase"*, answered by §9.1.13.8 1)'s `51`. A run unit's `StoreTable` store is that run unit's alone, so the file has
to carry what the run units tell each other. Three pieces do it, and each lives in one place:

- **The GENERATION in the work meta (page 2, 2.2.1.4).** Every statement that changes the file ends by writing page 2
  with its generation — its claim, one above the highest generation claimed on the file's history (`stamp mint`,
  finding R3-4) — and a coherent statement's entry (`PagedFile.Enter`) reads that ONE page and compares its generation
  with the one the store last saw there. Equal: nothing moved. Different: the store ADOPTS page 2 (its roots and, for a
  writable store, the ledger a writer allocates from) and DROPS the run unit's page cache for the file instead of
  reloading anything, so the statement then reads only the pages it touches — O(log n), an unshared statement's count
  plus the one meta read (`StoreCoherenceTests`). It is the cross-run-unit twin of
  `PhysicalFileTable.State.ReleaseGeneration` above, which can live in memory only because a sequential sibling is in
  the same run unit.
- **The coherent STATEMENT (`KeyedConnector.BeginStatement` → `StoreStatement`).** A connector whose sharing mode admits
  ANY other run unit's connector — every mode but `SHARING WITH NO OTHER` (`FileLockPosture.AdmitsAnotherRunUnit`) —
  runs every record statement inside the store's cross-run-unit MUTEX (`HostRegionLocks.StoreMutexByte`; exclusive
  through a writable handle, shared through an `OPEN INPUT` connector's, whose entry is a reader's and writes nothing,
  not even beside a writable sibling of its run unit): it enters (taking the mutex, waiting for another run unit's
  statement to finish, and reading page 2), the statement runs its record-lock check, its operation and its lock
  actions, and its change is ONE statement of the page layer whose pages and page 2 are written before the verb answers
  (`KeyedStore.Change`). `FileRegistry` wraps every keyed record entry point in one, so to every other run unit a
  statement is one step — which is what §12.4.5.9.4 GR8's *"The setting of a record lock is part of the operation of an
  I-O statement"* needs, and why a check-then-act race between two run units cannot exist. The OPEN's adoption rule,
  the CLOSE's promotion and a reader's re-adoption when its run unit's last writable connector closes run under the same
  mutex. A `NO OTHER` connector cannot meet another run unit's connector (Table 19 refuses every other opener in every
  run unit, PB833), so it runs the same statement path without the mutex and without the page-2 read. Every page is read
  and written POSITIONALLY through a connector's own handle, one page per call (`Store.HandlePageMedium`), never through
  the `FileStream`'s buffer, which would hand back a page another run unit has replaced; a page write or a sync the
  medium refuses is the statement's own §9.1.13.6 item 1 `30`, and nothing is deferred to the CLOSE.
  `WholeStoreBanDriftTests` keeps every whole-file read and write out of `Runtime/IO`.
- **Published RECORD LOCKS (`PhysicalFileTable.LockRecord` / `HolderOf`, `HostRegionLocks.RecordLockByte`).** Every
  lock the run unit grants through a connector that has `RecordLocks` (the keyed organizations) is also held
  as a region lock on that endpoint, on one byte of the record region (SHA-256 of the record identity, 60 bits,
  far beyond any record), and every release gives it back. `HolderOf` answers the conflict question once:
  this run unit's table for its own connectors, the host for every other run unit — and says WHICH, because only a
  holder outside the run unit can release while a statement waits (`RetryAttempt.HolderOutsideRunUnit`,
  kb/Work PB1163). A `RETRY` against such a holder waits OUTSIDE the mutex (`FileConnector.WaitOutsideStatement`),
  since the holder can only release by executing a statement of its own, and re-enters before its next attempt;
  `RETRY FOREVER` against it is therefore not DOC-A.1-109's deadlock. Two distinct records share a byte with
  probability 2^-60, and the only consequence would be an over-cautious `51` across run units, never a missed one.

**The host primitive is the `RegionLocks` endpoint (`IO/Sharing/`), and the byte map in `HostRegionLocks` is its one
table.** A connector holds one endpoint per handle, made by the host of the process (`IRegionLockHost`,
`HostRegionLocks.HostOfThisProcess`, chosen once): `HandleRegionLocks` — Windows `LockFileEx`/`UnlockFileEx` (per
handle, mandatory for the covered bytes, all of which lie at 2^62 or beyond) and Linux open-file-description locks
through `OfdRegionLocks` (classic `fcntl` locks are per process) — or `SidecarRegionLocks` (a lock file per held byte,
the bullet "The region locks on the same primitive" above) on every other host. Linux refuses an
exclusive lock through a descriptor not open for writing, so an `OPEN INPUT` connector holds the mutex SHARED (a
reader never changes the store) and, on Linux only, publishes a record lock shared and then TESTS it, giving it back
if another handle holds the byte — publish then test, as `RunUnitFileLock` does. A Windows reader publishes its record
lock EXCLUSIVELY (`RegionLocks.ExclusiveNeedsWritableHandle`): `LockFileEx` takes an exclusive lock through a
read-only handle, and its locks are per handle, so a test through the handle that holds the byte shared meets its own
hold and answers held with nobody else holding it; a lock file has no such limit and holds exclusively whatever the
connector's access. ⛔ **AN ENDPOINT GIVES EVERY BYTE BACK EXPLICITLY, NEVER BY THE CLOSE OF ITS HANDLE (kb/Work PB3549).**
Every host keeps the endpoint's own map of the bytes it holds (holds do not stack: a byte held at least as strongly already
is held at once, and one release gives it back), and the endpoint's disposal unlocks each of them through the handle while
the handle is still open; every owner of an endpoint (the keyed connector's CLOSE and abandoned OPEN, the sequential
connector's, a kept handle's give-back, `KeptFile.GiveBack`) disposes it BEFORE it closes the handle. The close is no release a
successor can rely on: a Linux open-file-description lock lives until every descriptor on the description is closed, and a
`fork` in another thread (a `CALL "SYSTEM"`, any child process the host program starts) holds a copy of every descriptor
until its `exec`; Windows documents that the time it takes to unlock a closed handle's locks *"depends upon available system
resources"*. A transaction's liveness byte left to the close was read as a LIVE owner by the next writable OPEN, whose
dead-owner step left the abandoned transaction standing (an intermittent red in a parallel Linux run; reproduced
deterministically by a second handle on the same open file, `tests/_shared/DescriptionCopy.cs`). The Windows column holder
(`HandleColumnLockHost`) unlocks its byte before it closes its handle for the same reason, and the OFD column host already
did. The model is `scripts/models/kept_liveness_model.py` (a self-test every gate and CI runs): the give-back under every
fork and exec interleaving, the probe's transient answers, a live owner never rolled back, and a watchdog of mutations (the
disposal that unlocks nothing, the close before the disposal, an interrupted or transiently refused probe read as unavailable,
an unknowable owner read as dead) each RED on the row it names. The region map: `RegionBase` = 2^62; the five
Table 19 column bytes at +0..+4 (`RunUnitFileLock`); the store mutex at +0x100; a sequential file's ENTRY MUTEX at +0x102
(2.2.2.4, always a lock file: the file it guards may not exist); the LOCK-PRESENCE byte at +0x101,
held shared by every handle that holds at least one published record lock, so a statement asks that one byte
(`PhysicalFileTable.AnotherRunUnitHoldsLocks`) before it pays for a record identity, a SHA-256 and a record byte; the
record region at 2^62 + 2^60 for 2^60 bytes. On macOS the same bytes are lock files (`SidecarRegionLocks`, kb/Work
PB2936; the five Table 19 column bytes likewise, `SidecarColumnLockHost`, kb/Work PB2484): a byte offset cannot be
locked there without a native call whose ABI differs on Apple's arm64, but a held file can be named for the byte, the
mode and the holder. A host that cannot hold the lock at all (a directory the process may not write, a network file
system without `flock`) answers `Unavailable` and the run unit keeps only its own record locks and store
(docs/CONFORMANCE.md DOC-A.1-75). `CrossRunUnitKeyedStoreDriftTests` and `CrossRunUnitSequentialLockDriftTests`
measure it with two `FileRegistry` instances (two run units sharing no table and no handle) on the host of the machine
AND on the lock-file host (`RegionHostKind`): `51` on `READ WITH LOCK` and
`REWRITE`, `IGNORING LOCK`, a sibling's `WRITE` visible at the next statement, every run unit's update surviving
every CLOSE, and a `RETRY` (n TIMES and FOREVER) that waits outside the mutex.

**The record-lock CEILING is the last check of a WRITE or REWRITE (kb/Work PB2848).** §12.4.5.9.4 GR7's `54` / `53`
judge the lock a statement requests, and that lock is the statement's effect on success (§14.9.51.4 GR11, §14.9.35.4
GR12), so the registry no longer asks the ceiling before calling the organization. It hands the connector a
`FileConnector.CommitGate` for the statement (`FileRegistry.LockCeilingRefusal`, only when the statement requests a NEW
lock) and every organization's WRITE and REWRITE asks it once (`RefusedAtCommit()`), after its own rules and before it
first changes the file or its store: the sequential record, line and print arms, `RelativeConnector.Write`/`Replace`,
`IndexedConnector.Write`/`Rewrite`. A new organization or arm inherits the order by calling it at its commit point;
`CommitGateDriftTests` measures every one with a gate that always refuses.

**The SEQUENTIAL organization publishes its record locks the same way (kb/Work PB2692).** Its `RecordLocks` endpoint is made for
the handle beneath its reader or writer (`FileConnector.FileLock`, read through `HostFile.FileOf`), writable exactly
when the host granted that handle write access, and three facts make the published lock guard the
right record across run units. (1) A writer that registered a record-locking posture opens its handle READ-WRITE
(`SequentialConnector.HostAccess`): the presence byte is a SHARED hold, which Linux refuses through a write-only
descriptor, and a lock with no presence byte is invisible to every other run unit's hot-path test. The read half is
a PREFERENCE for OUTPUT and a NEED for EXTEND (kb/Work PB2848): OUTPUT creates the file and has no record to read,
so a host that refuses the read (a write-only file) still grants the write, `OpenWriter` falls back to the mode's
write-only handle, and that connector numbers its records by the in-run-unit mint (its `FileLock` cannot read) and
publishes no presence; EXTEND numbers the records already in the file and answers `37` without the read. (2) A sharing
writer's released record is NUMBERED OFF THE MEDIUM (`ReleaseRecord` → `PhysicalOrdinalOfRelease`): the record lands
where `SharedAppendStream.ReleaseStart` says, and its ordinal is one more than the records starting before that
offset, counted incrementally by the one framing-aware count (`CountRecords`). The in-run-unit mint
(`PhysicalFileTable.State.ReleasedOrdinal`) serves only a writer that is the file's only writer, where it agrees with
the medium. (3) A reader whose file lock admits another writer RE-ANCHORS AT EVERY FRAME
(`EnsureReaderCoherent`): the release generation is one run unit's memory, so another run unit's REWRITE would
otherwise be served from the read-ahead. `CrossRunUnitSequentialLockDriftTests` measures it for the fixed, line and
varying framings.

**Emptying a keyed store is its CREATION, done in ONE place: `StoreTable.AttachCreated` and `PagedFile.Create` (kb/Work
PB754).** An `OPEN OUTPUT` creates the file (§14.9.27.4 GR18, *"After the creation of the file, the file contains no
records"*), and emptying a store is a change every attached connector sees instantly, so the creation attaches a NEW
empty store and refuses, loudly, to create one another connector of the run unit still holds — Table 19 has already
refused that OPEN (§9.1.13.9 1) e)), so reaching it is an arbitration defect. Its one truncation is `PagedFile.Create`
(three meta pages), reached only through `KeyedStore.Create`. The absent-OPTIONAL creation arms (§14.9.27.4 GR17) attach
normally: the file of no bytes the file lock created is adopted as the empty store. A store holds no records in memory,
so no connector clears one (`CrossRunUnitKeyedStoreDriftTests.NoConnectorClearsASharedStore`).

**The SORT/MERGE work store is an external merge sort in bounded memory (kb/Work PB3339).** The owner's ruling on the
keyed stores (2026-10-17, "Reading the entire file into memory as a design requirement is a fatal flaw") applies to
the sort-merge store as well: a SORT of a multi-gigabyte file is ordinary COBOL work. `CobolSort` keeps the statement
API and the procedure phase (`SortStore`), and the records live in ONE engine, `SortWork` (`IO/SortWork.cs`), owned by
the store for the statement's life:
- **The limit** is `SortMemory` (`IO/SortMemory.cs`), read once per run unit when `RunUnit.SortFiles` is built:
  `WISEOWL_COBOL_SORT_MEMORY` (bytes, with an optional `K`/`M`/`G` suffix; default 128 MiB, GnuCOBOL's `COB_SORT_MEMORY`
  default; floor 64 KiB) and `WISEOWL_COBOL_SORT_TMPDIR` (else `Path.GetTempPath()`), both registered in `RuntimeConfig`.
- **The release phase** appends to one in-memory chunk and charges each record its resident estimate (image,
  frame, extent table and the decoded key columns the chunk's sort will build). A record that would pass the limit
  first SEALS the chunk: a SORT sorts it (`SortKeyColumns` decoded once per record, an index sort whose tie is the
  release order) and writes it as a run; a MERGE appends each USING stream's part to that stream's run, after the
  §14.9.24.4 GR6 order check, which `Merge` raises when checking is enabled. F runs of one level merge into one run
  of the next level (`SortMemory.FanIn`: the limit over four 64 KiB read buffers, between 2 and 16), always the last F
  and so always consecutive.
- **The sequence phase** sorts or merges from memory when nothing was written (no disk cost for a file that fits);
  otherwise it writes the last chunk and merges runs until at most F remain. The return phase is `SortMergeCursor`, a
  k-way merge whose tie goes to the earliest run — the one rule that gives both §14.9.40.4 GR3 and §14.9.24.4 GR4.
  `Rewind` restarts it for each GIVING file.
- **Run files** are a fifth `HostFile` role (`HostFile.CreateSortWork`, `IO/FileSupport.cs`): a new file in the work
  directory, delete-on-close on Windows and unlinked at creation elsewhere, one handle per run, read back through the
  same handle (`SortRunFile`). `SortWork._files` is the ONE owner of every open run file: `NewRun` registers a file
  before its first record is written, a merge closes its inputs (`SortWork.Close`) only once its output is complete,
  and the run lists and `SortMergeCursor` only refer to files, so `SortWork.Dispose` (the statement's `finally`)
  closes every one after a failure as after a normal end. A record is its character count, its extent table (with an interned layout index,
  since `RecordExtents.Layout` is an object the fixed form of a GIVING file needs), then its UTF-16 code units
  unconverted — private to the process, so no file coded character set applies. An I/O failure of a work file is an
  implementor-defined fatal stop (`CobolImplementorFatalException`) naming the directory and the host's reason.
- **Memory**: at most the limit (or one record, when a record alone exceeds it) in the release phase; F read buffers
  and F head records in the return phase. `SortWorkBoundDriftTests` pins the shape: no record collection in the store
  but the chunk.

The static `CobolFile` facade (kept for the emitted surface) becomes a pure delegator to `RunUnit.Current.Files`.
The `Keyed*` static methods at `IndexedFile.cs:570-707` are **deleted**; their callers in `CobolFile.cs` collapse to a
single `Files[name]` polymorphic call. `RecordFraming` (today `KeyedFrames`) becomes the ONE framing helper used by all
three (sequential varying framing folds into it — same 4-byte LE prefix). File **sharing/locking**
(`CobolFile.Locks.cs` + the `FileSharing`/`FileLockMode` enums) becomes `IO/Sharing/` — a `PhysicalFileTable` owned by
`RunUnit` (today the sharing registry is another static in the Locks partial). `CobolSort`, `ReportWriter`,
`AcceptSource` (renamed `IClock`/`SystemClock`) stay in IO.

#### 2.2.1 The keyed store engine — a record-level, crash-consistent, page-granular format for RELATIVE and INDEXED files (kb/Work PB3330)

⛔ **STATUS.** This section is the DESIGN the owner ordered on 2026-10-17 ("THis is a completely unacceptable design", of the
whole-store model it replaced). The core groups kb/Work PB3331 to PB3335 have built it — the page layer and its ledger, the
B+ tree, RELATIVE and INDEXED on the engine, and coherence across run units at page granularity — and the whole-store model
is deleted, with no reader of its format kept; the paragraphs above and the two `docs/CONFORMANCE.md` rows (DOC-A.1-107,
DOC-A.1-75) describe the engine, and the performance witnesses (a) to (d) of 2.2.1.11 hold it to its bounds (PB3335,
PB3336). The runtime half of 2.2.1.10's transactions is built (kb/Work PB3337; the owner decided the scope on 2026-10-19
18:58 PDT — the whole module, "Option A"): in the tree, `Store/Transactions.cs` (the three trees' keys, the one walk that
retires a transaction, `IRestoringTrees`), the UNDO and HOLDINGS edits of every changing statement under commit control
(`KeyedStore.Change`), COMMIT as a promotion whose retirement retires the transaction with the region-3 reversal
(`PagedFile.Commit`, `Retire`), ROLLBACK and the dead-owner rollback as one statement (`KeyedStore.RollBack`), the
dead-owner step at every exclusive entry and writable OPEN, the holdings lock test (`KeyedConnector.HoldingsGate`), and
`FileRegistry.RegisterCommitControl` / `Commit` / `Rollback`, with the connector-level cases in
`tests/WiseOwl.Cobol.Tests.Unit/Store/CommitRollbackRuntimeTests.cs` (among them the abnormal-ending wiring of each run-unit
boundary, `RunUnit.Run`, `ProgramTable.RunMain` and the host's `CallFromHost`, which record the ending that
`RunUnit.Terminate` reads to choose the commit or the file-only ROLLBACK, kb/Work PB3481); every transaction row of 2.2.1.16 and 2.2.1.17 runs
through the fault-injecting medium of 2.2.1.12 in `tests/WiseOwl.Cobol.Tests.Unit/Store/TransactionFaultMatrixTests.cs`
(the process death, the refusal and the power loss at every point of COMMIT, its reversal, ROLLBACK, the dead-owner step
and a first change, and a holder's death at every point of another run unit's statement), and witness (e) of 2.2.1.11 in
`KeyedStorePerformanceWitnessTests`. Still the target: the compiler half (PB3338) and the sequential journal (PB3340). PB3330 lands when its last group has landed. The bar it meets is PB3330's, line by line: no statement reads or writes the whole file; OPEN
reads the header; a keyed statement touches O(log n) pages; the size limit is the host's; a statement's change reaches the
file when it completes, crash-consistently; `SHARING WITH ALL OTHER` is coherent at page granularity with today's record locks;
every current golden keeps passing plus measured performance witnesses; one engine, no second path and no reader of the old
format; and APPLY COMMIT / COMMIT / ROLLBACK are built on it.

**2.2.1.1 What the surveyed engines do (sources, read 2026-10-17; copied nothing).**

| Engine | Storage shape | Duplicates | Crash model | Source |
|---|---|---|---|---|
| GnuCOBOL 3.2 RELATIVE | one fixed slot per record number: `relsize = record_max + sizeof(size)`, `off = relnum * relsize`, read/written in place with `lseek`; an empty slot is a zero length word (`relative_read` 3048-3092, `relative_write` 3199-3260, `relative_delete` 3300-3334 of `libcob/fileio.c`) | n/a | in-place, no log | the extracted tarball at `scratchpad/gnucobol-src/gnucobol-3.2/libcob/fileio.c` |
| GnuCOBOL 3.2 INDEXED | one Berkeley DB `DB_BTREE` per key (`indexed_open`, 4612-4625); the environment, when `DB_HOME` is set, is `DB_CREATE \| DB_INIT_MPOOL \| DB_INIT_CDB` (3527) — a page cache and concurrent-data-store locking, no `DB_INIT_TXN`/`DB_INIT_LOG` | `DB_DUP` on a key declared WITH DUPLICATES (4612) | Berkeley DB's: none without a transactional environment | same file |
| Berkeley DB Btree | pages of 512 B to 64 KiB, a power of two, defaulting to the filesystem block size capped at 16 KiB; items over `page_size / (minkeys × 2)` go to overflow pages; the finest lock is the page | unsorted duplicates "in the order in which they were added"; `DB_DUPSORT` orders them by a comparator | write-ahead log, checkpoints that write dirty pages so older logs can go, recovery that undoes unfinished and redoes committed transactions; `DB_TXN_WRITE_NOSYNC` keeps ACI and loses only transactions since the last OS flush on a SYSTEM failure | docs.oracle.com/database/bdb181/html/programmer_reference/{general_am_conf,bt_conf,transapp_checkpoint,transapp_recovery}.html, api_reference/C/envset_flags.html |
| IBM VSAM KSDS / RRDS | control intervals (the unit of transfer; e.g. 4 KiB) grouped in control areas; a CI holds records from the low end, a CIDF and one RDF per record from the high end, free space contiguous; an insert that does not fit splits the CI (half the records move to a free CI of the CA), a CA without a free CI splits; a KSDS is a B+ tree (sequence set over the CAs, index set above); an RRDS is a string of fixed-length slots addressed as `slot size × RRN`, an insert into an occupied slot is an error, an RRN never changes; an alternate index is itself a KSDS whose record maps one alternate key to one or more prime keys, maintained by the base only when it is in the upgrade set | non-unique alternate keys: one AIX record, several prime keys | not COBOL's: VSAM RLS takes record-level SHARE (CR) or no (NRI, "dirty read") locks | en.wikipedia.org/wiki/Virtual_Storage_Access_Method; www.ibm.com/docs/SSGMCP_5.5.0/applications/designing/dfhp3mk.html; www.ibm.com/docs/SSLTBW_2.1.0/com.ibm.zos.v2r1.idad400/d4340.htm; www.ibm.com/support/knowledgecenter/SSLTBW_2.1.0/com.ibm.zos.v2r1.idad400/mod9.htm; support.sas.com/documentation/cdl/en/vsamref/61644/HTML/default/a001273101.htm |
| Micro Focus | IDXFORMAT 1 C-ISAM, 2 Level II, 3 (default), 4 (optimized for duplicate keys), 8 (large files), 12 (a separate `.idx` side file holding the key information, otherwise as 8); `rebuild -n` reports the format, `rebuild /q` rebuilds from the `.idx` file | per format | a crashed file is "corrupt" and is REBUILT by the utility | community.rocketsoftware.com/rocket-net-express-server-express-30/how-does-rebuild-report-various-idxformat-settings-19451; the IDXFORMAT and Rebuild pages as summarized by the search engine (the hosts `10252761.microfocus.com` and `cabs.microfocus.com` no longer serve them) |
| ACUCOBOL-GT Vision | block size 512 to 8192 bytes in 512-byte steps (versions 2-4 at most 1024), BLOCK CONTAINS sets it; version 3 keeps keys and data in one file capped at 2 GB; versions 4 and 5 split data and keys into segments of up to 2 GB each; version 6 uses 64-bit offsets | per version | a damaged file answers status 98 and is repaired by `vutil -rebuild` (per key, `-k n`) | community.rocketsoftware.com/rocket-acucobol-29/vision-filesystem-how-do-i-fix-a-corrupt-file-22582; the "Indexed Files - Vision" and "Other Vision Features" pages as summarized by the search engine (every microfocus.com host redirects to an opentext.com 404) |
| Informix C-ISAM | a `.dat` and an `.idx` file "always used together as a single, logical ISAM file"; B+ tree indexes with compressed keys; duplicate and unique keys; transaction management, logging and recovery; a `bcheck` index-integrity utility; 2 GB file limit before 7.25 | yes | a transaction log, replayed by recovery | public.dhe.ibm.com/software/data/informix/pubs/pdfs/cisam.pdf (the technical brief; text extracted locally) |

Three facts of the survey decide the shape below. Every production engine is PAGED (VSAM's CI, Berkeley DB's page, Vision's
block, C-ISAM's node), indexes keys in a B+ tree whose leaves carry the records or point at them, and keeps a relative file as
slots addressed by record number. Every engine that survives a crash without a rebuild utility does it with either a log
(Berkeley DB, C-ISAM) or by never overwriting a page a committed state still references; the ones that overwrite in place
without a log (GnuCOBOL's RELATIVE, Vision, Micro Focus) ship a rebuild utility instead. And duplicate alternate keys are kept
in release order by the ENGINE (`DB_DUP` insertion order; an AIX record's list), never recomputed from a record list — the
per-key ordinal vector kb/Work PB341 and PB2693 established is the same fact, and it stays.

**2.2.1.2 What the host costs (measured 2026-10-17, `scratchpad/proto-pb3330/Program.cs`, Windows 11 26300, .NET 10.0.13, NTFS,
the repository's drive and the temp drive; the numbers decide the fsync policy and the page size).**

| Operation (4 KiB pages) | Per operation |
|---|---|
| positional write handed to the OS, no fsync | 7.4-8.4 µs |
| positional read, random page of a 256 MiB file (mostly OS-cached) | 11.8-15.2 µs |
| positional read, hot page | 1.4-1.8 µs |
| write + `FlushFileBuffers` (`FileStream.Flush(true)`) | 2.9-3.1 ms |
| appends with one fsync per 10 / per 100 statements | 0.28-0.32 / 0.025-0.027 ms per statement |
| the whole-store model (deleted, PB3335): a persist of 64 MiB through the areas (every changing statement under ALL OTHER) | 77-92 ms |
| the whole-store model (deleted, PB3335): a load of 64 MiB (every OPEN; every reload) | 65-68 ms |

An fsync is three hundred times a page write. A design that makes every statement durable against POWER LOSS costs 3 ms per
WRITE (a million-record load: 50 minutes); one that makes every statement durable against PROCESS DEATH costs one page write
per page the statement changed (the same load: well under a minute). The standard asks for the first only at a COMMIT —
§9.1.18.3: *"The COMMIT statement is used to permanently commit any changes made to the files subject to APPLY COMMIT clauses
at that point, so that they will remain in that state even in the event of subsequent abnormal termination."* — and PB3330's
bar asks for the second of every statement ("a process that dies leaves the file in the state after its last completed
statement, and never a torn one"). So the engine hands every statement's pages to the operating environment before the
statement completes, and SYNCS at the points the standard and the file's integrity need: COMMIT, CLOSE, run-unit termination,
and a bounded checkpoint (2.2.1.6). Power loss between two sync points loses at most the statements since the last one and
never tears the file, which is Berkeley DB's `DB_TXN_WRITE_NOSYNC` contract ("transactions exhibit the ACI ... properties, but
not D") made explicit in the format rather than in a flag.

**2.2.1.3 The decisions, in one place.** (Decisions 4, 5 and 7 and the rules they name were corrected on 2026-10-17 by the
owner-approved finishers of the round-1 to round-4 reviews; 2.2.1.6, 2.2.1.7 and 2.2.1.10 carry each finding's evidence,
2.2.1.15 is the table every writer and reader of the format is checked against, and 2.2.1.16 and 2.2.1.17 are the crash
points and the two-run-unit interleavings, each with the rule that decides its outcome. Round 4 replaced two case-by-case
answers with two rules that make their class automatic: a transaction's locks are PAGES OF THE FILE, so no death can
release them before their rollback; and every identifier the format mints is CLAIMED before anything carries it, so no
attempt's fate can let it be issued twice. Round 5 added two more of the same shape: an answer is given only for a write
the medium ACKNOWLEDGED, a write handed over and unacknowledged is reversed under the same mutex hold before the answer,
and where neither can be acknowledged the answer is unknown, never a fate the medium can contradict; and every lock a
transaction holds is an entry of ITS OWN UNDO tree, the HOLDINGS tree being the index, so whoever retires or rolls back a
transaction finds every holding by the walk it already makes. 2.2.1.18 tabulates, for every identifier, lock and
before-image, who finds it and through which index, and, for every answer a program can be given, what the file holds
after any later crash.)

1. **ONE paged file, no sidecar, one engine.** The data file IS the store (owner 2026-09-07, kb/Work PB802: "No sidecar of any
   type"). It is a sequence of 4 KiB pages; pages 0, 1 and 2 are the three META pages (two COMMITTED slots and the WORK slot,
   2.2.1.6); every other page is a B+ tree page, an overflow page or a free page. RELATIVE and INDEXED are the same engine with
   different KEY ORDERS and a different number of trees; every sharing mode runs the same statement path. There is no small-file
   path, no in-memory path and no reader of the whole-store format: a whole-store file (its header's magic at offset 0,
   version 5 since kb/Work PB1099) is `StoreFormat.Foreign` and its OPEN answers '39' exactly as a format-3 file does today
   (CLAUDE.md rule 4; the paged format's meta pages carry the magic at offset 32 with version 6, so neither reader can
   take the other's file for its own, and `MetaPage.Describes` answers the existing `Foreign` arm).
2. **The page size is 4 096 bytes**, the host's I/O block (Berkeley DB's default rule; `HostFile.InputOutputAreaBytes` is
   already 4 096) and the size the measurements were taken at. It is a format constant, not a per-file choice: one size makes
   every page computation static, and a record larger than a page's payload goes to overflow pages (2.2.1.4), so no record
   size needs a bigger page. Page numbers are 32-bit, so the format addresses 2^32 × 4 KiB = 16 TiB (2.2.1.9).
3. **Records live in B+ trees.** INDEXED: one PRIME tree (prime key → the record's frame) and one ALTERNATE tree per alternate
   key ((alternate key, release ordinal) → prime key). RELATIVE: one tree (relative record number → the record's frame).
   The leaf entry's value is the ONE frame shape `RecordFraming.EncodeFrameHead`/`ReadFrame` already define (length word,
   extent table, an indexed record's ordinal vector, payload), so the frame codec of kb/Work PB2754 stays the one codec and the
   store-level shape around it (`ComposeStore`, `DecodeStore`, the gap tag, the composed image) is deleted. The FREE tree, the
   LEDGER tree, the TRANSACTIONS tree and every UNDO tree (2.2.1.10) are the same engine.
4. **Crash consistency is SHADOW PAGING with a GENERATION LEDGER and STAMPED POINTERS, not a log** (2.2.1.6): no page that
   either the COMMITTED meta or the WORK meta references is ever overwritten; a statement writes every page it changes to a
   page neither references (path copying to the root, every statement), then the WORK meta; every pointer carries the
   generation of the page it names, so a stale or torn page is recognized the moment it is read; a sync point promotes the
   work meta into the committed slot holding the older generation, so a torn promotion leaves the other slot intact, and the
   RETIREMENT statement that follows every promotion rewrites the work slot onto the new base; every changing statement
   CLAIMS every identifier it mints — its generation, a transaction id, its release ordinals — in the store's memory before
   its first page write and, under the mutex, on page 2 in the same breath, so neither this run unit nor another ever
   issues an identifier a dead or refused attempt drew (findings R3-4, R4-2, R4-3); every writable OPEN FENCES the
   generation (a promotion of the adopted state 4 097 generations up), so no
   generation a dead history may have stamped is ever stamped again; and a promotion is restartable from the PROMOTING flag
   its claim sets — completed when its slot landed, otherwise run again (a checkpoint) or unwound (a COMMIT), the slot
   being the one durable write that decides it. Rollback of a run unit's transaction is a record-level undo (2.2.1.10); recovery is a bounded verification
   at OPEN (no replay, no rebuild utility); and the three things a crash can do — lose nothing, lose the statements since the
   last sync, or (a transaction's owner dying) have that transaction rolled back — are the only three things it can do.
5. **Coherence across run units stays the PB2660 statement mutex, at PAGE granularity, for every sharing mode that admits
   another run unit's connector** (2.2.1.7): a coherent statement reads ONE page (the work meta) to learn whether it changed
   since the connector last saw it and, when it did, adopts it and DROPS its page cache; it then reads only the pages it
   touches. Nothing reloads and nothing persists whole. Record locks, the lock-presence byte, the store mutex and the Table 19
   columns keep their bytes, their hosts and their observable behaviour (2.2.1.8); one byte region is added, the
   transaction-liveness bytes of 2.2.1.10. A transaction's own locks are not in the region at all: they are pages of the
   file, its HOLDINGS (decision 7).
6. **The boundary of a relative or indexed file is the host's** (2.2.1.9): a WRITE or REWRITE answers '24' only when the
   record would take the store past the format's 2^32 pages or past a maximum file size the host REPORTS (`EFBIG`,
   `ERROR_FILE_TOO_LARGE`); a DELETE that meets it answers §9.1.13.6 item 1's '30' with no invalid key condition — item 4's '24' is an attempt to WRITE outside the boundaries, and §14.9.10.4 GR3 and GR4 give a DELETE the invalid key condition only when the file does not contain the record (its tree and ledger rewrite can need a growth page); a medium that is full answers §9.1.13.6 item 1's '30' at the STATEMENT, with the file as the
   previous statement left it, never at a later CLOSE.
7. **APPLY COMMIT, COMMIT and ROLLBACK are per-run-unit TRANSACTIONS with record-level before-images kept in the file**
   (2.2.1.10): a COMMIT is a promotion whose one durable write — the slot write and the sync that acknowledges it — carries
   the transaction's id and whose retirement forgets its before-images. It answers success only once that sync landed; one
   refused before its slot write has committed nothing; and one whose sync after the slot write fails — UNACKNOWLEDGED: the
   operating environment holds the slot, the medium may or may not — REVERSES itself under the mutex it still holds, the
   rollback of its transaction as the promotion's retirement and a promotion of the reversed state, before it answers, so
   that what the program is told agrees with every state the medium can later show; where neither the slot nor its reversal
   can be acknowledged the answer is "unacknowledged" ('30' for the COMMIT and for every ROLLBACK after it until a sync
   lands), never success and never a failure the medium can contradict (2.2.1.10, the UNACKNOWLEDGED-COMMIT rule; round 4's
   self-attack and round 5's finding R5-1). Every holding of a transaction is an entry of its own UNDO tree, and the
   HOLDINGS tree is the file's index of those entries by identity, so COMMIT, ROLLBACK and the dead-owner rollback find
   every lock a transaction holds by one walk of the tree they already read (finding R5-2). A ROLLBACK re-applies the before-images, touching no other run unit's records; a run unit that dies has its transaction rolled back by the FIRST
   statement any connector with a writable handle runs on the file afterwards — a record statement's coherent entry or an
   OPEN, under the mutex, before that statement touches anything (2.2.1.10, finding R3-1). Every record a transaction changes
   and every unique alternate key value its change frees is a HOLDING of the transaction, written INTO THE FILE (the
   HOLDINGS tree, 2.2.1.10) by the statement that makes the change, so the lock outlives the owner's handle: a writer that
   meets a holding at its own lock test probes the owner's liveness byte there and then — '51' while the owner lives, and
   when it is dead the rollback runs first, at that test, before the writer changes anything. The outcome does not depend
   on the instant the death fell (finding R4-1: a lock the operating environment releases with the handle cannot protect a
   rollback that runs after the death; the surveyed engines keep a dead holder's locks as RETAINED locks until its backout,
   and so does this one, in the file). A READ's lock (§12.4.5.9.4 GR4) stays a region lock, since nothing restores what a
   READ held. Plus the lock releases §14.9.7.4 GR3 and §14.9.36.4 GR3 name; the data-item half is the compiler's and is its
   own group (PB3338).

**2.2.1.4 The on-disk format (paged store format 6).** All integers little-endian, as the whole-store format's were. A POINTER is 12 bytes: page
number (4) + the generation of the write it names (8), 2.2.1.6 R3.

```
PAGE (4 096 bytes; every page)
   0   1  kind          M meta · I internal · L leaf · O overflow · F free
   1   1  flags         kind-specific (an overflow's bit 0: last of its chain; I, L, M: 0)
   2   2  entries       entry count (I, L); payload bytes (O)
   4   4  checksum      CRC-32C of every other byte of the page — bytes 0..3 and 8..4095 — the one torn-page detector
                        (2.2.1.6 R3, R6). (The first draft covered bytes 8..4095 only and left kind, flags and entries
                        unprotected: one flipped bit in a leaf's entry count passed the check. PB3331's layout golden flips
                        every bit of every page kind.)
   8   8  generation    the generation that last wrote this page (2.2.1.6 R2, R3)
  16   4  self          this page's number (a page read from the wrong offset is a torn read)
  20   4  next          O: the next overflow page of the chain; I, L, M: 0 (no sibling pointers: see below)
  24   2  deficit       I, L: the tree's PREFIX DEFICIT g on its ROOT page (the high-water marks, below); 0 on every other page
  26   2  overhang      I, L: the tree's CONTRACTION OVERHANG c − 1 on its ROOT page; 0 on every other page
  28   4  reserved      0
  32  ..  body

META (page 0 and page 1 = the two COMMITTED slots, written alternately; page 2 = the WORK slot; 2.2.1.6)
  32   8  magic "CBNFSTR" + format version 6          (the whole-store header is version 5 with its magic at offset 0, so it is
                                                     Foreign here; a page carrying this magic and version whose committed
                                                     slots both fail is `StoreFormat.Damaged`: R6's '30', not GR10's '39')
  40   1  organization  'R' relative · 'I' indexed
  41   1  record type   'F' fixed · 'V' varying
  42   4  minimum logical record size · 46 4 maximum logical record size   (§9.1.6, as the whole-store header)
  50   2  key count     0 relative; prime + alternates indexed
  52   8  generation    the coherence stamp (2.2.1.7); one more per statement that changed the file
  60   8  release mint  an indexed store's next release ordinal (kb/Work PB2693); 0 relative
  68   8  page count    pages in the file (the allocation frontier)
  76   8  transaction mint   the next transaction id (2.2.1.10)
  84  12  root[0]       POINTER to the prime tree (indexed) or the record-number tree (relative)
  96  12  free root     POINTER to the FREE tree (page number → nothing), the same B+ tree engine
 108  12  ledger root   POINTER to the LEDGER tree (2.2.1.6): page number → (generation of last write, state)
 120  12  transactions root  POINTER to the TRANSACTIONS tree (2.2.1.10): transaction id → POINTER to its UNDO tree
 132   8  committed generation   the generation of the promotion this slot's tree descends from (in a committed slot,
                        its own generation; in the work slot, its BASE: the ledger lists the pages allocated since it,
                        and R6 classifies the work slot by it)
 140   8  stamp mint    the highest generation CLAIMED on this history (2.2.1.7; finding R3-4): a changing statement
                        writes page 2 with `stamp mint` = its generation BEFORE its first page write, so a live run
                        unit that continues the history after a death never stamps a generation a dead statement
                        stamped; equal to `generation` after a completed statement, above it after a dead one
 148   1  flags         bit 0 PROMOTING: a promotion's claim set it and the retirement's page 2 clears it (R5), so a
                        coherent statement that finds it set under the mutex completes the promotion; the other bits 0
                        (the first draft's CLEAN CLOSE bit is gone: a writable OPEN fences whether or not the last close
                        was clean, R6, so no bit need prove one)
 149   8  committing    the transaction id a COMMIT's promotion commits (2.2.1.10; round 4's self-attack): set by that
                        promotion's claim, carried into the committed slot by its slot write — the one durable write that
                        decides the COMMIT — and cleared by the retirement that removes the entry; a fence copies it from
                        the committed meta it adopts; 0 otherwise. Meaningful in a committed slot; in page 2 it is a claim
 157  12  holdings root POINTER to the HOLDINGS tree (2.2.1.10; finding R4-1): (kind, identity) → transaction id — every
                        record identity and every unique key value an open transaction holds, kept in the file so that a
                        dead owner's locks survive its handle
 169  ..  per key, its descriptor, written by the ONE key codec
           (`MetaPage.WriteKeyDescriptor`/`ReadKeyDescriptor`): segment count(4) and per segment offset(4) length(4)
           (kb/Work PB1099's SOURCE-phrase windows), duplicates(1), suppress(Int32 biased code-unit count + UTF-16 code
           units, kb/Work PB1602), collation-length(1), collation(ASCII fingerprint); an ALTERNATE key's descriptor is then
           followed by its root(12), the POINTER to its tree (the prime key's tree is root[0] at 84, so the prime key's
           descriptor carries none)
   ..  the per-key descriptors are bounded by the page: 3 927 bytes hold at least 50 keys of 64-byte descriptors; a
       declaration past the page's capacity is refused at the OPEN with '39' (DOC-A.1-129 names the limit)

INTERNAL (I)   body: a leftmost child POINTER(12) at 32, then entries × { key length(2), key bytes, child POINTER(12) } ascending
LEAF (L)       body: entries × { key length(2), value length(4), key bytes, value bytes } ascending;
               a value whose length word has bit 31 set is an OVERFLOW REFERENCE (its low bits 16, the reference's own
               size): a POINTER(12) to the first overflow page + 4 bytes total length; each overflow page's `next` is paired
               with the next page's generation in its own body's first 8 bytes (so the chain is stamped like every other
               pointer)
OVERFLOW (O)   body: 8 bytes next-page generation, then up to 4 056 payload bytes, chained by `next`
FREE (F)       body: unused (a free page keeps its header so a stale read is recognizable)
```

- **Keys are BYTES in the file's channel character set** — the same `FileCharacterSet.Medium` bytes the frame payload carries
  (kb/Work PB690; one byte per character position, CODE-SET conversion applied, 2.2.1.5) — and the tree compares them through a
  KEY ORDER chosen once per tree at OPEN (`IKeyOrder`): ORDINAL bytes for a native collating sequence, WEIGHTED (decode, then
  the key's `CobolCollation` as `FreezeOpenSequences` fixed it at the OPEN, §12.4.5.7.4 GR1) for a declared one, NUMERIC for a relative record
  number (stored as 8 bytes big-endian, so ordinal byte order IS numeric order and the ordinal comparer serves), and COMPOSITE
  for an alternate tree (the key's order, then the 8-byte big-endian ordinal). A START's partial key (§14.9.41.4 GR17 b) sets its
  length, c) and d) truncate each stored key to it, e) compares them by §8.8.4.2.7's equal-length rule) is a
  seek to the prefix under the same order; the '02' look-ahead of §14.9.30.4 GR27 is the next entry's key prefix. §14.9.41.4
  GR17 c) and d) define that seek as a SEQUENTIAL search (forward, or in reverse for the LESS relations), and the tree narrows it by
  binary search wherever the qualifying entries are one run of the key order (`IKeyOrder.PrefixNarrows(length)`): at every
  length under an order that compares position by position — the bytes, and a sequence under §8.8.4.2.7 1)'s standard
  comparison (`CobolCollation.UsesStandardComparison`) — and under a LOCALE-BASED sequence at the key's whole length only:
  a START key as long as the record key (GR17 b)) has nothing truncated by c) and d), so the prefix comparison IS the key
  order and every whole-key START reads the tree's depth (a leaf or two more where it settles across a leaf boundary). The
  `WeightedKeyOrder` carries the key's length for that and refuses a longer key or prefix (GR14 bounds a START's length by the
  key's). A SHORTER partial key under a locale-based sequence is not one run (§8.8.4.2.11, *"not necessarily performed
  character-by-character"*: under the root locale "áa" < "ab" while "á" > "a"; review w1122-a R1-1), so it is GR17's
  sequential search, started at a PRIMARY-LEVEL BOUND and ended at the end of the run that ties it (review w1122-a R2-1,
  R3-1; the Mythos design decision of 2026-10-18):
  - **Two high-water marks per tree**, on the ROOT page's header (bytes 24..27 above), raised by every INSERT
    (`IKeyOrder.MarksOf`, once per inserted key) and never lowered by a DELETE, so they hold for every key the tree holds:
    the PREFIX DEFICIT g, the largest (cut − level-1 weights before it) of any key, and the CONTRACTION OVERHANG c − 1,
    the longest run between two consecutive cuts of any key, less one (`Collator.PrefixShapeOf`; a CUT is a position no
    mapping of UTS #10 S2.1 straddles — a code point, a Hangul syllable, the longest contraction, a discontiguous one to
    its furthest mark — that begins with a starter, so the walk of the text before it is the walk of that part on its own;
    a text walked in NFD is measured in its own positions through the normalizer's origins). An ignorable character (a
    control, a soft hyphen, a variable under shifted alternate handling) and a contraction raise g, an expansion lowers it;
    ordinary text has g = 0 and c = 1. Both are taken over the operand §8.8.4.2.11 compares — *"trailing spaces are
    truncated from the operands except that an operand consisting of all spaces is truncated to a single space"* — so a
    key's padding counts for nothing. The marks live on the root because PATH COPYING rewrites the root at every change of
    the tree: they are crash-consistent with the root the meta names by the same shadow paging as every node, travel with
    the root another run unit's statement left (`BTree.Adopt`, 2.2.1.7), and need no meta field and no wiring per tree.
  - **The bound.** For a probe of k positions, m = max(0, k − g − (c − 1)), and B = the first min(m, |Pp|) level-1
    weights Pp of the probe's own key, cut before the first weight below a space's (`IKeyOrder.BoundOf`). Every stored
    key's first |B| level-1 weights are then the first level-1 weights of its truncation to k (the last cut s at or
    before k lies at or after k − c + 1, the mappings before s are the truncation's own, and they yield at least
    s − g ≥ m weights), so "the stored key's first |B| level-1
    weights against B" (`IKeyOrder.CompareBound`) is a fixed-length comparison, monotone in the tree's order, whose
    answer below (above) B means a truncation below (above) the probe at level 1. The space cut is the trimming's: a
    truncation that ends in spaces of the key's interior loses them, so it is a proper prefix of the probe at level 1,
    while the stored key's own space weights meet the bound — and a bound weight below a space's would place that key
    above the bound while its truncation is below the probe. (A tailoring with a contraction that holds a space gives
    every key the deficit of its whole length, so every partial key takes the empty bound.)
  - **The walk.** FORWARD relations (EQUAL, GREATER, NOT LESS) seat at the first entry at or above B, REVERSE relations
    (LESS, NOT GREATER) at the last at or below it, both by the tree's binary descent (`Cursor.Search`); the walk is
    GR17's, entry by entry in the search's direction with the exact GR17 predicate (`ComparePrefix`) deciding, and it never
    leaves the RUN (the entries that tie B): an entry past the run's end has a truncation above the probe, so it is the
    answer of GREATER and NOT LESS when nothing in the run qualified and ends EQUAL with none (GR17 e) 2.); an entry before
    the run's start has a truncation below it, the answer of LESS and NOT GREATER. Inside the run no bound exists in this
    tree: its entries are ordered by their CONTINUATION past k before their lower levels, prime keys are unique so almost
    every continuation is its own group, and a qualifying variant ("SMÍTH…" above "SMITH") can sit in any group — the run
    is exactly the entries whose truncation ties the probe at level 1, the ones GR17 c) and d) must examine at the lower
    levels, and reading it is the optimum for this tree (only a second index, written by every INSERT, REWRITE and DELETE
    of every locale-keyed file, could beat it; `Cursor.Search` is the seam it would plug into). So the search is exact for
    every table and every key, reads the depth for every full-length probe and for every partial probe that no entry
    ties at the primary level, and the depth plus the leaves of the run up to its answer for one that entries tie; no
    statement reads the whole file unless the whole file ties the probe's base letters. m = 0 (a probe within the
    margin, or a tree whose marks are large) gives the empty bound, which is GR17 from the first (or last) entry as
    written; ordinary text (g = 0, c = 1) never takes it.
  - **The cases** (`LocalePartialStartTests`): the model (GR17 as written, `FindIndex` / `FindLastIndex` over the sorted
    keys) against the engine for every relation and every partial-key length, over key sets of accent and case variants,
    embedded controls and soft hyphens, interior and trailing spaces and tabs, expansions ("æ", "ß"), Czech "ch" and
    Danish "aa" cut by the truncation, Hungarian's doubled digraphs (three characters, two primaries: the overhang's case)
    and a TAB where a stored key's interior spaces fall to the trimming (the space cut's case), and through the composite
    order of an alternate tree; the pages (ordinary text reads the depth and the leaves its run crosses; a probe above
    every key the depth alone; a run of variants its variants ahead of the answer); the marks on the root page and zeros
    elsewhere (`AuditShape` checks it in every model run of `BTreeModelTests`), across CLOSE and OPEN, a DELETE and the
    tree's emptying; a statement that raises them killed before or after, or refused at, every one of its writes
    (2.2.1.16: the next OPEN finds the previous tree with its marks or the new one with the raised marks); a refused read
    at every read of the search ('30', the retry answers GR17's entry); and another run unit's statement adopted between
    two of this one's (2.2.1.17).
- **An INDEXED record is n + 1 entries, one per tree.** WRITE inserts the frame under the prime key and, for every alternate
  key whose value is not the key's SUPPRESS WHEN value (the §12.4.5.6.4 GR6 suppression; §14.9.30.4 GR21 c): such a record *"is not
  considered to exist"* under that key), an entry (alternate key, ordinal) → prime key, every ordinal the one fresh mint value (§14.9.51.4 GR40). REWRITE
  replaces the prime entry's value and, for each alternate key whose value or suppression changed (`AltChanged`, the one
  definition today), deletes the old composite entry and inserts the new one with a fresh ordinal — which is §14.9.35.4 GR24
  a)/b) by construction: *"When the value of a specific alternate record key is not changed, the order of retrieval when that
  key is the key of reference remains unchanged"*, and a changed key's record is last among its duplicates because its ordinal
  is the largest. DELETE removes n + 1 entries. A sequential READ under an alternate key walks that tree and takes the record by
  one prime lookup; the record's ordinal vector in the frame is what locates its alternate entries for REWRITE and DELETE, and
  a reload-time renumbering (PB2693's defect) cannot exist because nothing is renumbered. The n + 1 entries change in ONE
  statement of the page layer (`KeyedStore.Change`: every tree edited in memory, one reservation sized to all of them, every
  root set, page 2 last), and the statement's ordinal is the one `PagedFile.ReleaseMint` names after the statement's entry,
  which `PagedFile.Begin` then claims (in the tree: `IndexedConnector` over `IndexedStore`, kb/Work PB3334). The frame
  carries one ordinal per key the META PAGE records, and an alternate entry is a record's only while the record still
  carries the entry's ordinal under that key: the opt-out of DOC-A.1-129 (`WISEOWL_COBOL_KEYCHECK=OFF`) lets a description with a
  different key table drive the file's trees by key number, a tree it does not declare is not maintained by its statements,
  and such a tree's stale entry is passed over by every reader instead of naming a record that no longer has it.
- **A RELATIVE record is one entry.** The record number is the key; `Highest` (§14.9.51.4 GR29 a), *"one greater than the
  highest relative record number existing in the physical file"*) is the tree's last key; an empty slot costs nothing, so the
  sparse case of kb/Work PB1192 (one record at a key in the billions) is one leaf. START relational operators are seeks;
  READ NEXT / PREVIOUS are cursor steps.
- **A record larger than a leaf can hold inline goes to overflow pages**: a leaf entry keeps a value inline when the value is at
  most 1 016 bytes (a quarter of the body, Berkeley DB's `page_size / (minkeys × 2)` with two keys per page) AND the whole entry
  (6 + key + value bytes) is at most half a body, 2 032 bytes, and otherwise an overflow reference of 16 bytes; the frame is read
  from the chain. The second condition is the measured correction (kb/Work PB3332): with the first alone a 1 016-byte key and a
  1 016-byte value made a 2 038-byte entry, two of which do not share a page, so a split could not always make two pages; with
  both, any two entries share a page and every split is a two-way split. So the maximum record size is the host's, not the
  page's. Keys are bounded by the leaf: a prime key longer than 1 016 bytes, or an ALTERNATE key longer than 1 008 (its tree's
  entry key is the key and then the 8-byte ordinal — the measured correction of kb/Work PB3334; the first draft bounded every
  key at 1 016 and a 1 009-byte alternate key reached the tree as its caller's defect), is refused at the OPEN with '39' as
  a fixed-attribute conflict the format cannot hold (DOC-A.1-129), in every open mode, OUTPUT included, before the file is
  touched (`KeyedConnector.FormatHolds`); the tree itself refuses one as its caller's defect (`BTree.MaxKeyBytes`).
- **How the tree changes (kb/Work PB3332, `BTree`).** A statement's changes to a tree are made IN MEMORY first — the path to each
  changed entry copied, nodes split, merged or rebalanced — and FLUSHED once: every changed node is written exactly once, children
  before parents, to the statement's reserved pages, and every page the tree stopped referencing is handed to the ledger
  (`released` or `freed`, R2). So R1 holds by construction, no (page, generation) pair is written twice however many changes a
  statement makes to one tree (R3's corollary), and both the pages a statement will write and the pages it stops referencing
  are known before it begins (`StatementBound.Of`), which size its reservation: its own pages, and the LEDGER pages its
  releases take — a DELETE or shortening REWRITE of a committed record of k overflow pages adds k `freed` entries, so a
  bound from the reserved pages alone ran out of ledger pages on a 4 MB record (PB3332's branch review R1-1; a 1 000- and
  a 2 069-page record are cases, the second killed and refused at every write). A node is split when it passes a page body — where the larger half is smallest, or, for an
  APPEND above every key of the tree, just past what fitted before (Berkeley DB's rule at the right edge), so a sequential load
  fills its pages — and is merged with a sibling, or borrows from one, when a change SHRANK it below a quarter of a body (a page
  an append left small is left to fill). **There are no sibling pointers**: a leaf's `next` is 0, because path copying cannot
  keep them — a rewritten leaf's left neighbour would still name the superseded page, and re-pointing it would copy every leaf
  to its left, which R1 forbids doing in place. A cursor holds its root-to-leaf path and crosses leaves by climbing it to the
  first ancestor with a further child (so READ NEXT reads ≤ 1 page within a parent and ≤ d − 1 across one, 2.2.1.11); a cursor
  whose tree changed since it was positioned re-seeks from its current key: another statement replaced the root
  (`BTree.Adopt` takes the root a coherent entry adopted, 2.2.1.7), so it never walks a superseded path whose pages may have
  been reused, or the cursor's own run unit edited, flushed or discarded the tree — detected by the tree's CHANGE COUNTER,
  never by comparing roots, because a second edit of a node the pending edit already copied changes it in place under the
  same root (PB3332's branch review R1-2: the root comparison stepped through shifted lists, an entry returned twice or
  skipped). A torn or stale page met by a read answers '30' and discards the tree's pending edit; a read the medium REFUSES
  answers '30' too and is no verdict on the page — the resident LEDGER's load reports which (`PageCheck.Unreadable`), so R6
  step 3 never rejects a DESCENDANT for a refusal (PB3331's R1-2 on the tree; the tree's every read point is a refused-read
  case, `BTreeCrashTests`). The LEDGER is a RESIDENT instance (every node in memory, read whole at each
  adoption, R6 step 3 needing all of it anyway); every other tree reads the pages a walk needs.
- **Free space.** A page leaves a tree in one of two ways and the LEDGER (2.2.1.6 R2) records which: a page the COMMITTED tree
  references is `freed` and quarantined until the promotion that makes its freeing durable (R4); a page allocated since the
  committed generation is `released` and reusable by any LATER statement (R1 keeps the work meta's own tree intact). At a
  promotion the freed and released pages move to the FREE tree. An allocation takes a released page, else the lowest page of
  the FREE tree, else the page at `page count` (the file grows). The file never shrinks of its own accord; `OPEN OUTPUT`
  creates a new three-page store (§14.9.27.4 GR18, *"After the creation of the file, the file contains no records"*) through
  the connector's own handle as a PROMOTION OF THE EMPTY STORE, decided by one durable write: the three meta pages are read
  (and kept); the empty meta is given the generation N = the fence's bound over every meta page that decodes
  (max(generation + 4 097, `stamp mint` + 1), 0 when none does), so the new store never reissues a stamp the previous one
  wrote (R3) — written into the slot that does NOT hold the previous store's committed meta, and SYNCED (the commit point);
  then page 2, the other slot, `RandomAccess.SetLength` to three pages — the one truncation (the deleted whole-store `Persist`'s truncation,
  moved) — and a sync. Until the commit write is handed over the previous file is whole; after it the empty store is the
  committed meta. A REFUSAL at any point but the last sync is §14.9.27.4 GR25's unsuccessful OPEN, *"the file is not
  affected"*: the meta pages written so far get their previous bytes back, newest first, a file shorter than three pages its
  previous length TO THE BYTE (the medium's length is in bytes, `IPageMedium.Length`: a format-5 whole store or a foreign
  file need not be a whole number of pages, and a length kept in whole pages cut its tail off — branch review R2-1), then
  one sync, and the OPEN answers '30'; a refused last sync follows the truncation, so nothing is left to
  undo and the medium's state is unknown, as at every refused sync (2.2.1.16). (The first draft set the length to three
  pages, which kept the old store's three meta pages until they were overwritten: a death between the truncation and the
  first write left the OLD store's committed slots valid over a file cut to three pages, and the next OPEN adopted a tree
  whose pages were gone. The second, `SetLength` to ZERO first, emptied the file before anything was decided, so every
  refusal after it destroyed the previous store against GR25, and the new store's stamps restarted at 1 over pages the
  previous store had stamped. PB3331's creation cases kill and refuse the creation at every write, sync and the truncation,
  over a previous store and over a file with none.)
- **What the pages prove at a glance.** An internal page holds ⌊(4 064 − 12) / (2 + k + 12)⌋ keys of k bytes and one child
  more; a leaf holds ⌊4 064 / (6 + k + v)⌋ entries of v-byte values. With 22-byte internal entries (an 8-byte relative key and a
  12-byte pointer) a page holds 184 keys and 185 children, and a sequential load's append splits leave every internal page but
  the rightmost of its level with 184; a leaf of 8-byte frames holds 184. So such a tree is 1 level to 184 records, 2 levels to
  185 × 184 = 34 040, 3 levels to about 6.3 million and 4 to about 1.15 billion — the boundaries PB3332's shape test measures
  (184 → 1 level, 185 → 2, 34 040 → 2, 34 041 → 3). A realistic 80-byte record frame gives 39 per leaf: 3 levels hold about
  1.3 million records, 4 levels 245 million. With 40-byte prime keys an internal page holds 75 keys and a leaf 16 records of
  200 bytes: 3 levels cover 90 000 records, 4 levels 6.7 million, 5 levels 500 million. These are FULL pages, a sequential
  load's; random inserts leave pages about two-thirds full on average, which costs a level only near a boundary. Every level
  but the leaf fits a cache of a few hundred pages, which is why a READ is one page of I/O in the steady state and O(log n)
  only cold (2.2.1.11).

**2.2.1.5 The code set and the character set.** The frame payload and the key bytes are the channel bytes of today's
`FileCharacterSet.ToChannel` + `Medium.GetBytes` (one byte per character position; a character with no byte image is refused at
the WRITE with '91', DOC-A.1-110, before any page is touched), and a READ decodes them with `FromChannel` (§13.18.13.4 GR6 a):
the frames come back in the native character set). The meta page, the page headers, the key lengths and the ordinals are the
§9.1.7.2 information *"the implementor may add to the record on the physical storage medium"* and are never converted (the same
rule the whole-store codec applied to its length word and header). A declared COLLATING SEQUENCE orders the tree through the
weighted key order; its fingerprint in the meta page is what makes a file created under one sequence refuse an OPEN under
another ('39', §14.9.27.4 GR10 — *"The implementor defines which of the fixed-file attributes are validated during the
execution of the OPEN statement"*), exactly as today, because an index ordered under one sequence cannot be searched under
another.

**2.2.1.6 Crash consistency: the generation ledger.** The whole scheme is six rules; each has a drift test (2.2.1.12), and
2.2.1.15 tabulates every structure they write against every reader of it. The round-1 review (2026-10-17) found the first
draft's R3 (a fresh page rewritten in place) and R5 (page 0 overwritten in place) broke the bar; the round-2 review (the same
day) found the corrected promotion left the work slot on the old base (R2-1), a rejected history's generations could be stamped
again (R2-2) and an INPUT adopter's coherence contradicted its adoption (R2-5). The rules below are the corrected ones; each
finding's evidence is under the rule that answers it (R1, R2, R3, R5, R6 and 2.2.1.7).

- **R1 — a page that EITHER slot's tree references is never written.** The committed slot names a tree whose every page was
  synced to the medium at the promotion that wrote it; the work slot names the tree of the last COMPLETED statement. A
  statement that changes a page allocates another page, writes the new content there and re-points the parent, up to the root
  (path copying; the root's new pointer goes into the WORK meta). This holds for every tree, the ledger included. *The first
  draft rewrote in place a page allocated since the committed generation (its "R3") on the ground that the committed tree does
  not reference it; the review's scenario (finding R1-1) is decisive: 3 000 sequential WRITEs with no promotion, then a death
  during WRITE 3 001 after the rightmost leaf was rewritten in place and before the work meta — the work meta of statement
  3 000 names that leaf at generation 3 000, the page says 3 001, R6 rejects the work slot and adopts the committed one, and
  3 000 WRITEs that answered '00' are gone. Writing in place anything the last completed statement references cannot meet the
  bar, so nothing referenced is ever written.* The price is one path copy per tree per statement (2.2.1.11): a sequential
  relative WRITE writes d + 3 pages instead of 3, 50 µs instead of 25 at the measured 8 µs a page.
- **R2 — every page allocated since the committed generation is in the LEDGER with the generation of its last write and its
  state: `fresh` (referenced by the work tree), `released` (allocated since, no longer referenced; reusable by any statement
  later than the one that released it), `freed` (a committed page released since; quarantined, R4).** The ledger is a small
  B+ tree of the same engine whose root pointer is in the work meta; it is path-copied by the statement that changes it, like
  every tree, so the work meta and the ledger always agree. The ledger does not list its own pages: they are verified through
  their stamped pointers (R3) when the ledger is walked. The pages of the ledger a statement SUPERSEDES — the previous
  statement's ledger, allocated since the base — are `released` in the new one, and a `released` page the statement reuses
  for its own ledger leaves the list (PB3331's first simulator run: without the removal the ledger listed its own page as
  reusable and the next statement overwrote it while page 2 referenced it). **Allocation never happens while the ledger is being written:** a
  statement reserves its pages first — the released pages the in-memory ledger offers, then one batch of the lowest FREE-tree
  pages sized to the statement's bound (2.2.1.11) — its own pages plus the FREE-tree and LEDGER pages it rewrites, the
  ledger's sized by one entry per page reserved and per page the statement's edits stop referencing (`StatementBound`) —
  then growth — writes its data and free-tree pages, and writes the ledger LAST from the reservation; a reserved page the statement did not use is recorded as `released`, never lost. **The
  GENERATION CEILING:** a statement whose generation — its CLAIM, 2.2.1.7 — would exceed the committed generation + 4 096
  (`GenerationCeiling`, a format constant) promotes first — R5's statement-count checkpoint, stated as a rule of the format —
  so every stamp a history based on committed generation G issues lies in (G, G + 4 096], the stamps of a statement that died
  with its pages written and its meta unwritten included (a claim above G + 4 096 cannot be written without a promotion).
  R6's fence depends on this bound.
- **R3 — every POINTER is STAMPED with the generation of the write it names, and a read through a pointer verifies the page:
  its checksum, its `self`, and its generation equal to the stamp.** A page that is torn, lost (the medium shows an older
  version of the same page number, which a released-then-reused page makes possible) or stale is therefore recognized at the
  moment it is read, in every statement and not only at OPEN, and answered as §9.1.13.6 item 1's '30'. Stamping is what lets
  R6's verification trust the ledger's own pages and what makes a reused page safe. **Its corollary, which the stamp needs to
  mean anything: no (page, generation) pair is ever written twice in a file's history** (finding R2-2). Within one history a
  generation belongs to one statement ATTEMPT — the attempt CLAIMS it before its first page write, in the store's memory
  and, under the mutex, on page 2 (`stamp mint`, 2.2.1.7), so a later attempt stamps above it whether or not it completed:
  this run unit's from the memory a refused attempt leaves raised (finding R4-3), another's from page 2; every history
  begins at a promotion or at R6's FENCE, whose generation is above every stamp the history it supersedes could have issued
  (R2's ceiling, which bounds the claims); and the only pages written more than once — the three meta pages — are reached by
  their fixed numbers, never through a stamped pointer. *The first correction lacked the fence: a work slot rejected at
  generation G + k and a history restarted at G + 1 stamped the reclaimed pages with the generations the abandoned history
  had given them, and a lost write over one such page passed R3 with the abandoned content (the review's scenario: page 700 at
  generation 105 twice). The second correction closed that only across an OPEN (finding R3-4): on a shared file the history
  continues WITHOUT one — run unit A's WRITE reserved the lowest FREE page p, wrote it stamped g + 1 and was killed before
  page 2; run unit B's next statement found page 2 at g, the generation it last saw, stamped g + 1 and reserved p too, so
  (p, g + 1) was written twice, and a power loss that kept A's p and B's page 2 passed R3 with a record whose WRITE never
  completed and without one whose WRITE answered '00'. The claim is what makes a dead attempt visible to a live continuer: B
  finds `stamp mint` = g + 1 above the g it last saw and claims g + 2.*
- **R4 — a FREED page is not reused before the promotion that makes its freeing durable.** Until then the committed meta
  references it, and R1 forbids writing it. At promotion the ledger's `freed` and `released` pages move to the FREE tree and
  its `fresh` pages leave the ledger (they are now committed pages, R1 applies); the ledger's own pages, now referenced by the
  committed slot, are `freed` in turn (finding R3-2 confirmed this reading against the retirement paragraph, which had
  contradicted it). The moving is the RETIREMENT statement of R5.
- **R5 — a PROMOTION is: the CLAIM — page 2 := the meta being promoted AT A FRESH CLAIM P (page 2's own content for an
  ordinary promotion, with `generation` := P, the next claim; for the fence, the ADOPTED meta with `generation` = F,
  since after a rejection page 2 holds the superseded slot, 2.2.1.16), with `stamp mint` = the retirement's claim P + 1
  and the PROMOTING flag set (2.2.1.7) → sync the data file
  (`FileStream.Flush(true)` on the connector's own handle) → read both committed slots and write page 2's content, with
  `committed generation` = its generation, to the COMMITTED SLOT THAT DOES NOT HOLD THE COMMITTED META (the lower valid
  generation, or the invalid one — decided from the slots as they are NOW, under the mutex, never from memory: another run
  unit may have promoted since this one last looked) → sync again → the RETIREMENT statement, whose page 2 clears the flag
  (a page the retirement reserves for itself is `fresh` in the new ledger, never also moved into the FREE tree).**
  *Why a fresh claim (PB3331's simulator, 2026-10-18): the round-4 text promoted page 2 at its own generation g, the last
  completed statement's, with the retirement claiming above the mints. Refused statements since the base raise the mint
  without raising g, so the retirement's claim could lie above g + 4 096 — outside the window R2's ceiling gives a history
  based on g, which is what the fence at g + 4 097 relies on: a retirement that died there left pages stamped at the very
  generation the next OPEN's fence would reissue. Promoting at a claim P, above every claim before it, makes every history
  begin above everything it supersedes, and P is a meta generation only (no page is stamped P).*
  Two syncs make the meta copy strictly after the pages it names, and alternating the slots makes the one write the scheme
  depends on survivable: a promotion whose slot write is torn leaves the other committed slot whole and the work slot, synced
  by the first sync, naming the state the promotion was making durable (finding R1-4; the first draft overwrote page 0 in
  place and gave a torn page 0 no outcome). **A promotion is RESTARTABLE from its flag, and the slot decides how:** the
  promoter holds the mutex from the claim to the retirement's page 2, so a coherent statement that finds PROMOTING set under
  the mutex knows the promoter died part-way or was REFUSED part-way (a refused promotion answers '30' and leaves the flag;
  round 4's self-attack — except a COMMIT whose sync 2 is refused, which REVERSES before it releases the mutex, 2.2.1.10,
  finding R5-1, so a flag found beside a landed slot is a dead promoter's or a refused retirement's, and completing it is
  right for both), and page 2 — written last by the statement before the promotion, untouched by it but for the
  claim and the flag — is still exactly the state being promoted. The statement reads both slots. A slot that names page
  2's generation means the promotion LANDED, and the statement completes it: the syncs are not skipped, since the dead
  promoter may have died before its second one, and the retirement claims above the dead retirement's claim, so its pages
  are stamped afresh — and writes that claim to page 2 (the flag still set) before its first page write, so a death inside
  the restarted retirement is continued above it in turn; and were that claim to pass the promoted generation + 4 096, the
  promotion runs again from a fresh claim instead — a retirement that removes the transaction `committing` names, if any. No slot names it and
  `committing` is 0: the statement runs R5 from the claim — a checkpoint or a CLOSE is harmless when repeated. No slot
  names it and `committing` is a transaction t: the promotion was a COMMIT that did not land, and the statement UNWINDS
  it — page 2 := the same content with PROMOTING clear and `committing` 0, nothing else written — so that t is an ordinary
  active transaction again, intact with its UNDO tree and its holdings: its owner, alive, was told the COMMIT failed and is
  rolling back (§14.6.13.1.3 item 8) or retrying; dead, it is rolled back by the dead-owner step, which runs next. Only
  then does the statement proceed with its own work. R6 needs no flag: it classifies the same states by base and
  generation (a DESCENDANT whose promotion never reached a slot, or the PROMOTION SOURCE), and its fence applies the same
  rule to `committing` (step 4).
  **The retirement statement** (finding R2-1) is an ordinary statement — one generation (its claim, 2.2.1.7), R1-R3, page 2
  written last — that moves the promoted ledger's `released` and `freed` pages into the FREE tree (path-copying the FREE tree:
  its new pages are `fresh` in the new ledger, the FREE-tree pages it replaced `freed`), drops the `fresh` entries (committed
  pages now), records the promoted ledger's OWN pages — the pages its walk read; the committed slot references them through
  its ledger root, so R1 and R4 protect them until the next promotion — as `freed` in the new ledger (and the promoted FREE
  tree's own pages likewise), and writes page 2 with `committed generation` = the promotion's generation and the new
  ledger's root. A page at or past the promoted state's `page count` never enters the FREE tree: it is reached by nothing,
  and the next growth reuses it (a rejected history that grew the file leaves its grown pages there; putting them in the
  FREE tree as well would let growth and the FREE tree hand out the same page). *Finding R3-2: the round-2 text moved the
  promoted ledger's own pages straight into the FREE tree while R4 quarantined them; the FREE tree was wrong. The committed
  meta's ledger is read again whenever R6 adopts the committed meta (its fence retires that ledger, step 4), so a reused ledger
  page would fail R3 inside the fence of a file whose committed tree is intact; R4's reading stands, and 2.2.1.15's LEDGER row
  says so.* Until page 2 lands, the work slot still names the promoted ledger on the OLD base; that state is R6's
  PROMOTION SOURCE, and a writable OPEN (R6) or the next coherent statement of a live run unit (2.2.1.7, the PROMOTING flag)
  completes it. *The first correction's promotion never wrote page 2, so every OPEN after
  a CLOSE found a work slot whose base was older than the committed meta's, rejected it and reclaimed "the abandoned ledger's
  pages" — the committed tree's own leaves — into the FREE tree; the next WRITE then allocated a live leaf (the review's
  ten-WRITE scenario). The state after a promotion now has a name and a rule.* Promotions happen for EVERY file, under commit
  control or not: at CLOSE (which syncs once more after the retirement, so a closed file's OPEN verifies the retirement's few
  pages and nothing else), at the run unit's implicit CLOSE of every open file (`FileRegistry.CloseAll`), at COMMIT (2.2.1.10),
  as a CHECKPOINT at R2's generation ceiling or whenever the ledger holds more than 1 024 pages, and as R6's FENCE. A promotion
  never changes what a ROLLBACK restores, because the rollback target is the run unit's before-images (2.2.1.10), not the
  committed slot — which is why no file needs the first draft's "no promotion under commit control" and its unbounded ledger.
  The checkpoint bounds R6's verification at 4 MiB of reads (about 20 ms at the measured 12-15 µs a page) and costs, at the
  measured 3 ms per sync pair, under 2 µs per statement amortized.
- **R6 — the ADOPTION RULE decides which meta is the file. It runs UNDER THE STORE MUTEX** (2.2.1.7: exclusive through a
  writable handle, shared through a reader's) at the run unit's first OPEN of the physical file (a later connector of the same
  run unit attaches to the open store, 2.2.1.7, and runs none of this). Read pages 0, 1 and 2 (three reads; the whole of what
  an OPEN reads besides the verification below) and validate each checksum; then:
  1. the COMMITTED meta C is the valid committed slot with the higher generation G; if neither validates, the OPEN is
     unsuccessful with '30' (§9.1.13.6 item 1): the file is damaged beyond what the format can repair, and nothing is written;
  2. the WORK slot W is classified by its base (`committed generation`) and its generation against G:
     - a DESCENDANT: W validates and its base equals G — the last completed statement of the history built on C;
     - the PROMOTION SOURCE: W validates, its base is below G and its generation equals G — the promotion copied W into C and
       its retirement never landed; W's ledger IS C's ledger and nothing is abandoned;
     - FOREIGN: anything else — W torn; a base below G with another generation (a fence's slot write landed and the page 2
       its claim wrote did not — a power loss between the two, since a process death keeps both in order — so W is the slot
       that fence superseded, and C carries the ledger the retirement will retire); a base above G (damage).
       Nothing of W is trusted or reclaimed — when W was a rejected DESCENDANT the fence superseded, its never-committed pages
       leak, bounded by the checkpoint, like an unreadable ledger's;
  3. a DESCENDANT whose `fresh` pages all verify (R3, each against the ledger's generation) is ADOPTED — the file as the last
     completed statement left it, which is what a process death leaves (the operating environment keeps what was handed to
     it) and what a power loss leaves when every write reached the medium; a DESCENDANT with a page that fails is REJECTED
     and C is adopted — a power loss tore a page or lost a write; W's `fresh` and `released` pages were never committed and
     are reclaimable when its ledger is readable; the PROMOTION SOURCE and a FOREIGN W give C as well;
  4. a connector with a writable handle then FENCES (finding R2-2): a promotion (R5) of the ADOPTED meta at generation
     F = G + 4 097 — above every stamp a history based on G can have issued, R2 (and above every mint a slot or page 2
     still shows, which a valid history never puts higher) — into the slot that does not hold C, with
     its claim (page 2 := the adopted meta at generation F, PROMOTING set, `committing` = C's when C is adopted and 0 when
     W is — a nonzero `committing` in a DESCENDANT W is a COMMIT's claim that never landed, and this fence unwinds it as R5's
     restart rule does; a nonzero one in C is a COMMIT whose slot landed and whose retirement did not, and this fence's
     retirement finishes it: an INPUT connector that held C in memory adopts
     the same tree from page 2 at that moment, and a later OPEN classifies that page 2 as a DESCENDANT of C with the fence
     unlanded, or as the PROMOTION SOURCE once the slot landed) and its two syncs, then the retirement statement on base F (its generation is its claim, F + 1 or above), which also moves a
     rejected DESCENDANT's `fresh` and `released` pages, and that ledger's own pages (read by the walk; no slot references
     them once the fence's slot write landed), into the FREE tree when that ledger is readable — those below C's `page count`;
     the pages that history grew are reused by the next growth (an unreadable one leaks at most
     the checkpoint's pages; the file never shrinks of its own accord anyway). After the fence both the committed meta and page 2 name the adopted
     state, every page of it is durable, and no generation the superseded history may have stamped will be stamped again. A
     fence is a promotion like any other and changes no rollback target; it is harmless when repeated. It runs through the
     store's FIRST writable handle — the OPEN that gives the store one, even when an INPUT connector of the run unit opened
     the file first. A connector with an INPUT handle adopts in memory, writes nothing, and reads through 2.2.1.7's rule,
     which keeps the adopted meta's roots until page 2 changes (finding R2-5);
  5. then the DEAD-OWNER step of 2.2.1.10: the TRANSACTIONS tree is read and every transaction whose owner is dead is rolled
     back by a writable connector; a transaction whose owner is alive is left exactly as it stands. A COMMIT the fence
     finished (step 4) has no entry left to probe. The same step runs at the
     coherent entry of EVERY later statement through a writable handle (2.2.1.7; finding R3-1), so an OPEN is one of its
     occasions, not the only one.
  Neither outcome is torn, because the committed tree's pages were synced before the meta that names them (R5) and nothing
  wrote them since (R1, R4), and a work tree is adopted only when every page it changed is verified present and whole.
  **R6 never runs recovery on a live file** (finding R1-3): a live holder's statements run under the mutex and write their
  pages before their meta, so at any instant the mutex is free the work slot is a complete, present DESCENDANT; a second run
  unit's writable OPEN therefore adopts the live holder's last completed statement and fences it, which the live holder's next
  statement adopts in turn through 2.2.1.7 (page 2 changed under the mutex). The first draft's "under commit control, adopt
  the committed slot" branch — the one that rewrote a live file's work slot from the committed one when a second run unit
  opened it — no longer exists, because commit control is a transaction in the TRANSACTIONS tree, not a property of the file.
  2.2.1.12's matrix has the two-run-unit OPEN as a row.
- **A statement is atomic by the same rules.** Its page writes land only on pages no slot references; the work meta is
  written LAST. A death or a host refusal part-way (a full medium, a handle the host revoked) leaves the work meta naming the
  previous statement's tree, whose pages were never touched (R1); the statement drops the cache and answers '30' (§9.1.13.6
  item 1, *"A permanent error exists and no further information is available concerning the input-output operation"*), and
  §9.1.13.1's permanent error is in effect from there — the behaviour the coherent entry (`KeyedConnector.Enter`) gives a
  refused page-2 read, for every connector. The generation the dead or refused statement stamped is never issued
  again: it claimed it first — in the store's memory, and on page 2 under the mutex — so this run unit's next statement
  claims above it (its CLOSE's promotion and retirement included: finding R4-3), any other run unit's does (2.2.1.7), and
  the next OPEN fences above the whole history (R6). The same holds of every other identifier the attempt drew: a
  transaction id was DRAWN by its byte and claimed on page 2 before the attempt wrote a page, so a refused first change
  leaves `transaction mint` past it and gives the byte back (finding R4-2; 2.2.1.10), and a release ordinal, carried only
  by pages no pointer reaches, is claimed with the rest for one rule's sake. **A sync that fails** (`EIO`: the operating
  environment may have dropped the dirty pages it could not write) is not a refused page write: the statement answers '30'
  and the connector is in permanent error, but the medium's state is not known — the same as a power loss at an unknown
  instant — and R3 and R6 decide it at the next read or OPEN. Nothing the design promises across a power loss is promised
  across a failed sync, and nothing less: the file is never torn. The one sync whose failure the run unit must ANSWER for
  is a COMMIT's second (its transaction's durability is the promise the answer makes), and 2.2.1.10's UNACKNOWLEDGED-COMMIT
  rule says how: reverse under the same mutex hold, or answer unknown (finding R5-1).
- **Why not a write-ahead log.** A log is a second file (a sidecar, refused by PB802) or a region inside this one that needs
  its own wrap-around, checkpointing and replay; shadow paging needs none of those, and its recovery reads only the pages the
  ledger names. The price is R4's growth between promotions, bounded by the checkpoint for every file — the price every
  shadow-paging store pays.

**2.2.1.7 Coherence across run units, at page granularity (the successor of the PB2660 bullets above).** The three pieces keep
their places; what each does changes:

- **The generation lives in the WORK meta** (offset 52), stamped by every statement that changed the file (one more than the
  generation it read). **A connector whose sharing mode admits ANY other run unit's connector — every mode but `SHARING WITH NO
  OTHER`** (`FileLockPosture.AdmitsAnotherRunUnit`; the keyed connector's `_coherent` is derived from it, 2.2.1.13) — runs
  every record statement, and its OPEN's adoption rule (R6), inside the store mutex (`HostRegionLocks.StoreMutexByte`,
  exclusive through a writable handle, shared through a reader's), and enters by reading page 2 — ONE page read; a page 2
  that does not validate under a live store is answered '30' by a writer, which cannot know what another run unit
  completed, and leaves a reader on the tree it holds, since nothing it can follow moved. The READER is the entry, not
  the store: an INPUT connector's statement entering the writable store its run unit's I-O sibling opened holds the mutex
  shared and writes nothing, so it keeps the tree it holds and answers as another run unit's reader would, and the
  sibling's next statement, an exclusive entry, answers the '30' (§9.1.13.6 item 1). A page 2 the medium refuses to READ
  is '30' to either (2.2.1.16's refused READ row). *Measured (PB3335 review lead R1-L1;
  `StoreCoherenceTests.An_input_readers_entry_beside_its_writable_sibling_keeps_its_tree_over_a_torn_page_2` and
  `..._answers_30_to_a_refused_page_2_read`).* — and
  comparing three of its fields with THE ONES THE STORE LAST SAW THERE (at its OPEN, or at its own previous statement, which
  wrote it). **`generation`** equal: nothing moved, and the statement proceeds from the meta the store holds. Different:
  another run unit's completed statement, retirement or fence wrote page 2 under the mutex, so page 2 names a complete tree
  (R6's argument for a live file); the store ADOPTS page 2 as its meta — its roots and its ledger replace the ones in memory,
  the ledger's `released` entries being the only reusable pages now — records its generation as the last seen, and DROPS the
  run unit's page cache for the file instead of reloading anything. **`stamp mint`** above the generation last seen (finding
  R3-4): a statement of another run unit claimed that generation and never completed — it died, or a host refusal answered
  it '30' — and its pages may lie on the medium under that stamp, so the store's next claim is `stamp mint` + 1; the trees did
  not move, and nothing is dropped. **PROMOTING** set: the promoter died or was refused between its claim and its
  retirement's page 2, and the statement resolves it first — completes a promotion whose slot landed, runs an unlanded
  checkpoint again, unwinds an unlanded COMMIT (R5's restart rule) — before the dead-owner step, so a COMMIT's fate is
  settled before its transaction is probed. ⛔ Only an EXCLUSIVE entry resolves it: an INPUT connector of a run unit whose
  writable sibling opened the store enters that writable store under its own SHARED hold (a read-only handle cannot hold
  the mutex exclusive), adopts page 2 as a writer does, and leaves the flag to the next exclusive entry or the CLOSE
  (`PagedFile.Enter(exclusive)`), since two run units' readers resolving it under shared holds would claim from the same
  mints and write the same slot and page 2 interleaved, breaking R1. *Measured correction (PB3333 review, finding R1-2;
  `RelativeEngineTests.An_input_connector_beside_its_writable_sibling_reads_a_promoting_page_2_and_writes_nothing`).*
  Then a statement that CHANGES the file CLAIMS, in the
  store's memory first and then on page 2: it writes page 2 once more with `stamp mint` = its generation and `transaction
  mint` and `release mint` advanced past every id and ordinal it will mint (a first change under commit control has
  already DRAWN its id by taking the id's byte, 2.2.1.10), the roots, base and `generation` unchanged (page 2 still names
  the last completed statement's tree), before its first page write; a READ claims nothing, since it stamps nothing. The
  store's next claim is one above the highest of the generation last seen, page 2's `stamp mint` and ITS OWN LAST CLAIM,
  which a refused statement leaves in memory (finding R4-3); every page 2 the store writes — a completion, a promotion's
  claim, a retirement — is written from that memory, mints included. The meta
  the store holds is R6's choice at OPEN — which, for an INPUT connector that opened a crashed file, is the COMMITTED meta
  while page 2 still holds the rejected or foreign slot — and page 2 only from the moment page 2's `generation` changes
  (finding R2-5: the first correction compared page 2's generation with the adopted meta's, so an INPUT reader of a crashed
  file abandoned the committed tree at its first statement and walked the tree R6 had just rejected, down to the torn page
  and a '30' on a file its OPEN had accepted); an INPUT connector ignores the mint and the flag, which concern writers only.
  **The DEAD-OWNER step** (2.2.1.10) runs at this entry too, through a writable handle: every transaction of the TRANSACTIONS
  tree whose liveness byte is free is rolled back before the statement proceeds (finding R3-1); and the statement's own lock
  tests read the HOLDINGS tree, where a holding whose owner died AFTER this entry's probe is rolled back at the test itself
  (finding R4-1), so the entry's probe need not be the last word. The statement then reads the pages it touches: O(log n),
  the same count as an unshared statement plus the one meta read. The record statement's own page statement writes its
  dirty pages and the work meta before the mutex is given back, so the statement is one step to every other run unit, as
  before: each WRITE, REWRITE or DELETE is ONE statement of the page layer — the tree edited in memory, `PagedFile.Begin`
  sized to exactly the pages the edit writes, the flush, page 2 last (`RelativeStore.Change`, kb/Work PB3333) — run inside
  the registry's statement and so inside the mutex, so the statement's end has nothing left to write (`StoreStatement`
  carries no completion step: PB3335 deleted the pass-through it had become). *Measured correction (PB3333): the first text put the writes at
  `StoreStatement.Complete`; the verb knows its pages and its status ('24' for the boundary, '30' for a refusal) at the
  write, and answering them there keeps one terminal per verb.* A CLOSE PROMOTES only when its store completed a
  statement since its last promotion (its OPEN's fence included) and otherwise only syncs, so a connector that changed
  nothing never fails its CLOSE over a page 2 another run unit's death left torn (PB3333, the relative rows of
  `KeyedStoreReloadFailureDriftTests`). *Why not
  only the ALL OTHER writer:* the INPUT reader Table 19 admits beside a `SHARING WITH READ ONLY` writer (§9.1.15 2),
  *"restricts concurrent access to a physical file through file connectors other than this one, to input mode"*; the
  table's `SHARING WITH ALL OTHER` / `INPUT` request against an existing `sharing with read only` / `I-O` is *Normal open*)
  must find in the file every record that writer's WRITE released at the statement (§14.9.51.4 GR12, kb/Work PB1587;
  `ReadOnlyWriterReleaseDriftTests.AKeyedReadOnlyWriter_IsReadByAnotherRunUnit_AtTheWrite`), and an engine that writes at
  every statement must put that writer's statements under the mutex, or the reader meets a half-written path. One
  condition, one path.
- **A `SHARING WITH NO OTHER` connector** (Table 19 refuses every other opener in every run unit, kb/Work PB833) runs the same
  statement path with no mutex and no generation read, and its claim is the store's memory alone: no other run unit can
  continue its history, so its only continuers are this run unit's own next statement — which claims above the memory's
  record of a refused statement's claim, its CLOSE's promotion and retirement included — and the next OPEN, whose fence
  lies above every stamp it issued. *Finding R4-3: the round-3 text exempted this connector from the claim because no
  OTHER run unit continues its history, and overlooked that the SAME connector does, through CLOSE — the technique that
  ends §9.1.13.1's permanent error (DOC-A.1-105) — which promotes and retires: the retirement at `generation` + 1
  re-stamped the lowest FREE page the refused statement had stamped with that generation, and a power loss that kept the
  retirement's page 2 and lost its write of that page passed R3 with the refused statement's content. One claim rule for
  every mode, with the memory as its carrier where no other run unit reads, closes it; the page-2 write is kept for the
  modes with a reader.* Dirty pages are written at the end of each statement all the same (the bar: *the file is in the
  state after its last completed statement*), and the cache is never dropped. The whole-store "loaded at OPEN, persisted
  at CLOSE" branch is gone, so there is ONE statement path.
- **Within a run unit** the table of OPEN STORES per physical file is `StoreTable` (kb/Work PB3335; the successor of the
  whole-store model's `KeyedStoreTable`): one store — the handle's page I/O, the page cache, the ledger and the trees —
  attached by every connector of the run unit over that path (`Attach`, `AttachCreated` with its PB754 refusal, `Detach`
  on the last CLOSE), so a sibling's change is visible instantly because there is one cache and one set of trees.
  **A path names a physical file only at the instant it is asked (kb/Work PB3533).** Each entry also remembers the host's
  identity of the file its store was opened on (`HostFile.IdentityOf` of the attaching handle: `st_dev`/`st_ino` from
  `statx` on Linux, the volume serial number and the 128-bit file id of `FILE_ID_INFO` on Windows; `IO/Sharing/HostFileIdentity.cs`,
  the runtime's one `statx` binding), and a connector attaching through a handle on ANOTHER file at the path — the first one
  removed or renamed away and another made there outside the run unit, which Linux permits under an open handle — gets a store
  of its own: the path's entry becomes the new file's, the old store stays its connectors' alone, and `Detach` is by store, so
  their CLOSEs leave the new entry alone. COMMIT and ROLLBACK tell a file's transaction by its store, never its path
  (`FileRegistry.TransactionHolders`), so both files' transactions are reached. Where either identity is unknown (a host this
  binding was not written for, such as macOS; a file system with no file ids; a store opened with no file), the path decides,
  as before. **The Table 19 arbitration and the record-lock table (`PhysicalFileTable`) are keyed by the physical file too** (kb/Work
  PB3576): a `State` knows the identity of the file it stands for (`State.Identity`, learned from the first connector handle opened
  on it, `PhysicalFileTable.Learn`) and `PhysicalFileTable.For(path, HostFile.IdentityAt(path))`, asked at the arbitration of every
  OPEN before the handle exists, finds the state BY THAT IDENTITY, whatever the file is named: a file made at the path after the
  first was removed or renamed away has a state of its own (none of the first file's Table 19 entries, file locks or record locks),
  and the first file reached under its new name meets them all. A path finds a state only while the file is absent (the state an
  OPEN OUTPUT or an OPTIONAL creation gets, which learns its file once the handle exists) or where the host cannot tell files
  apart, as before. A host may give the number of a removed file to the next file made (Linux ext4 does, at once), so a state no
  connector has open yields its identity when the OPEN that made a file learns the same one (`Learn`; a state a connector has
  open keeps it, its handle keeps its file alive): the connector that made the file and every later OPEN or DELETE FILE of the
  path then meet one state. A connector's record statements use the state its last OPEN arbitrated (`FileConnector.Physical`, never a path
  lookup), so a file replaced under an open connector keeps its own locks; the run unit's record-lock ceiling (§12.4.5.9.4 GR7) counts
  every state. Witnesses: `PhysicalFileTableIdentityTests` (every host) and `CommitRollbackRuntimeTests.Two_connectors_on_a_path_whose_file_was_replaced_meet_none_of_each_others_locks` (Linux).
  *As built (kb/Work PB3333-PB3335):* the entry is a `KeyedStore` (`IO/KeyedStore.cs`), a `RelativeStore` or an
  `IndexedStore` — the run unit's page I/O (`Store.RunUnitPageMedium`: the attached connectors' own handles, each its §9.1.15 file lock, a
  writable one used whenever one is attached, so no second handle is ever taken), one `PagedFile` (the pages, the page cache
  `Store.PageCache`, the ledger, the counters) and the record-number tree; `PagedStore`'s role is those two together. A
  store opened through no writable handle is opened again — and so FENCED — when a writable sibling attaches, and opened
  again as a reader's (R6 in memory) when its last writable connector detaches, so no reader's statement writes through
  a handle that cannot. A read refused in that re-adoption answers the detaching connector's CLOSE '30' (2.2.1.16's
  refused READ row) and leaves the store on the writable meta it holds — the last completed statement's trees, which the
  readers' shared entries read and never write — with its fence OWED (`KeyedStore`'s `_fenceOwed`): the run unit's next
  writable OPEN opens the store again, and so fences, instead of taking it as a live writable store. *Measured (PB3335
  review lead R1-L2: the CLOSE answered '00' and the next OPEN I-O skipped the fence, so a run unit that died over the
  file in between left its torn page 2 to that writer's statements, '30';
  `StoreCoherenceTests.A_refused_re_adoption_at_the_writers_close_is_30_and_the_next_writable_open_fences`).*
- **Published record locks** are unchanged (2.2.1.8).

**2.2.1.8 Where the locks live.** Nowhere new but one region. The region map of `HostRegionLocks` — the five Table 19 column
bytes at 2^62, the store mutex at 2^62 + 0x100, the lock-presence byte at 2^62 + 0x101, a sequential file's entry mutex at
2^62 + 0x102 (2.2.2.4; `HostRegionLocks.EntryMutexByte`, held as a lock file on every host), the hashed record region at 2^62 + 2^60
— stays, held through the connector's own handle (`HandleRegionLocks`: Windows `LockFileEx`, Linux OFD locks) or the lock files
of `SidecarRegionLocks` on every other host, and the reason the Windows locks are harmless changes from "beyond
`RecordFraming.MaxStoreBytes`" to "beyond 2^44, the format's largest file" (`PagedFile.FormatPages` pages of 4 096 bytes, which the comment on
`HostRegionLocks` cites since PB3335). **Not added: a transaction's locks.** The records a transaction changed and the unique alternate key values its
changes freed are its HOLDINGS, pages of the file (the HOLDINGS tree, 2.2.1.10), not bytes of the region: a region lock dies
with its holder's handle, and a holding must outlive it (finding R4-1). The region keeps exactly today's kinds — the record
identity a READ locks (under commit control too, §12.4.5.9.4 GR4) and the explicit postures' locks — and `PhysicalFileTable`
tables only those. **Added: the TRANSACTION-LIVENESS region at 2^62 + 2^61, one byte per transaction id (`TransactionLivenessByte`
= base + (id mod 2^60)), held EXCLUSIVELY through the owner's writable handle for the life of the transaction** (2.2.1.10): the
operating environment releases it with the handle when the owner dies, and the lock-file host already deletes a dead holder's
file (`SidecarLockFiles`: *"delete it when its holder is dead"*), so "is this transaction's owner alive" is one test of one
byte on every host, by the same primitive and with the same liveness the record locks have. **Added for Windows (kb/Work PB3482,
PB3499): the Table 19 COLUMN region at `ColumnRegionBase` = 2^62 + 2^61 + 2^60, one 2^32-byte range per column (`ColumnRangeStart`: base + wire
number × 2^32), and the OWED-WRITES range at base + 5 × 2^32 (`OwedWritesRangeStart`),** each published as one random exclusive byte of
its range through the holder's own read handle (`HandleColumnLockHost`; the owed-writes byte only by an INPUT opener that holds a handle open
for writing, 2.2.1.10 As built (3)). **Added for the sequential organization (2.2.2.4): the ENTRY MUTEX byte, `EntryMutexByte` at 2^62 + 0x102, expressed by `SidecarRegionLocks` on EVERY host** — its holder is a file in the lock directory, so it exists whether or not the physical file does, which a byte of the file's region cannot (a DELETE FILE removes the file; a creation precedes it); for the same reason a sequential transaction's liveness is its holder file `txn-<id>.live`, not a byte of the file's region, and `TransactionLivenessByte` stays the keyed engine's. `PhysicalFileTable` (the run
unit's lock table, `LockRecord`/`HolderOf`/`Unlock`, the ceilings 54 and 53), the presence-byte shortcut, the publish-then-test
discipline for a reader on Linux, `RetryAttempt.HolderOutsideRunUnit` and the wait outside the mutex (`WaitOutsideStatement`)
keep their code and their witnesses: `CrossRunUnitKeyedStoreDriftTests` runs unchanged in intent over the new engine (its
sibling `KeyedStoreReloadFailureDriftTests` holds the refused page-2 read and the refused page write, and
`StoreCoherenceTests` the adoption cases of 2.2.1.7 and 2.2.1.12, and `ReadOnlyKeyedWriterStoreMutexTests` the rule that a
`SHARING WITH READ ONLY` or `ALL OTHER` writer makes every page write of a statement, the tree pages and page 2, inside the
mutex and gives it back when the host refuses one, on the machine's region host and on the lock-file host: kb/Work
PB3414, PB3458; the file helpers every one of these tests uses (all three organizations, and the temp host path `Tmp` and the best-effort `TryDelete`, kept to this one home by `FileHarnessOneHomeDriftTests`), and the store-mutex probe, are written once, in
`FileHarness`, which lives in `tests/_shared` and so reaches every test project, the Conformance witnesses included; only the store-mutex probe, which needs the runtime's internals, is that class's Unit-project half: kb/Work PB3459, PB3471, PB3472, PB3478). The
§9.1.15 file lock is still the handle the store travels through (kb/Work PB771), and every page read and write goes through
that handle positionally (`RandomAccess`), so no second handle is ever taken.

**2.2.1.9 The size limit, and the new DOC-A.1-107 text (to replace the row when PB3333/PB3334 land).** *"The boundary of a
physical relative or indexed file is the host file system's maximum file size, capped by the format at 2^32 pages of 4 096
bytes (16 TiB). A WRITE or REWRITE whose record would take the store past that boundary is unsuccessful with I-O status '24'
(§14.9.51.4 GR33 b) relative, GR42 d) indexed; the invalid key condition exists, the INVALID KEY phrase runs, no record is
released and the file is unchanged); the host's answer that the file cannot grow further (`EFBIG`, `ERROR_FILE_TOO_LARGE`) is the
same boundary. The '24' is a record WRITE's or REWRITE's alone: a DELETE, an OPEN or a CLOSE whose pages meet the boundary
answers §9.1.13.6 item 1's '30', since §9.1.13.5 item 4's condition is an attempt to write a record outside the boundaries and
§14.9.10.4 gives DELETE none. A medium with no room for the statement's pages (`ENOSPC`, `EDQUOT`, `ERROR_DISK_FULL`, `ERROR_HANDLE_DISK_FULL`,
`ERROR_DISK_QUOTA_EXCEEDED`) makes THAT statement unsuccessful with §9.1.13.6 item 1's '30' and leaves the file as the previous
statement left it; nothing is deferred to the CLOSE. A record's own size is bounded by the host, not the format (overflow pages).
The relative key's own range is separate: a key less than 1 or greater than 2 147 483 647 (the highest relative record number
permitted) answers '34' (§14.9.51.4 GR29 b)), and a sequential-access release whose number has more digits than the relative
key item answers '24' (GR33 c)). Same in every edition."* The page layer answers both halves as `StoreStatus.Boundary`, never
an escaping exception: a changing statement takes its RESERVATION before its claim, so one whose growth would pass 2^32 pages
(`PagedFile.FormatPages`) claims and writes nothing; and the production medium reports the host's "the file cannot grow
further" as `MediumSizeLimitException` (`HandlePageMedium.Write`, asked of `HostFile.IsFileSizeLimit` — the narrower half of
the sequential boundary's `HostFile.IsMediumBoundary`, the one place both read the host's codes), which a changing statement's
refused write answers as the boundary while every other refusal stays '30'. The page layer does not know which statement it
serves, so the CONNECTOR answers the boundary by the statement: a WRITE or REWRITE '24' with the invalid key condition, and a DELETE that meets it answers §9.1.13.6 item 1's '30' with no invalid key condition — item 4's '24' is an attempt to WRITE outside the boundaries, and §14.9.10.4 GR3 and GR4 give a DELETE the invalid key condition only when the file does not contain the record. A promotion, a CLOSE and an OPEN have no '24': at
either boundary they answer '30'. The mapping is written once for both organizations, `KeyedConnector.RecordWriteFailed`
(a record WRITE or REWRITE) beside `KeyedConnector.Failed` (every other statement). *The two arms of the boundary (PB3334 review, kb/Work PB3396):* a changing statement and a retirement reserve the same
BOUND (R2: `Ledger.WriteBound`, an upper bound of the ledger pages the write takes, a few pages even for an empty ledger),
and the format's last page treats it differently. A statement's boundary is its RESERVATION (`BoundaryAt.Reservation`): a
bound that would grow the file past 2^32 pages is refused before the claim, the implementor's definition of the
externally-defined boundary §9.1.13.5 item 4 leaves to it, so a WRITE a few pages short of the end may answer '24' although
it would have fitted. A RETIREMENT (an `OPEN I-O` fence, a promotion at a CLOSE, at the generation ceiling or at the ledger
checkpoint) has no '24' and its claim is already taken, so its boundary is the PAGE (`BoundaryAt.Take`): the pages of its
reservation past the format's last are `PageOrigin.Beyond`, carry no page number, and the retirement is refused ('30', the
store as the previous statement left it) only if it WRITES one of them. A fence that writes no page past the page count,
or no more pages than the format has left, therefore completes at the format's last page (measured: the
`PagedFileBoundaryTests` fence cases; `IndexedEnginePermanentErrorWitnessTests.BoundaryFile` opens I-O at 2^32 recorded
pages), and one that writes one more answers '30' while `OPEN INPUT` still reads the file (`KeyedFenceBoundaryTests`, the
`FenceRefused` program cases). A retirement that must write a page past the format's last is refused
whatever promotion it serves, and the store is then the previous statement's. Through statements that is met at the
OPEN's fence (a never-closed store whose adopted state owes a ledger page the format has no room for), while a CLOSE
behind statements completes: each statement leaves its retirement the pages it released, and the bound-sized
reservation that refused it is gone (measured: `PagedFileBoundaryTests`' close cases, a CLOSE behind the one statement
that fits at every headroom and a CLOSE after statements spent every FREE page; no headroom made a CLOSE answer '30').
A CLOSE whose page write the host refuses answers '30' (`KeyedClosePermanentErrorWitnessTests`).
`RelativeConnector.HighestRelativeRecordNumber` keeps its value: the
tree would hold 2^63 keys, but the number is the §12.4.5.13 key item's domain, a determination that need not change with the
format. The constant `RecordFraming.MaxStoreBytes`, `KeyedConnector.StoreHolds`, `RelativeStore.FramedBytesAfterPut` and
`IndexedStore.RecordBytes` are deleted with the model that needed them. The store header's bytes stay the file's §9.1.6 fixed
file attributes (*"A physical file has several attributes that apply to the file at the time it is created and cannot be changed
throughout the lifetime of the file"*), read by `FixedAttributeConflict` from the committed meta through `HostFile.OpenAuxiliary`
as today (the read of two pages, not of a variable-length header), and `SequentialConnector`'s "a keyed store opened through a
sequential description is not a conflict" keeps its answer through the same reader.

**2.2.1.10 APPLY COMMIT, COMMIT and ROLLBACK: per-run-unit transactions on the ledger.** The facility is declined today at bind
(`DeclinedFacilityPass.VisitApplyCommitClause`, Annex A.4.3 item 2) and COMMIT and ROLLBACK are CONTINUE
(`conformance:cases/14.9.7-COMMIT/pb137_commit_inert`, §14.9.7.4 GR1 / §14.9.36.4 GR1). PB3330's bar builds them on the engine, so the decline
is reversed by its groups: the runtime half (PB3337) and the compiler half (PB3338).

- **The scope the standard requires (finding R1-2, derived 2026-10-17).** §14.9.7.1: the COMMIT statement *"makes permanent all
  changes to all files subject to active APPLY COMMIT clauses in the run unit"*; §14.9.36.4 GR3: ROLLBACK restores those files
  *"to the state they were in at the execution of the last COMMIT statement or the start of the run unit if no COMMIT
  statements had previously been executed"*. Read alone, "the state they were in" could mean the whole file; the clauses
  around it settle that it means the run unit's own changes. §9.1.17 defines the unit of work the facility protects as one
  whose *"files and data are protected from the activity of other run units that wish to access any of the same records and
  data"* — the same RECORDS, through record locks (§9.1.16's implicit multiple-record locking), which is how Annex D.2.5
  describes concurrent run units with their own units of work on one file and names the one leak: *"in the event of a
  rollback, that record would then be inconsistent with what was used by the other run unit"* — the record this run unit
  changed, not the other run unit's work. A ROLLBACK that discarded another run unit's completed statements, or a COMMIT that
  made another run unit's uncommitted changes permanent so its later ROLLBACK could not restore them, would contradict all
  three. Where the words leave latitude the precedence of CLAUDE.md rule 1 was applied from sources: GnuCOBOL 3.2's
  `cob_commit` and `cob_rollback` (`libcob/fileio.c` 6861-6883) only call `cob_file_unlock` on every open file — no file state
  is committed or restored, so GnuCOBOL has nothing on scope; Micro Focus (Fileshare, "Database Integrity" and "The COMMIT
  Statement", microfocus.com/documentation/reuze/60d/{fsdbse,lhpdf808}.htm, read 2026-10-17) scopes a transaction to the
  program's own updates — it starts at that program's first WRITE, REWRITE or DELETE after OPEN, COMMIT or ROLLBACK, a
  ROLLBACK returns the files "to the state they were in before the transaction started", every record a transaction updates
  is locked until its COMMIT or ROLLBACK, and the server rolls back the pending transaction of a client that dies or times
  out; IBM has no COBOL verb (CICS SYNCPOINT and DFSMStvs commit a unit of recovery, likewise the task's own updates). So the
  scope is the RUN UNIT's changes through connectors under its active APPLY COMMIT clauses, and nothing else is touched.
- **A TRANSACTION is one run unit's open unit of work on one physical file.** It begins at the first WRITE, REWRITE or DELETE
  the run unit executes through a connector under commit control on that file (a read-only run unit has nothing to undo and no
  entry), DRAWS its id — the lowest value from page 2's `transaction mint` whose liveness byte (2.2.1.8) it can take
  exclusively through that connector's handle; a held byte is a live transaction's or a refused attempt's and is skipped —
  CLAIMS it (`transaction mint` := id + 1, in the store's memory and, under the mutex, on page 2 before any page write;
  2.2.1.7), and is an entry of the TRANSACTIONS tree: id → the POINTER to its UNDO tree. A first change refused after the
  byte was taken gives the byte back when it answers '30', since no entry carries the id; a death frees it. *Finding R4-2:
  the round-3 text minted the id from page 2 and advanced the mint only with the completion, so a first change refused
  part-way left a live run unit in permanent error holding byte t while page 2 still offered t, and the next first change
  of any run unit minted t and met a held byte — refused with no status, or a deadlock on the mutex if the take waited.
  Drawing by the byte and claiming before the first page write make the id, like the generation, an identifier issued once
  per history whatever the attempt's fate.* The UNDO tree is keyed (kind, identity) — the key the HOLDINGS tree uses —
  and holds under R, a record identity (the prime key bytes, or the 8-byte relative record number), the record's
  BEFORE-IMAGE as of the run unit's last COMMIT (or the start of the run unit): the whole frame, ordinal vector included,
  or an ABSENT marker for a record the transaction created; and under V, a unique alternate key value (the key's index and
  the value's channel bytes) the transaction's changes freed, nothing but the entry. Every WRITE, REWRITE and DELETE
  through a connector under commit control inserts the before-image under the record's identity unless an entry already
  exists (the first image is the one to restore; later changes to the same record do not replace it) and a V entry for
  every unique value it frees. The UNDO tree is written by the same statement that changes the record, under R1-R3, so it
  is exactly as crash-consistent as the data: a statement that completed has its before-image; one that did not has
  neither its change nor its image. **Its HOLDINGS are in the file too** (finding R4-1): the HOLDINGS tree — ONE tree per
  file, the same engine, its root in the meta page — maps (kind, identity) → transaction id, R and V as above, and the
  statement that changes the record writes the record's holding and every value its change frees in the same statement as
  the change and the UNDO entries. **The two trees carry the same keys: the HOLDINGS tree is the file's INDEX, by identity,
  of every open transaction's UNDO entries, and a transaction's UNDO tree is ITS OWN LIST of its holdings** — after every
  statement the HOLDINGS tree's key set equals the union of the UNDO trees' key sets (a drift test, 2.2.1.12), because one
  statement writes both and only the statement that retires or rolls back a transaction removes either. So a transaction's
  holdings are exactly the identities its rollback restores or re-inserts, they are visible to every statement under the
  mutex the moment the change is, they outlive the owner's handle, and COMMIT's retirement, ROLLBACK and the dead-owner
  rollback enumerate them by one walk of the UNDO tree they already read, at u + its leaves. *Finding R5-2: the round-4
  text keyed the HOLDINGS tree by identity only — no index by transaction — and listed V holdings nowhere else, yet had
  COMMIT and ROLLBACK find a transaction's holdings through its UNDO tree. A value freed by a LATER change of the same
  record (REWRITE K1's unique key X → Y, then Y → Z: the before-image holds X, the record Z, the holding V:Y) and a value
  freed by a DELETE of a record the transaction WROTE (before-image ABSENT) were in no before-image, so they leaked past
  COMMIT and ROLLBACK; a later WRITE with Y met the leaked holding, found its owner's byte free, rolled back a transaction
  with no entry — nothing — and met the holding again, without end. Recording every holding in the owner's UNDO tree is the
  cheaper of the two shapes (the other, a HOLDINGS tree keyed (transaction, kind, identity) with a second index for the lock
  test, path-copies two trees per change) and the one that makes the next kind of holding automatic: whatever a statement
  adds to the HOLDINGS tree it adds to the UNDO tree in the same breath, and the drift test holds the two equal.*
- **COMMIT** (§14.9.7.4 GR3: *"The execution of the COMMIT statement permanently applies any changes made to the files specified
  in all the active APPLY COMMIT clauses and releases all record locks associated with those files."*) = for every physical file
  on which this run unit has a transaction: a promotion (R5) whose claim carries `committing` = the transaction's id and whose
  RETIREMENT removes the transaction's entry and its holdings and releases its UNDO tree's pages; then the liveness byte is
  given back; then `PhysicalFileTable.ReleaseAllForConnector` for each connector under commit control (§9.1.16: *"A COMMIT
  statement releases all record locks for files subject to commit and rollback"*); GR4's file locks of files closed and not
  reopened since are released here too, which means a CLOSE of a connector under commit control KEEPS its handle (§9.1.15:
  *"the file lock is removed by the COMMIT statement"*) until the next COMMIT or ROLLBACK. **The COMMIT is decided by the
  promotion's one durable write, the slot write and the sync after it, and it is ANSWERED only by that sync.** Three
  regions, each with its rule (round 5's finding R5-1: the round-4 text decided the first two and left the third to R6,
  which gave the slot write two fates — the owner told '30', its unconditional ROLLBACK restoring nothing, because the
  ROLLBACK's own entry, or any other connector's, had meanwhile completed the landed COMMIT by the restart rule):
  **(1) Refused BEFORE the slot write** — the claim, sync 1, or the slot write itself refused by the host: the slot carries
  nothing and the transaction is exactly as it was — the claim is unwound by R5's restart rule, at the next entry of any
  connector, this run unit's ROLLBACK included — the COMMIT is unsuccessful, which for a file under an active APPLY COMMIT
  is §14.6.13.1.3 item 8's fatal condition, *"a ROLLBACK statement is executed unconditionally"*, and that ROLLBACK
  restores from the intact UNDO tree; every state the medium can later show agrees, since every durable slot carries the
  entry and the fence rolls it back. **(2) The slot write handed to the operating environment and sync 2 LANDED:**
  committed, whatever becomes of the retirement — a dead or refused retirement is completed by the next entry, whose
  restart finds the slot landed and removes the entry `committing` names; a power loss that loses it leaves the PROMOTION
  SOURCE, and R6's fence retires it — and the COMMIT answers success. **(3) The slot write handed to the operating
  environment and sync 2 REFUSED — the UNACKNOWLEDGED COMMIT.** Every live run unit now reads that slot as C (the
  operating environment keeps what it was handed, and every process reads through it) while the medium holds it or not.
  The owner cannot take the write back, and no answer but "not committed" can be made true for both of the medium's
  futures, so the COMMIT makes that answer true before it gives it, under the exclusive mutex it has held since its claim:
  it REVERSES. The reversal is the promotion's RETIREMENT run as the ROLLBACK of t — one statement, claimed above the
  COMMIT's generation, that does what every retirement does (page 2 onto the new base, the COMMIT's slot generation; the
  promoted ledger retired; PROMOTING clear; `committing` 0) and, in place of removing t's entry, re-applies every
  before-image of t's UNDO tree and removes the entry and its holdings (the ROLLBACK statement below, whose material is
  intact because no retirement ran; the UNDO pages, referenced by the slot, are `freed`, R4) — so page 2 is a DESCENDANT
  of the COMMIT's slot holding the records as at the previous COMMIT, which is what R6 adopts and every live entry
  follows — and then an ordinary promotion of that page 2 (R5: the claim, sync 1, both slots read, the slot that does not
  hold C — the pre-COMMIT slot, since the COMMIT's slot now holds the higher generation — sync 2, its retirement). When
  that promotion's syncs land, every future reads the rollback: both slots durable with the reversal's the higher; the
  COMMIT's slot lost and the reversal's landed; or both lost — the pre-COMMIT slot with t's entry, which the fence rolls
  back. The COMMIT then answers unsuccessful, and item 8's unconditional ROLLBACK finds no transaction on this file — as
  it does after a refused first change (2.2.1.16) — and rolls back the run unit's other files. *Why the reversal is the
  COMMIT's own and runs inside its mutex hold:* a refused promotion otherwise releases the mutex with the flag set, and the
  next entry of ANY connector completes a landed promotion from the slots alone — it cannot tell a refused sync 2
  (unacknowledged) from a refused retirement (acknowledged, success already answered), and only the owner knows which;
  left to the owner's ROLLBACK, another run unit's entry between the COMMIT and the ROLLBACK completes the retirement and
  the ROLLBACK restores nothing (the finding's scenario). Inside the hold no other entry sees the unacknowledged slot.
  **When the reversal itself is refused** — one of its page writes, or a sync of its promotion — the medium is refusing
  after it accepted a COMMIT's slot write, and t's fate ON THE MEDIUM is UNKNOWN: committed if the COMMIT's slot is the
  highest valid durable slot, rolled back otherwise. The store MARKS the file UNACKNOWLEDGED (in the run unit's memory),
  the COMMIT answers unsuccessful, and the file is in §9.1.13.1's permanent error; the only statements this run unit then
  executes on it are item 8's unconditional ROLLBACK and, when that is unsuccessful, the termination path's file-only
  rollback (§14.6.12), and each runs, under the mutex: if page 2 still carries t's entry under the flag, the reversal again;
  then a promotion of page 2 as it stands (harmless when repeated); and it answers by that promotion's sync — success
  clears the mark, since the durable committed meta now descends from the reversed state; a refused sync is '30'. A
  ROLLBACK that finds t's entry gone from page 2 without this run unit having rolled it back answers '30' too: another
  connector's entry completed the landed COMMIT in the operating environment while the owner could not reverse it, and
  the medium decides its durability. The program is thereby told, truthfully, that neither its COMMIT nor its ROLLBACK
  was acknowledged, and R6 settles the medium's state at the next OPEN as it settles every power loss (a landed COMMIT
  slot is finished; anything else rolled back). A medium that refuses every sync after accepting a slot write is the one
  case no design can answer consistently with the medium, and this one answers it as unknown rather than as either
  outcome. **What the program is told.** The standard gives COMMIT and ROLLBACK no I-O status — §9.1.13.1 sets it *"during
  the execution of a CLOSE, DELETE, OPEN, READ, REWRITE, START, UNLOCK or WRITE statement"* — and no exception condition
  for a commit the medium refuses (§14.9.7.4 GR2's EC-FLOW-COMMIT is for a COMMIT *"under the control of a recursive
  runtime element or a file SORT or MERGE statement"*), so what a refused COMMIT raises is the implementor's. GnuCOBOL 3.2
  has nothing (`cob_commit` unlocks and cannot fail); Micro Focus Fileshare ("Database Integrity",
  microfocus.com/documentation/reuze/60d/fsdbse.htm, read 2026-10-17): *"you cannot check the status of the COMMIT or
  ROLLBACK operation using the file status of any one data file"*, the program calls `fs_status` after the verb and *"A
  non-zero value returned in t-status indicates that the COMMIT or ROLLBACK operation failed"*; whether a failed COMMIT
  is rolled back, and whether a multi-file COMMIT is all or nothing, the page does not say. The determination (PB3338's
  commit row in `docs/CONFORMANCE.md`): a COMMIT or ROLLBACK the medium refuses raises the fatal EC-I-O-PERMANENT-ERROR
  for the file whose promotion or statement was refused, with '30' in that file's status item and the file in permanent
  error — exactly the condition item 8 names, *"a fatal exception condition for a file subject to an active APPLY COMMIT
  clause"*, so its unconditional ROLLBACK follows by the standard's own rule. **Files are committed one at a time, in the
  order of the run unit's open stores, each by its own slot write:** the order is the place `StoreTable` stamps on each file's
  store when it becomes the run unit's (`KeyedStore.OpenOrder`), read by `FileRegistry.TransactionHolders` whoever holds the
  transaction - a live connector or the state a CLOSE under commit control kept and the registry retired - so neither the
  connectors' registration order nor the order of the registry's collections decides it (kb/Work PB3494, review finding
  R1-2). A COMMIT refused at file i leaves the files before i
  committed (each acknowledged and durable) and the files from i on at the previous COMMIT once item 8's ROLLBACK has run.
  The standard defines no cross-file atomicity for a COMMIT that fails — §14.9.36.4 GR3 restores *"to the state they were
  in at the execution of the last COMMIT statement"*, which for the committed files is this one — and an all-or-nothing
  COMMIT across files would need a durable commit record outside any one file (a second file, refused by PB802, or a slot
  of one file that another file's OPEN must consult). PB3338's row states the determination; whether the owner wants the
  cross-file atomicity anyway is the one question in PB3330. *Round 4's self-attack: the round-3 COMMIT ran a removal statement first and
  promoted second, so a promotion refused after a landed removal (a medium that refuses an in-place write, or a revoked
  handle) reported failure with the transaction already gone, and the fatal path's ROLLBACK restored nothing; the reverse
  order reported success with the removal unsynced, which a power loss undid. A decision that is reported once must be
  made by one durable write, so the slot write carries it.* The promotion also makes any OTHER run unit's
  uncommitted changes on that file durable on the medium — harmlessly, because that run unit's UNDO tree is on the medium with
  them, and its ROLLBACK or its death restores them from it (the case finding R1-2 named: B under APPLY COMMIT writes K2, A
  COMMITs, B's later ROLLBACK still restores the state at B's last COMMIT).
- **ROLLBACK** (§14.9.36.4 GR3) = for every physical file on which this run unit has a transaction: ONE statement (one
  generation, atomic under 2.2.1.6) that walks the UNDO tree once and re-applies every R before-image — a frame is written
  back under its identity with its OLD ordinals (the alternate trees' composite entries are rebuilt from the frame's
  ordinal vector, so the duplicate order of §14.9.35.4 GR24 is the one at the last COMMIT; the release mint only ever
  grows), an ABSENT marker deletes the record — and removes from the HOLDINGS tree every key the walk met, R and V alike
  (finding R5-2), and the transaction's entry; then the region's record locks (its READ locks) are
  released (§9.1.16: *"A ROLLBACK statement releases all record locks for files subject to commit and rollback"*), the kept
  handles of connectors closed since the last COMMIT are released, and the §9.1.15 last sentence's case (a file not open at
  the previous commit and opened since) releases its file lock. No other connector can have changed a record the rollback
  restores, or taken a unique key value it re-inserts: each is a holding in the file (the next two bullets), and a holding is
  passed by no writer while its owner lives ('51') and by none after the owner's death without this rollback having run
  first, at that writer's own lock test or at its entry (the lock-test bullet; findings R3-1 and R4-1) — and an INPUT
  connector cannot change one. `ApplyUndo` therefore runs no uniqueness check and no lock test of its own, and a momentary
  double entry under a unique key inside the one statement (K1 restored under X before the transaction's own K2 with X is
  removed) is invisible, since the statement is atomic. A ROLLBACK needs no promotion: if its
  statement is lost to a power loss, the committed state still carries the transaction and the next writable statement or
  OPEN rolls it back again; the operation is idempotent. The one exception is a ROLLBACK on a file marked UNACKNOWLEDGED
  (the COMMIT bullet, region 3): there the committed meta the operating environment holds may be a COMMIT's slot naming t,
  which the fence would finish rather than roll back, so that ROLLBACK promotes the reversed state and answers by its sync.
- **Every record a transaction changes is locked by the connector that changed it, read or not, until COMMIT, ROLLBACK or
  termination** (finding R2-3, derived 2026-10-17). §9.1.16 makes commit control *"an implicit LOCK MODE IS AUTOMATIC WITH
  LOCK ON MULTIPLE RECORDS"* whose locks only COMMIT and ROLLBACK release; §12.4.5.9.4 GR4 locks on READ (*"Records are
  locked when any READ statement is executed"*); §14.9.51.4 GR11 sets the lock at a WRITE when *"the WITH LOCK phrase is
  specified or implied"* and names no implier, so what implies it is the implementor's; §14.9.10.4 GR7 releases a deleted
  record's lock only *"if record locks are in effect and the file is not subject to an active APPLY COMMIT clause"* — under an
  active APPLY COMMIT the release does not apply, and a lock kept on a DELETED record can only exist to hold its identity for
  the rollback (the clause's one NOTE, under rule 15 after FORMAT 2 DELETE FILE — *"For file connectors subject to APPLY
  COMMIT clauses, while those APPLY COMMIT clauses remain active, then file and record locking persists"* — says the same of
  the posture in general and continues about deleting FILES; finding R3-3: the round-2 text had placed it under GR7, and the
  deleted-record conclusion rests on GR7's own carve-out, §9.1.16 and D.2.5, not on that NOTE); §9.1.17's unit of work is *"protected from the activity of other run units that wish to
  access any of the same records and data"*; and Annex D.2.5 says *"It is possible for another run unit to read a record
  written or updated by the current run unit"* only with IGNORING LOCK — a written record is a locked record — and that *"As
  locks on records are not released until there is a commit or a rollback, other run units may not prevent the current run
  unit from doing what it needs to do with them in the course of a rollback or commit"*. Table A.1's "WRITE, automatic: no
  lock" governs the non-commit case by its own preamble. *The first correction read D.2.5 backwards: the inconsistency it
  names is a record another run unit READ ignoring the lock, not one it changed, and the design let a ROLLBACK restore a
  before-image over another run unit's completed WRITE of the same key — the harm finding R1-2 had named, half fixed.* Where
  the words leave the implier open, the precedence of CLAUDE.md rule 1: GnuCOBOL 3.2 has no commit control (`cob_commit` and
  `cob_rollback` unlock, `indexed_write` locks only on `COB_WRITE_LOCK`, `libcob/fileio.c` 3811-3818, 6866-6883) — nothing;
  Micro Focus Fileshare locks every record a transaction updates until its COMMIT or ROLLBACK (the survey above). So: commit
  control IMPLIES WITH LOCK for WRITE (GR11); REWRITE and DELETE lock the record they change whether or not a READ locked it
  first; and these locks are the transaction's HOLDINGS, pages of the file, not bytes of the region: the statement that
  changes the record writes (R, the record's identity: prime key bytes or relative record number) → its transaction id into
  the HOLDINGS tree, in the same statement as the change and the before-image, and the entry stays after a DELETE's record
  is gone, so another connector's WRITE of a record with that identity answers '51'. *Finding R4-1: the round-3 text
  published these locks in the region, where the operating environment releases them with a dead owner's handle; a writer
  whose entry had probed the owner alive, and whose own lock tests came after, found the record free when the owner was
  killed between the two, changed it, and the next entry's rollback restored the before-image over its completed statement.
  No probe closes a window between itself and a test of a different lock. A holding in the file does, because the test and
  the holding are the same page, read under the mutex, and the only fact about another process the test consults is one
  byte, probed at the test itself (the lock-test bullet).* The WRITE statement gains the lock test REWRITE and DELETE
  already have, placed where DOC-A.1-104 places theirs: after the invalid-key conditions, which ignore locks (§14.9.51.4
  GR33, GR42), and before the record is touched; the RETRY phrase applies (GR16: *"for the case where resources needed to
  write a record are locked by another run unit"*). The connector's own re-lock is idempotent (GR8).
- **A unique alternate key value a transaction's change frees is held too** (finding R2-4). A REWRITE that changes a key
  declared without DUPLICATES from X to Y, and a DELETE of a record whose such key is X, hold the VALUE X — a HOLDINGS entry
  of kind V: the key's index and X's channel bytes → the transaction id, and the same key in the transaction's UNDO tree
  (its own list of its holdings, finding R5-2), both written by the same statement — until COMMIT or
  ROLLBACK (the owner's, or the one its death brings); a WRITE or REWRITE through any other connector, in this run unit or
  another, that would insert X answers '51' at the same point of its order as the record holding above (RETRY applies); the
  transaction's own connector holds it and proceeds. Without it the rollback's re-insertion of X beside a record another run
  unit wrote with X would leave two records under a unique key, a state no statement can create and every later
  alternate-key check answers wrongly against (the review's scenario). The value holding is §9.1.16's *"circumstances other
  than a locked logical record that result in the return of a locked record status"*, which the implementor may specify;
  DOC-A.1-152 names it and the deleted-identity holding (PB3337). Keys WITH DUPLICATES need no holding.
- **The lock TEST, for every writer and every reader not IGNORING LOCK** (finding R4-1). READ, WRITE, REWRITE and DELETE test
  the region as today (the READ locks and the explicit postures) and, when page 2's holdings root is not empty, look the
  identity up in the HOLDINGS tree — one lookup, h pages, hot — and, for a WRITE or REWRITE, each unique alternate key
  value they would insert, at DOC-A.1-104's point of their order. No entry: the identity is nobody's, and no rollback will
  ever touch it, because a rollback touches holdings only. An entry of this run unit's own transaction, through a connector
  under commit control: proceed (GR8). An entry of any other transaction — another run unit's, or this run unit's through a
  connector no APPLY COMMIT names: probe its owner's liveness byte NOW. Held: the owner lives, '51', RETRY applies. Free: the
  owner died, and the transaction is rolled back HERE — the ROLLBACK statement above, run by the testing connector under
  the mutex it holds, before its own change — and the statement's conditions are evaluated again from the invalid-key
  conditions on, against the restored state (a WRITE of an identity the rollback re-created now answers '22'; a value it
  re-inserted, '22' under that key). The result does not depend on the
  instant the death fell: before this statement's entry (the entry's step rolled it back), between the entry and the test
  (the test does), or after the test (the owner was alive at the test, so the holding was its — '51' — or the identity was
  nobody's and stays outside every rollback). The order in which the operating environment releases a dead handle's locks
  is not assumed, since the holding is a page and the byte is the one lock consulted. A READ IGNORING LOCK tests nothing,
  as today (§9.1.16; D.2.5's tolerated read). An INPUT connector's READ tests the region and the holdings alike (a '51' for
  a record a live transaction changed, as today's region lock gives it).
- **The lock ceilings do not apply to the implicit commit posture.** DOC-A.1-154 and DOC-A.1-155 (255 per run unit and 15
  per connector, `PhysicalFileTable.RunUnitLockMax`/`ConnectorLockMax`, the standard's minima) stay for every explicit
  posture; a connector under commit control holds one region lock per record it READ since its last COMMIT and one holding
  per record it changed, which D.2.5's deferred batch commits (*"they may be deferred by using a data-item to count the
  transactions processed"*) make unbounded by design, and §12.4.5.9.4 GR7 lets the implementor set the maximum. GnuCOBOL 3.2
  imposes none of its own (one `DB_LOCK` per file, `lock_record`; no 53 or 54 anywhere). The determination (PB3337 rewrites
  both rows): for the commit posture the maximum is the host's — a holding is a tree entry and costs a page share, a READ
  lock costs one published region byte (one `LockFileEx` range, one OFD lock, or one lock file on the lock-file host) — and
  the standard's own warning applies — *"Making the frequency of commits too low will affect the ability of other run units
  to run concurrently"*.
- **Abnormal termination** (§9.1.18.5: *"When a run unit terminates abnormally, all file changes made since the previous commit
  or, if none, the beginning of the run unit are rolled back for all files for which there are active APPLY COMMIT clauses."*)
  is two mechanisms. Where the runtime still runs — a fatal exception condition (§14.6.13.1.3 item 8: *"if there is a fatal
  exception condition for a file subject to an active APPLY COMMIT clause, then a ROLLBACK statement is executed
  unconditionally"*) and §14.6.12's termination path (*"If commit and rollback is in effect, a rollback is executed. This
  rollback differs from user specified rollbacks in that only files and not any of the data items"* are restored) — the
  runtime element's abnormal-termination path executes the file-only ROLLBACK above before it exits (the compiler half, PB3338,
  routes both). Where it does not (a kill, an out-of-memory failure, a power loss), the transaction's liveness byte, its
  READ locks are freed by the operating environment with the handle — its holdings are not, being pages — and **the DEAD-OWNER STEP**
  rolls the transaction back: at the coherent entry (2.2.1.7) of EVERY statement through a connector with a writable handle
  — a record statement's or an OPEN's (R6 step 5), in any run unit, under the mutex, whether or not other run units hold the
  file open — when page 2's transactions root is not empty, the TRANSACTIONS tree is read, each transaction of ANOTHER run
  unit has its liveness byte probed (this run unit's own are alive by construction), and every transaction whose byte is
  free is rolled back by the ROLLBACK statement above before the entering statement proceeds. The probe has THREE answers,
  read in one place (`KeyedStore.OwnerOf`, `OwnerLiveness`; kb/Work PB3549): free is DEAD, held is ALIVE, and a host that
  cannot carry the lock is UNKNOWABLE, which the dead-owner step and the lock test read as alive (nothing is rolled back on a
  guess). A transient host failure is never the third answer: the host asks again before it answers (`OfdRegionLocks.Classify`
  repeats an interrupted call, `EINTR`, for as long as it recurs, and a refused lock record, `ENOLCK`, up to four calls in
  all; the lock-file host's publication and observation are bounded the same way). *Finding R3-1: the round-2
  text ran the step only at the next writable OPEN and rested the rollback's safety on the transaction's locks, which a
  dead owner no longer holds; between the death and that OPEN a live writer not under commit control changed the dead
  transaction's records and took its freed unique values with no lock refusing it, and the later rollback restored
  before-images over completed statements and re-inserted unique values beside the live writer's — R1-2's and R2-4's harms
  through a hole the locks could not cover.* What §9.1.18.5 requires is the rollback; when it runs is the implementor's, and
  the precedence survey (CLAUDE.md rule 1; GnuCOBOL has no commit control) settles it: Micro Focus Fileshare keeps a dead or
  timed-out client's locks on the SERVER until it rolls that client back, and the rollback is triggered by the first
  contending access — *"the Fileshare Server checks to see how long it is since the Fileshare Client holding the record
  lock last contacted the Fileshare Server"* and, past the timeout, *"Automatically rolls back any pending transaction being
  carried out by the first Fileshare Client"*, *"Releases all the record locks held by the first Fileshare Client"* and
  *"Returns error 9/068 'record locked' to the Client trying to read the record"* (microfocus.com/documentation/reuze/60d/
  fsdbse.htm, read 2026-10-17; `/t` in fscnfg.htm: *"The timeout only occurs if one Fileshare Client requests a record
  currently locked by another Fileshare Client"*); IBM converts a failed holder's active locks into RETAINED locks — *"Converts
  the active exclusive locks into retained locks"*, *"CICS returns the LOCKED condition to other tasks that request a lock"*,
  *"Preserving locks in this way also prevents other tasks from updating the records until the failure is resolved"*
  (ibm.com/support/knowledgecenter/en/SSGMCP_5.4.0/troubleshooting/dfht20d.html, read 2026-10-17; the z/OS SHCDS reference,
  SSLTBW_2.1.0 idai200/da6i2273.htm: *"These locks protect uncommitted changes and are needed for eventual backout"*, and
  *"For a failed subsystem, LISTSUBSYS shows the number of retained locks"*). Both keep a dead holder's records untouchable
  until its rollback has run; neither leaves a window. This engine has no server to hold a dead run unit's locks, so the
  FILE holds them: a transaction's holdings are pages (the holdings bullets above), which no handle's closing releases —
  IBM's retained locks, kept by the medium instead of a subsystem — and a dead owner is rolled back at the first entry
  that probes it or the first lock test that meets one of its holdings, whichever comes first (Fileshare's trigger, the
  contending access, is the second). *Finding R4-1: the round-3 text kept the locks in the region and rested on the entry's
  one probe, which speaks only for deaths BEFORE the entry; an owner killed between another writer's entry and that
  writer's lock tests left the tests passing on locks the operating environment had just released, and the next entry's
  rollback restored before-images over a completed statement — R3-1's harm through a window inside one statement. No
  arrangement of probes closes a window between a probe and a test of a different lock; a holding in the file does,
  because the holding is what the test reads.* The cost is one page-2 field already read, the TRANSACTIONS tree (≤ 2 hot
  pages) and one liveness probe per foreign transaction at the entry, only when a transaction exists, and the HOLDINGS
  lookup (h hot pages) at a lock test, only when a holding exists — the probe is the primitive the region's lock test
  already pays at every READ, REWRITE and DELETE. Between the death and that first writable statement, a connector opened
  INPUT (no writable handle: it can neither take the mutex exclusively on Linux nor write on any host) reads the file as it
  stands, dead transaction included — D.2.5's "possible for another run unit to read a record written or updated by the
  current run unit", for a run unit that no longer exists. DOC-A.1-75 (PB3335) and the commit row PB3338 writes state this
  determination.
- **CLOSE under commit control promotes like any CLOSE** (R5) and keeps the handle; the first draft's "a CLOSE of such a file
  writes the work meta and keeps the handle but does NOT promote" is withdrawn with the reason it existed (finding R1-5's
  contradiction was between that rule and PB3337's brief; both now say the same thing).
- **As built (kb/Work PB3337), where the text above left the shape open.** (1) The holdings test of REWRITE and DELETE is
  made where WRITE's is, after the statement's invalid key condition ('23') and before the record is touched
  (`KeyedConnector.HoldingsGate`, asked by each organization's verb); the region test keeps its place before the verb
  (DOC-A.1-104's order). A dead owner's rollback at the test, or a RETRY wait, makes the verb evaluate its conditions again
  from the top against the file as it then stands. (2) An INPUT connector that meets a dead owner's holding reads on: it
  writes nothing and reads the file as it stands. (3) A CLOSE of a connector an active APPLY COMMIT clause names keeps its
  handle, region endpoint and store attachment — whether or not the run unit changed a record through it (§9.1.15: *"The
  file lock is removed by an explicit or implicit CLOSE statement executed for that file connector, except where the file
  is subject to an active APPLY COMMIT clause."*; branch review finding R1-2) — in a `KeptFile`, the kept state KEYED BY THE PHYSICAL
  FILE it was kept on (kb/Work PB3494; the connector holds it in `KeyedConnector._kept`). The next
  COMMIT or ROLLBACK gives them back, and so does the run unit's end, after its implicit CLOSEs (§9.1.18.5: *"In both cases
  all file and record locks are then released"*; `FileRegistry.CloseAll`). The connector's own re-OPEN CARRIES THEM ON
  (finding R1-1): the attachment stays the connector's, so the run unit's store and its transaction on the file are the
  re-OPEN's, never a fresh store with no transaction; a re-OPEN that would take the same handle (the same share mode and
  write access) ADOPTS the kept one, with no instant in which the liveness byte or the file lock is free; one that takes
  another handle beside it moves the byte to the new handle inside the attach's store-mutex hold
  (`KeyedStore.HoldLivenessThrough`) and then gives the kept handle back; and where the kept handle's share mode refuses
  the new one, the kept HANDLE alone is given back first — the attachment and the transaction stay — and the byte is taken
  again through the new handle inside that hold; only a sharing refusal (`HostFile.IsSharingRefusal`) does that, any other
  `IOException` leaves the kept handle where it is (PB3483). While the run unit's transaction on the file stands and the kept handle is
  writable, the re-OPEN's handle is taken writable whatever its open mode (an INPUT re-OPEN included): it is the handle
  the COMMIT or ROLLBACK writes through; a handle the host lets hold no exclusive lock holds the byte SHARED, which
  refuses every other run unit's exclusive probe and draw alike. With no transaction standing (the connector only read)
  the re-OPEN's handle takes the access of its own open mode and no more (branch review finding R2-1). **THE
  OWED-WRITES POSTURE (kb/Work PB3482; it was a named wrong answer of this paragraph).** With the transaction standing,
  that writable handle under an INPUT re-OPEN is an INPUT opener that still owes its transaction's writes, and a host
  share mode cannot say so: on a host whose share modes are mandatory (Windows) `FileShare.Read`, the base posture of
  SHARING WITH READ ONLY (`FileLockPosture.OfSharingMode`), refuses every writer, so another run unit's OPEN that Table 19
  admits beside an INPUT opener (SHARING WITH READ ONLY on INPUT, I-O or EXTEND) was refused '61' where the table says
  '00', until the COMMIT or ROLLBACK. The root is the posture, not the re-OPEN: the share mode of a SHARING WITH READ ONLY
  connector over-restricts there, and the lock between run units is Table 19 itself, which Windows now carries as columns
  (`HandleColumnLockHost`, 2.2.1.8's byte map). So `FileConnector.Open` makes a second attempt, with
  `FileLockPosture.WhenOwedWritesAreAdmitted` (`FileShare.ReadWrite`), exactly when the first was refused by the host
  with the request's column HELD (Table 19 admitted it against every WiseOwl COBOL run unit) and another run unit's
  connector OWES WRITES: the re-OPEN INPUT of this very paragraph publishes it with its column (`FileConnector.OwesWrites`, from `KeyedConnector.WillTakeWritableHandle`: the
  OPEN is INPUT and `HandleAccess` is writable, the one expression `Lock` opens the handle by; `RunUnitFileLock.Amend`'s
  `owesWrites`), and the second attempt asks `RunUnitFileLock.AnotherOwesWrites` (kb/Work PB3499: it had asked whether any
  INPUT opener held a column, a proxy an unrelated reader satisfied); every request the table refuses is still refused, by the
  columns, and a program outside the protocol that holds the file for writing still refuses the READ ONLY request, whether or
  not a reader that owes nothing is open beside it. Witnesses:
  `FileLockPostureDriftTests.AnInputOpenerThatOwesNoWritesDoesNotAdmitAReadOnlyOpenBesideAnOutsideWriter` (all three
  organizations) and `HandleColumnLockHostDriftTests.AHolderThatOwesWritesIsSeenByAnotherHolderAndOnlyByAnother`, and
  the end-to-end witness:
  `CommitRollbackRuntimeTests.An_input_opener_that_owes_its_transactions_writes_admits_what_Table_19_admits` (both
  organizations, READ ONLY INPUT and I-O beside the re-OPENed INPUT opener, COMMIT and ROLLBACK, and the requests the
  table refuses meeting the widened connector's column). **THE SECOND ATTEMPT IS THE LAST (kb/Work PB3498).** A second
  attempt the host or the column refuses too ends the OPEN '61': no third attempt is made, a kept column is replaced and
  restored once per attempt (a second attempt refused at the column leaves the kept column as the first attempt's
  restore put it), and the unsuccessful OPEN leaves no column, no handle and the file as it was. Witnesses (the request
  count is read through `RunUnitFileLock.HostObserver`, which wraps the host a lock is established through):
  `FileLockPostureDriftTests.AnOpenWhoseSecondAttemptIsRefusedTooAnswers61AfterExactlyTwoRequestsAndLeavesNothingBehind`
  (all three organizations; a third run unit's column between the attempts) and
  `CommitRollbackRuntimeTests.A_reopen_refused_on_its_second_attempt_too_keeps_what_the_close_kept_and_makes_no_third_attempt`
  (both keyed organizations: refused twice by the host's share mode, and refused at the column on the second attempt).
  The sibling attempt loop, the publication's bounded tries, answers `Unavailable` when every try collides, never a
  refusal for the failure itself (`HandleColumnLockHostDriftTests.APublicationThatEveryTryCollidesInIsUnavailableAndLeavesWhatWasHeldStanding`);
  but a column another run unit holds and Table 19 refuses the request is a conflict the host still sees, so
  `HandleColumnLockHost.Unpublishable` tests the refused columns (the asker's own bytes left out) before it answers and
  an exhausted publication beside such a holder is `Refused`, '61' (`...IsRefusedByAHeldColumnTable19Refuses`; PB3518).
  `Publish` tells the two ways to fail apart (`RegionLockResult.Held`: every try collided; `Unavailable`: the handle or
  filesystem carries no byte-range lock) and only the first goes on to test the refused columns: a handle that cannot lock
  cannot test either, and is `Unavailable`, never a '61' no other connector contradicts
  (`...AHandleThatCarriesNoLockIsUnavailable...`; PB3518 review round 1).
  **The lock-file host answers the same two ways (kb/Work PB3632).** `SidecarLockFiles.Publish` used to answer null for a
  host that takes no lock AND for twelve lost races; it now returns the same `RegionLockResult` vocabulary
  (`Ok` with the holder, `Unavailable`: no lock here, `Held`: every try lost its race), so `SidecarColumnLockHost.Establish`
  tests the refused columns after an exhausted publication (`Unpublishable`, the lock directory a user may not write takes the
  same arm, PB2917) and answers `Refused` ('61') beside a holder Table 19 refuses, `Unavailable` otherwise; and
  `SidecarRegionLocks.Hold` looks at the holders after an exhausted publication, so a conflicting one is `Held` (a waiting hold
  goes on waiting for it) and not a lock this host cannot carry; a waiting hold that finds nothing in the way publishes one
  more round before it answers `Unavailable`, because the holder whose removal of the last lock file lost the races may have
  let go during the round (`RegionLocksDriftTests.AWaitingHoldWhoseHolderLetGoDuringTheLostRound_IsGrantedOnTheNextRound`). Witnesses (the lost races are driven through
  `SidecarLockFiles.RaceInjector`, the seam beside `PauseObserver`):
  `SidecarColumnLockHostDriftTests.AnExhaustedPublicationBesideAHolderTable19RefusesIsRefused_AndOneNothingRefusesIsUnavailable`
  and `RegionLocksDriftTests.AHoldWhoseEveryPublicationTryLostItsRace_IsHeldByAConflictingHolder_AndWaitsForIt`.
  A re-OPEN that fails leaves what the CLOSE kept as it was
  (§14.9.27.4 GR25): a kept handle the re-OPEN adopted is given back to the kept state (`AbandonOpen`), and one it gave away
  to try a share mode it refused is taken again with the access and share mode it had, attached to the store as an OPEN
  attaches its own, so the liveness byte is held by a live run unit again before any other run unit's dead-owner step can
  find it free (`KeyedConnector.RetakeKeptHandle`, run by `AbandonOpen` for every unsuccessful ending after the swap: another
  run unit's refusal of the new handle, a refused adoption, a torn page met while positioning; PB3483). The retake's own
  attach reads the store again when the give-back left it without a file, and a medium that refuses that read does not cost the
  kept state its handle: the liveness byte and the record locks are carried to the retaken handle without a page read
  (`KeyedConnector.CarryKeptHoldings`, the half of the attach the retake keeps whatever the adoption answers; PB3497), and
  the store stays unadopted until the connector's next OPEN reads it — or until the COMMIT or the ROLLBACK, which do not wait
  for that OPEN: a store with a standing transaction, no file and a writable handle adopts its file again through that handle
  at the statement (`KeyedStore.AdoptAgainForTransaction`, under the store mutex; §14.9.7.4 GR3, §14.9.36.4 GR3; PB3630) and
  answers '30' only if the medium still refuses the read. The OPEN's
  GR16 write probe is not made for a connector still holding its kept handle, whose own share mode would refuse it; the
  writable handle the re-OPEN takes asks the same question. So is the OPEN OUTPUT probe (`FileConnector.HeldByAnotherHandle`):
  the kept handle is the connector's own, not *"another file connector"* (§9.1.13.9 1) e)); the keyed connector gives it back for
  the instant of the question and takes it again when another handle does hold the file. ⚠ **A WINDOW, NAMED:** the share-mode
  window — between the kept handle's release and the new handle's taking, and the retake — is the one instant the byte and
  the host share mode are free while the owner lives, and a host whose share modes are per handle cannot close it: a handle
  that refuses the new one is a handle the new one cannot be beside. Another run unit that opens in that instant holds a handle
  the retake is refused by, the kept state is left without its handle (the OPEN answers its own status) and the next writable
  OPEN of another run unit rolls the transaction back as a death's. On Windows the kept column holder (2.2.1.8,
  `HandleColumnLockHost`) is a handle of the connector's own too, and it is NOT given back for the question: the probe
  beside it is a read request that admits only readers (`FileConnector.ProbeHeldByAnother`,
  `HostFile.IsWrittenOrExcludedByAnother`; `CommitRollbackRuntimeTests.A_reopen_is_not_refused_by_the_column_lock_its_own_close_kept`).
  The same window is the one a re-OPEN under SHARING WITH NO OTHER meets on Windows, whose `FileShare.None` handle the kept
  column holder would refuse: it gives way once the NO OTHER row's test has passed. The other way round, a kept handle
  under SHARING WITH NO OTHER (`FileShare.None`) refuses the column holder of the connector's own re-OPEN; it is the NO
  OTHER column itself, beside which no other run unit holds the file, so on Windows such a re-OPEN takes its column after
  the body, through the handle that replaced the kept one (`FileConnector.KeptHandleExcludesEveryOpen`). The Table 19
  run-unit file lock (the column every host publishes) is kept with the handle
  (§14.9.7.4 GR4: *"For files specified in active APPLY COMMIT clauses that have been closed and not reopened prior to the
  COMMIT statement, the file locks are released."*), so another run unit's conflicting OPEN is refused '61' on every host
  until the COMMIT or ROLLBACK, and it has NO such window: the connector's own re-OPEN REPLACES the kept column on the same
  hold (`FileConnector.TakeRunUnitFileLock` → `RunUnitFileLock.Amend` → `IColumnLockHost.Establish`'s `supersedes`: the new
  column is published while the old stays published, the test ignores both as the asker's own, and the old goes only when the
  request is admitted; a refusal changes nothing), and an unsuccessful OPEN puts the kept column back, untested, on the same
  hold (`RunUnitFileLock.Revert`, `KeyedConnector.TryRestoreKeptFileLock`; review lead R2-2). A holder the new column admitted
  meanwhile was admitted by an OPEN in progress, the OPEN's own instant, not a window. **The record locks are kept too** (kb/Work PB3480; §14.9.6.4 GR9: *"Except when the file
  is specified in an APPLY COMMIT clause, the file lock and any record locks associated with the file connector referenced by
  file-name-1 are released by the execution of the CLOSE statement"*; §9.1.16 releases them at a COMMIT or ROLLBACK): the
  registry's CLOSE (`SharedClose`) asks the connector (`FileConnector.CloseKeepsLocks`, the one decision its handle's keeping
  rests on) and releases none of them; they are published through the kept endpoint, so that endpoint is their one owner while
  the connector's re-OPEN carries them on (`PhysicalFileTable.ReleaseThrough`), and **`KeptFile.Release` is the ONE giving back of a
  kept state** (kb/Work PB3578): it releases ALL the record locks the connector holds on the file by `KeptFile.LockOwner` (the name the
  lock table holds them under, handed to the connector at every OPEN with its physical-file state, `FileConnector.AssociatePhysical`),
  the unpublished ones among them, and then the handle, the file lock and the attachment. The registry's COMMIT, ROLLBACK and
  run-unit end, a retired state's end, and the connector's own endings (`KeyedConnector.ReleaseKept`: the OPEN OUTPUT that creates
  the file anew) all come to it, so no lock stays in the table for a connector with nothing left to release it. A re-OPEN that takes a handle of its own moves them to its
  endpoint inside the attach's mutex hold (`PhysicalFileTable.Rehome`, beside `KeyedStore.HoldLivenessThrough`) before the kept
  handle goes, a re-OPEN that fails moves them back (`AbandonOpen`), and one whose kept handle was given back early (the
  share-mode window) takes them off the host with its own handle (`PhysicalFileTable.Unpublish`), leaves them in this run unit's
  table and publishes them again through the kept handle when `KeyedConnector.RetakeKeptHandle` takes it again
  (`PhysicalFileTable.Republish`), whose endpoint becomes their owner (`KeptFile.Endpoint`; the OPEN OUTPUT probe's retake moves
  them there through the attach's `Rehome`); a retake the host refuses leaves them in the table alone, where the COMMIT or ROLLBACK
  ends them like any unpublished lock (§14.9.27.4 GR25). **What a connector holds past its CLOSE belongs to the physical file, not to the
  connector** (kb/Work PB3484, PB3494). A connector that leaves the registry holding it - displaced by the re-registration of its
  element after a CANCEL (`CloseDisplaced`), or dropped with its object (`CloseAndDrop`) - or that an ASSIGN USING associates with ANOTHER
  physical file (§12.4.5.3 GR3 b: the association is made at the OPEN, `FileRegistry.Associate`) hands its `KeptFile` to the registry
  (`KeyedConnector.RetireKept` through its `KeptSink`, `FileRegistry.Retire`; `FileRegistry._retired`): it stays the COMMIT's and the ROLLBACK's (§12.4.6.3.4
  GR5: a canceled element's APPLY COMMIT clause stays active until a COMMIT or ROLLBACK deactivates it; §14.9.7.4 GR3 applies the
  changes of the files of the active clauses, and the changes the connector made to the first file stay changes to a file the clause
  names, whichever file it is associated with next) - `TransactionHolders` finds its transaction (one per physical file), and
  `ReleaseCommitLocks` and `CloseAll` give back what it kept, its unpublished record locks (by name, through its own physical-file state)
  included; a refused ROLLBACK or COMMIT of it is told through the file-name's connector. The OPEN that follows an association with another
  file is an ordinary OPEN of that file: it adopts neither the first file's handle nor its store, while the first file's transaction, record locks
  and file lock stand against other run units. The `KeptFile` carries what a ROLLBACK needs without the connector:
  the store, the restoring trees bound to that store (`KeptFile.Restoring`: the connector's `Restoring` is bound to the store it is
  attached to when asked, never to its field), the physical-file state and the table to detach from. The element's NEXT
  activation, or the connector's association with the same file again, OPENs what the retired state holds as a re-OPEN would:
  `FileRegistry.ReclaimRetired` hands the kept state to the connector of the
  same name, organization and physical file under commit control (`KeyedConnector.InheritKept`) at its OPEN, so its own run unit's kept handle never
  refuses it '61' (§9.1.13.9 1) owes '61' only against a file open by another file connector; the kept file lock is against other run units). Before this the dropped connector's handle, file lock and transaction were beyond every
  COMMIT, ROLLBACK and run-unit end, and the file lock lasted as long as the process; and before PB3494 a connector associated with
  another file adopted the first file's kept handle and store (reads answered the first file's records) and its Table 19 column
  was amended on the first file.
  **An OPEN OUTPUT over a file its connector's CLOSE kept ends the kept state only when the creation has LANDED** (kb/Work PB3493;
  §14.9.27.4 GR18 creates the file, GR25: *"If the execution of the OPEN statement is unsuccessful, the file is not affected"*):
  `KeyedConnector.Recreate` makes the new, empty store in a store of its own (`NewStore`, off the table) through the OPEN's own handle,
  beside the kept attachment, and `PagedFile.Create` undoes a refused creation (every page it wrote goes back), so a refusal - the
  share-mode window, a refused write or sync - leaves the transaction, the record locks, the file lock and the handle as the CLOSE
  left them (the OPEN's `AbandonOpen` runs as for any unsuccessful re-OPEN). A landed creation gives the kept state back
  (`ReleaseKept`) and registers the created store (`StoreTable.AttachCreated`). `PagedFile.Create` REPORTS whether the file is
  still the previous one (`replaced`, read as `KeyedStore.FileReplaced`), whatever its status, and the kept state ends exactly when
  it is not: the creation landed; the final sync was refused after the truncation (nothing left to undo); or the medium refused
  the UNDO as well (review finding R1-1) - the commit slot then holds the empty meta at N, so R6 adopts the empty store at every
  later OPEN, and a kept transaction would stand over a store the file no longer holds and its COMMIT would promote it beside
  that slot and answer success. In those two refusals the OPEN answers '30' (§9.1.13.6 item 1) and the kept state ends with the
  previous file. The undo goes on past a refused page, so the commit slot is the previous file's again wherever the medium lets it
  be (2.2.1.16's row). The same decision
  stands where the kept file is not there at the re-OPEN (an absent OPTIONAL file on INPUT or I-O, removed outside the run unit): the
  kept state belonged to that file and RETIRES (below), the OPEN attaching to none of it.
  **And where the path names ANOTHER physical file or none** (kb/Work PB3533, PB3576): the kept file was removed or renamed away
  outside the run unit and another made at its path, which Linux permits whatever the kept handle's share mode. The `KeptFile`
  carries the host's identity of the file it was kept on (`KeptFile.Identity`, from the CLOSE's handle), and the OPEN that probes the
  path and finds it PRESENT or ABSENT asks first, before the write probe, the file lock and the organization's body read any of the
  kept state, whether the path still names that file (`FileConnector.RetireWhatWasKeptOfAnotherFile` → `KeptFile.IsOfTheFileAt`: the
  same path and, where the host can tell, the same identity, `HostFile.IdentityAt`; an absent path names no file). When it does not,
  the kept state RETIRES to the registry (`KeyedConnector.RetireKept`) exactly as an ASSIGN USING to another path retires it
  (PB3494), and the OPEN is an ordinary OPEN of the file the path names (or of none), whatever it goes on to answer: §14.9.27.4
  GR25's *"the file is not affected"* is about that file, and the kept one is not it, so a '39' or a '61' of the new file leaves the
  retired state as it is. Retiring is possible because `PhysicalFileTable` and `StoreTable` hold the OLD file apart from the file now
  at the path (2.2.1.7): its record and file locks stand against the old file only, the COMMIT (§14.9.7.4 GR3: *"permanently applies
  any changes made to the files specified in all the active APPLY COMMIT clauses"*) commits its transaction wherever it is now
  named, and the ROLLBACK restores it. (PB3533 ended the state instead, leaving its transaction for the next writable OPEN's
  rollback, because the table then held one file per path.) The kept file's identity cannot pass to the new one meanwhile, because
  the kept handle, or the descriptor of the column lock kept with it, keeps the kept file alive; where the host has no identity to
  give, the path decides as before. On a host whose column locks are published by PATH (the sidecar host, macOS), the retired state's
  column file stays published until the COMMIT and the OPEN of the file at the path is arbitrated against it as against any holder
  (§9.1.15 cannot tell the files apart there). The registry's `KeyedConnector.KeptIsOfAnotherPath` (the ASSIGN USING case) stays a
  comparison of paths. Witnesses: `CommitRollbackRuntimeTests.A_reopen_of_a_file_replaced_outside_the_run_unit_retires_what_the_close_kept`
  (removed and renamed, INPUT and I-O, both organizations: the renamed file's change is COMMITted), `...An_unsuccessful_reopen_of_a_file_replaced_outside_the_run_unit_retires_what_the_close_kept`
  ('39', '61'), `...A_reopen_of_an_optional_file_that_vanished_retires_what_the_close_kept`, `...A_retired_kept_states_unpublished_record_locks_belong_to_its_file_and_end_with_the_commit`
  and `...Two_connectors_on_a_path_whose_file_was_replaced_each_reach_their_own_file` (2.2.1.7's store table); on
  Windows the kept handle's share mode admits no delete, so the host refuses the replacement and each witness checks that.
  (4) HOLDINGS and UNDO keys are ordered by the FILE'S key orders (`HoldingsKeyOrder`, kb/Work PB3485), never by their
  bytes: a record holding compares its identity through the prime tree's order and a value holding its alternate key number
  and then the value through that key's order, so two key images the file's collating sequence makes equal (§12.4.5.12.4
  GR1; §14.9.51.4 GR35) are ONE holding, whichever bytes name them. The orders are the connector's, handed to the page
  layer when it opens or creates the file (`PagedFile.HoldingsOrder`), because the page layer retires a transaction (the
  OPEN's fence, the restart rule, a COMMIT's retirement) with no connector in reach and a tree is read by the order it was
  built under. A record's identity is the image its PRIME TREE holds it under (the one its WRITE inserted; a REWRITE
  keeps the tree's key), taken from the tree by a REWRITE, a DELETE and a random READ (`IndexedStore.Get`), so the before-image a
  ROLLBACK restores goes back under the image it came from and the §9.1.16 record lock of every statement names one
  string (`IndexedConnector.MutationTargetRecordIdCore`, `FindRandom`). A key number the order table has none for (the
  `WISEOWL_COBOL_KEYCHECK` opt-out) compares as bytes. Under commit control the prime key bound is 1 015 bytes, one below the
  tree's, for the kind byte (DOC-A.1-129).
  (5) A transaction the run unit OWES a rollback — one whose ROLLBACK the medium refused, or whose file is MARKED
  UNACKNOWLEDGED — is never committed (finding R1-4): a COMMIT, the program's or the implicit one of a run unit ending
  normally, is refused before it touches the medium and its caller rolls back again, so the abnormal ending's refused
  file-only ROLLBACK is retried by the implicit CLOSE and, still refused, left to the dead-owner step; a store left with no
  writable handle refuses COMMIT and ROLLBACK ('30') rather than answering them. The rule is the run unit's registry's, ONE
  place for every storage engine (`FileRegistry.CommitHolder` over the set of owed transactions; the sequential journal's
  too, 2.2.2.7, review finding R1-3 of kb/Work PB3340): a refused ROLLBACK, and a COMMIT that left the transaction
  UNACKNOWLEDGED or reversed and not yet rolled back, each make it owed (a COMMIT refused before its decision does not: the
  transaction is intact, and a declarative's retry may commit it), and only a ROLLBACK that completes, or the transaction's
  end by another path, takes it out.
- **Record locking under commit control** is §9.1.16's implicit *"LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS"*: the
  registry registers that posture for the connector and refuses the LOCK clause and UNLOCK at bind (PB3338); READ locks (GR4,
  in the region), WRITE, REWRITE and DELETE hold the record they change and the unique key values they free (in the file;
  the bullets above), and the locks and holdings are released only by COMMIT, ROLLBACK and termination — a holding by the
  rollback the termination brings, if abnormal. A record the transaction changed is therefore readable by another connector
  only IGNORING LOCK (§9.1.16), which is D.2.5's one tolerated inconsistency.
- **A second connector of the SAME run unit** on the same physical file that no APPLY COMMIT names (§12.4.6.3.4 GR2 forbids only
  the same CONNECTOR being half-covered) changes records without before-images; the run unit's ROLLBACK restores only what its
  covered connectors changed, which is what D.2.5's *"components of the same run unit where the files are also accessed by
  independent file connectors that are not specified in APPLY COMMIT clauses"* describes.
- **The compiler half** (PB3338), as built. The binder accepts the clause for a RELATIVE or INDEXED file
  (`DataBinder.BindApplyCommitClause`, then `ResolveApplyCommit` once the data division is bound: §12.4.6.3.3 SR1-SR10,
  one diagnostic, COBOL4709; SR6 for a factory or instance definition is `OoEnvironmentRules`'), and the claimed
  clauses' antecedent family (LOCK MODE, SAME, OPEN sharing, READ/REWRITE lock phrases, UNLOCK) refuses its forbidden
  shapes in its own right (COBOL2974). The run unit's half of the statements is `TransactionControl`: each element
  instance's activated clause is a PARTICIPANT whose emitted `__CommitSave` / `__CommitRestore` copy its data items (the
  listed identifiers and SR9's file status, LINAGE and RECORD DEPENDING items) to and from an image the instance keeps,
  saved at activation (a ROLLBACK before the first COMMIT restores the state at activation, §14.9.36.4 GR3) and at every
  COMMIT that succeeds (§14.9.7.4 GR5); a canceled element or a returned INITIAL program RETIRES its participant (no
  longer restored; deactivated by the next COMMIT or ROLLBACK, §14.9.7.4 GR3, §14.9.36.4 GR4-GR6), and the deactivation
  takes each file connector the clause named that no remaining active clause names OUT of commit control
  (`FileRegistry.ReleaseCommitControl`, the inverse of `RegisterCommitControl`, putting back the file control entry's own
  lock posture; kb/Work PB3541): the file's later changes are no ROLLBACK's and item 8 no longer applies to it. COMMIT and ROLLBACK
  are run-unit calls (`CobolFile.Commit` / `Rollback`, named by their full name, since a statement's element need
  describe no file); a refused file is answered by name with '30' in its status, stored into the FILE STATUS item by
  the element that describes it, and handed to that file's exception processing. §14.6.13.1.3 item 8's ROLLBACK is ONE
  rule (`SequentialIoEmitter.Item8Applies`, kb/Work PB3542-PB3544), which every hook arm consumes: it is asked at the
  raise (`CobolFile.RollbackDueOnFatal`: checking for the raised condition not enabled, the status fatal, the file under
  commit control; asked before the declarative, whose own CLOSE would replace the status) and run after the declarative
  and before a RESUME transfers control or DOC-A.1-103 ends the run unit (`CobolFile.RollbackOnFatal`); never on a SORT
  or MERGE implicit transfer (item 2) nor after an ON EXCEPTION phrase that processes the condition (item 1). It is the
  ROLLBACK statement's general rules, so in a SORT input or output procedure it raises EC-FLOW-ROLLBACK (§14.9.36.4 GR2;
  every input-output statement on a file under commit control declares the condition, `EcBinder.NodeKindRaises`); its
  own refusal is not told to the program, and `TransactionControl.HasStatusItem` is a question that stores nothing (the
  refusal recorded whether a FILE STATUS item was told). `Item8RollbackSiteDriftTests` pins the one site. The statement's
  node carries the files of its own element's clause under commit control (`BoundCommitRollback.Files`, read from
  `ApplyCommitModel.CommitControlledFiles`, the one statement of "a declined SEQUENTIAL operand is absent"), and they are
  its I-O files (`EcBinder.IoFilesOf`), so EC-I-O checking for one of them raises EC-I-O-PERMANENT-ERROR through
  `__IoCheckEc` exactly as at any input-output statement; a refused file of ANOTHER element has no file description
  here, so its condition is declared with no file and only a directive naming no file enables it
  (`SequentialIoEmitter.EmitForeignFileIoCheck`), and with no declarative qualifying DOC-A.1-103 decides
  (`CobolFile.TerminateOnRefusalWithoutStatusItem`). The refusal leaves the connector in permanent error until its
  CLOSE. A refused COMMIT saves no data item; a refused ROLLBACK still restores them. GR1 is asked before GR2: with no active clause both
  statements are CONTINUE wherever they run. EC-FLOW-COMMIT and EC-FLOW-ROLLBACK are read at the statement's entry;
  EC-FLOW-APPLY-COMMIT (§12.4.6.3.4 GR3/GR4, Table 13's *"Invocation of a runtime element with an APPLY COMMIT clause in
  effect by a runtime element for which commit and rollback is not permitted"*) is the ACTIVATING element's: the CALL
  declares it, its guard sets the flag, `ProgramTable.CallProgram` latches it before the callee's checking scope opens
  (`ExceptionEngine.ActivatorFlowApplyCommit`), and the activation raises under the latch. With checking off a
  forbidden statement is not executed and a forbidden activation leaves the clause inactive. Witnesses:
  `TransactionControlTests` (the refusing medium at every operation of a COMMIT, two files, item 8, the flow
  conditions, retirement), `CommitRollbackRefusalWitnessTests` (the same refusals through a COMPILED program: the
  own-file and foreign-file arms in every combination of a USE procedure, a FILE STATUS clause and EC-I-O checking,
  and item 8 after an ordinary statement's fatal status), the `pb3338_ec_flow_apply_commit_under_*` goldens (the
  activation latch through compiled CALLs) and the `pb3338_apply_commit_*` goldens. Sequential files under APPLY COMMIT (§9.1.18.2 admits *"any type of file on a mass
  storage device"*) are kb/Work PB3340: the sequential organization has no ledger, and its commit facility is a separate design
  that must give a sequential transaction the same scope — 2.2.2, the transaction journal.

**2.2.1.11 Performance targets, and how a test measures them without a clock.** `PagedFile` counts its page reads, page
writes and syncs (`PagedFile.Counters`, cumulative: a witness takes the difference across a statement), and every witness asserts COUNTS, because a wall-clock test is a flake
generator (the repository's rule) and because a count is the thing the bar states. With d = the tree's depth (2.2.1.4 gives the
formulas), s = the pages a statement's splits add (one per level that splits, a root split included; a load's
statements together add exactly the pages its trees gain), ℓ = the ledger's path copy — its root and every ledger leaf
the statement's entries land on, at most the ledger tree's pages: 1 for a load whose pages are adjacent (an ascending
load), but the ledger is keyed by page number and a statement of scattered keys releases pages anywhere in the file, so
there ℓ grows with the ledger's fill up to the whole tree, which held at most 9 pages at R2's checkpoint in the measured workload (the ledger lists 1 024 entries plus the pages of the one statement that begins at it, and a leaf holds 213 of them when full, but the ledger is keyed by page number, so a scattered load leaves its leaves anywhere from half full to full and the page count follows that fill, not the entry count; measured,
PB3336: a random relative load writes ℓ ≈ 5 at 20 000 records, the ascending one 1, and the largest ledger of the twelve load
cells, 1 028 entries under two alternate keys, fills 9 pages; the load witness pins that measured 9 for every statement of every cell
(`KeyedStorePerformanceWitnessTests.LedgerPagesAtCheckpoint`, PB3410), and the OPEN-after-a-death witness asserts it for the
ledger tree it reads); the checkpoint, not the file's
size, bounds it — the free-tree batch ≤ 2 pages (taken once per statement at most), u = the UNDO tree's depth for a connector under commit control, and h = the HOLDINGS tree's depth (≤ 2
for a thousand open holdings), read only when page 2's holdings root is not empty:

| Statement | Pages read | Pages written | Syncs |
|---|---|---|---|
| OPEN (any mode, file present) | 3 + the ledger tree's pages and its `fresh` pages, each read once (R6's verification; after a CLOSE: the retirement's own, 2 measured; after a death: every `fresh` page the ledger lists, which R2's checkpoint bounds at 1 024 entries PLUS one statement's — `PagedFile.Begin` promotes only a ledger that already lists more than 1 024 when the statement begins, so the statement that begins at the checkpoint adds its own pages first: 1 028 entries measured under two alternate keys; the witnesses assert `LedgerCheckpoint` + the most pages one statement can list) + the transactions tree (≤ 2) | INPUT handle: 0. Writable handle: the fence, 1 (its claim) + 1 (a committed slot) + its retirement (≤ 2 ledger + 3 free tree + 1 meta); a dead transaction's rollback: its own statement. OUTPUT: 3 | writable: 2; INPUT: 0 |
| the coherent entry (every statement under the mutex, 2.2.1.7) | 1 (page 2) + when page 2's generation moved and the run unit's store is writable, the ledger tree's pages (it is loaded whole, so ≤ ℓ's bound; measured, PB3335: a writer allocates from the ledger it adopts) + through a writable handle, when page 2's transactions root is not empty, ≤ 2 (the TRANSACTIONS tree) and one liveness probe per transaction of another run unit (a host lock test, no page) | a dead transaction found: its rollback's own statement | 0 |
| READ by key / by RRN | ≤ d + overflow pages of the record (+ the coherent entry when the mutex is held; + h for the holdings lookup of a READ not IGNORING LOCK, when holdings exist); by an alternate key, its tree's d + 1 when the prefix seek of its (key value, release ordinal) composite lands past a leaf's last entry (a value that begins the next leaf: 13 to 17 in 1 000 measured) + the prime tree's d | 0 | 0 |
| READ NEXT / PREVIOUS | ≤ 1 (+ d when the cursor crosses a leaf; + d for an alternate key's prime lookup; + h as above) | 0 | 0 |
| WRITE / REWRITE / DELETE (indexed, k alternate keys) | ≤ (k + 1) × d + h × (1 + the unique values inserted), when holdings exist | ≤ (k + 1) × d + s + ℓ + 2 (free batch) + 1 (meta) + 1 (the claim, under the mutex only); under commit control + u × (1 + the unique values freed, one path copy per leaf the UNDO entries land on) + h × (the same count, the holdings' path copies) + the before-image's overflow pages | 0 |
| WRITE / REWRITE / DELETE (relative) | ≤ d + h when holdings exist | ≤ d + s + ℓ + 3 (+ 1 claim under the mutex); under commit control + u + h + overflow (a relative record has no unique alternate key, so one UNDO and one HOLDINGS entry) | 0 |
| an unsuccessful statement: '22', '23', '51' (and each attempt of its RETRY), a replayed permanent error (§9.1.13.1) | ≤ the paths it searched (+ the coherent entry); a replayed permanent error reads NOTHING — not the coherent entry's page 2, and it takes no store mutex: the statement *"does not touch the physical file"* (docs/CONFORMANCE.md DOC-A.1-105, §9.1.13.1), so `KeyedConnector.BeginStatement` is inert while the condition is in effect and no lock question is asked before the verb replays it, the condition outranking a '51' | 0 | 0 |
| a promotion (CLOSE or checkpoint; COMMIT; normal termination; the fence at OPEN) | 2 (both committed slots) + ≤ 2 (the ledger) + 3 (the free tree's path) | 1 (the claim with PROMOTING) + 1 (a committed slot) + the retirement (≤ 2 ledger + 3 free tree + 1 meta) | 2 (CLOSE: 3) |
| COMMIT, per file with a transaction | ≤ 2 (the TRANSACTIONS tree) + the UNDO tree whole — u + its leaves, one walk, which lists every R and V holding (finding R5-2) | the promotion, whose retirement also removes the entry and the holdings the walk listed and releases the UNDO tree (+ ≤ 2 + the HOLDINGS pages those entries sit on, one path copy per leaf) | 2 |
| COMMIT, sync 2 refused (the reversal, 2.2.1.10 region 3) | as above | the reversal statement (the retirement's pages + the ROLLBACK row's) + a second promotion (its claim, slot and retirement) | + 2 |
| ROLLBACK, per file with a transaction | ≤ 2 + the UNDO tree whole (the same walk) + ≤ (k + 1) × d per before-image | ≤ (k + 1) × d per before-image + the HOLDINGS pages its entries sit on + the statement's own bookkeeping, paid once (ℓ + 2 free batch + 1 meta, + 1 claim under the mutex: the WRITE row's terms; measured 2026-10-20, kb/Work PB3337: the earlier "+ 2 + 1" left out the ledger's path copy, which a one-record ROLLBACK already exceeds), in one statement; on a file marked UNACKNOWLEDGED + a promotion | 0; marked: 2 |

The witnesses: (a) **linearity** — a load of N and of 4N records (N = 5 000: the sizes straddle the prime tree's root
split) through `NO OTHER` and through `ALL OTHER`, ascending and random keys, RELATIVE and INDEXED with none and with two
alternate keys, asserts that no statement wrote more than its row (with ℓ the ledger tree's pages after it, and the
statements' excess over the no-split row summed over the load at most the pages the trees gained), that the ledger never
lists more than R2's checkpoint and one statement's pages, and that the mean pages written per statement at 4N exceed the
mean at N by at most the depth growth, the ledger's growth and the FREE batch — nothing that grows with the file;
(b) **OPEN reads the header** — opening a store of N or 4N records after a CLOSE reads ≤ 3 + 7 pages (the three slots, the
retirement's ledger and free-tree pages, the transactions tree; 5 measured), whatever N, and writes nothing through an INPUT
handle and ≤ 8 pages (the fence's claim, slot and retirement; 5 measured) through a writable one, syncing twice; after a
death, the OPEN reads 3 + the ledger tree's pages and its fresh pages, and adopts the dead run unit's completed statements
(R6); (c) **random READ is O(log n)** — 1 000 random READs on the 4N store read exactly d pages each, + 1 (page 2) under
the mutex, with the cache cold (`PagedFile.DropCache`, reached from a connector through `FileRegistry.PagedStoreOf`), and by
the unique alternate key its path, the prime tree's and the crossing row above. The READ NEXT walk reads every page of the
tree once and its AT END READ answers '10'; and every unsuccessful row above is a case beside its successful twin — a WRITE
refused '22', a READ and a DELETE refused '23', a WRITE the medium refuses ('30'), then the permanent error's replay by WRITE, READ, READ NEXT, START, REWRITE and
DELETE, one case each in both sharing modes, each reading, writing and syncing nothing (the unsuccessful row), a READ
meeting another run unit's lock ('51', a RETRY of three attempts, and a RETRY that sees the lock released and succeeds); (d) **no whole-store I/O exists** — a
drift test of `Runtime/IO` (`WholeStoreBanDriftTests`; a specific rule of those files by `drift_rules.py`'s depth, not a `Sweep` trait's tree-wide sweep, measured PB3335) asserts that `src/WiseOwl.Cobol.Runtime/IO/Store/` is the ONLY code under
`Runtime/IO` that calls `RandomAccess.Read`/`Write`/`GetLength`/`SetLength` on a keyed handle and that it never reads or writes
more than one page per call, and that `ArrayPool<byte>.Shared.Rent` with a file-length argument appears nowhere under
`Runtime/IO` (the shape `ReadImage` has today); (e) **a transaction costs its own records only** — a run unit's ROLLBACK after
m changed records writes ≤ m × ((k + 1) × d) + the HOLDINGS pages its entries sit on + the statement's own bookkeeping
(the ROLLBACK row above) and leaves every record of another run unit byte-identical; a COMMIT reads its UNDO tree once.
Witness (d) is `WholeStoreBanDriftTests` (kb/Work PB3335); (a) to (c) are `tests/WiseOwl.Cobol.Tests.Unit/Store/
KeyedStorePerformanceWitnessTests.cs` (kb/Work PB3336: one class per file shape, the real connectors over `PageArrayMedium`,
an in-memory page array per physical file, so a load costs no host I/O and the counts are the same on every host); (e) needs
the transaction runtime and is `KeyedStorePerformanceWitnessTests.A_transaction_costs_its_own_records_only` (kb/Work
PB3337: 40 records rolled back write 30 pages at d = 2 with no alternate key and 104 under two, and the COMMIT after them reads
its one-page UNDO tree once, 7 and 9 pages in all). For the
record, the prototype's wall-clock arithmetic: a million-record sequential relative load is about 6 page writes a statement at
8 µs, some 50 s of I/O before the tree's own work; the same load through today's `ALL OTHER` path is 10^6 × O(file) and does
not finish.

**2.2.1.12 The format's drift tests (each invariant pinned by one).** `PagedFile` is written over an `IPageMedium` — the
production medium is `RandomAccess` on the connector's `SafeFileHandle`; the test medium is an in-memory page array that
records which writes were SYNCED and can be asked to *lose every unsynced write* (a power loss) or to *keep them* (a process
death), to *tear* one named page, and to *serve an older version* of one named page (a lost write over a reused page), and
whose length is in BYTES (so a previous file of any length, a whole number of pages or not, is a case), and whose
per-run-unit handle kills or refuses that run unit at its n-th OPERATION (a write, a sync or the truncation) — before
it, after it, refused, or refused because the file cannot grow further — and refuses its n-th READ — so every crash and
refusal scenario is deterministic and runs on every host without a VM:
- R1: no write ever targets a page that the committed meta's tree OR the work meta's tree references (the medium records every
  write's page number; the test walks both trees after each statement);
- R2: the ledger lists exactly the pages allocated since promotion, with their last generation and state; a `released` page is
  reused only by a statement later than the one that released it; a statement never allocates after it starts writing the
  ledger (the medium's write order); the 4 097th statement after a promotion promotes before it stamps (the ceiling), and a
  ledger past 1 024 pages promotes at the next statement;
- R3: a page served at an older version under a stamped pointer is refused as torn by the next read of it, in a statement
  ('30') and at OPEN (the work slot rejected); and the corollary — the medium's write log never records one (page,
  generation) pair twice, across a rejection and a restart (finding R2-2's scenario: a fresh page of statement 105 reclaimed
  after a torn page of statement 110, the rerun's first stamp is the fence's), across a death mid-statement and a restart
  on the same reservation, and across a death mid-statement in one run unit and the next statement of ANOTHER run unit on
  the same open file (finding R3-4's scenario: A's WRITE claims g + 1, writes the lowest FREE page p and dies before page 2;
  B's WRITE finds the mint at g + 1, claims g + 2, reserves p and stamps it g + 2; the medium then keeps A's p and B's page
  2, and the OPEN rejects the work slot — the committed meta, never a torn state, at '00' for nobody); the same with A
  refused at its n-th write instead of killed; and a changing statement under the mutex writes page 2 twice — the claim before
  its first page write, with the roots unchanged, and the completion last; a NO OTHER connector's statement refused at its
  n-th write and then its CLOSE (finding R4-3): the retirement's stamps lie above the refused claim, the medium's log holds
  no (page, generation) pair twice, and the power-loss variant that keeps the retirement's page 2 and loses its write of
  the page the refused statement had stamped rejects the work slot at the next OPEN; the same across a refused statement
  and the same run unit's next statement through a second connector of the same store; and no mint on page 2 — `stamp
  mint`, `transaction mint`, `release mint` — ever decreases across the medium's write log;
- R4: a page freed at generation G is not allocated before the promotion of G, the promoted ledger's own pages included
  (finding R3-2: after a CLOSE and 10 WRITEs, no write targets a page the committed meta's ledger root reaches);
- R5: the committed slot's pages are all synced before the slot is written (the medium's sync log orders them), two
  consecutive promotions write different slots, a promotion reads both slots before it chooses (two run units promoting in
  turn never overwrite each other's committed meta), and every promotion is followed by its retirement — page 2 with the
  promotion's generation as its base and PROMOTING clear — before any further statement (the medium's write order); the
  restart: the promoter dies after each of its claim, its first sync, its slot write and its second sync, and after some of
  the retirement's page writes, and another live run unit's next statement completes the promotion (both slots and page 2
  then name it, the retirement's pages are stamped above the dead retirement's claim) before its own work; the same with
  the promoter REFUSED at each point instead of killed (round 4): a checkpoint or CLOSE promotion is completed by the next
  entry whatever the point; a COMMIT refused before its slot write is UNWOUND by the next entry — another run unit's
  statement, or the owner's own ROLLBACK, which then restores every before-image — and its transaction, UNDO tree and
  holdings are intact until then; a COMMIT refused in its RETIREMENT (after sync 2) reports success, and the next entry
  completes its retirement; a COMMIT whose SYNC 2 is refused (finding R5-1; the medium accepting writes, refusing the
  sync) REVERSES under its own mutex hold and reports failure, and afterwards — through the owner's unconditional
  ROLLBACK, another run unit's next statement, the next OPEN, and a power loss — the records are as at the previous COMMIT
  and the HOLDINGS and TRANSACTIONS trees hold nothing of t (a COMMIT that answers after a LANDED reversal has had its
  reversal's promotion synced, and a sync makes every earlier write durable, so no subset of {the COMMIT's slot, the
  reversal's pages, the reversal's slot} is left to lose; a power loss DURING the reversal, which the owner never survives
  to be told, is 2.2.1.16's reversal rows: committed while it keeps the COMMIT's slot and loses the reversal's pages, rolled
  back once the reversal's promotion has synced — measured, kb/Work PB3337, `TransactionFaultMatrixTests`); the same with a second run unit's statement injected between the COMMIT's return and the
  owner's ROLLBACK (it meets the reversed state, never a landed COMMIT to complete); the same with the reversal's own page
  write refused, or its promotion's sync refused: the COMMIT reports failure, the ROLLBACK reports failure while the sync
  is refused and success once a sync lands, and NO run reports a ROLLBACK successful while the medium holds the COMMIT's
  slot as its highest valid slot; the owner killed in either state is rolled back in the first and left committed in the
  second; a power loss at each point gives the same two outcomes through R6 (a DESCENDANT with the flag: the fence
  unwinds; the PROMOTION SOURCE: the fence finishes); and in no run is a COMMIT reported failed with its transaction
  gone, or reported successful and then rolled back, or a ROLLBACK reported successful and its transaction found
  committed by any later OPEN;
- R6: the adoption matrix — rows {a CLOSE then an OPEN; process death after a completed statement; process death
  MID-statement (after some of its page writes, before its meta); process death between a promotion's slot write and its
  retirement's page 2; a fence landed and its retirement did not; power loss with every write durable; power loss losing the
  last k writes; a torn fresh page; a torn work slot; a torn committed slot (the one the promotion was writing); both
  committed slots torn; an OPEN by a second run unit while the first holds the file; an OPEN by a third run unit after one of
  two holders died; an INPUT OPEN of a crashed file, then a writable OPEN in another run unit while the reader is live; a
  death mid-statement in one run unit, a completed statement of another live run unit, then a power loss that keeps the dead
  statement's pages and loses the live one's (finding R3-4); a promoter's death between its claim and its retirement's page 2
  while another run unit is live, then that run unit's statement} × columns {no transaction; a transaction open at the
  crash} → the expected cell: the work slot (= the last completed statement, a DESCENDANT) for the first four rows and for
  the second-run-unit row (nothing rolled back while the owner's liveness byte is held; a writable opener fences the live
  holder's last completed statement, and the holder's next statement adopts page 2); the committed meta — the same tree —
  for the promotion-source and fence-without-retirement rows, completed by the writable opener's retirement; the committed
  meta for the R3-4 row (the live statement's page 2 names its own stamp on the page the dead statement's write survived
  under, R3 refuses it, and no record of either WRITE is reported present), and the work slot — the completed promotion plus
  the live run unit's statement — for the promoter's-death row; the work slot for the torn-committed-slot row (the other committed slot is the
  committed meta, and the work slot, synced before the tear, is a DESCENDANT that verifies); the committed meta (= the last
  promotion) for the lost-writes row unless every listed page verifies, and for the torn-fresh-page and torn-work-slot rows,
  with the rejected ledger's pages in the FREE tree after the fence; '30' at the OPEN for both committed slots torn; for the
  INPUT-then-writable row, the reader's statements walk the committed tree until the writer's fence lands and page 2's tree
  from then on, never the rejected tree (finding R2-5); and in the transaction column, after the adoption, every before-image
  of the dead owner re-applied by the first writable statement — an OPEN's R6 step 5 or a live run unit's coherent entry,
  whichever comes first (the third-run-unit row: by the live holder's next statement if it comes before the OPEN, else by
  the OPEN, with the live holder's own records untouched either way) — and left in place by an INPUT OPEN — with the file
  reading back a consistent tree in every cell, and every writable-handle cell ending with the committed meta (the
  fence's slot) and page 2 naming the adopted state (the other committed slot holds the meta the fence superseded);
- the statement's atomicity: a medium that refuses the n-th write of a statement leaves the file at the previous statement
  and the statement at '30';
- the layout: a golden hex dump of an empty relative store and of an empty indexed store with two keys (the three meta pages
  byte for byte), and the page checksum detecting a single flipped bit in every page kind;
- the engine's tree: a property test of insert/delete/seek/next/prev against a `SortedDictionary` model under every key order,
  including composite keys, overflow values and splits and merges at every level (PB3332's `BTreeModelTests`, with the shape
  and I/O counts of 2.2.1.4 and 2.2.1.11 in `BTreeShapeTests` and a tree statement killed, killed just after and refused at
  each of its writes in `BTreeCrashTests`); a writer's coherent entry that adopts another run unit's page 2 reads the
  LEDGER tree whole (it is resident: allocation needs its released pages, R6 step 3 its fresh ones), at most the
  checkpoint's few pages;
- the transaction: two run units on one file, each under APPLY COMMIT, interleaving WRITE, REWRITE and DELETE on disjoint
  records; A's ROLLBACK restores A's records to A's last COMMIT and leaves B's as B left them; A's COMMIT then B's ROLLBACK
  restores B's; a run unit that CLOSEs and then ROLLBACKs restores the state at its last COMMIT; a first change under
  commit control refused at its n-th write (finding R4-2): the run unit holds no liveness byte afterwards, page 2's
  `transaction mint` lies above the id it drew, and another run unit's first change draws the next id with no wait and no
  refusal; the same with the refused run unit killed instead of alive, and with its claim write refused (the mint
  unmoved, the byte given back, the other run unit draws the same id); a byte a stranger holds is skipped and the id above
  it drawn;
- the transaction's locks (findings R2-3 and R2-4): A under APPLY COMMIT DELETEs K5 without a READ, B (not under commit
  control) WRITEs a record with prime key K5 → '51', and after A's ROLLBACK the file holds A's K5 as it was at A's last
  COMMIT; A WRITEs K1, B's READ K1 → '51' and READ IGNORING LOCK → the record, B's REWRITE K1 → '51'; A REWRITEs K1's unique
  alternate key from X to Y, B WRITEs K2 with X → '51', B WRITEs K2 with Y → '22'; A DELETEs K1 (unique key X), B WRITEs K2
  with X → '51', A ROLLBACKs → K1 reads back under X and K2 does not exist; the same four with B in the same run unit through
  a connector no APPLY COMMIT names; A's COMMIT releases every lock and B's refused statements then succeed; a transaction of
  300 changed records answers no '53' or '54'; **every holding is found** (finding R5-2): A REWRITEs K1's unique key X → Y
  then Y → Z, and A WRITEs K8 with W then DELETEs K8, then COMMITs (or ROLLBACKs): B's WRITE with Y, and with W, answers
  '00' (no '51', no loop) and the HOLDINGS tree is EMPTY; the same with A killed instead and B's statement rolling A back;
  and the drift test that after EVERY statement of every scenario the HOLDINGS tree's key set equals the union of the
  TRANSACTIONS tree's UNDO trees' key sets (walked by the test through the trees, not through the engine's own
  enumeration);
- the dead owner (finding R3-1): A under APPLY COMMIT DELETEs K5 (unique alternate key X) with no READ and is killed (the
  test medium and lock host release A's handle: its liveness byte; its K5 and X holdings stay in the file); B, not under
  commit control and live with the file open, WRITEs K5 → the entry rolls A back first, so K5 exists again with X and the
  WRITE answers '22'; B WRITEs K7 with X → '22'; B READs K5 → the record as at A's last COMMIT; the same with A's REWRITE of
  K1's unique key X → Y and B's WRITE of K2 with X ('22' after the rollback, not '00'); an INPUT connector of a third run unit
  reading between A's death and B's statement finds K5 absent (the file as it stands) and after B's statement present; and
  a WRITE of B's that found no dead transaction (A's byte held, A alive and idle) answers '51' as before;
- the holder's death at every point of another statement (finding R4-1): the same A (DELETE K5, unique value X, idle) is
  killed, in turn, after B's entry probe found it alive, after B's region test, after B's holdings lookup, after B's claim,
  between two of B's page writes and after B's page 2 — the test lock host releasing A's byte at exactly that point — for
  B's WRITE K5 and B's WRITE K7 with X: the answer is '22' (A rolled back at B's test) or '51' (A alive at B's test; the
  next entry rolls A back, and B's RETRY then answers '22'), never '00'; K5 reads back as at A's last COMMIT in every case;
  and no record B wrote is ever overwritten by A's rollback (B's WRITE K9, unrelated, in every window: '00' and present
  afterwards). A holds nothing in the region but its liveness byte (its K5 and X locks are holdings, 2.2.1.10), so the
  region's release order plays no part (2.2.1.17's round-4 table);

**2.2.1.13 What the design deletes and what it keeps (CLAUDE.md rule 4: no wrapper, no shim, no reader of the old format).**
Deleted with their callers (by kb/Work PB3333-PB3335; `WholeStoreBanDriftTests` keeps every name below from coming back): `RecordFraming.ComposeStore`, `DecodeStore`, `DecodeHeader`/`WriteHeader`'s store-level form,
`ReadHeader`'s stream walk (replaced by the meta-page reads), `GapTag`/`GapBytes`/`EncodeGap`, `ImageMedium`,
`MaxStoreBytes`, `HeaderBytes`, `OrdinalSlots`, `FrameBytes(StoredFrame)` and the boundary arithmetic; `KeyedConnector.Load`,
`Persist`, `PersistIfChanged`, `PersistIsReachable`, `ReadImage`, `ReadThroughAreas`, `WriteThroughAreas`, `ReadAt`,
`ReadGeneration`, `Reload`, `Fill`, `PersistFrames`, `ReleaseMint`, `StoreHolds`, `_storeHeaderBytes`, `HostFaultObserver` and
`HostFaultPoint`; `KeyedStore.Version`/`PersistedVersion`/`Generation`/`VersionAtEntry`; `RelativeStore` and `IndexedStore` as
in-memory record maps (their names may survive as the two tree-handle views, their maps may not); `KeyedStoreTable` as the
store of records; `KeyedConnector._coherent`'s derivation from `FileLockPosture.AdmitsAnotherWriter` — the keyed connector
asks `AdmitsAnotherRunUnit` (2.2.1.7), a new predicate beside the old one. `AdmitsAnotherWriter` itself STAYS (finding R2-7):
its four callers outside the keyed store — `FileSupport.OpenConnectorWriteStream` (the unbuffered shared append stream),
`EnsureReaderCoherent` (the reader's re-seek), `ReleaseRecord`'s ordinal (counted off the medium, kb/Work PB2692) and the
EXTEND's mint seed — ask exactly "may another WRITER exist", the sequential organization's question.
`SequentialConnector.ReleaseRecord`'s per-record flush is NOT among them (kb/Work PB1587, PB3415): it asks `AdmitsAnotherRunUnit`, because the INPUT reader §9.1.15 2) admits beside a `SHARING WITH READ ONLY`
writer must find in the physical file every record a WRITE answered '00' for (§14.9.51.4 GR12), exactly as it does beside
a RELATIVE writer; a flush per record for such a writer is the price of that, and `SHARING WITH NO OTHER` keeps its
buffered load. The rule is a
property of the ENGINE, not of the posture: a per-statement-writing engine must serialize against any other run unit, a
streaming writer only against another writer (its append position); so the predicate each connector asks follows the engine
it drives, and the whole-store engine's last caller left with PB3335. The key descriptor codec moved from `RecordFraming` to
`MetaPage`, the one format that records a key, and `StoreFormat` with it. Kept, unchanged in contract: the frame codec (`EncodeFrameHead`, `ReadFrame`,
`ReadStreamFrame`, `CountFrames`, `StreamFramingParses`, `IFrameMedium`, `StoredFrame`, `FrameHead`, the extent-table codec —
the sequential connector and `CobolSort` use them), `FixedFileAttributes` and its comparison, the region map and every lock
class, `PhysicalFileTable`, `StoreStatement`'s shape (`BeginStatement`/`Dispose`; its `Complete` step became a pass-through
once every verb wrote its own pages, and PB3335 deleted it), `FileRegistry`'s statement
wrapping, every status the organizations answer and the order they answer it in (DOC-A.1-104). The input-output areas of
§12.4.5.14.3 GR1 (*"If the RESERVE clause is specified, the number of input-output areas allocated is equal to the value of
integer-1"*) become the page cache's floor: the cache for a physical file holds at least the attached connectors' areas (one
page each) and at least the engine's working set (d + 3 pages), with the run unit's default of 256 pages (1 MiB) above both; a
RESERVE larger than the default enlarges the cache, and `HostFile.InputOutputAreaBuffer` sizes it in the one place it sizes
today.

**2.2.1.14 The implementation, as groups the fix lane lands one mechanism at a time** (the register entries are kb/Work
PB3331-PB3338; this paragraph is a pointer, not a tracker). PB3331 the page layer and the ledger over `IPageMedium`, with the
crash matrix (in the tree: `src/WiseOwl.Cobol.Runtime/IO/Store/` — `PagedFile`, `MetaPage`, `PageHeader`, `PagePointer`,
`Ledger`, `HandlePageMedium` — used by no connector yet, and the simulator and its cases under
`tests/WiseOwl.Cobol.Tests.Unit/Store/`; the LEDGER and the FREE set were serialized as overflow chains until
PB3332's tree replaced both); PB3332 the B+ tree over it
(in the tree: `BTree`, `BTreeCursor`, `IKeyOrder`, `KeyOrders`, `OverflowChain`, with the LEDGER a resident tree and the FREE
set the FREE tree, and its model, shape and crash cases beside the page layer's); PB3333 RELATIVE on the engine (its connector, its tests, the
relative halves of DOC-A.1-107 and of DOC-A.1-75 — and, because it is the first connector that writes at every statement, the coherence
condition of 2.2.1.7 for the connectors on the engine: `AdmitsAnotherRunUnit` with the mutex for every mode but NO OTHER,
while the whole-store connectors keep `AdmitsAnotherWriter` until they leave; finding R2-6 showed that leaving the condition to
PB3335 landed a READ ONLY relative writer with no mutex beside the INPUT reader Table 19 admits; in the tree: `RelativeConnector`
over `RelativeStore` in `KeyedStore.cs`, `Store/PageCache`, `Store/RunUnitPageMedium`, `StoreStatus.Boundary`,
`KeyedConnector.CoherentUnder`/`EnterStore`/`CompleteStore`/`InStoreMutex` with the whole-store machinery moved to
`IndexedConnector.WholeStore.cs`, and the connector-level crash, refusal and interleaving cases in
`tests/WiseOwl.Cobol.Tests.Unit/RelativeEngineTests.cs` over `KeyedStore.MediumObserver`); PB3334 INDEXED on the engine
(the trees, the ordinals, the collations, the indexed half of DOC-A.1-107 — and, once both organizations drive one engine, the
keyed lifecycle written ONCE, kb/Work PB2753; in the tree: `IndexedConnector` over `IndexedStore` in `KeyedStore.cs`, the
engine handle `KeyedStore` both stores derive from, with `KeyedStore.Change` the one multi-tree statement and
`KeyedStore.MediumObserver` the one fault seam; `KeyedConnector.OpenCore`/`CloseCore`/`Attach`/`Detach`/`FormatHolds`;
`IndexedConnector.WholeStore.cs` deleted, so no connector reaches the whole-store model; the connector-level cases in
`tests/WiseOwl.Cobol.Tests.Unit/IndexedEngineTests.cs`); PB3335 the deletion of the whole-store model's remains and
the rest of page-granular coherence (in the tree: `StoreTable`, `IO/KeyedStore.cs`, a reader's entry through a shared
mutex, `PagedFile.Enter(exclusive)`, and the reader's re-adoption under the mutex, `KeyedConnector.Detach`; the rest of
DOC-A.1-75; the whole-store ban, `WholeStoreBanDriftTests`; the connector-level coherence cases in
`tests/WiseOwl.Cobol.Tests.Unit/StoreCoherenceTests.cs`); PB3336 the performance
witnesses (a) to (d) of 2.2.1.11; PB3337 the transactions, COMMIT and ROLLBACK in the runtime, with their locks and
witness (e); PB3338 the compiler's APPLY COMMIT.
Same-file work is sequenced by `blocked_by`, and every group lands green on its own: no landing between two groups leaves main
with a sharing mode the engine writes under and the mutex does not cover.

**2.2.1.15 Who writes what, who reads what (the round-2 consistency pass, 2026-10-17).** Every persistent structure of the
format and every in-memory state the rules depend on, its writers (the rule, the moment, the mutex) and its readers (and what
each reader relies on). A change to any rule above is checked against this table; a row with a reader no writer serves, or a
writer no reader expects, is a design defect.

| Structure | Written by | Read by |
|---|---|---|
| **Committed slots, pages 0 and 1** (one is the committed meta C, the other the slot the next promotion takes) | R5 only: every promotion — CLOSE, `CloseAll`, COMMIT, the checkpoint (R2's ceiling or 1 024 ledger pages), R6's fence — writes the slot that does NOT hold C, after a data sync and before a second sync; an ordinary promotion copies the work meta with `committed generation` := its generation, the fence copies the ADOPTED meta with generation and `committed generation` := G + 4 097. Under the exclusive mutex for a coherent connector; a NO OTHER connector has nobody to exclude. | R6 at OPEN (the higher valid generation is C; `MetaPage.Describes` for the fixed attributes reads the same two pages); 2.2.1.7 never (a live statement reads page 2 only). Readers rely on: a slot that validates names a tree whose every page is durable (R5's first sync) and untouched since (R1, R4). |
| **Work slot, page 2** | Every statement that changes the file (the retirement included), TWICE under the mutex: the CLAIM before its first page write (`stamp mint` := its generation, the other mints advanced past what it will mint, everything else as read; a promotion's claim also sets PROMOTING, a COMMIT's sets `committing`, and the fence's claim rewrites page 2 to the adopted meta at generation F) and the completion LAST, after its data, FREE-tree and ledger pages, with generation := its claim and `committed generation` := its base (PROMOTING clear); a NO OTHER connector writes only the completion; `OPEN OUTPUT`/creation writes the initial three pages. Under the exclusive mutex (coherent) or none (NO OTHER). Never by a promotion's slot write itself and never by an INPUT connector. | R6 at OPEN (classified DESCENDANT / PROMOTION SOURCE / FOREIGN by base and generation against C's G; the mint and the flag play no part); 2.2.1.7 at every coherent statement (one read; the roots adopted when `generation` differs from the one last seen, the next claim raised when `stamp mint` is above it, the promotion completed when PROMOTING is set). Readers rely on: a page 2 written while the mutex was held names a complete tree (a claim changes no root; a completion's pages landed first), and a torn page 2 fails its checksum. |
| **`generation`** (in page 2 and the slots) | Each completed statement: its claim; the fence: G + 4 097; a promotion copies it. | 2.2.1.7's comparison with the generation last seen; R6's classification; R3's stamps are drawn from it. Relies on: R2's ceiling (no history claims above its base + 4 096) and the fence (every history starts above the superseded one), so no (page, generation) pair repeats. |
| **`stamp mint`** (page 2 only; finding R3-4) | The claim of every changing statement under the mutex, before its first page write: one above the highest of the generation last seen, the mint and the store's own last claim (finding R4-3; a NO OTHER connector's claim is the memory alone); the completion and every promotion copy the claimed value. | 2.2.1.7 at every coherent writer's entry (a mint above the generation last seen means a dead or refused attempt stamped pages under it; the next claim goes above). Relies on: the claim landing before any page carries its stamp (the write order within one statement, under the mutex), and the memory keeping a refused claim for the connector that made it. |
| **PROMOTING** (page 2's flags bit 0) | Set by a promotion's claim; cleared by its retirement's page 2 (R5), by a COMMIT's reversal statement (which IS that COMMIT's retirement; round 5) or by an unwind (an unlanded COMMIT's claim; round 4). | 2.2.1.7 at every coherent writer's entry and R6 at OPEN (set under a free mutex = the promoter died or was refused: both slots are read; landed → completed, unlanded → run again or, for a COMMIT, unwound). Relies on: the promoter holding the mutex from the claim to the retirement's page 2 while it runs, so nobody else wrote page 2 in between; a promoter that answered '30' released it with the flag set, and the slots say what it had done — a COMMIT refused at its sync 2 reverses before it releases (finding R5-1), so a flag beside a landed slot is never an unacknowledged COMMIT's. |
| **`committing`** (page 2 and the slots; round 4's self-attack) | A COMMIT's claim sets it to the transaction's id; the slot write carries it; the fence's claim copies the adopted C's (0 for an adopted W); the retirement that removes the entry clears it; a reversal clears it (round 5); an unwind clears it; every other page 2 copies it as read (0 in the steady state). | R5's restart rule and R6 step 4 (an unlanded COMMIT's claim is unwound; a landed one's retirement removes the entry it names). Relies on: it is meaningful only in a committed slot — in page 2 it is a claim — and an entry whose owner is dead is rolled back unless the committed slot names it, which the restart resolves before the dead-owner step probes anything. |
| **The UNACKNOWLEDGED mark** (in the run unit's memory, per physical file; round 5, finding R5-1) | Set by a COMMIT whose sync 2 was refused and whose reversal was then refused too (a page write or its promotion's sync); cleared by the first promotion of this run unit on the file whose sync lands. | This run unit's ROLLBACK (item 8's, or the termination path's): the reversal again if page 2 still carries the entry under the flag, then a promotion of page 2, the answer by its sync; '30' when the entry is gone and this run unit did not remove it. Relies on: the operating environment holding the COMMIT's slot as C until a higher slot lands, so a successful sync of any descendant of the reversed state settles the medium on the rollback. |
| **`committed generation`** (the base) | A promotion sets it to its generation in the slot; the retirement copies the promotion's generation into page 2; every later statement copies page 2's. | R6 (DESCENDANT = base equals G); the ledger's meaning (pages allocated since the base). |
| **`release mint`, `transaction mint`** | The CLAIM of the statement that mints an ordinal or begins a transaction (in the store's memory; on page 2 under the mutex, before its first page write; finding R4-2) and its completion; promotions copy them. | WRITE/REWRITE (the next ordinal), the first change under commit control (the next transaction id, DRAWN by taking its byte; a held byte is skipped). A rejected history's values are discarded with it: an ordinal re-minted orders only the live tree; a transaction id is never written without its byte having been taken first, so a stranger's held byte is skipped, never contended. |
| **`page count`** | The statement that grows the file, in its completion; promotions copy it. | Allocation (growth). A page written past it by a dead or refused statement is reached by nothing and reused by the next growth. |
| **Root pointers** (prime/record tree, alternate trees, FREE, LEDGER, TRANSACTIONS) | The statement that path-copies that tree, in page 2; promotions copy them. | Every tree walk, through R3's stamp check. |
| **The LEDGER tree** | Every statement, path-copied, written after its data and FREE-tree pages from the reservation (R2); the retirement writes the post-promotion ledger (the promoted ledger's fresh entries dropped, its released and freed pages gone to the FREE tree, the promoted ledger's own pages `freed` — the committed slot references them until the next promotion, R4; finding R3-2 — and the retirement's own new pages fresh). | R6 step 3 (every `fresh` page verified before a DESCENDANT is adopted); the retirement (what to move); the allocator (released pages); R4 (freed pages quarantined). Relies on: the ledger root in the meta the reader holds names a ledger whose own pages verify by stamp. |
| **The FREE tree** | The retirement (adds the retired pages and, at a fence after a rejection, the rejected ledger's never-committed pages); an allocation that takes a batch of its lowest pages (path-copied by the taking statement). | The allocator, after the in-memory ledger's released pages and before growth. Relies on: a page in the committed FREE tree is referenced by no slot's tree (the older committed slot is dead once a promotion's second sync completed, R5). |
| **Data pages** (internal, leaf, overflow) | The statement that changes them, always to a page neither slot's tree references (R1), stamped with the statement's generation, before page 2. | Every statement through stamped pointers (R3: checksum, `self`, generation); R6 through the ledger's `fresh` list. |
| **The TRANSACTIONS tree and the UNDO trees** (an UNDO tree: (kind, identity) → R: the before-image; V: the entry alone; round 5) | The first WRITE/REWRITE/DELETE of a run unit under commit control on the file (the entry), each later such statement (the before-image under R unless present, a V entry per unique value freed, in the same statement as the change), COMMIT (its promotion's retirement walks the UNDO tree, removes the entry and the holdings the walk listed, and releases the UNDO pages, after the slot write that decided it; a reversal does the same but re-applies the images), ROLLBACK and the dead-owner rollback (the same walk: the R images re-applied, every key removed from the HOLDINGS tree, the entry removed) — all ordinary statements under R1-R3. | ROLLBACK, COMMIT's retirement and the dead-owner step at every writable connector's coherent entry and at R6 step 5 (which transactions exist, whose owner is dead by the liveness byte; read only when page 2's transactions root is not empty; the walk is the ONE enumeration of a transaction's holdings, finding R5-2). Relies on: a before-image exists for every change that completed (same statement); every identity and unique value a rollback restores or frees is an entry here AND a holding in the file (the next row), so no writer passes it while the owner lives and none after its death without the rollback running first (findings R3-1, R4-1), and nothing is left in the HOLDINGS tree once the walk has run. |
| **The HOLDINGS tree** (finding R4-1): (kind R record identity / V unique key value, ordered by the file's key orders: `HoldingsKeyOrder`) → transaction id — the file's INDEX, by identity, of every open transaction's UNDO entries (round 5) | The statement that changes a record under commit control (its identity and every unique value it frees, in the same statement as the change and the UNDO entries with the same keys); COMMIT's retirement, ROLLBACK, a reversal and the dead-owner rollback remove the keys their UNDO walk listed, in their statement. Path-copied like every tree; its root in the meta page. Invariant: its key set = the union of the UNDO trees' key sets after every statement (the drift test). | The lock test of READ (not IGNORING LOCK), WRITE, REWRITE and DELETE of every connector, under the mutex, when page 2's holdings root is not empty: own transaction → proceed; another's → its owner's byte probed at the test ('51' held; free → rolled back there, then the test again). Never enumerated by transaction: it has no such index, and needs none. Relies on: a holding is written by the same statement as the change it protects and the UNDO entry that lists it, and is removed only by the walk that restores or commits it — so the set of identities a rollback touches IS the set the test refuses, whatever the operating environment did with the owner's handle, and a holding never outlives its transaction (finding R5-2's leak, and the endless rollback of an entry-less transaction it caused at the next lock test). |
| **The transaction-liveness byte** (2^62 + 2^61 + id mod 2^60) | Taken exclusively through the owner's writable handle when the transaction is DRAWN — before its id is claimed and before the entry is written, in the same statement; kept with the handle a CLOSE under commit control keeps, and moved by the connector's re-OPEN to its own handle inside the attach's mutex hold (shared on a handle that cannot hold it exclusively; 2.2.1.10 As built (3)); a held byte is skipped and the next id tried; given back at COMMIT after the entry's removal and its promotion, at ROLLBACK after its statement, by a first change refused before its page 2 (no entry carries the id), and by the operating environment when the owner dies. | The dead-owner step (every writable coherent entry, R6 step 5), the lock test that meets another transaction's holding (finding R4-1), the drawing of an id and `CloseAll` (held = alive; a run unit probes only other run units' transactions). Relies on: a transaction visible in the tree has its byte held by a live owner or by nobody, never by a stranger — the byte was taken before the id was claimed or written, and a held byte is never drawn (finding R4-2). |
| **The record-lock region** (2^62 + 2^60; a record identity) | READ under automatic locking (the commit posture's READ locks included); the explicit postures as today; released by UNLOCK/CLOSE for explicit postures and ONLY by COMMIT, ROLLBACK and termination for the commit posture; published through the holder's handle. A change under commit control publishes nothing here: its lock is its holding (the row above; finding R4-1). | READ, REWRITE, DELETE and (new) WRITE of every other connector, at DOC-A.1-104's point of their order, beside the HOLDINGS lookup; `IGNORING LOCK` bypasses both for READ. No ceiling for the commit posture's READ locks (DOC-A.1-154/155 rewritten by PB3337). Relies on: nothing a rollback restores is protected here, so a lock lost with a dead handle protects nothing that outlives it. |
| **The store mutex and the Table 19 column bytes** | Unchanged (PB2660, PB833): taken for every statement and the OPEN of a coherent connector, exclusive through a writable handle and shared through a reader's. | The same. |
| **The page cache, the generation last seen and the store's own last claim** (in memory, one `KeyedStore` per physical file per run unit) | The cache: filled by reads, DROPPED by 2.2.1.7 when page 2's `generation` changed; the generation and mints last seen: set at OPEN (page 2's, adopted or not) and at every adoption of page 2 and every own statement; the own last claim and the mints it advanced: recorded at the claim, whether or not the statement completes (findings R4-2, R4-3). | Every statement of every connector of the run unit on that file; every page 2 the store writes is written from this memory. Relies on: within the run unit there is one store, so a sibling's change is visible instantly, and a refused statement's claim is remembered by the store that made it until the store is detached — after which page 2, written from the memory by the CLOSE's promotion, carries it. |

**2.2.1.16 Every crash point (the round-3 self-attack, 2026-10-17).** For each path that writes, a process death BEFORE and
AFTER each write and each sync: the state the medium holds (a process death keeps every write handed to the operating
environment, in order; a POWER LOSS keeps an unordered subset of the unsynced ones and is the second column's variant),
the rule that recovers it, and why the bar holds — the file reads as the state after the last COMPLETED statement, never
torn, and an active APPLY COMMIT transaction whose owner died is rolled back (§9.1.18.5). "Next writer" is the first
coherent statement through a writable handle in any live run unit (2.2.1.7); "OPEN" is R6. Every row's outcome is a cell
of 2.2.1.12's matrix or of its R-tests; a row with none would be a design defect.

| Path | Death before / after | State on the medium (process death; power-loss variant) | Recovered by | Why the bar holds |
|---|---|---|---|---|
| A changing statement (coherent) | before its claim | nothing written; page 2 = the previous statement | nothing to do | the previous statement is the last completed one |
| | after the claim, before any page | `stamp mint` above `generation`; no page carries the claim | next writer: claims above the mint (no drop); OPEN: a DESCENDANT, adopt, fence | no page changed; the claim named a generation nobody else will stamp |
| | after some data, FREE-tree or ledger pages, before page 2 | pages stamped with the claim on pages no slot references; page 2 = the previous statement | next writer: claims above (R3's corollary; finding R3-4); OPEN: a DESCENDANT (the previous statement's ledger lists none of the dead pages), adopt, fence; the dead pages stay allocatable, since the reservation never took effect (`released`/FREE-tree entries unchanged); grown pages beyond `page count` are reused by the next growth. Power loss: the same, or page 2 torn → FOREIGN → C (the statements since the last sync, the accepted loss) | the dead statement's change is invisible (R1: its pages are reached by no pointer page 2 names) and its stamp is never reissued |
| | after page 2 | complete | none | it IS the last completed statement |
| A changing statement (NO OTHER; the claim in the store's memory) | as above, the claim being a memory write | as above | this run unit's own next statement, CLOSE included, claims above the memory (finding R4-3); OPEN: the fence lies above every stamp (R2's ceiling) | as above |
| A promotion (CLOSE, checkpoint, COMMIT, `CloseAll`) | before the claim | nothing | nothing | — |
| | after the claim (PROMOTING set), before or after sync 1, before the slot write | page 2 = the meta being promoted, flagged; slots unchanged | next writer: R5 restarted from page 2 (both slots read, the lower written, both syncs, the retirement); OPEN: a DESCENDANT (base = C's generation), adopt, fence | the state being promoted is the last completed statement's; the flag is the only change |
| | after the slot write, before sync 2 | the slot names page 2's generation (in the operating environment's cache); power loss: the slot torn or absent → the other slot is C | next writer: R5 restarted — the slot is left as it is, the syncs run, the retirement follows; OPEN: the PROMOTION SOURCE (slot valid) or a DESCENDANT (slot absent or torn), adopt, fence | the committed slot's pages were synced before it (sync 1); a torn slot leaves the other whole (R1-4) |
| | after sync 2, before any retirement page | as above, durable | as above | as above |
| | during the retirement, before its page 2 | retirement pages on no-slot pages, stamped with its claim | next writer: R5 restarted — the retirement claims above the dead claim and stamps its pages afresh; OPEN: the PROMOTION SOURCE, C adopted, its fence re-runs the retirement from C's ledger (whose own pages R4 kept, finding R3-2) | the promoted state is durable and whole; the retirement's pages were reached by nothing |
| | after the retirement's page 2 (flag clear, base = the promotion) | complete; CLOSE's third sync may be unlanded (power loss: the retirement's few pages → the PROMOTION SOURCE, as above) | none / OPEN as above | — |
| A CLOSE that owes no promotion (no statement completed since the store's last promotion, its OPEN's fence included; 2.2.1.7): ONE sync, no write, no read | before the sync / after it | nothing written; page 2 = the last promotion's retirement, untouched | nothing to do; OPEN: a DESCENDANT or C as the state was | nothing changed, so the last completed statement is what it was. A sync the host REFUSES is the CLOSE's '30' (§9.1.13.6 item 1), never '00': the program must not believe a file durable that the host refused to flush (`PagedFileCrashMatrixTests`' `CloseUnchanged` path and `KeyedStoreReloadFailureDriftTests`' unchanged-CLOSE cases; kb/Work PB3382) |
| The fence (R6 step 4; a promotion of the ADOPTED meta at F) | the same six points | the claim rewrote page 2 to the adopted meta at F (C's content after a rejection), so page 2 no longer holds a rejected slot from the moment the fence began | the next OPEN: before the slot landed, a DESCENDANT of C with generation F (its `fresh` pages are C's own, present) and a fence at the same F (harmless when repeated; the retirement claims above the dead one's mint); after it, the PROMOTION SOURCE. A live INPUT reader adopts page 2 when its generation becomes F — the same tree it held | the fence changes no record; the superseded history's stamps lie below F (R2) and are never reissued |
| COMMIT (2.2.1.10; round 4): the promotion with `committing` = t — the claim → sync 1 → the slot write → sync 2 → the retirement that removes t's entry and holdings → the byte and READ locks given back | before the claim | the entry, the UNDO tree and the holdings stand; the owner is dead (the byte free) | the dead-owner step (next writer or OPEN) rolls it back | the COMMIT never began; §9.1.18.5's rollback applies |
| | after the claim, before the slot write (before or after sync 1) | page 2 flagged with `committing` = t; the slots unchanged; the entry stands | next writer: the restart rule finds no slot naming page 2's generation and `committing` = t → UNWINDS the claim, then the dead-owner step rolls t back; OPEN: W is a DESCENDANT with the flag → the fence writes `committing` 0, step 5 rolls t back. Power loss: the same, or page 2 torn → FOREIGN → C (which carries the entry → rolled back) | the one durable write never happened, so the COMMIT is "not at all": the run unit never saw a result, and §9.1.18.5's "rolled back" is the outcome |
| | after the slot write, before sync 2 | the slot names page 2's generation with `committing` = t (in the operating environment's cache); the entry stands in both trees | next writer: the restart finds the slot landed → completes (syncs, the retirement removes t); OPEN: the PROMOTION SOURCE → C (`committing` = t) → the fence's retirement removes t. Power loss: the slot survived → as OPEN above; the slot torn or absent → the other slot is C, W (the claim) a DESCENDANT with the flag → unwound and rolled back | whole or not at all, decided by the slot alone (the same durable write that decides every promotion, R1-4); either outcome is a consistent tree and the standard fixes neither for a run unit that died inside its COMMIT |
| | after sync 2, before the retirement's page 2 | durable with `committing` = t; the entry stands; the owner may have been told success or died first | next writer: completes the retirement (removes t); OPEN: PROMOTION SOURCE → fence → retirement removes t | committed: the durable write landed and nothing rolls back an entry the committed slot names (R5's restart rule, the `committing` row of 2.2.1.15) |
| | after the retirement's page 2, before the byte is given back | complete; the byte is freed by the handle's close | nothing (no entry to roll back; the step probes only entries) | — |
| COMMIT, sync 2 REFUSED and the owner alive (the reversal, 2.2.1.10 region 3; round 5): the reversal statement → its promotion (claim, sync, slot, sync, retirement) → the answer | during the reversal statement, before its page 2 | the slot landed in the operating environment, flagged with `committing` = t; page 2 = the claim; the entry stands; reversal pages on no-slot pages | next writer: the restart finds the slot landed → completes the retirement (removes t); OPEN: the PROMOTION SOURCE → C → the fence finishes it | committed; the owner died inside its COMMIT, untold, so either outcome is a consistent tree the standard does not choose between (the row "after the slot write" above) |
| | after the reversal statement's page 2 (base = the COMMIT's generation, flag clear, t rolled back), before its promotion's slot lands | C = the COMMIT's slot (t named); page 2 = a DESCENDANT of it holding the pre-COMMIT records, no entry | next writer: adopts page 2 (generation moved) — the reversed tree — and, when the reversal's promotion claim had set the flag, runs that promotion again (a checkpoint); OPEN: W a DESCENDANT of C, verified → adopted → fence → durable rollback; power loss tearing a reversal page → C → the fence finishes the COMMIT | rolled back wherever the reversal's pages survived; committed where a power loss lost them — again untold; and once the reversal's slot syncs (the next row) only the rollback survives |
| | after the reversal's promotion landed and synced | both slots durable, the reversal's the higher; or the COMMIT's lost: the same meta | OPEN: C = the reversal; the retirement as any | rolled back: what the owner was told ('30'), for every later crash |
| ROLLBACK (one statement), and the dead-owner step (the same statement run by another run unit) | before its page 2 | the entry and the UNDO tree stand; some restored frames on no-slot pages | the dead-owner step redoes it (idempotent); a live run unit refused mid-way ('30') is in §9.1.13.1's permanent error with its transaction intact | nothing of the half rollback is reachable (R1) |
| | after its page 2 | the records are as at the last COMMIT, the entry and its holdings gone; the READ locks go with the handle | none | the state is a completed statement's |
| ROLLBACK on a file marked UNACKNOWLEDGED (round 5): the reversal if page 2 still carries t, then a promotion | at any point | as the reversal rows above | as the reversal rows above | the answer was not yet given; whichever state the medium keeps is one the owner was never told the opposite of |
| A transaction's first change (the byte taken, then one statement: the entry, the UNDO tree, the record) | before page 2 | no entry; the byte went with the handle | nothing | the change is invisible; an entry is never visible without its byte having been taken first |
| `OPEN OUTPUT` (creation, 2.2.1.4: the empty meta at N into the slot not holding the previous committed meta → sync → page 2 → the other slot → `SetLength` to three pages → sync) | before the commit slot's write was handed over (the reads included) | the previous file, untouched | the next OPEN finds the previous store (or, over a file with no store, answers '30'); OPEN OUTPUT recreates | the creating OPEN never began to change the file |
| | after the commit slot's write, at any later point | the commit slot holds the empty store at N, the higher generation; page 2, the other slot and the length as far as the creation got (an old page 2 is FOREIGN: its base is not N) | the next OPEN adopts the EMPTY store and fences it; power loss before the commit's sync: the slot lost → the previous file; after it: the empty store | §14.9.27.4 GR18's file "contains no records"; the previous store's pages past three are reached by nothing and carry stamps below N |
| Growth (a write at `page count`) | between the write and page 2 | the file is longer than `page count` | the next growth writes the same pages | pages past `page count` are reached by nothing |
| Every sync point, power loss between two | — | the unsynced statements, or some of them | R3 refuses a lost or torn page at its read; R6 falls back to C when W fails | 2.2.1.2's contract: at most the statements since the last sync are lost, and never a torn state |

**Every statement REFUSED part-way, and every continuer (the round-4 self-attack, 2026-10-17).** A host refusal — a full
medium, a quota, a revoked handle — at the n-th write of each path that writes, the run unit ALIVE afterwards (in §9.1.13.1's
permanent error for a record statement, DOC-A.1-105: every later statement on that connector replays the status and touches
nothing, until CLOSE); then the SAME connector's next statement, its CLOSE and the next OPEN, and ANOTHER connector's next
statement. A refusal differs from a death in exactly two things — the run unit keeps its memory and its handle, and it is told
— and every row is decided by the claim in memory (R4-3), the drawn byte (R4-2), the holdings in the file (R4-1) or the
slot (`committing`).

| Path refused at | The file after the refusal | The same connector's next statement · its CLOSE · the next OPEN | Another connector's next statement |
|---|---|---|---|
| A changing statement (coherent), at its CLAIM write | nothing changed; page 2 = the previous statement; the memory's claim raised, wasted | '30' replayed, nothing touched · CLOSE: when a statement completed since the store's last promotion, promotes, its claim above the memory, and retires; when none did, only syncs (2.2.1.7), since the wasted claim reached no page; the condition ends either way · OPEN: the fence | its entry finds page 2 unchanged and claims from page 2; the wasted claim never reaches a page |
| A changing statement (coherent), at a data, FREE-tree or ledger page | page 2 holds the claim (mints raised, roots unchanged); pages under the claim on no-slot pages; a drawn byte given back | '30' · CLOSE: when a statement completed since the store's last promotion, the promotion claims above (memory and page 2 agree); when none did, only syncs (2.2.1.7), and page 2's raised mints serve the next claim; the refused pages stay allocatable either way (the reservation never took effect) · OPEN: the fence | claims above `stamp mint` (finding R3-4); a first change draws above the claimed `transaction mint` (finding R4-2) |
| A changing statement (NO OTHER), at any page | the memory's claim raised; page 2 unchanged | '30' · CLOSE: when a statement completed since the store's last promotion, the promotion claims above the memory and writes page 2 from it, so the retirement never re-stamps the refused pages (finding R4-3); when none did, only syncs (2.2.1.7): page 2 is that promotion's retirement, no retirement runs, and the next OPEN's fence lies above the refused pages' stamps (R2's ceiling), as after a death · OPEN: the fence | none exists |
| A transaction's first change, at any page | as the row above; the byte given back; no entry, no holdings | '30' · the fatal condition's unconditional ROLLBACK (§14.6.13.1.3 item 8) finds no transaction · CLOSE / OPEN as above | draws the next id from the claimed mint; a byte still held is skipped (finding R4-2) |
| ROLLBACK, at any page | the entry, the UNDO tree and the holdings stand; restored frames on no-slot pages | '30' · the unconditional ROLLBACK is refused again while the medium refuses, and the run unit terminates (item 8): the dead-owner step restores when the medium allows · CLOSE: promotes the state with the entry, keeps the handle · OPEN: R6 step 5 | its lock tests meet the holdings: '51' while the owner lives; rolled back at the test or the entry once it is dead |
| COMMIT, at its claim, sync 1 or its slot write | the claim with `committing` = t may stand (flag set), the slot does not; the transaction intact; the COMMIT unsuccessful | the unconditional ROLLBACK: its entry unwinds the claim (no slot, `committing` = its own) and restores from the intact UNDO tree · CLOSE: its promotion's entry unwinds first · OPEN: a DESCENDANT with the flag → the fence writes `committing` 0; step 5 rolls t back if its owner is dead | its entry unwinds the claim (R5's restart rule) and proceeds; t stands, held by its live owner |
| COMMIT, at sync 2 (UNACKNOWLEDGED; round 5, finding R5-1) | the slot in the operating environment (every live run unit reads it as C), the medium's state unknown; the COMMIT, still holding the mutex, REVERSES: the retirement-as-rollback statement, then a promotion of the reversed state | reversal landed and synced: the COMMIT answers '30', the unconditional ROLLBACK finds no transaction on the file, and every future reads the rollback · CLOSE and OPEN: ordinary. Reversal refused (a page write, or its promotion's sync): the file is MARKED; the COMMIT answers '30'; the unconditional ROLLBACK reverses again if the entry still stands under the flag, promotes page 2 as it stands and answers by the sync — '30' while it is refused, '00' once one lands, and '30' if the entry is gone without this run unit's doing; the termination path's rollback the same · OPEN: R6 finishes a landed COMMIT slot and rolls back anything else | sees nothing while the reversal runs (the mutex); after a refused reversal that left the flag beside the landed slot, its entry completes the COMMIT in the operating environment (it cannot tell), after which the owner's ROLLBACK answers '30' — unknown to the owner, settled by whoever next syncs |
| COMMIT, in the reversal's promotion, before its slot write (the reversal statement landed) | page 2 = the reversed state as a DESCENDANT of the COMMIT's slot, flagged for the reversal's promotion (`committing` 0) | the file is MARKED, as above; the ROLLBACK's promotion runs the flagged promotion again | its entry runs the flagged promotion again (a checkpoint, harmless), which — if its sync lands — settles the medium on the rollback before the owner's own ROLLBACK does |
| COMMIT, in its retirement | the slot names page 2's generation with `committing` = t: committed; the flag stays; the COMMIT reports success | its entry completes the retirement (removes t) · CLOSE: the same · OPEN: the PROMOTION SOURCE → C → the fence's retirement removes t | its entry completes the retirement |
| A promotion (CLOSE, checkpoint, `CloseAll`), before the slot write | the flag set, `committing` 0; the slots unchanged | CLOSE: answers '30' and the connector is closed (a CLOSE ends the condition itself) · a checkpoint: the statement that needed it answers '30' before its own claim; its next entry runs the promotion again · OPEN: a DESCENDANT with the flag → the fence | its entry runs the promotion again (harmless when repeated) |
| A promotion, after the slot write (sync 2 or the retirement) | landed | its entry completes the retirement · OPEN: the PROMOTION SOURCE → the fence completes it | its entry completes the retirement |
| The fence (R6 step 4), at any point | the OPEN is unsuccessful ('30'); the connector is left closed (DOC-A.1-105) | the next OPEN: before the slot landed, a DESCENDANT of C at F with the flag (the fence runs again); after it, the PROMOTION SOURCE (the fence completes it) | the same at its OPEN; a live INPUT reader follows page 2's `generation` only |
| The dead-owner rollback run at A's entry, at any page | B's entry stands; A's statement answers '30' | A: '30' replayed · CLOSE · OPEN as a record statement | the next writer's entry redoes it (idempotent) |
| `OPEN OUTPUT`, at any read, write, sync or the truncation but the last sync | the previous file, byte for byte: the pages written get their previous bytes back (a page the file held only part of too) and a file shorter than three pages its length to the byte, then a sync (§14.9.27.4 GR25, *"the file is not affected"*) | the OPEN answers '30' and the connector stays closed · the next OPEN finds the previous store (or '30' over a file with none) · OPEN OUTPUT recreates | the same |
| `OPEN OUTPUT`, at its last sync | the empty store, committed and truncated; the medium's state unknown | '30' · the next OPEN adopts the empty store | the same |
| `OPEN OUTPUT`, at any refusal above whose UNDO is refused too (a restore write, the length; review finding R1-1 of kb/Work PB3493) | not the previous file: every page whose restore landed is back (the undo goes on past a refused one), the rest are the creation's; a commit slot left holding the empty meta at N makes the file the empty store, and one restored with page 2 refused leaves the previous committed store beside a foreign work slot | '30', the file REPORTED REPLACED (`PagedFile.Create`'s `replaced`): what a CLOSE under commit control kept over the previous store ends with it (2.2.1.10) · the next OPEN adopts by R6 | the same |
| Any path, a refused READ (OPEN's meta pages, R6 step 3's verification, a statement's entry, the promotion's read of both slots, the retirement's chains, a tree page) | unchanged by the read; whatever the path wrote before it stands as at a refused write at that point | '30' (§9.1.13.6 item 1): a refused read is no verdict on the page — it never rejects a work slot, never chooses a slot for a promotion, never reads as zeros | the same |
| A changing statement, at its reservation or a write the host refuses because the file cannot grow further (2.2.1.9) | the reservation: nothing claimed, nothing written; a refused write: as a refused write at that point | the BOUNDARY, the file unchanged: a WRITE or REWRITE answers '24' with the invalid key condition (§9.1.14); a DELETE answers '30' with no invalid key condition (§9.1.13.6 item 1: §9.1.13.5 item 4's '24' is a write's, and §14.9.10.4 GR3/GR4 give a DELETE the invalid key condition only for a record the file does not contain); the same connector's next statement that fits succeeds | the same; a promotion, a CLOSE and an OPEN meeting either boundary answer '30' (they have no '24') |
| The reader's re-adoption when the run unit's last writable connector CLOSEs and an INPUT sibling stays (2.2.1.7), a refused read | unchanged by the read; the CLOSE's promotion, when one ran, landed before it | that CLOSE answers '30' (§9.1.13.6 item 1) and the connector is closed · the store keeps the writable meta it holds, its fence OWED: the run unit's next writable OPEN opens it again and fences (R6) | the INPUT sibling's next statement enters as a reader over the trees the store holds: '00' |
| Any path, a sync failure | the medium's state unknown | '30', permanent error; R3 and R6 decide the state at the next read or OPEN, never a torn one | the same |

**2.2.1.17 Every two-run-unit interleaving over one file (the round-3 self-attack, 2026-10-17).** A and B are run units
with the file open; the column "ordered by" names the mutex or lock that serializes the pair and "outcome" the state each
sees. A NO OTHER connector has no row: Table 19's column bytes refuse every other opener (kb/Work PB833), so no second run
unit exists. Two connectors of ONE run unit share one `KeyedStore` and are serialized by `FileRegistry`'s statement wrapping
(2.2.1.7, "Within a run unit") and have no row either.

| Pair | Ordered by | Outcome |
|---|---|---|
| A's statement ∥ B's statement (any two record statements) | the store mutex: exclusive through a writable handle, shared through a reader's — so two readers overlap and a writer waits for every reader | the later writer's entry finds page 2's `generation` moved → adopts page 2 (roots and ledger), drops its cache, claims above the mint; a reader entering after a writer sees the writer's complete tree (page 2 is written last, under the mutex); two readers see the same tree |
| A's statement dies mid-way ∥ B's next statement | the mutex (freed by A's death) + the claim (finding R3-4) | B finds `stamp mint` above the generation it last saw, claims above it, reuses A's reserved pages under a higher stamp; A's change is invisible |
| A's statement is refused mid-way ('30') ∥ B's next statement | the same | the same; A is in §9.1.13.1's permanent error; was A's statement a first change under commit control, A gave its byte back and B's first change draws from the claimed mint (finding R4-2) |
| A's statement ∥ B's writable OPEN (R6 + fence) | the exclusive mutex | B's R6 sees a complete DESCENDANT (A's last completed statement), adopts and fences it; A's next statement adopts page 2 (base F), replaces its ledger with the retirement's (its `released` pages moved to the FREE tree) and drops its cache |
| A's statement ∥ B's INPUT OPEN | shared vs exclusive | B reads a complete W; B follows page 2 from then on (generation changes only) |
| A's statement ∥ B's COMMIT (a promotion) or checkpoint | the mutex | A's next statement adopts the retirement's page 2: new base, new ledger, cache dropped; A's own uncommitted changes became durable with A's UNDO tree (harmless, 2.2.1.10) |
| A's promotion ∥ B's promotion | the mutex + R5's read of both slots before the write | each writes the slot holding the lower valid generation; the second never overwrites the first's committed meta; the second's retirement runs on the second's base |
| A's promotion dies between its claim and its retirement's page 2 ∥ B's next statement | the mutex (freed by A's death) + PROMOTING | B completes the promotion (R5 restarted from page 2) before its own work; without the flag B would have continued on the old base and released pages the new committed slot referenced (found by this self-attack; fixed with R3-4) |
| A's statement ∥ B's ROLLBACK (B alive) | the mutex + B's holdings in the file (record identities, deleted identities, freed unique values; finding R4-1) | A's statements on B's records answered '51' (RETRY applies); B's ROLLBACK, one atomic statement, restores only B's records; A's completed statements stand |
| A's COMMIT ∥ B's ROLLBACK | the mutex | either order ends with A's changes durable and B's restored; A's promotion carried B's UNDO tree with B's changes |
| A's COMMIT, sync 2 refused (the reversal) ∥ B's statement (round 5) | the mutex, held by A from its claim through the reversal statement and its promotion | B waits; B's entry then finds page 2's generation moved and adopts the reversed state (A's records as at A's previous COMMIT, no entry of A's), and PROMOTING only if the reversal's promotion was refused part-way (`committing` 0: B runs it again, a checkpoint). B never sees A's landed slot under a flag, so B never completes a COMMIT A was told failed |
| A's reversal refused at a page write (the flag left beside the landed slot) ∥ B's statement, before A's ROLLBACK | the mutex (released by A's refusal) + the slots | B's entry completes A's COMMIT in the operating environment (restart rule: landed → complete); A's ROLLBACK, finding the entry gone, answers '30' — A is told "unacknowledged", which is the truth: the medium keeps the COMMIT if B's or anyone's later sync lands, and the pre-COMMIT state with A's entry (rolled back by the fence) if none does |
| A's reversal landed, its promotion's sync refused ∥ B's promotion | the mutex | B's promotion (its own COMMIT, CLOSE or checkpoint) promotes a descendant of the reversed state; when B's sync lands the medium is settled on the rollback, and A's next ROLLBACK's promotion then answers '00' if its own sync lands — never '00' while the medium's highest valid slot is A's COMMIT's |
| B dies with a transaction ∥ A's next writable statement or OPEN (the death before A's entry) | the mutex + the liveness byte (finding R3-1) | A's entry finds B's byte free, rolls B back before A's own statement; A's records untouched; B's records as at B's last COMMIT; a second writer entering later finds no entry |
| B dies with a transaction ∥ A's statement already past its entry (the death at any later point of A's statement; finding R4-1) | the mutex + B's holdings in the file + the liveness byte probed at A's lock test | A's test meets B's holding and probes B THEN: free → B rolled back inside A's statement before A's change, the test repeated ('22' where the rollback re-created the identity); held → '51', and the next entry rolls B back. An identity with no holding is nobody's and no rollback touches it, so a death after the test cannot reach A's change. The point-by-point rows are in 2.2.1.17's round-4 table |
| B dies with a transaction ∥ C's INPUT statement between the death and A's entry | the shared mutex | C reads the file as it stands (B's uncommitted state; D.2.5's tolerated read); after A's entry C adopts page 2 and reads the restored state |
| B's transaction begins ∥ A's dead-owner step | the mutex; the byte taken before the entry is written, in B's statement | A probes only entries in the tree; an entry B wrote has B's byte held (B alive) → left as it stands |
| B's COMMIT gives its byte back ∥ A's dead-owner step | B removes the entry (statement) and promotes before releasing the byte | A never sees an entry with a free byte for a live B |
| A's writable OPEN ∥ B's writable OPEN (recovery ∥ recovery) | the exclusive mutex | the second R6 reads the slots and page 2 fresh under the mutex: C = the first's fence F, W = its retirement (a DESCENDANT); it adopts and fences again at F + 4 097 (harmless) |
| A's writable OPEN ∥ B's INPUT OPEN of a crashed file | shared vs exclusive | B before A: B adopts C in memory and follows page 2 from A's fence claim (generation F = the same tree), never the rejected tree (R2-5); A before B: B reads A's fenced state |
| A's lock test (READ, WRITE, REWRITE, DELETE) ∥ B's lock publication | the record-lock region, publish-then-test (PB2660, unchanged) | '51' with RETRY outside the statement, as today |
| Both run units' statements ∥ page 2's fields | the mutex | a `generation` change is a complete tree (adopt); a `stamp mint` change alone is a dead or refused attempt (claim above, keep the cache); PROMOTING is a dead or refused promotion (resolve it by the slots and `committing`: complete, run again or unwind); an INPUT connector acts on `generation` only |

**A lock holder's death at every point of ANOTHER run unit's statement (the round-4 self-attack, 2026-10-17).** B owns a
transaction with holdings on K5 (deleted) and the unique value X (freed), is idle and holds no mutex, so its kill can land at
any instant of A's statement. A is any writer (under commit control or not) running WRITE K5, WRITE K7 with X, or WRITE K9
(an identity B does not hold). The one rule: a holding is a page of the file and a rollback touches holdings only (finding
R4-1), so A's change can be reached by B's rollback only through a holding, and every holding A meets is decided at A's own
test by B's byte at that instant.

| B dies ... | Ordered by | Outcome |
|---|---|---|
| before A's entry | the mutex + B's byte at the entry | A's entry rolls B back; K5 → '22', K7/X → '22', K9 → '00' |
| after A's entry probe found B alive, before A's lock tests | B's holdings in the file + B's byte at the test | A's holdings lookup meets B's holding and probes B then: free → B rolled back inside A's statement, A's conditions evaluated again from the invalid-key conditions on: K5 → '22', K7/X → '22'; K9 meets no holding → '00' |
| between A's region test and its holdings lookup, or between two of A's lookups (a WRITE with several unique values) | the same | the first lookup that finds B dead rolls B back and A's conditions are evaluated again from the start; the region's release order plays no part, since no holding is in the region |
| after A's test found B alive ('51' answered for K5 or X) | the next entry's probe | A's statement changed nothing; the next entry (A's RETRY re-entry, or any writer) rolls B back; A's retry → '22' |
| after A's test met no holding of B's (K9) — before A's claim, during A's writes, after A's page 2 | nothing needed | K9 is nobody's: B's rollback, whenever it runs, restores K5 and re-inserts X and touches K9 never; A's '00' stands |
| while A draws a transaction id (A's own first change) | the claimed `transaction mint` | A draws from page 2's mint upward, above every id ever written; B's freed byte is never re-drawn (finding R4-2) |
| B held a READ lock only (the region, §12.4.5.9.4 GR4) | the region | the lock goes with the handle; nothing restores a READ, so A's REWRITE of that record proceeds |
| B's COMMIT was refused before its slot write (the flag set, `committing` = B's id) ∥ A's entry | the mutex + the slots | A's entry unwinds the claim; B alive: A's tests meet B's holdings → '51'; B dead: A's dead-owner step rolls B back |
| B's COMMIT landed and its retirement was refused ∥ A's entry | the mutex + the slots | A's entry completes the retirement: B's entry and holdings gone, B's changes committed; A's tests meet nothing of B's |
| B's first change was refused ∥ A's first change | the claimed mint + the byte | A draws the next id with no wait and no refusal (finding R4-2) |
| A rolls B back at its test ∥ C's INPUT statement | the mutex (C shared, A exclusive) | C waits; after A's page 2, C adopts page 2 and reads the restored state |

**2.2.1.18 Who finds what, and what an answer promises (the round-5 self-attack, 2026-10-17).** The structural test behind
findings R5-1 and R5-2: for every identifier, lock and before-image the format carries, who finds it at COMMIT, at ROLLBACK,
at the dead-owner rollback and at OPEN, through which index, at what cost — a thing nobody finds is a leak, and a thing found
through an index that cannot list it is a defect; and, for every answer a program can be given, what the file holds after
any later crash — an answer the medium can later contradict is a defect.

| Thing | Where it lives | Found at COMMIT (the retirement) by | Found at ROLLBACK and the dead-owner rollback by | Found at OPEN (R6) by | Cost |
|---|---|---|---|---|---|
| a generation / `stamp mint` | page 2; every page header and pointer | copied into the slot; the retirement claims above it | the statement claims above it | the slots' and page 2's `generation`; the fence claims G + 4 097 above all | 0 (fields of pages already read) |
| a transaction id | page 2's `transaction mint`; the TRANSACTIONS tree (id → UNDO root); `committing` | `committing` in the claim and the slot; the entry by key | the entry by key (the owner knows its id; the dead-owner step walks the tree) | the TRANSACTIONS tree walked whole at step 5; `committing` in C and W at step 4 | ≤ 2 pages (the tree); 0 for the fields |
| its liveness byte | the region (2^62 + 2^61 + id) | given back after the retirement | given back after the statement; the dead-owner step probes it per entry | probed per entry at step 5 | one host lock test per entry |
| a release ordinal | page 2's `release mint`; a frame's ordinal vector; an alternate tree's composite key | nothing (committed with the pages) | the before-image's own vector, re-applied; the mint never decreases | nothing | 0 |
| PROMOTING, `committing` | page 2 flags and field; the slots | cleared by the retirement (or the reversal) | a flag met at the entry is resolved first (R5's restart rule) | step 4: unwind an unlanded COMMIT's claim, finish a landed one | 0 |
| a record holding R | the HOLDINGS tree (R, identity) → id, AND the owner's UNDO tree (R, identity) → image | the retirement's walk of the UNDO tree, every R key removed from the HOLDINGS tree | the same walk: the image re-applied, the key removed | step 5's rollback of a dead owner, the same walk | u + the UNDO leaves to read; one HOLDINGS path copy per leaf touched |
| a value holding V | the HOLDINGS tree (V, key index + bytes) → id, AND the owner's UNDO tree (V, ...) → nothing | the same walk, every V key removed | the same walk, removed (the value is free after the images are re-applied, or held again by the restored record) | the same | the same walk — the finding R5-2 leak had V keys in the index only |
| a before-image | the owner's UNDO tree (R, identity) | released with the UNDO pages (`freed`) | re-applied, then the pages `freed` | step 5, the same | (k + 1) × d per image re-applied |
| a READ's lock | the region (a record identity, through the holder's handle) | released after the retirement | released after the statement; a dead holder's went with its handle (nothing to restore) | nothing | one host lock release |
| the §9.1.15 file lock of a CLOSEd connector | the kept handle (`KeptFile`) | released (GR4) | released (§9.1.15's last sentence) | nothing | 0 |
| the UNACKNOWLEDGED mark | the run unit's memory | set when a COMMIT's sync 2 and its reversal are both refused | consulted: promote and answer by the sync | nothing (a dead run unit's mark died with it; R6 settles by the slots) | a promotion |

| The program is told | What the file holds in the operating environment | After a later process death | After a later power loss | What R6 finds |
|---|---|---|---|---|
| a record statement: '00' | the statement's pages and page 2, handed over | the same | the statement, or any earlier completed one, never a torn state (the stated contract: at most the statements since the last sync) | a DESCENDANT adopted, else C |
| a record statement: '30' (a refused write, or a refused sync) | the previous statement; the claim | the same | the same, or earlier | the same |
| COMMIT: '00' | the slot, synced; the retirement handed over | committed; the retirement completed by the next entry | committed (the slot is durable); the retirement redone by the fence | the slot is C (or the PROMOTION SOURCE), finished |
| COMMIT: '30' refused before the slot write | the claim at most; the entry intact | the claim unwound and t rolled back by the next entry | the same, by the fence | a DESCENDANT with the flag → unwound, t rolled back |
| COMMIT: '30' refused at sync 2, the reversal acknowledged | the reversal's slot, synced | rolled back | rolled back | C = the reversal |
| COMMIT: '30' refused at sync 2, the reversal refused — "unacknowledged" (the mark; the ROLLBACK '30' while no sync lands) | the COMMIT's slot as C, or page 2 holding the reversed state above it | whichever a later sync, anyone's, settles | the highest valid durable slot: the COMMIT's (→ committed) or the pre-COMMIT one with t's entry (→ rolled back) or the reversal's (→ rolled back) | the same three, each a consistent tree; the program was told neither outcome |
| ROLLBACK: '00' (an ordinary file) | the rollback statement, handed over; the entry gone from page 2 | rolled back | rolled back (the committed state carries the entry, idempotent) | the statement, or C with the entry → rolled back by the fence |
| ROLLBACK: '00' (a marked file) | the reversal's slot, synced | rolled back | rolled back | C = a descendant of the reversal |
| ROLLBACK: '30' | the entry intact (an ordinary refusal), or the marked file's unknown state | rolled back by the next entry; or unknown, as above | the same | the same |
| CLOSE: '00' | the promotion, synced | the same | the same | the retirement or the PROMOTION SOURCE |
| CLOSE: '30' | the promotion's claim at most, or a slot unsynced | the next entry's restart | the last synced promotion, or the statements since it | a DESCENDANT or C |
| nothing (the owner died) | whatever was handed over, in order | t rolled back by the next writable entry unless a COMMIT slot names it | the same by the fence | step 5 |

The one determination the tables surface that the standard does not settle (a COMMIT over several files, one refused) is
written at the COMMIT bullet and asked in PB3330.

#### 2.2.2 The sequential organization under APPLY COMMIT — the transaction journal (kb/Work PB3340)

⛔ **STATUS.** IMPLEMENTED by kb/Work PB3340 (wave 1157, 2026-10-23): the journal is `IO/Sharing/SequentialJournal.cs`, the
run unit's transactions `IO/Sharing/SequentialTransactions.cs` (owned by `FileRegistry`), the covered connector
`IO/SequentialConnector.CommitControl.cs`, the entry step `FileConnector.EnterPhysicalFile`, the seam `IO/IHostMedium.cs`, and
its rows run over real bytes in `SequentialJournalTests` and `SequentialCommitControlTests` through the fault-injecting medium
(`FaultHostMedium`, 2.2.2.12). The text below is the DESIGN of 2026-10-19 (the Mythos lane, kb/Work PB3426; the tier-2 memo
PB3420 orders), followed by the implementation (CLAUDE.md rule 2), and CORRECTED the same day after the branch review
found two compositions the first text got wrong (findings R1-1 and R1-2 in `kb/Work/PB3426.md`: a REWRITE journaled after a
capture, and a created file left in place by a rollback). The correction is not another prose round: the protocol below is
EXECUTED by `scripts/models/sequential_journal_model.py` (2.2.2.12), a self-test every gate and CI run, over every program of
up to four statements, every fault at every operation, every power-loss outcome and a second run unit at every operation
boundary — and it is RED when either finding is put back. The section gives a SEQUENTIAL file under an active APPLY COMMIT
clause the same transaction as 2.2.1.10 gives a keyed file — the same scope, the same three COMMIT regions, the same dead-owner
rollback — over a format that has no ledger. PB3340 follows PB3337 (the runtime's COMMIT and ROLLBACK) and PB3338 (the
compiler's APPLY COMMIT) in the tier-2 chain the owner released on 2026-10-19 18:58 PDT (the whole module, kb/Work PB3337:
"Option A"). **The
journal's home is the owner's decision of 2026-10-19 15:45 PDT (kb/Work PB3340, "option a approved"):** it lives in TRANSIENT files
beside the physical file (2.2.2.4); kb/Work PB802's "no sidecar of any type" (2026-09-07) refused a persistent
fixed-file-attribute catalog consulted at every OPEN and does not cover a journal that exists only while a transaction is
open, as the lock files of `SidecarLockFiles` exist only while a holder lives. The shapes the owner rejected are kept in
2.2.2.11 with what each loses.

**2.2.2.1 What the standard requires, and what it leaves to the implementor.** §9.1.18.2: *"Apart from sort-merge files and
report writer files, any type of file on a mass storage device may be specified"* — and §12.4.5.9.3 SR1 names the organization
outright: the implicit lock posture of a file under APPLY COMMIT applies *"including for sequential files"*. So a SEQUENTIAL
file (record sequential, fixed or RECORD VARYING; line sequential; a print file written with ADVANCING or under a LINAGE
clause) named in an APPLY COMMIT clause is subject to everything 2.2.1.10 derived for a keyed file: COMMIT makes its changes
permanent *"so that they will remain in that state even in the event of subsequent abnormal termination"* (§9.1.18.3) and
*"permanently applies any changes made to the files specified in all the active APPLY COMMIT clauses and releases all record
locks associated with those files"* (§14.9.7.4 GR3); ROLLBACK restores it *"to the state they were in at the execution of the
last COMMIT statement or the start of the run unit if no COMMIT statements had previously been executed"* and *"This includes
the setting of all file and record locks on those files"* (§14.9.36.4 GR3); abnormal termination rolls back *"all file changes
made since the previous commit or, if none, the beginning of the run unit"* (§9.1.18.5) and normal termination commits them
(§14.6.11 1): *"An implicit COMMIT statement is executed for the files specified in all active APPLY COMMIT clauses in the run
unit"*); the CLOSE keeps the file lock (§14.9.6.4 GR9: *"Except when the file is specified in an APPLY COMMIT clause, the file
lock and any record locks associated with the file connector referenced by file-name-1 are released by the execution of the
CLOSE statement"*) until the COMMIT (§14.9.7.4 GR4: *"For files specified in active APPLY COMMIT clauses that have been closed
and not reopened prior to the COMMIT statement, the file locks are released"*; §9.1.15: *"where a file has been closed and not
reopened prior to a commit, the file lock is removed by the COMMIT statement"*) or the ROLLBACK (§9.1.15's last sentence); the
OPEN's SHARING phrase is forbidden (§14.9.27.3 SR7: *"The sharing phrase shall not be specified for a file subject to an APPLY
COMMIT clause"*), so the posture is the file control entry's SHARING clause or the implementor's default; a DELETE FILE of
such a file is allowed and undone by a rollback (§14.9.10.4, the NOTE under GR15: *"Such files can still be deleted, however
if they are, then in the event of a rollback they will be restored to the state they were in at the last commit or, if none,
the start of the run unit"*); and a fatal condition on it runs item 8's unconditional ROLLBACK (§14.6.13.1.3 8): *"a ROLLBACK
statement is executed unconditionally"*). The scope is 2.2.1.10's, derived there from §9.1.17, §9.1.16 and Annex D.2.5 and
settled by the precedence survey: the RUN UNIT's own changes through its covered connectors, and nothing of another run unit's
is committed, restored or discarded by this one. What the standard leaves to the implementor, and this section decides: where
the before-state is kept (§9.1.7.2 lets the implementor add *"information ... to the record on the physical storage medium"*
and 2.2 chose to add none to a sequential file — it is plain bytes, or plain text, for interchange), what a sequential
transaction locks (§9.1.16: *"The implementor may specify circumstances other than a locked logical record that result in the
return of a locked record status"*), what a connector sees of its own file after a rollback that removed it (GR3 restores the
file and its locks and says nothing of the connector's open mode), and when a dead run unit's transaction is rolled back
(2.2.1.10's survey: at the first contending access, Micro Focus; retained until backout, IBM; GnuCOBOL has no commit control).

**2.2.2.2 What a sequential file's changes are — the whole list, and the composition derived from their ORDER.** A
sequential connector changes a physical file in exactly five ways, each a verb arm of `SequentialConnector` today: (a) a
WRITE under OPEN OUTPUT or EXTEND APPENDS bytes at the end — a fixed-width block, a length-framed VARYING frame, a delimited
line, or the print-control stream's record with its line ends, form feeds and LINAGE margins (`WriteRecord`,
`WriteAdvancingRecord`, through `ReleaseRecord`); (b) a REWRITE under OPEN I-O OVERWRITES one record IN PLACE and at its own
size — §14.9.35.4 GR16 refuses a record sequential replacement whose byte count *"is not equal to the number of bytes in the
record being replaced"* with '44', and GR17 c) space-fills a shorter line sequential record to the replaced line's length
(`OverwriteInPlace`, the ONE in-place overwrite); (c) an OPEN OUTPUT of an EXISTING file TRUNCATES it to nothing (§14.9.27.4
GR18: *"After the creation of the file, the file contains no records"*; `OpenWriter(FileMode.Create)`); (d) a DELETE FILE
(§14.9.10 format 2) REMOVES a closed file; (e) a CREATION — an OPEN OUTPUT of an ABSENT file (GR18: *"the successful execution
of the OPEN statement creates the file"*), and an OPEN EXTEND or I-O of an absent OPTIONAL file (GR17: *"the OPEN statement
creates the file. This creation takes place as if the following statements were executed in the order shown"* — an OPEN
OUTPUT and a CLOSE — and *"the I-O status value associated with file-name-1 is set to '05'"*; `OpenWriter(FileMode.Append)`,
`HostFile.OpenAuxiliary(FileMode.Create)`). There is no DELETE of a record, no key, no reordering, and no change to a byte
before the record being rewritten. **The composition is derived from the ORDER the changes run in, and it has two phases.**
In the BASE phase the file is the committed file with (a) appended past its committed length and (b) overwritten below it,
in any interleaving: the state at the last COMMIT is **L, the committed length** — the byte length of the file at the
transaction's FIRST APPEND, read at that statement and never at its begin (2.2.2.3 says why: at an append no other transaction
lives, so the length then is the committed one; at the begin it may count another transaction's uncommitted appends, present
or still in flight — the branch review's R2-1), to which a rollback TRUNCATES, undoing every append whatever its framing; a
transaction that never appended has no L and truncates nothing — and **the before-images of the records REWRITTEN that are
not its own appends** — (ordinal, byte offset, length, the bytes), the FIRST image of each ordinal only (a second REWRITE of
the same record adds nothing, as 2.2.1.10's "the first image is the one to restore"; a REWRITE of a record the transaction
itself appended lies at or past L and needs none, the truncation removes it). The first (c) or (d) ENDS the base phase: it CAPTURES the
file as it then stands — the committed file with the base phase's appends and overwrites in it — as **the before file**, and
from that point the file is REPLACED: every byte of it is the transaction's own, and every later verb, (a) to (e) alike (a
second OPEN OUTPUT, a DELETE FILE after an OPEN OUTPUT, an OPEN OUTPUT that creates the file again after a DELETE FILE, a
REWRITE of a record written since the capture), changes nothing the rollback needs to know, because the rollback discards the
replaced file whole and restores the before file, then re-applies the base phase's images to it and truncates it to L when
it has one. A transaction whose first change is (e) is REPLACED from the start with NO before file: the state at the last COMMIT is "no
file", and its rollback REMOVES the file. **Why by phase and not by a list of entries in time order:** an image taken after
the capture describes the REPLACED file, not the committed one — the branch review's R1-1: an OPEN OUTPUT, three WRITEs, a
CLOSE, an OPEN I-O, a READ and a REWRITE, journaled as the first text had it, rolled back to the committed file with the
replaced file's first record written over its first record — so the design journals nothing after a capture, and the
executable model (2.2.2.12) holds that composition for every order of the five verbs up to six statements deep.

**2.2.2.3 The transaction, and what it holds.** A TRANSACTION is one run unit's open unit of work on one physical file,
exactly as in 2.2.1.10: it begins at the first change — the first WRITE, REWRITE, OPEN OUTPUT, DELETE FILE or creation the
run unit executes through a connector under commit control on that file; a run unit that only reads has nothing to undo and
no transaction (it keeps its handle, 2.2.2.9, since GR9 keeps its file lock). Its HOLDINGS, the locks that outlive its
statement and that every other connector's lock test meets, are: (R) every ORDINAL it rewrote that is not its own append (the
record's §9.1.16 identity on this organization, `LastReadRecordId`; the image's ordinal), (END) the APPEND POINT and, once it
has appended, every ordinal at or past n(L), the record count at L — the records it appended — and, once REPLACED, (ALL) the
whole file. **Why the end of the file is a
holding (the determination §9.1.16's latitude grants):** a sequential record's identity is its ordinal, and a record removed
from the MIDDLE of a sequential file renumbers every record after it, so the only rollback that keeps every other connector's
record identities true is a TRUNCATION, and a truncation is only the undo of the transaction's appends if no other connector's
record lies past L. Holding the append point makes that so: a WRITE through any other connector — another run unit's under
`SHARING WITH ALL OTHER` (§14.9.51.4 GR19's concurrent extenders), or this run unit's through a connector no APPLY COMMIT
names — answers '51' while ANY transaction on the file lives, with the RETRY phrase applying as GR16 provides *"for the case
where resources needed to write a record are locked by another run unit"*; GR19's own *"otherwise in an undefined order"*
is what makes this a scheduling of the appends and not a change to any record. **Why every live transaction holds the end,
not only the one that appended — and why L is read at the first append, never at the begin:** an appending transaction's
rollback truncates to ITS L, so an append by another transaction after that L was read would be cut by it. So two
transactions never append to one file at once, and a transaction that only rewrites blocks another's appends until its COMMIT
— a coarser lock than the records it touched, stated as the determination it is; a transaction that rewrites a record the
other neither rewrote nor appended proceeds beside it (2.2.1.10's scope: its change is its own, outside the other's rollback).
That second transaction's L, were it read at its BEGIN, would count the first's uncommitted appends: those already in the file,
which the first's ROLLBACK then removes — a rollback truncating to that L after it would EXTEND the file with zero bytes, a
garbage record in a fixed-length file, NULs in a line sequential one, a misframed length word in a VARYING one, the branch
review's R2-1 — and the one still IN FLIGHT between the first's begin (under the entry mutex) and its write (outside it),
which that L would not count, so a rollback truncating to it would cut the first's record, committed or not. Read at the
first append, under the lock test that found no other live transaction, L is the committed length and nothing of anyone
else's can lie at or past it; a transaction that never appends has no L and never truncates; the model's interleavings
hold both cases (2.2.2.12). A READ not IGNORING LOCK of a held ordinal, and a
REWRITE of one, answer '51' as today's region lock answers them. **(ALL):** a REPLACED transaction holds every byte: another
run unit's writable OPEN of the file answers '61' until the COMMIT or ROLLBACK, and an OPEN OUTPUT or a DELETE FILE by anyone
also needs no other connector's handle on the file (Table 19's OUTPUT rows and DOC-A.1-75's exclusive request, as today),
which is what lets a capture copy the file with no one writing it (invariant I4 of the model). The transaction's READ locks
(the implicit AUTOMATIC posture's, §12.4.5.9.4 GR4, on records a covered I-O connector READ) stay region locks as in 2.2.1.10,
released by COMMIT, ROLLBACK and termination. **The lock test** of READ (not IGNORING LOCK), WRITE and REWRITE, at
DOC-A.1-104's point of their order, under a posture that admits another writer (`FileLockPosture.AdmitsAnotherWriter`, `SHARING
WITH ALL OTHER`; under `NO OTHER` and `READ ONLY` the host refuses the other connector's handle and no test is needed), lists
the file's open journals (2.2.2.4): this run unit's own transaction through a covered connector proceeds; any other
transaction's holdings — another run unit's, or this run unit's through an uncovered connector — are READ FROM ITS JOURNAL
(the journal of a live transaction is readable; its holder is not) and its owner's HOLDER is probed AT THE TEST: held, '51'
(RETRY applies); free, the owner died and the transaction is settled THERE (2.2.2.8), under the entry mutex the tester takes
for it, before the tester's own change, and the statement's conditions are evaluated again. The reasoning is 2.2.1.10's
finding R4-1 unchanged: no probe closes a window between itself and a test of a different lock, so the holding and the
liveness probe are read in the same step, and the holding is a durable record, never a region byte the operating environment
releases with a dead handle. A WRITE's first change — the test, the drawing of the journal, the reading of L and its record
— runs under the entry mutex as ONE step, so no other transaction begins on the file between the test and the header (the
model's interleavings at every operation boundary are what hold this). **So does the change of a connector no APPLY COMMIT
names** under a posture that admits another writer (review finding R1-1; `SequentialConnector.UncoveredChangeGuard`): its
lock test and its WRITE or REWRITE, through the RELEASE of the record to the operating environment, are one step under the
entry mutex. Tested outside it, another run unit's covered WRITE could begin and read its L between the test and the write,
so the uncovered record landed at or past L, outside every holding, and that transaction's ROLLBACK (or the settle after its
death) cut it — a record GR19 and GR12 keep; under it a transaction begins either before the test, which meets its END
holding ('51'), or after the release, whose bytes its L counts (and its image of a rewritten record holds the uncovered
bytes). Where this host cannot hold the mutex (a lock directory the process may not create), the uncovered statement
proceeds without it, DOC-A.1-75's rule for a waiting hold; a covered first change there is refused '30', so no transaction
of this process begins beside it either. The price is the mutex's lock file per such statement, serializing the WRITEs of
the uncovered writers that share one file across run units (`unit:SequentialCommitControlTests`
`An_uncovered_writers_record_is_never_cut_by_a_transaction_that_begins_during_its_write`: the covered WRITE run at the
uncovered one's release, through the fault-injecting medium's `BeforeOperation` seam). **The RETRY phrase (§14.7.9.3 GR4 b), as built in
`SequentialConnector.JournalLockTest`):** every journal arm of the test — WRITE, REWRITE and READ, covered or not — is ONE
attempt function under `FileRegistry.RetryLoop` with the statement's phrase, so a statement that meets a holding waits and
attempts again exactly as one that meets a region lock does. Each attempt is a whole entry step (a dead owner met on a later
attempt is settled there); an attempt that needs the entry mutex (a covered connector's first change) takes it for that
attempt alone and gives it back before the wait, so a waiting statement never keeps another run unit's entry step — or the
holder's own retirement — out. A holding of the tester's OWN run unit's transaction (met through a connector no APPLY COMMIT
names) cannot end while the statement runs, so the attempt is `RetryAttempt.InRunUnit`: no wait, and RETRY FOREVER ends
with '52' (A.1 item 109) — the region lock's rule for a holder inside the run unit (`unit:SequentialCommitControlTests`
`A_statement_meeting_another_run_units_journal_holding_retries_under_its_retry_phrase` and
`A_holding_of_this_run_units_own_transaction_is_not_waited_for`).

**2.2.2.4 The journal: where it lives, what it holds, and why nowhere else will do.** The journal of a transaction on physical
file F is THREE transient files in F's lock directory — `.wolk-<16 hex>/` beside F, named by `HostFile.PhysicalFileKey`
exactly as `SidecarLockFiles.LockDirectoryOf` names it, created on every host with the permissions `HostFile.CreateLockDirectory`
gives it (on macOS the directory already exists while any holder lives; on Linux and Windows it is created for the journal
and retired with its last file by `SidecarLockFiles.RetireDirectory`'s rule): **`txn-<id>.live`, the HOLDER** — a lock file in
`SidecarLockFiles`' sense on EVERY host, published pending-then-renamed and held open with the exclusive advisory lock
`HostFile.OpenLockHolder` takes for the life of the transaction, probed shared by `HostFile.ProbeLockFile` and refused exactly
while its owner lives, so "is this transaction's owner alive" is the one test the lock-file primitive already answers, with
the liveness the kernel gives a descriptor (a killed run unit holds nothing); **`txn-<id>.journal`, the RECORD** — opened by
everyone with read and write sharing, readable while its owner lives, which is what lets another run unit's lock test read
the holdings (2.2.2.3) and the entry step read a dead owner's decision; and **`txn-<id>.before`, the BEFORE FILE** — F's bytes
copied whole at the capture, with no header, present only once a transaction has captured. *Why not inside F:* 2.2 made a
record sequential file plain bytes and a line sequential file plain text on purpose (*"a header would stop a record
sequential file being plain bytes and a line sequential file being plain text, which is the interchange property those shapes
exist for"*, `RecordFraming`), and a fixed-length file has nowhere to put a byte that is not a record; the keyed engine keeps
its UNDO in the file only because its format is its own. *Why not in the run unit's memory alone:* §9.1.18.5 rolls back the
changes of a run unit that terminates abnormally, and 2.2.1's bar — the bar PB3340 names, "crash-consistent in the same sense
as 2.2.1" — includes the terminations the runtime cannot see (a kill, a power loss); an UNDO that dies with the process leaves
a killed run unit's uncommitted appends in the file for ever and makes "restored at the next entry" a promise the keyed
organization keeps and the sequential one breaks. §14.6.12's *"The circumstances of abnormal termination may be such that
execution of some or all of these operations is not possible"* licenses the runtime to do less when it CANNOT; it does not
license a design that cannot. *Why the lock directory:* it is the one place beside a physical file this runtime already owns,
keyed by the same name rule two spellings of one file share, created and retired by rules already measured for races
(`SidecarLockFiles`, kb/Work PB2917, PB3109); a journal is as transient as a lock file — it exists from the transaction's first
change to its retirement, and a leftover is consumed by the next entry (2.2.2.8) — and it describes no attribute of F that a
program reading F without this runtime would need: a copy of F taken without its journal is F as it stands, committed. That
transience, and that F stays byte-identical to what a program with no transactions would write, is what distinguishes it from
the catalog kb/Work PB802 refused ("no sidecar of any type", 2026-09-07: a `.cbattr` file describing every data file for ever,
consulted at every OPEN); the owner decided on 2026-10-19 that those words do not cover it (PB3340, "option a approved"; PB802
carries the clarification), and 2.2.2.11 keeps the rejected shapes. *Why the liveness is a holder file and not a byte of F's region* (2.2.1.10's `TransactionLivenessByte`,
which stays the keyed engine's): a byte of F's region is held through a handle on F, and this transaction's F may be DELETED
(a DELETE FILE under commit control removes the physical file, and its kept handle with it) or not yet EXIST (a creation's
first change precedes the file); a holder in the lock directory is independent of F's existence, and the one primitive gives
it the same liveness on every host. *Why the record is not the holder:* a live holder's content cannot be read by another
process (Windows' mandatory share modes; .NET's exclusive advisory lock on Unix refuses a reader's shared one), and the
holdings must be readable while their owner lives (R4-1). **The ENTRY MUTEX** — one per physical file, `EntryMutexByte`
(2^62 + 0x102, a new byte kind in 2.2.1.8's map) held EXCLUSIVELY through `SidecarRegionLocks` on EVERY host, whose exclusive
hold needs no handle on F (`ExclusiveNeedsWritableHandle` is false there: its holder is a file in the lock directory), because
the file it guards may not exist, which is the one case `HandleRegionLocks`' byte on F cannot serve — is taken for: every
writable OPEN (the entry step through the host open or creation), every DELETE FILE, every first change, every capture and
every rollback; never for an ordinary WRITE, READ or REWRITE. **The format of the record** (little-endian, like 2.2.1.4's): a
HEADER of 24 bytes — magic `WOLJ`, format 1, the transaction id (8), the START (`P`, the file was present, or `A`, absent), a
CRC-32 of the header — written and synced once, with the lock directory synced after it, before the first byte of F changes;
then RECORDS, each with a CRC-32 and each appended and synced before the change it precedes: `L` (the length 8), at most one,
the transaction's first append (R3); `R` (ordinal 8, offset 8, length 4, the bytes), one per rewritten ordinal of the base
phase that is not the transaction's own append; `K` (the kind, `O` for an OPEN OUTPUT or `D` for a DELETE
FILE, and the before file's length 8), at most one, the capture; and `D` (the decision, `C` committed, `O` open again after a
reversal, `R` rolled back), the LAST whole one of which is the transaction's decision, and none is `O`. A torn tail (a short
record, a failed CRC, an `R` length word the rest of the record file cannot hold — judged in 64-bit arithmetic and against the largest array before anything is allocated, PB3614) ENDS the records, exactly as a torn frame ends a stream file's data: the record's step had not changed F
when the death fell (2.2.2.5), so F is as the records before it say. **The transaction id** is DRAWN under the entry mutex:
the lowest non-negative integer with neither a holder nor a record file in the directory — a dead transaction's files were
settled by the entry step just before, under the same hold, so the smallest id is free once the directory is clean, and no
mint is needed because the files ARE the transaction's entry: their names carry the id. **The handle that holds the §9.1.15
file lock across the CLOSE.** A covered sequential connector takes its host handle at its first OPEN after the last COMMIT or
ROLLBACK and KEEPS it — across its CLOSE (§14.9.6.4 GR9) and across a re-OPEN in the same transaction, whose reader or writer
is rebuilt over the kept handle (an OUTPUT re-open truncates it after the capture, an EXTEND re-open seeks to its end, an INPUT
or I-O re-open seeks to its start) — until the COMMIT (§14.9.7.4 GR4) or ROLLBACK (§9.1.15's last sentence) releases it, as
`KeyedConnector` keeps its handle in its `KeptFile` (PB3337); the two exceptions are a DELETE FILE, which closes it
before the physical file is removed (a Windows handle not opened for deletion blocks the removal, and after it there is no
file to lock: the (ALL) holding is what refuses the next opener), and a creation, which opens it. It is opened READ-WRITE
where the host grants it (every mode of the transaction must be servable through it; a host that refuses the write grants a
read-only handle, and a later OPEN OUTPUT through it is the '37' §14.9.27.4 GR3 gives a file the run unit may not write),
with the posture's share mode, and it is the data handle too: the capture reads F through it, the restore writes F through it.
*Why the capture is a COPY and not a rename:* a handle follows the file it was opened on, so a rename of F to the before file
would leave the kept handle — the file lock — on the before file and F's name unguarded until a second handle took it, which
is the gap 2.2.1.10 forbids; and Windows refuses the rename of a file whose handle was not opened for it. The copy costs F's
size once per transaction, at the first OPEN OUTPUT of an existing file or DELETE FILE under commit control — the keyed store
pays the same through its UNDO pages for the same verbs — and nothing for a transaction that only appends and rewrites.
**The posture of a covered connector with no SHARING clause** is the implementor's (§9.1.15): `SHARING WITH NO OTHER` for
every open mode, including INPUT — the one handle serves every mode of the transaction, and the stricter of the two defaults
(`FileLockPosture`: READ ONLY for INPUT, NO OTHER otherwise, kb/Work PB322) is the only one that serves OUTPUT. A program that
wants concurrent readers of a file under commit control writes `SHARING WITH READ ONLY` or `ALL OTHER` in the file control
entry (SR7 forbids it on the OPEN).

**2.2.2.5 The write-ahead rules — five, each held by the model at every operation and each with a drift test.** **R1 — the
header before the first change.** Under the entry mutex: the holder published, the record created, the header (START) written
and SYNCED, the lock directory synced — all before the first byte of F changes or the file is created. A holder that cannot be
published refuses the statement ('30', §9.1.13.6 item 1; an OPEN leaves the file unaffected, §14.9.27.4 GR25) for either of two
causes the medium's `PublishHolder` answers with the publication's `RegionLockResult` (kb/Work PB3675): `Unavailable`, the host
takes no lock here, and `Held`, every try lost its race to another run unit (a failure to publish, not a finding about the
host): `SidecarLockFiles.Publish` counts three ways to lose a try, the unit removed the lock directory, or reaped the pending
holder file, or held it as it was locked or renamed (kb/Work PB3749); `Begin`'s refusal names the cause that happened. The model's mutation
`R1`, the header not synced, is RED on its witness row (2.2.2.12: "first WRITE under EXTEND"): a power loss after an append
with no durable header leaves the append in F for ever.
**R2 — the image before the overwrite.** A covered REWRITE in the base phase of a record that is not the transaction's own
append (every ordinal before its first append; one below n(L) after it) appends its `R` record and SYNCS it before
`OverwriteInPlace` writes F; the first record for an ordinal is the only one written (the connector keeps the set of ordinals
it has journaled in this transaction; a run unit that rewrites a million distinct records holds a million ordinals, as the
keyed transaction holds a million holdings — D.2.5's deferred commits make it unbounded by design). The data write itself is
not synced at the statement: a power loss that loses it leaves the record as its image, which the rollback re-applies anyway
(idempotent), and the COMMIT syncs F once. A REWRITE in the REPLACED phase, or of an ordinal at or past n(L), writes no record
(2.2.2.2). **R3 — the first append writes L; no append writes more.** The transaction's FIRST append reads F's length through
the kept handle after its lock test, which found no other live transaction (2.2.2.3), so that length is the committed one;
it appends the `L` record and SYNCS it before the record is written (for a first change that is a WRITE, under the same hold
of the entry mutex as the header). Every later append, and an append in the REPLACED phase, writes nothing: the committed
length is the undo of every append, so a WRITE under commit control pays nothing at the statement beyond what its posture
already pays (`ReleaseRecord`'s flush under a sharing posture, nothing under `NO OTHER`). The model's mutation `R3`, the
record not synced, is RED on the same witness row: a power loss that drops it keeps the append for ever. **R4 — the capture's order.** Under the entry mutex, with no other connector's handle on F: the
before file created and F's bytes written into it through the kept handle, the before file SYNCED, the lock directory synced,
THEN the `K` record appended and SYNCED, and only then F truncated (an OPEN OUTPUT) or removed (a DELETE FILE). So every death
leaves one of two states the settle reads unambiguously: no whole `K` — F is intact and a before file, if any, is litter
(deleted); a whole `K` — the before file is whole (its sync preceded the record's) and F is restored from it whatever F now
holds. The model's mutation `R4`, the `K` synced before the before file, is RED on its witness row ("DELETE FILE then COMMIT"):
a power loss between the two leaves a capture naming a before file that does not exist. **R5 — a refused sync of the record is fatal.** A record handed to the operating environment
whose sync is refused (the `L` of a first append, the `R` of a REWRITE, the `K` of a capture) may or may not be on the medium,
and the run unit cannot know which; the statement answers '30' (EC-I-O-PERMANENT-ERROR), item 8's unconditional ROLLBACK follows (§14.6.13.1.3 8),
and nothing more changes through the transaction: the rollback reads the record as the operating environment shows it (the
handed-over `K` present: the before file holds what F still holds, since F was not yet truncated; the `R` present: the image
is F's record, since the overwrite never ran; the `L` present or absent: the append never ran, so F is right either way), and
a power loss that drops the handed-over record drops nothing F needed. The
first text's capture deleted the before file on that refusal and left the cached `K` naming it, which the model found at its
first run. Drift tests: R1 by a journal medium that dies before the header's sync at a first WRITE (F intact, no journal), R2
by a death between the record's sync and the overwrite and between the overwrite and the next statement (both restore the
image), R3 by one record and one sync on a transaction's first WRITE and no journal write on its later ones (the medium's
write log) and by a death between the record's sync and the append (F at L), R4 by a death between the before
file's sync and the `K`'s (F intact, the before file reaped) and after the `K`'s (F restored), R5 by a refused sync of a `K`
whose write succeeded (item 8's rollback, F intact, no litter).

**2.2.2.6 COMMIT — decided by one durable write, answered by its sync, in 2.2.1.10's three regions.** For every physical file
on which the run unit has a transaction, sequential and keyed alike in the one order of the run unit's open files
(`FileRegistry.Commit`, PB3337): (1) F is synced through the kept handle (the writer's buffer flushed, `FlushFileBuffers` /
`fsync`), so every append and overwrite since the last COMMIT is durable, and F's DIRECTORY is synced, so a file the
transaction created or removed is created or removed on the medium too (a created file whose directory entry was never synced
is absent after a power loss, and a removed one is back); (2) THE DECISION: the record `D C` is appended and the journal
synced — one durable write, which is what 2.2.1.10's slot write is; (3) THE RETIREMENT: the before file (if any), the record
and the holder are deleted in that order and the lock directory synced (the directory retired with its last file), the
connector's READ locks released (`PhysicalFileTable.ReleaseAllForConnector`), the kept handle of a connector CLOSEd since the
last COMMIT released (GR4). The regions: **(1) refused before the decision's sync** — F's sync, the directory's, the `D C`
write itself refused: the journal still reads open, the transaction is intact, the COMMIT is unsuccessful — 2.2.1.10's
determination (PB3338's commit row): the fatal EC-I-O-PERMANENT-ERROR for F with '30' in F's status item, so §14.6.13.1.3 item
8's unconditional ROLLBACK follows and restores from the intact journal. **(2) the decision synced:** committed, and the COMMIT
answers success whatever becomes of the retirement — a journal left reading `C` is completed (deleted) by the next entry
(2.2.2.8), and by its own holder's next entry step while that holder lives (to every other run unit its holder is alive, so
only the owner can finish it before it exits). **(3) the decision's write handed to the operating environment and its sync
REFUSED — the unacknowledged COMMIT.** Every live process reads `C` through the operating environment while the medium holds
open or `C`. As in 2.2.1.10 the COMMIT makes "not committed" true before it answers: under the entry mutex it REVERSES —
appends `D O` and syncs, then runs the rollback of 2.2.2.7 (whose material is intact: no retirement ran) — and answers
unsuccessful, so item 8's ROLLBACK finds no transaction on F. When the reversal is refused too (the `D O` write or its sync),
the transaction's fate on the medium is unknown: the registry MARKS F unacknowledged in the run unit's memory, the COMMIT
answers unsuccessful with '30', and the only statements this run unit then runs on F are item 8's ROLLBACK and the
termination path's file-only rollback, each of which repeats the reversal under the mutex and touches F only once its own
sync succeeds; a later entry settles the medium's state (a `C` is completed, an open journal is rolled back), and the model's
invariant for the row is that F then holds exactly one of the two states and never a third. A journal whose header the next
entry finds torn is litter with nothing to undo (the header's CRC is checked before its decision is believed: a torn header
preceded the first change, R1). **What the program is told** is 2.2.1.10's determination unchanged, and the per-file order
of a multi-file COMMIT is its determination too: a COMMIT refused at file i leaves the files before i committed.

**2.2.2.7 ROLLBACK — one idempotent sequence under the entry mutex, run by whoever settles the journal.** For every physical
file on which the run unit has a transaction (the ROLLBACK statement, §14.9.36.4 GR3; item 8's unconditional one; §14.6.12's
file-only one at abnormal termination; and the entry step for a transaction of a run unit that died, 2.2.2.8), under the
entry mutex, from what the record says: (1) a whole `K`: F is REPLACED by the before file — created if absent, truncated to
nothing if present, the before file's bytes written in through the writer's handle (the owner's kept handle; the entering run
unit's own writable handle, opened for the purpose, when the owner is dead) — and a before file with no whole `K` is deleted
(litter: F was never truncated, R4); (2) START `P`: every `R` record's bytes are written at its offset (one below F's current
length; an offset at or past it is left to the truncation), F is truncated to L (`SetLength`) when the journal has an `L`
record — F is never shorter than that L, because only an appending transaction's rollback truncates, to its own L, and two
transactions never append at once (2.2.2.3), which the model asserts at every rollback and reaches only under its mutation
`R2-1`, on the witness row 2.2.2.12 names for it (the branch review's R3-1); a journal with no `L` record truncates
nothing (the branch review's R2-1: the first text's L, read at the begin, counted another transaction's appends, and a
`SetLength` to it after that transaction's own rollback would have EXTENDED F with zero bytes) — F is synced and its
directory synced; START `A`: F is removed (the owner closes its kept handle first) and its directory synced — the state at the last
COMMIT had no file, so a later OPEN INPUT answers '35', or '05' for an OPTIONAL file (GR17's creation undone: the branch
review's R1-2, which the first text truncated to an empty file where the standard requires none); (3) the record `D R` is
appended and synced, so a journal found with it is retired and never rolled back again — in particular one that a power loss
resurrects after its before file was deleted (the deletions of step 4 reach the medium only with the directory's sync, and a
resurrected `K` with no before file would otherwise name a restore that cannot run); (4) the retirement of 2.2.2.6 (3): the
before file, the record, the holder, the directory sync, the READ locks, the kept handles of connectors CLOSEd since the last
COMMIT (§9.1.16, §9.1.15's last sentence). Every step is idempotent and every death between steps leaves the journal present
and reading open (or `R`, retired by the next settle), so the next entry runs the same sequence again from step 1; the model
executes a refusal, a death and a power loss at every one of these operations and reads F equal to the state at the last
COMMIT after each. A rollback the OWNER runs that is refused midway is OWED: the transaction stays open, its holder held, and
the owner's next ROLLBACK, and its termination path, run the sequence again before anything else, and a COMMIT — the
program's, or the implicit one of a normal termination — is REFUSED ('30', item 8's ROLLBACK following), never applied: the
half-run restore a refused step leaves (F truncated by a capture's restore, images written back without the truncation to L)
is never made permanent, and the before file is never retired under it (the keyed rule of 2.2.1.10 (5), held in the one
place, `FileRegistry.CommitHolder`; review finding R1-3, witness `unit:SequentialCommitControlTests`
`A_transaction_whose_rollback_the_medium_refused_is_never_committed`). A handle the owner's rollback opened for the purpose
(no covered connector holds F) is closed whatever the rollback did, so a refused rollback never leaves one to refuse its own
retry's START `A` removal (review finding R1-5); a kill leaves it to the next entry. **F shorter than L** (review finding
R1-4): in a run of the protocol it cannot be, but L is read from the operating environment's view of F, which counts another
connector's appends that were released and never synced (an uncovered writer's CLOSE flushes; it does not sync), and a power
loss takes them; then none of the transaction's appends survived (they all lie past L), so step (2) truncates nothing and
leaves F as it stands — a truncation would extend F with zero bytes, and a refusal left the dead transaction unsettled, every
later writable OPEN answering '30' until an administrator removed the directory (witness
`A_dead_transaction_whose_file_is_shorter_than_its_committed_length_is_settled`). **The connector's own state after its run unit's ROLLBACK** (GR3 restores *"all file and record locks on
those files"* and the file, and leaves the open mode to the implementor): an OUTPUT or EXTEND writer's next WRITE lands at the
restored end (its stream is repositioned at L, or at the restored file's end — a `SharedAppendStream` repositions at every
write by construction), and when the rollback REMOVED the file (START `A`) the writer's next WRITE CREATES it again, as its
OPEN did, beginning a new transaction (START `A`) under the entry mutex: the connector's open mode is the program's, the
state at the last COMMIT was "no file", and the kept handle went with the file as the file lock did; an I-O or INPUT
connector's file position indicator and its last-read anchors are unchanged (the record it read back is restored in place at
the same offset, so a REWRITE after the ROLLBACK targets the same bytes; a position past the restored end reads AT END, '10';
a removed file reads AT END and a REWRITE on it answers '43', no READ having succeeded since); a print file's LINAGE page
state — `LinageCounter`, the device line, the pending top margin, the page model — is restored from the snapshot the last
COMMIT (or the OPEN) took, since the bytes it describes are the ones the truncation kept; the data items of the LINAGE and
RECORD clauses and the file status items are the compiler's (PB3338, §14.9.7.4 GR5). **The dead-owner rollback is this same
sequence** run by the entering connector of ANY run unit through ITS writable handle, which is why a leftover journal must be
readable and F writable by that run unit: a journal this process may not read is a holder it cannot observe (the
determination of DOC-A.1-75 for a lock file applies): the statement's lock test answers '51' and a writable OPEN of F answers
'61', loudly, until the owner or an administrator removes the `.wolk-*` directory — a refusal, where a silent grant would lose
a dead run unit's rollback. A lock directory this process may not even LIST (on Linux, the directory of F at mode 000 hides
it, and F with it) is no holder known to conflict: the entry step lacks the authority it needs, so a writable OPEN answers
'37' (§9.1.13.6 6) b), *"insufficient authority exists to access the file"*) and a covered DELETE FILE '37' (§14.9.10.4 GR16)
— the answer the OPEN's own presence probe gives such a file (`Holdings.Unlistable`; measured by the Linux gate, whose
`FileAuthorityPresenceTests` met the '61' first).

**2.2.2.8 The entry step, and what a file not under APPLY COMMIT pays.** At every WRITABLE OPEN of a sequential file (OUTPUT,
EXTEND, I-O; any posture, any run unit, covered or not), at every DELETE FILE, and at a covered connector's first change —
under the entry mutex, which the statement then keeps through its own host open, creation, capture or drawing — the
connector lists F's lock directory for `txn-*` names and, for each transaction id: its holder held → a live transaction,
whose holdings the entering statement's test then meets (a REPLACED one: '61' to a writable OPEN and '62' to a DELETE FILE,
§9.1.13.9 2)'s file sharing conflict of that statement; a base one:
'51' to a WRITE, to a READ or REWRITE of a held ordinal, and '61' to an OPEN OUTPUT or '62' to a DELETE FILE, which need no other
transaction on the file at all); its holder free → SETTLED, from the record: no record, or a torn header → litter (the holder,
a before file, the record: deleted); the decision `C` or `R` → the retirement completed; open → the rollback of 2.2.2.7. At the
entry of every WRITE, READ not IGNORING LOCK and REWRITE through a connector whose file lock admits another run unit's writer
(`FileLockPosture.AdmitsAnotherWriter`, `SHARING WITH ALL OTHER` only — the one posture under which a transaction can begin and
its owner die between two of this connector's statements), the same listing and probes run WITHOUT the mutex (a live
transaction's holdings are read from its record; a dead one is settled under the mutex, taken for that alone, before the
statement proceeds). The cost to a file NOT under APPLY COMMIT: ONE directory listing per writable OPEN and DELETE FILE (the
price §9.1.18.5 exacts from every writer so that a killed run unit's transaction is rolled back by the next writer of the
file — the keyed engine pays a page-2 read for the same promise; a directory that does not exist is the common answer and
costs one failed open), and under `ALL OTHER` one listing per changing statement, beside the seek and the count that posture
already pays per release (PB2692); no mutex, no journal, no holder, no sync. A connector opened INPUT runs no step and rolls
back nothing (no writable handle: it can neither restore F nor remove it): it reads F as it stands, a dead transaction's
appends included — D.2.5's "possible for another run unit to read a record written or updated by the current run unit" for a
run unit that no longer exists, the determination DOC-A.1-75 and the commit row PB3338 writes state for a keyed file — and its READ
not IGNORING LOCK of a held ordinal answers '51' as today.

**2.2.2.9 CLOSE, re-OPEN, DELETE FILE and termination under commit control.** A covered connector's CLOSE releases its records
(the writer's buffer flushed, an open print line ended, `CloseCore` as today), disposes its reader or writer, and KEEPS the host
handle with its region endpoint — the §9.1.15 file lock and the transaction's READ locks — as a `KeptFile`
does for the keyed organizations (PB3337); the holder file is the REGISTRY's, held from the first change to the retirement
whatever the connector does. A re-OPEN before the next COMMIT or ROLLBACK rebuilds the reader or writer over the kept handle
(2.2.2.4), and an OPEN INPUT of the file by the same run unit in that interval reads its own uncommitted changes (D.2.5; the
one handle sees its own writes). A DELETE FILE under commit control captures (R4) when the transaction is in its base phase,
closes the kept handle, removes the physical file and syncs its directory; the (ALL) holding refuses every other opener until
the COMMIT or ROLLBACK, and the run unit's own OPEN OUTPUT of the name in the same transaction creates the file again (a new
kept handle; REPLACED already, so nothing is journaled), its OPEN INPUT answers '35' (or '05' if OPTIONAL) and its OPEN EXTEND
'35' (or creates, if OPTIONAL), as the standard's file-not-present rules say of a file that is not there. Normal termination
commits every transaction (§14.6.11 1), then closes; the runtime's abnormal-termination path runs the file-only rollback of
2.2.2.7 before the process exits, retrying one that is owed (PB3338 routes it); a kill is the entry step's. A SORT or MERGE
whose GIVING file is a covered connector writes through that connector under its transaction (the sort's OPEN OUTPUT is a
capture, its WRITEs are appends); the EC-FLOW conditions of §9.1.18.7 are the compiler's.

**2.2.2.10 LINE SEQUENTIAL and the print stream are COVERED, with the same journal** (the explicit decision PB3340 asked for;
`docs/CONFORMANCE.md`'s commit row states it when PB3340 lands). §12.4.5.10.3 GR2 makes a line sequential record *"terminated
by an implementor-defined line delimiter"* and nothing else, so its two changes are the two the journal already undoes: an
append (truncated by L) and an in-place same-span overwrite (GR17 c), the image of the line's bytes at its anchor
`_lastLineStart`, length `_lastLineBytes`). A print file's WRITE ADVANCING arm and a LINAGE file's margins and form feeds are
appends. The byte-order mark of a UTF-8 line sequential file (DOC-A.1-115) is written at creation and is part of the before
file or lies below L, never inside a record. Nothing in the journal is organization-specific but the ordinal a record names
(a line's ordinal is its line number, as `_readOrdinal` counts it), which is why one journal serves the three framings and the
print stream.

**2.2.2.11 The shapes the owner rejected (2026-10-19 15:45 PDT, kb/Work PB3340: "option a approved"), kept with what each loses
so they are not re-asked.** The owner was shown four, with the costs of 2.2.2.13 and the losses below, and chose (A), this
section. **(B) Memory-only undo:** the same transaction, holdings and lock test (2.2.2.3) with the record kept in the run
unit's memory (`FileRegistry`), the before file replaced by an in-memory copy of F (O(file) memory at an OPEN OUTPUT of an
existing file or a DELETE FILE), and the COMMIT reduced to F's sync. What it loses: a run unit that is KILLED or loses power
under an open transaction leaves its uncommitted appends and overwrites in F for ever (no entry can roll them back: nothing
durable says what they were), other run units' lock tests cannot see a dead owner's holdings (they died with its memory),
and the keyed organization's §9.1.18.5 promise does not hold for the sequential one — a program's indexed file rolls back
after a crash and its sequential file beside it does not; `docs/CONFORMANCE.md` would have recorded it under §9.1.18.5 as a
documented determination with the cause, and PB3340's crash rows for the kill and the power loss would have been witnesses
of that determination rather than of a restoration. The runtime's own abnormal-termination path (a fatal exception
condition, §14.6.12) still restores under it, since the process still runs it. It is cheaper by one directory listing per
writable OPEN and one sync per covered REWRITE, and by every line of 2.2.2.4 to 2.2.2.8 that touches a file; the model of
2.2.2.12 would still apply to it with the medium's "die" and "power" modes re-read as witnesses of the determination.
**(C) The journal elsewhere** — a per-machine or per-user directory keyed by the file's physical name: nothing beside the
data file and no write permission needed there, but the crash promise holds only when the next entry runs on the same
machine as the same user; two machines sharing a file, or two users on one machine, silently get (B)'s behaviour, a reader
cannot tell which case it is in, and the lock directory's measured rules (`SidecarLockFiles`, kb/Work PB2917, PB3109) give
way to a new location rule. **(D) A header inside the file:** rejected in 2.2.2.4 — it stops a record sequential file being
plain bytes and a line sequential file being plain text, the interchange property 2.2's shapes exist for. **Why (A):**
PB802's words targeted a persistent catalog consulted at every OPEN; the journal has none of the properties that made the
catalog objectionable (it is transient, consumed by the next entry, and describes no attribute of F, which stays byte-identical
to what a program without transactions would write), its mechanism already exists in the runtime, and (B) is the only shape
honest about what it gives up, which is the one promise a transaction exists to keep.

**2.2.2.12 Every crash point and every interleaving, executed — the model, and the rows it names.** The owner's rule for a
concurrent design (2026-10-17, after the keyed store's five prose rounds) is an executable model from round one, reviewed only
once it passes. The model of this section is **`scripts/models/sequential_journal_model.py`**, a self-test every gate and CI
run (`self_tests.py` discovers it): it runs the protocol of 2.2.2.4–2.2.2.9 — the three files, the entry mutex, the kept
handle, the region READ locks, the holder's liveness, every write, sync, directory sync, create and delete in the order the
rules state — over a medium that REFUSES any one operation (the process continues), KILLS the run unit at any operation (its
locks and handles go, its unsynced writes stay in the operating environment's cache), or LOSES POWER at any operation (every
file keeps a prefix of its unsynced writes, a file created without a directory sync vanishes, a file deleted without one
returns), with a second run unit — outside commit control, and under its own: committed, rolled back, and left open across
the rest of the first's program and then committed, rolled back, killed, or appending first — run at every interleaving
point of the first: before every medium operation, and before every statement, where a ROLLBACK, a writable OPEN, a DELETE
FILE or a COMMIT takes the entry mutex as its first act (offered the points before medium operations only, the second run
unit could never begin between an append and the ROLLBACK that undid it, and the row named for R2-1 was a dead witness: the
branch review's R3-1) — and after "the next entry" (a fresh run unit's writable OPEN) asserts four invariants: **I1** F holds exactly the state at the
last COMMIT (the journal open, or torn) or exactly the state the COMMIT made durable (its decision synced), never a third, and
the program was told success only in the second case; **I2** nothing another run unit changed is undone, and a record another
run unit holds a lock on is never written by a rollback; **I3** no holder, record, before file or lock directory is left
behind; **I4** a capture or a DELETE FILE never runs while another connector holds the file. The gate's run sweeps the named
rows below and every legal program of up to four statements over an absent and a present file, OPTIONAL and not — 333,000
runs in about twenty seconds; a hand run (`--depth 5`) sweeps 684,000. **Its worth is proved in the same run** (the branch
review's R2-2): the self-test applies each named MUTATION of the protocol — each review finding put back (`R1-1`, an `R`
record journaled after a capture; `R1-2`, a creation journaled as a truncation to nothing; `R2-1`, L read at the begin) and
R1's, R3's and R4's write-ahead order broken — each naming the rows below that must catch it, its WITNESSES, and requires
each witness RED under its mutation on its own (a RED from some other row proves nothing about the row named for the
finding: under R3-1 the R2-1 row was GREEN under its own mutation while the arm passed on the in-flight twin other rows
reached), so a later change that weakens the oracle, the fault injector or a row fails the gate instead of leaving a
watchdog that no longer bites (`--mutations` runs that arm alone, `--mutation <name>` applies one to a hand sweep). It models the PROTOCOL, not the bytes (a record is a token,
L a count, a checksum "the record is whole or it is not"); the bytes are PB3340's, whose **`IHostMedium` seam** (`IO/IHostMedium.cs`) — `HostFile`'s
roles that change a file or a name (`OpenConnectorStream`, `OpenConnectorWriteStream`, `OpenAuxiliary`, `OpenTransient` for the
journal's files, the removal of a file, `SyncDirectory`, the lock-directory listing, the holder's publication and probe, the
retirement of a name and of the directory, the entry mutex) made instance-scoped on the run unit (`FileRegistry.Medium`, handed
to every sequential connector and journal through `SequentialTransactions`; the production implementation is
`HostMedium.Production`, which is `HostFile` and nothing else, as `RunUnitPageMedium` is the production `IPageMedium`), with a
test medium (`FaultHostMedium`, the unit tests') that logs every operation, REFUSES the n-th, DIES at it (the run unit's handles
and holders closed as a killed process's, its unsynced writes kept), or LOSES POWER at it (every file keeps its last synced
bytes and, only when a directory sync made it durable, its name — or, in the adversarial mode, every physical file keeps what
the operating environment was handed while the journal's files keep only what was synced), `SharedExtendOpenDriftTests`' ban on
every other host-path open under `Runtime/IO` and `SequentialJournalSeamDriftTests`' ban on every host change past the medium in
the connector and the journal making the seam complete — re-runs the same rows over real files, framings and CRCs. The
keyed organizations keep their own seam (`IPageMedium`, 2.2.1.12). The
rows, each a named scenario of the model and each run there in the refusal, death and power-loss modes at every operation and
with the second run unit at every boundary; the expected state in every row is I1's:

| row (the model's name) | the program | what it holds |
|---|---|---|
| R1-1 capture then REWRITE | F = C1 C2 C3; OPEN OUTPUT, WRITE X1 X2, CLOSE, OPEN I-O, READ, REWRITE Y1, ROLLBACK | nothing after the capture is journaled; F = C1 C2 C3 (the first text gave X1 C2 C3) |
| R1-1 append then capture | F = C1 C2; OPEN EXTEND, WRITE X1, CLOSE, OPEN OUTPUT, WRITE X2, ROLLBACK | the before file holds C1 C2 X1; truncated to L after the restore: C1 C2 |
| REWRITE then capture | F = C1 C2; OPEN I-O, READ, REWRITE Y1, CLOSE, OPEN OUTPUT, WRITE X1, ROLLBACK | the base phase's image applies to the restored file: C1 C2 |
| R1-2 OPTIONAL created by OPEN EXTEND | F absent, OPTIONAL; OPEN EXTEND ('05'), WRITE X1, ROLLBACK, CLOSE, OPEN INPUT | the file removed; the OPEN INPUT answers '05' (the first text left an empty file: '00') |
| R1-2 created by OPEN OUTPUT | F absent; OPEN OUTPUT, WRITE X1, ROLLBACK, CLOSE, OPEN INPUT | removed; '35' |
| DELETE FILE then re-create then ROLLBACK | F = C1 C2; DELETE FILE, OPEN OUTPUT, WRITE X1, CLOSE, ROLLBACK, OPEN INPUT | the re-creation is REPLACED-phase and unjournaled; the before file restores C1 C2 |
| DELETE FILE then COMMIT | F = C1 C2; DELETE FILE, COMMIT, OPEN INPUT | absent, durably (the directory synced before the decision); '35' |
| create, DELETE FILE, re-create, COMMIT | F absent; OPEN OUTPUT, WRITE X1, CLOSE, DELETE FILE, OPEN OUTPUT, WRITE X2, COMMIT | X2; a refusal or death anywhere before the decision: absent |
| first WRITE under EXTEND | F = C1 C2; OPEN EXTEND, WRITE X1, COMMIT | no journal and F intact / header torn: nothing to undo / F at L after the entry's rollback / committed only after the decision |
| REWRITE twice, ROLLBACK | F = C1 C2; OPEN I-O, READ, REWRITE Y1, REWRITE Y2, ROLLBACK | one image (C1); the record torn → ignored and F's record as before |
| COMMIT then more, ROLLBACK | F = C1; OPEN EXTEND, WRITE X1, COMMIT, WRITE X2, ROLLBACK | C1 X1: the second transaction's L is the first's committed length |
| CLOSE under commit, then COMMIT | F = C1; OPEN EXTEND, WRITE X1, CLOSE, COMMIT | the handle kept through the CLOSE (another run unit's `NO OTHER` OPEN '61' until the COMMIT), released by the COMMIT |
| R2-1 a transaction begun beside another's append, undone | F = C1 C2; the first: OPEN EXTEND, WRITE X1, CLOSE, ROLLBACK; the second, covered, at every point of the first: OPEN I-O, READ, REWRITE BY1, CLOSE, left open, and after the first's program ROLLBACK · dies · OPEN EXTEND, WRITE B2, ROLLBACK or COMMIT | the second's journal has no L until its own first append, so after the first's ROLLBACK its rollback restores C1 and truncates nothing — C1 C2, never C1 C2 and zero bytes (the first text's L = 3, read at a begin between the first's append and its ROLLBACK: the mutation `R2-1`'s first witness, a point only the statement boundary reaches, R3-1); its own append after the first's ROLLBACK writes L = 2 and is undone to C1 C2, committed to BY1 C2 B2 |
| R2-1 a transaction begun beside another's append, committed | F = C1 C2; the first: OPEN EXTEND, WRITE X1, CLOSE, COMMIT; the second as above | after the first's COMMIT the second's rollback cuts nothing of X1, even begun between the first's begin and its write (the first text's L = 2 would cut it: the in-flight twin, the mutation's second witness) — C1 C2 X1 after its rollback, BY1 C2 X1 after its commit |
| every row × the COMMIT's regions | F synced · directory synced · `D C` written · `D C` synced · the retirement's deletes | region 1: unsuccessful, F at the previous COMMIT after item 8's rollback; region 2: success, the retirement completed by the next entry or the holder's own; region 3: reversed and rolled back, or marked and settled by the next entry — never a third state |
| every row × the second run unit | append · rewrite of ordinal 0 · rewrite of ordinal 1 · truncate · DELETE FILE · read; plain, and covered with COMMIT, with ROLLBACK, and left open across the rest of the first's program and then committed, rolled back, killed, or appending and then committed or rolled back; at every medium operation and every statement boundary of the first | '51' to a WRITE while any transaction lives; '51' to a REWRITE of a held ordinal and '00' to one below L the first did not touch, which the first's ROLLBACK leaves (I2); '61' to an OPEN OUTPUT, a DELETE FILE and every writable OPEN of a REPLACED file; a read sees F as it stands; the first run unit's death after the second's statement is settled before the second's next change |
| a journal this process may not read | B's WRITE entry · B's OPEN EXTEND | '51' / '61', never a silent proceed (the `IHostMedium` rows; the model has no permissions) |
| a lock directory this process may not list | a writable OPEN · a covered DELETE FILE | '37', F unchanged (`unit:SequentialCommitControlTests.A_lock_directory_this_process_may_not_list_is_the_authority_an_open_or_delete_file_lacks`) |
| a lock directory the process may not create | the first covered change | '30' at that statement (R1 refused), F unchanged |

**2.2.2.13 What the design deletes, keeps and measures.** Deleted: the refusal of the sequential operand of an APPLY COMMIT clause, COBOL1709 (retired, never reallocated), with `FileModel.IsCommitControlled` and `ApplyCommitModel.CommitControlledFiles`, which only narrowed the clause to its keyed files: every file the clause names is under commit control.
Kept, unchanged in contract: every verb arm of `SequentialConnector`, `OverwriteInPlace` as the one in-place overwrite,
`ReleaseRecord` as the one release, `SharedAppendStream`, the region map (one new byte kind, `EntryMutexByte`, in 2.2.1.8's
map, expressed by `SidecarRegionLocks` on every host), `SidecarLockFiles`' holder, probe and retirement rules (the holder file
is one of its holders). Added: the journal (`IO/Sharing/SequentialJournal.cs`: the three files, the drawing of the id, the
five rules, the settle), the kept handle under commit control, the entry step, the lock test's journal arm, the `IHostMedium`
seam and its simulator, and the model `scripts/models/sequential_journal_model.py`, which the implementer extends with every
row the implementation adds and keeps green at every change to the protocol. Measured without a clock, through the medium's
operation log: a covered WRITE adds zero journal operations after the transaction's first, which adds one append and one
sync (R3); a covered REWRITE in the base phase adds one append and one sync; a capture adds one copy of F, two syncs and one directory sync; a COMMIT adds one sync of F, one directory sync, one
write and one sync of the record, and the retirement's three deletes and directory sync; a writable OPEN of an uncovered file
adds one directory listing and nothing else, and its WRITE nothing under a posture that admits no other writer (under
`SHARING WITH ALL OTHER`, one hold of the entry mutex through its release, 2.2.2.3's uncovered change); a rollback of a transaction with m rewritten records and a appended ones writes
m records, one truncation when a > 0 and one sync, never a byte of another connector's record.

**2.2.2.14 The keyed organizations' two CAPTURE verbs, and their creation, ride the same journal (kb/Work PB3492;
IMPLEMENTED — `KeyedConnector.JournalCapture`/`JournalCreation`, `FileRegistry.DeleteCovered`/`CommitKeyed`/`RollbackKeyed`,
`KeyedStore.AdoptRestored`).** 2.2.2.2's (c) and (d) are not sequential verbs: an OPEN OUTPUT of an EXISTING
relative or indexed file (§14.9.27.4 GR18: *"After the creation of the file, the file contains no records"*) and a DELETE FILE
of one (§14.9.10 format 2) replace or remove the whole physical file, and 2.2.1.10's transaction cannot undo either, because
its before-images live IN the file the verb replaces, and §14.9.36.4 GR3 restores the file *"to the state they were in at the
execution of the last COMMIT statement"* (the creation's `Recreate` ends the kept state when it lands, above, so the store
alone has nothing left to roll back); and a DELETE FILE after the CLOSE must not read its own kept handle as another
holder's, where the NOTE under §14.9.10.4 GR15 says *"Such files can still be deleted, however if they are, then in the event of a
rollback they will be restored to the state they were in at the last commit or, if none, the start of the run unit"* and GR15's
own conflict is a file *"locked by another file connector"*, which the connector's own kept handle is not. **The NOTE settles
the question kb/Work PB3492 put to the owner** (refuse, roll back and delete, or delete and keep the transaction): the delete
proceeds and the transaction stands, so a ROLLBACK restores the file and a COMMIT keeps it deleted. No owner decision is
needed. **The shape:** both verbs CAPTURE exactly as R4 orders it for a sequential file: under the entry mutex the journal is
begun (START `P`, `SequentialJournal.OfKeyedFile` set in the owner's memory), the physical file's bytes are copied — through
the OPEN OUTPUT's own read-write handle (`KeyedConnector.JournalCapture`), or, for a DELETE FILE, through a short-lived read
handle once the kept one is given back (`FileRegistry.DeleteCovered`, the sequential file's code) — into `txn-<id>.before`
and synced, the lock directory synced, the `K` record (`O` or `D`, the length) appended and synced, and only then the keyed
creation (`Attach`, `Recreate`) runs or the file is removed. The kept state ends with the creation as before (its handle, its
liveness byte, its locks), but its transaction does not: the before file holds the file AS IT STOOD, the run unit's
uncommitted changes and their UNDO pages included. **ROLLBACK** (`FileRegistry.RollbackKeyed`, under the entry mutex)
restores the before file (2.2.2.7 step 1) through a writable handle the run unit already holds on the file (a host with
mandatory share modes refuses any other beside it, so a covered re-OPEN takes a read-write handle while a replaced journal
stands, `KeyedConnector.HandleAccess`), else one opened for the purpose; then, BEFORE the journal's `D R` (the
`Rollback(data, restored)` hook), every store of the run unit on the file adopts the restored file (`KeyedStore.AdoptRestored`:
its in-memory transaction forgotten, the file adopted again through a writable handle) and runs 2.2.1.10's DEAD-OWNER step
on it, which rolls back the captured transaction: its liveness byte went with the handle the verb gave back, so its owner
reads as dead, exactly as a dead run unit's does (a death before the `D R` leaves the journal open, and the next settle and
the next writable OPEN's dead-owner step do the same); a file no store of the run unit holds is adopted through a store made
for that alone. **COMMIT** (`FileRegistry.CommitKeyed`) first promotes the run unit's keyed transaction in the file as it
stands now — a transaction that lived in a file the capture removed ended with the removal (`KeyedStore.ForgetTransaction`,
run when the DELETE FILE's removal lands, `RemovalGiveBack.Removed`), and a store left with no writable file is REFUSED, never
forgotten (branch review R1-1: a store without its handle is not a removed file) — and only then decides the journal (2.2.2.6), so a death between the two leaves the journal
open and the next entry restores the last COMMIT; the decision `D C` makes the replacement permanent, the captured
transaction going with the before file. The keyed transaction and the journal are ONE holder of `FileRegistry.TransactionHolders`
(the journal's), in the journal's place in the order of the open files. **A CREATION** (2.2.2.2 (e): an OPEN OUTPUT of an
absent relative or indexed file, an OPEN I-O or EXTEND of an absent OPTIONAL one) begins the journal with START `A` before the
handle creates the file (`KeyedConnector.JournalCreation`, R1), so its ROLLBACK removes the file, every handle of the run unit
on it given back first (`FileRegistry.KeyedJournalData.Release`) — **a connector still OPEN on it included** (branch review
R1-2: §14.9.36.4 GR3 restores the state that had no file, with no implementor latitude, and leaves the open mode as it is, the
sequential organization's rule of 2.2.2.7). The open connector's keyed transaction is rolled back, then it leaves the file
(`KeyedConnector.LeaveTheRemovedFile`: the store left, the handle given back, the Table 19 column suspended,
`FileConnector.SuspendRunUnitFileLock`) and stays in its open mode on no file, holding the absent OPTIONAL file's placeholder
store: a READ or START answers as for an absent OPTIONAL file ('10', '23'), a REWRITE or DELETE finds no record, and its
physical-file state stops claiming the removed file (`PhysicalFileTable.ForgetFile`). Its next WRITE, at its last check (so a
WRITE the empty file refuses creates nothing), creates the file again as its OPEN did — under the entry mutex, a new START `A`
journal, the creating handle, the empty store, the column resumed (`KeyedConnector.WriteCreatingTheFileAgain`) — and runs
inside its store's statement; a file another run unit made at the path meanwhile is not the WRITE's to replace ('30'). A run
unit that writes nothing more leaves no file (its normal end commits nothing and its implicit CLOSE keeps nothing). The
sequential twin gives its column back at the removal too (`SequentialConnector`'s `IJournalData.Release`, resumed by
`BeginAppending`): a SHARING WITH ALL OTHER writer's ROLLBACK was refused '30' on Windows, where the column is a handle on the
file (witness `unit:SequentialCommitControlTests`' `A_writer_under_all_other_whose_file_a_rollback_removed_creates_it_again_at_its_next_write`).
**An unsuccessful OPEN** that began the journal and did not affect the file — refused before its handle
created the file, a capture refused before its `K`, a creation refused and undone (`KeyedStore.FileReplaced` false) —
WITHDRAWS it (`SequentialJournal.Withdraw`: `D R` and the retirement; §14.9.27.4 GR25, *"the file is not affected"*), so the
kept transaction stands as the CLOSE left it; one that did affect it leaves the journal for item 8's ROLLBACK. **The entry
step of 2.2.2.8** runs at every writable OPEN of either organization (`FileConnector.EnterPhysicalFile`, one listing) and at
every DELETE FILE, a covered one's and an uncovered one's (`FileRegistry.JournalHoldsAgainstDelete`), so a dead owner's
capture is settled before 2.2.1.10's dead-owner step reads the restored store and before an uncovered DELETE FILE removes a
file a later settle would otherwise bring back. **The removal gives the Table 19 file lock back with the kept handle**
(`GiveBackForRemoval`, both organizations): where the host publishes the column through a handle on the file
(`HandleColumnLockHost`, a covered connector under `SHARING WITH READ ONLY` or `ALL OTHER`, or a keyed INPUT re-OPEN's READ ONLY
default) that handle refused the removal, and from the removal on the journal's (ALL) holding is the lock. **What the DELETE
FILE gives back it TAKES BACK unless the removal lands** (`FileConnector.GiveBackForRemoval` returns a `RemovalGiveBack`; branch
review R1-1): the kept handle goes for each attempt's probe of GR15's conflict and the file lock is suspended
(`RunUnitFileLock.Suspend`) just before the capture, and an attempt answered '62', GR14's '05', or a capture or removal the
medium refuses ('30', '37') retakes the handle — for a keyed file the transaction's liveness byte with it — and resumes the
file lock, under the entry mutex, so an unsuccessful DELETE FILE leaves the file and the transaction on it as the CLOSE left
them: the COMMIT promotes it and no dead-owner step reads it as a dead one's (witnesses `unit:CommitRollbackRuntimeTests`'
`A_refused_delete_file_after_a_close_leaves_the_transaction_for_the_commit`, `unit:SequentialCommitControlTests`'
`A_refused_delete_file_after_a_close_keeps_the_file_lock_until_the_commit`). Witnesses:
`conformance:cases/14.9.36-ROLLBACK/pb3492_keyed_file_replaced_or_removed` and `unit:CommitRollbackRuntimeTests`'
`A_keyed_capture_or_creation_refused_at_every_journal_operation_leaves_the_last_commit_or_the_committed_change` (every
operation of the journal's medium refused), `A_delete_file_after_a_close_is_not_refused_by_the_kept_handle_and_holds_the_file_until_the_rollback`,
`A_dead_run_units_keyed_capture_is_settled_by_the_next_entry` and `A_keyed_file_created_since_the_last_commit_is_removed_by_the_rollback`;
a death and a power loss at every journal operation are `unit:SequentialJournalTests`' sweep of the same `Capture`, `Rollback`
and `Settle`. The journal's types keep their first client's names (`SequentialJournal`, `SequentialTransactions`). **One primitive the design assumes and the runtime lacks:** a
DIRECTORY SYNC (2.2.2.5 R1 and R4, 2.2.2.6 (1) and (3), 2.2.2.7 (2)). .NET has no API for it: on Linux it is `open(dir,
O_RDONLY)` and `fsync` (the two-argument `open` is safe to declare, since the mode argument is read only with `O_CREAT`), on
macOS `fsync` does not reach the medium and `F_FULLFSYNC` goes through the variadic `fcntl` that `SidecarLockFiles` avoids for
Apple's arm64 ABI, and on Windows a directory's entries are journaled by NTFS and a directory handle opened with
`FILE_FLAG_BACKUP_SEMANTICS` can be flushed. The `IHostMedium` seam owns it, as one role with its determination per host
recorded in docs/CONFORMANCE.md beside DOC-A.1-75.

#### 2.2.3 The sequential frame index — bounded memory for the backward READ and a count that stores nothing (kb/Work PB3350)

⛔ **STATUS.** IMPLEMENTED (kb/Work PB3350, in the SORT and file I/O campaign PB3353; designed 2026-10-19 by the Mythos lane,
PB3426) in `IO/FrameIndex.cs`, `RecordFraming.CountFrames` and `SequentialConnector._frameIndex`. It replaced
`RecordFraming.FrameStarts`' `List<long>` — one offset per frame of a RECORD VARYING file, kept for the life of the OPEN by
`SequentialConnector._varyingStarts` and built in full only to be counted by `CountRecords` — with one walk that yields a
COUNT and a SPARSE index, so no consumer holds O(records) memory. The members: `FrameIndex.Observe` (the forward walk feeds
it), `Extend` (the counting walk, which is also the REVERSED count: `CountRecords(0, long.MaxValue, index)`) and `TryStartOf`
(a start, from the checkpoints and the window).

**The two consumers and the one walk.** `ReadStreamFrame` stays the one decoder and `StreamMedium` the one walk (its
snapshot of the length, PB2754, unchanged). (1) **The count** — `CountRecords`' VARYING arm, the OPEN EXTEND base and the
REVERSED count — is `RecordFraming.CountFrames(stream, from, to)`: the same loop, returning the number of frames that start
in [from, to) and storing none of them. (2) **The backward READ** (§14.9.30.4 GR21 rule c)'s decreasing arm, and OPEN INPUT
REVERSED's standing reversal) asks `SeekToOrdinal` for the START of ordinal n, and that start comes from a `FrameIndex` — a
checkpoint list holding the start of every S-th frame (S = 4096; one `long` per 4096 records: 2 KB per million, under 2 MB
per billion) and a WINDOW of the exact starts of the checkpoint span the connector is currently reading backward through (at
most S longs, 32 KB, rebuilt by one forward walk of ≤ S frame heads from the span's checkpoint when the position leaves the
span). `SeekToRecord` stays the ONE reposition (kb/Work PB352); the index answers only WHERE a frame starts.

**How the index is built, and why it is never stale within an OPEN.** The forward walk the connector already makes
(`NextFrame`) knows every frame's start as it passes it, so the index grows for free during forward reads: `Observe(start,
end)` takes a frame in when it begins exactly where the walked frames end (a BYTE-OFFSET contiguity test, not the connector's
`_readOrdinal`: START FIRST/LAST walks frames without advancing the ordinal, and a re-read after a backward step is already
known), and every S-th such frame's start is appended to the checkpoint list. A backward read whose target lies beyond the
frames walked so far — OPEN REVERSED's first READ, which targets the last record of a file nothing has read — extends the
checkpoints by ONE counting walk from where the walked frames end (`FrameIndex.Extend`), the same call that measures the
REVERSED count (`CountRecords(0, long.MaxValue, index)` returns both, so OPEN REVERSED pays one pass, where `CountRecords`
plus `FrameStarts` paid two). The layout cannot change while the connector is open — §14.9.35.4 GR16 makes a record
sequential REWRITE size-preserving, and a sibling's appends (ALL OTHER) lie past every indexed frame and are walked when a
target reaches them — so one index per OPEN is right; `OpenCore` replaces it. **A host read that throws mid-walk leaves
the index consistent:** `Extend` records a frame only after the decoder has returned it whole (the walk resumes at the last
whole frame), and `BuildWindow` invalidates the window (span −1, count 0) BEFORE it overwrites any slot, so an
`IOException` (status 30) never leaves the old span claimed over the new span's starts.

**Cost, stated so a witness can measure it without a clock.** A single backward read costs at most S frame-head reads (one
window build) and then one seek; a whole-file backward pass — the COBOL-85 REVERSED read of every record — costs one window
build per span, S heads each, so O(records) head reads in total, the same order as today's one full walk and the forward
read; memory is O(records / S + S). Rejected: a trailing length word after each payload (O(1) backward steps, but a FORMAT
change — every reader of the stream framing, the sort store's and `CobolSort`'s included, changes with it, and a file written
by the current format is misread by the new one; the framing is the interchange property of 2.2); walking from 0 at every
backward step (O(1) memory, but a backward pass of a file is then quadratic in its records); keeping the whole index but
building it lazily (what exists today; the memory is the defect).

**The witnesses.** `FrameIndexTests` (Unit): over a generated file of 2^20 frames the count allocates no per-frame storage
(the allocation delta of `CountFrames` is bounded by a constant), the index's checkpoint list holds ⌈frames / S⌉ entries and
the window at most S; every ordinal's start agrees with the file at strides 1 to 64, asked forward, backward and scattered;
a step inside a span costs no frame-head read and a step into another costs one window build (a stream that counts the
payload seeks counts the heads read); frames a sibling appended after the walk are found when a target reaches them; and a
structural scan finds no `List<long>` in the sequential I/O layer but the index's own checkpoint list. The goldens, which
cross the span boundary with a 4100-record RECORD VARYING file: `14.9.30-READ/pb3350_read_previous_varying_span` (GR21 c
and e, the forward walk's checkpoints) and `_unassigned/pb3350_open_reversed_varying_span_85` (the counting walk's); the
existing PREVIOUS and REVERSED cases stay byte-identical.

### 2.3 Values — the value library (numeric + text + tables), role-grouped
```
Values/                         namespace WiseOwl.Cobol.Runtime.Values
  Numeric/  CobolNum, CobolDec, CobolFloat, CobolEdit, CobolRounding, CobolSizeError, NumProfile, Pow10 (NEW)
  Text/     CobolString, CobolBool, CobolClass
  Tables/   CobolTable, CobolDynTable
```
`Pow10` (NEW, `Values/Numeric/Pow10.cs`) — the ONE power-of-ten source:
```csharp
internal static class Pow10 {
    private static readonly long[]   L = BuildLong();   // 10^0..10^18
    private static readonly Int128[] W = BuildWide();   // 10^0..10^38
    private static readonly Int128[] F = BuildFive();   // 5^0..5^54 — 10^n's ODD COFACTOR (kb/Work PB623)
    public static long   AsLong(int n) => L[n];         // callers already bound n ≤ 18 / ≤ 38 by design
    public static Int128 AsWide(int n) => W[n];
    public static Int128 FiveAsWide(int n) => F[n];     // the exact binary64 expansion's multiplier
}
```
`CobolNum.Pow10/Pow10Wide`, `CobolDec.Pow10`, `CobolDate.Pow10`, `CobolIntrinsics.Pow10I/Pow10D` all delegate to it,
then are deleted. Pure win, no behavior change (identical values, table-driven).

⛔ **There is deliberately NO `double` view (kb/Work PB623, 2026-09-05).** The 10^n-as-double table existed for
exactly one purpose — scaling a binary64 before landing it in a fixed-point receiver — and that multiply was the
defect: past 2^53 the product is itself rounded, so the landing answered with a value the sender never held, and a
negative scale fell out of the table into a loop that returns 1 (a trailing-P receiver stored 10^|scale| too much).
`CobolFloat.TryExactScaled` now lands the EXACT expansion using 5^scale and a shift, which is why the five table
replaced the double one rather than joining it. A future caller wanting 10^n as a double is either converting a
scaled value — `CobolFloat.ScaledToDouble`, whose own exact-power table stops at 10^22 for the same reason — or
reintroducing PB623.

The numeric value types (`CobolNum` scaled long/Int128 kernel · `CobolDec` decimal128 · `CobolFloat` float/double ·
`CobolEdit` PIC editing) are **coherent as-is** — keep the type set and their internal design; only the folder path and
the `Pow10` dedup change. (The compiler-side "force everything through Int128 even when it fits in `long`" concern is a
*NumericRenderer/emitter* issue, out of scope for this dimension.)

### 2.4 Verbs — the statement runtime (moved out of the mislabeled `Strings/`)
```
Verbs/                          namespace WiseOwl.Cobol.Runtime.Verbs
  CobolStringOps  (STRING / UNSTRING)
  CobolInspect    (INSPECT)
```
These implement *statements*, not elementary values — role-grouped away from the `Values/Text/` value types they were
arbitrarily filed beside. (`CobolSort`/`ReportWriter` are verb-runtime too but stay in `IO/` because they are file/
report-connector-bound; noted, not moved.)

### 2.5 Control — run-unit + inter-program
```
Control/                        namespace WiseOwl.Cobol.Runtime.Control
  RunUnit (NEW) · ProgramTable (was ProgramRegistry) · ExternalStore · ModuleStack (was CobolModule)
  ManagedPointer / StorageCell / CellPointer · CobolArg / ICobolProgram / CobolArgAdapt
  CobolObject · CobolPtr · ExternalSwitches · CobolInvokeArg
  RuntimeAbi (the runtime version and call ABI a compiled module records and RegisterModule checks; kb/Work PB2097)
  Signals/  StopRun · ProgramReturn · MethodReturn · ResumeSignal · NotImplemented
Repository/                     namespace WiseOwl.Cobol.Runtime.Repository
  CobolRepositoryAttribute (a module's assembly record: schema, runtime version, call ABI, registrar) — the attribute
  family of the external repository's schema (DESIGN-external-repository §6) grows here
```
A module joins a run unit only through its `__CobolModule.EnsureRegistered()` → `ProgramTable.RegisterModule`, the one
writer of the run unit's registration set (DESIGN-external-repository §11.2). The runtime's public surface is recorded
in `PublicAPI.Shipped.txt` (`Microsoft.CodeAnalysis.PublicApiAnalyzers`): a new public member is listed in
`PublicAPI.Unshipped.txt` in the same change, and a removal or incompatible change raises the major version.
The standalone executable's generated `Main` registers its own module INSIDE the run-unit boundary: it calls
`ProgramRegistry.RunModule(__CobolModule.EnsureRegistered, mainPath)` (`ProgramTable.RunModule`, kb/Work PB2846), which
runs the registration and then the main program under the §14.6.12 abnormal-termination surface and the §14.6.11
epilogue, so a registration the run unit refuses (another runtime major or call ABI, or an already-registered
outermost name) ends the run unit through that surface and never escapes as an unhandled .NET exception. A .NET host
enters through a program's generated host entry (`Cobol.<assembly>.<NAME>.Call`, DESIGN-external-repository §4.3, the
one host surface of §4.4), which registers the program's module and activates it at the outermost boundary;
`ProgramTable.RunMain(path)`, the same boundary as `RunModule` without the registration, is shipped surface no route calls.
**The process's own state belongs to the entry that IS the process (kb/Work PB3163, PB3193).** `RunModule` and the
`cobol` CLI's `Main` are the only writers of the process exit code and the only callers of
`IO.StandardStreams.EnsureUtf8` (UTF-8 standard output and error, and a UTF-8 reader over a redirected standard input,
DOC-A.1-59 and DOC-A.1-1); `RunMain`, `RunUnit.Run`, a host entry's activation and a CALL into a module write neither, so a run unit a host
embeds DISPLAYs through the host's `Console.Out` / `Console.Error` as the host configured them and leaves the host's
exit code and console as it found them. `ProcessStateOwnerDriftTests` (Unit) holds each process-owned write to its
owner; `RunUnitHostExitCodeTests` and `RunUnitHostConsoleTests` (Conformance) are the host-side witnesses.
`ProgramRegistry` → **`ProgramTable`** (an instance owned by `RunUnit`; the name "Registry" is reserved for the
process-level nothing-here). The name-resolution / state-model / CANCEL / sibling-module-probe logic is ported
verbatim onto the instance. `ExternalStore` and `CobolModule`→`ModuleStack` move off statics onto `RunUnit`. The CALL-
ABI types (`ManagedPointer`, `CobolArg`, `ICobolProgram`, `CobolArgAdapt`, `StorageCell`) currently live *inside*
`ProgramRegistry.cs` (700 lines, one file) — split into their own files under `Control/`.

### 2.6 Exceptions — EC engine (instance-owned)
```
Exceptions/                     namespace WiseOwl.Cobol.Runtime.Exceptions
  ExceptionState (instance on RunUnit) · ExceptionCatalog (static, immutable table — stays static) · EcFunctions
  Signals moved to Control/Signals/ (CobolFatalException stays here — it is an EC condition carrier)
```
`ExceptionState` becomes an instance (all its `static … { get; private set; }` → instance members; the ambient gates
`ArgumentFunctionChecking`/`DataConversionChecking` become instance flags). `ExceptionCatalog` is an immutable lookup
table — legitimately static, kept. `EcFunctions` reads `RunUnit.Current.Exceptions`.

### 2.7 Intrinsics — keep the family split; only the `Pow10` dedup + clock change
`Intrinsics/CobolIntrinsics.{cs,Exact,Float,Text}` + `CobolDate` are cohesive (the deep-dive's runtime home). No
reorg beyond: `Pow10*` → `Values/Numeric/Pow10`; `CobolDate`'s `AcceptSource.Now` dependency → `RunUnit.Current.Clock`.

**The clock seam carries the offset (R21, 2026-08-08):** `IClock.Now()` returns **`DateTimeOffset`** — the local
wall-clock reading WITH the local time differential factor CURRENT-DATE renders at positions 17–21 (§15.21.3 r1)
and the §15.3.3 offset format fields emit. EVERY now-reading in the greenfield tree goes through the seam —
the ACCEPT temporal sources, CURRENT-DATE, FORMATTED-CURRENT-DATE, SECONDS-PAST-MIDNIGHT and the §15.100
windowing execution-time defaults — so one run unit observes one clock and `WISEOWL_COBOL_CLOCK` pins all of them
at once (a pin may carry an explicit offset, `2026-06-10T14:30:45.67+02:30`; without one the machine-local
offset is assumed). The only direct `DateTime[Offset].Now` reads are `SystemClock`'s unpinned fallback and
`IntrinsicBinder.CompileClock`'s default (WHEN-COMPILED is the compilation timestamp, §15.99.3 r2);
`ClockSeamDriftTests` is the source-form guard that keeps it that way.

**THE §15.3 INTEGER-ARGUMENT INTAKE IS A CONTRACT WITH THE RENDERER, AND THE BODY'S PARAMETER TYPE IS ITS ONLY
STATEMENT (kb/Work PB22 + PB65 + PB254).** A §15 integer argument reaches a runtime body through exactly one of
two intakes, and which one is not a property of the renderer arm — it is a property of the ARGUMENT's own rule:

- **BOUNDED** — the function definition constrains the argument's VALUE (§15.5.2's integer date form for
  `INTEGER-OF-DATE`/`DAY-OF-INTEGER`/…, §15.23.3 r1's "less than 1 000 000", §15.36.3 r1's "greater than or
  equal to zero" together with a codomain `Int128` cannot hold). A value the body cannot represent then really
  "results in an incorrect value for that argument or for the returned value according to the rules specified in
  the function definition" (§15.3 rule 14), so the argument crosses `CobolIntrinsics.IntegerArg`, the ONE
  narrowing, which RAISES EC-ARGUMENT-FUNCTION instead of wrapping. **The body's parameter is `long`.**
- **TOTAL** — the definition constrains nothing but integer-ness, so the returned-value rule answers for every
  integer and §15.3 rule 14 has nothing to fire on. Putting the narrowing in front of such a body manufactures
  an exception condition the standard does not define: fatal under checking, and with checking off it
  substitutes 0 *for the argument*, which is how `TEST-DATE-YYYYMMDD(1.0E19)` once answered "the date is
  valid" (§15.90.4 r1d) and `FIND-STRING(… START AFTER 9999999999999999999)` once answered a match position
  where §15.37.4 r3 requires zero. **The body's parameter is `Int128`, and the renderer's intake is the wide
  one** (`IntrinsicRenderer.IntArgWide` / `ArgIntWide` → `AsIntWide`, which has no raise point). Members today:
  BOOLEAN-OF-INTEGER argument-1 (§15.13.3 r1), TEST-DATE-YYYYMMDD and TEST-DAY-YYYYDDD argument-1 (§15.90.3 r1
  / §15.91.3 r1), FIND-STRING argument-3 (§15.37.3 r3) and RANDOM's seed (§15.75.3 r2, PB636); the guard pairs a `…Dec` body
  with the arm of the same name minus the suffix.
  ⛔ **A TOTAL argument whose value rides the SDIDI takes NO native carrier at all (kb/Work PB637):** ANNUITY's
  argument-2 (§15.9.3 r3 "a positive integer" — a sign rule). An SDIDI integer holds 34 significant digits at any
  exponent, so it reaches past `Int128` as well as `long`; the SDIDI arm keeps the position's unbounded SDIDI
  integer (`DecArg` over `RawArg`) and `AnnuityDec(CobolRounding, CobolDec, CobolDec)` takes it as a `CobolDec`
  (an `Int128` intake ended the run unit on `ANNUITY(0, 10 ** 40)`, branch review R1-1).
  `IntrinsicCarrierAgreementDriftTests.IntegerArgumentsThatRideTheSdidi_TakeNoNativeIntake` holds that closed set.
  At rate 0 the equivalent expression is `1 / argument-2` (§15.9.4 r1a), exact for every n; at a positive
  rate an enormous n leaves `(1 + rate) ** n` outside the decimal128 range, which is the EXPRESSION'S size condition
  (EC-SIZE-OVERFLOW, §8.8.1.5.2 r2), the same at n = 10^6 and n = 10^20 — never an EC-ARGUMENT-FUNCTION that
  depends on whether n fits a native integer.

⚠ **The membership is not a maintained list anywhere.** Declaring the body's carrier IS the claim, and
`IntrinsicCarrierAgreementDriftTests.EveryTotalIntegerArgument_TakesTheWideIntake` re-derives the pairing by
reflection over the shipped signatures and fails when an arm and its body disagree — in either direction. A new
total function therefore needs one act, widening its body; forgetting its renderer arm is red, not silent.
⛔ **A type-6 position has NO `…Real` twin: the renderer takes the operand's INTEGER first (kb/Work PB651).**
Since kb/Work PB248 a floating-point operand at a §15.3 type-6 position is REFUSED under strict (COBOL1627 —
type 6 admits "an integer data item or an always-integral arithmetic expression", and a floating-point item is
neither), so it arrives only under `--permissive`, and docs/CONFORMANCE.md DOC-A.1-124 fixes what it then means:
the integer it holds, or the truncated integer with EC-ARGUMENT-FUNCTION checking off. That is a property of the
POSITION, so `IntrinsicRenderer.RawArg` — the one indexed argument entry every intake and lane predicate reads —
takes it there, before a lane is chosen, on every carrier, keeping the VALUE or saying it cannot. An SDIDI operand
STAYS on the SDIDI (`CobolIntrinsics.IntegerArgUnboundedDec`: the integrality test, then `CobolDec.TruncateToInteger`),
because an SDIDI holds 34 significant digits at any exponent and its integer reaches far past `Int128` —
MOD(10 ** 40, 3) and MOD(A ** 3, B) of a 15-digit A are integers the SDIDI body evaluates (kb/Work PB2630, PB69),
and landing them on `Int128` was EC-SIZE-OVERFLOW. A fixed-point operand's integer fits the `Int128` its unscaled
value rides, and a float past it (±1.7e38, ±∞ included) raises EC-ARGUMENT-FUNCTION through
`CobolIntrinsics.IntegerArgWideReal`. ⛔ The position intake never CLAMPS: a clamp
answers for a different integer, so `MOD(1.0E39, 7)` answered 1 where the operand's integer answers 5 (PB651 review
R1-2). Clamping is right only where the function answers the same for every integer past a bound — TEST-DATE-YYYYMMDD
/ TEST-DAY-YYYYDDD argument-1 (the r1a catch-all) and FIND-STRING argument-3 (0 once every match is ignored) — and
those arms alone take `IntrinsicRenderer.IntArgWideSaturating` (`CobolIntrinsics.IntegerArgSaturatingReal`);
`IntrinsicCarrierAgreementDriftTests.OnlyAnArgumentWhoseAnswerIsConstantPastABound_Saturates` keeps the set closed. An integer has no binary64-ness, so a float at a type-6 position never moves a call to the
binary64 lane: the integer-only functions (MOD, FACTORIAL, the date family, the TEST- validators, the windowing
trio) have no `…Real` body, and `IntrinsicRealArgDriftTests.NoRealBody_ServesAFunctionWhoseEveryPositionIsType6`
keeps it so. A mixed row keeps its twin for its type-10 position (`CombinedDatetimeReal`, reached by a float
argument-2). The lane is witnessed over every verified type-6 position by
`IntrinsicIntegerPositionPermissiveDriftTests` and in `FloatIntegerArgumentPermissiveTests`, the coercion lane's home.

⛔ **WHICH bodies owe a `…Real` twin is likewise derived, and FIND-STRING owes none (kb/Work PB635).** The
binary64 lane renders every argument through one `double` conversion, so it is enterable only for a call whose
whole §15.3 argument run is numeric (types 6 and 10) — `IntrinsicArgumentRules.ArgumentRunIsAllNumeric`, the
predicate `IntrinsicRenderer.RenderNum`'s float dispatch now tests. A function with a string, boolean or
reference position renders on its own arm whatever its operands are, so `FindStringReal`, `OrdReal`,
`TestNumvalReal` and `IntegerOfBooleanReal` must not be written; they were all four emitted, and all four failed
Roslyn with CS0117, while the dispatch asked only whether SOME argument was floating.
`IntrinsicRealArgDriftTests` scopes itself on that same predicate rather than on the old
"'s' and 'b' reject a float at bind time" reasoning, which held under strict conformance only —
`CheckArgumentClasses` warns and binds under `--permissive`, which is exactly the lane these bodies live in.

### 2.8 Namespace / assembly rename (G8)
Assembly is already `WiseOwl.Cobol.Runtime`. At G8, `RootNamespace` `WiseOwl.Cobol.Runtime` → **`WiseOwl.Cobol.Runtime`** and the
sub-namespaces above become real. This is a single coordinated change with the compiler's `RuntimeApi` façade (one
file emits the `using`s), so generated `using WiseOwl.Cobol.Runtime;` flips to `using WiseOwl.Cobol.Runtime.*;` in exactly one
place. Until G8 the root namespace stays `WiseOwl.Cobol.Runtime` and the reorg below is **folder-only** (namespaces
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
| rename (G8) | RootNamespace `WiseOwl.Cobol.Runtime` | `WiseOwl.Cobol.Runtime` (+ real sub-namespaces) | assembly/namespace coherence; one coordinated flip with the emitter `RuntimeApi` façade |
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
   `ProgramRegistry.cs` into per-type files, group `Control/Signals/`. Namespaces stay `WiseOwl.Cobol.Runtime[.X]` exactly
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
   `RootNamespace` to `WiseOwl.Cobol.Runtime` and realize the sub-namespaces; update the façade's emitted `using`s in one
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
3. **Static-facade lifetime — OWNER-RATIFIED (2026-07-16):** kept pre-G8 as the emitted surface (the
   byte-stability proof discipline — NOT back-compat; nothing has shipped); the facades RETIRE at **G8 Cut 3**
   together with the namespace flip, which already forces an emitted-surface change (one re-baseline instead of
   two). G8 design input recorded with the decision: the replacement must resolve the AsyncLocal-read cost —
   the generated run-unit driver should CAPTURE the run unit once (`var ru = RunUnit.Current;`) and route
   statement-level calls through the captured local (or an equivalent cached path), never a per-statement
   `RunUnit.Current` read on hot paths.
4. **Namespace granularity:** deferred to G8 with the flip itself (§2.8's sub-namespaced layout remains the
   plan of record; the folders already mirror it).
5. **`Values/` nesting:** ACCEPTED — `Values/{Numeric,Text,Tables}/` landed.
6. **Sequential connector split:** KEPT UNIFIED (line- + record-sequential + LINAGE in one
   `SequentialConnector`) — the shared position/framing state makes a split net-negative.
