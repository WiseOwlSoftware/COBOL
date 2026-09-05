# COBOL.NET — File I/O (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §8; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Deep, decision-complete design for the COBOL.NET FILES subsystem (typed records, clean byte boundary). Core architecture: the FD or SD record IS a .NET record struct (the record area is a typed field, not a byte buffer); the only bytes are at the on-disk edge, produced by a compiler-GENERATED per-layout codec (Serialize and Deserialize) running only at READ and WRITE. CODE-SET is one Encoding parameter threaded into that codec. The proven 364-NIST legacy handlers are ported VERBATIM for control logic (open-mode tables, ISO-cited status codes, the file-position-indicator plus key-of-reference plus duplicate-arrival state machines) but re-substrated from a byte array to a generic FileConnector plus an IRecordCodec. Covers all organizations (SEQUENTIAL, LINE SEQUENTIAL, RELATIVE, INDEXED), all access modes, OPEN CLOSE READ WRITE REWRITE DELETE START, FILE STATUS as a two-char string item, variable-length records, prime and composite ALTERNATE keys, SAME RECORD AREA, SORT and MERGE, and LINAGE. Ordering and lookup keys are a typed-derived CobolKey comparable (numeric by decoded value, alphanumeric by image plus collating, composite component-wise), decoupled from the stored payload; one comparison policy shared by indexed files and SORT.

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

> **Realization:** the shared area is realized as a synthesized REDEFINES class over the 01s (the
> REDEFINES deep-dive's tiers), not a separate wrapper type — the file edge and REDEFINES share ONE overlay
> mechanism (singular-pattern). With a fixed-point BINARY/PACKED leaf among the records (ST134A's SAME RECORD AREA
> pair) the class is Tier B: one string backing IS the record area (§12.4.6.4.4 GR2 — "an implicit redefinition of
> the area, with records aligned on the leftmost byte position"), each record/leaf a window accessor, binary leaves
> image-stored with the overpunch profile rewrite (REDEFINES deep-dive D10).

### D6. SORT and MERGE: the SD record is a typed struct; the sort store holds serialized images ordered by the same CobolKey policy; SORT key offsets are computed into the deterministic serialized image at compile time. Format-2 in-place table SORT operates on the typed array directly.

**Rationale.** Reuses the proven stable sort plus numeric-value-versus-collated-image key rule (14.9.40 SORT / 8.8.4.2.4 numeric compare; DUPLICATES IN ORDER is a stable sort). Table SORT data is already typed in memory so it diverges to a typed comparer, the one documented place the two SORT forms differ.

**Rejected alternatives.** A true k-way priority-queue merge (a perf optimization, deferred); a byte comparer for table sort (the data is already typed).

### D7. The serialized record image of a MIXED-USAGE record (fixed-point BINARY/PACKED leaves beside string-stored leaves) is the generated `AsImage()`/`FromImage()` character image — each such leaf contributes its fixed-width ZONED DIGIT image (`Pic.Digits` chars, trailing-overpunch sign), per `PicInfo.ImageSignKind`.

**Rationale.** ISO §13.18.60 USAGE GR4 makes a binary item's representation — including the algebraic sign — implementor-defined; `COBOLNET_DESIGN.md` §14.4's total digit-image rule defines it ONCE for every character context, and the record codec rides the same definition (singular-pattern: `IRecordCodec` for these records IS the generated pair — WRITE/RELEASE send `AsImage()`, READ/RETURN distribute via `FromImage()`; `SequentialIoEmitter.EmitImageInto`, `SortBinder.SortRecordOf`). The SORT/MERGE consequence is load-bearing: a signed COMP/COMP-3 key's compile-time descriptor (`BoundSortMergeKey.SignKind`) carries the IMAGE sign (`ImageSignKind` — trailing overpunch), never the leaf's stored `BinaryMinus`, so `CobolSort.NumericKey` decodes the zoned window algebraically (§14.9.40 GR8 / §8.8.4.2.4 — negatives before positives; the failure mode of decoding with `BinaryMinus` is SILENT: everything sorts, negatives order positive). Key offsets/lengths are already computed in digit-image coordinates (`SortOffsetInRecord`/`SortPhysicalWidth` sum `ImageWidth`) — no width changes anywhere. The on-disk record width for such records legitimately DIFFERS from the legacy's raw-byte form (e.g. ST133A: 80 chars where the legacy wrote 72 + 4 binary bytes) — chains are same-engine; cross-engine file compatibility is explicitly not required. Float (COMP-1/2), COMP-5 (BinaryCapacity exceeds the digit-count window), and INDEX leaves keep a record OUTSIDE the codec — the loud Tier-C island. Proven by ST108A/ST127A/ST133A/ST134A + `MixedUsageRecordImageDifferentialTests`.

**Rejected alternatives.** Raw big-endian bytes as Latin-1 characters (the legacy on-disk form): a SECOND representation for the same concept — compare/DISPLAY contexts already use digit images, so `RETURN`-then-compare would disagree with `IF group = group` about what a record "looks like"; also needs a new Binary decode kind in `CobolSort.Key` and control characters through every string seam, buying only a cross-engine compatibility that is not required. Leading-separate-sign images (width = Digits+1 — would change `ImageWidth` and every offset computation).

### D8. The RETRY phrase and the conflict-status class rule: an exhausted retry lands the CONFLICT'S OWN §9.1.13 status, decided in ONE place, and the timeout period's maximum meaningful value is defined as ZERO.

**The rule.** ISO §14.7.9.3 GR4 scopes the whole RETRY discipline: it engages only "if the I/O operation is
unsuccessful on the first attempt **because of a file sharing conflict condition or a record operation conflict
condition**". Every other outcome — success, and every other unsuccessful status — is the statement's own answer
and the phrase does not touch it. When the discipline does engage, GR4a (no phrase, or an arithmetic-expression
evaluating negative or zero) and the clause's closing paragraph give the *same* landing: "the appropriate value is
placed in the I-O status associated with the file connector according to the rules for 9.1.13". The status is
therefore **a function of the conflict's own class**, never a literal chosen at a call site.

**The class asymmetry is deliberate and load-bearing.** §9.1.13.9 (file sharing conflict) defines exactly two
values — `61` for OPEN and `62` for DELETE FILE — and **no deadlock value**, so a file-sharing conflict has no
conforming landing but its own; §14.9.10.4 GR15b is imperative there ("*The* value … is placed") where its
record-conflict twin GR6b says only "*A* value". §9.1.13.8 item 2's `52` is a **record**-conflict value whose
detection conditions the implementor defines. COBOL.NET detects a deadlock in exactly one circumstance: a
`RETRY FOREVER` waiting on a record locked by another file connector (§9.1.13.8 item 1). That holder is inside the
executing run unit and cannot release while this statement runs, so GR3's "until the input-output operation has
been completed" would never terminate — which is what makes it a deadlock rather than a timeout. Harmonizing the
two classes breaks conformance in one direction (a `52` for a file conflict) or two green goldens in the other.

**Why the class matters beyond the digits.** §9.1.13.1 keys the exception-name on the status's first digit: `5` →
`EC-I-O-RECORD-OPERATION`, `6` → `EC-I-O-FILE-SHARING`. Answering `52` for a file-sharing conflict raises the
*wrong* condition, so a USE declarative or exception-checking PERFORM keyed on `EC-I-O-FILE-SHARING` silently does
not fire. That is why this is a wrong-answer defect and not two cosmetic digits (kb/Work PB142).

**The GR2 determination (Annex A.1 item 166).** §14.7.9.3 GR2 requires the implementor to specify the timeout
temporary's picture `9(n)V9(m)` and the **maximum meaningful value** of arithmetic-expression-2. COBOL.NET defines
**n = 1, m = 0, maximum meaningful value = 0**. The ground is structural, not a convenience: every file and record
lock here is held by a file connector *of the executing run unit*, and a connector cannot release one while another
statement of the same run unit is executing, so no positive timeout period can change the outcome — a sleep would
only delay an identical answer. GR2 therefore clamps every SECONDS amount into a zero-length timeout period, GR4b's
"attempts as specified in General rule 2" performs none, and the closing paragraph lands the conflict's own status
— the same answer GR4a gives for a zero or negative expression. `RETRY FOR 0 SECONDS` and `RETRY FOR 30 SECONDS`
are thus correct **by one rule** rather than by a special case, which is the observable form of the determination
and is pinned as such in `2023/delete_file_sharing` (`DELSC0` / `DELSC30`). ⚠ Do not "fix" this into a
`Thread.Sleep`: that would hang a program for a guaranteed failure. The determination and its ground are recorded
in `docs/CONFORMANCE.md` §7 under A.1 items 165/166; the deadlock detection conditions are item 109.

**The two roundings are different rules and must stay separate in code.** GR1 rounds the TIMES count **up to the
next whole number** (`RETRY 1.5 TIMES` is two re-attempts), which is one of only two clauses in the standard that
say so — the other is ALLOCATE's §14.9.3.4 GR1 — and both render through the single
`NumericRenderer.AlignRoundedUp` (see `COBOLNET_NUMERIC_DESIGN.md`). GR2 instead stores the SECONDS period through
an implicit COMPUTE *without* the ROUNDED phrase, i.e. truncation at the implementor's `m`. One clause, two arms,
two roundings.

**No compile-time screen exists, and none may be added.** §14.7.9 has only 14.7.9.1/.2/.3 — General, General
format, General rules — and **no Syntax rules clause at all**. A non-integer or out-of-range arithmetic-expression
is LEGAL SOURCE that GR1/GR2/GR4a define at run time; rejecting it would be a `rejects_legal_source` regression.
`StatementBinder.BindRetry` binds the three forms with no screen and must stay that way.

**Where it lives.** `FileRegistry.RetryLoop` gates on `IsConflict` (§9.1.13.1's own first-digit classification) and
routes every landing through the private `ExhaustionStatus` — the ONE place the rule is written. Its six callers
(`DeleteFile`, `OpenShared`, `ReadLockGovern`, `ReadShared`, `RewriteShared`, `DeleteShared`) inherit it rather
than each judging for themselves; `WriteShared` deliberately takes the phrase and discards it (§14.9.51 GR16 —
no record-operation conflict is defined for WRITE). The drift test is
`CobolFileLockTests.RetryLoop_LandsTheConflictsOwnStatus_ByClass`, which asserts every retry-form × conflict-class
cell including the not-a-conflict row.

### D9. The L1–L3 phrase-placement leniency family is gated at ONE seam: an error under strict, a warning with an unchanged bind under `--permissive`.

**The rule shape.** Three syntax rules across READ/REWRITE/DELETE close a phrase out of a particular organization
or access mode:

| rule | forbids | when |
|---|---|---|
| §14.9.10.3 SR2 | INVALID KEY / NOT INVALID KEY | a DELETE RECORD referencing a file **in sequential access mode** |
| §14.9.35.3 SR2 | INVALID KEY / NOT INVALID KEY | a REWRITE referencing **a file with sequential organization**, *or* a file with **relative organization and sequential access mode** |
| §14.9.30.3 SR6 | ADVANCING / AT END / NEXT / NOT AT END / PREVIOUS | a READ whose file control entry specifies **ACCESS MODE RANDOM** |

**What was wrong.** All three were bound unconditionally with a "tolerated in the default (CCVS-lenient) mode"
comment and **no strict arm**, so at `--std 2023` strict the compiler accepted source the standard forbids and
emitted nothing — measured with CLI probes, not inferred. ⚠ §14.9.35.3 SR2 has **two arms** and only the second
was even commented: the sequential-*organization* arm binds through `SequentialIoBinder.BindRewrite`, which never
read `rewriteInvalidKeyPhrase()` at all — the phrase was parsed and dropped on the floor, a strictly worse shape
than its relative twin, which at least bound it as dead. Both arms are screened now, and both have a negative
fixture, because a fix landing only one of them reproduces the very shape that made the rule wrong.

**Where it lives.** `StatementValidation.ScreenForbiddenPhrase` is the one screen; the severity decision routes
through **`EditionContext.Removed`**, THE policy seam — which already carries documented-dialect-leniency gating
as well as removed-construct gating — so it is an ERROR under strict and a WARNING with an **unchanged bind**
under `--permissive`. Never a local `Permissive` test, never a parallel `Lenient()` method. ⛔ The legacy
`DialectStrictnessChecks` lives only in `src/CobolSharp.Compiler` and must not be revived.

**Why the tolerated path is safe.** The bind is unchanged under `--permissive` because the emitter's status-first
branches make a phrase that cannot fire simply dead — a `'2x'` invalid-key branch on a sequential-access DELETE,
a `'1x'` at-end branch on a random READ — never silently rerouted. That is what the CCVS-85 corpus depends on.

**One code, three rules.** `COBOLNET1720` serves all three, on the `COBOLNET1694` precedent: the *shape* is one
rule ("a phrase is written where this statement's syntax rules forbid it") and each site's message quotes its own
§/SR. §14.9.10.3 **SR1** (DELETE RECORD on a sequential-organization file) is deliberately **not** on this seam —
it is a hard `COBOLNET0865` error at every edition and strictness, because it is not a documented leniency.

**The edition-gate sweep was RUN, not assumed** (gating a construct breaks everything compiling it below that
edition). The rules are edition-invariant — `Removed` keys on `Permissive`, never on edition — and the probe
emits an identical diagnostic count at 85/2002/2014/2023. The corpus risk was the real one: a static scan finds
**315** `REWRITE … INVALID KEY` and **28** `DELETE … INVALID KEY` sites in the NIST programs, and NIST runs
**strict** by default. Compiling all **459** at `--std 85` produces the **same 17 pre-existing failures** (every
one read individually: `CM*` COMMUNICATION SECTION parse errors and `DB*` `COBOLNET1571` debug-module rejections)
and **zero** new ones — every one of those 343 phrase sites is on an organization/access mode where the phrase is
LEGAL. The harness was proved able to surface `COBOLNET1720` as a failure before that zero was trusted.

### D10. A physical file's §9.1.6 FIXED FILE ATTRIBUTES are persisted in a SIDECAR beside the data file, and OPEN compares against it (§14.9.27.4 GR10 → '39').

**The rule.** §9.1.6: a physical file's organization, key geometry, code set, logical record sizes, record type,
key collating sequence, physical record sizes and record delimiter "apply to the file at the time it is created
and cannot be changed throughout the lifetime of the file". §14.9.27.4 GR10 makes the OPEN statement compare the
connector's declared attributes with the file's and set I-O status **'39'** (§9.1.13.6 item 7) when they differ,
and delegates WHICH attributes are validated to the implementor — a **required, required-to-be-documented**
determination (Annex A.1 item 129).

**What was wrong.** The host-file model carried no record of a file's attributes at all, so the validated set was
empty *by omission*: a program that opened a file under a contradicting FD read **silently wrong data** with
status '00' (measured — a RELATIVE file of 10-byte records reopened INPUT through a LINE SEQUENTIAL 40-byte FD
delivered an empty record). That is the opposite of D-E's DELETE FILE answer, and deliberately so: a DELETE FILE
destroys the file, so validating nothing costs the program nothing (A.1 item 50's set IS empty, by definition —
`FileRegistry.ValidateFixedFileAttributes`), whereas everything after an OPEN — record length, key structure,
code set — is read back *through* the description being validated.

**The validated set VARIES BY ORGANIZATION — GR10's third sentence says it may.** "The implementor defines
which of the fixed-file attributes are validated during the execution of the OPEN statement. The validation of
fixed-file attributes may vary depending on the organization or storage medium of the file." COBOL.NET
validates exactly what the file's own storage FIXES — the attributes a disagreeing file description could not
read the file back through.

- **Every organization — the ORGANIZATION itself.** §9.1.6's "primary attribute", of which §9.1.6 names
  exactly three: "There are three organizations: sequential, relative, and indexed". So the recorded value is
  SEQUENTIAL, RELATIVE or INDEXED and nothing else, and §9.1.7.2's record-sequential/line-sequential
  distinction is NOT a fourth organization — it is §9.1.6's separately listed *record delimiter*.
- **RELATIVE and INDEXED — additionally the record type, the minimum and maximum logical record size, and
  (indexed) the key descriptors.** Those two organizations live in an implementor-defined store —
  `RecordFraming`'s framed whole-store layout, addressed by relative record number or by key value — whose
  STRUCTURE is those attributes; a description that disagrees cannot interpret the store at all.
- **SEQUENTIAL — nothing further, and that is a determination, not an omission.** §9.1.7.2 puts a sequential
  file's record lengths in the DATA and in the READING program, not in the file: "In record sequential files
  the length of each record is determined by any information the implementor may add to the record on the
  physical storage medium (such as record length headers)" — and COBOL.NET adds none to a fixed-length record
  sequential file, which is plain bytes — while "In line sequential files the length of each record is
  determined by the number of characters between the preceding line delimiter and the following line delimiter
  or the end of file if no line delimiter is present". The standard then answers every disagreement such a
  re-read can produce with a SUCCESSFUL completion rather than a refused OPEN: §9.1.13.2 item 3's '04', item
  5's '06' and item 7's '09'. Writing a print, report or extract file and reading it back under a different
  record description is the idiom those three statuses exist for — measured on this corpus, a uniform set
  broke six conforming programs, one of them into an infinite READ loop — so a '39' there would reject legal
  source, and it would also be a SECOND MECHANISM for the job `RecordLayoutNotice` already does.

**A sidecar, not a header.** `FixedFileAttributes.SidecarPath(host)` is the data file's own path with `.cbattr`
appended. A header inside the data file was rejected on two counts: line-sequential files would stop being plain
text (the interchange property that shape exists for), and every data file written by an earlier build would
become unreadable. The sidecar is additive, travels with the file, and is removed with it — `FileRegistry`'s
DELETE FILE drops it, so a catalog can never outlive its file and judge a different one later created at the same
path. Its format is a versioned `key=value` text file; an unknown key is ignored, so a later build may record a
new attribute without making its files unreadable to an older one.

**For indexed files the key half is not latitude at all.** §12.4.5.12.4 GR3 requires a prime key's data
description and its relative location within the record to "be the same as that used when the file was
created", and §12.4.5.6.4 GR3 says the same of every alternate key and of the NUMBER of alternate keys.
Neither states a consequence where it is written — GR10's conflict condition is the mechanism that detects a
violation — so recording the key descriptors is what gives those two rules an effect at all. The recorded
descriptor is the key's byte window plus its collating sequence, and that is the whole of "the data
description … as well as their relative location" this implementation can act on: §12.4.5.12.3 SR2 confines
a record key to category alphanumeric or category national, §12.4.5.12.4 GR1 makes key equality a relation
condition under the file's collating sequence (recorded, by weights), and both native sequences are one
code-unit ordinal over the UTF-16 substrate — so two descriptions sharing a window and a sequence order every
key value identically whatever their category.

**Where the two halves live, each in ONE place.** `FileConnector.Open` performs GR10's comparison — before
`OpenCore`, because GR25 leaves an unsuccessful OPEN's file unaffected and the OUTPUT/creation arms truncate —
and both OPEN dispatch arms reach it (the plain `FileRegistry.Open` and the sharing-active
`SharedOpenAttempt`). `FixedFileAttributes.Conflicts`, with `FixedFileAttributes.MediumFixesRecordLayout` — the one place GR10's
"may vary depending on the organization or storage medium" is exercised — is the whole definition of the
validated set, the twin of `ValidateFixedFileAttributes` for GR19, and the thing `docs/CONFORMANCE.md` row
`DOC-A.1-129` documents.
`FileConnector.DeclaredAttributes` assembles a connector's declared set ONCE for every organization (record type
and record-size bounds from the RECORD clause); the organizations supply only `CatalogOrganization` and, for
indexed, `CatalogKeys`.

**When the attributes are established** is exactly the two cases the OPEN statement CREATES the file: GR18 ("If
the OUTPUT phrase is specified, the successful execution of the OPEN statement creates the file") and GR17 (an
absent OPTIONAL file opened I-O or EXTEND is created as if OPEN OUTPUT / CLOSE). So an **OPEN OUTPUT is never
judged against the previous file's attributes — it replaces them**, which is what §9.1.6's "at the time it is
created" means and what the surveyed implementations do. An absent OPTIONAL file opened INPUT is *not* created
(Table 18) and records nothing.

**A file with no recorded attributes is not a conflict, and must never become one.** GR10 compares against "the
fixed file attributes of the file"; a file written by an external tool, or by a build older than the catalog,
states none, so nothing about it is validated. Rejecting it would turn a missing implementor artifact into a
rejection of legal programs. `RecordLayoutNotice` stays the arithmetic fallback for exactly that file — a stderr
notice when a fixed-length record-sequential file's byte length is not a whole multiple of its record length,
leaving the I-O status alone. The two are complementary, not duplicates, and the boundary is the ORGANIZATION
rather than the recording: GR10's '39' answers the disagreements a re-read cannot recover from (a relative or
indexed store opened as a byte stream, a different-organization description), and the notice answers the one
it can — a sequential record-size disagreement — on a recorded file as much as on an unrecorded one, because
the sequential validated set stops at the organization. One job, one mechanism, on either side of that line.
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

### REWRITE record-length rules are organization-dependent: a RECORD SEQUENTIAL REWRITE must equal the replaced record's length (14.9.35 GR16) or it is status 44; a RELATIVE or INDEXED REWRITE may change length (14.9.35 GR18) but must stay within the RECORD IS VARYING bounds (14.9.35 GR20) or it is status 44. On any 44 no logical updating takes place and the record area is unchanged (14.9.35 GR14).

For a record-sequential file the connector remembers the last-read frame start and length; REWRITE re-serializes, compares the serialized length to the remembered length, and returns 44 when they differ (GR16 — the in-place frame cannot change size). For a relative or indexed file the record length is allowed to differ (GR18), so REWRITE checks only that the serialized length lies within the file's RECORD IS VARYING minimum/maximum, returning 44 otherwise (GR20).

### Read-position state machine: a sequential READ (NEXT or PREVIOUS) issued after a prior unsuccessful sequential READ is 46; a sequential REWRITE or DELETE without a preceding successful READ is 43; START establishes an inclusive file-position indicator.

Port the proven per-connector last-read-unsuccessful, past-end, prev-op-was-successful-read, and read-next-inclusive flags. An at-end READ is itself an unsuccessful READ (14.9.30 GR24 — "when the at end condition exists, execution of the READ statement is unsuccessful"), so once the last-read-unsuccessful flag is set the NEXT sequential READ — whether READ NEXT or READ PREVIOUS — is unsuccessful with status 46 (14.9.30 GR21); it does NOT re-expose the last record. A sequential REWRITE or DELETE with no immediately preceding successful READ is status 43 (14.9.35 GR5).

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
- LINAGE:
  - **Counter-only physical model** (13.18.34 GR8 — "each logical page is contiguous to the next with no additional spacing"): the connector's pending-advance print stream is UNTOUCHED (no margin blank lines, nothing at page wrap; ADVANCING PAGE stays one `\f`); the whole feature is `SequentialFile`'s counter machine + `EndOfPage` flag + the LINAGE-COUNTER register. An ADVANCING or LINAGE file is forced to line-oriented output (a LINAGE file's plain WRITE reroutes to the advance-1 print path from its FIRST write — 14.9.51 GR25 / 13.18.34 GR7c3).
  - **One evaluator closure for both operand forms** (13.18.34 GR6): the emitter registers `CobolFile.SetLinage(name, () => (body, footing, top, bottom))` right after `Register` — literals are constant lambdas (GR6a); data-names read their program fields at call time (GR6b). The connector invokes it at OPEN OUTPUT (GR6b1, with counter := 1 per GR7d) and at the two page transitions — the ADVANCING-PAGE reset (GR6b2) and the overflow wrap (GR6b3), AFTER the overflow decision against the OLD body, because "the value applies to the next logical page". Evaluating data-names at these page transitions (not only at OPEN) is required by GR6b2/GR6b3 — evaluating solely at OPEN is a conformance hole (SQ208M/SQ210M).
  - **GR26 operational mapping** (the legacy `AdvanceLinageCounter` ported verbatim, proven over the SQ goldens): with post-advance counter c, body B, footing F — `c > B` ⇒ overflow end-of-page, counter := 1 (GR26a + GR7c4; the reposition lands on line 1, never a modulo carry); else `F > 0 ∧ c ≥ F` ⇒ footing end-of-page (GR26b; the footing area is [F, B] INCLUSIVE per GR3, so c == B is FOOTING, and overflow fires only when positioning passes the body). ADVANCING PAGE: counter := 1, no observable EOP (SR18 bars PAGE+EOP). The counter advances in the CONNECTOR as part of EVERY write (EOP phrase or not), after the physical presentation — an AT branch reads the post-advance counter.
  - **LINAGE-COUNTER register** (8.4.3.14): runtime-sourced (`BoundLinageCounterRef` → `CobolFile.LinageCounter(name)`), never a synthesized storage item (only the IOCS modifies it, GR7b); qualified `OF/IN file-name` resolves via the grammar's dedicated alternative, unqualified requires exactly one LINAGE file (SR3/8.4.2.2, ambiguity is a bind-time diagnostic). `ReferenceResolver.Resolve` returns null for the register early (the qualified form's cobolWord is the FILE-name).
  - **END-OF-PAGE phrases** branch on `CobolFile.EndOfPage(name)` read in the `if` HEADER (a branch body may WRITE the same file — SQ208M); EOP is a SUCCESSFUL write (GR27a — status 00, no USE hook competition). Bind-time diagnostics: SR19 (EOP without LINAGE — the old silent-drop), SR18 (PAGE+EOP), SR13 (mnemonic ADVANCING on a LINAGE file).
  - **EC-I-O-LINAGE seam** (GR6 value rules): the evaluator validates body > 0 and 0 < footing ≤ body (footing 0 = absent phrase) and throws LOUD until the EC subsystem lands — never a silent bad page model.
  - Conformance net: `LinageConformanceTests.cs` (per-GR: GR7c1–c4/GR7d, GR26a/b discrimination incl. c==B, GR6b1/2/3 timing, GR1 no-footing, qualified/ambiguous register, ADVANCING 0, SR13/18/19).
- On-disk framing: a fixed record-sequential file is contiguous; a variable sequential, relative, or indexed file uses a 4-byte little-endian length prefix; a sparse relative file uses 0xFF gaps.
- DELETE FILE statement (14.9.10 Format 2): delete the host path and reset the in-memory map. A present file that is deleted gives 00 and an absent file gives 05 — BOTH successful (14.9.10 GR14/GR20); the error paths are 41 (the connector is still open, GR13), 62 (the physical file is open by another connector, GR15), and 37 (insufficient authority or the storage medium forbids deletion, GR16/GR17). A missing file is NEVER 35 — 35 is an OPEN-only status.
- Keyed record stores are PER PHYSICAL FILE, not per connector (kb/Work PB143; §14.9.10.4 GR5 — "removed from
  the physical file"): `KeyedStoreTable` (registry-owned, keyed by resolved host path — the same key
  `PhysicalFileTable` arbitrates sharing and locks by) holds ONE `RelativeStore` (RRN → image) or `IndexedStore`
  (arrival-ordered records + the GLOBAL arrival mint) per host. The FIRST opener loads from disk; later openers
  ATTACH to the live store (never reload — the in-memory store is the truth while any connector holds it); every
  DELETE/WRITE/REWRITE is instantly visible to every attached connector; any CLOSE persists the one shared state
  (close order cannot resurrect a deleted record or drop another connector's write); the LAST detach drops the
  entry so a later OPEN re-reads the disk; OPEN OUTPUT empties the shared view. Position/key state (FPI, key of
  reference, the §14.9.51 GR29a sequential-WRITE slot, the GR38 high-key) stays per-CONNECTOR. The store is
  unconditional — two SELECTs to one ASSIGN target reach it with no SHARING clause — and sequential connectors
  are out of its scope (their OS-backed streams are already the shared store). A connector constructed outside a
  registry (a focused unit test) keeps a private store.
- I-O status discipline (kb/Work PB140): `FileConnector.Status`'s setter is the ONE assignment path — it records `EverAccessed` AND drops the §9.1.13.7 3) '43' gate (READ terminals re-arm through `ReadSucceeded`); openness is the ONE base `_openMode` bit, separate from the `OptionalAbsent` file-position state a CLOSE leaves unchanged (§14.9.6.4 GR6); `FileRegistry` throws on an unregistered or misrouted name (never a fail-open '00' — the SD/organization screens reject at bind time, COBOLNET1692/1693); `FileConnector.Close` maps OS failures to '30' (§9.1.13.6 item 1) with the sequential streams nulled either way; CLOSE WITH LOCK locks only on a successful close.
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

## Declined optional elements of the I-O statements and the file description entry

Two Annex A.4 modules of the file-handling surface are OPTIONAL language elements (§4.2.7) for which this
implementation claims NO support, per the owner's documented-non-support decision recorded in
`docs/CONFORMANCE.md` §5. They are **recognized and refused by name at bind**, never parse-errored and never
compiled inert:

| Module | Elements | Where it parses | Refusal |
| --- | --- | --- | --- |
| A.4.8 | FORMAT clause (§13.18.24) on an FD; SELECT WHEN clause (§13.18.51) on a record description entry | `formatClause` in `fileDescriptionClause`; `selectWhenClause` in `dataDescriptionClause` (`Core/CobolData.g4`) | `COBOLNET1705`, from `DataBinder` — the FD clause loop and `BindEntry` |
| A.4.13 | the `FILE file-name-1` alternative of WRITE (§14.9.51.2, **both** formats) and REWRITE (§14.9.35.2) | the `FILE fileName` alternative of `writeStatement` / `rewriteStatement` (`Core/CobolIO.g4`) | `COBOLNET1706`, from `SequentialIoBinder.DeclinedFilePhrase` — ONE site, both verbs |

**Why recognize-and-refuse, and why an ERROR rather than the accept-inert band.** Annex A.4.1: *"An
implementation shall accept the syntax and provide the functionality for an optional element only when
support for that language element is claimed by the implementor."* Unclaimed ⇒ the syntax is not accepted. A
parse error would satisfy that letter but not the product bar — it names nothing the user can act on, and it
is what the traceability inventory refuses to close a DOCUMENTED-NON-SUPPORT row against. So the grammar
carries the full printed general format (rendered from the PDF, not read off the OCR) purely so the binder
can name what it saw.

The severity is the point of difference from `COBOLNET1560/1578/1579/1580`, which ACCEPT their facility inert
and warn. Those facilities are **additive** — a program means what it says with the screen, message, commit
or validate behavior simply absent. These two are not: an inert FORMAT changes which bytes reach the medium
(§13.18.24.4 GR1 — external media format), an inert SELECT WHEN selects the wrong record description entry
(§13.18.51.4 GR1/GR2, with the §9.1.13.7 rule-5 status-45 path), and the FILE phrase carries its own
implicit-record model (§14.9.51.4 GR8, §14.9.35.4 GR9) that a whole-record-area write does not implement.
Compiling them inert is a **wrong answer**, not a missing facility.

**One mechanism.** Every A.3/A.4 decline — accept-inert and refuse alike — routes through
`EditionContext.Declined(descriptor, seen)`, and the DESCRIPTOR's severity chooses the disposition. There is
no local strictness test at any site. `--permissive` does **not** move an A.4 decline in either direction: it
is the migration seam for constructs an edition REMOVED (`EditionContext.Removed`), and a declined optional
element has no pre-removal semantics to preserve.

**Edition posture.** The decline is edition-INVARIANT — the module is unclaimed at 85, 2002, 2014 and 2023
alike, and the negative witnesses carry `*> reject-at: 85 2002 2014 2023`. One lexical consequence differs at
85: §8.9 reserves `FORMAT` only from 2002 (`ReservedWords.Table`, `r85=false`), so at `--std 85` the word is a
legal user-defined name. That is preserved by the `nameSlot` row in `tests/version-matrix/cobol-words.json`
— measured: `01 FORMAT PIC X(4).` plus a subscripted `FORMAT-E (2)` compiles and RUNS at `--std 85`, and
draws `COBOLNET0901` by name at 2002/2014/2023 through the existing §8.9 funnel. No `constructs.json` row is
owed: that register's contract is source that COMPILES CLEAN at its introducing edition, which a permanently
declined element never does — the same reason VALIDATE, the SCREEN SECTION, MCS and COMMIT/ROLLBACK have no
rows either.

**What is NOT declined.** A.4.1 NOTE 1: *"The higher-level constructs or cross-referenced topics are not
optional."* The WRITE and REWRITE statements themselves are mandatory, fully supported surface; only one
alternative of `{ record-name-1 | FILE file-name-1 }` is declined. `tests/conformance/85/`
`a413_declined_phrase_positive_control` is the run-and-compare witness for that half, and it also pins the
optional word `RECORD` of §14.9.35.2 (not underlined in the printed format, therefore optional) which this
grammar did not accept at all before 2026-09-02.

## ISO citations

- ISO/IEC 1989:2023 section 9.1.13 I-O status: two-character codes; first digit 1 at-end, 2 invalid-key, 3 4 7 9 fatal or exception; subsections 9.1.13.2 through 9.1.13.11 enumerate every code (00 02 04 05 06 07 09 10 14 21 22 23 24 30 34 35 37 39 41 42 43 44 46 47 48 49).
- Section 9.1.2 record area plus its NOTE: all 01s under an FD or SD implicitly redefine the same storage area per 13.18.33 GR3.
- Section 9.1.1: CODE-SET or FORMAT translation occurs only when a logical record transfers to or from the physical unit; padding is added or deleted as necessary.
- Sections 13.18.13 CODE-SET, 13.18.34 LINAGE, 13.18.43 RECORD VARYING DEPENDING ON, 13.18.41 implicit default record.
- Section 9.1.6 fixed file attributes (the attributes fixed when a physical file is created), with 14.9.27.4 GR10 (the OPEN statement's comparison against them and its '39'), GR17 and GR18 (the two cases in which the OPEN statement CREATES the file), 9.1.13.6 item 7 (the '39' status itself) and Annex A.1 items 50 and 129 (the two required, required-to-be-documented validated-set determinations — DELETE FILE's and OPEN's, which are not the same answer). See D10.
- Sections 14.9.30 READ, 14.9.35 REWRITE, 14.9.41 START, 14.9.51 WRITE, 14.9.10 DELETE and DELETE FILE, 14.9.40 and 14.9.24 SORT and MERGE, 8.8.4.2.4 numeric-key value compare, 8.8.4.2.7 alphanumeric compare with shorter-operand space-extension, 14.9.49 USE statement (declarative).
- Section 4.2.7 optional language elements and Annex A.4 (A.4.1 general + NOTE 1, A.4.8 items 1–2, A.4.13 items 1–2) — the declined-element section above; §13.18.24 FORMAT clause and §13.18.51 SELECT WHEN clause are the two A.4.8 clauses, §14.9.51.2 / §14.9.35.2 carry the A.4.13 FILE phrase.

## Open questions — RESOLVED (`COBOLNET_DESIGN.md` §15 #3 + §18, owner-confirmed 2026-06-08)

> Status of each: **Q6** — settled, native `long`/`Int128` (see below). **Q7** — settled by §18 #1: a `byte[]` is
> sanctioned ONLY at the external-medium boundary, serialized via `IRecordCodec`; the connector's confined byte-image
> payload IS that boundary, and the all-typed store remains a one-line flip. **Q1/Q2** — stand as the owner-reviewed
> mechanical defaults (SSOT §15 #3 Q-file-1/-3): internal framed format + in-memory load/flush for v1, a pluggable
> file-format provider / on-disk backend later. **Q4/Q5** — see the edge-case notes above.

- Q6 cross-subsystem foundational — **RESOLVED** (`COBOLNET_DESIGN.md` §18 #2/#3/#19/#22, owner-confirmed 2026-06-08): the numeric substrate is native unscaled `long` (≤18 digits) / `Int128` (19–38 digits), never `decimal`. The files codec is designed to exactly that lock — no conflict remains.
- Q7: the relative and indexed connector holds the file on-disk image as a byte array per record in memory (payload only; deserialized to the typed record only on hand-back; ordering by the typed CobolKey). This is the exact spot the owner objected to for program data. Accept the legitimate-confined-file-bytes framing, or switch to an all-typed sorted-dictionary-of-typed-records store? Lean: byte-image payload; a one-line flip if the owner prefers all-typed.
- Q1: the on-disk format for relative, indexed, and variable-sequential files is an internal framed convention (4-byte little-endian prefix, 0xFF gaps), not a standard interchange format. Keep for v1, or does commercial quality demand an interoperable format such as a real ISAM or GnuCOBOL-compatible layout? Recommendation: keep plus add a pluggable file-format provider later.
- Q2: indexed and relative persistence loads the whole file into memory on OPEN and flushes on CLOSE, fine for batch and NIST but not for multi-gigabyte files. Scope v1 to in-memory plus a later pluggable on-disk B-plus-tree or SQLite-backed backend?
- Q4 and Q5 minor: implement LINE SEQUENTIAL status 06 and 09 now or defer them to the post-85 feature drive (`docs/ISO2023_CONFORMANCE_PLAN.md`)? Confirm SAME AREA buffer-only and SAME SORT-MERGE AREA are acceptable no-ops in a managed runtime.
