# COBOL.NET — String Operations (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §7; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Design for COBOL.NET string manipulation on .NET `string`: INSPECT (TALLYING/REPLACING/CONVERTING with ALL/LEADING/FIRST/CHARACTERS, BEFORE/AFTER INITIAL, BACKWARD), STRING (DELIMITED BY / WITH POINTER / ON OVERFLOW), UNSTRING (DELIMITED BY [ALL] / DELIMITER IN / COUNT IN / WITH POINTER / TALLYING / ON OVERFLOW), reference modification as a typed substring view (read + write), alphanumeric MOVE (space-fill/truncate/JUSTIFIED) and comparison, and national (PIC N / UTF-16).

CENTRAL REPRESENTATION DECISION: alphanumeric/national elementary items are `string` at rest; every mutating op takes the value in and returns the new value, and the emitter assigns once: e.g. `FIELD = CobolStrings.InspectReplacing(FIELD, ops...);`, `RESULT = CobolStrings.StringInto(RESULT, ref ptr, sendings...);`. This is the natural port of the legacy `InspectRuntime`/`StorageArea` string code, which already does ALL its real work on `string`/`char[]` internally and only touches `byte[]` at the read/write edges — the port is "delete the byte edges, take value in / return value out." A new static runtime class `CobolNet.Runtime.CobolStrings` (sibling to `CobolString`/`CobolNum`) holds the ported algorithms; the existing `CobolString.Store`/`Compare` stay for whole-field MOVE/compare.

The legacy algorithms are PROVEN against 364 NIST tests (the comparison-cycle, region computation, ALL-skip, overflow conditions, GR4d signed-numeric de-signing). I am MINING them verbatim, changing only the I/O type from `(byte[],offset,len)` to `string`/`char[]`. Counters/pointers/COUNT route through the numeric subsystem (`CobolNum.Store` + `NumProfile`), not a parallel int path. Overlap (spec-undefined) becomes deterministic-and-safe via read-once / single-write-back.

THREE THINGS A NAIVE "it's all strings" DESIGN GETS WRONG, all handled here: (1) non-alphanumeric/group operands (INSPECT GR4b/4c, ref-mod GR2/3, STRING/UNSTRING) must be "treated as if redefined alphanumeric/national of the same size" — free for an elementary `string`, but a `record struct` group or `long` numeric must be materialized to its alphanumeric image, operated on, then DECOMPOSED back into typed sub-fields (the whole-group-alphanumeric boundary deferred to G6); (2) the lvalue gap — string-op targets are subscripted/qualified/ref-mod-sliced; they resolve through the ONE universal `Place` lvalue built by `ReferenceResolver` (`MemberPlace`/`RefModPlace`, SSOT §14.1 — implemented, G2 ✅), never a per-verb string-lvalue (see D7); (3) STRING only changes written positions (GR7) so the working buffer must start from the dest's current value.

## Decisions

### D1. string at rest; mutating ops are value-in/value-out (helper returns the new value, emitter assigns once).

**Rationale.** Matches the architecture's string-at-rest model (COBOLNET_ARCHITECTURE.md §3). The legacy InspectRuntime/StorageArea already compute on string/char[] internally and only bridge byte[] at the edges, so the port is mechanical: drop the byte edges, take string in / return string out. Value semantics also make spec-undefined overlap deterministic and benign.

**Rejected alternatives.** (a) Span<char>/mutable view over the field for in-place writes — IMPOSSIBLE: System.String is immutable, so a writable view requires fields to be char[] at rest, contradicting string-at-rest and forcing a second representation (violates the singular-pattern rule). (b) Keep a byte[] scratch and call legacy helpers — reintroduces the byte substrate the rewrite exists to remove.

### D2. Reference modification: 1-based `CobolString.RefMod` read helper + `CobolString.SpliceInto` splice write; bounds raise EC-BOUND-REF-MOD.

**Rationale.** ISO §8.4.3.3.4 GR4 defines leftmost = ordinal position 1; GR5b/5c require positive integers with leftmost+length-1<=size, else EC-BOUND-REF-MOD (and §7.3.23.3 GR1: zero length raises it unless REF-MOD-ZERO-LENGTH on). Splice `dst[..(p-1)] + slice + dst[(p-1+len)..]` is the exact value-semantics write; slice is pre-sized by CobolString.Store so editing is not re-applied (spec NOTE line 21209).

**Rejected alternatives.** Storing ref-mod slices as separate fields (no — a slice is a transient view of the parent, not its own datum, §8.4.3.3 GR5 'unique data item that is a subset'). Silently clamping out-of-range (rejected as default — masks bugs; throw is conformant since results are otherwise undefined; clamp left as an owner-gated lenient dialect).

### D3. Counters (TALLYING), COUNT IN, and WITH POINTER route through the numeric subsystem (CobolNum + NumProfile), never a parallel int path.

**Rationale.** They are numeric data items; the singular-pattern rule (feedback_one_mechanism_per_job) requires one canonical numeric store. Tally accumulates (INSPECT GR11: counter NOT initialized), so read current via CobolNum then store sum.

**Rejected alternatives.** Treat counters as raw C# long and bypass NumProfile — would skip PIC truncation/scale/sign and diverge from every other numeric store; rejected.

### D4. Overlapping operands: read source(s) into locals once, build into a working buffer, single write-back to dest.

**Rationale.** Spec calls overlap undefined (INSPECT GR13/18/21, STRING GR10, UNSTRING GR18) — undefined permits the sensible deterministic result. Read-once/write-once delivers that for free and is the only sane behavior. Also satisfies STRING GR7 (only written positions change) by seeding the working buffer from dest's current value.

**Rejected alternatives.** In-place mutation that re-reads dest during the op (would make REPLACING create spurious later matches — the legacy RunReplaceCycle already reads the PRE-modification text precisely to avoid this).

### D5. National (PIC N) uses the identical string helpers with NO surrogate-aware handling; one COBOL character = one UTF-16 code unit = one C# char.

**Rationale.** ISO §8.5.1.4 line 8067: 'COBOL does not provide any special handling or recognition of surrogate pairs … Each two-octet code element of UTF-16 is treated in COBOL as though it were itself a character.' §8.4.3.3.4 NOTE: ref-mod treats surrogate halves as separate positions. C# string indexing IS per-code-unit — so .NET string is the exact COBOL national model; PIC N and PIC X share every algorithm.

**Rejected alternatives.** StringInfo/text-element (grapheme) iteration or Rune enumeration — would over-merge surrogate pairs / combining sequences, violating the spec's explicit per-code-unit rule; rejected.

### D6. HIGH-VALUE → U+00FF and LOW-VALUE → U+0000 for alphanumeric; QUOTE → U+0022; SPACE → U+0020; ZERO → '0'.

**Rationale.** Matches the Latin-1 boundary codec the runtime already established (Text/CobolString uses Encoding.Latin1, byte k ↔ U+00kk, ADR R10) and the legacy lowerers' \xFF/\x00. Keeps figurative constants consistent across the file-I/O byte boundary.

**Rejected alternatives.** (none for alphanumeric — U+00FF/U+0000 matches the Latin-1 codec.) **National refinement (SSOT §14.9 / §18 #14):** national `HIGH-VALUE` = **U+FFFF** (the 2-octet extreme), national `LOW-VALUE` = **U+0000**; alphanumeric stays U+00FF/U+0000 (the single-octet extreme, preserving ASCII/Latin-1 ordering through the ordinal `CobolString.Compare`).

### D7. String-op targets use the universal `Place` lvalue (`Read()`/`Write(rhs)`) — NOT a separate StringLvalue abstraction. [canonical: SSOT §14.1]

> **The `Place` is the SOLE lvalue model** — there is NO separate StringLvalue abstraction (SSOT §14.1:
> **`StringLvalue` IS a `Place`**). There is ONE lvalue model (`Place`, built by `ReferenceResolver`) consumed
> identically by MOVE / arithmetic / INSPECT / STRING / UNSTRING / file READ-INTO / CALL-by-reference.
> INSPECT/STRING/UNSTRING emit `field.Write(CobolStrings.…(field.Read(), …))` over a `Place`. Introducing a second
> per-verb lvalue would violate `feedback_one_mechanism_per_job`.

**Rationale.** String-op targets are subscripted elements (FLD(I)), qualified names (X OF Y), and ref-mod slices (FLD(3:5)) — all resolved by `ReferenceResolver` into the universal `Place` (`MemberPlace`/`RefModPlace`, implemented in `src/Cobol.Net.Compiler/Binding/Model/Place.cs`). The single `Place` (a `MemberPlace`/`RefModPlace`) gives each a read C# expression and a write-back form. Centralizing this in the one resolver is required before any string-op write emits correctly.

**Rejected alternatives.** Per-statement ad-hoc string building (would duplicate the splice/subscript logic across INSPECT/STRING/UNSTRING/MOVE — rejected per refactor-first/scan-all-similar).

### D8. INSPECT/STRING/UNSTRING marshal operands into parallel arrays and call ONE runtime method per statement (per comparison cycle for INSPECT).

**Rationale.** All TALLYING/REPLACING operands form a single left-to-right comparison cycle (ISO §14.9.22.4 GR8); they cannot be lowered independently. This is the proven legacy structure (one IrInspectTallying/Replacing instruction).

**Rejected alternatives.** One helper call per operand — would break the shared-cycle eligibility (LEADING/FIRST/region interactions); rejected, it produces wrong NIST results.

## C# mapping

> **Backend note (G4, SSOT §18 #23).** Everything below is the **RoslynBackend rendering**. Codegen sits behind
> `ICodeGenBackend` over ONE backend-neutral bound tree (`--backend roslyn|cil`); the RoslynBackend (C# source) is
> primary/v1; a Cecil/CIL backend is future-additive with its OWN private structure→branch lowering — NO shared
> lowered IR. ALL semantics live in the binder/bound tree; emitters only RENDER; bound nodes carry no pre-rendered
> C#-specific fragments where a structured form is feasible: the bound tree carries STRUCTURED operands (operand
> kinds, patterns, BEFORE/AFTER regions, send/target lists — never pre-rendered C# fragments), and every backend
> renders the same statement-level calls into the shared `Cobol.Net.Runtime` entry points
> (`CobolStrings`/`CobolString`/`CobolNum`); a future CilBackend does its own private lowering to the identical
> runtime methods.

RUNTIME CLASS: `public static class CobolNet.Runtime.CobolStrings` (ported from legacy `InspectRuntime` + `StorageArea` STRING/UNSTRING). All algorithms operate on `string`/`char[]`; no byte arrays.

=== REFERENCE MODIFICATION ===
READ:  `FIELD.Substring(p-1, len)` where p,len are 1-based leftmost-position + length (ISO §8.4.3.3.4 GR4: leftmost = ordinal 1). With length omitted the substring runs to the end of the item. A single bounds helper validates and (when EC-BOUND-REF-MOD checking is enabled) raises the fatal EC-BOUND-REF-MOD:
  `public static string RefMod(string s, int leftmost, int length, bool allowZeroLength = false)` — validates 1<=leftmost<=s.Length; a SPECIFIED length shall be positive-nonzero with leftmost+length-1<=s.Length (item 5c) — a negative specified length is a violation regardless of the directive, and length==0 is a violation UNLESS `allowZeroLength` (the REF-MOD-ZERO-LENGTH directive, §7.3.23, relaxes ONLY zero). The OMITTED "to the end" form is encoded by passing the distinct sentinel **`CobolString.OmittedRefModLength`** (= `int.MinValue`, emitted via `RuntimeApi.OmittedRefModLength`) — NOT −1, so a specified length that evaluates to a negative is distinguishable from the omitted form and can raise (review C14; a −1 sentinel made the negative-length violation structurally undetectable). Checking OFF (the default) clamps/space-pads leniently (byte-identical to a pre-slice build).
WRITE (the lvalue): `FIELD[a:b] = expr` becomes splice + a single MOVE-into-slice:
  COBOL `MOVE X TO FIELD(3:5)` →
  `FIELD = CobolString.SpliceInto(FIELD, 3, 5, CobolString.Store(<X-image>, 5));`
  where `CobolString.SpliceInto(string dst, int leftmost, int length, string newSlice)` returns `dst[..(leftmost-1)] + newSlice + dst[(leftmost-1+length)..]` (newSlice already exactly `length` chars via CobolString.Store — left-justified space-pad/truncate, the receiving-into-a-slice MOVE). Editing is NOT re-applied (spec NOTE at line 21209: ref-mod of an edited item as whole-of-itself prevents editing).
  > **Runtime-roster note (SSOT §14.8):** ref-mod read/write live on **`CobolString`** (the
  > single-string class) as **`RefMod`** and **`SpliceInto`** — NOT on `CobolStrings` and NOT under a separate
> `…Store` splice-helper name.
  > `CobolStrings` is reserved for the MULTI-operand verbs (`InspectTally`/`InspectReplace`/`StringInto`/
  > `UnstringExtract`). (`CobolString.RefMod`/`SpliceInto` are implemented.)
Length-omitted write target: length = `dst.Length - (leftmost-1)`.

=== ALPHANUMERIC MOVE + COMPARISON (already present, keep) ===
MOVE alpha→alpha: `DEST = CobolString.Store(<src-image>, destWidth, justifiedRight);` (left-justify pad/truncate right; JUSTIFIED RIGHT pad/truncate left — ISO §14.9.25/§13.18.32). Numeric source → its DISPLAY image first.
COMPARE: `CobolString.Compare(a,b) <op> 0` — shorter operand space-extended (ISO §8.8.4.1.2), ordinal.

=== INSPECT ===
Signatures (port of InspectRuntime, byte[]→string; counters/COUNT via CobolNum):
  `int[] InspectTally(string target, int[] kinds, string?[] patterns, string?[] befores, string?[] afters, bool backward)` — returns per-operand counts; emitter adds each into its counter field via CobolNum.
  `string InspectReplace(string target, int[] kinds, string?[] patterns, string?[] repls, string?[] befores, string?[] afters, bool backward)` — returns new value.
  `string InspectConvert(string target, string fromSet, string toSet, string? before, string? after, bool backward)` — returns new value.
EMIT (COBOL `INSPECT WS-T TALLYING C FOR ALL "A" BEFORE "X"`):
  `var _c = CobolStrings.InspectTally(WS_T, new[]{0/*All*/}, new[]{"A"}, new[]{"X"}, new string?[]{null}, false);`
  `C = CobolNum.Store(CobolNum.AsLong(C, _P_C) + _c[0], 0, _P_C);`   // counter NOT initialized (GR11)
COBOL `INSPECT WS-T REPLACING ALL "A" BY "B"` → `WS_T = CobolStrings.InspectReplace(WS_T, new[]{0}, new[]{"A"}, new[]{"B"}, new string?[]{null}, new string?[]{null}, false);`
kinds map: Tally{All=0,Leading=1,Characters=2}; Replace{All=0,First=1,Leading=2,Characters=3} (the enum ordinals are runtime ABI). The tallying-phrase (Format 1/3) admits ONLY ALL / LEADING / CHARACTERS (ISO §14.9.22.2); FIRST is a REPLACING-phrase-only adjective and TRAILING is not an INSPECT keyword in any format, so `TALLYING … FOR FIRST`/`FOR TRAILING` is non-conforming and the binder REJECTS it with a diagnostic — never aliased to ALL. The single comparison cycle over ordered operands is preserved exactly (ISO §14.9.22.4 GR8) — this is why "ALL A" before "LEADING AH" leaves LEADING=0.

=== STRING ===
One helper call per statement (sendings marshalled into parallel arrays); pointer is `ref int` (read via `CobolNum.AsLong` into an `int` local at statement start, written back via `CobolNum.Store` at statement end):
  `string StringInto(string dest, ref int pointer, out bool overflow, StringSend[] sends)` where
  `readonly record struct StringSend(string Value, string? Delimiter, bool BySize)` — Value already the sending operand's display image; Delimiter null = DELIMITED BY SIZE-equivalent (whole value).
EMIT (`STRING A DELIMITED BY " " B DELIMITED BY SIZE INTO R WITH POINTER P ON OVERFLOW ... END-STRING`):
  `int _p = (int)CobolNum.AsLong(P, _P_P);` (or `=1` if no POINTER)
  `R = CobolStrings.StringInto(R, ref _p, out bool _ovf, new[]{ new StringSend(<A-img>, " ", false), new StringSend(<B-img>, null, true) });`
  `P = CobolNum.Store(_p, 0, _P_P);`
  `if (_ovf) { <on-overflow> } else { <not-on-overflow> }`
Working buffer starts as `dest.ToCharArray()` (GR7: only written positions change). Algorithm = legacy StringConcat/StringConcatLiteral per character with 1-based pointer; overflow when pointer<1 or >dest.Length before a char move (GR8).

=== UNSTRING ===
  `int UnstringInto(string source, ref int pointer, ref long tally, UnstringDelim[] delims, UnstringTarget[] targets, out bool overflow)` —
  `readonly record struct UnstringDelim(string Value, bool IsAll);`
  `class UnstringTarget { Action<string> SetInto; Action<string>? SetDelimIn; Action<int>? SetCount; PicShape IntoShape; }`
  (closures or out-params: but to keep emit simple, prefer per-INTO loop in EMITTED code calling a single-extract helper, mirroring legacy UnstringExtract.)
PREFERRED EMIT (mirrors proven legacy per-INTO loop): one `UnstringExtract` call per INTO target:
  `int _p = (int)CobolNum.AsLong(P, _P_P);  long _t = CobolNum.AsLong(TLY, _P_TLY);  bool _ovf=false;`
  pre-check: `if (_p<1 || _p>source.Length) _ovf=true;`
  per INTO: `var _ex = CobolStrings.UnstringExtract(source, ref _p, delimArr, allArr, out string? _delim); if(_ex>=0){ FLD = CobolString.Store(_ex_str, fldWidth); _t++; } if(DLM!=null) DLM=CobolString.Store(_delim??"", dlmWidth); if(CNT!=null) CNT=CobolNum.Store(_ex_count,0,_P_CNT); }`
  post: overflow |= (_p <= source.Length); writeback P, TLY.
  `(string extracted, int count, int newPointer, string matchedDelim) UnstringExtract(string src, ref int ptr, string[] delims, bool[] allFlags)` — earliest-delimiter wins, tie→first-listed, ALL skips contiguous repeats (all ported verbatim from legacy). Two contiguous delimiters → empty extract → space-fill (alpha) / zero-fill (numeric) via CobolString.Store / CobolNum.Store (GR8). DELIMITED BY absent → examine receiver-width characters — ONE LESS than the receiver size when the receiver's sign occupies a separate character position (SIGN … SEPARATE CHARACTER; an over-punched sign occupies none), stopping early if the source is exhausted first (GR11b).

=== NON-ALPHANUMERIC / GROUP MATERIALIZATION (the G6-boundary helper) ===
For a numeric/edited/group target/source, the emitter wraps with materialize↔writeback:
  read:  `string img = <typed>.AsAlphanumericImage();`  (numeric long → unsigned digit string GR4d; group record struct → field-concatenation image)
  op on `img`
  writeback: `<typed> = TypedFromAlphanumericImage(img);` (numeric: re-parse digits keep sign; group: re-slice the image back into each sub-field by its width). Helper signatures — **implemented (G6-core ✅)** as per-group instance methods generated on each record struct: `public readonly string AsImage()` (concatenates the leaves' character images) + `public void FromImage(string)` (distributes a character image back into the leaves) — see `GroupImageCodec` in `src/Cobol.Net.Compiler/CodeGen/DataDivision/`.

## Hard problems

### Non-alphanumeric / group operands: INSPECT GR4b/4c, ref-mod GR2/3, and STRING/UNSTRING all say a numeric/edited/group operand is 'treated as if redefined alphanumeric (or national) of the same size'. For a `record struct` group or a `long` numeric this needs materialize-to-image → operate → decompose-back-to-typed-fields — the whole-group-alphanumeric boundary deferred to G6.

Two paths. NATIVE-STRING path (in scope now): elementary alphanumeric/national `string` — direct. MATERIALIZE/WRITEBACK path (**implemented, G6-core ✅**): the generated group-struct method `AsImage()` concatenates each leaf's image left-to-right by declared width; `FromImage(string)` re-slices the result back into each leaf by width and re-stores (numeric leaves re-parse digits keeping scale/sign). Numeric elementary: `n.AsAlphanumericImage()` = unsigned zero-padded digit string (GR4d de-signing, sign retained on completion for identifier-1); writeback re-parses. The emitter selects the path by the bound item's PicCategory/IsGroup. This is the single case that breaks a 'it's all strings' design; it is scoped explicitly, not silent.

### BACKWARD inspection (ISO §14.9.22, NEW IN COBOL-2023 — VERSION_CHANGE_REFERENCE.md #77 / E.3.3 item 34): right-to-left scan with BEFORE/AFTER evaluated in scan direction; matching still left-anchored at each position. At `--std` 85/2002/2014 the BACKWARD phrase is rejected with a not-yet-introduced diagnostic (G1).

Port the legacy reverse-wrapper verbatim: reverse the target string AND each multi-char pattern/delimiter/before/after, run the existing forward passes, then reverse the result buffer back for REPLACING/CONVERTING (TALLYING needs no un-reverse — per-operand counts are direction-independent). FROM/TO sets for CONVERTING are positional maps, NOT reversed. This exactly reproduces the spec NOTE example (INSPECT BACKWARD "A12C21D12EF" TALLYING CHARACTERS BEFORE "12" = 2).

### The lvalue/subscript/ref-mod grammar shape: `FIELD(3:5)` and `TBL(I)` parse identically as `cobolWord subscriptPart` where subscriptPart is a flat `subToken+` sequence in lexer SUBSCRIPT mode; the binder distinguishes ref-mod by the presence of SUB_COLON (CobolParserCore.g4 lines 362-403).

The BINDER (`ReferenceResolver`) must replicate the legacy discrimination: scan the subToken sequence; if it contains SUB_COLON → ref-mod (split into leftmost arithmetic-expr : length arithmetic-expr, producing a `RefModPlace` wrapping the inner `Place`); else → subscript list (a subscripted `MemberPlace`). Each backend then only RENDERS the resulting `Place`'s read/write forms (RoslynBackend → `CobolString.RefMod`/`SpliceInto`; a future CilBackend renders its own) — semantics stay in the binder per the dual-backend discipline (SSOT §18 #23). refModPart (default-mode `arithmeticExpression COLON arithmeticExpression?`) is the alternative shape when not after an identifier. Both feed `CobolString.RefMod`/`CobolString.SpliceInto`.

### STRING WITH POINTER and UNSTRING WITH POINTER/TALLYING write the pointer/tally back, but those are numeric items at arbitrary scale; STRING GR7 requires only written dest positions change.

Read pointer/tally via CobolNum.AsLong(field, profile) into a C# `int`/`long` local at statement start; pass by `ref`/track in emitted code; write back via CobolNum.Store at statement end. Seed STRING's working char[] from dest.ToCharArray() so untouched positions retain prior content. Overflow conditions: STRING ptr<1 or >len before a char move (GR8); UNSTRING ptr<1 or >len at START (GR15a) OR all receivers acted upon with chars remaining (GR15b) — both ported from legacy pre/post checks.

### UNSTRING DELIMITED BY ALL + multi-delimiter OR + DELIMITER IN / COUNT IN interactions; two contiguous delimiters; field-vs-literal delimiters (delimiter field can change between INTOs).

Port legacy UnstringExtract exactly: earliest-matching delimiter across OR-set wins; tie → first-listed (no char belongs to >1 delimiter, GR10); ALL skips all contiguous repeats and the whole run counts as one delimiter; two contiguous delimiters → zero-length extract → receiver space-filled (alpha) or zero-filled (numeric) per GR8 via CobolString.Store/CobolNum.Store; DELIMITER IN gets matched delimiter (space-filled if delimiter was end-of-source, GR11d); COUNT IN = chars examined excluding delimiter (GR11e, GR4); tally += 1 per receiver ACTED UPON (GR14, legacy returns -1 when source exhausted = not acted upon). Field delimiters re-read per statement (legacy reads them into the delim string[] once at statement start, which is correct — delimiter identifiers are item-identified once).

## Edge cases

- Zero-length identifier-1: INSPECT is a successful no-op leaving operands unchanged (§14.9.22.4 GR2); UNSTRING terminates immediately (§14.9.48.4 GR2); STRING with a zero-length sending operand ignores that operand (§14.9.43.4 GR3a). Guard at helper entry.
- Ref-mod length 0: EC-BOUND-REF-MOD unless REF-MOD-ZERO-LENGTH directive ON (§7.3.23.3 GR1, line 4914), then a zero-length result is allowed (RefMod returns "").
- Ref-mod out of range / non-integer / leftmost+length-1 > size: EC-BOUND-REF-MOD (§8.4.3.3.4 GR5b/5c trailing paragraph line 7089).
- Ref-mod past end with length omitted: extends to rightmost position (§8.4.3.3.4 GR5c) — RefModToEnd = Substring(p-1).
- STRING/UNSTRING pointer past end / < 1: STRING sets EC-OVERFLOW-STRING and stops further transfer (§14.9.43.4 GR8); UNSTRING sets EC-OVERFLOW-UNSTRING (§14.9.48.4 GR15a). ON OVERFLOW taken if present, else nonfatal-exception continue.
- INSPECT REPLACING/CONVERTING size mismatch (literal-3 size != literal-1 size; CHARACTERS replacement not 1 char; CONVERTING toSet shorter than fromSet): EC-RANGE-INSPECT-SIZE, results undefined (§14.9.22.4 GR14/15/22). Legacy guards: ReplaceAll skips when pat.Length != repl.Length; CONVERTING maps only up to min(from,to) length.
- INSPECT signed numeric identifier-1 (DISPLAY, no editing): inspected as unsigned absolute digits, original sign retained on completion (§14.9.22.4 GR4d) — port legacy ReadInspectTarget/ReplacingPass signed path.
- INSPECT shared comparison cycle: 'ALL A' before 'LEADING AH' → LEADING counts 0 because the leading A is consumed by the earlier operand first (§14.9.22.4 GR8) — preserved by the single ordered cycle.
- INSPECT BEFORE/AFTER with delimiter not found: BEFORE → behaves as if absent (whole remainder eligible); AFTER → operand NEVER eligible (§14.9.22.4 GR9b/9c). Legacy ComputeRegion: AFTER-not-found → empty region (start=end).
- STRING only changes the dest positions actually written; all other positions keep prior content (§14.9.43.4 GR7) — working buffer seeded from current dest.
- UNSTRING two contiguous delimiters → empty receiver → space/zero fill (§14.9.48.4 GR8); end-of-source before delimiter → last char examined, DELIMITER IN space-filled (GR11b/11d).
- MOVE numeric source → alphanumeric receiver moves the DISPLAY image (digits, no sign/point for unsigned); a longer source truncates right, shorter pads spaces right (left-justified) or per JUSTIFIED RIGHT.
- National (PIC N): surrogate pair bisected by truncation/ref-mod is NOT detected (§8.4.3.3.4 NOTE, §8.5.1.4) — by design; user's responsibility. No special handling.
- Figurative constant as STRING/UNSTRING delimiter or INSPECT pattern: one-char item of the receiver's usage (display U+00xx / national) — SPACE=' ', ZERO='0', HIGH-VALUE=U+00FF, LOW-VALUE=U+0000, QUOTE='"'.
- DELIMITED BY SIZE / delimiter null: whole sending value transferred (§14.9.43.4 GR1).

## Per-edition gating (G1: four compilers in one)

String ops span editions. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Rows cite `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows, so 85↔2002 gating derives from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog); matrix cases per `docs/VERSION_TEST_MATRIX_DESIGN.md` (the (construct × edition) matrix; Phase 0 done):

- **INSPECT … BACKWARD — introduced in COBOL-2023** (VCR #77, E.3.3 item 34). At `--std` 85/2002/2014: reject with a "not introduced until COBOL-2023" diagnostic; the matrix gets a negative case per pre-2023 edition.
- **REF-MOD-ZERO-LENGTH directive + zero-length ref-mod — COBOL-2023** (VCR #11/#30/#109). At `--std 2023`: zero-length ref-mod is legal only with the directive ON, else EC-BOUND-REF-MOD (plus FLAG-14 flagging per §7.3.15.4 GR4 i)). At earlier `--std`: the directive itself is diagnosed as not-yet-introduced, and a zero-length ref-mod result is undefined by those editions — we keep the conformant throw in all editions (deterministic; "undefined" permits it).
- **National (PIC N, N"…" literals, national string-op operands) — a 2002 introduction** (derive gating from the 2002 standard; no VCR row). At `--std 85`: reject with a not-yet-introduced diagnostic.
- **Reference modification and INSPECT CONVERTING** predate 2002 and are in the project's COBOL-85 baseline (NIST CCVS-85 exercises both); no gate.
- All other behavior here (comparison cycle, overflow conditions, MOVE/JUSTIFIED, dest-seeding) is edition-invariant unless a VCR row says otherwise; any divergence is gated by DialectLevel in the BINDER, never in a backend.

## ISO citations

- ISO/IEC 1989:2023 §8.4.3.3.4 (reference-modification general rules: 1-based leftmost ordinal, EC-BOUND-REF-MOD on out-of-range/zero/non-integer, length-omitted to rightmost, UTF-16 surrogate-as-position NOTE) — spec lines 7071-7091
- ISO/IEC 1989:2023 §7.3.23.3 GR1 (REF-MOD-ZERO-LENGTH directive: zero-length ref-mod raises EC-BOUND-REF-MOD when off) — line 4914
- ISO/IEC 1989:2023 §8.4.3.3.4 GR2/GR3 (numeric/edited DISPLAY operand treated as alphanumeric of same size; usage NATIONAL non-national treated as national of same size) — lines 7075-7077
- ISO/IEC 1989:2023 §8.5.1.4 (limitations of character handling: no surrogate-pair recognition; each UTF-16 code element treated as one character) — lines 8051-8067
- ISO/IEC 1989:2023 §8.8.4.1.2 (alphanumeric relation condition: shorter operand space-extended)
- ISO/IEC 1989:2023 §14.9.22 INSPECT — General rules §14.9.22.4 (GR2 zero-length no-op; GR4a-d operand treatment incl. signed-numeric de-signing; GR8 single comparison cycle; GR9 BEFORE/AFTER; GR10/16 ALL/LEADING/FIRST transitivity; GR12 tally rules; GR13/18/21 overlap undefined; GR14/15/22 EC-RANGE-INSPECT-SIZE; GR17 replace rules; GR19 format-3; GR20/23 CONVERTING) — lines 28205-28358; BACKWARD §14.9.22.4 GR3/8 + NOTE — line 28227-28267
- ISO/IEC 1989:2023 §14.9.43 STRING — General rules §14.9.43.4 (GR1 DELIMITED BY SIZE whole value; GR3a-c transfer + zero-length sending ignored; GR4/5 POINTER >0; GR6 char-at-a-time; GR7 only written positions change; GR8 EC-OVERFLOW-STRING + ON OVERFLOW; GR10 overlap undefined) — lines 32337-32383
- ISO/IEC 1989:2023 §14.9.48 UNSTRING — General rules §14.9.48.4 (GR2 zero-length terminate; GR7 ALL contiguous; GR8 contiguous delimiters fill; GR9 multi-char delimiter; GR10 OR/first-listed; GR11a-g pointer/DELIMITED BY/COUNT IN/DELIMITER IN; GR13 pointer increment; GR14 tally per receiver acted upon; GR15 overflow conditions; GR16 ON OVERFLOW; GR18 overlap undefined) — lines 32764-32859
- ISO/IEC 1989:2023 §14.9.25 MOVE (alphanumeric receiving: left-justify space-fill/right-truncate) + §13.18.32 JUSTIFIED (right-justify pad/truncate left)
- ISO/IEC 1989:2023 §14.6.10 Overlapping operands (results undefined when operands overlap) — referenced by INSPECT GR13/18/21, STRING GR10, UNSTRING GR18; spec note line 24376
- ISO/IEC 1989:2023 NOTE at line 21209 (ref-mod of an edited receiving item as the whole of itself prevents editing rules being reapplied)

## Open questions (#1 resolved in `COBOLNET_DESIGN.md` §18; #2–#4 still open — record resolutions in the SSOT §18)

- National HIGH-VALUE/LOW-VALUE code points — **RESOLVED (SSOT §18 #14):** national `HIGH-VALUE` = **U+FFFF**, national `LOW-VALUE` = **U+0000**; alphanumeric stays U+00FF/U+0000 (Latin-1 codec). Full custom-`ALPHABET` collating is deferred behind the fixed `CobolString.Compare(a, b, weights?)` seam.
- Out-of-range / zero / non-integer reference modification: throw (raise EC-BOUND-REF-MOD → CobolRuntimeException) vs clamp. Recommend THROW as default (conformant; results otherwise undefined). Should a lenient dialect that clamps be offered (the legacy compiler had dialect-gated leniencies)? Owner-gated.
- Exception-condition surfacing in v1: the spec ties STRING/UNSTRING/INSPECT/ref-mod to EC-OVERFLOW-*, EC-RANGE-INSPECT-SIZE, EC-BOUND-REF-MOD as checkable conditions. v1 wires ON OVERFLOW directly and throws for fatal ref-mod; full EC handling (>>TURN, USE AFTER EXCEPTION CONDITION, EC- status registers) is the broader exception subsystem (M2 EC/exceptions). Confirm string-ops only needs ON OVERFLOW + ref-mod-throw now, deferring EC-register integration.
- REF-MOD-ZERO-LENGTH directive plumbing: it is a compile-time directive affecting whether zero-length ref-mod is legal. Is it threaded into the binder now (so RefMod emits the zero-allowed branch) or deferred? Recommend recognizing the directive in G7 as part of the version framework: REF-MOD-ZERO-LENGTH is a COBOL-2023 directive (`VERSION_CHANGE_REFERENCE.md` #11/#30/#109) — at `--std 2023` it gates zero-length ref-mod (OFF ⇒ EC-BOUND-REF-MOD, plus FLAG-14 flagging per §7.3.15.4 GR4 i)); at `--std` 85/2002/2014 the directive itself is diagnosed as not-yet-introduced and zero-length ref-mod keeps the conformant throw (undefined in those editions). Default OFF (throw on zero) until then.
