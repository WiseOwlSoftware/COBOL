CobolSharp Memory Model — StorageBlocks, Offsets, REDEFINES, OCCURS & DEPENDING‑ON Architecture
==============================================================================================

> **STATUS** — The authoritative reference for CobolSharp's byte-accurate memory model: StorageBlocks,
> field offsets, REDEFINES overlays, OCCURS / OCCURS DEPENDING ON, PIC/USAGE encoding (DISPLAY, COMP,
> COMP-3, COMP-5, COMP-1/2, NATIONAL), reference modification, group MOVE/COMPARE, LINKAGE binding and
> FD record buffers.
> **Implementation status:** the byte/StorageBlock memory model described here is **substantially
> implemented** (it is today's proven engine): `StorageArea.cs` (`ProgramState` = the WORKING/LOCAL/LINKAGE
> byte areas + `StorageHelpers`), `PicRuntime.cs` (the byte interpreter / codec), the layout pass
> `StorageLayoutComputer.cs` (Semantics; assigns offsets), and the `(Area, Offset, Length, PicDescriptor)`
> quad `StorageLocation` (`src/CobolSharp.Compiler/CodeGen/StorageLocation.cs`, a `readonly record struct`).
> **However this byte model is SUPERSEDED-IN-PART** by the typed-native data model: COBOL data is migrating
> to typed `record struct`s (`char→string` UTF-16, numeric→`long`/`decimal`, groups→nested `record struct`,
> `OCCURS→T[]`, pointers→`ManagedPointer`), gated behind `EnableTypedFields` (default **OFF** → corpus
> byte-identical), with the **byte/StorageBlock engine being ISLANDED** as a classifier-scoped fallback.
> The byte model is no longer "the substrate"; it is the safety floor and the codec at the typed↔byte
> boundary. Where this document and the live data-model docs disagree, **the live docs win.**
> **Stack:** .NET 10 / C# 14; backend CIL-only via Mono.Cecil (NO custom VM; the Roslyn C# backend is a
> future additive option, Cecil = oracle); all memory access is pure managed (`byte[]` / `Span<byte>`,
> no `unsafe`/`stackalloc`), AOT/WASM-safe.
> **Authoritative live docs (defer to these):**
> - `docs/DATA_MODEL_ARCHITECTURE.md` — the typed-native ADR (records→`record struct`; byte image is a
>   classifier-scoped island; the `IDataSlot` chokepoint where typed and byte meet; the 15 byte-backing
>   classification triggers; pointers→`ManagedPointer`; the 7-stage migration).
> - `docs/DATA-DIVISION-LAYOUT-DESIGN.md` — the canonical layout-algorithm design (OCCURS/REDEFINES/
>   RENAMES/USAGE binder model, 3-pass layout, size-by-USAGE table, semantic validation).
> - `docs/RECORD_STRUCT_STORAGE_DESIGN.md` — the staged engineering plan for the real `record struct`
>   storage substrate and the typed↔byte split.
> - Plan SSOT: `docs/MASTER_PLAN.md`. Doctrine: `PROMPT.md`.

Purpose
-------
Define the byte-accurate memory-model rules for:
- StorageBlocks (WORKING‑STORAGE, LOCAL‑STORAGE, LINKAGE, FD/SD buffers, temporaries)
- Field offsets and metadata
- REDEFINES overlays
- OCCURS tables
- OCCURS DEPENDING ON (ODO)
- Alignment and packing rules
- Group items and elementary items
- PIC/USAGE binary-layout semantics (DISPLAY, COMP, COMP‑3, COMP‑5, COMP‑1/2, NATIONAL)
- Substringing and reference modification
- Group MOVE / COMPARE / CORRESPONDING and overlap rules
- Numeric / string encoding rules and edge-case behavior
- Deterministic memory layout across platforms
- CIL‑friendly lowering and debugger integration

This document governs how the **byte-island fallback** of CobolSharp represents COBOL data in memory. In
the typed-native model (live docs above) most of these items are decoded values in a `record struct`; the
rules here apply to the bytes that back overlays, file records, pointer/`BASED` targets, EXTERNAL/GLOBAL
shared storage, and measured hot loops.

------------------------------------------------------------
SECTION 1 — MEMORY MODEL OVERVIEW
------------------------------------------------------------

CobolSharp's byte model is **byte‑addressable, explicit‑layout, deterministic**:
- Each COBOL data item resides in a StorageBlock at a fixed, compile-time offset.
- A StorageBlock is a contiguous `byte[]` buffer (in code: a `ProgramState` byte area).
- The compiler assigns an offset + length for every field; PIC/USAGE determines encoding.
- Group items are structural overlays of their children (no storage of their own).
- REDEFINES overlays bytes without copying — redefining items share the target's offset.
- OCCURS arrays are contiguous repeated regions.
- OCCURS DEPENDING ON allocates the MAX physical size; the logical length is a runtime value.
- Packed decimal (COMP‑3) uses BCD; binary (COMP, COMP‑5) uses fixed-width little-endian integers;
  DISPLAY uses raw bytes (single-byte for X/9, UTF‑16 for NATIONAL).
- Layout is **the same across all platforms** — no architecture-dependent alignment.

> **Encoding note (corrected per the typed model, `DATA_MODEL_ARCHITECTURE.md` §R10):** the byte↔char
> convention at the island / file boundary is the full **Latin-1** bijection (byte `k` ↔ U+00`kk`), **not
> bare ASCII**, so binary content (`LOW-VALUE`/`HIGH-VALUE`/arbitrary bytes) round-trips losslessly. (An
> "ASCII only; non-ASCII → runtime error" rule would be incorrect here.)

StorageBlocks exist for:
- WORKING‑STORAGE
- LOCAL‑STORAGE
- LINKAGE SECTION
- FD/SD record buffers
- Object instance blocks (OO classes — one block per instance)
- Temporary blocks (SORT, JSON/XML serialization buffers)

------------------------------------------------------------
SECTION 2 — STORAGEBLOCK STRUCTURE & FIELD METADATA
------------------------------------------------------------

A StorageBlock conceptually contains:
- `byte[] Buffer`
- A `FieldOffset[]` table
- A `FieldMetadata[]` table
- OCCURS metadata
- REDEFINES metadata

> In the live code the per-operand handle is the `(Area, Offset, Length, PicDescriptor)` quad —
> `StorageLocation` (a `readonly record struct`) — and the byte areas live in `ProgramState`
> (`StorageArea.cs`). `PicDescriptor` carries the compile-time PIC/USAGE info; the typed migration splits
> it into a compile-time `FieldShape` + a runtime `NumProfile` (see `DATA_MODEL_ARCHITECTURE.md` §9).

**FieldOffset** carries: absolute byte offset, length in bytes, PIC category (DISPLAY, NATIONAL, COMP,
COMP‑3, COMP‑5), scale (for numeric), sign rules, OCCURS index (if applicable), REDEFINES parent.

**FieldMetadata** carries: name, level number, `IsGroup` flag, children (for groups), REDEFINES target,
OCCURS count, ODO variable reference.

The compiler emits per field: offset, length, PIC info, USAGE info, OCCURS info, REDEFINES target.

------------------------------------------------------------
SECTION 3 — OFFSET ASSIGNMENT & PACKING RULES
------------------------------------------------------------

3.1 Sequential allocation
-------------------------
Offsets assigned in declaration order, top‑down / left‑to‑right:
`offset = previous_offset + previous_size`, without gaps unless alignment rules require.

3.2 Group items
---------------
- Group size = sum of children sizes.
- Group offset = first child offset.
- Offsets accumulate recursively for nested groups.

3.3 REDEFINES
-------------
- Shares the target's offset; size = size of the redefining item.
- No additional storage allocated; does NOT affect the offset of subsequent items.

3.4 OCCURS
----------
OCCURS n TIMES → allocate `n * element_size` bytes, elements contiguous.

3.5 OCCURS DEPENDING ON
-----------------------
Physical size = `max_occurs * element_size`; logical size = runtime value. Memory always reserves MAX.

3.6 Alignment & padding
-----------------------
COBOL traditionally does **not** require alignment. CobolSharp's deterministic default is **no implicit
alignment** — all items byte-packed, no padding between fields, ensuring identical offsets across
platforms. (An *optional/configurable* natural-boundary alignment for COMP / recommended for COMP‑5 is
possible, with the RECORD layout always deterministic regardless of the setting; the live model packs
tightly.) NATIONAL is the one intrinsic exception — it advances in 2‑byte units.
Padding rules: DISPLAY items space‑padded and truncated on overflow; numeric items zero‑padded with the
sign stored per USAGE rules.

------------------------------------------------------------
SECTION 4 — DATA TYPE / PIC / USAGE LAYOUT RULES
------------------------------------------------------------

4.1 DISPLAY (PIC X / PIC A)
---------------------------
- 1 byte per character; space-padded; truncated on overflow; no null terminator.

4.2 Numeric DISPLAY (PIC 9)
---------------------------
- 1 byte per digit, ASCII digits, right-justified, zero-padded.
- Sign stored as a trailing or leading character per the SIGN clause; a separate sign byte if
  `SIGN IS SEPARATE`. Numeric conversion of a DISPLAY field of spaces is treated as zero.
- Example: `"123"` stored as `31 32 33`.

4.3 COMP (binary)
-----------------
Fixed mapping by digit count, stored **little‑endian** (native .NET):
- PIC 9(1)–9(4) / S9(4): 2 bytes (Int16)
- PIC 9(5)–9(9) / S9(9): 4 bytes (Int32)
- PIC 9(10)–9(18) / S9(18): 8 bytes (Int64)

4.4 COMP‑5 (native binary)
--------------------------
Same widths as COMP but: no truncation on assignment, no decimal scaling, always native integer width;
overflow is checked.

> **Truncation note (typed model, `DATA_MODEL_ARCHITECTURE.md` §4):** COMP/BINARY truncate by **digit
> count** (`mod 10^n`); COMP‑5 truncates by **binary capacity** (`9(4) COMP-5` = 0..65535, defined
> wraparound). A store keyed off digit-count alone is wrong for COMP‑5.

4.5 COMP‑3 (packed decimal)
---------------------------
- Two digits per byte (BCD); last nibble is the sign.
- Size = `ceil((digits + 1) / 2)` bytes; odd digit counts padded with a leading zero.
- Sign nibble: C/F = positive, D = negative.
- Example: `PIC S9(5) COMP-3` → 3 bytes: byte1 = d1 d2, byte2 = d3 d4, byte3 = d5 sign.
  (`PIC S9(5)V99 COMP-3` → 4 bytes.)

4.6 FLOATING-POINT (COMP‑1 / COMP‑2)
------------------------------------
COMP‑1 (4-byte IEEE 754 float) and COMP‑2 (8-byte IEEE 754 double) ARE supported (`DecodeComp1/2`); the
typed model maps them to `float` / `double`.

4.7 SIGN rules
--------------
SIGN LEADING/TRAILING:
- DISPLAY → stored as an ASCII sign (overpunch, or a separate byte if SEPARATE).
- COMP‑3 → stored as the last nibble (low nibble of the last byte for TRAILING).
- COMP / COMP‑5 → two's-complement.

------------------------------------------------------------
SECTION 5 — GROUP ITEMS
------------------------------------------------------------

- Group items are **untyped overlays**: no PIC of their own; the children define interpretation; size =
  sum of children; always contiguous.
- Group MOVE = a byte‑for‑byte copy of the entire region (uses a temporary buffer if source/target
  overlap). MOVE CORRESPONDING matches by name (case-insensitive), moving only matching fields.
- Group REDEFINES = all children share the same bytes; the debugger shows all interpretations.

Example:
```
01 CUSTOMER.
   05 ID        PIC 9(5).        (5 bytes)
   05 NAME      PIC X(30).       (30 bytes)
   05 BALANCE   PIC S9(7)V99.    (10 bytes)
```
Total size = 45 bytes.

> **Typed-model refinement (`DATA_MODEL_ARCHITECTURE.md` §2.4):** a same-layout group MOVE is a value-type
> struct assignment in *either* representation; a *dissimilar-layout* group MOVE materializes the source's
> canonical byte image and lays it into the destination as raw bytes; group COMPARE always materializes
> byte images and compares lexicographically (byte order ≠ field-wise numeric order for COMP).

------------------------------------------------------------
SECTION 6 — REDEFINES
------------------------------------------------------------

6.1 Basic rule
--------------
- Redefining item shares the target's offset; same length (or shorter); no additional storage, no copying.
- All children of both views map to the same byte region; assignments update all views.

6.2 Type independence
---------------------
Redefined fields may differ in PIC and USAGE, and may be numeric or alphanumeric.

6.3 Scope
---------
- REDEFINES of a group → the entire group is overlaid.
- REDEFINES of an elementary item → overlaid at byte level.
- REDEFINES with OCCURS → overlay the entire table (no per-element overlay); a table may REDEFINE a
  scalar and vice-versa.
- REDEFINES with ODO → the overlay uses the MAXIMUM size.

6.4 Allowed edge cases
----------------------
- REDEFINES larger-over-smaller is allowed (the larger view may read beyond the original's logical size).
- REDEFINES shorter than the original is allowed (unused bytes ignored / overlay truncated).
- REDEFINES of COMP‑3 over DISPLAY is allowed.

> In the typed model, REDEFINES is the canonical **byte-island** trigger (#1): the whole REDEFINES
> equivalence class reverts to one inline byte buffer, and every view becomes a typed accessor over that
> buffer. Island membership is downward-transitive to all subordinate elementary items.

------------------------------------------------------------
SECTION 7 — OCCURS & OCCURS DEPENDING ON
------------------------------------------------------------

7.1 Fixed OCCURS
----------------
OCCURS n TIMES → `n * elementSize` bytes, contiguous.
Indexing is **1-based**: `offset = base + (index − 1) * elementSize`.

7.2 Nested OCCURS
-----------------
Multidimensional, row-major: `offset = base + (i * innerSize) + j * elementSize`.

7.3 OCCURS DEPENDING ON (ODO)
-----------------------------
`OCCURS n TO m DEPENDING ON var`:
- Memory allocated for the maximum (m).
- Active length determined by `var` at runtime; `var` must be numeric (DISPLAY or COMP).
- Bounds checked at runtime; clamped to the legal range: `var < n → use n`; `var > m → use m`.
- ODO is evaluated on READ, WRITE, MOVE CORRESPONDING, JSON/XML GENERATE, and SORT input/output.

> **Live behavior (`DATA_MODEL_ARCHITECTURE.md` §4, RL210A/211A/ST146A):** a whole-group operand over an
> ODO uses **sender = current count, receiver = MAX with space-fill** (ISO §13.18.39.3); READ-into uses
> MAX. Such groups are byte-backed (trigger 15) because one typed shape cannot carry both lengths.
> Out-of-range ODO **clamps to [min, max]**.

------------------------------------------------------------
SECTION 8 — SUBSTRINGING & REFERENCE MODIFICATION
------------------------------------------------------------

8.1 Syntax
----------
`identifier(start:length)`

8.2 Offset / length calculation
-------------------------------
- DISPLAY: `offset = base + (start − 1)`, `bytes = length`.
- NATIONAL: `offset = base + (start − 1) * 2`, `bytes = length * 2` (positions count UTF‑16 code units).

8.3 Bounds
----------
Per the spec, an out-of-range reference modifier is an exception condition (EC-BOUND-REF-MOD); the engine
raises the appropriate runtime/declarative path.

> **Typed-model refinement (`DATA_MODEL_ARCHITECTURE.md` §2.4 / §3 trigger 3):** ref-mod on a *proven
> homogeneous* single elementary alphanumeric/national item is plain char-position **span slicing on the
> `string`** — NO byte-backing. Ref-mod forces byte-backing only when it type-puns (slices raw bytes
> across heterogeneous/non-DISPLAY storage, or over a numeric-edited / overpunch-signed item, or a
> variable-bound slice over a heterogeneous group).

------------------------------------------------------------
SECTION 9 — STRING & NATIONAL STORAGE
------------------------------------------------------------

9.1 DISPLAY (PIC X)
-------------------
- Stored as raw bytes (Latin-1 convention at the boundary), space-padded, truncated on overflow,
  no null terminator.

9.2 NATIONAL (PIC N)
--------------------
- UTF‑16, 2 bytes per character, space-padded with U+0020.
- Surrogate pairs allowed; truncation never splits a surrogate pair; a NATIONAL item with an odd byte
  count is a runtime error.
- Length measured in characters, not bytes.

9.3 Mixed / converting operations
---------------------------------
- DISPLAY → NATIONAL: widen to UTF‑16.
- NATIONAL → DISPLAY: narrow to the single-byte set.
- A mixed national/alphanumeric group: NATIONAL encoding takes precedence.

> In the typed model **both** PIC X and PIC N are an in-memory UTF‑16 `string` (§1.2 of the ADR); the
> single-byte-vs-UTF-16 distinction is an *on-disk* `CODE-SET` decision applied only at the I/O boundary.

------------------------------------------------------------
SECTION 10 — MEMORY OPERATIONS (byte engine)
------------------------------------------------------------

- **MOVE** source → target: type-aware conversion with padding/truncation, sign handling, and decimal
  scaling for numerics.
- **MOVE CORRESPONDING**: matches by name (case-insensitive); moves only matching fields.
- **INITIALIZE** a group: alphanumeric → spaces, numeric → zeros, NATIONAL → UTF‑16 spaces.
- **INSPECT**: operates on DISPLAY bytes or NATIONAL UTF‑16 units.
- **Group MOVE / overlapping MOVE**: byte-for-byte copy; overlapping moves (`MOVE A(1:5) TO A(3:5)`)
  extract the substring then write to the target region.

> Numeric ops in the byte engine historically used `Decimal`; the typed model's numeric substrate is
> `CobolNum` / `CobolDecimal` (a `BigInteger` carrier + `NumProfile`) for 1–31-digit precision — see the
> ARITHMETIC/NUMERIC docs and `DATA_MODEL_ARCHITECTURE.md` §R5.

------------------------------------------------------------
SECTION 11 — FD RECORD BUFFER MODEL
------------------------------------------------------------

11.1 FD record
--------------
Each FD/SD defines a record group item backed by its own RecordBuffer StorageBlock, with key offsets and
key lengths.

11.2 READ / WRITE / REWRITE
---------------------------
- READ: FileManager loads bytes into the record buffer.
- WRITE: FileManager writes the record buffer bytes to the file.
- REWRITE: overwrites the current record.

11.3 Variable-length records
----------------------------
Supported via `RECORD VARYING` / `RECORD CONTAINS m TO n` and a runtime length field (per-slot
length-prefixed persistence for relative files).

> File records stay **byte-backed** in the typed model (trigger 5): the disk image *is* bytes, so a
> pass-through `READ … REWRITE` does not transcode the whole record. The entire current file-I/O subsystem
> (sequential/relative/indexed, PIC-aware COMP keys, the relative slot model) is reused unchanged. See the
> FILE-IO architecture docs.

------------------------------------------------------------
SECTION 12 — LINKAGE SECTION BINDING
------------------------------------------------------------

12.1 BY REFERENCE
-----------------
The LINKAGE item overlays the caller's StorageBlock (offset = caller offset); no copy is performed.

12.2 BY CONTENT
---------------
The LINKAGE item receives a copy of the caller's bytes (stored in the callee's storage).

12.3 BY VALUE (OO / 2002+)
--------------------------
A primitive value is passed directly into a local variable, not a StorageBlock.

> In the typed model, **all LINKAGE items and all `CALL … BY REFERENCE` arguments are *unconditional*
> byte triggers** (#11/#12) because the callee's view of the storage is unknowable at the caller's compile
> time (separate / dynamic compilation). This is the single most important soundness guard.

------------------------------------------------------------
SECTION 13 — CIL LOWERING RULES
------------------------------------------------------------

13.1 StorageBlock allocation & access
-------------------------------------
Allocate `new StorageBlock(size)`; generated IL accesses fields through runtime helpers:
`ctx.Storage.GetBytes / SetBytes`, `GetPackedDecimal / SetPackedDecimal`, `GetBinary / SetBinary`,
`GetNumeric / SetNumeric`, `GetString / SetString`. Field access pushes the buffer + offset + length and
calls the StringEngine / NumericEngine. Accessors enforce COBOL semantics centrally (padding/truncation,
sign rules, decimal alignment, OCCURS indexing).

13.2 REDEFINES / OCCURS / ODO lowering
--------------------------------------
- REDEFINES → same offset + same length assigned at compile time.
- OCCURS → `elementOffset = base + index * elementSize`.
- ODO → `activeCount = clamp(var, min, max)`.

13.3 No unsafe code
-------------------
Pure managed `byte[]` (and `Span<byte>` / `Span<char>` / array slicing); **no pointers, no `stackalloc`,
no unmanaged memory, no GC pinning** — AOT/WASM-safe and deterministic.

> **Live lowering (typed model, `DATA_MODEL_ARCHITECTURE.md` §9):** MOVE/COMPARE/arithmetic dispatch on
> the static **`IrDataSlot`** sum-type pair (`TypedFieldSlot` / `ByteWindowSlot`). The existing
> `IrLocation` hierarchy becomes the `ByteWindowSlot` producer; `CilLocationEmitter` pushes one
> `Span<byte>` for a window. The byte×byte cell IS the path described above — it remains a valid fallback
> for every other cell.

------------------------------------------------------------
SECTION 14 — DEBUGGER INTEGRATION
------------------------------------------------------------

The debugger surfaces, per field: name, PIC/USAGE clause, offset, length, raw bytes, decoded value,
REDEFINES overlays (all interpretations), OCCURS elements, ODO active length, and NATIONAL strings.

> See the DEBUGGER architecture docs (design-only, Phase E). In the typed model, typed records display as
> `customer.Balance == 42.50m` / `customer.CustName == "ACME"` rather than a raw byte array.

------------------------------------------------------------
SECTION 15 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

- **Zero-length items:** allowed; always empty; cannot be numeric.
- **REDEFINES smaller/larger:** allowed; a larger redefining view may read beyond the logical size; a
  shorter overlay ignores the unused tail bytes.
- **REDEFINES overlapping OCCURS:** allowed; accessor logic must respect the logical OCCURS length.
- **COMP‑3 odd digit count:** high nibble padded with a leading zero.
- **COMP‑3 SIGN TRAILING:** stored in the low nibble of the last byte.
- **COMP‑3 invalid sign nibble:** an exception condition — the live engine accepts 0x0A–0x0F as positive
  on decode and normalizes to 0x0C on encode, per the typed-model R2 grid; a truly invalid nibble routes
  to the data-exception path.
- **NATIONAL odd byte count:** runtime error.
- **NATIONAL inside REDEFINES:** allowed; the debugger shows raw bytes.
- **ODO < minimum → use minimum; ODO > maximum → use maximum.**
- **MOVE to a smaller field:** truncation; SIZE ERROR on numeric overflow.
- **Substring beyond the field:** out-of-range reference modification → exception (EC-BOUND-REF-MOD).
- **Numeric conversion of DISPLAY spaces:** treated as zero.
- **Mixed national/alphanumeric group:** NATIONAL precedence for encoding.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp byte memory model:
- Implements deterministic, platform-independent, byte-accurate StorageBlocks.
- Supports DISPLAY, COMP, COMP‑3, COMP‑5, COMP‑1/2, NATIONAL, OCCURS, ODO, REDEFINES, RENAMES, ref-mod.
- Uses explicit offsets with no padding (NATIONAL excepted), Latin-1 at boundaries.
- Provides safe, verifiable, AOT/WASM-compatible memory access (pure managed, no `unsafe`).
- Integrates with StringEngine / NumericEngine / FileManager and the debugger.
- **Is being islanded** as the classifier-scoped fallback under the typed-native data model — the safety
  floor and the codec at the `IDataSlot` boundary, no longer the substrate. For all current rules defer to
  `docs/DATA_MODEL_ARCHITECTURE.md`, `docs/DATA-DIVISION-LAYOUT-DESIGN.md`, and
  `docs/RECORD_STRUCT_STORAGE_DESIGN.md`.
