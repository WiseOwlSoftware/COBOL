# COBOL.NET — Numeric Model (native scaled-integer) (deep-dive design)

> **Status: LIVE / authoritative subsystem design** for the COBOL.NET rewrite (COBOL -> idiomatic
> typed-native C# via Roslyn; no byte substrate). The condensed cross-referenced view is
> `docs/COBOLNET_DESIGN.md` §6; THIS is the full design (decisions + rationale + C# mapping + hard
> problems + edge cases). The locked invariants and cross-cutting consistency live in the SSOT.

## Summary

Decision-complete design for COBOL.NET's native scaled-integer numeric model. SUBSTRATE (owner-locked, documented + hardened): every fixed-point datum is a native integer holding its UNSCALED value (all digits; decimal point = compile-time scale metadata). Storage CLR type by capacity: PIC ≤18 digits → `long`; 19–38 digits → `Int128` (value type); COMP-1/COMP-2 → `float`/`double`; COMP-5 / BINARY-CHAR family → `long`/`Int128` by digit tier, bounded to the native two's-complement byte width by binary-wrap. NO `decimal`, NO `BigInteger`.

THE CENTRAL HARDENING: the runtime's value engine (`src/Cobol.Net.Runtime/Numeric/CobolNum.cs`) is Int128-monomorphic, NOT long — a long-only engine would silently overflow real COMPUTE (e.g. `COMPUTE c = a * b` on two PIC 9(18) = 36 digits). The single intermediate carrier is an Int128 value + its compile-time scale (conceptually `CobolInt(Int128 Unscaled, int Scale)`). Storage stays the narrow native type; every operand widens long→Int128 at op entry, scales-align, computes in Int128, and a single `TryStore` rescales/rounds/truncates/bounds-checks back into the receiver's storage type. The 'Int128 escape boundary' is reached only when a single product of two ≥19-digit operands exceeds Int128 (~38 digits) → EC-SIZE-OVERFLOW.

INTERMEDIATE PRECISION (ISO §8.8.1, mined from the proven legacy `decimal` path): arithmetic operates on the algebraic VALUE (§8.8.1.2). Per-operator result scale: ADD/SUBTRACT → max(scales); MULTIPLY → sum(scales); DIVIDE/COMPUTE-division → a guard scale = max(receiver-scales, operand-scales) + DIV_GUARD_DIGITS (DIV_GUARD_DIGITS=14, reproducing the legacy decimal accumulator's ~28-sig-digit headroom — the one policy `decimal` auto-picked that I must make explicit). EXPONENTIATION: native = **EXACT `Int128` repeated multiplication while the result fits the carrier, and the §8.8.1.2 implementor-defined double approximation past it** (⚖ OWNER DECISION 2026-08-03, fix-queue PB18 — `CobolIntrinsics.PowNativeIntDec`, ONE arm on the SDIDI carrier since kb/Work PB69: exact Int128 significand while it fits, the double approximation past it, an Int128 landing that cannot hold it raising EC-SIZE-OVERFLOW; see D19); standard modes = `CobolDec.Pow` per §8.8.1.5.4 (integer powers by repeated SDIDI multiply — r2a–d exactly). Statement-arithmetic enforces the 31-digit composite-of-operands limit (§ rule 2, p595); COMPUTE expressions have NO composite limit (§8.8.1.2 rule 7) — Int128 is the cap, SIZE ERROR past ~38 digits. Default mode = NATIVE arithmetic (§8.8.1.3, implementor-defined = Int128 fixed-point); STANDARD (2002) / STANDARD-DECIMAL (2014, decimal128) are implemented via the `CobolDec` SDIDI, and STANDARD-BINARY is documented-unsupported (spec-obsolete).

ALL USAGES with capacity/truncation: DISPLAY/COMP/COMP-4/BINARY → DigitCount discipline (PIC 99 COMP holds 0–99 not 0–32767); COMP-3/PACKED → 2n−1 packed-digit capacity; COMP-5/BINARY-CHAR…DOUBLE → native two's-complement width (PIC S9(4) COMP-5 = −32768..32767, PIC 9(4) COMP-5 = 0..65535); COMP-1/COMP-2 → IEEE, bypass the scaled engine. The 8 ROUNDED modes are the `CobolRounding` enum; `Store` is the no-SIZE-ERROR (silent-truncation) branch and `TryStore` (bool, receiver-unchanged-on-overflow, PROHIBITED-inexact → SIZE ERROR) is the ON SIZE ERROR branch.

NUMERIC-EDITED formatting: PORT the proven two-pass legacy `PicRuntime.FormatByEditPattern`/`FormatNumericEdited` verbatim (Z * $ + - CR DB B 0 / . , fixed+floating insertion, asterisk fill, BLANK WHEN ZERO, full-field-blank) into a `CobolEdit.Format(CobolInt, EditPattern, env)` runtime helper; the receiver field is a C# `string`. SIGN OVERPUNCH (NIST-exact, confirmed against legacy NIST-passing tests): IBM-ASCII tables positive 0→'{',1→'A'…9→'I'; negative 0→'}',1→'J'…9→'R'. Default = TrailingOverpunch; SignKind ∈ {LeadingOverpunch, TrailingOverpunch, LeadingSeparate('+'/'-'), TrailingSeparate}. NumProfile's `SignKind` field is the `NumericSign` enum (`src/Cobol.Net.Runtime/Numeric/NumProfile.cs`), incl. a fifth member `BinaryMinus` for binary/packed DISPLAY images. P scaling, mixed float/fixed, IS NUMERIC, comparison scale-align, MOVE rescale all specified below.

## Decisions

### D1. Runtime value engine is Int128-monomorphic via a `readonly record struct CobolInt(Int128 Unscaled, int Scale)`; storage stays the narrow native type (long/Int128/native-int/float/double).

**Rationale.** Native `long` overflows real COMPUTE: a long-only engine caps Pow10 at 10^18 and does `num *= Pow10(exp)` / `Rescale` on `long`, so `COMPUTE c = a*b` with two PIC 9(18) operands (36 digits) silently overflows. Int128 holds 38 digits — covers every legal COBOL fixed-point picture (max 31 digits ISO §8.3.3.3.2, 38 in some 2002 profiles) AND their sum/product intermediates within the 31-digit composite limit. Int128 is a hardware-adjacent value type (two longs, no GC, no allocation) — orders cheaper than BigInteger and exactly the 'fixed-size Int128 escape hatch' the architecture names.

**Rejected alternatives.** (a) Keep long-only — REJECTED: silently wrong on common multiply/divide; the whole point of the rewrite is correctness. (b) decimal/BigInteger intermediates — REJECTED: owner-locked out (decimal is software 96-bit/28-digit and can't even hold standard-decimal's 34; BigInteger allocates). (c) Generic `INumber<T>` arithmetic monomorphized per storage width — REJECTED: codegen + JIT-bloat complexity, unreadable generated C#, and storage-width-typed math reintroduces the very overflow we're eliminating. One Int128 path is the singular pattern.

> **Realization.** The engine is Int128-monomorphic, and the carrier is realized as **Int128-TYPED C# expressions
> + the COMPILE-TIME scale the renderer already owns (`NumX(Expr, Scale)`) + monomorphic `CobolNum` kernels taking
> `(Int128 value, int scale)`** — i.e. `CobolInt` with its `Scale` field resolved statically. Rationale: the
> renderer computes every scale at compile time and emits them as constants; a runtime `Scale` field would box
> static knowledge into per-value state and add construction ceremony to every generated expression for no
> semantic gain. The kernel surface (`Rescale`, `Store`, `TryStore`, `Divide`, `DivideOrThrow`, `MulChecked`,
> `FormatDisplay*`, `ParseDisplay`, `FromAlphanumeric`) IS this section's Align/Add/Sub/Mul/Div engine, in Int128.
> Every emitted operation forces wide math (`(Int128)(a) op (b)`); a ≤18-digit receiver stores through one
> width-aware `(long)` cast at the store site (`ArithmeticEmitter.Narrow` — Int128 storage for 19+ digits uses the
> wide tier). The >38-digit single-product ESCAPE raises OverflowException via checked `MulChecked` in ON SIZE
> ERROR contexts (full EC-SIZE-OVERFLOW mapping with the EC model).

### D2. DIVIDE/COMPUTE-division quotient is computed at an explicit guard scale = max(all receiver fraction-scales, all operand scales) + DIV_GUARD_DIGITS, DIV_GUARD_DIGITS = 14; the final per-receiver store rescales+rounds to the receiver's scale.

**Rationale.** The proven legacy lowerer (ArithmeticLowerer.LowerDivide) computes division into a `decimal` accumulator (≈28-29 significant digits) and only rescales to the receiver on the final IrMoveAccumulatedToTarget — so its division 'intermediate scale' is decimal's natural headroom, the ONE scale `decimal` auto-picked that Int128 forces me to make explicit. 14 guard digits past the deepest receiver/operand scale reproduces that headroom while staying inside Int128's 38 digits for realistic operand sizes; rounding then happens exactly once, at the receiver, honoring the ROUNDED mode (ISO §14.7 — ROUNDED applies to the transfer into the resultant, NOTE 1 p595).

> **Realization.** `NumericRenderer.Divide` keeps the exact single-rounding path for the OUTERMOST division (no
> guard needed — `RoundDiv` rounds on the true integer remainder) and applies `DivGuardDigits = 14` to the
> NESTED/higher-precision case, CLAMPED so the radix-alignment exponent keeps an 18-digit dividend inside the wide
> engine (exponent ≤ 20 ⇒ ≤ 38 digits). The guard reproduces the legacy-decimal headroom the goldens encode.

**Rejected alternatives.** (a) Quotient at receiver scale directly (legacy IrPicDivide degenerate path) — REJECTED for the general case: rounds before the receiver knows its scale, loses guard digits, fails NIST division-rounding tests. (b) Unbounded rational/exact division — REJECTED: COBOL division is inherently lossy at a finite scale; exactness is undefined for non-terminating quotients. (c) decimal accumulator like legacy — REJECTED: locked out + caps at 28 digits anyway.

**ROUNDED phrase & size-error wiring.** The ROUNDED phrase (§14.7.4) is wired per-receiver through `CobolNum.Store(value, scale, profile, mode)`; the eight modes resolve in the binder (no phrase → Truncation, MODE IS x → the named mode, bare ROUNDED → the program's DEFAULT ROUNDED mode from the OPTIONS paragraph — `OptionsModel.DefaultRounding`, resolved in `ExpressionBinder`, ISO §11.9.6, NearestAwayFromZero when absent). For a **single (outermost) division feeding a receiver** — the DIVIDE statement and a COMPUTE whose working scale equals the receiver scale — the quotient is computed *directly at the receiver scale with the receiver's mode* via `CobolNum.Divide`→`RoundDiv`, which is **exact** because `RoundDiv` rounds on the true integer remainder (it sees all lost precision), so no guard digits are needed. A division **nested inside a larger expression** (where intermediate precision must survive further operations) uses the D2 guard-scale model (compute the quotient at `max(scales)+DIV_GUARD_DIGITS`, then round once at the receiver), overflow-safe under the Int128 carrier. `TryStore` (D6) is emitted whenever an ON SIZE ERROR phrase is present — it returns false (receiver unchanged) on a high-order capacity overflow or a PROHIBITED-inexact rescale, so `ROUNDED MODE IS PROHIBITED` on an inexact result raises the size error (a division's PROHIBITED-inexactness is caught in `DivideOrThrow` from the exact remainder, since the division rounds at the receiver scale). Two-phase per §14.7.7 rule 4: a `try`/`catch (CobolSizeError | OverflowException)` wraps the per-receiver stores; `DivideOrThrow` (zero divisor) and `MulChecked` (intermediate overflow, §14.7.5 case 5) are emitted ONLY in that checked context, so a statement WITHOUT the phrase keeps the unchecked `Divide`/`*`/`Store` path.

### D3. Default arithmetic mode = NATIVE (§8.8.1.3); the Int128 fixed-point engine IS the documented implementor-defined native technique. ARITHMETIC IS STANDARD-DECIMAL is implemented (the SDIDI via `CobolDec`); STANDARD-BINARY is documented-unsupported (spec-obsolete).

> **Arithmetic modes (per edition, end to end):**
> - **NATIVE** (the default): the documented implementor-defined technique IS the exact Int128 fixed-point engine
>   (D1/D2) — per §8.8.1.3 fully conformant.
> - **STANDARD-DECIMAL — implemented** (`Cobol.Net.Runtime/Values/Numeric/CobolDec.cs`): the SDIDI (§8.8.1.5.2)
>   as `readonly record struct CobolDec(Int128 Sig, int Exp)` — every operation computes EXACTLY (a 256-bit
>   hi:lo-UInt128 scratch for products/quotients; shift-subtract 256÷128 division, clarity-first until profiled)
>   and rounds ONCE per op to 34 significant digits with the INTERMEDIATE ROUNDING mode (§11.9.11: default
>   NEAREST-AWAY-FROM-ZERO; NEAREST-EVEN; TRUNCATION; PROHIBITED ⇒ EC-SIZE-TRUNCATION, surfaced as the size
>   error until the EC model), then range-checks at the decimal128 bounds (§8.8.1.5.2 r2 — adjusted exponent
>   > +6144 ⇒ EC-SIZE-OVERFLOW; below the 10⁻⁶¹⁷⁶ subnormal quantum the value re-rounds onto it, and a nonzero
>   value that rounds to zero there ⇒ EC-SIZE-UNDERFLOW; the ONE `Clamp` in the `Round34Wide` funnel).
>   Fixed-point operands lift EXACTLY (≤31 digits ≤ 34); FLOAT operands convert via `CobolDec.FromDouble` —
>   the §8.8.1.5.1 implementor-defined conversion, defined as the SHORTEST round-trip decimal identity of the
>   IEEE value (≤17 digits ⇒ always exact in the significand; Infinity ⇒ EC-SIZE-OVERFLOW, NaN ⇒ the
>   invalid-operation EC-DATA-INCOMPATIBLE). The renderer routes `Combine`/`Power`/comparisons through
>   `CobolDec` when the mode is in effect (`NumX.Dec`; the mode branch runs BEFORE the D16 float branch);
>   **exponentiation** is `CobolDec.Pow` (§8.8.1.5.4: integer exponents by square-and-multiply over `Mul` —
>   exactly r2a–r2d for 1–4, the r2e implementor-defined form beyond, r3's `1/(b**|e|)` for negatives, the
>   §8.8.1.2 r6 / r4 EC-SIZE-EXPONENTIATION legs; a non-integer exponent is the r2e double approximation
>   converted through `FromDouble`). The receiver's ROUNDED applies only at the final transfer via the
>   `CobolNum.Store/TryStore(CobolDec, …)` overloads (§14.7 NOTE 1). The final transfer has an UNCHECKED and a
>   CHECKED form (PB74, 2026-08-17): `CobolDec.ToUnscaled` (MOVE / no-phrase store) keeps only the low-order
>   digits of a magnitude past the Int128 carrier — the same high-order truncation the store then applies —
>   while `ToUnscaledChecked` (ridden by `TryStore(CobolDec)` and the emitter's checked numeric-edited
>   transfer) raises `CobolSizeError` EC-SIZE-TRUNCATION for it (§14.7.5 case 3 / no-phrase rule 4), so
>   `10 ** 100 ON SIZE ERROR` fires and leaves the receiver unchanged. Every PROHIBITED raise carries its
>   level-3 name — EC-SIZE-TRUNCATION per §14.7.4.3 r7 (the transfer) and §11.9.11.2 r3d (the intermediate) —
>   so an EXCEPTION-STATUS / USE / PERFORM-WHEN selection sees the right condition. An Int128 significand represents every
>   decimal128 value's 34-digit significand exactly; only the rejection of `decimal` (96-bit/28-digit) stands.
> - **Intrinsic functions under the standard modes (§15.4.1 r1):** a function WITH an equivalent arithmetic
>   expression must return exactly the SDIDI-evaluated EAE value. The exact-Int128 family (MOD/REM/MAX/MIN/
>   RANGE/SUM/MEDIAN/MIDRANGE/ABS/SIGN/INTEGER…) satisfies it **for an argument list that is entirely
>   fixed-point** — every EAE step is exact in both engines
>   (the documented-equivalence header of `CobolIntrinsics.Exact.cs`; the one recorded residue: an exact result
>   past 34 digits — FACTORIAL 31–33, a >34-digit SUM chain — keeps MORE precision than the per-op-rounded
>   SDIDI would).
>   ⛔ **THIS PARAGRAPH READ "already satisfies it", UNQUALIFIED, AND THAT WAS FALSE IN THREE MEASURED WAYS
>   (fix-queue PB32, 2026-08-03).** It is recorded here rather than quietly reworded because the claim is what
>   made the family look reviewed:
>   · **A `Dec`-carried argument did not COMPILE.** `NumX` has three carriers — exact scaled `Int128`, the
>     `CobolDec` SDIDI, binary64 — and `IntrinsicRenderer` handled two, so a §15.3 type-10 arithmetic-expression
>     argument (legal at 2014/2023) reached `MaxScaled(params Int128[])` as a raw `CobolDec` and Roslyn reported
>     `CS1503` on conforming source. **Now landed at the ONE choke point, `IntrinsicRenderer.Arg`**, through the
>     same `WorkingScale(floor)` discipline the NUMVAL and float families use.
>   · **A float argument still demotes the whole list to binary64** — ✅ **CLOSED IN TWO STAGES**: PB38 landed
>     the mode-before-float ordering at the ONE landing, and **PB56 (2026-08-09) landed the Dec-carrier body
>     itself** — `CobolIntrinsics.Dec.cs`, dispatched by `IntrinsicRenderer.RenderDec`: under a standard mode a
>     Dec/float-bearing argument list evaluates its §15.4.1 r1 equivalent arithmetic expression ON the SDIDI
>     (arguments lifted exactly per §8.8.1.5.1/.2 r1, NEVER quantized) and lands at the receiver once. An
>     all-fixed-point list stays on the exact Int128 family (the documented equivalence above). The same change
>     removed the COBOLNET0899 stage: ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION now evaluate
>     their inexact-division EAEs in SDIDI form (STANDARD-DEVIATION's root is the §15.4.1-last-¶ prose
>     approximation, converted in per §8.8.1.5.1), and the prose family's binary64 results CONVERT in
>     (`CobolDec.FromDouble`) instead of truncating to unscaled at a receiver-derived working scale. Pinned by
>     `pb56_dec_carrier_intrinsics` (golden) + `CobolIntrinsicsDecTests` (bodies + the exact-carrier agreement
>     theory that keeps the all-fixed routing honest).
>   · **MEDIAN and MIDRANGE wrapped on the EXACT path**, at one fifth the magnitude of MAX/MIN/SUM/RANGE,
>     because returning at scale common+1 spends a decimal digit of carrier headroom that its siblings do not.
>     **Now `EC-SIZE-OVERFLOW`** per the substrate paragraph's escape-boundary policy, which the code had simply
>     never implemented (`CobolIntrinsics.ExactMul`).
>   · **SUM and RANGE wrapped the same way and were NOT reached by that pass** (PB252): `SumScaled` was
>     `Int128 s = 0; foreach (var x in xs) s += x;` and `RangeScaled` was `MaxScaled(xs) - MinScaled(xs)`, both
>     in an unchecked context, while the per-argument `RescaleEscape` bounds each ALIGNED ARGUMENT and says
>     nothing about their sum. The helpers are now named for the OPERATION they guard — `ExactAdd` / `ExactSub` /
>     `ExactMul` over the one `SizeEscape` — rather than for MEDIAN's halving, which is what made them look
>     inapplicable to their siblings; `ExactCarrierBoundaryDriftTests` now closes the set by reflection.
>   MEAN's one inexact division evaluates in SDIDI form (IntrinsicRenderer), making the spec's
>   §15.4.1 NOTE-2 relation `FUNCTION MEAN(a b c) = (a+b+c)/3` TRUE. The prose-approximation family (SQRT/trig/
>   log/E/PI) has no EAE, so its VALUE is implementor-defined in every mode (§15.4.1 last ¶) — but never its
>   CONTAINER: under a standard mode the returned value **is itself contained in an SDIDI** (§15.4.1: "the
>   returned value for numeric and integer functions is contained in a temporary standard data item in the
>   intermediate form defined for the arithmetic mode in effect"; §8.8.1.5.1 makes the mode "a method of
>   evaluating an arithmetic expression, an arithmetic statement, the SUM clause, **and certain integer and
>   numeric functions as specified in 15.4.1**"), the binary64 approximation entering it through the §8.8.1.5.1
>   conversion. ⛔ **kb/Work PB253 (2026-09-05): this paragraph used to read "converting into expressions per
>   §8.8.1.5.2 r1", which describes only the OPERAND case and is what let the container rule be read as
>   receiver-conditional.** `IntrinsicRenderer.RenderFloat` had duly tested the receiver SHAPE first, so under
>   `ARITHMETIC IS STANDARD-DECIMAL` (and 2002's plain `STANDARD`) the SDIDI arm was unreachable for every
>   receiver-less or float-receiver reference and the raw binary64 escaped into the item-92 text, MOVE-source,
>   float-receiver and relation channels: `DISPLAY FUNCTION SIN(1E-20)` printed `1E-20` where the SDIDI's text
>   is `0.00000000000000000001`, and `MOVE FUNCTION TAN(a)` / `COMPUTE r = FUNCTION TAN(a)` returned
>   16331239353195368.96 and 16331239353195370.00 for ONE call — which §15.4.1's "the returned value is the same
>   for all instances of a given function within a single execution of the runtime element" forbids outright.
>   **The mode-before-D16 ordering stated above therefore binds `RenderFloat` exactly as it binds
>   `CombineCore`, `Power` and `Landed`**; the residue it governs is the family with neither an EAE nor a
>   `RenderDec` body — ACOS/ASIN/ATAN/COS/SIN/TAN/LOG/LOG10/RANDOM. Pinned by `pb253_prose_float_standard_container`
>   (2002) and `pb253_prose_float_sdidi_container` (2014 + 2023).
>   (ANNUITY / PRESENT-VALUE / VARIANCE / STANDARD-DEVIATION were staged loud under the standard
>   modes — COBOLNET0899 — until their SDIDI evaluations landed with PB56; the stage is gone.)
>   **The NUMVAL family under the standard modes (PB60, RV-15.67.4-1a / RV-15.69.4-3, 2026-08-17):** NUMVAL /
>   NUMVAL-C / NUMVAL-F have no EAE, but their returned value is FIXED by their own rules — §15.67.4 r1 and
>   §15.68.4 r1 "the numeric value represented by argument-1", and §15.69.4 r3 says it outright for NUMVAL-F
>   under standard-decimal (r2 grants NATIVE and standard-binary the approximation) — and §15.4.1 places it in
>   an SDIDI temporary. So the standard-mode value is the ONE positional scan's (sign, unscaled, frac[, exp])
>   lifted to `CobolDec` EXACTLY at the parsed scale (`CobolIntrinsics.NumvalDec` / `NumvalCDec` / `NumvalFDec`
>   — projections of the same `NvScan`/`NvfScan` the native twins and the TEST- validators ride, sharing the
>   native twins' reject projections), dispatched by `IntrinsicRenderer.RenderDec`'s NUMVAL arm, which routes
>   UNCONDITIONALLY under the standard modes (the argument is a string — no carrier question arises). No working
>   scale, no receiver, no ≥6/≥9 floor: `NUMVAL("1.2345678")` is 1.2345678 in every channel, a 34-digit argument
>   (legal under the standard-mode digit cap) is exact, and NUMVAL-F's E-exponent lifts through
>   `CobolDec.FromParsed` — the ONE §8.8.1.5.2 r2 range check every SDIDI result gets (`1E+9999` ⇒
>   EC-SIZE-OVERFLOW). The NATIVE-mode value stays the documented item-92 working-scale determination
>   (CONFORMANCE.md). Pinned by `pb60_numval_standard_decimal` + the NUMVAL tests in `CobolIntrinsicsDecTests`
>   (incl. the native-agreement theory on the shared fixed-point domain).
> - **The RW SUM clause (§8.8.1.5.1 names it):** documented-equivalence consumption at the ReportWriterEmitter
>   chokepoint — each §13.18.54 GR3 accumulation is one ≤32-digit fixed-point addition, exact and digit-identical
>   in both engines; no routing needed.
> - **STANDARD-BINARY — documented-unsupported** (COBOLNET0806 at every edition that has it): spec-obsolete
>   (§8.8.1.4.1 NOTE 1); a binary128 SDIDI has no exact .NET carrier; revisit only if the spec retains it. Its
>   2014 introduction edge is the pass's `arithmetic-standard-binary-2014` row (0900 below 2014; pending —
>   no compiling cell).
> - **Plain `ARITHMETIC IS STANDARD`** (the 2002 mode — the 2002 OPTIONS paragraph's NATIVE|STANDARD clause):
>   obsolete 2014, dropped by ISO 2023 (Annex E.2 item 21; §8.8.1.2 names only the three). Implemented as the
>   same CobolDec SDIDI engine (the standard intermediate for its reachable operands IS the decimal form); the
>   `arithmetic-standard-2002` dual-window row emits 0900 below 2002, the 0903 obsolete flag at 2014, and 0807
>   at 2023 (error strict / warning permissive).
> - **Edition gates:** the OPTIONS paragraph is 2002+ (COBOLNET0804, `options-paragraph-2002` — with ARITHMETIC
>   NATIVE|STANDARD); the 2014-only clauses each carry their own 0900 row (`options-default-rounded-2014`,
>   `options-intermediate-rounding-2014`, `options-entry-convention-2014` [conservative — the in-repo 85/2002
>   evidence establishes only ARITHMETIC at 2002], `options-float-binary/decimal-2014`, `options-initialize-2014`,
>   and the STANDARD-BINARY/STANDARD-DECIMAL keyword rows); ROUNDED MODE IS is 2014+ (COBOLNET0803); the
>   composite-of-operands check is 31 at EVERY edition (§14.7.7 rule 2 — an 85-specific tightening to 18 is
>   refuted by CCVS-85 itself: NC101A composes 21 digits in a MULTIPLY).

**Rationale.** §8.8.1.3 lets the implementor define native arithmetic and it is the default when no ARITHMETIC clause is present — which is the entire NIST/conformance corpus. STANDARD-DECIMAL requires a decimal128 (34-digit, exp ±6144) decimal-floating type per §8.8.1.5.2 / ISO 60559:2020 — modeled exactly by an Int128 significand (`CobolDec`), never .NET `decimal` (96-bit/28-digit). STANDARD-BINARY is marked obsolete by the spec itself (NOTE, §8.8.1.4 / p9086). Native = Int128 fixed-point is fully conformant and reproduces the NIST-passing legacy behavior.

**Rejected alternatives.** (a) Implement STANDARD-DECIMAL via .NET `decimal`/`BigInteger` — REJECTED: the lock forbids both; STANDARD-DECIMAL is instead the exact Int128-significand SDIDI (`CobolDec`). (b) Claim full ISO arithmetic-mode conformance without the modes — REJECTED: would be an uncited spec-compat claim; NATIVE + STANDARD-DECIMAL is the honest, conformant surface.

### D4. Port the legacy two-pass NUMERIC-EDITED formatter (PicRuntime.FormatByEditPattern + FormatNumericEdited) verbatim into a value-level `CobolEdit.Format(CobolInt value, EditPattern pat, PicEnvironment env) → string`; the edited field's CLR storage is `string`.

**Rationale.** NUMERIC-EDITED is a famously fiddly subsystem (fixed vs floating $ + -, asterisk check-protect, BLANK WHEN ZERO, full-field-blank-on-zero, comma/B suppression inside floating zones, CR/DB, DECIMAL-POINT IS COMMA, CURRENCY SIGN). The legacy engine passes 364 NIST tests, so it is the proven PORTING SOURCE — re-deriving from scratch risks regressions the conformance corpus already covers. AUTHORITY NOTE (process rule #1): the ISO spec (§13.18.40 PICTURE editing rules), not the legacy oracle, defines correctness — validate the ported formatter clause-by-clause against the spec, resolve any legacy↔spec discrepancy to the SPEC (dialect-gated when the behavior is edition-varying), and treat NIST as a regression net that VERIFIES, never scopes. It needs no byte buffer: it produces a C# string directly, which is exactly the edited item's native representation (PicCategory.NumericEdited → `string`).

**Rejected alternatives.** (a) Rewrite the editor from the ISO §14.9.x grammar — REJECTED: high regression risk against a battle-tested oracle; the task explicitly says mine the legacy for behavior. (b) Use .NET ToString format strings — REJECTED: cannot express floating insertion, check-protect, overpunch, or COBOL's zero-suppression rules. (c) Keep editing in a byte buffer — REJECTED: no byte substrate; the value→string transform is pure.

### D5. NumProfile carries a `SignKind` field — the enum `NumericSign` (LeadingOverpunch / TrailingOverpunch[default] / LeadingSeparate / TrailingSeparate / BinaryMinus) — reproducing the IBM-ASCII overpunch tables: positive {ABCDEFGHI for 0-9, negative }JKLMNOPQR.

**Rationale.** Confirmed NIST-exact against the legacy NIST-passing integration tests: PIC S9(3) +42→"04B", -42→"04K", -150→"0015}", +150→"0015{", SIGN LEADING -37→"}37". A signed-DISPLAY item's character image (what DISPLAY emits and what file records carry) is determined entirely by SignKind — a bare `Signed` bool cannot reproduce the image, so `NumProfile` carries the `NumericSign SignKind` field. The default with no SIGN clause is TRAILING overpunch.

**Rejected alternatives.** (a) Keep only `Signed` bool — REJECTED: cannot produce the overpunched DISPLAY image or SIGN SEPARATE; would fail any signed-display NIST test. (b) Store sign as a separate C# `bool isNegative` field beside magnitude — REJECTED: the unscaled Int128/long already carries the sign natively; the SignKind affects only the EXTERNAL image (DISPLAY/serialization), so it belongs in formatting metadata, not a parallel storage field.

### D6. Harden `Store` → `TryStore`: returns bool (false = ON SIZE ERROR), leaves receiver unchanged on overflow, raises SIZE ERROR for ROUNDED MODE PROHIBITED when the result is inexact at the receiver scale; capacity check is discipline-specific (DigitCount / PackedDecimal 2n−1 / BinaryCapacity two's-complement-by-width).

**Rationale.** The current new CobolNum.Store silently `%= Pow10(Digits)` truncates with no SIZE ERROR — that is only the no-ON-SIZE-ERROR branch. The legacy clean CobolNum.TryStore is the proven correct shape (ISO §14.7.5: on SIZE ERROR the receiver is unmodified and the imperative clause runs). The three capacity disciplines are already correctly modeled by NumericTruncation; TryStore must consult them. COMP-5 unsigned-8-byte (0..ulong.Max) exceeds `long`, so its storage type is `Int128` (the monomorphic wide engine carries the full range) and the capacity check branches on signed-vs-unsigned width.

**Rejected alternatives.** (a) Throw on overflow — REJECTED: SIZE ERROR is a recoverable COBOL condition with an imperative handler, not an exception; throwing would skip the receiver-unchanged rule and the NOT ON SIZE ERROR path. (b) Always truncate silently — REJECTED: wrong when ON SIZE ERROR is present and wrong for PROHIBITED.

> **BinaryCapacity enforcement.** The BinaryCapacity leg is enforced in BOTH `Store`
> (WRAP by native two's-complement width: the deterministic no-ON-SIZE-ERROR truncation, the width analog of
> high-order digit truncation) and `TryStore` (range check → SIZE ERROR when the value leaves the byte-width
> range), keyed off `NumProfile.StorageLength` and branching signed vs unsigned (`WrapBinary` / `InBinaryRange`,
> §14.9.25.4 GR6.d.2.b for the unsigned magnitude rule). The unsigned 8-byte range (0..2^64−1) is carried by the
> **Int128** substrate rather than a native `ulong` — the monomorphic wide engine already represents it, so no
> separate storage type is needed (COMP-5 and BINARY-DOUBLE both stay long/Int128 by digit tier). The
> **BINARY-CHAR family** (USAGE BINARY-CHAR/-SHORT/-LONG/-DOUBLE, ISO §13.18.60.4 GR12) rides this exact leg:
> `PicInfo.BinaryItem` synthesizes a PICTURE-less Numeric item with the fixed byte width (1/2/4/8 → StorageWidth),
> BinaryCapacity truncation, SIGNED default / UNSIGNED, and an implied DISPLAY digit count = the range's
> max-magnitude decimal width (CHAR 3 / SHORT 5 / LONG 10 / DOUBLE 19 signed · 20 unsigned — the spec gives no
> implied PICTURE, GR21, so this is the documented implementor choice). PICTURE is prohibited on the family
> (§13.16.3 SR8 → COBOLNET0870).

### D7. COMP-1/COMP-2 (and mixed float/fixed expressions) bypass the scaled-integer engine: the float operand promotes the whole sub-expression to `double`, computed in IEEE, and the final store to a fixed-point receiver converts double→CobolInt at the receiver scale (round per mode).

**Rationale.** ISO §8.8.1: a floating operand makes the result floating. The legacy MoveNumericToNumeric / StoreArithmeticResult explicitly guard `if (Usage is Comp1 or Comp2) skip fixed-point scaling` and store the IEEE value directly. COMP-1=float, COMP-2=double are hardware IEEE (no PIC truncation). The 31-digit composite rule excludes float operands (§ rule 2b p595: when any operand is float, the limit applies to the OTHER operands only).

**Rejected alternatives.** (a) Force floats through the Int128 scaled path — REJECTED: float values aren't base-10 fixed-point; scaling them is meaningless and lossy. (b) Promote everything to decimal when a float is present — REJECTED: locked out + wrong (COBOL float arithmetic is IEEE binary, not decimal).

### D17. NumProfile carries a REQUIRED `ByteForm` field — the enum `NumericByteForm` (None / Zoned / Binary / Packed / PackedNoSign) — because the CAPACITY discipline can never imply the BYTE REPRESENTATION: USAGE DISPLAY and USAGE BINARY are both `DigitCount`.

*Landed (V59 step 2). Load-bearing spec: §13.18.60.4 GR4 (BINARY "a radix of 2 is used"), GR7 (DISPLAY, an alphanumeric coded character set aligned on a character boundary), GR11 (PACKED-DECIMAL "a radix of 10 … the minimum possible configuration", and the 2023 WITH NO SIGN phrase), GR12 (the fixed-width binary usages); §4.2.16 + Annex A.1 items 205/215 (documenting BINARY's and PACKED-DECIMAL's representation is a REQUIRED implementor-documentation item).*

**Rationale.** A usage decides THREE orthogonal facts — capacity (`Truncation`), byte representation (`ByteForm`), sign presentation (`SignKind`) — and the profile carried only two of them. Because the two it carried *looked* sufficient, the record-image codec derived the representation from the digit count, so a `PIC 9(4) COMP` and a `PIC 9(4) COMP-3` both reached a file as the four ASCII bytes `31 32 33 34` while `FUNCTION BYTE-LENGTH` reported 2 and 3 (V59; §15.14.4 r1 vs §15.50.4 r3 cannot disagree in a single-byte-character model). The forms are implementor-defined and therefore OURS to pin: `Binary` = two's complement, BIG-ENDIAN, `StorageLength` bytes; `Packed` = BCD two digits per byte, trailing sign nibble `0xC`/`0xD` (`0xF` for an unsigned item); `PackedNoSign` = digit nibbles only; `Zoned` = one byte per digit position, sign per `SignKind`. Big-endian and the `0xF` unsigned nibble follow the IBM / Micro Focus / GnuCOBOL survey — the latter deliberately diverging from our own legacy `PicRuntime.EncodeComp3`, which writes `0x0C` for any positive value. `NumericByteFormDriftTests` pins the whole `Usage` table so a new usage cannot inherit a representation nobody chose.

**The codec.** `CobolNum.Image.cs` (`FormatImage`/`ParseImage`, beside `FormatDisplay`) is the ONE place a value becomes bytes and back — whole-group image, file record, SORT key window, Tier-B REDEFINES backing all go through it, and the bytes ride the SAME Latin-1 `string` carrier as a DISPLAY leaf (`RecordFraming`), so no second whole-group mechanism appears. `None` at a byte boundary THROWS (a compiler invariant break, not a COBOL condition). Decoding is tolerant in the standard's own direction: a non-decimal packed nibble contributes no digit and a foreign sign nibble reads by the universal rule (`0xB`/`0xD` negative, everything else positive), matching how the zoned decoder treats incompatible data (§14.6.13.2, undefined → deterministic).

**The width ladder must be SUFFICIENT, not merely conventional.** §13.18.60.4 GR4 closes with "Sufficient computer storage shall be allocated by the implementor to contain the maximum range of values implied by the associated decimal picture character-string." `1-2-4-8` is exactly right through 18 digits and then stops; a 19–38-digit picture (legal at COBOL-2002+, already stored as `Int128`) takes a **16-byte tier**, because a signed 19-digit maximum 10^19−1 exceeds 2^63−1. Sign-independent, per GR12's SIGNED/UNSIGNED-same-width precedent. Before this, `FUNCTION BYTE-LENGTH` answered 8 for `PIC S9(31) COMP`.

**Rejected alternatives.** (a) Derive the form from `Truncation` — REJECTED: that IS the defect (DISPLAY and BINARY share `DigitCount`). (b) Derive "has a packed sign nibble" from `StorageLength` — REJECTED: the widths COLLIDE at odd digit counts (3 digits is 2 bytes with or without the nibble), so an odd-digit unsigned item's last digit would decode as a sign; `PackedNoSign` is its own member. (c) Default the field to `Zoned` instead of `required` + `None = 0` — REJECTED: an unstated form would silently claim one byte per digit, which is the same substitution bug one level down; `None` makes a codec handed a form-less profile (USAGE INDEX, §13.18.60.4 GR10) fail loud. (d) Carry the form on a separate image-only struct — REJECTED: the record-image codec already threads the leaf's `NumProfile`, and a second carrier for one concept is the §4.1 incoherence trap.

### D16. The floating-point USAGE TRIO (FLOAT-SHORT / FLOAT-LONG / FLOAT-EXTENDED) extends D7 to the full implementor-defined float facility (Phase 6a); the pinned IEEE family (FLOAT-BINARY-*/FLOAT-DECIMAL-*) is a separate leg (Phase 6b, stays loud).

*Implemented (Phase 6a). Load-bearing spec: §13.18.60.4 GR13.*

**Scope (6a).** `FLOAT-SHORT`→`System.Single`, `FLOAT-LONG`→`double`, `FLOAT-EXTENDED`→`double`
(no .NET quad — conformant via the §13.18.60.4 GR13 **subset nesting** short⊆long⊆extended, :22824: every long value
is expressible in extended, so equality satisfies it), and the vendor synonyms `COMP-1`=FLOAT-SHORT, `COMP-2`=FLOAT-LONG.
GR13 (:22824): the trio are *signed numeric data items in an implementor-defined floating-point
format* — so representation/range are OUR choice (IEEE binary32/64), documented per conformance item 207. **COMP-1/2
(the `Usage.Float`/`Double` vendor synonyms) share every operation of this facility** — arithmetic, DISPLAY, and MOVE
all route through the float paths below. **6b (stays COBOLNET0899):**
`FLOAT-BINARY-32/64/128` + `FLOAT-DECIMAL-16/34` (§13.18.60.4 GR14-GR20 :22826-22904) — a DISTINCT facility with pinned
bit layouts, the endianness (GR19) + decimal-encoding (GR20) phrases, and (decimal64/128) a type .NET lacks; plus the
external-float PICTURE `E`. Shipping the binary family without decimal/endianness would be the forbidden "broad
half-done" — 6a is the complete implementor-defined facility, 6b the pinned one.

**Representation.** A float elementary item is a NATIVE `float`/`double` field (never the scaled substrate, no
NumProfile, no character image) — the loud Tier-C island in any group/REDEFINES/record (the `IsImageCapable`/
`IsCharacterImage`/`StoreAsImage` guards already require `IsFloat:false`). PICTURE-less (§13.18.60.2 — a `PIC` with a
float usage is a new **COBOLNET1521** reject; the 08xx declaration band is exhausted); a new `PicInfo.FloatItem(usage)` factory (mirrors `IndexItem`/
`PointerItem`, `Digits:0/Scale:0` inert). `Usage.FloatShort/Long/Extended` fold into `IsFloat`; a new `IsSingle`
predicate (Float/FloatShort) drives the `f` literal suffix + the `(float)` store cast. `ClrType` short→float,
long/extended→double; init `0f`/`0d`.

**Arithmetic (extends D7; NATIVE §8.8.1.3, the default).** Under NATIVE, any expression with ≥1 float operand
evaluates ENTIRELY in `double` (a single operand widens exactly; result category floating). Realized by a `bool
Real` flag on the `NumX` carrier (parallel to `Dec`): a float leaf → `Real` NumX (`(double)(read)`);
`NumericRenderer.Real(x)` gains a `Real` pass-through arm (the ONE double-conversion helper for all three carrier
kinds); `Combine` takes the `Real` branch under NATIVE; `Negate`/`Power` get Real arms. **Under STANDARD /
STANDARD-DECIMAL the mode branch runs FIRST** (P10 Step 12): a float operand converts into SDIDI form through
`CobolDec.FromDouble` (the §8.8.1.5.1 implementor-defined conversion — the shortest round-trip decimal identity)
and the operations are the SDIDI ones; comparisons likewise convert each operand not already in SDIDI form to
it (a float via `CobolDec.FromDouble`) and compare in SDIDI (§8.8.4.2.4 — under standard-decimal arithmetic
each operand not already a standard-decimal intermediate is converted to that form and the comparison is made
between the two intermediates). Store to a FIXED receiver lands via a new
`CobolFloat.ToScaled(double,scale,mode)` (double→unscaled Int128, rounded; ±Inf/overflow saturate so the existing
capacity check fires SIZE ERROR; NaN→0+latch EC-SIZE) then the EXISTING `CobolNum.Store`/`TryStore` funnel (ROUNDED +
SIZE ERROR for free; MOVE truncates toward zero §14.6.8.2, COMPUTE uses the receiver ROUNDED mode). Store INTO a float
receiver = a native cast to `ClrType` (holds the algebraic value §14.6.8.3 GR1; NO size error — IEEE overflow is Inf,
a valid value; ROUNDED is a no-op).

**MOVE/DISPLAY/compare.** literal→float = the fixed→float cast; float→fixed = `ToScaled`; float→float = a `ClrType`
cast. DISPLAY = `CobolFloat.Display(float/double)` — invariant-culture shortest round-trip (§14.9.11 GR1
implementor-defined; goldens use exact binary fractions for cross-platform stability). Compare = under NATIVE
arithmetic (the default), a native `double` comparison when either operand is `Real` (§8.8.4.2.4 — native
arithmetic compares by native rules; IEEE NaN-unordered / ±0-equal fall out of C# — spec-conformant, no epsilon);
under STANDARD / STANDARD-DECIMAL each operand not already in SDIDI form is converted to it (a `Real` via
`CobolDec.FromDouble`) and the comparison is made in SDIDI (§8.8.4.2.4). The runtime file is `CobolFloat.cs`.

**Floating-point LITERALS (§8.3.3.3.3).** A `FLOATLIT` lexer
token — `( [0-9]+ '.' [0-9]* | '.' [0-9]+ ) 'E' [-+]? [0-9]+` (significand SHALL include a decimal point, §8.3.3.3.3
r2; 'E' not `[eE]` — the lexer is case-insensitive) placed BEFORE `DECIMALLIT` so maximal munch keeps `1.5E3` one
token — plus a `FLOATLIT` alt in `numericLiteralCore` (and the DECIMAL-POINT IS COMMA twin `COMMA_FLOATLIT`,
kb/Work PB98). `EmitText.UnscaledLit` detects the E and returns a `Real` NumX with the literal verbatim (a COBOL
float literal is already valid C# `double` syntax — the form the CALL/INVOKE argument emitters still take).
⛔ **THE LITERAL IS ITS EXACT VALUE IN EVERY POSITION AND EVERY MODE** (owner decision D-B, 2026-08-30; kb/Work
PB156 + PB195, superseding the per-position split PB99 left behind). `NumericRenderer.LiteralNum` is the ONE
rendering: a floating-point literal becomes its §8.3.3.3.3 rule-5 value — significand × 10^exponent — on the `Dec`
(SDIDI) lane, whether it sits in an arithmetic expression or statement, a MOVE source, a relation / EVALUATE
comparand, a PERFORM VARYING FROM / BY value or a function argument. The former native arm made it a binary64
operand on D16's §8.8.1.3 latitude, and §14.9.2.4 GR4 (with its §14.9.44.4 GR4 SUBTRACT twin) does not grant that:
GR4 excludes only operands "described with usage" binary-*/float-*, a LITERAL is not described with usage
anything, and §14.7.7 rule 2 lists "a floating-point literal" as its OWN bullet beside the usage list — proof the
drafters name it when they mean it. Measured: `ADD 1.0E+0` to a PIC 9(20) holding 12345678901234567890 owed …891
and delivered …168.
**D16 IS NARROWED, NOT OVERTURNED.** `CombineCore` is unchanged and its `Real` lane still takes precedence whenever
a float ITEM or a float RECEIVER is present — that is native float arithmetic, IEEE binary64, and it is exactly
the case where GR4's own condition stops holding. What D16 no longer claims is that the notation of a literal changes its
VALUE: `ADD 1.0E+0` computes what `ADD 1.0` computes, and `2 ** 0.5E+0` what `2 ** 0.5` computes, because those
are the same values. ⚠ The notation *does* still select the intermediate LANE, and that is a published
§8.8.1.3 determination rather than an invariant (CONFORMANCE.md A.1 item 82): a float literal operand routes a
NATIVE statement onto the decimal128 lane (34 significant digits) where a fixed-point literal keeps
`CombineNative`'s Int128 one (~38), so `A * B + 0.0E+0` and `A * B + 0.0` differ for 18-digit A and B, and
INTERMEDIATE ROUNDING (§11.9.11) reaches the first and not the second. The sentence "a literal's NOTATION never
changes the arithmetic" was written in four places and is false in all of them (kb/Work PB274).
The binder checks the literal's FORM (SR2/SR3/SR4 — COBOLNET1661) at the ONE normalizer and its RANGE at the ONE
expression chokepoint (`CheckLiteral`, now decimal128 in every mode — one carrier, one range) and, separately, at
the VALUE funnel for a FLOAT-* subject, where §13.18.63.3 SR2 asks for "permissible values within the range
indicated by the PICTURE clause or the USAGE clause" (cite.py-verified verbatim) and, a FLOAT-* subject having
no PICTURE, the bound is therefore the ITEM's binary form. The implementor-defined exponent range of
§8.3.3.3.3 r3 is published in CONFORMANCE.md §7 A.1 item 82, together with its one residual: SR2 admits a 36-digit
significand and the SDIDI carrier holds 34, so a 35th/36th significand digit rounds at `CobolDec.FromParsed`.
⚠ **Known exception, registered not fixed:** the CALL argument emitter renders a bare numeric-literal argument
through `EmitText.UnscaledLit` rather than this rendering — kb/Work PB263 (BY CONTENT: a raw Roslyn CS1503) and
PB264 (BY VALUE: the fraction digits truncated away).

**Float-integration edge cases.** The Real integration covers these paths: a float source/result into a
NUMERIC-EDITED receiver (MOVE + COMPUTE) lands via `ToScaled` at the MASK scale (`CobolEdit.MaskScale`, NOT
`pic.Scale`, which is 0 for an edited item); NEAREST-TOWARD-ZERO uses an explicit nearest-tie-toward-zero (not
`MidpointRounding.ToZero`, which is DIRECTED and would truncate all values); a fractional level-88 VALUE on a float
uses a native-double membership branch (exact-inverse membership, not a scale-0 truncation); a fixed-only expression
into a float receiver uses a `TargetReal` flag that makes the whole RHS Real when every target is float (so `10/3`
evaluates in binary64, not at receiver-scale 0; reset at the condition-render entry, the H1 staleness discipline);
PROHIBITED on an inexact float uses a `CobolFloat.InexactAtScale` gate that raises SIZE ERROR + leaves the receiver
unchanged (§14.7.4.3 rule 7); a transcendental intrinsic into a float receiver returns Real from `RenderFloat` when
`TargetReal` (full binary64, not quantized to 9 digits).

**Edition gate.** The trio introduced 2002 → the `ConstructRegistry` introduction gate stands (COBOLNET0900 below
2002, silent ≥2002; default `--std` 2023). COMP-1/2 accepted at ALL editions (universal vendor synonyms of the
conformant usages — a documented asymmetry, matches the legacy oracle).

### D18. Every WORKING SCALE is capped at the receiver's Int128 headroom, and a RECEIVER-LESS float render is not quantized at all (`ReceiverContext.WorkingScale(floor)` / `.Receiverless`).

**Decision.** Two rules, one carrier. (a) When a fixed-point receiver governs the render, a value landed at a
WORKING scale uses `ws = min(max(receiverScale, floor), 38 − receiverIntegerDigits)`, where `floor` is the
family's own: **9** for the §15.4.1 float family and `**` (`FloatWorkingScale`), **6** for the NUMVAL family
(§15.67/§15.68/§15.69). One method parameterised by the floor — NOT one rule per family, because the defect is a
property of the working-scale-then-rescale SHAPE and not of which function produced the value, and a per-family
copy is exactly how PB5 fixed the float clamp while leaving `Numval`/`NumvalF` clamping at `long.MaxValue`.
(b) When NOTHING governs the render — `ReceiverContext.None`: a relation operand, a sign condition, a
DISPLAY/STRING text operand, a MOVE source — a float-family value stays binary64 (`NumX.Real`) and is never
quantized.

**Corollaries that fall out of (a) and (b), both landed with it.** `CobolIntrinsics.Numval`/`NumvalC`/`NumvalF`
return `Int128` and saturate there through one shared `Rescaled` helper (which also BOUNDS the decimal shift
before calling `Pow10.AsWide`, whose fallback loop wraps past 10³⁸ — an unbounded `E±nn` would otherwise multiply
by a wrapped power and yield a plausible wrong value instead of a saturated one). And `NumericRenderer.Align`
gained a `Real` arm: rule (b) lets a binary64 reach the receiver-less integral sites (subscript, SET amount,
PERFORM VARYING FROM/BY, report VARYING, RETRY count), and without it the double expression went straight to a
caller expecting a scaled integral — the PB2 shape. It was already reachable through a COMP-2 operand, so the arm
is a latent fix as well as a required one, and it belongs at the ONE choke point rather than at forty call sites.

**The round-UP sibling: `NumericRenderer.AlignRoundedUp` (kb/Work PB142/PB151).** `Align` truncates on every lane
— that is its contract, and it is right for the value-semantics intermediate sites it serves. A handful of clauses
instead say the expression is **"rounded up to the next whole number"**, and those must NOT reach `Align`.
The population is **closed, not sampled**: a sweep of the standard for that phrase returns exactly TWO normative
sites — **§14.7.9.3 GR1** (the RETRY phrase's `arithmetic-expression-1`, an n-TIMES count) and **§14.9.3.4 GR1**
(ALLOCATE's `arithmetic-expression-1`, a byte count). `AlignRoundedUp` is the one place that rule is written; both
clauses call it, and `NumericRoundUpSiteDriftTests` asserts that neither round-up-ruled emit site reaches
`Align(…, 0)` — the defect it was built from was not a wrong helper but a call site choosing the wrong one
(`RETRY 1.5 TIMES` emitted 1 re-attempt where GR1 requires 2, and `RETRY 0.5 TIMES` emitted 0, silently degrading
to the no-retry case).

The mode is **`CobolRounding.TowardGreater`** — round toward positive infinity, which *is* "rounded up to the next
whole number" on every lane (`CobolFloat` via `Math.Ceiling`, `CobolNum`/`CobolDec` by adding one only when
positive). Deliberately **not** `AwayFromZero`, which agrees on positives but rounds −1.5 to −2 where the rule
requires −1. Both clauses guard their own negatives — §14.9.3.4 GR2 shunts ≤ 0 to NULL, §14.7.9.3 GR4a shunts a
negative or zero expression to the unsuccessful path before the rounded value is used — so the choice is
behaviour-neutral at both of today's sites and simply correct at the next one. The lane structure mirrors `Align`
exactly, checked landings included.

⚠ **One named exemption, so it is not read as drift.** ALLOCATE's NATIVE-FLOAT lane keeps `CobolPtr.AllocateReal`,
which fuses the same ceiling with GR2's ≤ 0 ⇒ NULL and with the storage-not-available outcomes only the ALLOCATE
statement defines (a NaN or over-wide request answers "not available" instead of raising). Routing it through a
checked emitter-side landing would trade that defined outcome for a size-error condition and re-open a green
PB151 landing. The rounding RULE is still written once; `AllocateReal`'s ceiling is that rule realised inside the
statement's own outcome machinery, and the drift test knows the exemption by name.

The RETRY phrase's SECONDS arm is governed by a DIFFERENT rule and keeps its own truncating rendering: §14.7.9.3
GR2 stores the timeout period through an implicit COMPUTE **without** the ROUNDED phrase into a `9(n)V9(m)`
temporary. One clause, two arms, two roundings — see `COBOLNET_FILES_DESIGN.md` **D8**.

**Rationale (fix-queue PB13).** `FromDouble` saturates at `Int128.MaxValue`, and the store then rescales
ws→receiver scale, which DIVIDES the sentinel back down. The receiver's digit-capacity check — the one mechanism
that would raise the size error — therefore never sees it, which is why the saturation was SILENT rather than
loud. The flat `max(Scale, 9)` needed `receiverIntegerDigits + 9` digits where Int128 supplies 38, so a `PIC 9(31)`
receiver was two digits short: `COMPUTE R = FUNCTION EXP(70)` stored 0170141183460469231731687303715 — wrong by a
factor of ~15 — and reported NO SIZE ERROR. The cap restores BOTH halves of the contract, and the second is what
makes it sufficient rather than merely better: a value that fits can no longer saturate, and a value that does not
saturates to a sentinel that STILL exceeds the receiver after the rescale, so the store raises the size error
condition (§14.7.5 case 5 — a native intermediate outside its implementor-defined range, that range being declared
checked under §8.8.1.3). This is the same discipline `CobolFloat.ToScaled` already relied on; it got the guarantee
free by landing AT the receiver's scale, and only the working-scale path lost it.

Rule (b) is the half no working-scale choice can reach, because with no receiver there is no scale to quantize TO
and the `ws = 9` stand-in was arbitrary. §15.4.1 leaves "the characteristics and representation of the returned
value" to the implementor under native arithmetic, and **COBOL.NET's determination is that the §15.4.1 float
family's returned value IS a binary64** — the quantization is part of the TRANSFER into a fixed-point receiver
(§14.6.8), not part of the value. Every consumer of a receiver-less numeric already had a `Real` arm and is more
correct on it: a relation compares natively (§8.8.4.2.4, the arm a COMP-2 operand already took), the text channel
renders through the one `CobolFloat.Display` a float ITEM uses (§14.9.11.4 GR1), and a MOVE source lands through
`CobolFloat.ToScaled` at the receiver's scale — the saturation-safe form. This is also what `docs/CONFORMANCE.md`
had ALREADY documented ("a FLOAT-valued function renders through the same shortest-round-trip `CobolFloat.Display`
a COMP-2 item does, so the function and an item holding its value agree"); the working-scale form silently broke
it, printing `2.000000000` for `FUNCTION SQRT(4)` against a determination that says "no zero padding", and
printing the saturation sentinel outright for `FUNCTION EXP10(31)`.

**Blast radius, measured.** The cap BINDS only past 29 integer digits (below that `38 − intDigits ≥ 9` and the
float floor still wins), so no ordinary picture changed; the whole Conformance corpus moved exactly one golden,
`da2_function_as_text`, and it moved TOWARD its own stated determination.

**Rejected alternatives.** (a) Widen the clamp constant — REJECTED and provably impossible: at ws = 9 a 31-digit
receiver needs 40 digits and Int128 supplies 38, whatever constant is chosen. (b) Raise inside `FromDouble` —
REJECTED: it cannot see a receiver, the capacity check already reports the condition correctly, and raising there
would convert an EC-SIZE-TRUNCATION into an EC-SIZE-OVERFLOW with different fatality for no gain. It would also
abort legal source in the receiver-less case (`DISPLAY FUNCTION EXP10(31)`), which is worse than rejecting it.
(c) Encode "receiver-less" as `IntegerDigits == 0` — REJECTED: an all-fraction receiver (`PIC V9(9)`) also has
zero integer digits but DOES define a quantization scale, hence the separate `Receiverless` flag.

**Guard.** `FloatQuantizeHeadroomDriftTests` proves both invariants over every legal (integer-digits, scale) pair
a picture can present, pins the below-29 behaviour-neutrality, and fails on a hand-rolled `Math.Max(rcv.Scale, 9)`
at either quantize site — a mistake no runtime test can see, since it is correct for every ordinary picture.
Goldens: `pb13_float_quantize_headroom` (the two cases the queue carried) and `pb13_float_quantize_siblings` (the
four the sibling sweep found, `**` included — it reaches the same quantizer and carried the identical defect).

## C# mapping

DATA-DIVISION → CLR storage type:
- `01 N PIC 9(5).`            → `long N;`            (unscaled; 5 digits ≤ 18)
- `01 B PIC S9(4)V99 COMP-3.` → `long B;`            (unscaled = value×100; scale=2 metadata)
- `01 W PIC S9(31).`          → `Int128 W;`          (>18 digits — legal only at `--std ≥2002`; a >18-digit picture is a compile diagnostic at `--std 85`)
- `01 H PIC 9(18) COMP-5.`    → `long H;`            (unsigned; ≤18 digits → long, >18 → Int128; width bounded by binary-wrap) ; `S9(9) COMP-5` → `long`
- `01 R PIC S9(7)V9(2).`      → `long R;`            (DISPLAY signed; SignKind=TrailingOverpunch metadata)
- `01 F COMP-1.`              → `float F;`  ; `COMP-2` → `double`
- `01 E PIC ZZ,ZZ9.99-.`      → `string E;`          (numeric-edited image)

PROFILE (compile-time emitted once per item, runtime carrier):
```csharp
public readonly record struct NumProfile {
  public required int Digits; public required int FractionDigits;
  // no separate P fields — FractionDigits IS the net signed scale (V-fraction + leading-P − trailing-P; may be NEGATIVE)
  public required bool Signed; public NumericSign SignKind;
  public required NumericTruncation Truncation;      // CAPACITY: where SIZE ERROR bites
  public required NumericByteForm ByteForm;          // REPRESENTATION: what the item occupies at a byte boundary (D17)
  public int StorageLength;                          // the exact byte width Binary/Packed/PackedNoSign lay out
  public int FractionScale => FractionDigits;   // signed; CobolNum.Rescale handles a negative scale natively
}
public enum NumericSign { TrailingOverpunch, LeadingOverpunch, LeadingSeparate, TrailingSeparate, BinaryMinus }
// The PINNED byte representations (implementor-defined by §13.18.60.4 GR4/GR7/GR11/GR12 — D17):
//   None         reaches no byte image at all (USAGE INDEX) — a codec handed one rejects it LOUDLY
//   Zoned        USAGE DISPLAY: one byte per digit position, sign per SignKind
//   Binary       two's complement, BIG-ENDIAN, StorageLength bytes (1-2-4-8 by digit count)
//   Packed       BCD, two digits per byte, trailing sign nibble 0xC/0xD (0xF unsigned) — Digits/2+1 bytes
//   PackedNoSign the 2023 WITH NO SIGN form: digit nibbles only — ceil(Digits/2) bytes
public enum NumericByteForm { None = 0, Zoned, Binary, Packed, PackedNoSign }
```

INTERMEDIATE CARRIER + ENGINE (conceptual — realized per D1 as Int128-typed `NumX` expressions + `CobolNum` kernels):
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

BACKEND NOTE (dual backend, `--backend roslyn|cil`): `CobolNum`/`CobolInt`/`CobolEdit` are the backend-NEUTRAL runtime contract — the bound tree carries the structured operation (operands, scales, ROUNDED mode, receivers) and each `ICodeGenBackend` (Roslyn C# primary; future Cecil/CIL, own private lowering) renders calls to the same runtime API. The snippets below are the Roslyn rendering, not the semantic definition.

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
Console.WriteLine(CobolNum.FormatDisplaySigned(R, P_R));   // → "015}"  ({/} table, last digit overpunched)
```
NUMERIC-EDITED MOVE `MOVE AMT TO E` (E = ZZ,ZZ9.99-):
```csharp
E = CobolEdit.Format(CobolInt.FromStorage(AMT, P_AMT.FractionScale), P_E.EditPattern, _env);
```
IS NUMERIC on a DISPLAY field (the only nontrivial class test — magnitude+sign image are validated against PIC):
```csharp
if (CobolNum.IsNumericClass(rawImageOrValue, P_X)) ...
```

## Edition gating & diagnostics (G1 — four compilers in one)

`cobol.exe` targets ISO COBOL 1985 / 2002 / 2014 / 2023 via `--std 85|2002|2014|2023`. Every edition-varying construct carries TWO co-equal obligations: (1) the complete per-edition ISO-spec behavior in every edition that HAS it; (2) the correct DIAGNOSTIC in every edition that LACKS it (not-yet-introduced or removed). Tests (NIST etc.) only VERIFY; they never SCOPE. Derive each gate from `specs/ISO_COBOL.md` (Annex E) + `docs/VERSION_CHANGE_REFERENCE.md` (the 130-row edition-change checklist — 2002→2023 deltas ONLY; it has NO 85→2002 rows, so derive 85↔2002 gating from the 2002 standard / the ISO2023_CONFORMANCE_PLAN M2 catalog), and land each as (construct × edition) cases in the VERSION TEST MATRIX (`docs/VERSION_TEST_MATRIX_DESIGN.md` — the (construct × edition) matrix; Phase 0 done). Known gates in this subsystem:

- **PICTURE digit ceiling — 18 (1985) vs 31 (2002+)**: `PIC S9(31)` (the `Int128` storage tier) is legal only at `--std ≥2002`; under `--std 85` any picture >18 digits is a compile diagnostic. The long→Int128 boundary is edition-reachable only at ≥2002.
- **Composite-of-operands limit — 31 digits at every edition** (§14.7.7 rule 2): the compile-time threshold is the constant 31 — the spec states it with no edition qualifier, and CCVS-85 itself (NC101A composes 21 digits in a MULTIPLY) refutes any 85-specific tightening to 18.
- **ROUNDED MODE IS / the 8 modes / DEFAULT ROUNDED (§11.9.6) / INTERMEDIATE ROUNDING (§11.9.11) — 2014+**: at `--std 85|2002` bare ROUNDED means the single nearest-away-from-zero rounding, and `ROUNDED MODE IS …` + those OPTIONS clauses are not-yet-introduced diagnostics. 2014→2023 behavior delta: rounding raises EC-SIZE-TRUNCATION only under PROHIBITED (`VERSION_CHANGE_REFERENCE.md` row 53).
- **ARITHMETIC clause — LIVE (P10 Step 12)**: `NATIVE`/`STANDARD` are 2002 introductions (rows `options-paragraph-2002` / `options-arithmetic-native-2002` / `arithmetic-standard-2002`; the E.2-item-21 back-derivation), `STANDARD-BINARY`/`STANDARD-DECIMAL` 2014+ keyword rows on the `VisitArithmeticMethod` arm; at 2023 `STANDARD` is REMOVED (row 28 — 0807, error strict / warning permissive) and it is OBSOLETE-flagged (0903) at 2014; `STANDARD-BINARY` is OBSOLETE at 2023 (row 116) and documented-unsupported everywhere (COBOLNET0806 — distinct from the edition gates). The 2014-only OPTIONS clauses carry their own 0900 rows (`options-default-rounded/-intermediate-rounding/-entry-convention/-float-binary/-float-decimal/-initialize-2014`). At `--std 85` the whole paragraph is not-yet-introduced (0804).
- **BINARY-CHAR/SHORT/LONG/DOUBLE usages — 2002+** (they lower to the COMP-5 binary-wrap discipline); diagnosed at `--std 85`. **LIVE:** PICTURE-less native 1/2/4/8-byte two's-complement integers (SIGNED default / UNSIGNED widens), BinaryCapacity truncation, implied DISPLAY digit width 3/5/10/19·20; introduction gate COBOLNET0900 below 2002, PICTURE prohibited COBOLNET0870 (§13.16.3 SR8). COMP-5 itself is an extension — document its per-dialect availability.
- **EC-* exception conditions (EC-SIZE-*) — 2002+**: at `--std 85` only the ON SIZE ERROR phrase semantics exist; EC names/TURN must not surface.

(Verify every edition attribution above against the spec's Annex E / the edition record before wiring the gate — the spec, not this list, is authority.)

## Hard problems

### Int128 escape boundary: EVERY arithmetic operation on the intermediate, not only MULTIPLY (§14.7.5 rule 5).

**The governing rule.** §8.8.1.3 makes native arithmetic implementor-defined; this design defines the intermediate as unscaled `Int128` and declares its range **CHECKED**, which is precisely the precondition of §14.7.5 rule 5 — *"if native arithmetic is in effect and the implementor defines that the range of values allowed for the intermediate data item is to be checked, when an arithmetic operation on the intermediate data item would cause the new value to be outside of the allowed range"*. So **any** step that leaves the carrier is the size error condition: it lands on `ON SIZE ERROR` leaving the receiver unchanged (§14.7.5 rule 1), or terminates the run unit when neither the phrase nor EC-SIZE checking is present. **A wrap is never an answer** — §15.4.1 asks at worst for "an implementor-defined approximation", and a sign-flipped value approximates nothing.

⛔ **This section said MULTIPLY, and that omission shipped twice.** Written for products only, it left the intrinsic family's ADD and SUBTRACT unguarded: PB32 found MEDIAN/MIDRANGE wrapping, and PB252 found SUM's `s += x` and RANGE's `max - min` still wrapping eight months later — `FUNCTION SUM` stored a sign-flipped value through an `ON SIZE ERROR` phrase that was NOT taken, and `FUNCTION RANGE` returned a NEGATIVE range, a value §15.76.4 r1 cannot produce. The policy is per-OPERATION and the code now says so: `CobolIntrinsics.Exact.cs#SizeEscape` is the one raise site, with `ExactAdd` / `ExactSub` / `ExactMul` beside it, and `ExactCarrierBoundaryDriftTests` closes the exact-carrier entry-point set by reflection so a new function is guarded or explicitly exempted before it can ship.

⚠ It is per-site `checked`, deliberately NOT `<CheckForOverflowUnderflow>` on the Runtime project: binary-field stores and truncation paths wrap MODULARLY on purpose (§13.18.40, §14.9.25.4 GR6) and a project-wide switch would turn every one of those conforming wraps into an exception.

Statement arithmetic (ADD/SUB/MUL/DIV with GIVING) enforces the 31-digit composite-of-operands limit at COMPILE time (§14.7.7 rule 2) → guaranteed to fit Int128, no runtime check needed. COMPUTE expressions have NO composite-of-operands limit (§8.8.1.2 rule 7 — that clause is about the composite limit and nothing else; it is **not** the escape boundary's citation, which is how a wrong § reached user-visible message text and was corrected by PB252): compute in Int128; before each Mul, if both operand digit-counts sum > 38, raise EC-SIZE-OVERFLOW. A future >38-digit need is the only thing that would force a wider carrier — explicitly out of v1 scope and flagged.

### DIVIDE intermediate/quotient scale — the one scale `decimal` auto-picked that Int128 forces explicit.

Mined from legacy ArithmeticLowerer.LowerDivide: it divides into a `decimal` accumulator (28-29 sig digits) then rescales to the receiver only on the final store. Reproduce with guardScale = max(receiver scales, operand scales) + DIV_GUARD_DIGITS(=14); RoundDiv at guardScale, then TryStore rounds once to the receiver scale per the ROUNDED mode. REMAINDER (DIVIDE…REMAINDER): quotient truncated to GIVING's fraction-digits (NOT rounded), remainder = dividend − quotient×divisor at the GIVING scale (legacy ComputeCobolRemainder / IrCobolRemainder uses givingFracDigits, no rounding).

### Mixed float/fixed arithmetic and the COMP-1/COMP-2 capacity bypass.

Type-classify each arithmetic sub-expression: if any operand is float-usage, the sub-expression evaluates in `double` (IEEE), and the final store to a fixed-point receiver converts double→CobolInt at the receiver scale with the ROUNDED mode (legacy StoreArithmeticResult guards `if Usage is Comp1/Comp2 skip scaling`). A pure-fixed sub-expression stays Int128. The 31-digit composite limit applies only to the non-float operands (rule 2b). COMP-5 stores in its exact native width and bounds by two's-complement (binary-wrap), not by digit count.

### MOVE numeric→numeric rescale across different scales/usages without going through arithmetic.

MOVE is value-preserving with receiver-scale alignment (ISO §14.9.25): load sender as CobolInt at its scale, TryStore into the receiver (rescale to receiver FractionScale — TRUNCATION rounding for a plain MOVE, never ROUNDED; high-order truncation per receiver Digits; unsigned receiver drops sign = stores magnitude, §14.9.25.4 GR6.d.2.b). A scaled→integer MOVE drops the fraction (truncate). DISPLAY↔COMP↔COMP-3 differ only in external image/capacity, not value — the same TryStore covers all, then the receiver's encoder (overpunch for signed DISPLAY, packed for COMP-3) renders the image.

### P scaling (leading and trailing) in store, divide, and edited-format paths.

MODEL: a single signed scale (rather than two P fields — this removes a derived getter and a second source of truth). `NumProfile.FractionDigits` IS the net signed scale (V-fraction + leading-P − trailing-P, may be NEGATIVE; ISO §13.18.40) and `CobolNum.Rescale` handles a negative scale natively. Leading P deepens the scale (point left of every digit); trailing P makes the scale negative — stored digits are multiples of 10^|scale|, and ScaleAndRound must round to that 10^P grid (legacy clean ScaleAndRound: shift by +trailingP, round to integer units, multiply back). Port the grid-rounding into CobolInt and into CobolEdit (legacy FormatByEditPattern divides absValue by 10^trailingP before building digits). Capacity counts only the explicit '9' digits, not the P positions.

### Comparison scale-alignment (IF A = B, A > B) across operands of different scale/usage/sign.

Comparisons are on algebraic value (ISO §8.8.4.2). Load both operands as CobolInt, Align to common scale, compare the Int128 unscaled values (sign included). A numeric-vs-numeric comparison NEVER goes through the receiver-truncation path (no capacity loss). A numeric-vs-alphanumeric/figurative comparison compares against the numeric value of the literal/ZERO. Equal magnitude with +0/−0 compares equal (signs of zero are the unique value 0).

### IS NUMERIC / IS NOT NUMERIC class condition on a DISPLAY field (and the sign-image validity).

For a typed numeric field the value is always a valid number, so IS NUMERIC is trivially true UNLESS the field can hold an externally-set image (REDEFINES over alphanumeric, file input, ACCEPT). For those the test validates the character image against the PIC: every position a digit, the sign position a valid overpunch/separate sign for the SignKind, no embedded spaces (legacy IsNumericClass logic, PicRuntime line ~2386: overpunch chars {A-I}/{J-R}/{/} accepted only at the overpunch position). Port that image-validation as CobolNum.IsNumericClass(image,profile) used only on the byte-boundary/redefines cases.

### ON SIZE ERROR semantics across multiple receivers and the initial-evaluation step (ISO rule 4, p595).

Two-phase per the spec: (a) evaluate the expression into the intermediate CobolInt — if SIZE ERROR here (Int128 overflow / exponentiation EC), NO receiver changes and the ON SIZE ERROR imperative runs. (b) store left-to-right into each receiver via TryStore — if a single receiver overflows, only THAT receiver is left unchanged and processing continues to the next receiver; ON SIZE ERROR runs if ANY store failed. Generated C# accumulates a `bool __sizeErr` across the per-receiver TryStore calls and branches once at the end.

## Edge cases

- Negative value MOVEd to an UNSIGNED receiver stores the magnitude (ISO §14.9.25.4 GR6.d.2.b) — TryStore returns the signed value, the receiver's encoder/storage drops the sign; for the unscaled-long storage this means the stored long is Math.Abs (but watch long.MinValue: use %Pow10(digits) before abs, as the legacy FormatUnsignedDisplay does).
- +0 and −0 are the unique value 0 for sign tests and comparison (SDIDI NOTE 1); an all-zero magnitude with a negative overpunch must still test as zero and as NOT negative.
- DISPLAY of a zero value in a signed trailing-overpunch field: last digit '0' positive → '{' (e.g. PIC S9(4) value 0 → "000{"), NOT "0000" — confirmed by legacy table.
- Numeric-edited zero with floating $ or Z across the whole field and no fixed 9 → entire field blanked to spaces (or '*' under check-protect); but if any fixed 9 exists the zeros print. BLANK WHEN ZERO overrides to all-spaces regardless.
- Asterisk check-protect (*): suppressed positions AND the comma/B insertion chars inside the floating zone become '*', but the decimal point stays '.' (legacy pass-2 logic).
- DECIMAL-POINT IS COMMA swaps the roles of '.' and ',' in both the picture interpretation and the edited output. **CURRENCY SIGN is a SET, and the currency STRING may have any length (§12.3.7.3 r21–r27 / §12.3.7.4 GR13 / §13.18.40.4 GR14; PB60 / AR-15.68.3-3, 2026-08-17):** `DataBinder.CurrencySigns` maps every currency PICTURE SYMBOL (uppercase-keyed, letter cases equivalent per r20) to its currency string — a bare `CURRENCY SIGN "#"` binds '#'→"#" (r22), the 2002+ `CURRENCY SIGN "USD" WITH PICTURE SYMBOL "U"` binds 'U'→"USD" (r23: any length; at least one non-space; none of `0-9 + - , . *`), r21 refuses two clauses binding equivalent symbols to different strings, and r25 IMPLIES `'$'→"$"` unless a clause names '$' as literal-7 or literal-8 — so `PIC $$,$$9.99` stays legal beside a declared '#'. A PICTURE uses at most one symbol kind (§13.18.40.3 r24/r28); `PictureAnalyzer` finds which, CANONICALIZES the mask's symbol to `$` (`PicInfo.EditMask`), records the string on `PicInfo.CurrencyString` (null when it is "$"), and adds the string's extra length to `PicInfo.Length` for the FIRST occurrence (GR14: "The first occurrence of the currency symbol adds the number of characters in the currency string to the size of the item. Each subsequent occurrence of the currency symbol adds one"). `CobolEdit.Format/TryFormat/DeEdit` take `currencyString:` per item (`EmitContext.EditCfg(pic)` — the per-unit `currency:` symbol argument is gone): the core edits the LOGICAL one-position-per-symbol image and `ExpandCurrency`/`CollapseCurrency` map it to/from the physical one — fixed insertion puts the string where the symbol is (`PIC U9.99` → `USD5.25`), floating insertion lands it once before the first nonzero digit (`PIC UUU9` → ` USD12` / `USD123` / `  USD0`), an all-floating zero is all spaces at the physical width, BLANK WHEN ZERO blanks the physical width, and de-editing removes the string before the digit walk. NUMVAL-C / TEST-NUMVAL-C without argument-2 take the unit's `SoleCurrencyString` (§15.68.3 r3 — "$" with no clause, the one explicitly specified string, and COBOLNET1644 when two or more distinct strings are specified; r25's implied '$' is the picture-symbol default, not a competing string). Pinned by `pb60_currency_sign_set` / `pb60_currency_sign_bare_implied_dollar` + `CobolEditCurrencyStringTests`; negative `numval-c-ambiguous-currency`.
- COMP-3 unsigned (PIC 9 COMP-3) must store sign nibble 0x0C (positive) and never retain a negative — the legacy EncodeComp3 fix: unsigned packed wrongly kept 0x0D and decoded negative.
- COMP-5 signed negative extreme (−2^(width−1), e.g. −32768 for 2 bytes) is IN range — a naive magnitude check mis-flags it; bound the signed two's-complement value, not the magnitude (legacy clean ExceedsBinaryCapacity).
- COMP-5 unsigned 8-byte range (long.Max, ulong.Max] requires `Int128` storage — a signed-long codec cannot hold it; the monomorphic wide engine carries the full 0..2^64−1 range.
- Exponentiation: 0**0 → EC-SIZE-EXPONENTIATION; negative base with non-integer exponent → EC-SIZE-EXPONENTIATION; negative base with integer exponent → defined (real) result; a base whose both-roots are returned uses the positive root (§8.8.1.2 rule 6).
- Divide by zero → EC-SIZE / SIZE ERROR, receiver unchanged (NOT a .NET DivideByZeroException — CobolInt.Div must guard b.Unscaled==0 and signal up).
- ROUNDED MODE PROHIBITED: an inexact result at the receiver scale raises SIZE ERROR and leaves the receiver UNCHANGED even though no overflow occurred (ISO §14.7.4.3 rule 7) — TryStore checks inexactness before the capacity check.
- Trailing-P rounding grid: PIC 9(3)P value 1234 is stored as 1230 (multiple of 10^1) and capacity counts only the 3 nines; ScaleAndRound must round to the 10^P grid, not to scale 0.
- Composite-of-operands limit is a COMPILE-time diagnostic for ADD/SUBTRACT/MULTIPLY/DIVIDE (not COMPUTE); exceeding it is a compile error, not a runtime SIZE ERROR. The threshold is 31 digits at every edition (§14.7.7 rule 2 — see the Edition gating section).
- Mixed signed/unsigned and DISPLAY/COMP operands in one COMPUTE all reduce to algebraic CobolInt values — representation differences vanish in the intermediate; only the final receiver's representation matters.

## ISO citations

- §14.7.5 rule 5 — **the escape boundary's governing rule**: with native arithmetic in effect and the implementor's intermediate range declared CHECKED, an arithmetic operation that takes the intermediate outside that range IS the size error condition
- §14.7.5 rule 3 — a result further from zero than the resultant data item permits is the size error condition regardless of the carrier (no implementor latitude)
- §14.7.5 rule 1 — under the SIZE ERROR phrase, every resultant data item keeps the value it had at the start of the statement
- §8.8.1.1 General — evaluation rules depend on the mode of arithmetic in effect
- §8.8.1.2 rule 7 — arithmetic expressions allow combining operations WITHOUT the composite-of-operands and receiving-item restrictions (so COMPUTE has no 31-digit limit; ADD/SUB/MUL/DIV statements do)
- §8.8.1.2 rule 6 — exponentiation rules (0**0 / negative base / both-roots → positive)
- §8.8.1.3 Native arithmetic — implementor-defined; in effect when ARITHMETIC IS NATIVE or no ARITHMETIC clause (the corpus default); implementor specifies the techniques (= Int128 fixed-point here)
- §8.8.1.5.2 Standard-decimal intermediate data item (SDIDI) — decimal128, 34 digits, exp ±6144, smallest 1.0E-6176 (the mode the lock cannot natively support)
- §8.8.1.5.4 — exponentiation integer powers expand to repeated multiply (operand-2 = 1,2,3,4 → operand-1, op*op, ...)
- §14.7.7 rule 2 (p595) — composite of operands ≤ 31 digits for ADD/DIVIDE/MULTIPLY/SUBTRACT under native arithmetic; when a float/intrinsic operand is present the limit applies to the OTHER operands only
- §14.7.7 rule 4 (p595-596) — two-phase arithmetic execution: initial evaluation into an intermediate data item (SIZE ERROR here → no receiver changed), then left-to-right store into each receiver (per-receiver SIZE ERROR leaves only that receiver unchanged)
- §14.7.4.3 (rules 3-10) / §11.9.6 (DEFAULT ROUNDED) / §11.9.11 (INTERMEDIATE ROUNDING) — the eight ROUNDED modes; ROUNDED MODE PROHIBITED (§14.7.4.3 rule 7) raises the size error condition (EC-SIZE-TRUNCATION) on an inexact result and leaves the receiver unchanged
- §14.9.25.4 GR6.d.2.b — a negative result stored into an unsigned receiver stores the magnitude (sign dropped)
- §8.3.3.3.2 — fixed-point numeric literals 1 through 31 digits (the picture digit-count ceiling driving the long→Int128 boundary)
- §8.5.1.2 / §13.18.60 — the three capacity disciplines (digit-count for DISPLAY/COMP/BINARY, packed 2n−1 for COMP-3, native two's-complement width for COMP-5/BINARY-*)
- §8.8.4.2 — simple relation conditions compare algebraic values (comparison scale-alignment, +0=−0)

## Open questions (resolved in `COBOLNET_DESIGN.md` §18)

- ARITHMETIC IS STANDARD-DECIMAL (full ISO §8.8.1.5: decimal128, 34 sig digits, exp ±6144) is implemented as the exact Int128-significand SDIDI (`CobolDec`) — never .NET `decimal` (96-bit/28-digit), which the lock forbids. The SDIDI is used ONLY when ARITHMETIC IS STANDARD-DECIMAL is in effect; STANDARD-BINARY stays unimplemented (spec-obsolete). Per-edition: the clause gates per the Edition gating section (STANDARD-DECIMAL/-BINARY are 2014+), and STANDARD-BINARY emits a clear not-implemented diagnostic.
- DIV_GUARD_DIGITS value (14, reproducing the legacy decimal accumulator's headroom). Empirical: confirmed against the NIST division-rounding goldens (byte-identical) — a too-small guard loses rounding precision, too-large risks Int128 overflow on already-deep operands. Tunable, but locked pending any new counter-example.
- The >38-digit ceiling: COBOL-2002/2014 permit pictures up to 31 (mandatory) and some profiles to 38; arithmetic intermediates on 19+-digit operands can exceed Int128. If the conformance target ever requires guaranteed-exact arithmetic beyond 38 digits, the carrier must widen (Int256 / a fixed 128-bit decimal). Confirm 38 digits is sufficient for the 2002/2014/2023 `--std` conformance scope (per `docs/VERSION_CHANGE_REFERENCE.md` + the version test matrix), else this becomes a substrate question.
- RESOLVED: `COBOLNET_ARCHITECTURE.md` §3's data-model table is native `long`/`Int128`-unscaled (NO `decimal`); no second SSOT remains.

### D19. NATIVE `**` is EXACT while the result fits the `Int128` carrier and the documented double approximation past it; §8.8.1.2 rule 6 is screened on EVERY native arm.

**Decision (⚖ OWNER, 2026-08-03; fix-queue PB18 + PB28 + PB32).** Two parts, and only the first is a choice.

**(a) The technique — a choice, taken.** §8.8.1.3 makes native arithmetic implementor-defined, so any of exact,
approximate, or exact-then-raise conforms. COBOL.NET's documented native technique is **exact `Int128` repeated
multiplication whenever the result fits the carrier, falling back to the double approximation when it does not** —
never a size error merely for outgrowing the carrier. `CobolIntrinsics.PowNativeIntDec` is the one implementation
(since PB69 — see the ⛔ paragraph below; the Int128 twin's saturating fallback is gone).

**Rationale.** Routing an integer power through `System.Math.Pow` contradicted D3's own "exact `Int128`
fixed-point engine": `COMPUTE R = 10 ** 30` into a `PIC 9(31)` returned `1000000000000000071935427891953` where
`Int128` holds 10³⁰ exactly. ⭐ **And the SURVEY decided the shape of the fallback** (`survey_compilers_on_latitude`
— the owner asked for it before deciding): IBM Enterprise COBOL and Micro Focus both fall back to FLOATING POINT
past the fixed capacity, and GnuCOBOL has no boundary at all because `cob_decimal` is GMP arbitrary-precision.
**No surveyed implementation raises a size error merely because the exact power outgrew the carrier**, which is
what ruled out the EC-SIZE-EXPONENTIATION alternative that `CobolDec.Pow`'s precedent would otherwise have
suggested. The cost is that the technique is VALUE-dependent — the same expression is exact or approximate
depending on magnitude — and that is deliberate and documented here rather than left to read as drift.

**⚠ SCALE IS WHY THE EXACT ARM IS RESTRICTED TO A SCALE-0 BASE.** A scale-*s* base to the *n* has scale *s·n*, so
`1.5 ** 30` needs ~36 significant digits before a receiver is considered and there is no compile-time scale to
give the result; a scale-0 base raised to an integer is scale 0 **whatever the exponent**, so the result scale is
known without knowing the exponent's value. A fractional base keeps the approximation arm.
**⚠ AND A NEGATIVE EXPONENT IS THE RECIPROCAL, NOT AN INTEGER.** A first cut returned the exact integer at scale 0
unconditionally and turned `COMPUTE R = 2 ** -2` into **0.0000** instead of 0.2500 — which is why the result
carrier is the SDIDI, whose scale is a run-time fact (`2 ** -2` is the Dec 0.25; the exponent's SIGN is a run-time
fact for a data-item exponent, so no compile-time scale can serve both regimes).

**(b) §8.8.1.2 rule 6 — NOT a choice.** Rule 6's own title is "Native, standard-binary, and standard-decimal
arithmetic", so it binds native `**` exactly as it binds the SDIDI one. **r6a** (a zero base shall have an
exponent greater than zero) and **r6c** (a negative base shall have an integer exponent) are mandatory `shall`
requirements whose violation sets EC-SIZE-EXPONENTIATION, Fatal in Table 14. `CobolDec.Pow` had enforced both
since it was written; every NATIVE arm went straight to `System.Math.Pow` with no rule-6 check, so the same
program answered differently depending only on whether an ARITHMETIC clause was present — `0 ** 0` returned
**1** (IEEE's convention, not COBOL's) and `-2 ** 0.5` returned **0** (`Math.Pow` yields NaN and the quantizer
turned it into zero), both silently. One screen, `CheckPowRule6`, now runs on every native arm.
**r6b** is a selection rule, not a screen ("if the evaluation yields both a positive and a negative real number,
the value returned is the positive number") and cannot arise here — both bodies yield a single value. Checked in
the same pass rather than left to be discovered as a fourth leg.

**⛔ THE VALUE PAST THE CARRIER RIDES THE SDIDI, AND THERE IS ONE ARM (kb/Work PB69, 2026-08-18).** `PowNativeInt`
— the Int128-returning literal-exponent twin — is deleted. Its past-the-carrier fallback SATURATED to
`Int128.MaxValue`, which is safe above a STORE's capacity check and poison at every value-semantics consumer:
`FUNCTION MOD(A ** 3, B)` answered from the sentinel (the same number for A³ and A⁴), `A ** 4 > A ** 3` was FALSE and
`A ** 3 = A ** 4` TRUE, `A ** 3 / A ** 2` was 1.7e8, and `A ** X` (the Dec arm) gave a third number. Now every
integer power renders through `PowNativeIntDec`: the exact `Int128` loop while it fits — `CobolDec.From` keeps the
full significand, so a 38-digit power is exact on the SDIDI too — and the double approximation past it, on the ONE
carrier that holds both and owns its scale at run time. The consumers follow: a relation compares Decs exactly
(`CobolDec.Compare`); a NATIVE arithmetic operation with a Dec operand evaluates on the SDIDI (`CombineCore` — it
used to land the Dec into Int128 at the operation's static scale, which truncated `2 ** -2 + 1` to 1 and did not
compile for `*` and `/`; the float lane keeps precedence when a float operand or receiver is present); an intrinsic
with a Dec RAW argument routes to its SDIDI body under native too (`IntrinsicRenderer.RenderNum` — MOD/REM keep
exact integers exact through `ModDec`/`RemDec`'s integer fast path); an integer argument lands at scale 0 directly
(`IntArg` reads the RAW operand — the working-scale landing ate the headroom of a 33-digit power); and every
landing of a Dec into the Int128 carrier that cannot hold it — an argument, an aligned operand, a subscript —
raises **EC-SIZE-OVERFLOW** (`CobolDec.ToUnscaledIntermediate`; §14.7.5 case 5, A.1 item 179 "checked") instead of
returning modular low-order digits. The receiver-independent value is therefore: exact when it fits; the
approximation, consistently, when it does not; and a size error only where an Int128 slot cannot represent it.
Cost, documented: a 35–38-digit exact intermediate that only `**` can produce rounds to the SDIDI's 34 digits
inside a native `*` or `/`. **A.1 item 179's "checked" now names three places** (CONFORMANCE.md).

**⛔ THE CONSUMERS OF A RENDERED INTERMEDIATE HAVE ONE LANDING AND ONE STORE (kb/Work PB84/PB85, 2026-08-18).** The
day after PB69 the PB68+PB69 battery went red on NIST NC250A: `IF 9 ** TWO + (180 - 90) IS NOT POSITIVE` was
`CS0019` — the sign condition read the rendered `NumX` as a native carrier (`{v.Expr} > 0`), and so did SET pointer
UP/DOWN BY and ALLOCATE … CHARACTERS (`(long)({x.Expr})`), CALL … BY VALUE arithmetic-expression, and INVOKE … BY
CONTENT arithmetic-expression (`CobolNum.Store(ex.Expr, ex.Scale, …)`); the same five were ALREADY Roslyn errors for
every arithmetic expression under STANDARD-DECIMAL. The two-arm-dispatch shape again: the relation renderer, the
arithmetic store and the MOVE store each had the `Dec` arm, these did not. The rule is now written once:
`NumericRenderer.Landed(x, rcv)` is THE landing of an intermediate into the Int128 lane at the receiver's working
scale (`WorkingScale(SdidiLandingScaleFloor)`, checked — moved out of `IntrinsicRenderer`, which delegates; the
constant was called `NumvalScaleFloor` until kb/Work PB251 showed the name was a fiction — §15.67 prescribes no
working scale, so the floor is an implementor choice for THIS landing and nothing else); `FixedLane`
is the same for a consumer with no float arm of its own (the DIVIDE … REMAINDER kernel — a FLOAT-LONG sender was
snapshotted `Int128 t = double`, PB85 — a native float lands truncated at `FloatWorkingScale`); the sign condition
tests `Int128.Sign(dec.Sig)` (`RuntimeApi.DecSign` — exact at every exponent, never a landing);
`NumericRenderer.StoreArgs`/`StoreExpr` are THE carrier switch at a fixed-point store, spelled once for the
arithmetic store, the numeric MOVE and INVOKE BY CONTENT. **A new consumer of a `NumX` funnels through one of these
or it is wrong for two of the four carriers.** Goldens `pb84_sdidi_intermediate_consumers` and
`pb84_standard_decimal_intermediate_consumers`.

**⛔ EVERY CARRIER'S LANDING PAST THE Int128 CARRIER HAS TWO FORMS, CHOSEN BY THE LANDING — NEVER BY THE VALUE
(kb/Work PB77, 2026-08-18).** The CHECKED landing — an arithmetic store under ON SIZE ERROR / EC-SIZE checking, and
every intermediate consumer with no capacity check downstream (`Align`, an argument) — SATURATES so the receiver's
capacity check raises the size error (PB13's invariant, `ReceiverContext.WorkingScale`'s cap keeping the sentinel
above the receiver). The UNCHECKED landing — a MOVE (§14.6.8.2 r4 "truncation on either end"; there is no size
error in a MOVE), the no-phrase arithmetic store (§14.6.13.1.3 item 8 — CONFORMANCE.md item 70: the resultant takes
the LOW-ORDER digits), INVOKE BY CONTENT — keeps the low-order digits, because it has no check to see a sentinel and
truncating a sentinel stores garbage: `MOVE FUNCTION NUMVAL-F("5E+30") TO PIC V9(9)` stored 884105727 and
`COMPUTE X5 = <COMP-2 1.0E+40>` stored 03715, the low digits of `Int128.MaxValue`. The SDIDI carrier had the two
forms since PB74 (`CobolDec.ToUnscaledChecked` / `ToUnscaled`); the pair now exists for the native exact family
(`CobolIntrinsics.Rescaled(…, checkedLanding)` — the unchecked arm is `CobolNum.RescaleStoreCap`, digits a ≤38-digit
store could never use dropped BEFORE the multiply) and the float family (`CobolFloat.ToScaled` checked /
`ToScaledUnchecked` unchecked — inside the carrier ONE function, `RoundScaled`; past it `CobolFloat.LowOrderDigits`,
the double's exact ±m·2^e expansion at the landing scale rounded by the ONE `CobolNum.RoundDiv` kernel over
`BigInteger`, the runtime's second and last cold BigInteger path; `CobolIntrinsics.FromDouble(…, checkedLanding)`
likewise). The emitter NAMES the landing at every site — `RuntimeApi.FloatToScaled(…, bool checkedLanding)` has no
default; `NumericRenderer.StoreArgs(…, checkedLanding)` takes it from `ecState.SizeErrVar` (arithmetic) or `false`
(`StoreExpr`: MOVE, INVOKE); the quantizer / NUMVAL-family renders carry the ONE `NumericRenderer.CheckedFlag`
(`, checkedLanding: true` under `ReceiverContext.InSizeError`). `CarrierLandingFormTests` pins the kernels and
greps the sites; golden `pb77_move_past_the_carrier` pins sixteen spec-derived rows. **In-carrier landings did not
move** — the in-carrier binary64 product's own rounding is the documented conversion manner (CONFORMANCE.md §3),
and whether it should become the exact expansion everywhere is kb/Work PB90.

**A size error that ESCAPES an arithmetic statement is a fatal exception condition, not a crash (kb/Work PB75,
2026-08-18).** Every `CobolSizeError` raise site — the SDIDI decimal128 range check (`CobolDec.Clamp`, §8.8.1.5.2
r2), `CobolDec.Pow`'s bounds, the PROHIBITED-inexact intermediate, `CobolNum.RescaleEscape`'s Int128 escape
boundary (PB69), the zero divisors, `IntegerOfBoolean`'s width — can fire OUTSIDE an arithmetic statement (a
condition, a DISPLAY / function argument, a subscript, an INVOKE argument, or a no-phrase statement whose checking
is off), where nothing caught it: `IF 10 ** 100000 > 5` under STANDARD-DECIMAL was an unhandled stack trace, exit
127. `CobolSizeError` now derives from `CobolFatalException`; the EC design's D9–D12 machinery gives a checked
non-arithmetic statement the USE / PERFORM-WHEN dispatch (§14.7.5 no-phrase rules → §14.6.13.1.3 #4/#5) and the
run-unit boundary terminates the rest loudly (#7 / #8) — the disposition table in COBOLNET_CONDITIONS_EXCEPTIONS
_DESIGN.md. Arithmetic statements' own `catch (CobolSizeError)` (the phrase; `EmitArith`) is unchanged and takes
precedence (#1).

**Rejected alternatives.** (a) Raise EC-SIZE-EXPONENTIATION when the exact result does not fit — REJECTED by the
owner on the survey: principled, and matched by no shipping COBOL, so `1.5 ** 30` would start raising where every
other compiler approximates. (b) Keep `Math.Pow` everywhere and reword D3 — REJECTED: it leaves `10 ** 30` wrong
against a carrier that holds it exactly, and it leaves PB32's receiver-shape defect with no route to a fix, since
that closes only when `**` gains an exact integer arm.

**What this closed beyond itself.** PB32's remaining half. `Power` returned `Real: true` in a receiver-less
context, so `A ** 2` was exact under `COMPUTE` and binary64 under `DISPLAY`/an `IF` subject — which routed
`FUNCTION MOD` to a DIFFERENT BODY and produced 930000008 against 930000007, making
`IF FUNCTION MOD(A ** 2, B) = 930000007` evaluate FALSE. Testing the OPERANDS before the receiver restores §15.4's
rule that a function's value must not depend on the shape of its receiver. Pinned by
`2023/pb18_native_power_exact_and_rule6`.
