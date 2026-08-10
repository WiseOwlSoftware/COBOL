# PB60's last three rows — apply plan (drafted through the PB60-batch battery freeze, 2026-08-09)

## RV-15.67.4-1(a) — the SDIDI Dec branch (STANDARD-DECIMAL exactness)
Derivation: §15.4.1 (SDIDI temporary under the standard modes) + §8.8.1.5.2 (34-digit precision).
The one-scan makes it trivial:
1. Runtime: `NumvalDec/NumvalCDec/NumvalFDec` — project NvScan/NvfScan to the SDIDI carrier at the
   PARSED scale exactly: `CobolDec.At(neg ? -unscaled : unscaled, frac [+ exp for F])` (find CobolDec's
   actual factory — grep `CobolDec.From|CobolDec.At` — the PB65 Dec-carrier intrinsics established the
   pattern). digitCap 34 arrives from the emitter as today.
2. Renderer: the three "Numval*" RenderNum cases gain the Dec arm the PB65 intrinsics use — when
   `num.StandardDecimal`, emit the *Dec runtime call and return the Dec-shaped NumX (find the exact
   NumX Dec constructor from an existing case, e.g. "Exp" under SD).
3. Probe p08b's SD legs flip: SD-REL=EQ (already fixed by relation threading? re-probe), SD-RAW renders
   1.2345678 (the Dec text channel), the golden gains an OPTIONS ARITHMETIC IS STANDARD-DECIMAL
   program (separate .cob — OPTIONS is per-unit).
4. TEST- twins unaffected (integer verdicts).

## AR-15.68.3-3 — the CURRENCY SIGN SET model
Derivation: §12.3.7.3 r23 (multiple CURRENCY SIGN clauses, each picture-symbol distinct), §15.68.3 r3
(NUMVAL-C default requires exactly ONE currency string for the unit when argument-2/LOCALE absent).
1. DataBinder.Switches#SwitchBindCurrency: the scalar pair (CurrencyString/CurrencyPicSymbol) becomes
   a MAP picSymbol → currencyString; the multi-char literal-6 WITH PICTURE SYMBOL reject (COBOLNET0896
   "not yet supported" — deferral debt) dies; keep the single-char rule for literal-6 WITHOUT a
   PICTURE SYMBOL (it IS the symbol then, §12.3.7.3's own constraint — re-derive exact rule numbers).
2. PICTURE editing reads the SYMBOL (unchanged, single char); the INSERTION of a multi-char currency
   STRING into an edited result is the real feature half — measure what EditFormat does with
   CurrencyPicSymbol today; if multi-char insertion needs EditFormat work, that is IN SCOPE (rule 3)
   but measure first: the r23 model may land with single-symbol insertion intact when only ONE clause
   exists, and the SET model's NUMVAL-C half (r3: >1 clause + no argument-2 → compile error) is the
   row's own demand.
3. BindNumvalCFamily's r3 injection: with >1 currency in the map and no argument-2 → COBOLNET error
   (r3's "there shall be only one"); with exactly one → inject it (today's behavior).
4. Sweep: ctx.Data.CurrencyPicSymbol consumers (MaskScale calls etc.) — the map's DEFAULT symbol
   (the single entry, or '$' with no clause) keeps them working; multi-clause units make
   PICTURE-with-symbol resolution per-symbol (PictureAnalyzer must look UP the symbol in the map).

## AR-15.67.3-5 — DECIMAL-POINT IS COMMA scoping
Derivation: §12.3.4-level (the fleet cites it) — the SPECIAL-NAMES of the OUTERMOST program apply to
contained programs (a contained program may not have a CONFIGURATION SECTION at all — §10-level rule).
Mechanism: DataBinder.Switches#DecimalPointIsComma is per-source-unit; the contained unit re-binds
with defaults. Fix: the switch state INHERITS from the containing program's binder context (find how
contained programs get their BinderContext — the inheritance seam), with the grammar-level rule that
a contained program's own CONFIGURATION SECTION is illegal (check current handling). Affects comma
picture editing + NUMVAL commaMode + literals — measure the blast radius; probably its own commit.
