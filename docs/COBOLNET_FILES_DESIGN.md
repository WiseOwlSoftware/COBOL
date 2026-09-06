# COBOL.NET — File I/O (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §8; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Deep, decision-complete design for the COBOL.NET FILES subsystem (typed records, clean byte boundary). Core architecture: the FD or SD record IS a .NET record struct (the record area is a typed field, not a byte buffer) — including for a file description entry written with NO record description entries, which §13.4.5.3 SR3 permits and which gets §14.9.30.4 GR6's implied entry synthesized at bind time (D18); the only bytes are at the on-disk edge, produced by a compiler-GENERATED per-layout codec (Serialize and Deserialize) running only at READ and WRITE. CODE-SET is one Encoding parameter threaded into that codec. The proven 364-NIST legacy handlers are ported VERBATIM for control logic (open-mode tables, ISO-cited status codes, the file-position-indicator plus key-of-reference plus duplicate-ordering state machines) but re-substrated from a byte array to a generic FileConnector plus an IRecordCodec. Covers all organizations (SEQUENTIAL, LINE SEQUENTIAL, RELATIVE, INDEXED), all access modes, OPEN CLOSE READ WRITE REWRITE DELETE START (CLOSE dispatching on the §14.9.6.4 GR2 physical-file category through Table 14 — D11), FILE STATUS as a two-char string item, variable-length records, prime and composite ALTERNATE keys, SAME RECORD AREA, SORT and MERGE, and LINAGE. Ordering and lookup keys are a typed-derived CobolKey comparable (numeric by decoded value, alphanumeric by image plus collating, composite component-wise), decoupled from the stored payload; one comparison policy shared by indexed files and SORT.

## Decisions

### D1. The FD or SD record is a typed record struct (the record area); there is no per-file byte buffer. Bytes exist only transiently in a compiler-generated codec at the disk edge.

**Rationale.** Owner lock: a COBOL record is a .NET record struct and file I/O is the only legitimate bytes. A generated per-layout codec keeps the model native and the serialization fast and inspectable in the generated source.

**Rejected alternatives.** Keep a byte-array record area (the banned legacy substrate); interpret PIC at runtime (slow, untyped).

### D2. Port the three proven legacy handlers control logic verbatim but re-substrate to a generic FileConnector plus IRecordCodec.

**Rationale.** That logic encodes hundreds of ISO 9.1.13 and 14.9.30-51 edge cases proven by 364 NIST tests; re-deriving it would re-discover every bug the legacy already fixed (stale alt-index, START inclusive-FPI, 43 and 46 read-position, ascending-WRITE 21, duplicate-key 02 and the GR26 duplicate order). It is orthogonal to the byte-vs-typed substrate; one connector per organization sharing one codec is the singular pattern.

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
routes every landing through the private `ExhaustionStatus` — the ONE place the rule is written. Its FIVE call
sites (`DeleteFile`, `OpenCore`, `ConflictOnLockedRecord`, `RewriteShared`, `DeleteShared`) inherit it rather than
each judging for themselves, and that is every RETRY-bearing verb: the two READ formats do NOT call it
separately, they both reach `ConflictOnLockedRecord`, which is the one §14.9.30.4 GR9 check (kb/Work PB340).
`WriteShared` deliberately takes the phrase and discards it, and the ground is NOT §14.9.51.4 GR16 alone: GR16
says only that RETRY controls WRITE "for the case where resources needed to write a record are locked by another
run unit", which cannot arise in a single-run-unit model. What rules WRITE out of §9.1.13.8 item 1 is that its '51'
is "an attempt to ACCESS a record" and the WRITE general rules define no '51' leg (GR33/GR42 state the invalid-key
checks ignore record locks), so the first attempt decides. ⚠ The §12.4.5.9 GR7 lock-CEILING statuses '53'/'54' a
WRITE WITH LOCK can raise are first-digit '5' and therefore inside `IsConflict`, but no re-attempt within one run
unit can clear a ceiling and `ExhaustionStatus` returns them unchanged under every form, so routing them through
`RetryLoop` would be behaviour-identical; they stay outside it deliberately.

**No site upstream of `RetryLoop` may restate its outcome.** Splitting the SECONDS arm off the FOREVER arm was a
one-line change to the code and a change to NOTHING ELSE, and five descriptions of the old arm survived it — in
`StatementBinder.BindRetry`, `FileLockBinder.BindVerbRetry`, `BoundTree.RetryKind`, `BoundKeyedIo.Retry` and
`FileRegistry.ConflictOnLockedRecord` — plus `constructs.json`'s `retry-phrase-2002` row, which is the SOURCE of
a generated registry. Each of those now POINTS here instead of naming a status, so the next change to the retry
rules cannot leave a description behind it (kb/Work PB346).

**The drift tests.** `CobolFileLockTests.RetryLoop_LandsTheConflictsOwnStatus_ByClass` asserts every retry-form
× conflict-class cell, including the negative-SECONDS rows GR4a names explicitly and the not-a-conflict row;
`RetryLoop_AttemptCount_FollowsGR1AndGR4` asserts the same rules on the axis a status cannot witness, the ATTEMPT
COUNT; and `ReadUnderEveryRetryForm_BindsGR9ToTheRetryRules_OnBothFormats` covers §14.9.30.4 GR9's own
delegation to §14.7.9 through BOTH read formats — until it existed, no test passed a RETRY phrase to a READ
at all. The corpus witness is `conformance:2023/pb346_read_retry_record_conflict` with its 2014 twin.

### D9. The L1–L3 phrase-placement leniency family is gated at ONE seam: an error under strict, a warning with an unchanged bind under `--permissive`.

**The rule shape.** Six rules across READ/WRITE/REWRITE/DELETE — five syntax rules and one general format —
close a phrase out of a particular organization or access mode:

| rule | forbids | when |
|---|---|---|
| §14.9.10.3 SR2 | INVALID KEY / NOT INVALID KEY | a DELETE RECORD referencing a file **in sequential access mode** |
| §14.9.35.3 SR2 | INVALID KEY / NOT INVALID KEY | a REWRITE referencing **a file with sequential organization**, *or* a file with **relative organization and sequential access mode** |
| §14.9.51.3 SR2 | INVALID KEY / NOT INVALID KEY | a WRITE referencing **a file with sequential organization** — stated as a format rule ("If the organization of the write file is sequential, format 1 shall be specified"), and Format 1 of §14.9.51.2 has no INVALID KEY bracket (⚠ that diagram is the whole prohibition, so it was **rendered from the PDF**, printed pages 785–786, not read off the OCR, whose known bias is toward falsely-restrictive syntax: Format 1 carries ADVANCING + END-OF-PAGE and no INVALID KEY, Format 2 the reverse) |
| §14.9.30.3 SR6 | ADVANCING / AT END / NEXT / NOT AT END / PREVIOUS | a READ whose file control entry specifies **ACCESS MODE RANDOM** |
| §14.9.30.3 SR7 | PREVIOUS | a READ referencing a file with **LINE SEQUENTIAL organization** |
| §14.9.30.2 Format 1 | INVALID KEY / NOT INVALID KEY | a READ referencing a file with **sequential organization** — every such READ is a Format-1 read (§12.4.5.5.2 SR2 → §14.9.30.3 SR8 → §14.9.30.4 GR19), and Format 1 has no INVALID KEY bracket |

**What was wrong.** All three were bound unconditionally with a "tolerated in the default (CCVS-lenient) mode"
comment and **no strict arm**, so at `--std 2023` strict the compiler accepted source the standard forbids and
emitted nothing — measured with CLI probes, not inferred. ⚠ §14.9.35.3 SR2 has **two arms** and only the second
was even commented: the sequential-*organization* arm binds through `SequentialIoBinder.BindRewrite`, which never
read `rewriteInvalidKeyPhrase()` at all — the phrase was parsed and dropped on the floor, a strictly worse shape
than its relative twin, which at least bound it as dead. Both arms are screened now, and both have a negative
fixture, because a fix landing only one of them reproduces the very shape that made the rule wrong.
⚠ **WRITE was the same drop a third time** (kb/Work PB691). `SequentialIoBinder.BindWrite` never called
`w.writeInvalidKey()` either, and unlike REWRITE it had no diagnostic at all: the program compiled clean at every
edition and on both severity axes, and neither imperative ran. It hid because the `writeInvalidKey` sub-rule of
the grammar was consumed by the **keyed** binder only — the two-arm dispatch with one arm fixed, which is this
repo's most reproducible defect shape. The mechanical sweep that finds it is *for every I-O statement rule, which
sub-rules does each binder arm consume?*, and it is now run whenever a phrase gains an organization-conditioned
arm.

**Where it lives.** `StatementValidation.ScreenForbiddenPhrase` is the one screen; the severity decision routes
through **`EditionContext.Removed`**, THE policy seam — which already carries documented-dialect-leniency gating
as well as removed-construct gating — so it is an ERROR under strict and a WARNING with an **unchanged bind**
under `--permissive`. Never a local `Permissive` test, never a parallel `Lenient()` method. ⛔ The legacy
`DialectStrictnessChecks` lives only in `src/CobolSharp.Compiler` and must not be revived.

**Why the tolerated path is safe — and what "tolerated" obliges.** The bind is unchanged under `--permissive`
because the emitter's status-first branches make a phrase that cannot fire simply dead — a `'2x'` invalid-key
branch on a sequential-access DELETE, a `'1x'` at-end branch on a random READ — never silently rerouted. That is
what the CCVS-85 corpus depends on. ⛔ **But "dead" is a claim about the INVALID arm only, and it was over-read
into a licence to drop the whole phrase** (kb/Work PB691). Having accepted the program, the compiler owes the
phrase the meaning §9.1.14 gives it, and §9.1.14's final rule has **two** items: item 1 sends a *non*-invalid-key
unsuccessful completion to exception processing, and item 2 — "If the I-O status indicates a successful
completion, control is transferred to the end of the input-output statement **or to the imperative-statement
specified in the NOT INVALID KEY phrase if it is specified**" — makes the NOT INVALID arm **live** on every one
of these statements, because each of them succeeds. So on a sequential-organization WRITE or REWRITE the INVALID
arm is provably dead (§9.1.13.5's four invalid-key statuses `'21'`–`'24'` each name a relative or indexed file)
while the NOT INVALID arm must RUN, and both sequential arms bind the pair and render it through the one
§9.1.14 renderer rather than dropping it.

**Rows five and six are kb/Work PB334, and they arrived the same way SR2's sequential arm did.** SR7 pairs an
ORGANIZATION with a read DIRECTION, and `SequentialIoBinder.BindRead` never called `readDirection()` at all — so
SR7 had **no attachment point anywhere in the compiler**, and a line-sequential `READ … PREVIOUS` was accepted and
read FORWARD. §14.9.30.3 SR10 (the KEY phrase, a hard `COBOLNET0864`) and the Format-1 membership rule were the
same omission on the same arm. All three now read their phrase; §14.9.30.3 SR6 and SR10 live in
`StatementValidation` as ONE check each, called by BOTH READ binder arms, because a per-arm copy is precisely how
they came to be enforced on one organization only. ⚠ READ's NOT INVALID KEY is the ONE phrase on this seam that is
**not** dead under `--permissive`: §14.9.30.4 GR13c transfers control to it on a successful read, so
`BoundRead.InvalidKey` carries it and `SequentialIoEmitter` renders the NOT arm (never the INVALID arm — a
sequential READ raises no `'2x'` status).

**One §9.1.14 renderer, every arm.** `SequentialIoEmitter.EmitInvalid` renders the transfer contract — INVALID on
`'2'`, NOT INVALID on `'0'` — for the keyed READ/WRITE/REWRITE/DELETE/START **and** for the tolerated
sequential arms. It lives on the sequential emitter because that class is the declared home of the file-I/O
common services `KeyedIo` consumes (status store, USE hook, image splice, RETRY renders); a second copy on the
keyed side is exactly how the sequential WRITE came to have no branch at all.

**One code, six rules.** `COBOLNET1720` serves all six, on the `COBOLNET1694` precedent: the *shape* is one
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
### D11. A physical file's §14.9.6.4 GR2 CATEGORY is a named property of the connector, and CLOSE executes Table 14's cell rather than a per-form transcription.

**The rule.** §14.9.6.4 GR2 partitions physical files into (a) Non-unit, (b) Sequential single-unit,
(c) Sequential multi-unit and (d) Non-sequential single/multi-unit, and GR3's **Table 14** is indexed by
those four names crossed with the four written CLOSE forms (plain, WITH NO REWIND, UNIT, UNIT FOR REMOVAL).
Each cell is a set of the symbols a–g GR3 defines. A conforming implementation must place every physical
file it supports in exactly one category, and §14.9.6.4 GR3 symbol c (“closing operations specified by the
implementor”) is a **required, required-to-be-documented** determination (Annex A.1 item 24).

**The decision.** `PhysicalFileCategory` is an enum with GR2's four members and `FileConnector.Category` is
an ABSTRACT property, so the placement is total by construction; `Table14.Cell(format, category)` returns
the printed symbol set and `FileRegistry.CloseByFormat` is the ONE dispatch that executes it. The written
forms (`Close`, `CloseNoRewind`, `CloseReelUnit`, `CloseReelUnitForRemoval`) are thin entries over it, and
`CLOSE … WITH LOCK` layers the reopen prohibition over the plain form — it is not a Table 14 row.

**The placement, and why it is forced.** `SequentialConnector` is (a) Non-unit; `RelativeConnector` and
`IndexedConnector` are (d) by organization alone. (a) is not a free choice: §9.1.13.2 item 6 defines the
`'07'` this compiler reports for the NO REWIND, REEL/UNIT and FOR REMOVAL phrases as the status of a CLOSE
that “references a physical file on a non-reel/unit medium”, and Table 14 prints symbol g (which is what
makes `CLOSE … WITH NO REWIND` answer `'07'`) only in the Non-unit column. The determination is documented
for users in `docs/CONFORMANCE.md` §7 (Annex A.1 item 24) and §2 rows 28–30 / 33–34.

**⛔ THE SAME DETERMINATION GOVERNS THE OPEN STATEMENT'S NO REWIND PHRASE, AND ONE SITE OWNS IT.** §9.1.13.2
item 6 names *“an OPEN statement with the NO REWIND phrase”* in the same sentence as the three CLOSE phrases,
so the OPEN half of the phrase is the same medium question, not a second one. §14.9.27.4 **GR11** — *“The NO
REWIND phrase will be ignored if it does not apply to the storage medium on which the file resides. If the NO
REWIND phrase is ignored, the OPEN statement is successful and the I-O status associated with file-name-1 is
set to '07'.”* — and **GR12**, its complement *“If the storage medium for the file permits rewinding …”*,
partition the media between them, so choosing category (a) chooses GR11 and makes GR12 vacuous. The phrase
rides `BoundOpenFile.NoRewind` (per FILE-NAME, as §14.9.27.4 GR20 requires — the mode, SHARING and RETRY are
the GROUP's, the REWIND phrase is the file-name's own), both emitter arms carry it (the plain `FileOpen` and
the sharing-aware `FileOpenShared`, since the phrases are independent), and `FileRegistry.NoRewindPhraseEffect`
is the ONE place the rule is written: it refuses any category but (a) LOUDLY, and overlays '07' on a status
whose first digit is '0' — GR25 a) reserves an unsuccessful open's own diagnosis. §14.9.27.3 SR5 and SR6 —
the phrase is for sequential files, under INPUT or OUTPUT only — are screened at bind (COBOLNET1802/1803),
which is what keeps categories (b), (c) and (d) off the runtime path in the first place; SR5 is the exact twin
of the CLOSE rule §14.9.6.3 SR1, and until kb/Work PB317 only the CLOSE spelling had a screen while the OPEN
phrase parsed and was silently dropped.

**What it buys.** Categories (b) and (c) are unreachable today, so symbols a, b, d and f — previous units,
no rewind of the current reel, unit removal, rewind — and symbol e's two unit-media branches are vacuous
rather than missing, which is what `docs/CONFORMANCE.md` §8 `DRV-GR-14.9.6.4-L2.1` records. Adding a
unit-structured medium is then a bounded change: give its connector a category, fill the cells
`CloseByFormat`'s loud arm names, and implement §14.9.27.4 GR12 b) where `NoRewindPhraseEffect` refuses.
`CloseTable14Tests` is the drift test that keeps that true — it pins the
16 cells against the printed table, asserts both enums still have exactly four members (the `_` arm C#
requires for out-of-range casts would otherwise swallow a new one), and fails the moment any connector
answers (b) or (c), on the OPEN side (`OpenNoRewind_IsTheCloseArmsTwinOnTheSameMedium`) as well as the CLOSE
side. Before this, each written form carried a hand-copied transcription of its own Non-unit
cell, REEL/UNIT and REEL/UNIT FOR REMOVAL shared one bound kind so `FOR REMOVAL` had no consumer at all,
and the category placement existed only as a sentence in a doc comment that `CONFORMANCE.md` contradicted
(kb/Work PB235).

**GR1 is hoisted into the dispatch.** “If the file connector is not open, the CLOSE statement is
unsuccessful and the I-O status indicator … is set to '42'” now guards every symbol, so an unsuccessful
CLOSE performs none of the closing actions — including §14.9.6.4 GR9's lock release, which used to run
ahead of the connector's own `Close()` on every path. **Symbol e does not release locks either**: its
non-unit branch is “the file remains in the open mode … and no other action takes place”, and GR9's release
rides symbol c (“Close file”), so `CLOSE … UNIT` keeps the file lock and every record lock — pinned by
`conformance:2002/pb235_close_unit_locks`.

### D12. The OPEN statement's REPEATED GROUP is the bound tree's shape: one `BoundOpenFile` per file-name, carrying its OWN group's mode and phrases. The statement node holds no phrase state.

**The rule.** §14.9.27.2's general format has two nested brace pairs, and the OUTER pair — verified by
rendering the printed page, not by reading the transcription — encloses the whole group
`{open-mode} [sharing-phrase] [retry-phrase] {file-name-1 [WITH NO REWIND]} …` and carries its own trailing
ellipsis. Both phrases therefore sit INSIDE the repeated group, beside the open mode and the file-names they
govern. §14.9.27.4 GR20 settles what that means: *"If more than one file-name is specified in an OPEN
statement, the result of executing this OPEN statement is the same as if a separate OPEN statement had been
written for each file-name in the same order as specified in the OPEN statement. These separate OPEN
statements would each have the same open mode specification, the sharing-phrase, retry-phrase, and REWIND
phrase as specified in the OPEN statement."* The open mode is in that list and is unarguably per group — a
multi-group OPEN exists to open files in differing modes — so the phrases listed beside it are per group too.
§14.9.27.4 GR23 bounds the other side: *"If there is no SHARING phrase on the OPEN statement, then file
sharing is completely specified in the file control entry."*

**The design.** `BoundOpenFile(FileModel File, BoundOpenMode Mode, SharingMode? Sharing, RetrySpec? Retry,
string? Unsupported)` is GR20's normal form written down: `BindOpen` flattens the groups into exactly the
separate OPENs the rule names, in source order, and `BoundOpen` is nothing but that list. The statement node
carries **no** phrase property, so a consumer cannot re-broaden a phrase's scope — the previous shape put
`SharingOverride` and `Retry` on the statement, and both the binder (a `sharing` local hoisted out of the
`openClause` loop) and the emitter (one `bool shared` computed before the per-file loop) then leaked one
group's phrase over every file. The two leaks had different fingerprints, which is the signature of a scalar
standing in for a per-group value: the bind leak was forward-only (a later group inherited an earlier
group's phrase, so `OPEN OUTPUT SHARING WITH ALL OTHER F1 OUTPUT F2` rejected legal source at §14.9.27.3
SR8 while its reverse ordering compiled), the emit leak order-independent (a file written BEFORE the phrase
was routed through the sharing facade too, and `FileRegistry.OpenShared` then made it a sharing participant
for the rest of the run). Pinned by `conformance:2002/pb316_open_group_scope`, which writes every arbitration
twice — once as separate OPEN statements, once as one statement with the same groups — and requires the two
to agree, so the test asserts GR20 itself rather than any implementor-defined default.

**This is the carrier the rest of the OPEN work needs.** The NO REWIND phrase (§14.9.27.4 GR11/GR12) and the
per-file syntax rules §14.9.27.3 SR1/SR2/SR5/SR6 are all written per file-name-1, and each adds a field or a
screen to `BoundOpenFile` rather than a second statement-level property.

### D13. A syntax rule of the OPEN statement is enforced ONLY in the OPEN binder, with its FULL antecedent — never a second time at the file control entry.

**The rule.** §14.9.27.3 SR8: *"When file-name-1 is not subject to an APPLY COMMIT clause, then if the sharing
phrase is omitted from the OPEN statement and the ALL phrase is specified in the SHARING clause of the file
control entry for file-name-1 or if the ALL phrase is specified on the OPEN statement, the LOCK MODE clause
shall be specified in the file control entry for file-name-1."* It is a rule about **file-name-1**, which
§14.9.27.2's general format defines as the OPEN statement's operand, and every branch of its antecedent
presupposes an OPEN statement. The SHARING clause's own clause imposes nothing of the kind: §12.4.5.15.3 has
exactly ONE general rule — *"The SHARING clause specifies the sharing mode to be used for the file unless it is
overridden by the SHARING phrase of the OPEN statement"* — and **no syntax rules**, so §14.9.27.3 SR8 is the
standard's only source of a LOCK MODE requirement anywhere.

**The design.** `StatementValidation.CheckOpenSharingAllOther(file, sharing ?? file.Sharing)`, called from
`BindOpen` once per file-name, is SR8's only site. Its two disjuncts collapse onto the EFFECTIVE sharing mode —
the group's phrase when one is written, the file control entry's clause when none is (§14.9.27.4 GR23) — which
is exactly *[ALL on the OPEN]* OR *[phrase omitted AND the SELECT says ALL]*; the leading conjunct reads
`FileModel.SubjectToApplyCommit`, set by `DataBinder.BindIoControl` from the I-O-CONTROL APPLY COMMIT clause
(§12.4.6.3). The exemption is load-bearing rather than decorative: §12.4.5.9.3 SR1 forbids writing a LOCK MODE
clause *"for a file that is the subject of an APPLY COMMIT clause"*, so without it SR8 would demand the one
clause another rule forbids and such a program could not be written at all. It is reachable today — COBOLNET1709
declines the clause but is `PermissiveInert`, so `--permissive` compiles the program.

**Why the file control entry cannot host it.** A second copy lived in `DataBinder.BindFileControl` and had to
drop the antecedent, because a SELECT sees no OPEN statement and the I-O-CONTROL paragraph is not bound until
after the file control entries are. It cost both harms at once: legal source rejected (a file whose OPEN
supplies its own non-ALL sharing phrase; a file declared `SHARING WITH ALL OTHER` and never opened) and, for a
program that really does violate SR8, the same rule reported TWICE in two different spellings. The general
shape: **a syntax rule listed under a statement's clause belongs to that statement's binder; a declaration-time
copy of it can only be a paraphrase, because a declaration cannot see the statement the rule quantifies over.**

`conformance:2002/pb319_sr8_antecedent` pins the antecedent falsified in both compilable ways;
`conformance:negative/sharing-all-no-lockmode` (disjunct 1) and
`conformance:negative/pb316-open-sharing-all-no-lockmode` (disjunct 2) pin it holding.
`conformance:OpenSharingLockModeTests` carries the two assertions no `.cob` corpus can make — that ONE violation
draws exactly ONE diagnostic (a substring-matching `.err` is blind to a duplicate, which is how the second copy
survived), and the APPLY COMMIT exemption, which needs the permissive axis.
### D14. ISO §14.9.27.4 Table 19 is a STRUCTURE, and it arbitrates EVERY OPEN — not only the connectors that declared a SHARING clause.

**The rule.** §9.1.15 puts the gate on the physical file, not on the connectors that opted in: *"Before access
to a shared physical file is allowed through an OPEN statement, the sharing mode and the open mode of that OPEN
statement shall be allowed by all other file connectors that are currently associated with the physical file,
as described in 9.1.13, I-O status; 14.9.27, OPEN statement; and Table 19"*. **Table 19** is that decision
table: 7 *Open request* rows × 5 *Most restrictive existing sharing mode and open mode* columns = 35 cells,
each *Normal open* or *Unsuccessful open*, and §9.1.13.9 item 1 supplies the status the unsuccessful cell
reports (`'61'`) while §14.9.27.4 GR25 supplies its effect (*"the file is not affected"*).

**The decision.** `OpenRequestRow` and `ExistingSharingColumn` are enums with the printed row and column groups
as their members, `Table19.Cell(row, column)` is an exhaustive switch over all 35 printed cells, and
`FileRegistry.Conflicts` is a lookup into it and nothing else. `FileRegistry.OpenCore` is the ONE OPEN
dispatch — the plain `Open` and the phrase-bearing `OpenShared` are entries over it — so every connector is
arbitrated whether or not it wrote a SHARING or LOCK MODE clause. That also covers the opens §14.9.27.4 names
without a statement of their own: Table 19 shows the results of opening files currently open by another
connector *"including those implicitly opened by the SORT and MERGE statements"*, and those route through
`CobolFile.OpenInput`/`OpenOutput` → `Open` → `OpenCore` with no arm of their own. Because every successful OPEN registers in `PhysicalFileTable`, every close
path releases: `SharedClose` (from the Table-14 CLOSE dispatch), `CloseDisplaced`, `CloseAll` and
`CloseAndDrop`. The registration is gated on the OPEN's STATUS, not on `IsOpen`, so a re-OPEN of an already-open
connector — `'41'`, which leaves the connector in its ORIGINAL mode — no longer re-registers it under the failed
request's mode.

**⛔ The PRINTED TABLE is the arbiter, not §9.1.13.9's five sub-cases, and the difference is four cells.**
§9.1.13.9 item 1 enumerates five *"possible violations"* (a)–(e). Sub-cases (c) and (d) are written over
*"I-O or extend"*, but Table 19's existing-side column groups are `extend I-O output` — so a literal reading of
the five sub-cases answers *Normal open* in four combinations the table marks *Unsuccessful open* (an incoming
SHARING WITH READ ONLY request, in the EXTEND/I-O or INPUT mode, against a connector open in the OUTPUT mode).
§9.1.15 rule 2 resolves it in the table's favour in so many words — *"unsuccessful if the physical file is
associated with another file connector whose open mode is other than input"* and *"subsequent requests to open
the physical file through other file connectors in a mode other than input … will be unsuccessful"* — and
OUTPUT is a mode other than input. `OpenTable19Tests` pins both readings: the 35 cells one by one against the
**rendered PDF page**, and the §9.1.15 prose over all 144 (sharing, mode)² connector pairs, asserting that the
sub-case reading disagrees in exactly four. The finding is an assertion, not a comment, so it cannot rot.

**⛔ The implementor default is UNDETERMINED, and the arbitration decides nothing about it.** §9.1.15: *"If no
specification is made in either location, the implementor defines the sharing mode in which the file is
opened"*. `FileRegistry.ImplementorDefaultSharing` is the ONE place that default is named and it is `null`;
choosing its value is the owner determination tracked as `kb/Work` **PB322** (Annex A.1 items 77 and 131). A
`FileSharing?` therefore threads through `ConnectorShare`, `PhysicalFileTable.State.Open` and `Conflicts`, and
`Conflicts` arbitrates an undetermined mode **universally**: a conflict only where EVERY candidate mode gives
*Unsuccessful open*. No `'61'` this compiler answers today can be contradicted by PB322's answer, and when
PB322 lands, replacing the `null` collapses the quantifier to a plain lookup. With BOTH connectors undetermined
that leaves exactly §9.1.13.9 1) e) — *"An attempt is made to open a physical file in the output mode and the
physical file is currently open by another file connector"* — the sub-case that names no sharing mode at all,
which is what makes a second OPEN OUTPUT deterministic instead of an OS sharing violation leaking `'30'` into
the I-O status. The quantifier is extensionally identical to substituting ALL OTHER, because ALL OTHER is
Table 19's least restrictive row AND its least restrictive column group; that is a property of the printed
table rather than a choice, and it is asserted so a PB322 landing fails a test instead of drifting.

**What it buys, and what it cost before.** `Conflicts` used to be a four-test predicate chain whose final
`return false;` carried the comment `// (e) ALL OTHER` — the letter of a sub-case it had never implemented —
so every incoming OPEN OUTPUT was permitted and truncated a file another connector held open; and
`FileRegistry.Open` consulted the table only `if (IsSharingActive(name))`, while `RegisterSharing` set
`FileConnector.SharedStreams` (an OS handle opened `FileShare.ReadWrite`). Together those made a file declared
`SHARING WITH NO OTHER` strictly LESS protected than one declaring nothing: the clause dropped the OS exclusion
and handed arbitration to a table that could not see the plain connector coming. The guarding test was six
`InlineData` rows against 35 cells with no row whose incoming mode was OUTPUT (kb/Work PB321). Two silent
defaults went with it — the emitter registered a LOCK-MODE-only file as ALL OTHER, and a RETRY-phrase-only OPEN
registered ALL OTHER here — three arms of one determination, two of which had already answered it.

**Residue (reported, not fixed):** the runtime has no edition, so the arbitration also runs at `--std 85`. No
SHARING clause is accepted there, so the only conflict reachable is sub-case (e), and a second OPEN OUTPUT
answers `'61'` where the OS accident answered `'30'`. Table 19 rides Annex A.4.7, a COBOL-2002 introduction, so
whether a pre-2002 edition may report a value from the 2002 5x/6x family is a determination in A.1 item 77's
family; nothing in the corpus exercises it.

### D15. The connector-to-physical-file ASSOCIATION is a per-statement act with ONE entry point, `FileConnector.Associate`, and the connector's `HostPath` is its only answer — nothing caches a resolved path.

**The rule.** §14.9.27.4 GR26 routes the OPEN statement to §12.4.5.3 GR3/GR4, and GR3 is written as a timing rule
first and a value rule second: *"The ASSIGN clause specifies the association of the file connector referenced by
file-name-1 to a physical file identified by device-name-1, literal-1, or the content of the data item referenced by
data-name-1. **The association occurs at the time of execution of an OPEN, SORT, or MERGE statement that referenced
file-name-1**, according to the following rules: a) When the TO phrase … is specified and the USING phrase is
omitted, … identified by the specification of device-name-1 or the value of literal-1 …; b) **When the USING phrase
of the ASSIGN clause is specified, the file connector … is associated with a physical file identified by the content
of the data item referenced by data-name-1 in the runtime element that executes the OPEN, SORT, or MERGE
statement.**"* §9.1.21 (Dynamic file assignment) is the concepts clause that names the whole facility, and Annex
D.19.9.2's NOTE states the consequence outright: *"The MOVE statements only have an effect on the dynamic assignment
when a subsequent OPEN statement for the file connector is executed."*

**The shape — THE OPERANDS TRAVEL WITH THE STATEMENT.** `FileConnector.HostPath` is a settable property whose ONE
writer is `FileConnector.Associate(spec, dynamic)`, and `FileRegistry.OpenCore` — the one OPEN dispatch that the plain
`Open`, `OpenNoRewind`, `OpenShared` and the emitted SORT/MERGE implicit opens (§14.9.40.4 GR12a/GR15a, §14.9.24.4
GR7a) all funnel through — is its only caller. `spec` is the EXECUTING element's own ASSIGN specification, rendered at
the statement by `SequentialIoEmitter.ExecutingElementArgs(file)` and passed as an argument of the OPEN call; `dynamic`
selects GR3 b)'s content rules over GR3 a)'s plain literal, so both arms of GR3 run through one entry point rather than
a register-time mechanism for TO and an open-time mechanism for USING.

**⛔ Why the source is an ARGUMENT and never state on the connector (kb/Work PB673).** Both arms of GR3 name the
element that runs the statement — a) *"in the source unit that specifies the OPEN, SORT, or MERGE statement"*, b)
*"in the runtime element that executes the OPEN, SORT, or MERGE statement"* — and a file connector is neither
per-element nor per-activation. An EXTERNAL file connector is ONE object per run unit shared by every describing
element (§13.18.22.4 GR4 a), whose entries §12.4.5.3 GR1 b) requires only to be CONSISTENT — unlike GR1 i), which
makes FILE STATUS *the same* external item — so two programs may legally hold separate storage for data-name-1
(COBOL.NET's GR1 b) consistency rule is textual sameness of the clause: `docs/CONFORMANCE.md` §7, `DOC-A.1-72`). A
RECURSIVE non-INITIAL unit's internal connector is unit-scoped last-used state across activations (§8.6.4,
§14.6.2.3.3) while its LOCAL-STORAGE is per-activation. An installed closure therefore answers with whichever
element/activation installed it LAST, which is the executing one only by accident: the earlier
`CobolFile.SetAssignUsing(key, () => <data-name-1>)` install (unguarded, for the kb/Work PB168 reason) opened the
wrong physical file with status '00'. Rendering the operand at the statement makes the right answer structural —
the emitting unit IS the executing element and the emitted expression reads the LIVE activation's storage — and the
runtime entry points take the operands with NO DEFAULT, so an emitter path that forgets them is a compile error.
Both emit paths get it from the same helper: the statement emitters for a program, and the same ones inside
`OoEmitter`'s class bodies for an object — Annex D.19.9.2's own worked example is an instance file.

**Why the sharing registry stopped caching the host path.** `ConnectorShare` used to hold a `Host` copy taken at
`RegisterSharing` time, and every physical-file-table lookup (`ReadLockGovern`, `ReadShared`, `WriteShared`,
`RewriteShared`, `DeleteShared`, `SharedClose`) read that copy. It was only ever right because the path could not
change; a mutable association makes it a stale key that would arbitrate §9.1.15 sharing over the file the connector
used to be associated with. The field is gone and the lookups read `c.HostPath` (or `HostPathOf(name)`).

**The failure status is the standard's own.** GR3's closing sentence — *"If the association cannot be made because
the content of the data item referenced by data-name-1 is not consistent with the specification for device-name-1 or
literal-1, the OPEN, SORT, or MERGE statement is unsuccessful"* — has a dedicated I-O status, §9.1.13.6 item 2's
**'31'**, and `Associate` returns it. The CONTENT rules it applies are the implementor's (§12.4.5.3 GR4, Annex A.1
items 10 and 73) and are stated in `docs/CONFORMANCE.md` §7 under `DOC-A.1-73`.

**An unassociated connector is a real state, not an error.** A bare `ASSIGN USING data-name-1` names no
device-name-1/literal-1 at all, so nothing identifies a physical file until the first OPEN/SORT/MERGE. It registers
with an EMPTY `HostPath`, and `DELETE FILE` on one takes §14.9.10.4 GR14 — *"If the file associated with file-name-1
is not present, the execution of the DELETE FILE statement is successful and the I-O status value … is set to
'05'"* — rather than re-resolving data-name-1, which would be an implementor extension to GR3's closed list of
associating statements on the one verb where guessing wrong destroys data.

**§12.4.5.2 SR7 is enforced at bind time**, once the data forest is indexed (`DataBinder.ResolveAssignUsing`):
data-name-1 shall reference an alphanumeric data item (`COBOLNET1810`) and shall not be subordinate to the file
description entry for file-name-1 (`COBOLNET1811`). The second half is the dangerous one — an operand inside the
file's own record area is overwritten by every READ of that file.

**Rejected alternatives.** Emitting a `CobolFile.Assign(key, expr)` statement before every OPEN (spreads one rule
across every OPEN site, and misses SORT/MERGE unless each is remembered); re-resolving inside
`FileConnector.Open` (too late — `SharedOpenAttempt` consults the physical-file table on the OLD path before
calling it); keeping the registration-time resolve and adding a second dynamic path (two mechanisms for one job).
### D16. The READ statement's lock options are TWO INDEPENDENT PRINTED BRACKETS, and they are two facts from the grammar through to the runtime — contention and retention — never one enum.

ISO §14.9.30.2 prints the READ lock options as two plain brackets, in this order, and then the KEY phrase:

    [ ADVANCING ON LOCK | IGNORING LOCK | retry-phrase ]      (Format 2 drops ADVANCING ON LOCK)
    [ WITH LOCK | WITH NO LOCK ]
    [ KEY IS { data-name-1 | record-key-name-1 } ]            (Format 2 only)

Measured, not inherited: `clause_page.py 14.9.30.2` -> PDF page 722, and `figure_geometry.py 722` reports the
Format-1 brackets at y=280.90 (h=48.79, three stacked alternatives) and y=339.26 (h=31.98, two), the Format-2
brackets at y=507.57 and y=547.48 and the KEY bracket at y=589.03, all with PLAIN stems — contrast the AT END
group at y=377.08 and the INVALID KEY group at y=634.55, which the same tool flags
`<-- CHOICE INDICATORS (5.2.6.4)`. Three rules follow and all three are load-bearing:

- **§5.2.6.2** — a bracket admits "the syntax element contained within the brackets or one of the alternatives
  contained within the brackets", so AT MOST ONE alternative comes out of each bracket.
- **§5.2.6.1** — an option may also be selected "by specifying a unique combination of possibilities from a
  series of brackets", so the two brackets are INDEPENDENT: `IGNORING LOCK WITH NO LOCK` is one legal READ.
- **§5.2.1** — the phrases "shall be written … in the sequence given in the general format", so the KEY phrase
  FOLLOWS both brackets and the reverse spelling is not conforming.

**The shape.** `CobolIO.g4#readLockContentionPhrase` is bracket 1 (`readAdvancingOnLock | readIgnoringLock |
retryPhrase`) and `#recordLockPhrase` is bracket 2 (`WITH? NO LOCK | WITH? LOCK`) — one grammar object per
printed bracket, which is what writes "at most one of these" down exactly once. `BoundRead` / `BoundKeyedRead`
therefore carry FOUR fields, not one: `Lock` (the retention bracket), and `AdvancingOnLock` / `IgnoringLock` /
`Retry` (the three alternatives of the contention bracket, at most one of which the grammar can deliver). The
runtime mirrors it: `FileRecordLock` is the retention bracket alone, `CobolFile.ReadShared` takes
`advancingOnLock` and `ignoringLock` as their own arguments, and the Format-2 `ReadLockGovern` takes
`ignoringLock` (ADVANCING ON LOCK is not in the Format-2 general format — D17). WRITE and REWRITE reference
`recordLockPhrase` only, because §14.9.51.2 and §14.9.35.2 print `[ retry-phrase ]` and
`[ WITH LOCK | WITH NO LOCK ]` and no IGNORING LOCK at all.

**Why it is written as a decision (kb/Work PB331).** One rule, `recordLockPhrase : IGNORING LOCK | WITH? NO
LOCK | WITH? LOCK`, reached through three free optionals, inverted BOTH cardinalities at once: what the printed
brackets make mutually exclusive was free (`ADVANCING ON LOCK RETRY 3 TIMES IGNORING LOCK` compiled), and what
they make independent was exclusive (`IGNORING LOCK WITH NO LOCK` was `no viable alternative at input 'WITH'`).
The same merge put IGNORING LOCK on WRITE and REWRITE, and — because a single enum cannot say two things — it
kept the legal contention+retention pair out of `ApplyReadLockDiscipline`'s GR11 b) release. It also SILENTLY
SUPPLIED §14.9.30.3 SR3: `FileLockBinder.CheckRecordLockPhrase`'s summary claimed to enforce "IGNORING LOCK and
WITH LOCK are mutually exclusive" while its body tested only SR4's automatic-locking condition, so the
forbidden pair was rejected by the collapse and by nothing else. Splitting the brackets without landing a real
SR3 check in the same change set would have opened that hole; `CheckIgnoringLock` -> COBOLNET1818 is the check,
and `negative/pb331-read-ignoring-with-lock` is its witness, paired with the permitted-pair positive
`2002/pb331_read_lock_brackets`. ⚠ SR3's "the LOCK phrase" is WITH LOCK and NOT WITH NO LOCK: §14.9.30.4 GR11 b)
names "the NO LOCK phrase" and GR11 d) "the LOCK phrase" as different phrases in the same statement.

**One optional word came with the measurement.** Page 722 underlines ADVANCING and LOCK and NOT ON
(`figure_extract.py 722` -> `_ADVANCING_ ON _LOCK_`), so by §5.2.3 `READ … ADVANCING LOCK` is conforming; it was
`error COBOL0001: missing token before 'LOCK'`. The grammar now spells the phrase `ADVANCING ON? LOCK`.

**And one optional word written down TWICE.** The statement's single printed `RECORD` — one word, after the
`{NEXT | PREVIOUS}` braces — lived in BOTH `readDirection : (NEXT | PREVIOUS) RECORD?` and `readStatement`'s own
`RECORD?`, so `READ SQF NEXT RECORD RECORD … END-READ` compiled clean. §5.2.3 makes a non-underlined word one
that may be OMITTED, not one that may be repeated. `readDirection` is now `NEXT | PREVIOUS`, which is also what
§5.2.6.3 describes: the NEXT alternative "contains only optional words", so it is the default and the whole rule
is optional at the call site. Neither spelling of the word was wrong on its own; having the same optional word in
two rules was, and no test could see it because both single-`RECORD` spellings still parsed
(`feedback_one_rule_one_place`).

### D17. Record-lock governance on a READ is split by FORMAT, not by ORGANIZATION: one governed Format-1 read serves sequential, relative and indexed, and it owns the GR22 skip-scan.

**The rule shape.** ISO §14.9.30.4 GR7–GR12 are ALL-FORMATS rules; GR22 is not. ADVANCING ON LOCK appears only
in the **Format-1** general format (§14.9.30.2) and §14.9.30.3 SR6 bars it under ACCESS MODE RANDOM, so it is a
phrase of the sequential-ACCESS read on **every organization** — sequential, relative and indexed alike. The
compiler had split the governance the other way, by organization: `FileRegistry.ReadShared` (which carried the
skip-scan) began `if (… c is not SequentialConnector) return false;`, and the keyed emitter called the post-read
`ReadLockGovern`, which has no advancing-on-lock parameter at all. `BoundKeyedRead.AdvancingOnLock` was set by
the binder, edition-gated by `VersionConformancePass`, and read by nothing: a relative or indexed
`READ … ADVANCING ON LOCK` answered `'51'`, the one status GR22 rules out (kb/Work PB340).

**Why GR22 needs no pre-read peek, on any organization.** The obvious repair — give the keyed read the
sequential arm's PRE-read governed entry — is the wrong half of the truth. A pre-read conflict check exists for
one reason: GR10 a) requires the file position indicator to be UNCHANGED when the record operation conflict
condition arises, and only the sequential walk can name the record it is about to deliver (its next ordinal) —
a relative or indexed walk selects "the first existing record … greater than the file position indicator"
(GR21) and learns which that is by reading it. But GR22 states the conflict condition **does not exist**, and
its model is explicitly post-read: *"as if the locked record were read and then the same READ statement were
executed"*. The locked record IS read and the position DOES advance; that is the rule, not a compromise. So the
skip is one `continue` after the physical step, shared by all three organizations.

**The shape.** `FileRegistry.ReadShared(name, previous, phrase, advancingOnLock, ignoringLock, retry…, out image)`
is the ONE governed Format-1 read and returns the **I-O status** (a record was made available iff it begins
`'0'`), which is the more general of the two contracts the emitters need. Three collaborators, each the single
place its rule is written:

| member | rule |
|---|---|
| `ReadFormat1Step` | ONE physical NEXT/PREVIOUS retrieval on any organization — the step GR22 repeats |
| `PeekFormat1RecordId` | GR9's pre-read conflict target where GR10 a) can be honoured; `""` where it cannot |
| `ConflictOnLockedRecord` | GR9 + §14.7.9's RETRY, shared with the Format-2 `ReadLockGovern` |

`SequentialIoEmitter` renders it through `RuntimeApi.FileReadSharedOk`, which wraps the call in `[0] == '0'` so
its bool contract is unchanged; `KeyedIoEmitter` renders `ReadKind.Next`/`Previous` through the same entry
and keeps `ReadLockGovern` for the **Format-2** random read, where it is right: there is no next record to
advance to and no ADVANCING phrase in that format. (The Format-2 post-read `'51'` still leaves the position
advanced against GR10 a) — that is kb/Work PB338, untouched here and unaffected by this split.)

**The binder half came with it.** §14.9.30.3 SR9 implies NEXT under ACCESS DYNAMIC "if any of the following
phrases is specified: **ADVANCING**, AT END, or NOT AT END", and `KeyedIoBinder.BindRead` tested two of the
three, so `READ f ADVANCING ON LOCK` bound as a Format-2 random read — the GR22 loop was unreachable for the one
spelling SR9 exists to name (kb/Work PB335).

**The drift test that generalises it.** `BoundIoPhraseConsumptionDriftTests` asserts that every phrase property
declared by a bound I-O node is READ by its emitter, over all eight READ/WRITE/REWRITE/DELETE/START nodes. Three
inventory rows were open for that one shape at once — this one, OPEN … WITH NO REWIND (PB317) and the sequential
READ direction (PB334). It cannot see a phrase the BINDER never stored, which was PB334's remaining half — now
landed: `BoundRead` carries `ReadKind` and `InvalidKey`, so the drift test covers that arm too, and
`previous` reaches `ReadFormat1Step` / `PeekFormat1RecordId` from BOTH emitters.
### D18. A file description entry with NO record description entries still has a record area, and it is SYNTHESIZED AT BIND TIME — ISO §14.9.30.4 GR6's implied entry, made real — never worked around at the consumers.

ISO §13.4.5.3 SR3 expressly contemplates the shape: *"When no record description entries are specified: a) a
RECORD clause shall be specified in the file description entry, b) a FILE phrase specifying file-name-1 and the
FROM phrase shall be specified on all WRITE and REWRITE statements associated with the file, and c) an INTO
phrase shall be specified on all READ statements associated with the file."* SR7 confines the *one or more record
description entries* requirement to INDEXED files, so `FD FIN RECORD CONTAINS 20 CHARACTERS.` with no level-01 is
legal on both the sequential and the relative organizations. §14.9.30.4 GR6 then says exactly what that file's
record area IS: a READ INTO *"proceeds as though there were one record description entry describing an
alphanumeric group item of the maximum size established by the RECORD clause"*.

**The decision:** `DataBinder.MaterializeImpliedRecord` builds that entry — an unnamed level-01 GROUP over one
FILLER `PIC X(n)` — as the last act of binding each FD, and everything downstream sees an ordinary record.
`FileModel.AreaRecord` is consequently non-null for every FD that can be opened with data, and is null only for a
REPORT file (§13.4.5.3 SR8 forbids it record descriptions) and for a SELECT with no FD at all.

- **A GROUP, not an elementary `PIC X(n)`,** because the distinction is observable: §14.9.25.4 makes a MOVE whose
  sender is a group item a group (alphanumeric) move, so `READ … INTO` a numeric receiver copies bytes where an
  elementary alphanumeric sender would convert. GR6 says "group item".
- **Unnamed and off `ByName`,** because the entry is implied: the program cannot reference it, which is precisely
  why SR3 b) and c) require the `FILE … FROM` and `INTO` phrases on the verbs.
- **The size is "the maximum size established by the RECORD clause"** — format 1's integer-1, format 3's
  integer-5, format 2's integer-3. A format-2 clause with no `TO` phrase establishes none (§13.18.43.4 GR10
  defers to "the greatest number of bytes described for a record in that file", and none is described), and
  neither does an absent clause, which §13.4.5.3 SR3 a) and §13.18.43.3 SR1 forbid outright: **COBOLNET1836**.
- **The two entry kinds that take SR3's permission back are SCREENS, not fallbacks: COBOLNET1837.** §13.4.5.3 SR7
  (INDEXED) and §13.4.6.3 SR2 (sort-merge) both require record description entries, because both are keyed and a
  key is located IN a record (§12.4.5.12.3 SR2; §14.9.40.3 SR6 a).

**Rejected alternatives.** *Register the connector and leave `AreaRecord` null* — the shape this replaced: the
record-less arm was asked TWICE and answered differently (`SequentialIoEmitter.EmitFileRegistration` registered
only a REPORT file; `KeyedIoEmitter.EmitRegistration` returned outright), so both organizations produced NO file
connector and the first I-O verb aborted the run unit — and repairing registration ALONE would only have turned
the crash into a silent no-op, because the READ record-area store and the implicit INTO move were separately
guarded on the same null. Five null arms for one absent fact is four too many (`feedback_one_rule_one_place`).
*Synthesize at emit time instead* — the record has to exist for the binder's own record-area resolution
(`ReferenceResolver.ResolveItem`), not just for the registration call.

### D19. A syntax rule of a FILE CONTROL ENTRY clause is enforced ON THE ENTRY, in `DataBinder.BindFileControl` — never on a verb. It is the converse of D13, and the two together decide where every I-O syntax rule goes.

**The rule.** §12.4.5.5.2 SR2: *"The DYNAMIC and RANDOM phrases shall not be specified for a sequential file."*
The general format says the same thing structurally — Format 3, the sequential file control entry, admits only
`[ ACCESS MODE IS SEQUENTIAL ]` — but a format is not a check, and nothing screened it: `SELECT F ASSIGN TO "f"
ORGANIZATION IS SEQUENTIAL ACCESS MODE IS RANDOM.` compiled and ran clean (kb/Work PB692).

**Where it lives.** `DataBinder.BindFileControl`, immediately after the clause loop, beside the §12.4.5.9 SR2
screen already there: `file.IsSequential && file.AccessMode is Random or Dynamic` — the predicate NAMES the
two phrases SR2 names rather than negating SEQUENTIAL → **COBOLNET1858**,
positioned at the ACCESS MODE clause itself. Both banned phrases go through one screen and one descriptor,
because SR2 is one prohibition with two spellings.

**Which files are "sequential".** `FileModel.IsSequential` — the ONE spelling of sequential organization, now
shared by both entry screens rather than each open-coding the enum pair. It answers three source shapes:
an explicit `ORGANIZATION IS SEQUENTIAL`; `LINE SEQUENTIAL` (§12.4.5.10.3 GR2 puts the phrase in the ORGANIZATION
clause, that clause is written only in the Format-3 entry, and §12.4.5.2 SR11 says *"Format 3 shall be specified
only for a sequential file or a report file"*); and the **omitted** clause, which §12.4.5.10.3 GR6 makes
*"sequential organization with the RECORD SEQUENTIAL phrase"*. The omitted clause is the shape a screen keyed on
a written clause would miss, and it is the shape a user is most likely to write.

**Why a verb cannot host it.** D13's converse, and for the mirror-image reason. §12.4.5.5.2 SR2 quantifies over
the ENTRY, not over a statement: a program that declares the combination and never opens the file has already
violated it. Screening it at READ (or WRITE, or START) would leave every unread file unchecked, would report the
one entry error once per statement in programs that do use the file, and would state a declaration rule in the
vocabulary of a statement that does not appear in it. **A syntax rule listed under a file control entry clause
belongs to the entry binder; a syntax rule listed under a statement belongs to that statement's binder (D13).
Neither is a place to put the other.**

**What this makes unreachable, deliberately.** From here on `IsSequential && AccessMode != Sequential` cannot
hold, so every statement-level screen that tests `AccessMode == Random` on a **sequential-organization** file is
defence-in-depth — kept (it is the statement's own rule, and it costs one comparison), never the only guard.
§14.9.30.3 SR6's screen in `KeyedIoBinder` is unaffected: RANDOM is legal for relative and indexed files, which
is the only path that reaches it.

**Edition-invariance was derived, not assumed.** No `docs/VERSION_CHANGE_REFERENCE.md` row touches §12.4.5.5, and
ANSI X3.23-1985's sequential-I-O file control entry likewise offered no ACCESS MODE but SEQUENTIAL, so the screen
is unconditional and three of the four negatives assert `COBOLNET1858` at 85/2002/2014/2023. The LINE SEQUENTIAL
negative names 2002/2014/2023 only — 1985 has no line sequential organization at all, so its 1985 verdict belongs
to that organization's edition gate (kb/Work PB688), not to this rule.

**The complement was measured, not assumed.** A screen is evidence about what it REJECTED, never about what it
let through. A static scan of all 1483 SELECT entries under `tests/` reports every cell this screen rejects
EMPTY — no corpus program declares the combination, so the change cannot over-reject anything the suite already
compiles — and every legal (organization × access) cell populated **except** LINE SEQUENTIAL with an explicit
`ACCESS MODE IS SEQUENTIAL`, which had zero witnesses. `conformance:2023/pb692_line_sequential_access_sequential`
is that missing positive; `conformance:2023/l1_open_mixed_org_access` already pins RELATIVE/RANDOM and
INDEXED/DYNAMIC.

**Still homeless, and NOT closed by this** (measured over the entry-rule family while here):
§12.4.5.5.2 **SR1** (RANDOM banned on a file named in a SORT/MERGE USING or GIVING phrase — a statement-context
rule, so D13 puts it in the SORT/MERGE binder, not here); §12.4.5.2 SR8/SR9/SR11/SR13 (format ↔ organization /
SD consistency); §12.4.5.2 SR12 (LINE SEQUENTIAL excludes RESERVE). The **key** entry rules
(§12.4.5.6.3 SR1, §12.4.5.12.3 SR1, §12.4.5.13.3 SR1 — no key under OCCURS) are kb/Work PB699, which is this
same decision applied to `KeyedIoBinder.KeyedValidateFile`: an entry rule that waits for a verb.
§12.4.5.2 **SR10** — *"The RELATIVE clause shall be specified if the DYNAMIC or RANDOM phrase of the ACCESS
clause is specified"* — belongs with them: its substance IS enforced by `KeyedValidateFile`, but cited there as
§12.4.5.13, which carries no such requirement, and being verb-driven it misses a relative file that is declared
RANDOM and never referenced.

## C# mapping

> Backend neutrality (G4; SSOT §18 #23): everything semantic in this section — FILE STATUS capture, the AT END /
> INVALID KEY branch, READ INTO / WRITE FROM expansion, the prologue registrations — is a structured BOUND-TREE form;
> this section shows the primary RoslynBackend rendering. The future CilBackend renders the SAME bound nodes behind
> `ICodeGenBackend` with its own private lowering; no bound node carries pre-rendered C# text.

An FD record CUST-REC with CUST-ID PIC 9(5), CUST-NAME PIC X(20), CUST-BAL PIC S9(7)V99 COMP-3 maps to a public record struct CUST_REC holding public long CUST_ID, public string CUST_NAME, public long CUST_BAL, where CUST_BAL is the UNSCALED long (scale 2 is compile-time metadata per the owner numeric lock; no decimal). The record area is a single field of that type in the program class. The connector is a public sealed generic FileConnector of TRec exposing Open, Close, Read, ReadPrevious, ReadByKey, Write, Rewrite, Delete, Start, SetKey, plus properties CurrentSlot, LastRecordLength, EndOfPage, and LastStatus. There is NO program-supplied key parameter; the key is the current value of the typed RECORD KEY or RELATIVE KEY field. The codec is a generated IRecordCodec exposing Serialize, Deserialize, PrimeKey, AlternateKey, FixedLength, MinLength, MaxLength, and CodeSet. Stores: the sequential connector uses a StreamReader or StreamWriter for line-sequential and a FileStream for record-sequential with a 4-byte little-endian length prefix for varying; the relative connector uses a sorted dictionary from int slot to byte image with 0xFF gaps; the indexed connector uses a sorted dictionary from CobolKey to byte image as the sole source of truth, with alternates derived on demand and a PER-KEY release ordinal for duplicate ordering. The run-unit prologue emits one registration per SELECT plus AddAlternateKey calls. After each I/O verb the compiler stores the connector LastStatus into the FILE STATUS item then branches AT END or INVALID KEY on the first char (1 at-end, 2 invalid-key, 3 4 7 9 fatal to a USE declarative). READ INTO lowers to Read plus a typed group MOVE; WRITE FROM lowers to a typed MOVE plus Write; a sequential RELATIVE WRITE or READ NEXT MOVEs CurrentSlot back into the RELATIVE KEY field.

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

### The READ DIRECTION is one fact on both bound READ nodes, and a backward sequential read is a REPOSITION feeding the same physical read body.

§14.9.30.4 GR19 — "An implicit or explicit NEXT phrase or a PREVIOUS phrase results in a sequential read:
otherwise, the read is a random read and the rules for format 2 apply" — makes the direction phrase and the
FORMAT one fact, so ONE enum carries it: `ReadKind { Next, Previous, Random }`, on `BoundRead` and
`BoundKeyedRead` alike, reached through `IBoundRead`. `VersionConformancePass.GateStatement` has ONE arm over
that interface. (It was `KeyedReadKind`, reachable only from the keyed node; the sequential node had no direction
member, so `READ … PREVIOUS` on a SEQUENTIAL file bound as a forward read AND skipped its COBOLNET0900 2002 gate
— kb/Work PB334.)

`SequentialConnector.Read(previous, out image)` implements GR21's "When the file is a sequential file" block
directly: rule b) — the file position indicator established by a prior OPEN selects "the first existing record …
regardless of whether NEXT or PREVIOUS is specified", so both directions target ordinal 1; rule c) — after a
successful READ the target is one greater (NEXT) or one less (PREVIOUS); rule e) — no such record is the at end
condition ('10' + the AT END imperative, GR24). `TargetReadOrdinal(previous)` is that arithmetic in one place, and
it is also the §14.9.30.4 GR9 pre-read conflict target the sharing registry checks, so `ADVANCING ON LOCK`
skip-scans in the statement's own direction — GR22 ends the scan at "the end of the file … if NEXT is specified or
implied, or the beginning of file … if PREVIOUS is specified".

A backward read is a **reposition**, not a second reader: `SeekToOrdinal` positions the stream at the record's
first character and the ordinary read body then delivers it, which is what keeps `_lastReadBlockStart` (the
in-place REWRITE anchor, §14.9.35.4 GR5) correct after a PREVIOUS. The offset is arithmetic on a fixed-width
record-sequential file; on a RECORD VARYING file it comes from `RecordFraming.FrameStarts`, a prefix-only scan
built **lazily on the first backward read**, so a forward READ walk pays nothing for a facility it never uses.
LINE SEQUENTIAL has no backward walk and needs none — §14.9.30.3 SR7 forbids the phrase there — and a stream that
cannot seek is not §14.9.30.4 GR20's "single reel/unit mass storage file", which the connector reports as the '30'
permanent error rather than silently reading something else.

### EXTERNAL files shared across programs plus GLOBAL FD inheritance in nested programs, with matching layouts.

A process-wide registry keyed by external name (with an Area discriminator for record sharing, porting IC227A); nested programs resolve the parent connector via a global registry (porting IC233A and 234A); the codec and layout must match across declarations.

## Edge cases

- OPTIONAL files: OPEN INPUT missing gives 05 and positions at EOF (first READ is 10); OPEN I-O or EXTEND missing creates and gives 05; non-optional missing gives 35 (not silent-create, the legacy RelativeFileHandler bug fix).
- Sequential RELATIVE WRITE assigns the next slot and MOVEs it into the typed RELATIVE KEY field; READ NEXT exposes CurrentSlot for the same MOVE-back (14.9.51 sequential).
- Random RELATIVE: key below 1 gives 34, an occupied slot gives 22 (INVALID KEY), an absent slot gives 23; sequential digit overflow gives 24, sequential READ digit overflow gives 14.
- Indexed: ascending-order WRITE in ACCESS SEQUENTIAL gives 21 on a non-increasing key; a duplicate prime or alt-without-duplicates gives 22; alt-with-duplicates gives 02; START supports a generic partial or prefix key compare (14.9.41) and positions inclusively so the next READ NEXT returns the matched record.
- **START is a THREE-organization verb** (kb/Work PB352). 14.9.41 writes its general rules three times, once
  per organization heading — GR11/GR12 for RELATIVE, GR18/GR19 for INDEXED, GR20/GR21 for SEQUENTIAL — and
  14.9.41.3 SR2 makes FIRST or LAST the REQUIRED phrase on a sequential-organization file, so the sequential
  arm is not an extension but the only shape a conforming START on one can have. `SequentialConnector`
  answers it over the ONE framing walk (`NextFrame`, extracted out of `Read` so START's record scan cannot
  become a second copy of the line/varying/fixed framing rules): FIRST rewinds to record 1, LAST scans to the
  last frame, each repositions the reader through `SeekToRecord` (which discards the buffered data and resets
  the derived read state — the byte anchors, the GR15 unread remainder, the 9.1.16 record ordinal), and
  positioning is INCLUSIVE per 14.9.30.4 GR21's sequential arm b). Every failure arm — an empty file, a
  non-seekable stream, an absent OPTIONAL file — is '23' (9.1.13.5 item 3 b)/d)), never an abort; a wrong open
  mode is '47' (9.1.13.7 item 7, Table 20's blank Output/Extend cells); an unsuccessful START arms 9.1.13.7
  item 6 a)'s '46' on the next sequential READ. Goldens: `conformance:2002/pb352_start_sequential_first_last`
  (record sequential, at the oldest edition START FIRST/LAST exists at),
  `conformance:2023/pb352_start_sequential_first_last_line` (the LINE-sequential framing, 2023 because the
  organization is — kb/Work PB688) and the Sequential-row START cells of
  `conformance:2023/l1_table20_seq_relative`.
- **The START key operand is screened by TWO rules with one home each** (kb/Work PB354).
  `RecordLayout.KeyIndexOfKeyItem` answers "this IS a record key of the file" — by reference identity, or by
  12.4.5.12.4 GR4's identical BYTE POSITIONS in another record description entry of the SAME file (hence
  equal widths, never a prefix) — and it is what 14.9.30.3 SR11 (`READ … KEY`, which has no generic arm at
  all) and 14.9.41.3 SR6 a) ask. `RecordLayout.GenericKeyIndex` answers SR6 b) alone, and enforces all three
  of its conditions: leftmost-coincident *within a record of the file* (b 1.), the same class, category and
  usage as that key (b 2. — via the ONE 8.5.2.1 Table-2 classifier), and no longer than it (b 3.). SR4's
  "shall not be subject to any OCCURS clauses" is its OWN named check on BOTH organization arms, through
  `RecordLayout.IsSubjectToOccurs`, ahead of the position walk — it used to be a side effect of the offset
  walk bailing out, reported under SR6's message, and the relative arm had no such check at all. The same
  predicate supplies the three key clauses' identical bans (12.4.5.12.3 SR1 RECORD KEY, 12.4.5.6.3 SR1
  ALTERNATE RECORD KEY, 12.4.5.13.3 SR1 RELATIVE KEY) in `KeyedIoBinder.KeyedValidateFile`.
- READ INTO and WRITE FROM lower to the verb plus a typed group MOVE (receiving uses the MAX length for ODO records, the ST146A lesson).
- Record length mismatch on READ (a fixed file whose physical record differs from the FD size) gives status 04; add for conformance since the legacy pads silently.
- LINE SEQUENTIAL: newline-framed, TrimEnd on WRITE, pad or truncate on READ, LastRecordLength is the line length; status **06 and 09 are both implemented** — 06 is the GR15 over-length truncation (the file position indicator keeps the unread remainder, NOTE 3), 09 the GR16 character-set warning below. LINE SEQUENTIAL itself is a COBOL-2023 introduction; see Per-edition gating.
- **The LINE SEQUENTIAL CHARACTER SET is ONE set behind FOUR rules** (`LineSequentialCharacterSet`, kb/Work PB329). Annex A.1 item 115 makes the set a REQUIRED, documented determination and the standard names it from four places: 14.9.30.4 GR16 / 9.1.13.2 item 7 (a SUCCESSFUL READ whose record area holds a non-member ⇒ '09', the record still delivered), 14.9.51.4 GR23 (WRITE ⇒ unsuccessful, '71'), 14.9.35.4 GR17 d) (REWRITE ⇒ unsuccessful, '71') and 9.1.13.10 item 1 (both write directions leave the record area — and the medium — unchanged). **The determination is: every character at code point U+0020 or above is a member; the C0 controls below it are not** (derivation and the GnuCOBOL survey at `docs/CONFORMANCE.md` DOC-A.1-115). Design consequences: (a) the set lives in ONE type and the connector reaches it through ONE predicate, `SequentialConnector.RecordAreaOutsideLineCharacterSet`, so the read arm and all THREE write entry points (`Write`, `WriteAdvancing`, `WriteBeforeAndAfter`) cannot diverge — before PB329 only REWRITE had an arm and it carried a private CR/LF test; (b) the subject is the RECORD AREA, tested CHARACTER-wise, so a national record area is read two bytes at a time as UTF-16BE exactly as `FitRecord`/`TrimRecordEnd` pad and trim it (a byte-wise test would refuse every national line sequential record); (c) GR16 is stated after GR15 and asks only that the read be successful, so '09' is the status that lands even on a truncated ('06') read.
- **The RECORD-AREA CATEGORY flag (`NationalRecordArea`) is set from ANY of the FD's record descriptions, not from the widest one.** 13.18.33.4 GR3 — "Multiple level 1 entries subordinate to a FD or SD entry represent implicit redefinitions of the same area" — makes them all descriptions of ONE area, and 14.9.51.4 GR21/GR22 key the trailing-space rule on *record-name-1*, so a WRITE naming the national record must shed national spaces whatever a sibling description says. Keying on the widest description alone (the original PB327 selector) silently answered "alphanumeric" for `01 R PIC N(4). 01 B PIC X(8).`, so that FD's WRITE shed one 0x20 with `string.TrimEnd` and left a seven-byte line ending in half a national position; the READ then re-padded alphanumerically to the same eight bytes, so the golden that was meant to pin the national fill never exercised it. ⚠ The flag is per-CONNECTOR while GR21/GR22 are per record-name-1; an FD carrying both a national and an alphanumeric record description answers national for both. Carrying the category on the statement is the shape that would separate them, and it is deliberately not built while no corpus program writes the alphanumeric sibling of a national area — a second national axis to say so would be the two-mechanism anti-pattern.
- CODE-SET translates only character (alphanumeric and DISPLAY-numeric digit) bytes, not COMP or COMP-3 binary fields (13.18.13); the default is the native ASCII set.
- LINAGE:
  - **Counter-only physical model** (13.18.34 GR8 — "each logical page is contiguous to the next with no additional spacing"): the connector's pending-advance print stream is UNTOUCHED (no margin blank lines, nothing at page wrap; ADVANCING PAGE stays one `\f`); the whole feature is `SequentialFile`'s counter machine + `EndOfPage` flag + the LINAGE-COUNTER register. An ADVANCING or LINAGE file is forced to line-oriented output (a LINAGE file's plain WRITE reroutes to the advance-1 print path from its FIRST write — 14.9.51 GR25 / 13.18.34 GR7c3).
  - **One statement-supplied operand set for both forms** (13.18.34 GR6): every OPEN and every sequential WRITE entry takes a `LinagePage?` — `SequentialIoEmitter.LinageArg(file)` renders `new LinagePage(body, footing, top, bottom)` from the file model (literals fold to constants, GR6a; data-names render the EXECUTING element's field reads, GR6b) or `null` for an FD with no LINAGE clause. The runtime adopts the values at OPEN OUTPUT (GR6b1, with counter := 1 per GR7d — `FileRegistry.SharedOpenAttempt` → `SequentialConnector.BeginLinagePage`, at the COMPLETION of a successful open as GR6b1 says) and at the two page transitions — the ADVANCING-PAGE reset (GR6b2) and the overflow wrap (GR6b3), AFTER the overflow decision against the OLD body, because "the value applies to the next logical page". Evaluating data-names at these page transitions (not only at OPEN) is required by GR6b2/GR6b3 — evaluating solely at OPEN is a conformance hole (SQ208M/SQ210M). **⛔ The page model is connector state; the operand SOURCE is not** (kb/Work PB673): a connector-held evaluator closure belonged to whichever activation installed it last, which for a RECURSIVE unit with a LOCAL-STORAGE operand is a RETURNED activation's dead storage — and it was installed on the program path only, so a class's LINAGE FD (`OoEmitter`) had no page model at all. `HasLinage` is gone with it: "this FD has a LINAGE clause" is now exactly "the statement supplied a page".
  - **GR26 operational mapping** (the legacy `AdvanceLinageCounter` ported verbatim, proven over the SQ goldens): with post-advance counter c, body B, footing F — `c > B` ⇒ overflow end-of-page, counter := 1 (GR26a + GR7c4; the reposition lands on line 1, never a modulo carry); else `F > 0 ∧ c ≥ F` ⇒ footing end-of-page (GR26b). ADVANCING PAGE: counter := 1, no observable EOP (SR18 bars PAGE+EOP). The counter advances in the CONNECTOR as part of EVERY write (EOP phrase or not), after the physical presentation — an AT branch reads the post-advance counter.
    - ⚖ **`c == B` IS AN ADJUDICATED BOUNDARY, DECIDED AGAINST THE PRINTED ARM TEXT — do not "fix" either comparison to match GR26's words.** GR26 a) as printed fires at `c ≥ B` and GR26 b) is clamped to `c < B`; at `c == B` those two cannot both hold with §13.18.34.4 GR2 (all B lines may be written), GR3 (the footing area is [F, B] INCLUSIVE) and GR26's own lead sentence (the lines "do not fit within the current page body"). The strict `c > B` boundary above is the reading `docs/CONFORMANCE.md` §4 records as **DETERMINATION — the §14.9.51.4 GR26 a)/b) boundary at LINAGE-COUNTER = page size** (kb/Work PB686), with the survey (IBM, Micro Focus, GnuCOBOL-via-NIST) and the NIST SQ201M evidence. It is pinned at the boundary by `tests/conformance/2023/pb686_linage_gr26_boundary.cob` and its `85/` twin — **including the no-FOOTING arm**, because the literal reading would make the last body line unwritable for a file that never mentions FOOTING, and a FOOTING-only fixture cannot see that.
  - **LINAGE-COUNTER register** (8.4.3.14): runtime-sourced (`BoundLinageCounterRef` → `CobolFile.LinageCounter(name)`), never a synthesized storage item (only the IOCS modifies it, GR7b); qualified `OF/IN file-name` resolves via the grammar's dedicated alternative, unqualified requires exactly one LINAGE file (SR3/8.4.2.2, ambiguity is a bind-time diagnostic). `ReferenceResolver.Resolve` returns null for the register early (the qualified form's cobolWord is the FILE-name).
  - **END-OF-PAGE phrases** branch on `CobolFile.EndOfPage(name)` read in the `if` HEADER (a branch body may WRITE the same file — SQ208M); EOP is a SUCCESSFUL write (GR27a — status 00, no USE hook competition). Bind-time diagnostics: SR19 (EOP without LINAGE — the old silent-drop), SR18 (PAGE+EOP), SR13 (mnemonic ADVANCING on a LINAGE file).
  - **EC-I-O-LINAGE seam** (GR6 value rules): the evaluator validates body > 0 and 0 < footing ≤ body (footing 0 = absent phrase) and throws LOUD until the EC subsystem lands — never a silent bad page model.
  - Conformance net: `LinageConformanceTests.cs` (per-GR: GR7c1–c4/GR7d, GR26a/b discrimination incl. c==B, GR6b1/2/3 timing, GR1 no-footing, qualified/ambiguous register, ADVANCING 0, SR13/18/19).
- On-disk framing: a fixed record-sequential file is contiguous; a variable sequential, relative, or indexed file uses a 4-byte little-endian length prefix; a sparse relative file uses 0xFF gaps.
- DELETE FILE statement (14.9.10 Format 2): delete the host path and reset the in-memory map. A present file that is deleted gives 00 and an absent file gives 05 — BOTH successful (14.9.10 GR14/GR20); the error paths are 41 (the connector is still open, GR13), 62 (the physical file is open by another connector, GR15), and 37 (insufficient authority or the storage medium forbids deletion, GR16/GR17). A missing file is NEVER 35 — 35 is an OPEN-only status.
- Keyed record stores are PER PHYSICAL FILE, not per connector (kb/Work PB143; §14.9.10.4 GR5 — "removed from
  the physical file"): `KeyedStoreTable` (registry-owned, keyed by resolved host path — the same key
  `PhysicalFileTable` arbitrates sharing and locks by) holds ONE `RelativeStore` (RRN → image) or `IndexedStore`
  (the records plus the GLOBAL release-ordinal mint) per host. The FIRST opener loads from disk; later openers
  ATTACH to the live store (never reload — the in-memory store is the truth while any connector holds it); every
  DELETE/WRITE/REWRITE is instantly visible to every attached connector; any CLOSE persists the one shared state
  (close order cannot resurrect a deleted record or drop another connector's write); the LAST detach drops the
  entry so a later OPEN re-reads the disk; OPEN OUTPUT empties the shared view. Position/key state (FPI, key of
  reference, the §14.9.51 GR29a sequential-WRITE slot, the GR38 high-key) stays per-CONNECTOR. The store is
  unconditional — two SELECTs to one ASSIGN target reach it with no SHARING clause — and sequential connectors
  are out of its scope (their OS-backed streams are already the shared store). A connector constructed outside a
  registry (a focused unit test) keeps a private store.
- Indexed duplicate order is PER KEY OF REFERENCE, and the file position indicator is a KEY VALUE (kb/Work
  PB341, PB342). 14.9.30.4 GR26 scopes the release order to "an alternate record key that is the key of
  reference", and 14.9.35.4 GR24 a) freezes an untouched key's order across a REWRITE while b) moves the record
  last under a changed one - so the release ordinal is a VECTOR on `KeyedRec`, one slot per key (slot 0 the
  prime, assigned at release and never re-stamped, which makes it the record's physical release order too).
  WRITE stamps one fresh ordinal into every slot; REWRITE re-stamps only the keys whose value or SUPPRESS WHEN
  state changed, and "changed" is ONE predicate (KeyEq under the key's collating sequence, per GR24's closing
  sentence) that GR24 a), GR24 b), GR24's suppression sub-rules and the 9.1.13.2 2 c) '02' all read.
  14.9.41.4 GR17 e) 1. puts only a KEY VALUE in the file position indicator, so a START-seeded walk enters a
  duplicate set at the end GR26 names for ITS direction - first-released forward, last-released backward - and
  the duplicate-set position of a prior READ is separate connector state, which is what 14.9.30.4 GR21 rules
  e)/f) name instead of the indicator. CLOSE persists a TOPOLOGICAL order of the per-key duplicate orders
  (`IndexedConnector.PersistOrder`), so a reload - which can only give every key the file's own order -
  reproduces all of them; with no REWRITE repositioning that IS release order, byte-identical to before. RESIDUE:
  the per-key orders can be made mutually cyclic, and one sequence of record images cannot then carry them.
- I-O status discipline (kb/Work PB140): `FileConnector.Status`'s setter is the ONE assignment path — it records `EverAccessed` AND drops the §9.1.13.7 3) '43' gate (READ terminals re-arm through `ReadSucceeded`); openness is the ONE base `_openMode` bit, separate from the `OptionalAbsent` file-position state a CLOSE leaves unchanged (§14.9.6.4 GR6); `FileRegistry` throws on an unregistered or misrouted name (never a fail-open '00' — the SD/organization screens reject at bind time, COBOLNET1692/1693); `FileConnector.Close` maps OS failures to '30' (§9.1.13.6 item 1) with the sequential streams nulled either way; CLOSE WITH LOCK locks only on a successful close.
- Keyed-verb branch discipline (kb/Work PB325): the ACCESS MODE is the SOLE discriminator of a keyed verb's branch, and it lives in ONE place — `KeyedConnector.Access`, the abstract base `RelativeConnector` and `IndexedConnector` share. Every branch the standard draws inside a keyed verb is drawn on it (§14.9.51.4 GR29 a)/b), GR38/GR39; §14.9.35.4 GR5 vs. GR21/GR22/GR23; §14.9.10.4 GR2 vs. GR3/GR4), and the OPEN MODE enters only as the permission test that FOLLOWS — Table 20 (§14.9.27.4 GR8), whose unsuccessful cells §9.1.13.7 items 8 and 9 name. ⛔ An open mode in a branch PREDICATE inverts that dependency: `_access == Sequential || Mode == Extend` made the runtime's answer depend on an unenforced bind-time screen (§14.9.27.3 SR2) and turned Table 20's blank Random/Dynamic × WRITE × Extend cell into a successful append instead of item 8 b)'s '48'. The whole table is walked by conformance:2023/l1_table20_seq_relative + l1_table20_indexed and, for the cell conforming source cannot reach, unit:Table20WriteOpenModeTests.
- READ preconditions live in the BASE, once, in the standard's own order (kb/Work PB336). `FileConnector` owns
  the three of them and every organization's READ entry delegates: `ReadOpenModeGuard` (§14.9.30.4 GR2 /
  §9.1.13.7 item 7 → '47'), `SequentialReadGuard` (GR2, then GR21's "if the previous READ or START statement for
  the file connector was unsuccessful … the I-O status is set to '46'", then §9.1.13.4 item 1 c)'s '10'), and
  `RandomReadAbsentOptionalGuard` (§9.1.13.5 item 3 b) → '23'). **The ORDER inside `SequentialReadGuard` is
  itself the rule**: the absent-OPTIONAL arm is an unsuccessful READ that arms the '46' poison, which is exactly
  what makes '10' apply only to a READ "attempted for the first time" — write the arms the other way and the
  '46' branch below is unreachable for the life of the open mode. Three private copies of this chain, all three
  in the wrong order, is what PB336 was; the goldens are `tests/conformance/{2023,85}/pb336_optional_absent_read_46*`
  and the structural gate is `ReadPreconditionOrderDriftTests`. The random-side guard is deliberately NOT the
  sequential one: GR21 opens "For a sequential READ statement" so a random READ never yields '46', and item 3 b)
  has no "first time" qualifier so every random READ on an absent optional file is '23' — but the failed random
  READ still arms the poison, because §9.1.13.7 item 6 b) ("The preceding READ statement …") is not restricted to
  sequential reads. START's own absent-optional rule (§14.9.41 GR5) is a different statement's rule and lives in
  each connector's own START body.
- **START's preconditions live in the same base, for the same reason** (kb/Work PB352). `FileConnector` owns
  `StartOpenModeGuard` — 14.9.41.4 GR1's "input or I-O" test, whose unsuccessful value is 9.1.13.7 item 7's
  '47' and whose blank cells are Table 20's — and the virtual `InvalidateFilePosition`, which is GR7 ("the file
  position indicator is set to indicate that no valid record position has been established"). The guard applies
  GR7 on the way out, because a refused START is an unsuccessful one: GR1's test had been written out FIVE
  times (twice in each keyed connector, once in the sequential arm) and every copy returned '47' leaving the
  indicator alone. A keyed connector overrides `InvalidateFilePosition` to clear its FPI VALIDITY BIT as well —
  its indicator is a key value plus a bit, where the sequential connector's is the stream position — and each
  connector's `StartFail` is that invalidation plus the invalid key condition's '23'.
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
- **READ PREVIOUS is not COBOL-85** (a 2002 introduction — derive from the 2002 standard): rejected at 85 on
  EVERY organization (the gate is one `IBoundRead` arm in the VersionConformancePass; it used to be two arms and
  the sequential one did not carry it — kb/Work PB334). FLAG-14 flags every READ PREVIOUS (row 108).
  ⚠ **The 2014→2023 after-OPEN change (row 29) is the INDEXED leg only.** Annex E.2 item 22 ("READ PREVIOUS
  statement following an OPEN statement. Ensure that an at end condition occurs.") is INFORMATIVE and names the
  rule it amended: §14.9.30.4 GR21's indexed sub-rule d.3, which in the 2023 text reads "If no such record is
  found or PREVIOUS is specified and the previous operation on the file was an OPEN statement, the at end
  condition exists." The RELATIVE and SEQUENTIAL sub-rule blocks print rule b) unamended — "the first existing
  record that is selected is made available, regardless of whether NEXT or PREVIOUS is specified" — so on those
  two organizations the after-OPEN behaviour is **identical at 2002, 2014 and 2023** and the connector takes no
  edition parameter for it. The sequential leg is asserted at all three editions by
  `conformance:{2002,2014,2023}/pb334_read_previous_sequential`; the relative leg is kb/Work PB343.
- **ORGANIZATION LINE SEQUENTIAL is a COBOL-2023 INTRODUCTION** (§12.4.5.10.3 GR2), so it is rejected at 85,
  2002 AND 2014 — `constructs.json` row `file-organization-line-sequential-2023`, gated on the clause's
  RECOGNITION by `VersionConformancePass.ParseArm.VisitOrganizationClause` (COBOLNET0900). **The edition IS
  derivable from the 2023 spec after all** (kb/Work PB688 corrected the earlier "not derivable — derive it from
  the 2002 standard" reading): the **Foreword's** list of the main changes this third edition makes over
  ISO/IEC 1989:2014 names “Line Sequential file organization” outright. Annex E happens to carry no item for
  it and `VERSION_CHANGE_REFERENCE.md` no row, which is what made it look underivable. §9.1.6 / §9.1.7.1 still
  name exactly THREE organizations, so LINE SEQUENTIAL is a *phrase* selecting the line-delimited type of the
  sequential organization (§9.1.7.2) — the `{ LINE | RECORD }` inner choice of the §12.4.5.10.2 general format.
  Two consequences the corpus had to absorb: every golden below 2023 that named the organization moved to
  `tests/conformance/2023/`, and **a report file COBOL.NET writes cannot be read back at all below 2023** —
  the report writer frames a report file as CRLF-delimited text whatever its ORGANIZATION, and only a
  line-sequential READ recovers those lines, so the 85/2002 report goldens that observed their report by
  re-reading it are 2023 programs. **`RECORD SEQUENTIAL` — the other half of the 2023 inner choice — is NOT
  yet accepted by the grammar** (`organizationType` has `LINE SEQUENTIAL | SEQUENTIAL | RELATIVE | INDEXED`);
  when it lands it gates from the SAME arm.
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

## The `record-name-1` operand — ONE rule for WRITE, REWRITE and RELEASE

Three statements print `record-name-1` in their general format, and all three say the same thing about it:

- WRITE §14.9.51.3 SR5 — *"Record-name-1 is the name of a logical record in the file section of the data
  division and may be qualified."*
- REWRITE §14.9.35.3 SR1 — the identical sentence.
- RELEASE §14.9.32.3 SR1 — *"Record-name-1 shall be the name of a logical record in a sort-merge file
  description entry and it may be qualified."*

**The rule has ONE home: `StatementValidation.ResolveRecordName`**, which returns the owning `FileModel` or
false having reported (COBOLNET1757), in `ResolveFile`'s shape. It enforces two things:

1. **Identity, not containment.** The reference shall BE one of the file's `Records`. Its predecessor,
   `SequentialIoBinder.FileOfRecord`, walked the reference's `DataItem` up to its top-level 01 and tested THAT
   root, so what it enforced was *the reference lies somewhere inside a record* — and every subordinate item
   passed, at all three verbs, with no diagnostic at any `--std`. The released/written width is taken from the
   REFERENCE, so the wrong operand produced a wrong-**width** record rather than anything loud: `RELEASE
   SR-DATA` on an 8-byte SD injected a 5-byte image, space-extended to 8, as a record the program never
   released. The upward walk survives as the message's explanation — naming the record the operand is
   subordinate to is the repair.
2. **No reference modification.** A record-name is a user-defined word (§8.3.2.2.25); §5.2.4's operand table
   gives that operand type *"User-defined word, including qualification and subscripting if needed"* and
   nothing further, and §8.4.3.3.3 SR5 permits reference modification only *"anywhere an identifier referencing
   a data item of class alphanumeric, boolean, or national is permitted"* (its NOTE: *"where data-name-n is
   used in a general format or syntax rule, then reference-modification is not permitted"*). The printed
   RELEASE format writes `record-name-1`, not `identifier-1`. A reference modifier rides on the `RefModPlace`
   DECORATOR and leaves `Place.Item` untouched, which is exactly why a containment test could not see it.

RELEASE's second half — *"in a **sort-merge** file description entry"* — is RELEASE's alone and stays in
`CheckReleaseRecord`, which is now asked only of a reference that already IS a logical record and so takes a
non-null `FileModel`.

**Grammar unchanged.** `releaseStatement : RELEASE dataReference` and `recordName : dataReference` stay
general. Both halves of one syntax rule belong in one place, and the subordinate-item half cannot be decided by
a grammar at all; narrowing `recordName` would also turn a citable diagnosis into a parse error.

**Staging.** ISO §4.2.2 ¶2 makes the compile-time indication mandatory for *"violations of the general formats
and the explicit syntax rules of standard COBOL"*. WRITE and REWRITE previously staged an unresolvable record
to `BoundUnsupported`, so `WRITE WS-REC` drew only the COBOLNET1756 **deferral warning** — the compiler
announcing ITS OWN gap for what is the source's error. The two arms are now separate: a name that resolves to
nothing already has COBOLNET1639 (§8.4.2.1) from the resolver and stays a deferral; a reference that resolves
but is not a logical record is refused at bind.

**Edition posture.** Edition-INVARIANT — the rule is written identically in 1985, 2002 and 2014, so there is no
gate and no `constructs.json` row. Witnesses: `tests/conformance/negative/pb347-*` (seven cases, three verbs ×
the subordinate / reference-modified / not-a-file-record faults, each `*> reject-at: 85 2002 2014 2023`) and
the acceptance twins `tests/conformance/2023/pb347_record_name_identity` and
`tests/conformance/85/pb347_record_name_identity_85`, which pin that the qualified `OF` form still binds at all
three verbs and that a SORT returns exactly the records RELEASE named. (kb/Work PB347)

## The COLLATING SEQUENCE clause — SR8 counts CLAUSES, not occurrences

§12.4.5.7.3 SR8 says *"Neither data-name-1 nor record-key-name-1 shall be specified in more than one COLLATING
SEQUENCE clause"*, and the unit it counts is the **clause**. §12.4.5.7.2's Format-2 figure prints
`COLLATING SEQUENCE OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3` with the ellipsis immediately
right of the closing brace, so per §5.2.7 the repeated portion is the brace group and no rule adds a distinctness
requirement to the repetition: **one clause may list the same key twice, and one clause is never *more than
one*.** §12.4.5.7.4 GR6 is indifferent to the repeat — *"Alphabet-name-3 applies to record keys identified by
data-name-1 or record-key-name-1"* names the clause's own alphabet either way.

`DataBinder.ResolveFileCollating` therefore screens SR8 with the shared `ConstructOperandRegister<string>`
(`Binding/ConstructOperandRegister.cs`), whose construct here is the Format-2 CLAUSE, per file control entry —
never a `HashSet` hoisted out of the loop over `FileModel.KeyLevelCollating`, which is what the register exists to
make impossible: that shape screened per NAME across every clause at once and rejected
`COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV` with a diagnostic (COBOLNET1582) whose own text — *named in more
than one COLLATING SEQUENCE clause* — the program falsified. The same register enforces §14.9.49.3 SR7/SR8/SR9/SR14
over the USE statement (`COBOLNET_CONDITIONS_EXCEPTIONS_DESIGN.md`, kb/Work PB364); it is one mechanism for every
rule whose boundary is a construct and whose subject is an operand written inside it. Because the register
remembers the LAST clause that named a key rather than the first, ONE violation is ONE diagnostic even when the
offending clause also repeats the key internally.

**Edition posture.** The file-control COLLATING SEQUENCE clause is a COBOL-2002 addition (`constructs.json`
`file-collating-clause-2002`, COBOLNET0900 at `--std 85`), so SR8's behaviour lanes are 2002/2014/2023 and 85 is the
gate lane. Witnesses: `conformance:FileCollatingSequenceSpecTests` (both arms per edition, the
one-diagnostic count, and the 85 gate), the positive golden
`tests/conformance/2002/pb703_collating_key_named_twice` (a key named twice in ONE clause, whose output proves
GR6 still gives each key its own clause's alphabet) and the negative
`tests/conformance/negative/pb703-collating-key-in-two-clauses` (`*> reject-at: 2002 2014 2023`). (kb/Work PB703)

## ISO citations

- ISO/IEC 1989:2023 section 12.4.5.7 COLLATING SEQUENCE clause: 12.4.5.7.2's two general formats (file-level and key-level), 12.4.5.7.3 SR3 (at most one file-level clause per file control entry), SR4/SR5 (a Format-2 name shall be a declared RECORD KEY or ALTERNATE RECORD KEY), SR8 (a key in at most one clause) and 12.4.5.7.4 GR2–GR6 (which sequence applies to which key), with 5.2.7 for the ellipsis that makes the Format-2 brace group repeatable.
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
- Q4 — **RESOLVED: implemented, not deferred.** LINE SEQUENTIAL status 06 landed with the GR15 remainder machinery, and 09 landed with kb/Work PB329 together with the Annex A.1 item 115 determination it delegates to (`docs/CONFORMANCE.md` DOC-A.1-115) and the WRITE-side '71' arm (14.9.51.4 GR23) that had never existed. See the LINE SEQUENTIAL edge-case notes above.
- Q5 minor: confirm SAME AREA buffer-only and SAME SORT-MERGE AREA are acceptable no-ops in a managed runtime.
