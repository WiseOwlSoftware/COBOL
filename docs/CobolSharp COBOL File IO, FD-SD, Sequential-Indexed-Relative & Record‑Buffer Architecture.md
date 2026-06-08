CobolSharp COBOL File I/O, FD/SD, Sequential/Indexed/Relative & Record‑Buffer Architecture (CIL‑Only)
====================================================================================================

> **STATUS BANNER.** Authoritative design reference for the COBOL file-I/O subsystem (FD/SD, SEQUENTIAL/
> RELATIVE/INDEXED organizations, OPEN/CLOSE/READ/WRITE/REWRITE/DELETE/START, file-status, keys, locking).
> **Implementation status: ~90% implemented** — the runtime lives under `src/CobolSharp.Runtime/IO/`
> (`CobolFileManager`, `IFileHandler`, `SequentialFileHandler`, `IndexedFileHandler`, `RelativeFileHandler`,
> `FileStatus`) with the compiler side in `src/CobolSharp.Compiler/Semantics/Bound/Binding/FileIoBinder.cs`,
> `…/CodeGen/Lowering/FileIoLowerer.cs`, `…/CodeGen/Emission/CilFileIoEmitter.cs`, plus
> `FileStatusValidator`/`FileStateValidator`. The NIST SQ (sequential), RL (relative), IX (indexed) and ST
> (sort) suites are baselined green. **Verify any specific claim below against `src/` before relying on it** —
> several details (status-code values, locking, B+-tree, WASM virtual FS) are *design intent*, not the shipped
> behaviour.
> **Stack: .NET 10 / C# 14.** **Backend: CIL-only via Mono.Cecil (no custom VM / no bytecode interpreter;**
> a Roslyn C# backend is a future additive option). Record buffers are currently **`byte[]` over the
> StorageBlock byte engine**, which is being *islanded* as the typed-native data model lands
> (`docs/DATA_MODEL_ARCHITECTURE.md`); file I/O is one of the classifier-scoped byte-image fallbacks and stays
> on the byte path. Plan SSOT: **`docs/MASTER_PLAN.md`**; doctrine: **`PROMPT.md`**.

Purpose
-------
Define the authoritative architecture for:
- FD and SD file descriptions
- File control blocks / file descriptors and runtime handles
- Record buffers, key buffers and StorageBlocks
- Sequential, Indexed, and Relative file organizations
- ACCESS MODE (SEQUENTIAL, RANDOM, DYNAMIC)
- OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START
- File status codes
- Key handling (primary, alternate, collation)
- Locking and sharing modes
- Exception routing (INVALID KEY, AT END, ON EXCEPTION) and declaratives
- Integration with the runtime FileManager
- AOT/WASM‑safe file I/O
- CIL‑friendly lowering
- Debugger integration

This document governs how CobolSharp implements COBOL file I/O semantics on .NET.

> **Implementation note on naming.** This doc refers variously to a `FileDescriptor`, a
> `FileControlBlock (FCB)`, and an `ExecutionContext.FileManager`. The shipped runtime uses a
> **`CobolFileManager`** that maps each COBOL file name to an **`IFileHandler`** (one implementation per
> organization). Treat "FCB"/"FileDescriptor"/"FileHandle" in this doc as the conceptual file-state object,
> realized concretely by the per-file `IFileHandler` instance plus its registration in `CobolFileManager`.

------------------------------------------------------------
SECTION 1 — FILEMANAGER OVERVIEW
------------------------------------------------------------

The runtime FileManager (shipped as `CobolFileManager`) provides:
- File open/close (`Open`, `Close`)
- Sequential read (`ReadNext`, `ReadPrevious`)
- Keyed/indexed read (`ReadByKey`, with key-of-reference index)
- Relative read (by relative record number)
- WRITE / WRITE-variable / REWRITE / DELETE
- START positioning (with key-of-reference index)
- File status population
- Locking and sharing (design)
- Record buffer management

All file operations are:
- Pure managed code
- Deterministic
- Cross-platform
- AOT/WASM‑safe

Each COBOL file is represented by:
- A FileControlEntry (from the ENVIRONMENT DIVISION FILE-CONTROL paragraph)
- A FileDescriptor / FCB (from the FD or SD)
- A RecordBuffer (StorageBlock, currently a `byte[]`)
- A FileHandle (runtime object — the registered `IFileHandler`)

------------------------------------------------------------
SECTION 2 — FD / SD ARCHITECTURE
------------------------------------------------------------

2.1 FD structure
----------------
FD defines:
- Record layout (group item)
- File organization
- Access mode
- Record length
- Keys (primary, alternate)
- Collation (for indexed files)

Example:

    FD MyFile.
      01 MyRecord.
         05 Field-A PIC X(10).
         05 Field-B PIC 9(5).

Compiler generates:
- A RecordBuffer StorageBlock for the record
- An offset table for fields
- PIC/USAGE metadata
- Key metadata

2.2 SD structure
----------------
SD is used for:
- SORT/MERGE temporary files
- Same structure as FD
- Managed by the SortEngine

------------------------------------------------------------
SECTION 3 — FILE ORGANIZATIONS
------------------------------------------------------------

CobolSharp supports the three standard COBOL file organizations, each mapping to a specific .NET storage
model and a dedicated `IFileHandler` implementation:

3.1 SEQUENTIAL (`SequentialFileHandler`)
----------------------------------------
- Records stored in order of writing; no random access.
- READ NEXT only; WRITE appends; REWRITE requires a prior READ and OPEN I-O.
- DELETE / START are not allowed (illegal).

Mapped to:
- .NET FileStream
- Line‑based or fixed‑length record encoding
- Supports variable-length records (RECORD IS VARYING … DEPENDING ON; see §6 and `IFileHandler.WriteVariable`/
  `LastRecordLength`/`MaxVaryingRecordSize`/`MinVaryingRecordSize`).

3.2 RELATIVE (`RelativeFileHandler`)
------------------------------------
- Records addressed by relative record number (RRN); holes (gaps) allowed.
- DELETE marks a record inactive; reading a deleted/missing record → INVALID KEY.

Mapped to:
- Structured binary file with fixed‑size slots
- Direct addressing via RRN (plus per-slot length prefix for variable-length relative records)

3.3 INDEXED (`IndexedFileHandler`)
----------------------------------
- Records stored with a primary key; optional alternate keys (WITH DUPLICATES allowed).
- Random access via key; READ NEXT/PREVIOUS supported.

Mapped to:
- An index + data-file pair. *(A B+‑tree optimized for sequential and random access is the design target;
  treat the concrete index structure as an implementation detail — verify vs `IndexedFileHandler`.)*

Operation matrix:

| Op            | SEQUENTIAL          | RELATIVE            | INDEXED                     |
|---------------|---------------------|---------------------|-----------------------------|
| OPEN modes    | INPUT/OUTPUT/I-O/EXTEND | INPUT/OUTPUT/I-O/EXTEND | INPUT/OUTPUT/I-O/EXTEND |
| READ NEXT     | yes                 | yes (DYNAMIC)       | yes                         |
| READ PREVIOUS | no (status 21)      | yes (DYNAMIC)       | yes                         |
| READ KEY/RRN  | n/a                 | by RRN              | by key (prime/alternate)    |
| WRITE         | append              | to RRN slot         | insert by key (dup → 22)    |
| REWRITE       | I-O only            | yes                 | yes (key must not change)   |
| DELETE        | illegal (92)        | mark slot deleted   | remove key + data           |
| START         | illegal (21)        | yes                 | yes (EQ/GT/GE/LT/LE)        |

------------------------------------------------------------
SECTION 4 — ACCESS MODES
------------------------------------------------------------

4.1 SEQUENTIAL — READ NEXT only.
4.2 RANDOM — READ/WRITE by key or RRN only.
4.3 DYNAMIC — both sequential and random allowed (enables READ NEXT/PREVIOUS interleaved with keyed access).

------------------------------------------------------------
SECTION 5 — OPEN/CLOSE SEMANTICS
------------------------------------------------------------

5.1 OPEN INPUT
- File must exist; position at first record; read‑only (WRITE/REWRITE/DELETE not allowed).

5.2 OPEN OUTPUT
- Create new file or truncate existing; position at beginning; WRITE allowed; READ not allowed.

5.3 OPEN I-O
- File must exist; READ/WRITE/REWRITE/DELETE allowed.

5.4 OPEN EXTEND
- Append to end of file; create file if missing (per OPTIONAL/mode rules); WRITE allowed; READ not allowed.

5.5 CLOSE
- Flush buffers; release locks; release file handles; update/reset file status.
- Implicit CLOSE of all open files at run-unit termination (NIST RL208A).

5.6 Lowering
- `call FileManager.Open(fileHandle, mode)` (then set file status).

------------------------------------------------------------
SECTION 6 — RECORD BUFFER ARCHITECTURE
------------------------------------------------------------

6.1 RecordBuffer
- Each FD has a StorageBlock for the record (currently a `byte[]` over the byte engine), an offset table for
  fields, and key metadata.
- Optional input buffer / output buffer / key buffer per file.
- Buffers are mapped to file records byte‑for‑byte (explicit-layout semantics).
- Lifetime: allocated at OPEN, reused for each READ/WRITE.

6.2 READ — FileManager loads the record into the RecordBuffer; the StorageBlock is updated in place.

6.3 WRITE — FileManager writes bytes from the RecordBuffer.

6.4 REWRITE — FileManager overwrites the current record.

6.5 DELETE — FileManager marks the record deleted (indexed/relative).

6.6 Variable-length records (RECORD IS VARYING … DEPENDING ON)
- `WriteVariable` writes exactly the supplied bytes (no trailing-space trim) so the on-disk length round-trips.
- `LastRecordLength` is set after a READ so the DEPENDING ON item reflects the record actually read
  (ISO §13.18.43).
- `MaxVaryingRecordSize` / `MinVaryingRecordSize` enforce boundary violations (I-O status 44, ISO §9.1.13):
  a WRITE longer/shorter than the bounds fails. (Per-slot length-prefixed persistence supports variable-length
  relative records — NIST RL206A/207A.)

------------------------------------------------------------
SECTION 7 — KEY HANDLING
------------------------------------------------------------

7.1 Primary key — `RECORD KEY IS keyField`; must be unique; used for indexed access.

7.2 Alternate keys — `ALTERNATE RECORD KEY IS altKey WITH DUPLICATES`; may be duplicates; ascending or
descending. Maintained automatically on WRITE/REWRITE/DELETE.

7.3 Key of reference — `ReadByKey`/`Start` take a `keyIndex`: -1 = prime record key, 0+ = alternate record
key index (ISO §14.9.30 / §14.9.41). Handlers without alternate keys (sequential, relative) ignore it.

7.4 Key extraction — the compiler generates offset+length for each key field and comparison functions
(PIC-aware: COMP/COMP-3 keys compared numerically — see NIST relative/indexed COMP-key fixes).

7.5 Collation / comparison
- DISPLAY keys → lexicographic (ASCII or the program collating sequence).
- COMP / COMP‑3 keys → numeric.
- NATIONAL keys → UTF‑16 code-point order.

------------------------------------------------------------
SECTION 8 — FILE OPERATIONS
------------------------------------------------------------

8.1 READ
Forms:
- `READ file INTO ws`
- `READ file NEXT`
- `READ file PREVIOUS` (INDEXED and RELATIVE in DYNAMIC mode)
- `READ file KEY IS value` (INDEXED)
- `READ file RELATIVE KEY IS rk` / by RRN (RELATIVE)

On success: file status "00"; RecordBuffer updated.
On failure: e.g. "10" end of file, "23" key not found, "35"/"42" file not open, "14" relative key overflow.

READ NEXT after DELETE skips deleted records.

8.2 WRITE
- Sequential: appends.
- Relative: writes to the RRN slot (or next available slot).
- Indexed: inserts by key; duplicate primary key (on a no-duplicates file) → INVALID KEY (status 22).
- Alternate keys updated automatically for indexed files.

8.3 REWRITE
- Requires a successful READ prior; overwrites the current record.
- Indexed: key must not change.

8.4 DELETE
- Removes the record (indexed: removes prime + alternate key entries; relative: marks slot deleted).
- Sequential: not allowed.

8.5 START
- `START file KEY {= | > | >= | < | <=} value` positions the cursor for the next READ NEXT/PREVIOUS.
- INVALID KEY (no matching record / key out of range) if positioning fails.
- Implicit-KEY (prime key when KEY phrase omitted) is supported for relative and indexed files.

------------------------------------------------------------
SECTION 9 — FILE STATUS CODES
------------------------------------------------------------

CobolSharp implements the full ISO/IEC 1989:2023 I-O status code set. The shipped values (from
`src/CobolSharp.Runtime/IO/FileStatus.cs`) are:

| Code | Meaning |
|------|---------|
| 00 | Success |
| 02 | Successful op but a duplicate alternate key exists |
| 04 | Record length does not match FD RECORD CONTAINS |
| 05 | OPEN on OPTIONAL file that did not exist (created / available for first write) |
| 07 | Successful CLOSE/OPEN NO REWIND, REEL/UNIT etc. on a non-reel medium (ISO §9.1.13.2) |
| 10 | End of file (AT END) |
| 14 | Sequential READ on relative file where relative key exceeds max |
| 21 | Key value not in ascending sequence (sequential WRITE to indexed) |
| 22 | WRITE with duplicate key on a file that disallows duplicates |
| 23 | READ/START found no record matching the key |
| 24 | Record boundary violation (relative key exceeds file boundary) |
| 30 | Permanent I/O error (no more specific code) |
| 34 | WRITE past end boundary of sequential file |
| 35 | OPEN failed — file does not exist (INPUT/I-O) |
| 37 | OPEN failed — insufficient access permissions |
| 39 | OPEN — file attributes conflict with FD definition |
| 41 | OPEN on a file that is already open |
| 42 | CLOSE on a file that is not open |
| 43 | DELETE/REWRITE without a preceding successful READ |
| 44 | Record boundary violation (record too large / out of VARYING bounds) |
| 46 | No valid next record position for sequential READ |
| 47 | READ/START on a file not open for INPUT or I-O |
| 48 | WRITE on a file not open for OUTPUT, I-O, or EXTEND |
| 49 | DELETE/REWRITE on a file not open for I-O |

> **NOTE.** Some non-standard or speculative codes occasionally seen in early notes (e.g. "23 = record
> locked", "90 = runtime error", "91 = lock conflict", "92 = logic error", "93 = file integrity error") do
> **not** match the shipped, ISO-2023-conformant table above and were design placeholders. The table above is
> authoritative.

File status is updated after every file operation (OPEN/READ/WRITE/REWRITE/DELETE/START/CLOSE) and written
into the user‑defined FILE STATUS variable when one is declared (`FILE STATUS IS fs`).

------------------------------------------------------------
SECTION 10 — EXCEPTION ROUTING & DECLARATIVES
------------------------------------------------------------

10.1 INVALID KEY — triggered by failed READ KEY, failed REWRITE/DELETE, duplicate key on WRITE, invalid
relative key, START with no matching key.

10.2 AT END — triggered by READ NEXT at EOF, READ PREVIOUS at BOF, READ on empty file.

10.3 ON EXCEPTION — runtime errors, file corruption, permission issues, lock conflicts.

10.4 ExceptionState integration — on error the runtime ExceptionState is populated and INVALID KEY / AT END /
NOT-INVALID / NOT-AT-END / ON EXCEPTION phrases are evaluated.

10.5 Declaratives — if no in-line handler applies, the matching USE procedure is dispatched:
USE AFTER EXCEPTION / USE AFTER ERROR / USE AFTER STANDARD EXCEPTION, including GLOBAL USE inheritance
across nested/called programs (GlobalUseDeclarativeRegistry; NIST IC233A/234A) and USE re-entrancy guarding
(ISO §14.9.49.4 GR2; NIST RL111A).

------------------------------------------------------------
SECTION 11 — LOCKING, SHARING & CONCURRENCY *(design)*
------------------------------------------------------------

> Locking/sharing is *design intent* — verify what is actually wired before relying on it.

11.1 Lock modes — AUTOMATIC (default), MANUAL, EXCLUSIVE.

11.2 Sharing modes — SHARE ALL, SHARE READ, SHARE NONE.

11.3 Lock model
- Cooperative / advisory locking; **no OS‑level file locks** (AOT/WASM‑safe).
- Per‑record locking for indexed files; file‑level locking for sequential files.
- READ WITH LOCK locks a record until the next READ/WRITE/REWRITE; `UNLOCK file` releases.
- Lock conflicts surface as a file-status error; long waits time out (deadlock avoidance) rather than block.

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES
------------------------------------------------------------

Each statement lowers to a `CobolFileManager` call (via the FileManager bound to ExecutionContext), with the
returned status moved into the FILE STATUS variable and the INVALID KEY / AT END / NOT-… blocks branched on:

| Statement       | Lowers to                              |
|-----------------|----------------------------------------|
| OPEN            | `FileManager.Open(file, mode)`         |
| CLOSE           | `FileManager.Close(file)`              |
| READ (seq)      | `FileManager.ReadNext` (set status, branch AT END) |
| READ PREVIOUS   | `FileManager.ReadPrevious`             |
| READ KEY        | `FileManager.ReadByKey(…, keyIndex)`   |
| READ (relative) | `FileManager.Read…` by RRN             |
| WRITE           | `FileManager.Write` / `WriteVariable`  |
| REWRITE         | `FileManager.Rewrite`                  |
| DELETE          | `FileManager.Delete`                   |
| START           | `FileManager.Start(key, condition, keyIndex)` |

READ/WRITE/REWRITE/DELETE lowering wraps the call so that on error the ExceptionState is populated and the
declarative is dispatched. WRITE emits its INVALID/NOT-INVALID blocks like the other I-O verbs (NIST IX108A).
Compiler entry points: `FileIoBinder` (bind) → `FileIoLowerer` (lower) → `CilFileIoEmitter` (emit).

------------------------------------------------------------
SECTION 13 — AOT/WASM‑SAFE FILE I/O
------------------------------------------------------------

- No unsafe code: no pointers, no `stackalloc`.
- No dynamic codegen: static file operations.
- Deterministic behavior: same results across platforms (CoreCLR, AOT, WASM).
- WASM file system: uses a virtual FS; indexed files stored as structured blobs. *(Design intent for the
  WASM target — verify before relying on it.)*

------------------------------------------------------------
SECTION 14 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger surfaces, per file:
- File name, organization, access mode
- Current record number / file position
- Key buffer contents and current key value
- Record buffer (raw bytes + decoded fields)
- File status
- Lock state
- ExceptionState

Sequence points are emitted for OPEN, CLOSE, READ, WRITE, REWRITE, DELETE, START.
*(Debugger is design-only — Phase E — across the project.)*

------------------------------------------------------------
SECTION 15 — EDGE‑CASE BEHAVIOR
------------------------------------------------------------

- READ after end‑of‑file → AT END (status 10), repeatable.
- READ after OPEN OUTPUT → illegal (status 47 — read not open for input).
- WRITE with duplicate primary key → INVALID KEY (status 22).
- WRITE with a missing/invalid key → INVALID KEY.
- REWRITE/DELETE without a preceding successful READ → status 43 (and the INVALID KEY path).
- DELETE on a sequential file → illegal (DELETE only on relative/indexed).
- START on a sequential file → illegal (status 21).
- READ PREVIOUS on a sequential file → illegal (status 21).
- START with no matching key → INVALID KEY (status 23).
- READ NEXT after START GREATER → reads the first record after the positioned key.
- READ NEXT after DELETE → skips deleted records.
- Reading a missing/deleted relative record → INVALID KEY.
- Indexed file with a duplicate alternate key → allowed unless the key disallows duplicates.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp File I/O architecture:
- Implements full COBOL FD/SD, sequential/relative/indexed semantics with the three standard ACCESS MODEs.
- Provides deterministic, byte‑accurate record-buffer and key-handling behaviour (including variable-length
  records and per-key-of-reference access).
- Implements the full ISO-2023 I-O status code set and integrates with ExceptionState + declaratives
  (INVALID KEY, AT END, ON EXCEPTION, USE AFTER, GLOBAL USE).
- Maps every COBOL I-O verb to a `CobolFileManager`/`IFileHandler` runtime call via
  FileIoBinder → FileIoLowerer → CilFileIoEmitter.
- Generates clean, verifiable, debugger‑friendly, AOT/WASM‑safe CIL (CIL-only via Mono.Cecil; no custom VM).
- Currently rides the byte/StorageBlock engine (a classifier-scoped byte-image fallback that stays on the
  byte path as the typed-native data model is islanded around it).
