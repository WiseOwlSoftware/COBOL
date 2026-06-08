# COBOL.NET — REDEFINES / RENAMES (storage overlay) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §4; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

COBOL.NET stores a datum as its VALUE (PIC 9(4)→a long holding unscaled 1234; PIC X(4)→a 4-char string). REDEFINES (ISO 13.18.44) and RENAMES/level-66 (13.18.45) are byte-image REINTERPRETATION: a second, differently-typed VIEW over the same storage. Value model and byte image coincide only for trivial same-USAGE/same-layout puns; every hard case is hard because they diverge (a PIC 9(4) COMP punned as PIC XX is the two binary bytes, unrelated to the long's value; PIC X(20) punned as -9(9).9(9) reinterprets 20 char positions under an edit template). With NO shared byte[], two typed reps cannot both be live, stored, and coherent. THE ONE CORRECT COHERENCE ANSWER: they do not both exist. A "redefines class" (entries sharing a storage area) has exactly ONE stored backing — the canonical — and EVERY other view is a computed accessor (a C# property) over it. Never two stored fields per storage area (the incoherence trap; violates feedback_singular_pattern). RECOMMENDED HYBRID = 4 tiers, one per class, priority cascade D>C>B>A: A-Alias (identical PIC+USAGE, or RENAMES-without-THRU): one typed field, other names are pass-through properties. B-StringCanonical (whole class is USAGE DISPLAY — alphanumeric/DISPLAY-numeric/edited/alphabetic): canonical = ONE string of class-max width (a DISPLAY item's byte image IS its characters); each view = typed accessor (substring / parse-digits→long / format) over it; NO bytes; this is the dominant real case and covers the ENTIRE near-term NIST path (corpus check: immediate REDEFINES classes are DISPLAY-homogeneous). C-ByteCanonical (mixed-USAGE puns observing COMP/COMP-1/2/3/5/INDEX cross-view): canonical = ONE class-scoped byte[] of class-max width, SYNC-aware offsets; each leaf = typed get/set accessor over (offset,length,usage) via a small RedefCodec runtime helper (mine legacy PicRuntime/PicDescriptor); byte image confined to the class, never the record, never persisted beyond it. D-Reject loud (spec-forbidden/unmodelable: object/pointer/message-tag/strongly-typed rules 12/14; OCCURS DEPENDING ON / variable-length / dynamic-length rules 5/17): a diagnostic, which is conformant since these are already illegal. RENAMES folds into the same tiers as a COMPOSED view over EXISTING fields (it adds no storage; GR1 no-THRU = attribute inheritance = Tier A; GR2 THRU = alphanumeric group view = Tier B composition over the spanned leaves' display images). Tier classification reuses the legacy RecordClassificationPass closure shape (byte propagates across the REDEFINES class + to all subordinates, monotone, terminating), re-verdicted to the lattice A⊑B⊑C⊑D (join=max tier). Prerequisite: the data model does NOT capture this yet — DataBinder DROPS the REDEFINES clause and SKIPS level 66 — so add RedefinesTarget/Renames/RedefinesClass and bind them. Owner fork: confirm Tier C's persistent class-scoped byte[] as the accepted "bytes only at a boundary" realization (recommended, off the critical path since A/B ship NIST-green regardless) vs collapsing C into D for zero-byte purity.

## Decisions

### D1. One canonical stored backing per redefines class + every other view is a computed C# accessor (property) over it — the spine.

**Rationale.** The only way two typed reps over one storage stay coherent WITHOUT a shared byte[] is for one to be stored and the others derived on access. This is the direct fix for the bug that triggered the greenfield pivot (DEVLOG 457: typed writes invisible through another view).

**Rejected alternatives.** Materialize-on-demand for BOTH views (no single owner of truth → ambiguous which write wins). Keep a byte[] per RECORD (the legacy byte engine — the rejected substrate).

### D2. Never emit two stored fields for one redefines class.

**Rationale.** feedback_singular_pattern (one canonical pattern per result) + correctness: with no shared storage, a write through one stored field would silently not appear through the other.

**Rejected alternatives.** "Store both, sync on every write": O(views) write fan-out, and the sync direction for a partial cross-field-overlap view is undefined.

### D3. Reject [FieldOffset]/StructLayout(Explicit) overlay outright (no exceptions).

**Rationale.** Cannot overlay a reference type (string) on a long; the dominant real pun is alphanumeric↔numeric which it cannot express; even long-over-long overlays VALUES not the byte REINTERPRETATION COBOL means; forces unsafe/blittable constraints; buys nothing Tier A doesn't already cover idiomatically.

**Rejected alternatives.** Use FieldOffset for the rare all-blittable-same-width class — not worth a second mechanism for a case Tier A handles.

### D4. Tier B canonical is a string (not char[]/byte[]).

**Rationale.** A DISPLAY item's external image IS its characters; the runtime (CobolString) is string-based; views compose with Substring/format.

**Rejected alternatives.** char[] (no value semantics, fights CobolString). byte[] (forces encode/decode on every DISPLAY access — Tier C's cost — for no benefit when content is already characters).

### D5. Tier C confines the byte image to the CLASS, never the record; the byte[] is the PERSISTENT canonical (not materialize-on-demand).

**Rationale.** Honors "bytes only at a boundary" by making the boundary the rare mixed-USAGE class, codec-mediated. It is the ONLY storage for such a class so there is nothing to materialize FROM — distinct from the transient materialize-on-demand used by whole-group-alphanumeric (a different subsystem).

**Rejected alternatives.** Demote the whole 01 record to bytes (the legacy island — far wider blast radius).

### D6. RENAMES folds into the same A/B/C tiers as a COMPOSED view over existing fields.

**Rationale.** RENAMES adds no storage (GR1/GR2); the spanned items already have canonical backings; the rename is an accessor that composes them via each leaf's existing read/write so heterogeneous (numeric-in-span) cases work generally.

**Rejected alternatives.** Give RENAMES its own backing — would duplicate storage and need syncing (the two-stored-fields trap again).

### D7. Tier selection reuses the legacy RecordClassificationPass transitive-closure shape, re-verdicted to the lattice A⊑B⊑C⊑D (join = max tier).

**Rationale.** That fixpoint (byte propagates across the REDEFINES class + to all subordinates; monotone; terminating) IS the class-membership + tier-propagation algorithm; do not re-derive (feedback_singular_pattern, feedback_production_quality_always).

**Rejected alternatives.** Write a fresh ad-hoc classifier (risks re-introducing the subtle propagation bugs the legacy pass already solved + corpus-tested).

### D8. SYNC-aware Tier C offsets come from a ported StorageLayoutComputer, not re-derived.

**Rationale.** SYNCHRONIZED inserts alignment slack that shifts offsets in the shared image; the proven computer already does this correctly.

**Rejected alternatives.** Recompute alignment by hand (error-prone; SYNC offset math is exactly what the legacy already proved).

### D9. A view suppresses only its stored VALUE field; a numeric view STILL emits its NumProfile and carries its PICTURE's natural C# surface type.

**Rationale.** EmitArithAssign emits `<CsName> = CobolNum.Store(value, scale, _P_<CsName>)`; if a numeric view is an arithmetic target and ALL its emit were suppressed, _P_<view> would not exist and generated C# would not compile. Keeping the NumProfile + natural type lets the existing FieldNum/ReadAsString/Store/Compare paths operate on a view unchanged.

**Rejected alternatives.** "Views emit nothing" (too absolute — breaks the emitter contract on the first numeric view targeted by arithmetic).

## C# mapping

TIER A (alias): 01 WS-COUNTER PIC 9(4). / 01 WS-COUNT-ALIAS REDEFINES WS-COUNTER PIC 9(4). →
  private static long WS_COUNTER = 0L; private static readonly NumProfile _P_WS_COUNTER = ...;
  private static long WS_COUNT_ALIAS { get => WS_COUNTER; set => WS_COUNTER = value; }   // pass-through property, ONE stored field.
RENAMES no-THRU (GR1) → a property forwarding to the renamed item's field.

TIER B (string canonical — IC101A CCVS pattern): 03 COMPUTED-A PIC X(20) VALUE SPACE. / 03 COMPUTED-N REDEFINES COMPUTED-A PIC -9(9).9(9). / 03 CM-18V0 REDEFINES COMPUTED-A. / 04 COMPUTED-18V0 PIC -9(18). →
  // ONE stored backing, class width = max over views = 20; ONLY the original's VALUE inits it (rule 9).
  private static string _redef_COMPUTED_A = new string(' ', 20);
  // The ORIGINAL is itself a VIEW over the backing (so exactly one stored member):
  private static string COMPUTED_A { get => _redef_COMPUTED_A; set => _redef_COMPUTED_A = CobolString.Store(value, 20); }
  // Alphanumeric/edited view (off,len): get=backing.Substring(off,len); set=StorePunch(backing, off, len, CobolString.Store(value,len)).
  // DISPLAY-numeric view (off,len,scale,signed): get-as-number = CobolNum.ParseDisplay(backing.Substring(off,len), _P_view) → scaled long;
  //   set-from-number = StorePunch(backing, off, len, CobolNum.FormatDisplaySigned(value, scale, _P_view)).
  // IMPORTANT (advisor #2): a view SUPPRESSES only its stored VALUE field; a NUMERIC view STILL EMITS its NumProfile (_P_<view>) —
  //   the accessor needs it, and EmitArithAssign already references _P_<CsName>. Each view's C# surface type = its PICTURE's natural type
  //   (numeric→long/decimal, alphanumeric/edited→string) so the existing FieldNum/ReadAsString/Store/Compare paths work unchanged.
  private static readonly NumProfile _P_COMPUTED_18V0 = new NumProfile { Digits=18, FractionDigits=0, Signed=true, ... };

RENAMES THRU (GR2 — NC252A: 66 RENAME1 RENAMES NAME1 THRU NAME3): composed accessor over EXISTING fields, NOT a new backing.
  Express composition via each leaf's existing read/write (advisor #2 — so HETEROGENEOUS spans work):
  private static string RENAME1 {
    get => ReadImage(NAME1A) + ReadImage(NAME1B) + ReadImage(NAME2) + ReadImage(NAME3A) + ReadImage(NAME3B); // each leaf's DISPLAY image
    set { /* distribute value left-to-right back into the spanned fields by width via each leaf's MOVE-into path */ } }
  (all-PIC X span → plain string concat; a numeric leaf in the span → CobolNum.FormatDisplay on get, the numeric MOVE path on set.)

TIER C (scoped byte[] — mixed-USAGE pun): 01 PUN. / 05 AS-TEXT PIC X(4). / 05 AS-NUM REDEFINES AS-TEXT PIC 9(8) COMP. →
  private static byte[] _redef_AS_TEXT = new byte[4];   // ONE stored backing, class width 4 (persistent, NOT materialize-on-demand).
  private static string AS_TEXT { get => RedefCodec.GetText(_redef_AS_TEXT,0,4); set => RedefCodec.PutText(_redef_AS_TEXT,0,4,value); }
  private static long AS_NUM { get => RedefCodec.GetBinary(_redef_AS_TEXT,0,4,signed:false); set => RedefCodec.PutBinary(_redef_AS_TEXT,0,4,value,signed:false); }
  RedefCodec (the ONLY byte surface this subsystem adds): GetText/PutText (Latin-1 lossless), GetBinary/PutBinary (COMP/COMP-5 width+endian+wrap),
  GetPacked/PutPacked (COMP-3 nibbles+sign), GetDisplay/PutDisplay (DISPLAY digits incl. overpunch) — mine legacy PicRuntime/PicDescriptor/PackedDecimal.

MODEL CHANGES (prerequisite): DataItem gets DataItem? RedefinesTarget; RenamesInfo? Renames; RedefinesClass? Class; bool IsCanonical.
  new record RenamesInfo(string FromName, string? ThruName){ DataItem? From; DataItem? Thru; }
  enum RedefinesTier { Alias, StringCanonical, ByteCanonical, Rejected }
  class RedefinesClass { DataItem Canonical; List<DataItem> Views; RedefinesTier Tier; int Width; string BackingCsName => "_redef_"+Canonical.CsName; }
  DataBinder: parse clause.redefinesClause()?.dataReference() → RedefinesTarget (resolve after forest built); STOP skipping level 66 (it is a
  dataDescriptionEntry alternative, line 200 of CobolData.g4, whose body has renamesClause()) — bind it carrying Renames, attached to the owning
  record as a sibling (NOT a storage-tree child); a post-pass groups entries into RedefinesClasses and runs the §4 classification.

## Hard problems

### Two differently-typed C# reps over one storage must stay coherent with NO shared byte[].

They do not both exist: ONE canonical stored backing per redefines class; every other view is a computed accessor (C# property) over it. Tier picks the canonical (typed field / string / class-scoped byte[]).

### Init rule (REDEFINES SR9): no view (redefiner) may carry VALUE; only the original's VALUE inits the canonical.

Emitter checks item.IsCanonical: a view emits NO stored value field and NO initializer (only the canonical original initializes the backing). Prevents the naive per-field init from clobbering the original.

### A level-01 non-EXTERNAL original may be redefined by something LARGER (SR8 exception) — sizing from the original loses bytes.

Class width = MAX storage width across all views (RedefinesClass.Width); the canonical backing (string/byte[]) is sized to the max, not the original.

### Partial cross-field overlap — a view can span field-1 + half of field-2 (offsets need not align to leaf boundaries).

Model every leaf as a window (offset,length[,usage]) over the concatenated image — the Tier B/C accessor model. A naive field↔field map cannot express it; offset/length accessors can.

### Mixed-USAGE pun (PIC X over COMP) — no character string can represent a binary field's bytes.

Tier C: ONE class-scoped byte[] canonical; each leaf is a typed accessor over (offset,length,usage) via RedefCodec (Latin-1 text, binary endian/width, packed nibbles, DISPLAY+overpunch) — mined from legacy PicRuntime/PackedDecimal. Confined to the class, never persisted further.

### Numeric view as an arithmetic target needs its NumProfile, but a view's stored field is suppressed.

Suppress only the stored VALUE field; STILL emit the view's NumProfile (_P_<view>) and give the view its PICTURE's natural surface type so EmitArithAssign / FieldNum / Store work unchanged on a view (advisor correction).

### RENAMES THRU over a heterogeneous span (a numeric leaf inside an alphanumeric group view).

Compose via each leaf's EXISTING read/write: get = concat each leaf's DISPLAY image (CobolNum.FormatDisplay for a numeric leaf, the field for an alphanumeric one); set = distribute the value left-to-right back into each leaf by width via its MOVE-into path. Not raw `+` on a long.

### Signed-DISPLAY overpunch: the byte image of S9(n) carries the sign as an overpunch on a digit, so a numeric view's encode/decode is not plain ASCII digits.

Dependency: implement overpunch in CobolNum.FormatDisplaySigned / ParseDisplay (encode+decode incl. overpunch + leading/trailing separate sign) before Tier B/C numeric-view accessors are exact. CobolNum.FormatDisplay currently explicitly defers it (returns magnitude).

### Lossless byte↔char carrier for alphanumeric content that holds binary (0x00–0xFF).

Latin-1 / ISO-8859-1 (Encoding.Latin1): byte b ↔ (char)b, lossless round-trip. CROSS-CUTTING: identical convention needed by whole-group-alphanumeric and file I/O — settle ONCE as a CobolNet.Runtime codepage constant (feedback_singular_pattern), not a REDEFINES-local convention.

### SYNCHRONIZED inserts alignment slack that shifts offsets in the shared image.

SYNC inside a class can break char-boundary alignment of views → classifier raises the class to Tier C (any doubt → bytes), where a ported StorageLayoutComputer supplies SYNC-aware offsets; SR15 alignment-mismatch → COBOLNET_REDEF_ALIGN diagnostic, not silent.

### REDEFINES inside an OCCURS table element (SR5 forbids OCCURS on the subject/target but allows a redefine inside an element).

The overlay is PER ELEMENT: the array element type (the record struct for the OCCURS group) carries the canonical+accessors, indexed by subscript. ODO anywhere in the class → Tier D.

## Edge cases

- Init only from the original (SR9): suppress initializers + stored value field on every non-canonical class member.
- Larger level-01 redefiner (SR8 exception): class width = max across views, not the original's width.
- Partial cross-field overlap: leaves are (offset,length) windows over the concatenated image.
- REDEFINES inside OCCURS (SR5): overlay is per array element; element type carries canonical+accessors.
- Signed-DISPLAY overpunch (SR-numeric): numeric view encode/decode is not plain ASCII digits; CobolNum currently defers overpunch.
- Alphanumeric view over binary content: needs a lossless 8-bit (Latin-1) carrier — shared with whole-group-alphanumeric + file I/O.
- Multiple redefinitions of one area (SR7): all redefiners name the original → one class, one canonical, anchored via RedefinesTarget closure.
- Group redefiner over an elementary target and vice-versa: width/offsets over leaves; tier decided by the union of all leaves' usages.
- Nested REDEFINES (SR11: target may itself be a redefiner): follow RedefinesTarget transitively to the true non-redefining anchor; one canonical.
- RENAMES single-group (NC252A RENAME3 RENAMES NAME2) → Tier A forwarding to the group's composite read.
- RENAMES sub-span (RENAME2 RENAMES NAME1A THRU NAME1B) → Tier B composition over just the spanned leaves; data-name-3 not subordinate to / not before data-name-2 (SR11) — binder validates a forward sibling range.
- RENAMES must immediately follow the record's last entry (SR2), qualified only by 01/FD/SD (SR3): attach 66 entries to the owning record, not into the storage tree.
- Group MOVE/compare of a redefines original: read = the canonical (field/string/materialized byte image), write normalizes through the canonical's set; a group comparison/CORR lowers field-wise and never RAISES the tier (legacy ADR §3.4).
- EXTERNAL/GLOBAL with REDEFINES (GLOBAL GR3: only the subject is global): cross-program identity deferred to the LINKAGE/EXTERNAL subsystem; the redefines canonical = the externalized member.
- Numeric view wider than 18 digits: parses to Int128 (the numeric subsystem's escape hatch), not long.
- Reference modification of a view: composes with the accessor's own offset (Tier B = substring of substring; Tier C numeric = ref-mod over the materialized image, shared with whole-group-alphanumeric).
- Tier D loud-reject classes: object/pointer/message-tag/strongly-typed (SR12/SR14); OCCURS DEPENDING ON / variable-length / dynamic-length (SR5/SR17) — these are already illegal, so a diagnostic is conformant.

## ISO citations

- ISO/IEC 1989:2023 §13.18.44 REDEFINES — SR4 (no lower level-number between target and subject)
- §13.18.44 SR5 (no OCCURS on subject/target; no occurs-depending table either side)
- §13.18.44 SR7 (multiple redefinitions of one area each name the original)
- §13.18.44 SR8 (subject size ≤ target, except a level-01 non-EXTERNAL target may be redefined larger)
- §13.18.44 SR9 (no VALUE on the subject or any subordinate, except level-88)
- §13.18.44 SR10 (the new descriptions follow the target's area without intervening new areas)
- §13.18.44 SR11 (data-name-2 may itself be subordinate to / be a REDEFINES entry)
- §13.18.44 SR12 (REDEFINES not for class object/message-tag/pointer or a strongly-typed group)
- §13.18.44 SR14 (data-name-2 not object/message-tag/pointer/strongly-typed or a member thereof)
- §13.18.44 SR15 (required alignment of the subject must match the target's)
- §13.18.44 SR17 (neither side a variable-length group or dynamic-length elementary item)
- §13.18.44 GR1 (storage starts at the target's first bit; the larger of the two sizes wins; the target's own reference size is unchanged)
- §13.18.44 GR2 (any of the names may reference the shared area)
- §13.18.44 GR3 (under VALIDATE each redefinition is checked independently; PRESENT WHEN selects)
- §13.18.45 RENAMES — SR2 (entries immediately follow the record's last data description entry)
- §13.18.45 SR3 (data-name-1 qualification rules; none subject to OCCURS)
- §13.18.45 SR4 (data-name-2/3 are elementary or groups in the same record, not the same name)
- §13.18.45 SR5 (data-name-2/3 not level 1/66/77/88)
- §13.18.45 SR7 (data-name-2/3 not subscripted)
- §13.18.45 SR8 (range excludes object/message-tag/pointer/strongly-typed/variable-length/occurs-depending items)
- §13.18.45 SR10 (the renamed area defines an integral number of bytes)
- §13.18.45 SR11 (data-name-3 begins at/after data-name-2 and ends after it; data-name-3 not subordinate to data-name-2)
- §13.18.45 GR1 (no-THRU: all attributes + storage of data-name-2 become data-name-1's)
- §13.18.45 GR2 (THRU: data-name-1 is an alphanumeric group view spanning data-name-2's first elementary through data-name-3's last)
- §13.18.27 GLOBAL GR3 (with REDEFINES, only the subject possesses the global attribute)
- §14.9.25 MOVE rules (GR4 byte preservation for same-representation group moves)
- §13.18.55 SYNCHRONIZED (alignment slack shifts offsets in the shared image)
- §8.5.3 Types (strongly-typed item rules referenced by REDEFINES SR12/14)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- OWNER FORK: confirm Tier C's PERSISTENT class-scoped byte[] canonical as the accepted realization of "bytes only at a boundary" (C-allow, RECOMMENDED — Tiers A/B stay 100% typed incl. the entire near-term NIST path which is DISPLAY-homogeneous, so the fork is off the critical path and A/B ship NIST-green regardless) — OR mandate rejecting mixed-USAGE REDEFINES loudly (collapse Tier C into Tier D), trading real-program coverage (X-over-COMP record/comms layouts are common in production COBOL) for zero-byte purity.
- Latin-1 lossless 8-bit carrier (Encoding.Latin1) is a CROSS-SUBSYSTEM convention shared with whole-group-alphanumeric and file record serialization — settle it ONCE as a CobolNet.Runtime codepage constant; owner-visible because it spans subsystems.
- Signed-DISPLAY overpunch: CobolNum currently DEFERS it (FormatDisplay returns magnitude). Tier B/C numeric-view accessors need CobolNum.FormatDisplaySigned/ParseDisplay (overpunch + leading/trailing separate sign). A sequencing dependency, not a design fork.
- EXTERNAL/GLOBAL redefines identity: defer cross-program canonical-member selection to the LINKAGE/EXTERNAL subsystem.
- Int128 views (>18 digits): align numeric-view parsing with the numeric subsystem's Int128 escape hatch.
- Tier C distinction from materialize-on-demand: Tier C byte[] is PERSISTENT (the only storage for a mixed-usage class); transient materialize-on-demand belongs to whole-group-alphanumeric — confirm the two are kept as separate mechanisms.
