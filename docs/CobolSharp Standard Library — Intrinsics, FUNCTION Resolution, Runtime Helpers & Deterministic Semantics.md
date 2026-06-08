CobolSharp Standard Library — Intrinsics, FUNCTION Resolution, Runtime Helpers & Deterministic Semantics
=======================================================================================================

> **STATUS (2026-06-07):** Design reference for the intrinsic-function / standard-library subsystem.
> **Implementation status: LARGELY IMPLEMENTED (~90%+).** ~94 intrinsic FUNCTIONs are implemented and
> dispatched. The real implementation lives in `src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs`
> (~900 lines) as `public static class IntrinsicFunctions` — a flat set of `static` methods over
> `decimal`/`string`/`int` (NOT a runtime object `ExecutionContext.FunctionLibrary` as the older essays
> imagined; that name is design fiction). The compile path is: parser → `ExpressionBinder` (binds the
> `FUNCTION name(args)` call, validates argument shape, resolves return type; `FUNCTION LENGTH` and the
> few constant cases are folded at bind time) → `IrIntrinsicCall` IR node → `CilExpressionEmitter`
> emits a `call` to the matching `IntrinsicFunctions.*` static method.
>
> **Stack: .NET 10 / C# 14.** Backend is **CIL-only via Mono.Cecil** (no custom VM / no bytecode
> interpreter; a Roslyn C# backend is a *future* additive option — Stage-5, Cecil = oracle). The data
> model is migrating to typed-native (`char→string`, `numeric→long/decimal`, `groups→record struct`,
> `OCCURS→T[]`, `pointers→ManagedPointer`) behind `EnableTypedFields` (default OFF). The numeric
> substrate is `CobolNum`/`CobolDecimal`; the byte/`StorageBlock` engine is being **islanded**.
> Sections below that say "byte length", "packed decimal", "ASCII", or "ExecutionContext.FunctionLibrary"
> describe the byte-engine view and should be read as the islanded fallback, not the typed-native target.
>
> **NOT YET / partial** vs the design below: BIT-AND/OR/XOR/NOT (bitwise intrinsics), RANDOM-SEED as a
> separate intrinsic, MODE/RANGE statistical extras, and the `DecimalMath.*` arbitrary-precision helper
> tier are aspirational — verify any specific function against `IntrinsicFunctions.cs` before claiming it.
> Domain errors (SQRT(-1), LOG(≤0), ACOS(|x|>1)) currently map to the **EC-ARGUMENT-FUNCTION** defined
> result (0) rather than raising SIZE ERROR — see "Edge-case behavior", which supersedes the older
> "SIZE ERROR" wording.
>
> Plan SSOT: **`docs/MASTER_PLAN.md`**. Doctrine: `PROMPT.md`. Numeric data-model context:
> `docs/DATA_MODEL_ARCHITECTURE.md`.
> *Consolidated from 3 prior architecture docs (2026-06-07): "Standard Library — Intrinsics…" (base),
> "FUNCTION, Intrinsics & Runtime Library Architecture", and "Intrinsic Functions, Type Conversion &
> Runtime Library Architecture".*

Purpose
-------
Define the authoritative architecture for:
- COBOL intrinsic functions (FUNCTION xxx)
- Deterministic numeric, string, date/time, and statistical intrinsics
- Intrinsic resolution and overload selection
- Type-conversion rules across usages (DISPLAY, NATIONAL, COMP, COMP‑3, COMP‑5)
- Runtime helper library (`CobolSharp.Runtime.Intrinsics`)
- Deterministic semantics across CoreCLR, AOT, and WASM
- CIL‑friendly lowering
- Error handling and EC‑ARGUMENT‑FUNCTION / ExceptionState integration

This document governs how CobolSharp implements the COBOL standard library and intrinsic functions.

------------------------------------------------------------
SECTION 1 — INTRINSIC FUNCTION OVERVIEW
------------------------------------------------------------

CobolSharp implements the ISO/IEC 1989:2023 intrinsic functions (§15). Coverage by category:
- Numeric intrinsics (ABS, SQRT, INTEGER, INTEGER‑PART, FRACTION‑PART, REM, MOD, EXP, EXP10, LOG, LOG10,
  FACTORIAL, the trig family SIN/COS/TAN/ASIN/ACOS/ATAN, SUM/MAX/MIN over operands or tables)
- String intrinsics (LENGTH, TRIM, REVERSE, UPPER‑CASE, LOWER‑CASE, CONCATENATE, SUBSTITUTE,
  SUBSTITUTE‑CASE)
- Date/time intrinsics (CURRENT‑DATE, WHEN‑COMPILED, DATE‑OF‑INTEGER, DAY‑OF‑INTEGER,
  INTEGER‑OF‑DATE, INTEGER‑OF‑DAY, and the day‑of‑year/weekday helpers)
- Statistical intrinsics (MEAN, MEDIAN, MIDRANGE, RANGE, VARIANCE, STANDARD‑DEVIATION; MODE — design)
- Conversion intrinsics (NUMVAL, NUMVAL‑C, ORD, ORD‑MAX, ORD‑MIN, CHAR)
- Boolean intrinsics (BOOLEAN‑OF‑INTEGER, INTEGER‑OF‑BOOLEAN)
- Random intrinsics (RANDOM; RANDOM‑SEED — design)
- National‑character intrinsics (NATIONAL‑OF, DISPLAY‑OF)
- System/environment intrinsics (COMMAND‑LINE, ARGUMENT‑VALUE, ARGUMENT‑NUMBER, LOCALE family) — design
- Bitwise intrinsics (BIT‑AND, BIT‑OR, BIT‑XOR, BIT‑NOT) — design

All intrinsics are pure functions: deterministic, side‑effect free, AOT/WASM‑safe.

> Authoritative coverage list = the methods actually present in
> `src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs`. The bullets marked "— design" above are
> not yet implemented; do not assume them present.

------------------------------------------------------------
SECTION 2 — FUNCTION RESOLUTION
------------------------------------------------------------

2.1 Compile‑time resolution
---------------------------
The binder (`ExpressionBinder`) resolves, for each `FUNCTION name(arg1, arg2, …)`:
1. Normalize the name (case‑insensitive).
2. Look it up in the intrinsic-function table.
3. Validate argument count.
4. Validate argument types/categories.
5. Determine the return type.
6. Annotate the bound node with the function metadata (becomes an `IrIntrinsicCall`).

2.2 No runtime lookup
---------------------
All intrinsics are bound statically — there is no runtime name dispatch / reflection. Each resolved
call lowers to a direct `call` of a `static` method.

2.3 Overload resolution
-----------------------
Some functions have multiple signatures (e.g. LENGTH, ORD/CHAR, NUMVAL/NUMVAL‑C, TRIM with
LEADING/TRAILING/BOTH, MAX/MIN/SUM over operands vs. a `table(ALL)`). Selection is by:
- Numeric category (integer, decimal, floating)
- String category (DISPLAY, NATIONAL)
- Parameter count / optional parameters
CobolSharp selects the most specific match.

2.4 Constant folding
--------------------
- `FUNCTION LENGTH` is computed at bind time (ISO §15.24 — number of character positions in the
  argument; a reference-modified operand `x(s:l)` has length `l`). Variable-length groups follow
  §15.50.4 rules 4(b)/7 (use the current DEPENDING value).
- When *all* arguments are literals, an intrinsic may be evaluated at compile time and replaced with a
  literal result (with overflow / invalid-argument detection); otherwise a runtime call is emitted.

2.5 Error cases (compile time)
------------------------------
- Unknown intrinsic → compile‑time error.
- Wrong number of arguments → compile‑time error.
- Wrong argument type/category → compile‑time error.

------------------------------------------------------------
SECTION 3 — FUNCTION ARGUMENT EVALUATION
------------------------------------------------------------

3.1 Left‑to‑right evaluation
----------------------------
Arguments are evaluated left to right, before the function call, with full `decimal` precision for the
numeric path.

3.2 BY VALUE only
-----------------
Intrinsic functions always receive BY VALUE arguments — no aliasing of caller storage.

3.3 Optional / repeating arguments
----------------------------------
Some functions allow omitted arguments with default values (e.g. TRIM mode) and some accept a variadic
operand list or a `table(ALL)` reference. The `table(ALL)` form (ISO §15.4) is expanded by the binder
to pass every occurrence; for an OCCURS DEPENDING ON table, `SUM`/`MEAN`/etc. range over the *current*
depending value (SUM-scoped, §15.50.4).

------------------------------------------------------------
SECTION 4 — NUMERIC INTRINSICS
------------------------------------------------------------

4.1 ABS — `ABS(x) → |x|`. Decimal-based (`Math.Abs`).

4.2 SQRT — `SQRT(x)`; `x < 0` → EC‑ARGUMENT‑FUNCTION (defined result 0; see §15).

4.3 INTEGER / INTEGER‑PART / FRACTION‑PART
- `INTEGER(x)` = `Math.Floor(x)` (largest integer ≤ x).
- `FRACTION‑PART(x)` = `x − INTEGER‑PART(x)`.
(Note: INTEGER floors toward −∞; INTEGER‑PART truncates toward zero — see the spec and the
implementation for the exact pair.)

4.4 REM / MOD
- `REM(a, b)` = remainder with the sign of the dividend `a` (`a − b·trunc(a/b)`).
- `MOD(a, b)` = remainder with the sign of the divisor `b` (`a − b·floor(a/b)`).

4.5 EXP / EXP10 / LOG / LOG10
- `EXP(x)` = eˣ, `EXP10(x)` = 10ˣ, `LOG(x)` = natural log, `LOG10(x)` = base‑10 log.
- `LOG`/`LOG10` of `x ≤ 0` → EC‑ARGUMENT‑FUNCTION (defined result 0).
- Implemented via `System.Math` on `double` then `FromDouble` (NaN→0, ±Inf/overflow→clamp to
  `decimal.Max/Min`).

4.6 Trigonometric — SIN, COS, TAN, ASIN, ACOS, ATAN; out-of-domain (e.g. `ACOS(|x|>1)`) → 0.

4.7 FACTORIAL — `n < 0` → 0 (EC‑ARGUMENT‑FUNCTION); `n ≥ 28` → `decimal.MaxValue` (28! overflows the
decimal range).

4.8 RANDOM — deterministic PRNG seeded per ExecutionContext (see §9).

4.9 SUM / MAX / MIN — over an operand list or an OCCURS `table(ALL)`; mixed numeric operands promote to
decimal; mixed string is illegal.

------------------------------------------------------------
SECTION 5 — STRING INTRINSICS
------------------------------------------------------------

5.1 LENGTH — number of character positions (ISO §15.24).
- DISPLAY (byte engine): byte/character count of the (islanded) byte image.
- NATIONAL: UTF‑16 character count (national strings already migrate toward typed `string`).
Computed at bind time where the operand's length is statically known.

5.2 TRIM — `TRIM(x)` removes trailing spaces; `TRIM(x, LEADING)` leading; `TRIM(x, BOTH)` both.

5.3 REVERSE — reverses *characters*, not bytes; NATIONAL is surrogate-pair safe.

5.4 UPPER‑CASE / LOWER‑CASE — DISPLAY uses the program collating sequence / invariant case mapping;
NATIONAL uses Unicode case mapping. Deterministic, locale-independent.

5.5 CONCATENATE — joins operands into one alphanumeric/national result.

5.6 SUBSTITUTE / SUBSTITUTE‑CASE — `SUBSTITUTE(a, b, c)` replaces `b` with `c` in `a`. SUBSTITUTE is
case-sensitive; SUBSTITUTE‑CASE is case-insensitive. Matching is left-to-right and non-recursive (a
just-substituted region is not re-scanned).

------------------------------------------------------------
SECTION 6 — CHARACTER-CODE & COLLATING INTRINSICS
------------------------------------------------------------

6.1 ORD / CHAR (collating-sequence aware)
- `FUNCTION CHAR(n)` = the character in ORDINAL POSITION `n` of the **alphanumeric program collating
  sequence** (1-based).
- `FUNCTION ORD(c)` = the 1-based ordinal position of `c` in the alphanumeric program collating
  sequence. (Both honor the active program collating sequence — see the collating subsystem.)

6.2 ORD‑MAX / ORD‑MIN — ordinal of the largest / smallest argument under the collating sequence.

------------------------------------------------------------
SECTION 7 — DATE/TIME INTRINSICS
------------------------------------------------------------

7.1 CURRENT‑DATE — returns `YYYYMMDDHHMMSShh±ZZZZ` (date, time to hundredths, and the local time-zone
offset). Sourced from a single `DateTimeProvider` so tests can pin it.

7.2 WHEN‑COMPILED — the timestamp of compilation.

7.3 INTEGER‑OF‑DATE — `YYYYMMDD` → integer days since the Gregorian epoch.

7.4 DATE‑OF‑INTEGER — inverse of INTEGER‑OF‑DATE.

7.5 DAY‑OF‑INTEGER / INTEGER‑OF‑DAY — the same conversions in day-of-year (`YYYYDDD`) form.

All date math uses Gregorian-calendar rules with explicit leap-year logic, no locale dependence;
invalid date arguments map to EC‑ARGUMENT‑FUNCTION (defined result 0).

------------------------------------------------------------
SECTION 8 — STATISTICAL INTRINSICS
------------------------------------------------------------

- MEAN — arithmetic average (decimal accumulation).
- MEDIAN — middle value after a stable sort.
- RANGE — `max − min`.
- MIDRANGE — `(max + min) / 2`.
- VARIANCE / STANDARD‑DEVIATION — population statistics.
- MODE (design) — most frequent value.

Implemented with decimal accumulators and stable sorting for determinism. (Kahan summation is the
intended stabilizer for large operand sets.)

------------------------------------------------------------
SECTION 9 — CONVERSION, BOOLEAN, RANDOM & BITWISE INTRINSICS
------------------------------------------------------------

9.1 NUMVAL / NUMVAL‑C
- `NUMVAL(x)` converts a numeric DISPLAY string to decimal (strict parsing; leading/trailing spaces
  ignored; sign allowed).
- `NUMVAL‑C(x)` additionally handles currency symbols and digit-group commas.
- Invalid input → EC‑ARGUMENT‑FUNCTION / runtime exception per the routing in §13.

9.2 NATIONAL‑OF / DISPLAY‑OF
- `NATIONAL‑OF(x)`: DISPLAY → NATIONAL (UTF‑16).
- `DISPLAY‑OF(x)`: NATIONAL → DISPLAY (errors on non‑representable characters).

9.3 BOOLEAN‑OF‑INTEGER / INTEGER‑OF‑BOOLEAN
- `0 → FALSE`, non‑zero → TRUE; `FALSE → 0`, `TRUE → 1`.

9.4 RANDOM / RANDOM‑SEED
- `RANDOM()` uses a deterministic PRNG (intended: Xoshiro256**), seeded per ExecutionContext; default
  seed = 1 when none is supplied.
- `RANDOM‑SEED(x)` (design) sets the PRNG seed.

9.5 Bitwise (design) — BIT‑AND / BIT‑OR / BIT‑XOR over integer values or bitstrings; BIT‑NOT unary.

------------------------------------------------------------
SECTION 10 — TYPE-CONVERSION RULES (across usages)
------------------------------------------------------------

> Byte-engine view; under typed-native (`EnableTypedFields` ON) DISPLAY numeric and national are already
> `long`/`decimal`/`string`, so several of these conversions collapse to identity. The packed-decimal
> path is the islanded fallback.

- DISPLAY numeric → decimal: parse digits (byte engine = ASCII), ignore leading/trailing spaces, allow
  sign / overpunch.
- NATIONAL numeric → decimal: parse UTF‑16 digits, same rules as DISPLAY.
- COMP / COMP‑5 → decimal: binary integer widened to decimal.
- COMP‑3 → decimal: packed decimal unpacked, sign nibble validated.
- decimal → DISPLAY: formatted per the receiving PIC (truncation/rounding applied on store).
- decimal → NATIONAL: rendered as UTF‑16 digits.
- DISPLAY → NATIONAL: widen to UTF‑16; NATIONAL → DISPLAY: narrow (error if not representable).

------------------------------------------------------------
SECTION 11 — RUNTIME HELPERS & IMPLEMENTATION
------------------------------------------------------------

11.1 Where it lives
-------------------
`src/CobolSharp.Runtime/Intrinsics/IntrinsicFunctions.cs` — `public static class IntrinsicFunctions`,
one `static` method per FUNCTION keyword, signatures over `decimal` / `string` / `int`. There is **no**
`ExecutionContext.FunctionLibrary` object; the older essays' "FunctionLibrary" / per-engine
("NumericEngine", "StringEngine", "DateTimeEngine") split is design fiction — the real surface is the
flat static class plus the collating subsystem (for ORD/CHAR) and the `DateTimeProvider`.

11.2 Numeric operations
-----------------------
`System.Decimal` for the core path; `System.Math` (on `double`, via `FromDouble`) for transcendental
functions; checked/clamped conversions for overflow. (An arbitrary-precision `DecimalMath.Sqrt/Exp/Log`
helper tier is a future option for full-precision transcendental results.)

11.3 String operations
----------------------
UTF‑16 (`string` / `Span<char>`) with low-allocation slicing; surrogate-pair-safe reverse/trim; case
mapping via invariant / Unicode rules. AOT/WASM-safe.

11.4 Date/time operations
-------------------------
`System.DateTime` / `System.TimeSpan` via a single `DateTimeProvider`; UTC-based math; ISO-compliant
date arithmetic; deterministic for tests.

11.5 Statistical operations
---------------------------
Decimal accumulators + stable sorting.

------------------------------------------------------------
SECTION 12 — CIL LOWERING RULES
------------------------------------------------------------

12.1 Call lowering
------------------
`FUNCTION name(args)` → bound node → `IrIntrinsicCall(name, args)` → `CilExpressionEmitter` emits
`call CobolSharp.Runtime.Intrinsics.IntrinsicFunctions.<Method>(…)`.
Example: `COMPUTE x = FUNCTION ABS(y)` → evaluate `y` → `call IntrinsicFunctions.Abs` → store `x`.

12.2 Argument lowering
----------------------
Arguments are lowered to the required CLR type (decimal / string / bool / int) using per-argument
temporary locals; a temporary holds the return value before the store-to-target conversion.

12.3 Constant-folding lowering
------------------------------
If folded (e.g. LENGTH, all-literal calls), the call is replaced by a literal and no runtime call is
emitted.

12.4 Return-value lowering
--------------------------
The helper's return is converted to the target PIC / receiving item on store.

------------------------------------------------------------
SECTION 13 — EXCEPTION HANDLING
------------------------------------------------------------

13.1 Intrinsic error sources
----------------------------
Domain errors (SQRT(‑1), LOG(≤0), ACOS(|x|>1)), overflow, invalid character, non‑representable
DISPLAY/NATIONAL conversion, invalid date arguments.

13.2 Current behavior & routing
-------------------------------
- Domain / argument errors map to the **EC‑ARGUMENT‑FUNCTION** condition with the ISO *defined result*
  (0 for the math cases) rather than aborting — see `FromDouble`/`Factorial`/date guards in the
  implementation. This supersedes the older "SIZE ERROR" wording in the legacy essays.
- The intended full routing order, as the EC exception subsystem lands (Phase C): (1) ON EXCEPTION
  phrase → (2) applicable USE/declarative → (3) ExceptionState / EC condition.

------------------------------------------------------------
SECTION 14 — AOT/WASM-SAFE & DETERMINISTIC SEMANTICS
------------------------------------------------------------

- No floating-point nondeterminism on the core path — numeric semantics are decimal; transcendental
  functions go through `double` only inside `FromDouble`, with clamped, deterministic results.
- No locale-dependent behavior — case mapping and numeric parsing are invariant.
- No reflection — all intrinsics are statically bound.
- All helpers are pure managed code → WASM-compatible; works across CoreCLR, AOT, and WASM.

------------------------------------------------------------
SECTION 15 — EDGE-CASE BEHAVIOR
------------------------------------------------------------

- SQRT(‑1), LOG(0 or negative), ACOS(|x|>1): EC‑ARGUMENT‑FUNCTION → defined result 0 (NOT a fatal
  SIZE ERROR in the current engine).
- FACTORIAL(negative) → 0; FACTORIAL(≥28) → `decimal.MaxValue`.
- LENGTH of empty string → 0; LENGTH of NATIONAL → character count, not bytes.
- TRIM / REVERSE of NATIONAL with surrogate pairs → never splits a pair; a surrogate pair counts as one
  character.
- NUMVAL of an invalid numeric → EC‑ARGUMENT‑FUNCTION / runtime error.
- CHAR of an out-of-range / invalid ordinal → EC‑ARGUMENT‑FUNCTION / runtime error.
- SUBSTITUTE with overlapping patterns → left-to-right, non-recursive.
- RANDOM with no prior RANDOM‑SEED → ExecutionContext default seed (= 1).
- SEARCH‑ALL on an empty string → zero occurrences.

------------------------------------------------------------
SECTION 16 — DEBUGGER INTEGRATION (design — Phase E)
------------------------------------------------------------

When the debugger lands, it surfaces per intrinsic call: function name, argument values, return value,
type conversions, constant-folded vs. runtime evaluation, and any EC‑ARGUMENT‑FUNCTION / ExceptionState.
Sequence points are emitted at function-call start and end.

------------------------------------------------------------
Summary
------------------------------------------------------------
The CobolSharp standard library:
- Implements the ISO/IEC 1989:2023 intrinsic-function set (~94 dispatched) as flat `static` methods in
  `CobolSharp.Runtime.Intrinsics.IntrinsicFunctions`, bound statically and lowered to direct CIL `call`s
  via `IrIntrinsicCall` (Mono.Cecil backend — no custom VM).
- Provides deterministic, locale-independent numeric / string / date-time / statistical / conversion /
  boolean semantics, with collating-aware ORD/CHAR and a single pinnable DateTimeProvider.
- Uses pure managed helpers for AOT/WASM safety.
- Maps argument-domain errors to EC‑ARGUMENT‑FUNCTION defined results rather than aborting.
- Migrates with the typed-native data model (numeric → CobolNum/CobolDecimal, national → string).
