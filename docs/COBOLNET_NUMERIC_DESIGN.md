# COBOL.NET — Numeric Model (native scaled-integer) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §6; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for COBOL.NET's native scaled-integer numeric model. SUBSTRATE (owner-locked, documented + hardened): every fixed-point datum is a native integer holding its UNSCALED value (all digits; decimal point = compile-time scale metadata). Storage CLR type by capacity: PIC ≤18 digits → `long`; 19–38 digits → `Int128` (value type); COMP-1/COMP-2 → `float`/`double`; COMP-5 by width → `sbyte/byte/short/ushort/int/uint/long/ulong` (binary-wrap). NO `decimal`, NO `BigInteger`.

THE CENTRAL HARDENING (advisor crux #2): the runtime's value engine must be Int128-monomorphic, NOT long. The current `src/CobolNet.Runtime/Numeric/CobolNum.cs` is long-only and silently overflows real COMPUTE (e.g. `COMPUTE c = a * b` on two PIC 9(18) = 36 digits). Redesign: a `readonly record struct CobolInt(Int128 Unscaled, int Scale)` is the single intermediate carrier. Storage stays the narrow native type; every operand widens long→Int128 at op entry, scales-align, computes in Int128, and a single `TryStore` rescales/rounds/truncates/bounds-checks back into the receiver's storage type. The 'Int128 escape boundary' is reached only when a single product of two ≥19-digit operands exceeds Int128 (~38 digits) → EC-SIZE-OVERFLOW.

INTERMEDIATE PRECISION (ISO §8.8.1, mined from the proven legacy `decimal` path): arithmetic operates on the algebraic VALUE (§8.8.1.2). Per-operator result scale: ADD/SUBTRACT → max(scales); MULTIPLY → sum(scales); DIVIDE/COMPUTE-division → a guard scale = max(receiver-scales, operand-scales) + DIV_GUARD_DIGITS (DIV_GUARD_DIGITS=14, reproducing the legacy decimal accumulator's ~28-sig-digit headroom — the one policy `decimal` auto-picked that I must make explicit). EXPONENTIATION integer powers expand to repeated multiply (§8.8.1.5.4 a–d). Statement-arithmetic enforces the 31-digit composite-of-operands limit (§ rule 2, p595); COMPUTE expressions have NO composite limit (§8.8.1.2 rule 7) — Int128 is the cap, SIZE ERROR past ~38 digits. v1 mode = NATIVE arithmetic (§8.8.1.3, implementor-defined = Int128 fixed-point); STANDARD-DECIMAL (decimal128) and STANDARD-BINARY are owner-gated/deferred.

ALL USAGES with capacity/truncation: DISPLAY/COMP/COMP-4/BINARY → DigitCount discipline (PIC 99 COMP holds 0–99 not 0–32767); COMP-3/PACKED → 2n−1 packed-digit capacity; COMP-5/BINARY-CHAR…DOUBLE → native two's-complement width (PIC S9(4) COMP-5 = −32768..32767, PIC 9(4) COMP-5 = 0..65535); COMP-1/COMP-2 → IEEE, bypass the scaled engine. The 8 ROUNDED modes are the existing `CobolRounding` enum (correct); harden `Store`→`TryStore` (bool, receiver-unchanged-on-overflow, PROHIBITED-inexact → SIZE ERROR), the current `Store` is the no-SIZE-ERROR branch only.

NUMERIC-EDITED formatting: PORT the proven two-pass legacy `PicRuntime.FormatByEditPattern`/`FormatNumericEdited` verbatim (Z * $ + - CR DB B 0 / . , fixed+floating insertion, asterisk fill, BLANK WHEN ZERO, full-field-blank) into a `CobolEdit.Format(CobolInt, EditPattern, env)` runtime helper; the receiver field is a C# `string`. SIGN OVERPUNCH (NIST-exact, confirmed against legacy NIST-passing tests): IBM-ASCII tables positive 0→'{',1→'A'…9→'I'; negative 0→'}',1→'J'…9→'R'. Default = TrailingOverpunch; SignKind ∈ {LeadingOverpunch, TrailingOverpunch, LeadingSeparate('+'/'-'), TrailingSeparate}. NumProfile MUST gain a SignKind field (advisor #4 — currently only a `Signed` bool). P scaling, mixed float/fixed, IS NUMERIC, comparison scale-align, MOVE rescale all specified below.

## Decisions

### D1. Runtime value engine is Int128-monomorphic via a `readonly record struct CobolInt(Int128 Unscaled, int Scale)`; storage stays the narrow native type (long/Int128/native-int/float/double).

**Rationale.** Native `long` overflows real COMPUTE: the current CobolNum.cs caps Pow10 at 10^18 and does `num *= Pow10(exp)` / `Rescale` on `long`, so `COMPUTE c = a*b` with two PIC 9(18) operands (36 digits) silently overflows. Int128 holds 38 digits — covers every legal COBOL fixed-point picture (max 31 digits ISO §6046, 38 in some 2002 profiles) AND their sum/product intermediates within the 31-digit composite limit. Int128 is a hardware-adjacent value type (two longs, no GC, no allocation) — orders cheaper than BigInteger and exactly the 'fixed-size Int128 escape hatch' the architecture names.

**Rejected alternatives.** (a) Keep long-only — REJECTED: silently wrong on common multiply/divide; the whole point of the rewrite is correctness. (b) decimal/BigInteger intermediates — REJECTED: owner-locked out (decimal is software 96-bit/28-digit and can't even hold standard-decimal's 34; BigInteger allocates). (c) Generic `INumber<T>` arithmetic monomorphized per storage width — REJECTED: codegen + JIT-bloat complexity, unreadable generated C#, and storage-width-typed math reintroduces the very overflow we're eliminating. One Int128 path is the singular pattern.

### D2. DIVIDE/COMPUTE-division quotient is computed at an explicit guard scale = max(all receiver fraction-scales, all operand scales) + DIV_GUARD_DIGITS, DIV_GUARD_DIGITS = 14; the final per-receiver store rescales+rounds to the receiver's scale.

**Rationale.** The proven legacy lowerer (ArithmeticLowerer.LowerDivide) computes division into a `decimal` accumulator (≈28-29 significant digits) and only rescales to the receiver on the final IrMoveAccumulatedToTarget — so its division 'intermediate scale' is decimal's natural headroom, the ONE scale `decimal` auto-picked that Int128 forces me to make explicit. 14 guard digits past the deepest receiver/operand scale reproduces that headroom while staying inside Int128's 38 digits for realistic operand sizes; rounding then happens exactly once, at the receiver, honoring the ROUNDED mode (ISO §14.7 — ROUNDED applies to the transfer into the resultant, NOTE 1 p595).

**Rejected alternatives.** (a) Quotient at receiver scale directly (legacy IrPicDivide degenerate path) — REJECTED for the general case: rounds before the receiver knows its scale, loses guard digits, fails NIST division-rounding tests. (b) Unbounded rational/exact division — REJECTED: COBOL division is inherently lossy at a finite scale; exactness is undefined for non-terminating quotients. (c) decimal accumulator like legacy — REJECTED: locked out + caps at 28 digits anyway.

**Implementation status (interim, DEVLOG 495 — the ROUNDED phrase, long engine).** The ROUNDED phrase (§14.7.4) is now wired per-receiver through `CobolNum.Store(value, scale, profile, mode)`; the eight modes resolve in the binder (`Receiver(Place, CobolRounding)`; no phrase → Truncation, MODE IS x → the named mode, bare ROUNDED → the program's DEFAULT ROUNDED mode from the now-fully-parsed OPTIONS paragraph — `DataBinder.Options.DefaultRounding`, ISO §11.9.6, NearestAwayFromZero when absent; DEVLOG 496). For a **single (outermost) division feeding a receiver** — the DIVIDE statement and a COMPUTE whose working scale equals the receiver scale — the interim long engine does NOT use the D2 guard scale (`+DIV_GUARD_DIGITS`); instead it computes the quotient *directly at the receiver scale with the receiver's mode* via `CobolNum.Divide`→`RoundDiv`, which is **exact** because `RoundDiv` rounds on the true integer remainder (it sees all lost precision), so no guard digits are needed and `long` never overflows. The D2 guard-scale model (round once at the receiver after computing the quotient at `max(scales)+14`) is still the target for divisions **nested inside a larger expression** (where intermediate precision must survive further operations); that path keeps TRUNCATION today and is correct-to-1-ULP only once the Int128 carrier (D1) lands — at which point the guard scale becomes overflow-safe. So D2 is the destination; the interim is exact for the outermost division and matches the legacy on the bare-ROUNDED NIST corpus. UPDATE (DEVLOG 497): `TryStore` (D6) now exists and is emitted whenever an ON SIZE ERROR phrase is present — it returns false (receiver unchanged) on a high-order capacity overflow or a PROHIBITED-inexact rescale, so `ROUNDED MODE IS PROHIBITED` on an inexact result now correctly raises the size error (a division's PROHIBITED-inexactness is caught in `DivideOrThrow` from the exact remainder, since the division rounds at the receiver scale). Two-phase per §14.7.5: a `try`/`catch (CobolSizeError | OverflowException)` wraps the per-receiver stores; `DivideOrThrow` (zero divisor) and `MulChecked` (intermediate long-overflow, §14.7.5 case 5) are emitted ONLY in that checked context, so a statement WITHOUT the phrase keeps the unchecked `Divide`/`*`/`Store` path (byte-identical). Still interim: a no-phrase statement does not raise EC-SIZE (the fatal path awaits the EC model); intermediate overflow beyond the long range, additive/scaling overflow, and COMP-5 width bounds await the Int128 carrier (D1) — which subsumes `MulChecked` by computing wide then capacity-checking once at the store.

### D3. v1 arithmetic mode = NATIVE (§8.8.1.3); the Int128 fixed-point engine IS the documented implementor-defined native technique. ARITHMETIC IS STANDARD-DECIMAL and STANDARD-BINARY are deferred and owner-gated.

**Rationale.** §8.8.1.3 lets the implementor define native arithmetic and it is the default when no ARITHMETIC clause is present — which is the entire NIST/conformance corpus. STANDARD-DECIMAL requires a decimal128 (34-digit, exp ±6144) decimal-floating type per §8.8.1.5.2 / ISO 60559:2020 — incompatible with both the locked substrate AND .NET `decimal`. STANDARD-BINARY is marked obsolete by the spec itself (NOTE, §8.8.1.4 / p9086). Documenting native = Int128 fixed-point is fully conformant and reproduces the 364-NIST-passing legacy behavior.

**Rejected alternatives.** (a) Implement STANDARD-DECIMAL now — REJECTED: needs a decimal-float type the lock forbids; would silently reintroduce decimal/BigInteger. Flagged as a genuine owner open question, not solved. (b) Claim full ISO arithmetic-mode conformance — REJECTED: would be an uncited spec-compat claim; native is the honest, conformant v1 surface.

### D4. Port the legacy two-pass NUMERIC-EDITED formatter (PicRuntime.FormatByEditPattern + FormatNumericEdited) verbatim into a value-level `CobolEdit.Format(CobolInt value, EditPattern pat, PicEnvironment env) → string`; the edited field's CLR storage is `string`.

**Rationale.** NUMERIC-EDITED is a famously fiddly subsystem (fixed vs floating $ + -, asterisk check-protect, BLANK WHEN ZERO, full-field-blank-on-zero, comma/B suppression inside floating zones, CR/DB, DECIMAL-POINT IS COMMA, CURRENCY SIGN). The legacy engine passes 364 NIST tests, so it is the proven oracle — re-deriving from the spec grammar risks regressions the conformance corpus already covers. It needs no byte buffer: it produces a C# string directly, which is exactly the edited item's native representation (PicCategory.NumericEdited → `string`).

**Rejected alternatives.** (a) Rewrite the editor from the ISO §14.9.x grammar — REJECTED: high regression risk against a battle-tested oracle; the task explicitly says mine the legacy for behavior. (b) Use .NET ToString format strings — REJECTED: cannot express floating insertion, check-protect, overpunch, or COBOL's zero-suppression rules. (c) Keep editing in a byte buffer — REJECTED: no byte substrate; the value→string transform is pure.

### D5. Add a `SignKind` enum field to NumProfile (LeadingOverpunch / TrailingOverpunch[default] / LeadingSeparate / TrailingSeparate) and reproduce the IBM-ASCII overpunch tables: positive {ABCDEFGHI for 0-9, negative }JKLMNOPQR.

**Rationale.** Confirmed NIST-exact against the legacy NIST-passing integration tests: PIC S9(3) +42→"04B", -42→"04K", -150→"0015}", +150→"0015{", SIGN LEADING -37→"}37". A signed-DISPLAY item's character image (what DISPLAY emits and what file records carry) is determined entirely by SignKind — the current NumProfile (and the new one) carry only a `Signed` bool, which cannot reproduce the image. The default with no SIGN clause is TRAILING overpunch (legacy SignStorageKind default).

**Rejected alternatives.** (a) Keep only `Signed` bool — REJECTED: cannot produce the overpunched DISPLAY image or SIGN SEPARATE; would fail any signed-display NIST test. (b) Store sign as a separate C# `bool isNegative` field beside magnitude — REJECTED: the unscaled Int128/long already carries the sign natively; the SignKind affects only the EXTERNAL image (DISPLAY/serialization), so it belongs in formatting metadata, not a parallel storage field.

### D6. Harden `Store` → `TryStore`: returns bool (false = ON SIZE ERROR), leaves receiver unchanged on overflow, raises SIZE ERROR for ROUNDED MODE PROHIBITED when the result is inexact at the receiver scale; capacity check is discipline-specific (DigitCount / PackedDecimal 2n−1 / BinaryCapacity two's-complement-by-width).

**Rationale.** The current new CobolNum.Store silently `%= Pow10(Digits)` truncates with no SIZE ERROR — that is only the no-ON-SIZE-ERROR branch. The legacy clean CobolNum.TryStore is the proven correct shape (ISO §14.7.5 / §14.9.4: on SIZE ERROR the receiver is unmodified and the imperative clause runs). The three capacity disciplines are already correctly modeled by NumericTruncation; TryStore must consult them. COMP-5 unsigned-8-byte (0..ulong.Max) exceeds `long`, so its storage type must be `ulong`/Int128 and ExceedsBinaryCapacity must branch on signed-vs-unsigned width (legacy clean code already does this).

**Rejected alternatives.** (a) Throw on overflow — REJECTED: SIZE ERROR is a recoverable COBOL condition with an imperative handler, not an exception; throwing would skip the receiver-unchanged rule and the NOT ON SIZE ERROR path. (b) Always truncate silently — REJECTED: wrong when ON SIZE ERROR is present and wrong for PROHIBITED.

### D7. COMP-1/COMP-2 (and mixed float/fixed expressions) bypass the scaled-integer engine: the float operand promotes the whole sub-expression to `double`, computed in IEEE, and the final store to a fixed-point receiver converts double→CobolInt at the receiver scale (round per mode).

**Rationale.** ISO §8.8.1: a floating operand makes the result floating. The legacy MoveNumericToNumeric / StoreArithmeticResult explicitly guard `if (Usage is Comp1 or Comp2) skip fixed-point scaling` and store the IEEE value directly. COMP-1=float, COMP-2=double are hardware IEEE (no PIC truncation). The 31-digit composite rule excludes float operands (§ rule 2b p595: when any operand is float, the limit applies to the OTHER operands only).

**Rejected alternatives.** (a) Force floats through the Int128 scaled path — REJECTED: float values aren't base-10 fixed-point; scaling them is meaningless and lossy. (b) Promote everything to decimal when a float is present — REJECTED: locked out + wrong (COBOL float arithmetic is IEEE binary, not decimal).

## C# mapping

DATA-DIVISION → CLR storage type:
- `01 N PIC 9(5).`            → `long N;`            (unscaled; 5 digits ≤ 18)
- `01 B PIC S9(4)V99 COMP-3.` → `long B;`            (unscaled = value×100; scale=2 metadata)
- `01 W PIC S9(31).`          → `Int128 W;`          (>18 digits)
- `01 H PIC 9(18) COMP-5.`    → `ulong H;`           (unsigned 8-byte native) ; `S9(9) COMP-5` → `int`
- `01 R PIC S9(7)V9(2).`      → `long R;`            (DISPLAY signed; SignKind=TrailingOverpunch metadata)
- `01 F COMP-1.`              → `float F;`  ; `COMP-2` → `double`
- `01 E PIC ZZ,ZZ9.99-.`      → `string E;`          (numeric-edited image)

PROFILE (compile-time emitted once per item, runtime carrier):
```csharp
public readonly record struct NumProfile {
  public required int Digits; public required int FractionDigits;
  public int LeadingScaleDigits; public int TrailingScaleDigits;
  public required bool Signed; public SignKind Sign;           // NEW (advisor #4)
  public required NumericTruncation Truncation; public int StorageLength;
  public int FractionScale => Math.Max(0, FractionDigits + LeadingScaleDigits);
}
public enum SignKind { TrailingOverpunch, LeadingOverpunch, LeadingSeparate, TrailingSeparate }
```

INTERMEDIATE CARRIER + ENGINE (replaces the long-only CobolNum):
```csharp
public readonly record struct CobolInt(Int128 Unscaled, int Scale) {
  public static CobolInt FromStorage(long v, int scale)  => new(v, scale);
  public static CobolInt FromStorage(Int128 v,int scale) => new(v, scale);
  public static (CobolInt,CobolInt) Align(CobolInt a, CobolInt b){
     int s=Math.Max(a.Scale,b.Scale);
     return (new(a.Unscaled*Pow10(s-a.Scale),s), new(b.Unscaled*Pow10(s-b.Scale),s)); }
  public static CobolInt Add(CobolInt a,CobolInt b){var(x,y)=Align(a,b);return new(x.Unscaled+y.Unscaled,x.Scale);}
  public static CobolInt Sub(CobolInt a,CobolInt b){var(x,y)=Align(a,b);return new(x.Unscaled-y.Unscaled,x.Scale);}
  public static CobolInt Mul(CobolInt a,CobolInt b)=> new(a.Unscaled*b.Unscaled, a.Scale+b.Scale);     // scale sums
  public static CobolInt Div(CobolInt a,CobolInt b,int guardScale,CobolRounding m){             // explicit guard
     int exp=b.Scale+guardScale-a.Scale; Int128 num=a.Unscaled,den=b.Unscaled;
     if(exp>=0)num*=Pow10(exp);else den*=Pow10(-exp);
     return new(RoundDiv(num,den,m), guardScale); }                                              // b==0 → caller SIZE ERROR
}
// TryStore: rescale to receiver.FractionScale (ROUNDED), capacity-check by discipline, false=ON SIZE ERROR
public static bool TryStore(CobolInt v, in NumProfile r, CobolRounding m, out Int128 stored);
```

GENERATED C# for `COMPUTE GROSS ROUNDED = RATE * HOURS + BONUS ON SIZE ERROR PERFORM E.` (RATE S9(3)V99, HOURS 9(3)V9, BONUS 9(5)V99, GROSS S9(7)V99):
```csharp
var __t = CobolInt.Add(
            CobolInt.Mul(CobolInt.FromStorage(RATE,2), CobolInt.FromStorage(HOURS,1)),  // scale 3
            CobolInt.FromStorage(BONUS,2));                                              // aligns to 3
if (CobolNum.TryStore(__t, P_GROSS, CobolRounding.NearestAwayFromZero, out var __g)) GROSS=(long)__g;
else { /* PERFORM E */ }
```

DISPLAY of a signed item `DISPLAY R` (R = -150, PIC S9(4) trailing overpunch):
```csharp
Console.WriteLine(CobolNum.FormatSignedDisplay(R, P_R));   // → "015}"  ({/} table, last digit overpunched}
```
NUMERIC-EDITED MOVE `MOVE AMT TO E` (E = ZZ,ZZ9.99-):
```csharp
E = CobolEdit.Format(CobolInt.FromStorage(AMT, P_AMT.FractionScale), P_E.EditPattern, _env);
```
IS NUMERIC on a DISPLAY field (the only nontrivial class test — magnitude+sign image are validated against PIC):
```csharp
if (CobolNum.IsNumericClass(rawImageOrValue, P_X)) ...
```

## Hard problems

### Int128 escape boundary: a single MULTIPLY of two ≥19-digit operands (or a deep COMPUTE product) exceeds Int128's ~38-digit range; COMPUTE expressions have NO composite-of-operands limit (§8.8.1.2 rule 7).

Statement arithmetic (ADD/SUB/MUL/DIV with GIVING) enforces the 31-digit composite-of-operands limit at COMPILE time (ISO rule 2, p595) → guaranteed to fit Int128, no runtime check needed. COMPUTE expressions: compute in Int128; before each Mul, if both operand digit-counts sum > 38, raise EC-SIZE-OVERFLOW (the documented escape boundary). Document this as the native-arithmetic range limit (§8.8.1.3 permits implementor-defined range). A future >38-digit need is the only thing that would force a wider carrier — explicitly out of v1 scope and flagged.

### DIVIDE intermediate/quotient scale — the one scale `decimal` auto-picked that Int128 forces explicit (advisor #3).

Mined from legacy ArithmeticLowerer.LowerDivide: it divides into a `decimal` accumulator (28-29 sig digits) then rescales to the receiver only on the final store. Reproduce with guardScale = max(receiver scales, operand scales) + DIV_GUARD_DIGITS(=14); RoundDiv at guardScale, then TryStore rounds once to the receiver scale per the ROUNDED mode. REMAINDER (DIVIDE…REMAINDER): quotient truncated to GIVING's fraction-digits (NOT rounded), remainder = dividend − quotient×divisor at the GIVING scale (legacy ComputeCobolRemainder / IrCobolRemainder uses givingFracDigits, no rounding).

### Mixed float/fixed arithmetic and the COMP-1/COMP-2 capacity bypass.

Type-classify each arithmetic sub-expression: if any operand is float-usage, the sub-expression evaluates in `double` (IEEE), and the final store to a fixed-point receiver converts double→CobolInt at the receiver scale with the ROUNDED mode (legacy StoreArithmeticResult guards `if Usage is Comp1/Comp2 skip scaling`). A pure-fixed sub-expression stays Int128. The 31-digit composite limit applies only to the non-float operands (rule 2b). COMP-5 stores in its exact native width and bounds by two's-complement (binary-wrap), not by digit count.

### MOVE numeric→numeric rescale across different scales/usages without going through arithmetic.

MOVE is value-preserving with receiver-scale alignment (ISO §14.9.25): load sender as CobolInt at its scale, TryStore into the receiver (rescale to receiver FractionScale — TRUNCATION rounding for a plain MOVE, never ROUNDED; high-order truncation per receiver Digits; unsigned receiver drops sign = stores magnitude, GR8). A scaled→integer MOVE drops the fraction (truncate). DISPLAY↔COMP↔COMP-3 differ only in external image/capacity, not value — the same TryStore covers all, then the receiver's encoder (overpunch for signed DISPLAY, packed for COMP-3) renders the image.

### P scaling (leading and trailing) in store, divide, and edited-format paths.

Leading P: adds to FractionScale (the implied point is left of stored digits) — value is scaled down; FractionScale getter already folds LeadingScaleDigits in. Trailing P: stored digits are multiples of 10^TrailingScaleDigits — ScaleAndRound must round to that 10^P grid (legacy clean ScaleAndRound: shift by +trailingP, round to integer units, multiply back). Port this into CobolInt.ScaleAndRound and into CobolEdit (legacy FormatByEditPattern divides absValue by 10^trailingP before building digits). Capacity counts only the explicit '9' digits, not the P positions.

### Comparison scale-alignment (IF A = B, A > B) across operands of different scale/usage/sign.

Comparisons are on algebraic value (ISO §8.8.4.2). Load both operands as CobolInt, Align to common scale, compare the Int128 unscaled values (sign included). A numeric-vs-numeric comparison NEVER goes through the receiver-truncation path (no capacity loss). A numeric-vs-alphanumeric/figurative comparison compares against the numeric value of the literal/ZERO. Equal magnitude with +0/−0 compares equal (signs of zero are the unique value 0).

### IS NUMERIC / IS NOT NUMERIC class condition on a DISPLAY field (and the sign-image validity).

For a typed numeric field the value is always a valid number, so IS NUMERIC is trivially true UNLESS the field can hold an externally-set image (REDEFINES over alphanumeric, file input, ACCEPT). For those the test validates the character image against the PIC: every position a digit, the sign position a valid overpunch/separate sign for the SignKind, no embedded spaces (legacy IsNumericClass logic, PicRuntime line ~2386: overpunch chars {A-I}/{J-R}/{/} accepted only at the overpunch position). Port that image-validation as CobolNum.IsNumericClass(image,profile) used only on the byte-boundary/redefines cases.

### ON SIZE ERROR semantics across multiple receivers and the initial-evaluation step (ISO rule 4, p595).

Two-phase per the spec: (a) evaluate the expression into the intermediate CobolInt — if SIZE ERROR here (Int128 overflow / exponentiation EC), NO receiver changes and the ON SIZE ERROR imperative runs. (b) store left-to-right into each receiver via TryStore — if a single receiver overflows, only THAT receiver is left unchanged and processing continues to the next receiver; ON SIZE ERROR runs if ANY store failed. Generated C# accumulates a `bool __sizeErr` across the per-receiver TryStore calls and branches once at the end.

## Edge cases

- Negative value MOVEd to an UNSIGNED receiver stores the magnitude (ISO §14.9.25 GR8) — TryStore returns the signed value, the receiver's encoder/storage drops the sign; for the unscaled-long storage this means the stored long is Math.Abs (but watch long.MinValue: use %Pow10(digits) before abs, as the legacy FormatUnsignedDisplay does).
- +0 and −0 are the unique value 0 for sign tests and comparison (SDIDI NOTE 1); an all-zero magnitude with a negative overpunch must still test as zero and as NOT negative.
- DISPLAY of a zero value in a signed trailing-overpunch field: last digit '0' positive → '{' (e.g. PIC S9(4) value 0 → "000{"), NOT "0000" — confirmed by legacy table.
- Numeric-edited zero with floating $ or Z across the whole field and no fixed 9 → entire field blanked to spaces (or '*' under check-protect); but if any fixed 9 exists the zeros print. BLANK WHEN ZERO overrides to all-spaces regardless.
- Asterisk check-protect (*): suppressed positions AND the comma/B insertion chars inside the floating zone become '*', but the decimal point stays '.' (legacy pass-2 logic).
- DECIMAL-POINT IS COMMA swaps the roles of '.' and ',' in both the picture interpretation and the edited output; CURRENCY SIGN replaces '$' as the currency symbol and CurrencyOutputChar may differ from the picture symbol.
- COMP-3 unsigned (PIC 9 COMP-3) must store sign nibble 0x0C (positive) and never retain a negative — the legacy EncodeComp3 fix: unsigned packed wrongly kept 0x0D and decoded negative.
- COMP-5 signed negative extreme (−2^(width−1), e.g. −32768 for 2 bytes) is IN range — a naive magnitude check mis-flags it; bound the signed two's-complement value, not the magnitude (legacy clean ExceedsBinaryCapacity).
- COMP-5 unsigned 8-byte range (long.Max, ulong.Max] requires `ulong` storage — a signed-long codec cannot hold it (legacy clean CobolNum explicitly calls this out).
- Exponentiation: 0**0 → EC-SIZE-EXPONENTIATION; negative base with non-integer exponent → EC-SIZE-EXPONENTIATION; negative base with integer exponent → defined (real) result; a base whose both-roots are returned uses the positive root (§8.8.1.2 rule 6).
- Divide by zero → EC-SIZE / SIZE ERROR, receiver unchanged (NOT a .NET DivideByZeroException — CobolInt.Div must guard b.Unscaled==0 and signal up).
- ROUNDED MODE PROHIBITED: an inexact result at the receiver scale raises SIZE ERROR and leaves the receiver UNCHANGED even though no overflow occurred (ISO §14.9.4) — TryStore checks inexactness before the capacity check.
- Trailing-P rounding grid: PIC 9(3)P value 1234 is stored as 1230 (multiple of 10^1) and capacity counts only the 3 nines; ScaleAndRound must round to the 10^P grid, not to scale 0.
- Composite-of-operands 31-digit limit is a COMPILE-time diagnostic for ADD/SUBTRACT/MULTIPLY/DIVIDE (not COMPUTE); exceeding it is a compile error, not a runtime SIZE ERROR.
- Mixed signed/unsigned and DISPLAY/COMP operands in one COMPUTE all reduce to algebraic CobolInt values — representation differences vanish in the intermediate; only the final receiver's representation matters.

## ISO citations

- §8.8.1.1 General — evaluation rules depend on the mode of arithmetic in effect
- §8.8.1.2 rule 7 — arithmetic expressions allow combining operations WITHOUT the composite-of-operands and receiving-item restrictions (so COMPUTE has no 31-digit limit; ADD/SUB/MUL/DIV statements do)
- §8.8.1.2 rule 6 — exponentiation rules (0**0 / negative base / both-roots → positive)
- §8.8.1.3 Native arithmetic — implementor-defined; in effect when ARITHMETIC IS NATIVE or no ARITHMETIC clause (the corpus default); implementor specifies the techniques (= Int128 fixed-point here)
- §8.8.1.5.2 Standard-decimal intermediate data item (SDIDI) — decimal128, 34 digits, exp ±6144, smallest 1.0E-6176 (the mode the lock cannot natively support)
- §8.8.1.5.4 — exponentiation integer powers expand to repeated multiply (operand-2 = 1,2,3,4 → operand-1, op*op, ...)
- §14.7.5 (rule 2, p595) — composite of operands ≤ 31 digits for ADD/DIVIDE/MULTIPLY/SUBTRACT under native arithmetic; when a float/intrinsic operand is present the limit applies to the OTHER operands only
- §14.7.5 rule 4 (p595-596) — two-phase arithmetic execution: initial evaluation into an intermediate data item (SIZE ERROR here → no receiver changed), then left-to-right store into each receiver (per-receiver SIZE ERROR leaves only that receiver unchanged)
- §14.9.4 / §11.9.6 (DEFAULT ROUNDED) / §11.9.11 (INTERMEDIATE ROUNDING) — the eight ROUNDED modes; ROUNDED MODE PROHIBITED raises SIZE ERROR on an inexact result and leaves the receiver unchanged
- §14.9.25 GR8 — a negative result stored into an unsigned receiver stores the magnitude (sign dropped)
- §6046 — fixed-point numeric literals 1 through 31 digits (the picture digit-count ceiling driving the long→Int128 boundary)
- §8.5.1.2 / §13.18.60 — the three capacity disciplines (digit-count for DISPLAY/COMP/BINARY, packed 2n−1 for COMP-3, native two's-complement width for COMP-5/BINARY-*)
- §8.8.4.2 — simple relation conditions compare algebraic values (comparison scale-alignment, +0=−0)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- ARITHMETIC IS STANDARD-DECIMAL (full ISO §8.8.1.5: decimal128, 34 sig digits, exp ±6144) is incompatible with the locked substrate AND with .NET `decimal` (96-bit/28-digit). v1 ships NATIVE arithmetic only (documented per §8.8.1.3). Delivering STANDARD-DECIMAL would require a decimal-floating type the lock forbids. OWNER DECISION NEEDED: (a) permanently scope COBOL.NET to native arithmetic (recommended — it is fully conformant as a default and is what the 364-NIST corpus uses), or (b) later add a quarantined decimal-float intermediate type usable ONLY when ARITHMETIC IS STANDARD-DECIMAL is in effect. STANDARD-BINARY is spec-obsolete and should stay unimplemented.
- DIV_GUARD_DIGITS value (proposed 14, reproducing the legacy decimal accumulator's headroom). Owner/empirical: confirm against the NIST division-rounding tests during G5 — a too-small guard loses rounding precision, too-large risks Int128 overflow on already-deep operands. This is tunable but should be locked once validated.
- The >38-digit ceiling: COBOL-2002/2014 permit pictures up to 31 (mandatory) and some profiles to 38; arithmetic intermediates on 19+-digit operands can exceed Int128. If the conformance target ever requires guaranteed-exact arithmetic beyond 38 digits, the carrier must widen (Int256 / a fixed 128-bit decimal). Confirm 38 digits is sufficient for the M2/M3/M4 conformance scope, else this becomes a substrate question.
- COBOLNET_ARCHITECTURE.md §3 data-model table currently says signed/scaled + COMP-3 → `decimal` — this CONTRADICTS the lock and PicInfo.ClrType (long-unscaled, no decimal). The table must be corrected to long/Int128-unscaled in the same change that lands this design, to avoid two SSOTs (flag, not a question — but needs an owner-visible doc edit).
