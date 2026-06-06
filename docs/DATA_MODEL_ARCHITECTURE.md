# CobolSharp Data-Model Re-Architecture — The Best Native .NET COBOL

**Status:** Converged architecture decision · **Date:** 2026-06-05 · **Target:** .NET 9 / C# 13
**Audience:** project owner (final sign-off to begin migration) · **Decision class:** foundational, multi-session
**Provenance:** synthesized from a four-proposal judge panel (Appendix A), then **refined through an owner-led design
dialogue on 2026-06-05** that inverted the panel's central premise. **This document supersedes the panel's "COBOL
data must remain a contiguous byte image" framing.** The byte image is *not* the substrate; it is a bounded
fallback. Typed-native .NET is the default.
**Reviewed 2026-06-06** by an adversarial multi-agent panel (45 verified findings) — verdict
**proceed-with-changes**; this revision folds in the 4 high + 6 medium findings and the top completeness gaps. Full
report: `docs/DATA_MODEL_REVIEW.md`.

**Grounded in:** `StorageLocation.cs` (the `(Area,Offset,Length,PicDescriptor)` quad), `PicDescriptor.cs`,
`PicRuntime.cs` (2,669-line byte interpreter), `StorageArea.cs` (`ProgramState` = three `byte[]` + `StorageHelpers`),
`CobolDataPointer.cs` (already a managed interior reference), `IrExpression.cs` / `IrType.cs` /
`IrLocationExtensions.cs` (the `IrLocation` hierarchy).

---

## 0. Thesis

The goal is the **best native .NET implementation of COBOL**, with no backward-compatibility constraint on the
code we generate. That goal has one concrete consequence the prior draft got backwards:

> **Typed-native is the default. COBOL records map to .NET `record struct`s; elementary items map to native .NET
> value types (`long`, `decimal`, `double`, `bool`, and `string` for character data). A byte image is materialized
> only where the COBOL semantics genuinely observe bytes — REDEFINES/RENAMES type-puns, file records, and a few
> measured hot loops — and that byte form is a small inline value *embedded in* the otherwise-typed record, not a
> heap `byte[]` and not the storage substrate.**

The earlier conclusion ("explicit-layout unions are rejected; everything sits on a contiguous byte image") was an
artifact of assuming `PIC X` is a `string` *field* (a managed reference, which the CLR forbids overlapping). Once
we recognize that (a) the **common case has no overlay at all**, and (b) the **rare overlay island can be a
blittable inline value**, the byte image collapses from "the substrate" to "a classifier-scoped exception."

Three observations from the dialogue drive the whole design:

1. **REDEFINES is critical to support but rare per record.** So it must never tax the common path. The right
   architecture optimizes the no-overlay case and treats overlays as a contained island.
2. **A .NET `string` is the right representation for *all* character data**, not a liability — because whole-field
   `MOVE` (the dominant alphanumeric op) is a free reference copy under string immutability, reads/compares run
   over `ReadOnlySpan<char>` with zero allocation, the value-producing verbs (`INSPECT REPLACING`, `STRING`) map to
   BCL primitives, and there is **zero conversion at every .NET API / interop / OO boundary**.
3. **In-memory representation is independent of external encoding.** "Single-byte vs UTF-16" is an *on-disk*
   `CODE-SET` decision applied at the I/O boundary — never an in-memory layout. In memory, processed text is always
   a UTF-16 `string`.

---

## 1. First principles (the decisions, and why)

### 1.1 Records → `record struct`; elementary items → native typed values

A COBOL record becomes a .NET `record struct` whose fields hold **decoded values**, not bytes:

```csharp
// 01 CUSTOMER. 05 CUST-ID 9(6). 05 CUST-NAME X(30). 05 BALANCE S9(7)V99 COMP-3.
public record struct Customer {
    public long   CustId;     // the value 42, never the bytes "000042"
    public string CustName;   // UTF-16, logically 30 char positions
    public decimal Balance;   // signed, scale 2
}
```

`ADD 1 TO CUST-ID` is `c.CustId = CobolNum.StoreInt(c.CustId + 1, digits: 6)` — a native add plus a centralized
truncation, **no decode/encode cycle**. This eliminates the dominant cost of today's interpreter, which
ASCII-parses, computes, and re-formats on every arithmetic op.

### 1.2 Character data → UTF-16 `string` (both `PIC X` and `PIC N`)

Spec basis (ISO/IEC 1989:2023, `specs/ISO_COBOL.md` §8.1.2): the standard is encoding-agnostic and explicitly
permits the alphanumeric *and* national coded character sets to both be the full UCS in UTF-16 (NOTE 2, line 5077;
NOTE line 5209). A national character is a UTF-16 code unit and a surrogate pair occupies two character positions
(lines 1564, 2326) — **identical to .NET `char`/`string` semantics**. So `PIC N(5)` ↔ a 5-`char` string,
position-for-position, losslessly. With no back-compat constraint we make the computer's coded character set
**UTF-16 for both X and N**, so all character data is `string`.

Why `string` is the right default, not a perf liability:

- **Whole-field `MOVE` is free.** `MOVE A TO B` for equal `PIC X(8)` (and even `X(32000)`) is `b = a` — an O(1)
  reference copy, zero allocation, under string immutability. A mutable buffer must copy all N chars.
- **Reads/compares are allocation-free** via `a.AsSpan()` / ordinal `MemoryExtensions`.
- **Value-producing verbs map to BCL primitives.** `INSPECT … REPLACING ALL c1 BY c2` → `Replace`; `STRING` →
  `IndexOf`/slice/`Concat`; `LEADING/FIRST/CHARACTERS/BEFORE/AFTER`, `STRING`'s `POINTER`/`OVERFLOW`, and `UNSTRING`
  compose from BCL primitives + spans. The new value these verbs produce is intrinsic to the operation, not a
  `string` tax — and the scanning side runs over spans with no allocation.
- **Zero conversion at every .NET boundary** — DISPLAY, interop, OO methods, the eventual C# backend all consume
  `string` directly.
- **A .NET `string` is a sequence of u16 code units that need not be valid Unicode**, so it holds arbitrary
  content (including `LOW-VALUE`/`HIGH-VALUE`/binary) losslessly.

**Two correctness obligations** when leaning on the BCL: (1) **always `StringComparison.Ordinal`** — COBOL is
character-value based; the BCL's culture-aware defaults would silently break semantics; (2) **COBOL length/fill
rules are ours** — `REPLACING` never changes length (operands are equal-size by rule), `STRING` leaves the
untouched tail of the receiver intact (no space-fill, unlike `MOVE`).

The one place a mutable buffer beats `string` — `INSPECT REPLACING`/`CONVERTING` done truly in place in a tight
loop — is a measured peephole optimization (§2.6), not the default.

### 1.2.1 COBOL string-manipulation verbs → .NET String / Span APIs (where appropriate)

**Principle: implement COBOL string manipulation over `System.String` / `ReadOnlySpan<char>` and the BCL string
APIs wherever the COBOL semantics match the API contract — do not hand-roll what the framework already does
correctly and fast.** "Where appropriate" is the operative clause: delegate when the contract matches, *compose*
from BCL primitives when no single API fits, and **implement ourselves only the COBOL-specific semantics the BCL
does not model** (the guardrails below). Delegating is the default; rolling our own is the exception.

| COBOL operation | .NET API (the appropriate mapping) | Notes |
|---|---|---|
| `MOVE` (equal length) | `=` (reference copy) | O(1), zero alloc under immutability |
| `MOVE` (pad / truncate / `JUSTIFIED`) | `PadRight(' ')` / `Substring` / `PadLeft(' ')` / `string.Create` | COBOL space-fill + right-justify rules |
| alphanumeric comparison | `span.SequenceCompareTo` **only when no PROGRAM COLLATING SEQUENCE is active**; otherwise the weight-table comparison (see collating note below) | shorter operand space-extended (ours); never culture-aware |
| `INSPECT … TALLYING` | `span.Count(value)` / `IndexOf` loop | `ALL`/`LEADING`/`FIRST`/`CHARACTERS` + `BEFORE/AFTER` bounds composed |
| `INSPECT … REPLACING ALL` | `string.Replace` | char and equal-length string overloads (length-preserving) |
| `INSPECT … REPLACING LEADING/FIRST/CHARACTERS` | `IndexOf` + slice + `Replace` within the bound | `FIRST` ≠ `Replace` (all-occurrences) → splice |
| `INSPECT … CONVERTING` | per-char translate over a span (table map) | COBOL `TR`-style; built on spans, no single BCL call |
| `STRING … DELIMITED BY … INTO` | `IndexOf` (delimiter) + `AsSpan().Slice` + `string.Concat`/`string.Create` | INTO-at-`POINTER` preserving the receiver tail + `ON OVERFLOW` are ours |
| `UNSTRING … DELIMITED BY … INTO` | `string.Split` / `MemoryExtensions` split / `IndexOf` scan | `COUNT IN`/`DELIMITER IN`/`TALLYING`/`POINTER` bookkeeping ours |
| reference modification `x(s:l)` | `AsSpan(s-1, l)` (read) / splice (write) | char positions, not bytes |
| `FUNCTION UPPER-CASE` / `LOWER-CASE` | `ToUpperInvariant` / `ToLowerInvariant` | invariant by default; honor program `LOCALE` when specified |
| `FUNCTION TRIM [LEADING/TRAILING]` | `Trim` / `TrimStart` / `TrimEnd` (`' '`) | |
| `FUNCTION REVERSE` | span reverse / `new string(reversed)` | |
| `FUNCTION LENGTH` | `string.Length` | character positions |
| `FUNCTION SUBSTITUTE` (2014) | chained `string.Replace` (ordinal) | |
| `FUNCTION CONCATENATE` / `FORMATTED-*` | `string.Concat` / formatting | |
| `FUNCTION CHAR` / `ORD` | `(char)` / `(int)` | collating-sequence-aware in this project |

**Guardrails for "where appropriate" — the BCL must be driven, not trusted blindly:**

1. **Always ordinal — but ordinal ≠ collating.** Use `StringComparison.Ordinal` and the `*Ordinal` overloads
   everywhere; the BCL's *culture-aware defaults* (`IndexOf(string)`, `Compare`, `StartsWith`, …) would silently
   break semantics (Turkish-İ, accent folding). **However, ordinal guards only against culture folding; it does
   NOT implement `PROGRAM COLLATING SEQUENCE`.** `string.CompareOrdinal` must **not** be used for a relation
   condition when a collating sequence is active — see the collating note below.
2. **COBOL length/fill rules are ours, applied around the BCL call.** `REPLACING`/`CONVERTING` never change length;
   `MOVE` space-fills and truncates; `STRING` leaves the receiver's untouched tail intact (no space-fill);
   `UNSTRING` space-fills short receivers.
3. **Don't force-fit COBOL-only semantics.** Figurative constants (`SPACE`/`ZERO`/`HIGH-VALUE`/`LOW-VALUE`/`QUOTE`/
   `ALL lit`), sender/receiver overlap behavior, alphanumeric↔national equivalence, and program-collating-sequence
   effects on comparison are implemented by us — the BCL is used for the mechanical scan/transform underneath.
4. **Scanning is allocation-free.** The read/scan side (`INSPECT`/`UNSTRING` source, comparison, delimiter search)
   runs over `ReadOnlySpan<char>`; only the produced result allocates.
5. **Class conditions and case-mapping use literal ranges / per-`char` overloads, never Unicode-wide helpers.**
   `IS ALPHABETIC` is the closed set {A–Z, a–z, space} (ISO §8.8.4.4) — literal range checks, **not**
   `char.IsLetter` (Unicode-wide, currently a real bug in `PicRuntime.IsAlphabeticClass`); `IS NUMERIC` is literal
   `'0'`–`'9'` plus valid sign bytes. `FUNCTION UPPER-CASE`/`LOWER-CASE` use the per-`char` overload
   (`char.ToUpperInvariant(char)`), never the string overload (which can alter a surrogate pair as a unit).

**Collating note (resolves review finding H4).** COBOL alphanumeric *relation* comparison is defined over the
`PROGRAM COLLATING SEQUENCE` (OBJECT-COMPUTER / ALPHABET, ISO §8.8.4.1.2), not native code-point order; the
compiler already implements this via a 256-entry weight table (`PicRuntime.CompareAlphanumericWithSequence`,
dispatched by `ConditionLowerer` whenever `ProgramCollatingSequence != null`). The default: **(b)** adapt that path
to operate on `ReadOnlySpan<char>` via a 65 536-entry `ushort` weight table built at program load from the 256-byte
COBOL sequence (keeps the typed path); **(a)** byte-back any item in a program with a non-identity collating
sequence is the conservative fallback until (b) is wired. The same applies to `FUNCTION CHAR`/`ORD` and SORT/MERGE
keys. Plain ordinal applies only when no collating sequence is active.

**String ops dispatch on the FieldShape *category* (X vs N), not a bare `string`.** Even under UTF-16-both, the
typed `string` must carry its alphanumeric-vs-national category in `FieldShape` so comparison conversion, national
space-padding, the correct collating sequence, and `EC-DATA-CONVERSION` remain dispatchable.

This principle lives in `Text/StringOps.cs` (§5): one place that maps each verb to its BCL implementation with the
guardrails enforced centrally, rather than scattering ad-hoc string code through the lowerer.

### 1.3 In-memory representation ≠ external encoding

Single-byte / EBCDIC / ASCII / UTF-8 is a **per-file `CODE-SET` property applied at the I/O serialization
boundary**, never an in-memory layout. Storing `PIC X` as single bytes in memory *and* doing string ops would
transcode byte↔UTF-16 on every operation — exactly the per-op "rebuild from bytes" cost we are eliminating. So:

- **WORKING-STORAGE / LOCAL-STORAGE text → `string` (UTF-16):** zero conversion for string ops.
- **External bytes → `CODE-SET`:** a file's bytes convert once on READ/WRITE — which *any* design must do, since
  disk records are bytes regardless of in-memory type.

### 1.4 Byte-backing is the exception, scoped by a classifier

A small inline byte value is used only for items that genuinely observe bytes (§2, §3):

- **REDEFINES / RENAMES type-pun islands** (incompatible views of one storage region).
- **FD/SD file records** — kept as their byte image so a pass-through `READ … REWRITE` does **not** transcode the
  whole record; only a field actually string-processed decodes, once, at use.
- **Pointer / `BASED` / `ADDRESS OF` / `ALLOCATE` targets** that need a stable addressable region.
- **`IS EXTERNAL` / cross-program `GLOBAL`** shared storage (one canonical representation).
- **Measured in-place-mutation hot loops** (peephole, §2.6).

Everything else is typed-native. The byte form is a blittable inline value embedded in the typed record — never a
heap `byte[]`, never the substrate.

### 1.5 Pointers → managed references; OO → .NET classes

`USAGE POINTER`/`BASED`/`ADDRESS OF` become a managed reference (`ManagedPtr`, §6); GC is transparent, no
opaque-handle registry. OO COBOL maps to real .NET classes (§7).

### 1.6 One interop chokepoint, byte path is the safety floor

Typed fields and byte islands meet only at the `IDataSlot` chokepoint (§2.5). Because every slot of a data category
can materialize a byte window, the existing, fully-tested byte engine is **always a valid implementation** — the
migration's safety floor and the reason this can land one rule at a time with the suite green throughout.

### 1.7 Initialization — VALUE / INITIALIZE / figurative constants

A typed field's `default(T)` (`null` string, `0` numeric) is **not** the COBOL category default the byte model
produces, and `VALUE HIGH-VALUE` / `VALUE ALL '*'` / signed-scaled or group-level VALUE have no `default(T)` form —
so initialization is modeled explicitly, never left to CLR defaults:

- **Typed fields get a COBOL-correct initializer** in the record's constructor: alphanumeric → `new string(' ', n)`
  or the padded VALUE literal; numeric → the VALUE or `0`; figurative VALUE → the materialized constant under the
  Latin-1 convention (byte k ↔ U+00kk; `HIGH-VALUE` = U+00FF, `LOW-VALUE` = U+0000). Never `default(T)`.
- **`INITIALIZE`** lowers to the same per-field initializers (honoring `REPLACING` / `TO VALUE` / `THEN TO DEFAULT`
  / `WITH FILLER`).
- **A VALUE not expressible as a typed initializer** (group-level VALUE over mixed members, VALUE on a
  numeric-edited item, or a VALUE whose byte image is later read through an overlay) is itself a classification
  trigger → byte-backed, initialized by writing its byte image (today's path).
- **Stage-0 consequence:** Stage 0 classifies everything byte-backed, so initialization is byte-identical there by
  construction; typed initializers are introduced *with* each Stage-3 typed flip and gated by the same suite-green
  check. (This closes the review's highest-priority completeness gap — `default(T)` ≠ COBOL fill.)

### 1.8 Aliasing & in-place mutation discipline

COBOL data items are **reference-identity storage cells**; a `record struct` is **copy-on-assignment**. The design
must never let a value-copy silently swallow a mutation. Every group/element-access site that can be a *receiver*
uses by-reference discipline:

- table-element mutation is a **`ref`-returning indexer** (`ref var e = ref table[i]`), never a value-returning
  `this[i]` (which would update a temporary — `MOVE X TO TBL(i)-FLD` must write the live element);
- a group passed to a mutating `PERFORM`/paragraph or method is passed `ref` (or is byte-backed);
- `ADDRESS OF` an interior field demotes its class to byte (trigger 6); `CALL … BY REFERENCE` is byte-backed
  (trigger 11), which also resolves the cross-program value-copy problem.

Reads may take `in` / by-value copies freely.

---

## 2. Overlays in the typed model (the make-or-break)

### 2.1 Why an overlay island cannot be two idiomatic fields

For `05 N PIC 9(8). 05 C PIC X(8) REDEFINES N.`, N and C **are the same 8 bytes**. `int N` + `string C` as
independent fields fails for two reasons: (a) the shared storage must hold anything *either* view can legally
write — through C a program can store `"ABCDEFGH"`, embedded spaces, `LOW-VALUE`, binary — which an `int` cannot
represent and which COBOL requires to survive and be re-readable through the 9 view; only **the bytes themselves**
can hold everything both views express; and (b) a managed reference (`string`) cannot overlap another field (CLR
loader rule). So the shared region of a type-pun class is *forced* to a byte-exact value.

### 2.2 The island: a blittable inline byte value embedded in the typed record

The classifier reverts **only the overlapping island** to an inline byte value (`[InlineArray(N)]` — blittable,
lives *in* the struct, not on the heap), with every view a typed accessor that encodes/decodes against that one
buffer. The rest of the record stays typed and decode-free:

```csharp
// 01 CUSTOMER.
//    05 CUST-ID     PIC 9(6).
//    05 CUST-NAME   PIC X(30).
//    05 PACKED-DATE PIC 9(8).                  ← overlay island
//    05 ALT-DATE REDEFINES PACKED-DATE.
//       10 YY 9(4).  10 MM 9(2).  10 DD 9(2).
public record struct Customer {
    public long   CustId;     // typed  — zero decode
    public string CustName;   // typed  — zero decode
    public Date8  Date;       // island — byte-backed inline value
}

[InlineArray(8)]                                   // 8 contiguous bytes, inline, blittable
public struct Date8 {
    private byte _e0;
    public long PackedDate { get => Codec.Disp(this,0,8); set => Codec.Disp(ref this,0,8,value); }
    public long Yy         { get => Codec.Disp(this,0,4); set => Codec.Disp(ref this,0,4,value); }
    public long Mm         { get => Codec.Disp(this,4,2); set => Codec.Disp(ref this,4,2,value); }
    public long Dd         { get => Codec.Disp(this,6,2); set => Codec.Disp(ref this,6,2,value); }
}
```

`MOVE 20260605 TO PACKED-DATE` lays 8 ASCII bytes; `MOVE YY TO WS` decodes bytes 0–3 → `2026`. They alias because
they share the buffer — the buffer is the single source of truth, byte-exact, both views, zero heap. The island's
canonical encoding is fixed: 1 byte = 1 character position (Latin-1 / program charset), buffer length = the
byte-image size of the redefined base item (ISO GR8). "Zero conversion" is precise — zero on the typed fast path;
exactly *one* codec call per island-boundary crossing.

### 2.3 Two implementation forms per island (both store the region once)

- **True value-type union** — `[StructLayout(LayoutKind.Explicit)] + [FieldOffset(0)]` over blittable views — when
  every view is a value type (numeric-over-numeric of equal size; inline-bytes-over-inline-bytes). Genuine CLR
  aliasing, no second storage. Now *legal* precisely because no view is a managed reference.
- **Single inline buffer + accessor views** (as in §2.2) — for group-over-group, RENAMES, and mixed cases where a
  `[FieldOffset]` union is awkward. One buffer; views decode/encode.

The lowering picks per island; both are byte-exact. **CLR foot-gun:** the `[StructLayout(Explicit)]` form is always
a self-contained, all-blittable nested struct; a `[FieldOffset]` is *never* placed on a struct that also holds a
managed (`string`/`object`) field — that fails to load with `TypeLoadException` at runtime, not compile time.

### 2.4 Each overlay feature → a view

- **REDEFINES** = two (or more) accessors over the same slice (or `[FieldOffset]` fields at one offset).
- **RENAMES (66)** = an accessor over `slice(offsetOf(first), end(last) − offsetOf(first))` — no storage of its
  own (matches today's layout builder skipping level 66). A renaming item is alphanumeric per spec → a `string`
  view over the slice.
- **Reference modification `x(s:l)` is *conditional*:** on a plain `string` it is char-position span slicing
  (`s.AsSpan(s-1, l)` for read; rebuild for write) — **no byte-backing needed.** It forces the island to bytes
  *only* when it type-puns (slices raw bytes across heterogeneous storage, e.g. across a binary field).
- **Group MOVE / COMPARE.** Same-layout and dissimilar-layout differ sharply, and the representation cost is *not*
  uniform:
  - **Same .NET type (same layout):** group MOVE is a **value-type struct assignment** `dest = source` — copying
    every member (typed fields by value, `string` by reference [safe under immutability], embedded byte islands
    inline) — in *both* the typed and byte-backed representations. Byte-equivalent for canonical content (a
    byte-island source preserves exact bytes, incl. non-canonical, since both sides are the same inline byte type);
    FILLER is a fixed per-type constant so "skipping" it is correct. **No materialization, no per-member encode,
    no typed-vs-byte cost difference.**
  - **Dissimilar layout (reinterpretation):** the source's canonical byte image is materialized and laid into the
    destination as raw bytes (span copy + alphanumeric pad/truncate); a typed destination re-decodes, or is
    byte-backed. **This is where the materialize/encode cost actually lives.**
  - **Group COMPARE** always materializes the byte image(s) and compares lexicographically — it cannot shortcut to
    struct equality, because byte order ≠ field-wise numeric order for COMP (sign nibbles, etc.).

  `MOVE CORRESPONDING` lowers to per-matching-field elementary moves.

### 2.5 The `IDataSlot` chokepoint — where typed and byte meet

Every operand lowers to a compile-time sum type the emitter pattern-matches (no runtime virtual dispatch on hot
paths):

```
abstract IrDataSlot
 ├─ TypedFieldSlot(field, NumProfile?)                  // c.CustId, c.CustName, c.Balance
 └─ ByteWindowSlot(bufferRef, offset, length, FieldShape) // an island slice — today's quad, retyped to a span
```

MOVE/COMPARE/arithmetic dispatch on the static slot-kind pair:

| src → dst | Typed | Byte window |
|---|---|---|
| **Typed** | native assignment + `CobolNum` truncation — no buffer touched | `CobolNum.Encode` the value into the dst window via the boundary codec |
| **Byte window** | decode the src window, store with `CobolNum` truncation | the existing path verbatim — `PicRuntime.Move<srcCat>To<dstCat>` |

**Three MOVE cells always route through the byte codec** regardless of operand representation, because they are
positional/byte operations with no scalar form: numeric-edited→numeric (**de-edit**, reads the stored edited
character image), alphanumeric/national→numeric (rightmost-position reinterpretation), and figurative→numeric
(`HIGH/LOW-VALUE`/`QUOTE` are category alphanumeric). These never take the typed×typed path.

**Every slot of a data category can materialize a `ByteWindowSlot`** (a typed field encodes into a scratch window),
so the byte×byte cell — the fully-tested system — is always a valid implementation of the other three. That is the
universal escape hatch and the migration safety floor. The guarantee holds for {numeric, alphanumeric, national,
boolean, edited, group}; it does **not** extend to {`POINTER`, `OBJECT REFERENCE`}, which are typed-only with no
byte image — but those are spec-prohibited as CALL BY REFERENCE / EXTERNAL operands (ISO §14.9.4.3 SR10,
§13.18.11 GR4) and any overlay of them is caught by trigger 6, so the gap is unreachable. Note that
`ByteWindowSlot.length` may be a runtime expression (ODO) — the chokepoint dispatches on slot *kind*, not a static
length.

### 2.6 The peephole exception

`INSPECT REPLACING`/`CONVERTING` are guaranteed same-length, so a hot loop hammering one field in place can use a
mutable `Span<char>` scratch and finalize to `string` once — a local optimization the lowerer applies when it sees
the pattern. Not a representation change; the field is still a `string`.

---

## 3. The classifier — conservative, monotone, complete-before-codegen

A `RecordClassificationPass` (after symbol resolution, before layout/codegen) assigns each item/record a
representation. The **default is typed**; an item (transitively, with its REDEFINES class) becomes a **byte
island** if it has any of:

1. REDEFINES (target or redefiner — the whole equivalence class is one island);
2. RENAMES (66) spanning dissimilar items;
3. **reference modification**, as a statically-decidable over-approximation (not the runtime "does it actually
   type-pun" test):
   - *3a.* any item that is the receiver or sender of `x(s:l)` **unless** it is statically a single elementary
     alphanumeric or national item (a proven-homogeneous `string`);
   - *3b.* any refmod of a non-alphanumeric DISPLAY/NATIONAL item, a numeric-edited item, or an overpunch-signed
     DISPLAY item — ISO §8.4.3.3.4 GR2: it is operated on as alphanumeric over its **character image**, which a
     typed `long`/`decimal` has no positional form of (the everyday `MOVE WS-DATE(1:4)` on a `PIC 9(8)` idiom);
   - *3c.* any variable-bound refmod (non-literal `s`/`l`) over a group with heterogeneous (mixed char/numeric, or
     any non-DISPLAY) content — the pre-scan cannot prove the slice stays within a homogeneous run;
4. **group-as-alphanumeric operations** (group MOVE/COMPARE/CORR, a group used as an alphanumeric operand, or a
   class condition over a group). These operate on the group's *byte image* but **do not by themselves force the
   group to be byte-backed**: a typed group **materializes its canonical byte image on demand via the codec** at the
   `IDataSlot` boundary (§2.5) — its elementary members stay typed for elementary arithmetic, and FILLER/slack is
   reconstructed from its fixed init constant. A raw same-layout **struct copy is not a valid shortcut** when any
   member is non-DISPLAY (it would skip FILLER and could normalize non-canonical packed bytes — ISO §14.9.25 GR4
   forbids representation conversion in a group move); the move is field-wise/materialized instead. **Permanent**
   byte-backing is triggered only when (a) a group MOVE **reinterprets** the moved bytes under a *dissimilar* layout
   (the destination must hold the raw moved image — *dissimilar* = differing SYNC-aligned byte offsets, even if
   declared fields match), or (b) a member can hold **observable non-canonical** bytes via a separate byte-write
   path (REDEFINES/file/pointer — already triggers 1/5/6). A same-layout group MOVE is a value-type
   assignment in *either* representation (§2.4), so the representation choice affects only *dissimilar-layout*
   reinterpretation and group COMPARE (where a typed group materializes its byte image) — a profiling decision
   (Open Question #4), not a correctness one;
5. membership in an `FD`/`SD` (file record — its disk image is bytes; kept byte-backed for pass-through, §8);
6. pointer / `BASED` / `ADDRESS OF` / `ALLOCATE` target needing a stable address;
7. `SYNCHRONIZED` slack observable via another byte trigger (SYNC alone never triggers);
8. `IS GLOBAL` / `IS EXTERNAL` cross-program sharing (one canonical representation);
9. raw-byte interop with an already-byte island of mismatched layout;
10. any item whose category cannot be fully resolved (defensive default);
11. **`CALL … USING … BY REFERENCE` arguments** (static *or* dynamic), transitively — the callee receives a raw
    byte alias into this storage region (ISO §14.2.3 GR8: a BY REFERENCE formal parameter occupies the *same
    storage* as the argument). Under separate or dynamic compilation the callee's LINKAGE re-description is
    unknowable at the caller's compile time, so address-escape via BY REFERENCE is an **unconditional** byte
    trigger — this is the single most dangerous omission the review found;
12. **all `LINKAGE SECTION` items** — formal parameters whose storage is owned by the caller and whose layout the
    compiler may not renegotiate without changing the CALL ABI;
13. **numeric-edited items** — kept byte-backed so the stored edited character image and the de-edit path
    (numeric-edited sender → numeric receiver, ISO §14.9.25 GR5) remain available;
14. **write-pattern items** — an item that is the receiver of refmod *writes*, or of repeated `STRING … POINTER`
    advances, inside a `PERFORM` loop is byte-backed for that loop scope, avoiding O(N²) string reallocation on the
    typed path (the §2.6 peephole promoted to a classification decision);
15. any group transitively containing an `OCCURS DEPENDING ON` item and used as a **whole-group operand** — the
    sender uses the current count, the receiver the MAX with space-fill (ISO §13.18.39.3); one typed shape cannot
    carry both lengths.

**Island membership is downward-transitive:** every subordinate elementary item of an island's REDEFINES/RENAMES
class is a `ByteWindowSlot` view, never a standalone `TypedFieldSlot`, regardless of the §4 default mapping.

**Soundness (load-bearing):** a **two-phase + fixpoint** pass over **one compilation unit** that completes
*entirely before any instruction is lowered*. Phase A marks data-division triggers; Phase B scans the procedure
division for refmod / group ops / class conditions / pointer & CALL-argument usage and demotes typed→byte; Phase C
propagates demotion across **intra-program** interop edges to a fixpoint (lattice height 1 — representations only
move typed→byte — so it terminates). **A feature can never be discovered mid-emit.** Any doubt → byte — explicitly
including non-literal refmod bounds over groups; refmod is *never* assumed "pure char-position on a string" unless
the base is a proven-homogeneous string field. **Scope limit:** the fixpoint cannot cross compilation / dynamic-CALL
boundaries, so the defense there is the *unconditional* conservative trigger (11/12), **not** analysis. The
debug-build encode→decode round-trip assertion at each `IDataSlot` boundary turns an *intra-program*
misclassification into a test failure; the cross-program case is guarded by the trigger, since that alias never
crosses this program's chokepoint.

---

## 4. The full COBOL → .NET type-mapping table

| COBOL item | Typed-native field (default) | Byte-island view (when classified) | Notes |
|---|---|---|---|
| `PIC 9(1..9)` / `S9(1..9)` DISPLAY/COMP/BINARY/COMP-5 | `int` (n≤9) / `long` | window + `Disp/Comp/Comp5` codec | value, not ASCII; `NumProfile` carries a `TruncationPolicy` — COMP/BINARY truncate by **digit count** (`mod 10^n`), COMP-5 by **binary capacity** (`9(4) COMP-5` = 0..65535, defined wraparound). `StoreInt(value, digits)` keyed off digits alone is wrong for COMP-5 |
| `PIC 9(10..18)` / 5–8-byte binary | `long` | window + codec | |
| `V`/`P` scaling, or COMP-3 / PACKED-DECIMAL | `decimal` (**value carrier only**; digits/scale/sign identity in `NumProfile`) | window + `Comp3`/scaled codec | every compare / SIZE-ERROR read goes through NumProfile-aware helpers, never raw `decimal ==` |
| `PIC 9(19..31)` extended precision | `decimal` (≤28–29 digits) else byte/`BigInteger`-scaled | byte path for full 31-digit | documented edge (Risk R5) |
| `COMP-1` / `COMP-2` | `float` / `double` | IEEE codec | already correct in `DecodeComp1/2` |
| `PIC X(n)` / `A(n)` | **`string`** (UTF-16, logically n positions) | `[InlineArray] Bytes<N>` window | string ops via BCL+spans, ordinal |
| `PIC N(n)` national | **`string`** (UTF-16) | `Chars<N>` window | 1 `char` = 1 national position |
| numeric-/alphanumeric-edited | edited-`string` projection (output) | `Edited` window (carries pattern) | reuse `FormatNumericEdited` |
| `PIC 1` boolean | `bool` | `Bool1` window | one position '0'/'1' |
| `USAGE INDEX` / `INDEXED BY` | `int` (occurrence number) | — | `SET UP/DOWN BY` = `+=`/`-=` |
| `USAGE POINTER` / `BASED` / `ADDRESS OF` | `ManagedPtr` (§6) | 8-byte window only if overlaid as bytes | GC-tracked, no registry |
| `OBJECT REFERENCE` | typed .NET reference / `object?` | — (references have no byte image) | OO (§7) |
| group item | nested `record struct` of children | group window over whole span | all-DISPLAY group MOVE = field-wise copy; non-DISPLAY group materializes its canonical byte image at the op (members stay typed) — §2.4 |
| `OCCURS n` (fixed) | `T[]` / `CobolTable<T>` (`[InlineArray]`-backed) | indexer window `this[i] => slice(...)` | 1-based handled in lowering |
| `OCCURS DEPENDING ON` | `CobolTable<T>` sized to MAX + `CurrentCount` | indexer window + runtime length | whole-group operand: **sender = current count, receiver = MAX with space-fill** (ISO §13.18.39.3) → such groups are byte-backed (trigger 15); READ-into uses MAX (RL210A/211A/ST146A) |

---

## 5. Runtime library shape

`PicRuntime` is **demoted** from "the only path" to "the byte-island engine and the `IDataSlot` boundary codec."

```
CobolSharp.Runtime/
  Numeric/
    CobolNum.cs            // Store/TryStore: scale → round(8 ISO modes) → truncate(digit-count OR binary-capacity) → sign.
                           //   TryStore returns bool for ON SIZE ERROR — NEVER throws. ALSO SafeAdd/Sub/Mul/Pow:
                           //   EVERY expression-tree operator is a no-throw size-error-setting helper, not just the
                           //   final store (today intermediate decimal.op_* can throw before ON SIZE ERROR can fire).
                           //   19–31-digit intermediates use BigInteger, NOT decimal (decimal = 28–29 digits; see M1/R5).
    INumericCodec.cs       // static-abstract strategy: Decode(span,in NumProfile)->value; Encode(span,in NumProfile,value)
    Codecs/                //   DisplayUnsigned, DisplaySignedOverpunch, DisplaySignedSeparate, Comp, Comp5, Comp3, Comp1, Comp2
    Rounding.cs            // 8 ISO rounding modes (logic exists in PicRuntime; EXTRACTED AND CORRECTED —
                           //   RoundProhibited must set EC-SIZE-TRUNCATION, not silently truncate)
  Text/                    // the DEFAULT path for character data
    CobolString.cs         // helpers over System.String: ordinal MOVE (ref copy), pad/truncate, justify, compare
    StringOps.cs           // COBOL string verbs → .NET String/BCL APIs per §1.2.1 (ordinal; COBOL fill/length rules
                           //   enforced here; intrinsics UPPER-CASE/TRIM/REVERSE/SUBSTITUTE map to String/Span APIs)
    Edit.cs                // FormatNumericEdited / alphanumeric-edited (the surviving formatter)
  Bytes/                   // the byte-island engine = today's PicRuntime body, repurposed
    IslandCodec.cs         // Encode(value->window)/Decode(window->value) per FieldShape; the boundary codec
    LegacyPicRuntime.cs    // retained; byte-island op implementation + IDataSlot boundary
  Memory/
    ManagedPtr.cs          // {object? owner, int offset, int length} — managed pointer (§6)
    ProgramStorage.cs      // successor to ProgramState; byte areas shrink to islands + file records
  Io/  …                   // FileRuntime / indexed / relative — reused UNCHANGED (records are byte-backed)
  Oo/  …                   // base support for OO COBOL (§7)
```

**Source generators (Phase 2, with the Roslyn backend):** a Roslyn incremental generator consumes the layout
builder's per-record field map and emits the typed `record struct` plus island accessors as on-disk, steppable
`.cs`. Until then the Cecil backend emits the equivalent CIL directly; the generator is additive.

---

## 6. Pointer model — managed reference, no opaque-handle registry

`CobolDataPointer(byte[] Buffer, int Offset, int Length, PicDescriptor Pic)` is already a managed interior
reference. Generalize:

```csharp
public readonly record struct ManagedPtr(object? Owner, int Offset, int Length) {
    public static ManagedPtr Null => default;        // all-zero ≡ NULL
    public bool IsNull => Owner is null;
}
```

- `USAGE POINTER` field = `ManagedPtr` (GC tracks `Owner`; no native heap, no handle table).
- `ADDRESS OF x` = `new ManagedPtr(ownerRegion, offsetOf(x), len(x))`.
- `BASED` + `SET ADDRESS OF b TO p` = the based item's view is constructed over `p`'s region+offset.
- `ALLOCATE` = `new byte[]` (or pooled) owned by the `ManagedPtr`; `FREE` = drop the reference (GC reclaims).
- `SET p UP/DOWN BY n` = `p with { Offset = p.Offset + n }` (undefined across distinct owners — the same latitude
  COBOL gives across allocations).
- A pointer overlaid as `X`/`9` bytes is byte-backed by trigger 6.
- `ADDRESS OF` a typed item is a trigger-6 event demoting its class to byte, so a `ManagedPtr.Owner` is always a
  `byte[]` / island buffer; `OBJECT REFERENCE` uses the typed-reference path (§7), not `ManagedPtr`.

This is the owner's stated Phase-2 direction; GC is transparent because the IL holds managed references to managed
regions — no registry indirection needed.

---

## 7. OO COBOL model — real .NET classes

| COBOL OO | .NET |
|---|---|
| `CLASS-ID. Foo INHERITS Bar.` | `public class Foo : Bar` |
| instance `METHOD-ID. M.` / `FACTORY` method | instance method / `static` method |
| `OBJECT REFERENCE Foo` / `USAGE OBJECT` | typed `Foo` reference field (or `object?`) |
| `INVOKE obj "M" USING …` | `obj.M(…)` (`callvirt`) |
| `SELF` / `SUPER` | `this` / `base` |
| `PROPERTY` | C# property |
| `INVOKE Foo "NEW"` | `new Foo()` |

Object instance data is a per-instance record (so REDEFINES/refmod inside object data still classifies normally),
but object **identity** is a real .NET reference, sharing the managed-reference machinery of §6. Each OO feature
ships a `tests/conformance/<version>/` test in the same commit (standing rule).

---

## 8. File I/O — unchanged engine

File records are **byte-backed** (trigger 5): the disk image *is* bytes, and keeping them as bytes avoids
transcoding an entire record on a pass-through `READ … REWRITE` — only a field actually string-processed decodes,
once, at use. So the entire current `FileRuntime`/`IO/` subsystem (sequential/relative/indexed, variable-length
length-prefix persistence, PIC-aware COMP keys, the relative slot model) is **reused without change**.
`READ`/`WRITE`/`REWRITE` move raw bytes ⇄ the record region; `READ INTO`/`WRITE FROM` cross the `IDataSlot`
boundary. The external encoding is the file's `CODE-SET` applied here, at the boundary. Indexed keys are a sub-span
compared by byte image (correct per ISO collating). **The hardest-won part of the suite (SQ/RL/IX/ST) sits behind
the byte path and never moves.**

---

## 9. Compiler-pipeline changes

Canonical phase structure (Parse → Bind → Lower → Emit) preserved.

| Phase | Change |
|---|---|
| Parser / AST | none (no semantics in the parser — doctrine) |
| Semantics | **NEW** `RecordClassificationPass` (two-phase + fixpoint, §3); annotates each `DataSymbol` with its representation |
| `PicDescriptor` | split: **`FieldShape`** (compile-time category/usage/editing/sign/length — stays in compiler) + **`NumProfile`** (`readonly record struct` digits/scale/signed — the only thing handed to runtime). Ends the per-op `PicDescriptor` `newobj`. |
| Layout (`RecordLayoutBuilder`) | one walk, two products: typed records → field list (offsets only for islands); byte islands → today's offsets. REDEFINES island sizing stays here. |
| IR (`IrType.cs`) | add `IrTypedRecordType`/`IrTypedField` and the `IrDataSlot` sum type (`TypedFieldSlot`/`ByteWindowSlot`). The existing `IrLocation` hierarchy becomes `ByteWindowSlot`-producing — **re-targeted, not redesigned** (the IR is already backend-neutral — the leverage point). |
| Lowering | MOVE/COMPARE/arith dispatch on the slot-kind pair (§2.5); CORR → per-field for typed; arithmetic emits native ops wrapped by `CobolNum.Store` only where `NumProfile` proves normalization is needed; string verbs → `Text/` ops. |
| Emission (`CilEmitter` + `Emission/*`) | Typed: native field load/store + native arithmetic + `string`/span ops. Byte: existing `PicRuntime` calls. `CilLocationEmitter` pushes one `Span<byte>` for windows; `EmitLoadPicDescriptor` `newobj` retired for `NumProfile` literals + codec types. |
| Backend | **Keep Cecil/CIL primary through the migration.** Add the Roslyn C# backend in Phase 2 behind `--backend csharp`, Cecil retained as a **differential oracle** (run the corpus both ways, diff all baselines). Flip default to C# only after a sustained green run. |

---

## 10. Phased migration — green at every step (1047 / 480 / 364)

Big-bang is rejected (PROMPT.md staged-regression gate). The plan exploits two facts: **the byte model is today's
proven model**, and **`Span<byte>` over the existing `byte[]` is byte-identical to `byte[]+offset+length`.** Guard
must be all-green before each stage proceeds.

**Stage 0 — Scaffolding, zero behavior change.** Introduce `NumProfile`, `CobolNum`, `FieldShape`, the `IrDataSlot`
sum type (with only `ByteWindowSlot`), and `Span<byte>` adapter overloads (old `byte[]+offset` delegate via
`area.AsSpan`). Classify **every item byte-backed**. Byte-identical; suite green. Proves the plumbing.
**Byte-identity caveat (M6):** the `PicDescriptor → FieldShape + NumProfile` split must be a *lossless* rename —
`FieldShape` carries every field `PicRuntime` consumes (signStorage, editPattern, blankWhenZero, scale digits,
isJustifiedRight, …), not just digits/scale/sign — and the all-byte-backed start makes the 1047/480/364 suite
itself the mechanized byte-identity check. (Alternatively defer the split to Stage 6 and add `NumProfile` as an
additive parallel type here.)

**Stage 1 — Numeric pipeline, differentially validated.** Route byte-island arithmetic through `CobolNum.Store`;
**differential-test** byte-for-byte vs today's `ApplyScalingAndRounding`+`EncodeNumeric` over a generated value
grid (digits × scale × sign × **usage** × rounding × overflow) before anything flips. Front-loads the one component
whose subtle incorrectness would be pervasive, with the byte runtime as oracle — **except** the >28-digit and
PROHIBITED-rounding branches, which are validated against an *independent* high-precision reference, since the
decimal-based byte runtime is common-mode blind there (both sides would agree on the wrong/zero answer).

**Stage 2 — `RecordClassificationPass`, fallback ON.** Add the classifier but keep a flag forcing everything
byte-backed; assert it *would* classify correctly without acting. Green by construction.

**Stage 3 — Enable typed for the narrowest subset, one rule at a time.** Flip typed on for records with only
elementary items and no triggers; add `TypedFieldSlot` and the typed↔typed / typed↔byte lowerings. **Character
first is cheap** (whole-MOVE = ref copy). Run the full guard after **each rule-widening commit** (numerics →
groups → OCCURS → …). NIST CCVS programs are overlay-heavy and mostly stay byte-backed — and keep passing via the
byte path; ordinary programs and `tests/conformance/` increasingly hit the typed fast path. The
materialize-to-window fallback guarantees any unhandled typed case degrades to the byte path, never crashes.

**Stage 4 — Pointers (managed ref) + OO classes.** Convert `CobolDataPointer`→`ManagedPtr`; map OO COBOL to .NET
classes. Largely additive; each feature ships a conformance test in the same commit.

**Stage 5 — Roslyn C# backend, Cecil as oracle.** Add `--backend csharp` emitting readable `record struct` +
island types + paragraph methods. CI lane diffs the whole corpus against the Cecil oracle on all baselines — any
divergence is a hard failure. Flip default to C# after a sustained green run.

**Stage 6 — Finalize runtime shape (§5); post-conformance rename** (`CobolSharp` → `COBOL.NET`, executable
`cobol.exe`) per the standing post-conformance goal.

At every stage a working, shippable compiler is one flag away. Typed-% is a coarse *coverage* indicator only — not a
perf or safety gate; the real gates are the Stage-5 Cecil-vs-C# oracle diff (correctness) and runtime profiling on a
representative workload (performance). Open Question #4 sets the coverage ambition and defuses any "flip the easy
leaves" incentive.

---

## 11. Performance and debuggability

- **Performance.** Typed records eliminate the decode→compute→encode tax that dominates hot loops today. Character
  data gets O(1) whole-field MOVE for equal-length operands (reference copy; unequal length pads/truncates) and
  allocation-free reads/compares via spans. `record
  struct`/`[InlineArray]` keep data stack-local and allocation-free; OCCURS → real arrays enable JIT bounds-check
  elision. The `decimal` cost is bounded by an integer fast path (`long`) for unscaled PIC 9. Byte islands cost
  what they cost today (often I/O-bound), never worse. Net: dramatically faster common case, no regression on the
  overlay/file case.
- **Debuggability.** Typed records show as `customer.Balance == 42.50m` and `customer.CustName == "ACME"`, not
  `byte[24]{0x32,…}`. Phase 2's C# backend emits real, steppable C# with a PDB and optional `#line` mapping back to
  `.cob`.

---

## 12. Risks and the remaining owner decisions

### Risks (with mitigations)

| # | Risk | Mitigation |
|---|---|---|
| R1 | **Classifier under-detects a byte-observation path** (alias/pointer chain, or a separately-compiled / dynamic CALL callee whose LINKAGE view is unknowable here) → silent corruption | Conservative default (any doubt → byte); **per-compilation-unit** fixpoint completes before emission; **CALL … BY REFERENCE arguments and all LINKAGE items are *unconditional* byte triggers (11/12)** precisely because the callee's view is unavailable at the caller's compile time. The debug round-trip assertion catches only *intra-program* misclassification; the cross-program case is guarded by the trigger, not the assertion. |
| R2 | **`CobolNum.Store` not byte-identical to legacy** on an edge (P-scaling, 31-digit, overpunch, multi-sign-nibble) | Stage-1 differential test over a generated grid — axes incl. USAGE, SignStorageKind, multi-sign-nibble decode (accept 0x0A–0x0F positives; encode normalizes to 0x0C) — vs the byte runtime; the **>28-digit and PROHIBITED-rounding branches validated against an *independent* high-precision reference**, not the (common-mode-blind) decimal byte runtime. |
| R3 | **Binary content in a `string`** (`LOW-VALUE`/`HIGH-VALUE`/group-moved bytes) | A .NET `string` holds arbitrary u16 losslessly; all char ops are ordinal/byte-value based; an item the program treats as raw bytes is classified byte-backed anyway. |
| R4 | **`[InlineArray]` escape/size limits** for island buffers | Threshold → pooled `byte[]` above a cap; inline buffers accessed as spans within a stack scope; escaping (LINKAGE/EXTERNAL/BASED) islands use pooled arrays. |
| R5 | **`decimal` precision < 31 digits** (ISO mandates 1–31; `decimal` = 28–29) — incl. **DISPLAY 9(19..31)** (today's `DecodeDisplay` silently returns `0m` above 28 digits) and **intermediate** results | `CobolNum`'s 19–31-digit intermediate is `BigInteger`, not decimal; route by algebraic magnitude (`n_stored + trailingP ≥ 29`), not stored-digit-count alone. **This is Open Question #1 — resolve before Stage 1**, as it decides whether `decimal` is a legal default at all. |
| R6 | **Roslyn backend divergence / build speed** | Divergence is the safety mechanism: Cecil oracle + byte-exact baselines make any diff a hard CI failure. Keep Cecil for the fast inner loop; cache metadata references. |
| R7 | **Dual-path maintenance during migration** | Temporary by design; Stage 6 demotes `PicRuntime` to `Bytes/`. Shared semantics (`CobolNum` is the single source of truncation/ROUNDED/SIZE-ERROR) keep the paths from diverging. |
| R8 | **Scope creep / big-bang temptation** | Strict per-rule, per-stage guard gating; never proceed on red; update `docs/ISO2023_CONFORMANCE_PLAN.md` per stage; DEVLOG per commit. |
| R9 | **WS `OCCURS` of `PIC X` → per-element heap strings** (~24 B object overhead each) | Measure on the corpus; large char tables may use a packed/inline char representation or stay byte-backed; tie to Open Question #3 (inline-vs-pooled by **escapedness**, not size alone). |
| R10 | **CODE-SET / island byte↔char codec must be Latin-1, not ASCII** | The convention is the full 0x00–0xFF Latin-1 bijection (byte k ↔ U+00kk) so binary content round-trips losslessly at file I/O and island boundaries. |

### Settled in the 2026-06-05 dialogue (no longer open)

- **Character representation:** `string` (UTF-16) for both `PIC X` and `PIC N`; byte form only for islands.
- **In-memory vs external encoding:** decoupled; single-byte/EBCDIC/UTF-8 is a `CODE-SET` boundary concern.
- **Overlay mechanism:** inline byte island embedded in the typed record (value-type union or buffer+accessors).
- **Pointers/OO:** managed references / .NET classes.

### Open questions for the owner

1. **Numeric value type policy — resolve BEFORE Stage 1 (it gates whether `decimal` is a legal default).** Confirm
   the tiered default — native `long`/`int` for unscaled integer PIC, `decimal` for scaled/packed within 28 digits,
   and **`BigInteger` (not `decimal`) for the 19–31-digit range and all intermediates** — vs a single uniform
   representation. The Stage-1 oracle's >28-digit grid must be validated against an *independent* high-precision
   reference, not the decimal-based byte runtime (which is common-mode blind there).
2. **Backend end-state.** Adopt the Roslyn C# backend as the eventual default (readable/debuggable, best OO fit,
   most marketable as `COBOL.NET`) with Cecil demoted to a test-only oracle (recommended, after Stage 5 proves it
   green) — or keep Cecil/CIL as the shipping backend with C# as an inspection aid only?
3. **Island buffer threshold.** The inline-vs-pooled cap for island `[InlineArray]` buffers (e.g. ≤256 inline).
   Where exactly? (Affects only islands now, not the common `string` path.)
4. **Conformance ambition during migration.** Acceptable for NIST-heavy overlay programs to remain byte-backed
   indefinitely (green and correct, just not "native"), with typed targeted at new/ordinary code — or drive a
   specific % of the corpus to typed?
5. **OO/pointer sequencing.** Pointers in Stage 4 on Cecil (recommended — `CobolDataPointer` already exists) and OO
   in Stage 5 alongside the C# backend (its natural home) — or defer both to Phase 2?

### Tracked completeness investigations (resolve before the relevant stage; detail in `docs/DATA_MODEL_REVIEW.md` §5)

- **USE FOR DEBUGGING / DEBUG-ITEM** populates `DEBUG-CONTENTS` with the *character image* of a monitored item — a
  byte-observability path needing a procedure-division-driven trigger (like the refmod pre-scan).
- **EXTERNAL / run-unit-shared storage memory model** — trigger 8 asserts "one canonical representation" with no
  threading / visibility / lifetime model; needs a defined .NET memory-model story before EXTERNAL goes typed.
- **Embedded-precompiler ABIs** (EXEC SQL / EXEC CICS host variables, copybook comm-areas) bind by *byte address +
  PIC layout* (SQLDA, DFHCOMMAREA, BMS) — they force large swaths of commercial code to byte islands via a trigger
  not yet listed; acknowledge for the `COBOL.NET` commercial trajectory even though unsupported today.
- **Stage-5 differential-oracle soundness** — non-deterministic output (ACCEPT FROM DATE/TIME, RANDOM,
  system-dependent status, uninitialized-storage-dependent output) forces an exclusion list that can blind the
  oracle exactly where typed-vs-byte default-value differences surface; "any diff is a hard failure" overstates the
  guarantee. Define the determinism contract + exclusion policy before relying on it.

---

## 13. First concrete slice (what to build first)

1. `NumProfile` + `CobolNum.Store/TryStore` (8 rounding modes; SIZE ERROR via `TryStore`, never exceptions) with
   the Stage-1 **differential oracle** vs `PicRuntime.ApplyScalingAndRounding`+`EncodeNumeric`/`DecodeNumeric` over
   a generated value grid — the riskiest correctness component, proven first.
2. Split `PicDescriptor` → compile-time `FieldShape` + runtime `NumProfile`; keep `PicDescriptor` only inside
   `Bytes/`.
3. `IrDataSlot` sum type + `Span<byte>` adapter overloads; all items byte-backed → suite green (Stage 0).
4. `RecordClassificationPass` returning byte for everything, then incrementally enabling typed — character data
   first (cheapest, highest ergonomic payoff) — (Stages 2–3).

---

### Bottom line

**Typed-native is the architecture; the byte image is a bounded, classifier-scoped fallback.** COBOL records →
`record struct`; numerics → `long`/`decimal`; character data → `string` (UTF-16, both X and N), with equal-length
whole-MOVE as a free reference copy and verbs over BCL+spans; pointers → managed refs; OO → .NET classes. Overlays are an inline
byte island *embedded in* the typed record — a value-type union or buffer-with-views — never a heap `byte[]` and
never the substrate. External encoding lives at the `CODE-SET` boundary. The two representations meet only at the
`IDataSlot` chokepoint, where the proven byte engine is always a valid fallback — so the design is never less
correct than today, dramatically faster on the common path, byte-exact where COBOL demands, and migratable one rule
at a time with the 1047/480/364 suite green at every step.

---

## Appendix A — Provenance (the four-proposal panel)

This design synthesizes a judge panel of four proposals; the 2026-06-05 owner dialogue then **inverted the panel's
central premise** (it had concluded the byte image must be the substrate because `[FieldOffset]` cannot overlap a
`string` with an `int` — true only if `PIC X` is a managed-reference field; making character data a `string` *value
surface* with byte islands as blittable inline values dissolves that). The panel's enduring contributions, kept
here: P3's **classification spine** + single interop chokepoint; P2's **typed-views-over-shared-storage** overlay
representation and codec-strategy numerics; P1's **centralized, differentially-validated `CobolNum`** (with its
three-state `Lazy` policy dropped — binary classification removes the sync problem); P4's **Roslyn C# backend +
Cecil-as-oracle**, deferred to Phase 2. Original scoring table and per-proposal verdicts are retained in version
control history of this file (prior revision).

**Files grounding this design (absolute paths):**
`E:\CobolSharp\src\CobolSharp.Runtime\PicRuntime.cs` (codec body → `Bytes/` engine + boundary codec),
`E:\CobolSharp\src\CobolSharp.Runtime\PicDescriptor.cs` (split into `FieldShape` + `NumProfile`),
`E:\CobolSharp\src\CobolSharp.Runtime\StorageArea.cs` (`ProgramState` → `ProgramStorage`),
`E:\CobolSharp\src\CobolSharp.Runtime\CobolDataPointer.cs` (→ `ManagedPtr`),
`E:\CobolSharp\src\CobolSharp.Compiler\CodeGen\StorageLocation.cs` (the quad → `ByteWindowSlot`),
`E:\CobolSharp\src\CobolSharp.Compiler\CodeGen\RecordLayoutBuilder.cs` (one walk, two products),
`E:\CobolSharp\src\CobolSharp.Compiler\IR\IrType.cs` / `IrExpression.cs` / `IrLocationExtensions.cs`
(the `IrLocation` hierarchy → `ByteWindowSlot`-producing; add `IrTypedRecordType`/`IrDataSlot`),
`E:\CobolSharp\src\CobolSharp.Compiler\CodeGen\Emission\CilLocationEmitter.cs` (push one `Span<byte>`; retire
`EmitLoadPicDescriptor`), `E:\CobolSharp\src\CobolSharp.Compiler\CodeGen\Lowering\DataMovementLowerer.cs`
(slot-pair dispatch), `E:\CobolSharp\scripts\guard.sh` (add the C#-backend oracle diff lane).
