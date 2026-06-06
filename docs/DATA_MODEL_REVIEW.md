# DATA_MODEL_ARCHITECTURE.md — Adversarial Review Report

**Reviewed document:** `E:\CobolSharp\docs\DATA_MODEL_ARCHITECTURE.md`
**Review date:** 2026-06-06
**Reviewer:** synthesis lead (adversarial panel; 48 raised findings, 3 refuted/excluded, 6 completeness gaps)
**Decision class:** foundational, multi-session — owner sign-off to begin migration

---

## 1. Executive summary and overall verdict

### Verdict: **proceed-with-changes**

The architecture is sound and the migration plan is defensible. The core bet — typed-native as the
default, with byte-backing as a classifier-scoped, conservatively-triggered exception, and an
always-valid byte-window fallback at the `IDataSlot` chokepoint — is the right design and is *not*
invalidated by any finding. The "any doubt → byte" rule, the debug-build round-trip assertion, and
the Stage-0-everything-byte-backed start give the plan a genuine safety floor. **Crucially, no
production code is affected today**: every finding is against a forward-looking design document for a
migration that has not begun. The cost to fix each gap now (a sentence in the ADR) is trivial
compared to discovering it as a silent-corruption hunt during Stage 3.

That said, the document as written contains a **small number of genuine specification gaps in the
classifier trigger list (§3)** that, if implemented literally, would produce silent wrong results.
The most important of these are not exotic — they are the *most common* idioms in real COBOL:

- **CALL ... USING BY REFERENCE** has no classifier trigger. A `record struct` is a value type; the
  callee's mutation is lost, and a separately-compiled callee's LINKAGE re-description has no
  contiguous byte image to alias. This is the single most dangerous omission.
- **Reference modification of an elementary numeric DISPLAY item** (`MOVE WS-DATE(1:4) ...` on a
  `PIC 9(8)`) has no character image in the typed `long`/`decimal` model, and trigger #3 as written
  does not fire on it. This is the everyday date-slicing idiom.
- **Group MOVE / COMPARE / class-condition over a group containing COMP/COMP-3/BINARY** members
  cannot be reproduced from decoded values; the spec forbids internal-representation conversion, and
  same-layout struct copy can corrupt non-canonical packed content.
- **PROGRAM COLLATING SEQUENCE** is silently dropped by mapping alphanumeric comparison to
  `string.CompareOrdinal` — a wrong-results regression for any program with a custom alphabet, and
  this is an existing, tested feature.

These are all closable with additions to the §3 trigger list and clarifications to §2.4 / §2.5 / §4.
None requires architectural rework. **The owner should require the §3 trigger additions and the
collating-sequence resolution as a precondition to Stage 3, and should resolve the numeric-substrate
open question (Open Question #1) before Stage 1, because it determines whether `decimal` is even a
legal default.**

### Finding counts (post-adjudication, de-duplicated)

| Severity (adjudicated) | Count |
|---|---|
| Critical | 0 |
| High | 4 |
| Medium / owner-decision | 6 |
| Low / partial-overstated | 18 |
| Completeness gaps to investigate | 6 |

(No finding survives adjudication at *critical*. Four findings raised as "critical" were downgraded
to *high* because the ADR is a pre-implementation design with a byte-backed safety floor; the defect
is real but no shipped code is wrong. The two `confirmed` findings — CALL BY REFERENCE and
PROGRAM COLLATING SEQUENCE — are both high.)

---

## 2. High findings (must be fixed in the ADR before Stage 3)

### H1. CALL ... USING BY REFERENCE has no classifier trigger — value-copy loses callee mutations and separate compilation breaks the alias contract
*(De-duplicated: this consolidates the `clr` "record struct passed by value", the `classifier`
"separate compilation fixpoint", the `classifier` "dynamic CALL", and the `migration` "CALL BY
REFERENCE / EXTERNAL" findings — all four describe the same root cause.)*

**Problem.** §4 maps a group to a .NET `record struct` (a value type); §1.1 stores `PIC 9(6)` as a
`long` value, never bytes. ISO §14.9.4.4 GR11 / §14.2.3 GR8 (spec line 23740) require a BY REFERENCE
formal parameter to "occupy the same storage area as the argument" — a byte-level aliasing contract.
The current runtime honors this: `CobolDataPointer.CreateByReference(byte[], offset, len)`
(CobolDataPointer.cs:33-35) passes a raw window into the caller's storage with `PicDescriptor` =
`default!`, and the callee imposes its own LINKAGE layout. The §3 trigger list (items 1–10) names
EXTERNAL/GLOBAL (trigger 8) but **never names CALL ... USING BY REFERENCE arguments or LINKAGE
SECTION items.** Consequences:

1. A `record struct` argument is passed by value; a callee's mutation updates the copy and is lost.
2. Under separate compilation (CLI compiles one source file; dynamic CALL resolves at runtime via
   `CobolProgramRegistry.Resolve` / `Assembly.LoadFrom`), caller A's classifier cannot see callee
   B's LINKAGE re-description. A typed argument has no contiguous byte image for B to alias. This is
   in principle undecidable for dynamic CALL — no fixpoint, static or link-time, can enumerate the
   callee — so the only sound posture is to byte-back at the point of address-escape.
3. The R1 "whole-program fixpoint" mitigation is misnamed: the fixpoint runs over one compilation
   unit and cannot reach across compilation boundaries. The debug round-trip assertion guards this
   program's `IDataSlot` boundaries, which the cross-CALL alias never crosses — so the assertion is
   structurally blind to exactly this failure mode.

**Required ADR revision.**
- Add **trigger 11** to §3: *"any item used as a CALL ... USING ... BY REFERENCE argument (static or
  dynamic), and any item it transitively contains, is byte-backed — the callee receives a raw byte
  alias into this storage region."*
- Add **trigger 12** to §3: *"all LINKAGE SECTION items are byte-backed — they are formal parameters
  whose storage is owned by the caller and whose layout the compiler cannot renegotiate without
  changing the CALL ABI."*
- Correct the R1 row in §12: replace "whole-program fixpoint" with "per-compilation-unit fixpoint;
  address-escape via CALL BY REFERENCE is an *unconditional* byte trigger precisely because the
  callee's view is unavailable at the caller's compile time." Note that for cross-program/dynamic
  cases the safety net is the conservative trigger, **not** the debug assertion.
- Add a cross-program CALL conformance test (the NIST IC-series is exactly this scenario) to the
  guard before any Stage-3 typed-argument widening.

---

### H2. Reference modification (and class/alphanumeric examination) of an elementary numeric DISPLAY item has no character image in the typed model, and the classifier does not catch it
*(De-duplicated: consolidates the `overlay` "refmod of numeric DISPLAY", the `classifier` "any doubt
presumes detect doubt", and the `spec` "refmod of a DISPLAY numeric" findings.)*

**Problem.** ISO §8.4.3.3.4 GR2 (spec line 7075) and §8.4.3.3.3 SR1 (line 7047): a USAGE DISPLAY
item whose category is other than alphanumeric is, for reference modification, "operated upon as if
it were redefined as a data item of class and category alphanumeric of the same size." So
`NUM-FLD(2:3)` on `PIC 9(6)` value 42 must read characters 2–4 of the DISPLAY image `"000042"`. A
.NET `long 42` has no positional character image. §3 trigger #3 is written as *"refmod that slices
across heterogeneous storage ... pure char-position refmod on a string does not trigger"* — a
numeric DISPLAY item is neither a `string` (so the "char-position on a string" path is inapplicable)
nor heterogeneous storage (it is a single homogeneous field), so the trigger **does not fire**. The
item stays a `long`, the operation is unimplementable, and lowering silently produces wrong results.
This is the single most common refmod-on-numeric idiom (extracting YY/MM/DD from a `PIC 9(8)` date),
and the same hole applies to a class condition / INSPECT / STRING that reads a numeric DISPLAY item's
digit characters, and to signed-DISPLAY overpunch bytes that are positionally addressable.

The framing flaw is deeper than one missing case: trigger #3 frames the decision as *"does it
type-pun"* (a runtime property — whether `x(s:l)` straddles a binary child depends on runtime `s`/`l`)
rather than the statically-decidable *"could it ever observe non-homogeneous bytes."* "Any doubt →
byte" is only sound if the classifier conservatively byte-backs *every* group/item that is the target
of refmod unless it is provably a single homogeneous alphanumeric/national elementary item.

**Required ADR revision.** Rewrite trigger #3 in §3 as a decidable over-approximation with explicit
sub-rules:
- *3a.* Byte-back any data item that is the identifier-1 of a reference modification **unless** it is
  statically a single elementary alphanumeric or national item (a true uniform `string`).
- *3b.* ANY refmod of a non-alphanumeric DISPLAY (or NATIONAL) numeric, numeric-edited, or
  overpunch-signed item triggers byte-backing (per §8.4.3.3.4 GR2 — there is no string to `AsSpan`
  over). Independent of literal vs runtime bounds.
- *3c.* A variable-bound refmod (non-literal `s` or `l`) over a group containing heterogeneous (mixed
  numeric+char, or COMP) content triggers byte-backing — the pre-scan cannot prove the slice stays in
  a homogeneous run.
- Add to the §3 soundness paragraph: "any doubt" explicitly covers non-literal start/length over
  groups, and refmod is *never* assumed to be "pure char-position on a string" unless the base is a
  proven-homogeneous string field.

---

### H3. Group MOVE / COMPARE / class condition over a group containing COMP/COMP-3/BINARY cannot be reproduced from decoded fields
*(De-duplicated: consolidates the `spec` "group MOVE/comparison with COMP members" and the `spec`
"IS NUMERIC / class condition on a group" findings. The `overlay` "group MOVE as byte windows" and
`overlay` "REDEFINES class straddling the boundary" findings are related and folded into the punch
list.)*

**Problem.** ISO §14.9.25 GR4 (spec line 28901): a non-elementary (group) move "is treated exactly
as if it were an alphanumeric to alphanumeric elementary move, except that there is **no conversion
of data from one form of internal representation to another** ... the receiving area will be filled
without consideration for the individual elementary or group items." Line 9513: a group compares as
elementary alphanumeric over its raw byte image. The ADR's §2.4 *"identical-layout group MOVE =
struct/field-wise copy"* and §4 *"identical-layout group MOVE = struct copy"* are byte-equivalent
**only when every transitive member is usage DISPLAY/NATIONAL** (where decoded value == raw bytes).
For a group with COMP-3 members, a struct copy re-encodes each `decimal` through the codec, which need
not reproduce non-canonical sign nibbles (0x0F vs 0x0C) or redundant packed digits — corrupting the
byte image a subsequent alphanumeric view would read. §3 trigger #4 catches only *dissimilar-layout*
group ops and "group used as an alphanumeric operand while containing numerics"; a **same-layout**
COMP-3-bearing group-to-group MOVE meets neither condition and is routed to the corrupting struct
copy. Class conditions (IS NUMERIC / IS ALPHABETIC / class-name) over a group are likewise unlisted
and require the raw concatenated byte content (e.g. `IF G IS NUMERIC` on a group containing a COMP
field must inspect the binary bytes, which are almost never ASCII `'0'`–`'9'`).

**Required ADR revision.** Extend §3 trigger #4 to read: *"dissimilar-layout group MOVE/COMPARE/CORR,
**OR any group MOVE/COMPARE where any transitive member has non-DISPLAY usage (COMP, COMP-3, COMP-5,
BINARY, COMP-1, COMP-2, FLOAT-\*)**, OR a group that is the operand of a class condition (IS NUMERIC /
IS ALPHABETIC / IS ALPHABETIC-LOWER / IS ALPHABETIC-UPPER / IS BOOLEAN / IS class-name), or a group
used as an alphanumeric operand while containing numerics."* Add a note to §2.4 and §4's group row:
"struct copy is byte-equivalent only when every transitive member is DISPLAY/NATIONAL." Also extend
the Phase B scan description from "refmod/group/pointer usage" to explicitly include class conditions.
Owner must accept that COMP-bearing records — the majority of line-of-business data — stay byte-backed,
which materially narrows the "typed-native is the default" claim for commercial code.

---

### H4. Mapping alphanumeric comparison to `string.CompareOrdinal` silently drops PROGRAM COLLATING SEQUENCE
*(`stringdefault` — confirmed.)*

**Problem.** §1.2.1 maps *"alphanumeric comparison → string.CompareOrdinal / span.SequenceCompareTo
... ordinal"* and Guardrail 1 frames "always Ordinal" as THE correctness fix. Ordinal protects
against culture folding, but COBOL alphanumeric comparison is defined over the **PROGRAM COLLATING
SEQUENCE** (OBJECT-COMPUTER / ALPHABET, ISO §8.8.4.1.2), not native code-point order. The compiler
already implements this correctly via a 256-entry ordinal→weight table
(`PicRuntime.CompareAlphanumericWithSequence`; `ConditionLowerer` dispatches to
`IrStringCompareWithSequence` whenever `ProgramCollatingSequence != null`). `string.CompareOrdinal`
over UTF-16 has no hook for this table; switching the default to it makes every `IF A < B` under a
custom alphabet (e.g. EBCDIC on an ASCII host) compare by Unicode code point — silently wrong output.
The ADR's own Guardrail 3 says "program-collating-sequence effects on comparison are implemented by
us" — which directly contradicts the unqualified `CompareOrdinal` table row. The collating subsystem
is marked COMPLETE in project memory, so this is a regression of a tested feature, not a hypothetical.

**Required ADR revision.** Remove the unqualified "alphanumeric comparison → CompareOrdinal" row.
Document one of three explicit options (and amend Guardrail 1 to match):
- (a) classify any item whose program declares a non-identity PROGRAM COLLATING SEQUENCE as
  byte-backed (conservative — collating-sensitive programs stay on the proven byte path); **or**
- (b) adapt `IrStringCompareWithSequence` to operate on `ReadOnlySpan<char>` using a 65 536-entry
  `ushort` weight table built at program load from the 256-byte COBOL sequence; **or**
- (c) restrict the `CompareOrdinal` mapping to programs with no active collating sequence and route
  the collating case through (a) or (b).
The same caveat applies to `FUNCTION CHAR`/`ORD` (already noted as "collating-sequence-aware") and to
SORT/MERGE keys.

---

## 3. Medium findings and decisions for the owner

These are real gaps with lower blast radius, or genuine design decisions the ADR defers.

### M1. Numeric substrate must be decided **before Stage 1** (Open Question #1).
`decimal` holds 28–29 digits; ISO §13 mandates 1–31 digit positions (spec line 20332). The ADR
acknowledges this as R5 but scopes it to COMP/COMP-3 only. Two harder cases are omitted: (1) **DISPLAY
9(19..31)** — today's `DecodeDisplay` falls through `long.TryParse` then `decimal.TryParse`, returning
`0m` on >28 digits (silent decode-to-zero); (2) **intermediate precision** — the differential oracle
(Stage 1) compares `CobolNum.Store` against the *same* decimal-based byte runtime, so it is
**common-mode blind** to every precision loss above 28 digits (both sides agree on the wrong/zero
answer). *Owner decision:* require that `CobolNum`'s internal intermediate for the 19–31-digit range
is `BigInteger` (or equivalent), **not** `decimal`, and that the Stage-1 oracle's grid includes
>28-digit values validated against an **independent** high-precision reference (not the byte runtime).
Also expand R5 to explicitly name DISPLAY 9(19+).

### M2. COMPUTE intermediate arithmetic throws `OverflowException` — the "TryStore never throws" guarantee does not cover intermediate add/multiply/power.
§5 promises `TryStore` never throws, but it guards only the *final* store. Today's emitter lowers
intermediate ops to raw `decimal.op_Multiply` / `op_Addition` (CilExpressionEmitter.cs:111-125) with
no try/catch; only DIVIDE/Remainder go through `SafeDivide`/`SafeRemainder`. `COMPUTE X = A * B * C`
where `A*B` overflows decimal throws and crashes the run unit *before* ON SIZE ERROR can fire,
violating ISO §14.7.5 (size error during initial evaluation must leave resultants unchanged).
`Power` uses `Math.Pow(double,double)`, losing precision and producing `Infinity → garbage decimal`
on overflow. *Required:* add `SafeAdd`/`SafeSubtract`/`SafeMultiply` runtime helpers mirroring
`SafeDivide`, wire them into the Add/Subtract/Multiply IR cases, replace `Math.Pow` with a
BigInteger integer-power for integer exponents; amend §5/§9 to state that **all** expression-tree
operators are no-throw size-error-setting helpers, not just the final store.

### M3. COMP-5 / BINARY native truncation, TRUNC, and overflow wraparound are not modeled.
§4 maps COMP and COMP-5 both to `long`, collapsing the distinction that matters: COMP truncates by
PIC digit count (`mod 10^digits`), COMP-5 uses full binary capacity (PIC 9(4) COMP-5 holds 0..65535,
not 0..9999) with defined wraparound. `CobolNum.StoreInt(value, digits: n)` keyed off `digits` will
wrongly mod a COMP-5 value to its PIC digit count. *Owner decision:* NumProfile must carry a
`UsageKind` or `TruncationPolicy` (DigitCount vs BinaryCapacity); add COMP-5 full-range/wraparound
vectors to the Stage-1 grid; flag TRUNC(STD/OPT/BIN) as a future dialect-axis item in the conformance
plan (real programs depend on it; out of ADR scope but must be named).

### M4. SYNCHRONIZED slack and "dissimilar-layout" need a byte-offset-aware definition.
Trigger 7 gates SYNC on "another byte trigger," but two SYNC-bearing groups with identical declared
fields can have different slack placement; a group MOVE between them, or into a flat `X(n)` receiver,
must move the slack bytes verbatim (group move = alphanumeric, no conversion). *Required:* tighten
trigger #4's "dissimilar-layout" to "two groups are dissimilar-layout if their SYNC-aligned byte
offsets differ, even if declared fields are structurally identical," and require the classifier to
compute group byte-size from the SYNC-inclusive offsets (`StorageLayoutComputer` already does this
correctly). Document the implementor SYNC alignment rules (ISO §13.18.55 GR9 *requires* the
implementor to specify them).

### M5. De-editing and the alphanumeric→numeric / figurative→numeric MOVE cells need explicit byte routing.
§4 marks numeric-edited as an "edited-string projection (output)" — but de-editing (numeric-edited
*sender* → numeric receiver, ISO §14.9.25 GR5; EC-DATA-INCOMPATIBLE on impossible content, line
24904) requires reading the stored edited character image. An output-only projection has no de-edit
path. Likewise, alphanumeric→numeric MOVE (rightmost-31 positional reinterpretation) and figurative
→numeric (HIGH/LOW-VALUE/QUOTE are category alphanumeric per Table 17) are positional/byte operations,
not scalar parses. *Required:* state in §3 that numeric-edited items are byte-backed (so the existing
`MoveNumericEditedToNumeric` de-edit path exists), and add a note to §2.5's slot-pair table that
three MOVE cells always route through the byte codec (numeric-edited→numeric, alphanumeric/national
→numeric, figurative→numeric), never the typed×typed path.

### M6. Stage-0 "byte-identical by construction" bundles a behavior-neutral scaffolding claim with an invasive 2 669-line refactor.
"Classify everything byte-backed" reproduces today *only if* the `PicDescriptor → FieldShape +
NumProfile` split (also in Stage 0) is bit-exact. PicRuntime consumes ~15 descriptor fields
(signStorage, editPattern, blankWhenZero, leadingScaleDigits/trailingScaleDigits, isJustifiedRight,
environment, IsGroup, ...); §13 describes NumProfile as only "digits/scale/signed." *Owner decision
/ required:* either (a) defer the `PicDescriptor` split to Stage 6 and add NumProfile as an additive
parallel type in Stage 0, **or** (b) state explicitly that FieldShape is a *lossless* rename carrying
all PicRuntime-consumed fields and that the byte-identical guarantee is enforced by the existing
suite-green gate (the all-byte-backed start makes the 1047/480/364 suite itself the mechanized check).
The current wording asserts byte-identity without a Stage-0 differential oracle.

---

## 4. Low / partial / overstated findings (documentation refinements)

These were adjudicated `partial` and downgraded to low; each is a worthwhile prose-precision fix but
none affects the architecture. Grouped by theme.

**Island / overlay representation (all low):**
- *REDEFINES class straddling typed/byte boundary* — add a §3 rule: island classification is
  **downward-transitive**; all subordinate elementary items of every class member are ByteWindowSlot
  views, not standalone TypedFieldSlots, regardless of §4's table.
- *Group MOVE "as byte windows"* — already covered by trigger #4; tighten §2.4 wording to say both
  operands are byte islands *before* codegen (lowered byte-island→byte-island), not a per-op scratch
  materialization.
- *X-over-9 / X-over-COMP island codec encoding* — pin the island's canonical byte encoding
  explicitly (1 byte = 1 char position, Latin-1/program-charset; buffer length = byte-image size of
  the redefined base item per ISO GR8); refine "zero conversion" to "zero on the typed fast path;
  one codec call per island boundary crossing." Add an X-over-COMP conformance test.
- *Recursion / island address escape* — add one sentence: CALL BY REFERENCE is an escape path
  subsumed by the new trigger 11; update R4's "escaping islands" list to include BY REFERENCE.
  LOCAL-STORAGE recursion is a non-issue (items live in the heap `ProgramStorage`).

**CLR feasibility (all low):**
- *ManagedPtr Owner type* — narrow `object?` to `byte[]` (Owner is always a byte[]/island buffer);
  state ADDRESS OF a typed item is a trigger-6 event demoting its equivalence class to byte.
- *`decimal` dynamic scale* — add an invariant box to §5: the `decimal` field is a value carrier
  only; (digits, scale, signed) live in NumProfile; every COMPARE / SIZE-ERROR read goes through
  NumProfile-aware helpers, never raw `decimal ==`. Change the §4 annotation from "carries scale" to
  "value carrier; scale identity in NumProfile."
- *[InlineArray] "never a heap byte[]"* — qualify the §2.2 / Bottom-line absolute claim: it holds for
  non-escaping overlay islands; address-stable islands (BASED/ADDRESS OF/EXTERNAL/GLOBAL/CALL-aliased)
  are pooled byte[] by necessity (consistent with R4). Optionally name the two sub-kinds
  (inline-eligible vs address-stable) and tie Open Question #3 to escapedness, not size alone.
- *Explicit-layout union foot-gun* — add to §2.3: the `[StructLayout(Explicit)]` form is always a
  self-contained, all-blittable nested struct; `[FieldOffset]` is never placed on a `record struct`
  that also holds managed (`string`/`object`) fields (TypeLoadException at runtime, not compile time).

**String-as-default (all low):**
- *"Free MOVE" overstated* — §1.2.1 already qualifies it (equal-length); just add the equal-length
  qualifier to the §11 and Bottom-line summary bullets.
- *HIGH-VALUE/LOW-VALUE u16 mapping* — pin Latin-1 bijection (byte k ↔ U+00kk; HIGH-VALUE = U+00FF,
  LOW-VALUE = U+0000); `ConditionLowerer`/`DataMovementLowerer` already use U+00FF. Note that
  `MOVE HIGH-VALUES` to a plain PIC X field triggers no byte island, so the typed-string path must
  honor the convention.
- *IS ALPHABETIC over UTF-16* — **also a real one-line code fix:** `PicRuntime.IsAlphabeticClass`
  currently calls `char.IsLetter` (Unicode-wide), broader than ISO §8.8.4.4's closed {A–Z,a–z,space}
  set; replace with literal range checks. Add a 5th §1.2.1 guardrail: class conditions use literal
  ranges, never `char.IsLetter`/`char.IsDigit`; the locale-in-effect path (LC_CTYPE) is correct and
  must not be "fixed" away. (`IS NUMERIC` digit check is already literal and correct.)
- *UTF-16 case-mapping on supplementary planes* — `FUNCTION UPPER-CASE`/`LOWER-CASE` must use the
  per-`char` overload (`char.ToUpperInvariant(char)`), never the scalar-aware string overloads, which
  can change a surrogate pair as a unit. (REVERSE/TRIM over `char` are already code-unit-correct.)
- *X↔N "same string surface" / lossy narrow* — fix the §1.2 citation (use NOTE 2 line 5077, not the
  source-repertoire lines); note in §2.5 that X↔N typed MOVE is identity under UTF-16-both;
  `NarrowNationalToBytes`'s '?' substitution is the *old* byte path being demoted, not the typed path.
  But the typed `string` must still carry the X-vs-N **category** (in FieldShape) so comparison
  conversion, the correct collating sequence, national-space padding, and EC-DATA-CONVERSION are
  dispatchable — a bare `string` is insufficient.
- *Memory doubling + per-record transcode* — overstated (FD records stay byte-backed → no per-record
  transcode), but add two Risk rows: (R9) WS OCCURS tables of PIC X become per-element heap strings
  (~24 B object overhead); measure on the corpus; (R10) the CODE-SET boundary codec must be Latin-1,
  not ASCII, to round-trip 0x00–0xFF losslessly.
- *Peephole / write-heavy idioms* — add a write-pattern trigger to §3 (refmod-write or repeated
  STRING POINTER-advance in a PERFORM loop → byte-backed for the loop scope) to avoid O(N²)
  allocation on the typed path; reframe §2.6 as a write-pattern classifier decision, not solely an
  INSPECT peephole. (STRING/UNSTRING allocate once per statement, not per char — that part was
  overstated.)

**Numeric oracle (all low):**
- *decode-island→decimal→encode not byte-identical* — the byte×byte path (the dominant island path)
  does **not** round-trip through decimal (overstated); but add a codec policy note: `IslandCodec`
  must accept all valid positive sign nibbles (0x0A/0x0B/0x0C/0x0E/0x0F) on decode and document that
  Typed→Byte encode normalizes to 0x0C; extend the R2 grid with multi-sign-nibble vectors.
- *oracle value grid not exhaustive* — add USAGE and SignStorageKind as explicit grid axes; require
  an independent reference (not PicRuntime) for the PROHIBITED-rounding and P-scaling branches. Fix
  the existing `RoundProhibited` non-conformance (silently truncates; must set EC-SIZE-TRUNCATION,
  PicRuntime.cs:2286/2329) before claiming the rounding axis validated.
- *"lifted verbatim" overstates what exists* — Rounding.cs/CobolNum.cs do not exist; reword §5/§13
  from "lifted verbatim" to "extracted and corrected," and state the oracle validates
  regression-equivalence to legacy *and* spec-correctness (two distinct bars).
- *V/P scaling decimal overflow* — the real issue is algebraic-value magnitude, not silent ulp error
  (overstated); generalize R5: route to byte/BigInteger when `n_stored + trailingP ≥ 29` (not only
  when stored digits ≥ 19). The Stage-1 oracle catches the rest as loud `OverflowException`s.

**Migration / classifier (all low):**
- *"every slot can materialize a byte window" is false for ManagedPtr / OBJECT REFERENCE* — scope the
  §1.6/§2.5 guarantee: universal for {numeric, alphanumeric, national, boolean, edited, group}; not
  for {pointer, object-reference}, which are typed-only (CALL BY REFERENCE and EXTERNAL are
  spec-prohibited for pointer/object items — ISO §14.9.4.3 SR10, §13.18.11 GR4 — and overlay is
  caught by trigger 6).
- *data-dependent refmod bounds* — folded into H2/3c above.
- *debug round-trip assertion blind to cross-program paths* — fold into H1; downgrade the "never
  silent corruption" claim to "intra-program typed↔byte misclassification becomes a test failure";
  the cross-program defense is the conservative trigger, not the assertion.
- *ODO group sender/receiver length asymmetry* — add an explicit trigger (group transitively
  containing ODO, used as a whole-group operand → byte) and replace the §4 ODO note with the full ISO
  §13.18.39.3 rule (sender = current count, receiver = MAX, space-fill the excess); note in §2.5 that
  `ByteWindowSlot.length` may be a runtime expression ("static slot *kind* dispatch," not static
  length).
- *"% records typed" is a vanity metric* — add a note to §10 that typed-% is a coarse coverage
  indicator only; the real gates are the Stage-5 Cecil-vs-C# oracle diff (correctness) and runtime
  profiling on a representative workload (performance). Resolve Open Question #4 to defuse the
  "flip easy leaves" incentive.

---

## 5. Completeness gaps to investigate (not yet analyzed by any finding)

The 48 findings were all about operations on *already-initialized* data. Six areas were not examined
and should be investigated before Stage 3:

1. **VALUE clause / INITIALIZE / figurative-constant initialization into the typed model.**
   Initialization is the first thing that runs for every item. `default(record struct)` (string =
   `null`, numerics = 0) is **not** the COBOL category-default fill the byte model produces, and
   `VALUE HIGH-VALUE` / `VALUE ALL '*'` / numeric-edited or signed-scaled VALUE / group-level VALUE
   have no `default(T)` expression. **Stage-0 "byte-identical" is violated the moment a typed field
   is born, before any operation runs.** This is the highest-priority gap — investigate it alongside
   the M6 Stage-0 concern.

2. **USE FOR DEBUGGING / DEBUG-ITEM / WITH DEBUGGING MODE.** The debugging module populates
   DEBUG-CONTENTS with the *character image* of any monitored item on every reference — a
   byte-observability path with no §3 trigger. If any item can be made a debugging target
   (procedure-division-driven, like the refmod pre-scan), it demotes arbitrary fields to byte.

3. **`record struct` value-copy aliasing beyond CALL** — the general copy-on-assignment problem.
   COBOL data items are *reference-identity* storage cells; `record struct` is copy-on-assignment.
   Table-element mutation through a value-returning `CobolTable<T>` indexer
   (`MOVE x TO TABLE(i)-FIELD` writes a temporary, not the table), a group passed to a mutating
   PERFORM/paragraph, and interior-field ADDRESS OF all silently operate on copies. The design needs
   `ref`/`ref readonly`/by-ref-indexer discipline at every group access site. H1 sampled only the
   CALL instance; this is the broader class.

4. **Concurrency / memory model for EXTERNAL and run-unit-shared storage.** Trigger 8's "one
   canonical representation" is asserted with no memory model: a typed `record struct` canonical rep
   defeats sharing via value-copy; a byte-island rep needs synchronized re-decode across programs;
   `ManagedPtr` into shared/ALLOCATE'd regions has undefined cross-thread visibility/lifetime. No
   finding addressed threading or the .NET memory model.

5. **Embedded precompiler ABIs (EXEC SQL / EXEC CICS host variables, copybook communication areas).**
   The commercial trajectory (COBOL.NET) is dominated by host variables bound by *byte address +
   PIC layout* (SQLDA, DFHCOMMAREA, BMS maps). These force large swaths of real programs back to byte
   islands via a trigger the ADR does not list, undercutting the "common case is typed" premise for
   exactly the commercial corpus the rename targets. Zero support today; out of current scope, but
   the foundational bet should acknowledge it.

6. **Soundness of the Stage-5 Cecil-vs-Roslyn differential oracle itself.** The whole migration rests
   on this oracle, but the project's own history shows oracle-defeating non-determinism (NC214M
   dropped as non-deterministic; flaky parallel-load timeouts). A differential oracle is valid only
   for deterministic, environment-independent output — ACCEPT FROM DATE/TIME, RANDOM,
   system-dependent file status, uninitialized-storage-dependent output, EXTERNAL/threaded ordering
   all produce legitimate diffs, forcing an exclusion list that blinds the oracle exactly where
   typed-vs-byte default-value differences (e.g. uninitialized typed 0 vs byte garbage) surface. And
   an oracle over existing baselines cannot catch divergence on overlay edges no baseline exercises —
   so "any diff is a hard CI failure" overstates the guarantee.

---

## 6. Concrete punch-list of edits to DATA_MODEL_ARCHITECTURE.md

Apply these edits (ordered by priority). Section references are to the current document.

**§3 — classifier trigger list (the load-bearing edits):**
1. Add **trigger 11**: any CALL ... USING ... BY REFERENCE argument (static or dynamic), transitively,
   is byte-backed. *(H1)*
2. Add **trigger 12**: all LINKAGE SECTION items are byte-backed. *(H1)*
3. Rewrite **trigger 3** as the decidable over-approximation 3a/3b/3c (refmod of anything other than a
   proven-homogeneous string field → byte; refmod of any non-alphanumeric DISPLAY/NATIONAL/edited item
   → byte; variable-bound refmod over a heterogeneous group → byte). *(H2)*
4. Extend **trigger 4** to fire on any group MOVE/COMPARE with a non-DISPLAY member, on class
   conditions over a group, and tighten "dissimilar-layout" to be SYNC-byte-offset-aware. *(H3, M4)*
5. Add a **downward-transitivity rule**: all subordinate items of an island class are ByteWindowSlot
   views, not TypedFieldSlots. *(low — REDEFINES straddle)*
6. Add a **numeric-edited byte trigger** and a **write-pattern trigger** (refmod-write / repeated
   STRING POINTER-advance in a loop). *(M5, low)*
7. Add an **ODO-whole-group-operand trigger**. *(low — ODO)*
8. In the soundness paragraph, downgrade "never silent corruption" to intra-program scope and state
   the cross-program defense is the conservative trigger. *(H1, low)*

**§1.2.1 — string table and guardrails:**
9. Remove the unqualified "alphanumeric comparison → CompareOrdinal" row; document the
   collating-sequence option chosen. *(H4)*
10. Add a 5th guardrail: class conditions use literal ranges (never `char.IsLetter`/`char.IsDigit`);
    case-mapping uses the per-`char` overload. *(low)*
11. State that string ops dispatch on the FieldShape **category** (X vs N), not a bare `string`. *(low)*

**§2.4 / §2.5 — overlay and chokepoint wording:**
12. Clarify "identical-layout group MOVE = struct copy" is byte-equivalent only for all-DISPLAY/NATIONAL
    groups; the dissimilar/COMP-bearing case is byte-island→byte-island. *(H3)*
13. Pin the island canonical byte encoding (1 byte = 1 char position, Latin-1; buffer = redefined-base
    byte size); refine "zero conversion" to "one codec call per island boundary." *(low)*
14. Add the three byte-routed MOVE cells (de-edit, alphanumeric→numeric, figurative→numeric). *(M5)*
15. Note `ByteWindowSlot.length` may be a runtime expression. *(low — ODO)*
16. Scope "every slot can materialize a byte window" to exclude pointer/object-reference. *(low)*

**§4 — type-mapping table:**
17. Annotate `decimal` rows: "value carrier; scale identity in NumProfile." *(low)*
18. Add a NumProfile `UsageKind`/`TruncationPolicy` note distinguishing COMP (digit-count) from COMP-5
    (binary-capacity). *(M3)*
19. Replace the ODO note with the full sender/receiver length-asymmetry rule. *(low)*

**§5 / §13 — runtime shape:**
20. Reword "(exist today; lifted verbatim)" → "(logic exists in PicRuntime.cs; extracted and
    corrected)"; require CobolNum's 19–31-digit intermediate to be BigInteger; state all
    expression-tree operators are no-throw size-error helpers (not just the final store). *(M1, M2, low)*
21. Add a `decimal`-carrier invariant box. *(low)*

**§10 / §12 — migration and risks:**
22. State Stage-0 byte-identity is enforced by the suite-green gate (or defer the PicDescriptor split
    to Stage 6); name the FieldShape lossless-rename requirement. *(M6)*
23. Correct the R1 row (per-compilation-unit fixpoint; CALL BY REFERENCE unconditional byte trigger).
    *(H1)*
24. Expand R5 (DISPLAY 9(19+); algebraic-magnitude routing `n_stored + trailingP ≥ 29`). *(M1, low)*
25. Expand the R2 grid (USAGE, SignStorageKind, multi-sign-nibble, PROHIBITED-rounding vs independent
    reference). *(M3, low)*
26. Add R9 (WS OCCURS-of-PIC-X heap-string overhead) and R10 (CODE-SET codec must be Latin-1). *(low)*
27. Add the equal-length qualifier to the §11 and Bottom-line "free MOVE" bullets. *(low)*
28. Add a §10 note that typed-% is a coverage indicator, not a perf/safety gate; resolve Open
    Question #4. *(low)*

**New §3.1 (or §3 appendix) — initialization model, currently absent:**
29. Add a model for VALUE clause / INITIALIZE / figurative-constant initialization of typed fields
    (the Stage-0 byte-identity hole). *(completeness gap #1)*

**Conformance tests to add to the guard in the same commits:**
30. X-over-COMP REDEFINES; cross-program CALL ... USING (IC-series); MOVE HIGH-VALUES to a trigger-free
    PIC X(4) (byte-exact 0xFF); refmod of a numeric DISPLAY date field; group MOVE between SYNC groups
    of differing slack; COMP-5 store-out-of-PIC-range.

**Companion code fix (independent of the migration, valid today):**
31. `PicRuntime.IsAlphabeticClass`: replace `char.IsLetter(c)` with literal `A–Z/a–z` range checks.
32. `PicRuntime` RoundProhibited (line 2286/2329): set EC-SIZE-TRUNCATION instead of silent truncate.
