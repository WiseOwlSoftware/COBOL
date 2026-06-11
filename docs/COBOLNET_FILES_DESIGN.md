# COBOL.NET — File I/O (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §8; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Deep, decision-complete design for the COBOL.NET FILES subsystem (typed records, clean byte boundary). Core architecture: the FD or SD record IS a .NET record struct (the record area is a typed field, not a byte buffer); the only bytes are at the on-disk edge, produced by a compiler-GENERATED per-layout codec (Serialize and Deserialize) running only at READ and WRITE. CODE-SET is one Encoding parameter threaded into that codec. The proven 364-NIST legacy handlers are ported VERBATIM for control logic (open-mode tables, ISO-cited status codes, the file-position-indicator plus key-of-reference plus duplicate-arrival state machines) but re-substrated from a byte array to a generic FileConnector plus an IRecordCodec. Covers all organizations (SEQUENTIAL, LINE SEQUENTIAL, RELATIVE, INDEXED), all access modes, OPEN CLOSE READ WRITE REWRITE DELETE START, FILE STATUS as a two-char string item, variable-length records, prime and composite ALTERNATE keys, SAME RECORD AREA, SORT and MERGE, and LINAGE. Decisive post-advisor move: ordering and lookup keys are a typed-derived CobolKey comparable (numeric by decoded value, alphanumeric by image plus collating, composite component-wise), decoupled from the stored payload; one comparison policy shared by indexed files and SORT.

## Decisions

### D1. The FD or SD record is a typed record struct (the record area); there is no per-file byte buffer. Bytes exist only transiently in a compiler-generated codec at the disk edge.

**Rationale.** Owner lock: a COBOL record is a .NET record struct and file I/O is the only legitimate bytes. A generated per-layout codec keeps the model native and the serialization fast and inspectable in the generated source.

**Rejected alternatives.** Keep a byte-array record area (the banned legacy substrate); interpret PIC at runtime (slow, untyped).

### D2. Port the three proven legacy handlers control logic verbatim but re-substrate to a generic FileConnector plus IRecordCodec.

**Rationale.** That logic encodes hundreds of ISO 9.1.13 and 14.9.30-51 edge cases proven by 364 NIST tests; re-deriving it would re-discover every bug the legacy already fixed (stale alt-index, START inclusive-FPI, 43 and 46 read-position, ascending-WRITE 21, duplicate-arrival 02 and 26). It is orthogonal to the byte-vs-typed substrate; one connector per organization sharing one codec is the singular pattern.

**Rejected alternatives.** Re-derive file semantics from scratch (loses 364 tests of edge-case knowledge); a single mega-connector with org branches (less cohesive).

### D3. Ordering and lookup keys are a typed-derived CobolKey comparable, decoupled from the stored payload: numeric by decoded value, alphanumeric by character image (ordinal or collating-weight, shorter-operand space-extension), composite component-wise.

**Rationale.** One comparison policy shared by indexed files and SORT. Fixes the legacy Latin-1 byte-string sorted-dictionary trick that silently mis-ordered COMP, COMP-3, and signed keys (their on-disk image is not order-preserving). Matches the spec rule to compare the key data item (14.9.30 and 41).

**Rejected alternatives.** Order by serialized key bytes (concedes the binary-key ordering bug and forks policy from SORT); a key projector by field offset (reintroduces an offset model); a non-generic connector over object (boxing per field access).

### D4. The relative and indexed connector in-memory model stores the serialized byte image per record as the payload (keyed and ordered by the typed CobolKey), deserialized to the typed record only on hand-back.

**Rationale.** The image is exactly the form persisted on CLOSE (one representation), bounded memory regardless of record-graph richness, and ordering is still typed; bytes never escape the connector except via the codec. Surfaced explicitly as owner-visible because an in-memory byte array is the exact thing the owner objected to for program data, but here it is legitimate confined file bytes.

**Rejected alternatives.** An all-typed sorted-dictionary-of-typed-records store (viable, more native, but holds the full object graph and gains nothing semantically; a one-line flip if the owner prefers it).

### D5. Multiple 01s under one FD (and SAME RECORD AREA) are a discriminated record-area wrapper: READ deserializes the raw bytes into EVERY 01 view; WRITE serializes the named view.

**Rationale.** ISO 9.1.2 NOTE and 13.18.33 GR3: all 01s under an FD implicitly redefine the same storage area. Round-tripping every view off the same raw bytes reproduces the COBOL byte-overlay (read alphanumeric then inspect as numeric) without a persistent byte substrate. It is the file-edge analogue of REDEFINES.

**Rejected alternatives.** A single shared byte-array record area for multi-01 FDs (the banned substrate); ignore the overlay (breaks reinterpretation-of-record NIST patterns).

> **As-built note (Phase 1E):** the shared area is realized as a synthesized REDEFINES class over the 01s (the
> REDEFINES deep-dive's tiers), not a separate wrapper type — the file edge and REDEFINES share ONE overlay
> mechanism (singular-pattern). With a fixed-point BINARY/PACKED leaf among the records (ST134A's SAME RECORD AREA
> pair) the class is Tier B: one string backing IS the record area (§12.4.6.4.4 GR2 — "an implicit redefinition of
> the area, with records aligned on the leftmost byte position"), each record/leaf a window accessor, binary leaves
> image-stored with the overpunch profile rewrite (REDEFINES deep-dive D10).

### D6. SORT and MERGE: the SD record is a typed struct; the sort store holds serialized images ordered by the same CobolKey policy; SORT key offsets are computed into the deterministic serialized image at compile time. Format-2 in-place table SORT operates on the typed array directly.

**Rationale.** Reuses the proven stable sort plus numeric-value-versus-collated-image key rule (14.9.40 and 22; DUPLICATES IN ORDER is a stable sort). Table SORT data is already typed in memory so it diverges to a typed comparer, the one documented place the two SORT forms differ.

**Rejected alternatives.** A true k-way priority-queue merge (a perf optimization, deferred); a byte comparer for table sort (the data is already typed).

### D7 (Phase 1E, 2026-06-10). The serialized record image of a MIXED-USAGE record (fixed-point BINARY/PACKED leaves beside string-stored leaves) is the generated `AsImage()`/`FromImage()` character image — each such leaf contributes its fixed-width ZONED DIGIT image (`Pic.Digits` chars, trailing-overpunch sign), per `PicInfo.ImageSignKind`.

**Rationale.** ISO §13.18.60 USAGE GR4 makes a binary item's representation — including the algebraic sign — implementor-defined; `COBOLNET_DESIGN.md` §14.4's total digit-image rule defines it ONCE for every character context, and the record codec rides the same definition (singular-pattern: `IRecordCodec` for these records IS the generated pair — WRITE/RELEASE send `AsImage()`, READ/RETURN distribute via `FromImage()`; `CSharpEmitter.EmitImageInto`, `StatementBinder.SortRecordOf`). The SORT/MERGE consequence is load-bearing: a signed COMP/COMP-3 key's compile-time descriptor (`BoundSortMergeKey.SignKind`) carries the IMAGE sign (`ImageSignKind` — trailing overpunch), never the leaf's stored `BinaryMinus`, so `CobolSort.NumericKey` decodes the zoned window algebraically (§14.9.40 GR8 / §8.8.4.2.4 — negatives before positives; the failure mode of decoding with `BinaryMinus` is SILENT: everything sorts, negatives order positive). Key offsets/lengths are already computed in digit-image coordinates (`SortOffsetInRecord`/`SortPhysicalWidth` sum `ImageWidth`) — no width changes anywhere. The on-disk record width for such records legitimately DIFFERS from the legacy's raw-byte form (e.g. ST133A: 80 chars where the legacy wrote 72 + 4 binary bytes) — chains are same-engine; cross-engine file compatibility is explicitly not required. Float (COMP-1/2), COMP-5 (BinaryCapacity exceeds the digit-count window), and INDEX leaves keep a record OUTSIDE the codec — the loud Tier-C island. Proven by ST108A/ST127A/ST133A/ST134A + `MixedUsageRecordImageDifferentialTests`.

**Rejected alternatives.** Raw big-endian bytes as Latin-1 characters (the legacy on-disk form): a SECOND representation for the same concept — compare/DISPLAY contexts already use digit images, so `RETURN`-then-compare would disagree with `IF group = group` about what a record "looks like"; also needs a new Binary decode kind in `CobolSort.Key` and control characters through every string seam, buying only a cross-engine compatibility that is not required. Leading-separate-sign images (width = Digits+1 — would change `ImageWidth` and every offset computation).

## C# mapping

> Backend neutrality (G4; SSOT §18 #23): everything semantic in this section — FILE STATUS capture, the AT END /
> INVALID KEY branch, READ INTO / WRITE FROM expansion, the prologue registrations — is a structured BOUND-TREE form;
> this section shows the primary RoslynBackend rendering. The future CilBackend renders the SAME bound nodes behind
> `ICodeGenBackend` with its own private lowering; no bound node carries pre-rendered C# text.

An FD record CUST-REC with CUST-ID PIC 9(5), CUST-NAME PIC X(20), CUST-BAL PIC S9(7)V99 COMP-3 maps to a public record struct CUST_REC holding public long CUST_ID, public string CUST_NAME, public long CUST_BAL, where CUST_BAL is the UNSCALED long (scale 2 is compile-time metadata per the owner numeric lock; no decimal). The record area is a single field of that type in the program class. The connector is a public sealed generic FileConnector of TRec exposing Open, Close, Read, ReadPrevious, ReadByKey, Write, Rewrite, Delete, Start, SetKey, plus properties CurrentSlot, LastRecordLength, EndOfPage, and LastStatus. There is NO program-supplied key parameter; the key is the current value of the typed RECORD KEY or RELATIVE KEY field. The codec is a generated IRecordCodec exposing Serialize, Deserialize, PrimeKey, AlternateKey, FixedLength, MinLength, MaxLength, and CodeSet. Stores: the sequential connector uses a StreamReader or StreamWriter for line-sequential and a FileStream for record-sequential with a 4-byte little-endian length prefix for varying; the relative connector uses a sorted dictionary from int slot to byte image with 0xFF gaps; the indexed connector uses a sorted dictionary from CobolKey to byte image as the sole source of truth, with alternates derived on demand and an arrival map for duplicate ordering. The run-unit prologue emits one registration per SELECT plus AddAlternateKey calls. After each I/O verb the compiler stores the connector LastStatus into the FILE STATUS item then branches AT END or INVALID KEY on the first char (1 at-end, 2 invalid-key, 3 4 7 9 fatal to a USE declarative). READ INTO lowers to Read plus a typed group MOVE; WRITE FROM lowers to a typed MOVE plus Write; a sequential RELATIVE WRITE or READ NEXT MOVEs CurrentSlot back into the RELATIVE KEY field.

## Hard problems

### Key ordering must match COBOL semantics for binary, signed, and composite keys; the legacy Latin-1 byte-string sorted-dictionary trick silently mis-orders COMP keys.

Order by the typed CobolKey comparable: numeric by decoded value, alphanumeric by image plus collating, composite component-wise. One policy across indexed files and SORT; the on-disk image is payload only and never drives ordering.

### Multi-01 record-area overlay: a program writes through one 01 then reads another 01 reinterpretation of the same bytes, with no persistent byte buffer to overlay.

READ deserializes the raw record bytes into every 01 view of the FD via a discriminated wrapper; the file edge has the bytes anyway. Cost is order of number of views, and views are few.

### REDEFINES and RENAMES inside an FD record cannot be deferred (the codec must emit the redefining layout bytes).

The codec serializes the base (first) definition; a REDEFINES sub-view is materialized by re-deserializing the emitted bytes under the redefining layout on demand. Designed once and shared with the general working-storage REDEFINES solution.

### Variable-length REWRITE must equal the replaced record length (14.9.35 GR16) or it is status 44 with the record unchanged.

The connector remembers the last-read frame start and length; REWRITE re-serializes, compares serialized length to the remembered length, and returns 44 if different.

### Read-position state machine: READ NEXT after AT END is 46; READ PREVIOUS after AT END returns the last record; a sequential REWRITE or DELETE without a preceding successful READ is 43; START establishes an inclusive file-position indicator.

Port the proven per-connector last-read-unsuccessful, past-end, prev-op-was-successful-read, and read-next-inclusive flags verbatim (14.9.30 GR21, 14.9.35 GR5).

### EXTERNAL files shared across programs plus GLOBAL FD inheritance in nested programs, with matching layouts.

A process-wide registry keyed by external name (with an Area discriminator for record sharing, porting IC227A); nested programs resolve the parent connector via a global registry (porting IC233A and 234A); the codec and layout must match across declarations.

## Edge cases

- OPTIONAL files: OPEN INPUT missing gives 05 and positions at EOF (first READ is 10); OPEN I-O or EXTEND missing creates and gives 05; non-optional missing gives 35 (not silent-create, the legacy RelativeFileHandler bug fix).
- Sequential RELATIVE WRITE assigns the next slot and MOVEs it into the typed RELATIVE KEY field; READ NEXT exposes CurrentSlot for the same MOVE-back (14.9.51 sequential).
- Random RELATIVE: key below 1 gives 34, an occupied slot gives 22 (INVALID KEY), an absent slot gives 23; sequential digit overflow gives 24, sequential READ digit overflow gives 14.
- Indexed: ascending-order WRITE in ACCESS SEQUENTIAL gives 21 on a non-increasing key; a duplicate prime or alt-without-duplicates gives 22; alt-with-duplicates gives 02; START supports a generic partial or prefix key compare (14.9.41) and positions inclusively so the next READ NEXT returns the matched record.
- READ INTO and WRITE FROM lower to the verb plus a typed group MOVE (receiving uses the MAX length for ODO records, the ST146A lesson).
- Record length mismatch on READ (a fixed file whose physical record differs from the FD size) gives status 04; add for conformance since the legacy pads silently.
- LINE SEQUENTIAL: newline-framed, TrimEnd on WRITE, pad or truncate on READ, LastRecordLength is the line length; status 06 and 09 are deferred to the post-85 feature drive (`docs/ISO2023_CONFORMANCE_PLAN.md` catalog) — LINE SEQUENTIAL itself is not COBOL-85; see Per-edition gating.
- CODE-SET translates only character (alphanumeric and DISPLAY-numeric digit) bytes, not COMP or COMP-3 binary fields (13.18.13); the default is the native ASCII set.
- LINAGE: LINAGE-COUNTER, PAGE reset, page overflow (GR26a), footing-area end-of-page (GR26b); END-OF-PAGE phrases branch on the EndOfPage flag; an ADVANCING or LINAGE file is forced to line-oriented output.
- On-disk framing: a fixed record-sequential file is contiguous; a variable sequential, relative, or indexed file uses a 4-byte little-endian length prefix; a sparse relative file uses 0xFF gaps.
- DELETE FILE statement (14.9.10): delete the host path and reset the in-memory map; status 00, 05, or 35.
- SAME AREA buffer-only and SAME SORT-MERGE AREA are no-ops in a managed runtime (pure memory-layout optimizations with no observable behavior).

## Per-edition gating (G1 — four compilers in one `cobol.exe`)

File I/O contains edition-varying constructs. Every edition-varying construct carries TWO co-equal obligations:
(1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every
edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Rows cite
`docs/VERSION_CHANGE_REFERENCE.md`, the 130-row edition-change checklist (2002→2023 deltas ONLY — it has NO 85→2002
rows; derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog); the
(construct × edition) cases land in the version test matrix (`docs/VERSION_TEST_MATRIX_DESIGN.md`, Phase 0 done).

- **DELETE FILE (14.9.10 Format 2) is NEW in 2023** (rows 58/78; E.3.3 items 15/35): rejected with a
  not-yet-introduced diagnostic under `--std` 85/2002/2014; its statuses 05/37/39/41/62 from DELETE FILE exist only
  at 2023. (Format-1 record DELETE is 85.)
- **READ PREVIOUS is not COBOL-85** (a 2002 introduction — derive from the 2002 standard): rejected at 85. Its
  behavior ALSO changed 2014→2023 (row 29): READ PREVIOUS immediately after OPEN retrieves the first record at 2014
  but raises the at-end condition at 2023 — the connector's read-position state machine must take the target
  edition. FLAG-14 flags every READ PREVIOUS (row 108).
- **ORGANIZATION LINE SEQUENTIAL is not a COBOL-85 organization**: rejected at 85. The exact introduction edition is
  not derivable from the 2023 spec (no ledger row; the ledger has no 85→2002 row set) — derive it from the 2002
  standard before gating.
- **RECORD IS VARYING (13.18.43) is a 2002 introduction** (derive from the 2002 standard) — the 85 RECORD clause has
  only the CONTAINS forms; at 85 the VARYING phrase is rejected and `RECORD CONTAINS m TO n` drives
  `IsVaryingRecord`.
- **Format-2 in-place table SORT (D6) is a 2002 introduction** (derive from the 2002 standard): rejected at 85
  (file-format SORT is 85). Note also row 27: MERGE newly prohibited in another MERGE's output procedure / a
  file-format SORT input-output procedure at 2023.
- **OPTIONAL reach differs by edition** (85 restricts which organizations/open modes admit OPTIONAL; 2002 widens
  it) — the edge-case table above states the 2023 behavior; derive and gate the 85 subset from the 1985 standard.
- Gating lives in the BINDER off the ONE `DialectMode` (SSOT §14.11): a construct the target edition lacks never
  reaches the bound tree (it is a `COBOLNET-` diagnostic); the connectors/codec implement the union of editions, and
  per-edition BEHAVIOR variants (e.g. READ PREVIOUS after OPEN) are bound-tree/runtime-parameterized by edition,
  never duplicated per backend. The NIST corpus compiles at `--std` 85 and is the 85 positive net; the
  rejected-construct negative cases are version-test-matrix work, not NIST work.

## ISO citations

- ISO/IEC 1989:2023 section 9.1.13 I-O status: two-character codes; first digit 1 at-end, 2 invalid-key, 3 4 7 9 fatal or exception; subsections 9.1.13.2 through 9.1.13.11 enumerate every code (00 02 04 05 06 07 09 10 14 21 22 23 24 30 34 35 37 39 41 42 43 44 46 47 48 49).
- Section 9.1.2 record area plus its NOTE: all 01s under an FD or SD implicitly redefine the same storage area per 13.18.33 GR3.
- Section 9.1.1: CODE-SET or FORMAT translation occurs only when a logical record transfers to or from the physical unit; padding is added or deleted as necessary.
- Sections 13.18.13 CODE-SET, 13.18.34 LINAGE, 13.18.43 RECORD VARYING DEPENDING ON, 13.18.41 implicit default record.
- Sections 14.9.30 READ, 14.9.35 REWRITE, 14.9.41 START, 14.9.51 WRITE, 14.9.10 DELETE and DELETE FILE, 14.9.24 and 14.9.45 SORT and MERGE, 14.9.40 and 14.9.22 numeric-key value compare, 8.8.4.1.2 alphanumeric compare with shorter-operand space-extension, 14.6.6 USE declarative.

## Open questions — RESOLVED (`COBOLNET_DESIGN.md` §15 #3 + §18, owner-confirmed 2026-06-08)

> Status of each: **Q6** — settled, native `long`/`Int128` (see below). **Q7** — settled by §18 #1: a `byte[]` is
> sanctioned ONLY at the external-medium boundary, serialized via `IRecordCodec`; the connector's confined byte-image
> payload IS that boundary, and the all-typed store remains a one-line flip. **Q1/Q2** — stand as the owner-reviewed
> mechanical defaults (SSOT §15 #3 Q-file-1/-3): internal framed format + in-memory load/flush for v1, a pluggable
> file-format provider / on-disk backend later. **Q4/Q5** — see the edge-case notes above.

- Q6 cross-subsystem foundational — **RESOLVED** (`COBOLNET_DESIGN.md` §18 #2/#3/#19/#22, owner-confirmed 2026-06-08): the numeric substrate is native unscaled `long` (≤18 digits) / `Int128` (19–38 digits), never `decimal`; `COBOLNET_ARCHITECTURE.md`'s decimal rows were corrected in the same change set. The files codec is designed to exactly that lock — no conflict remains.
- Q7: the relative and indexed connector holds the file on-disk image as a byte array per record in memory (payload only; deserialized to the typed record only on hand-back; ordering by the typed CobolKey). This is the exact spot the owner objected to for program data. Accept the legitimate-confined-file-bytes framing, or switch to an all-typed sorted-dictionary-of-typed-records store? Lean: byte-image payload; a one-line flip if the owner prefers all-typed.
- Q1: the on-disk format for relative, indexed, and variable-sequential files is an internal framed convention (4-byte little-endian prefix, 0xFF gaps), not a standard interchange format. Keep for v1, or does commercial quality demand an interoperable format such as a real ISAM or GnuCOBOL-compatible layout? Recommendation: keep plus add a pluggable file-format provider later.
- Q2: indexed and relative persistence loads the whole file into memory on OPEN and flushes on CLOSE, fine for batch and NIST but not for multi-gigabyte files. Scope v1 to in-memory plus a later pluggable on-disk B-plus-tree or SQLite-backed backend?
- Q4 and Q5 minor: implement LINE SEQUENTIAL status 06 and 09 now or defer them to the post-85 feature drive (`docs/ISO2023_CONFORMANCE_PLAN.md`)? Confirm SAME AREA buffer-only and SAME SORT-MERGE AREA are acceptable no-ops in a managed runtime.
