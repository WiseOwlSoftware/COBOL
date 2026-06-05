# CobolSharp — Spec-Conformance Fix Backlog (WS-SPEC round 1)

Generated 2026-06-04 from the WS-SPEC authoring workflow (9 module agents authored 57 verified-passing
conformance tests for under-tested live COBOL-85 features; these are the **30 features that did NOT pass** —
genuine compiler bugs/gaps, each diagnosed with a spec citation and a code location). This is the next
implementation round. Author the passing test alongside each fix (the agents have the test shape ready).

Priority tags: **P0** = correctness bug likely affecting many programs; **P1** = real feature gap; **P2** =
strictness/edge.

## Nucleus
- ✅ **DONE (DEVLOG 333) — `DISPLAY … WITH NO ADVANCING`** was a silent no-op. §14.9.11. Fixed by threading a
  `NoAdvancing` flag through `BoundDisplayStatement` → `IrPicDisplay` → `CilDataEmitter` (Console.Write vs
  WriteLine). Test: `NucleusSpecTests.Display_WithNoAdvancing_SuppressesNewline`.
- **P1 — `INITIALIZE … WITH FILLER`** parsed but not honored: FILLER items are not reset. §14.9.20 GR 5.a.2.
- **P1 — `INITIALIZE … ALL TO VALUE`** behaves like plain INITIALIZE (category default) instead of resetting to
  each item's VALUE clause. §14.9.20 GR 5.c.1.

## Intrinsic Functions
- **P1 — `REPOSITORY … FUNCTION … INTRINSIC`** paragraph not parsed (parse error) → keyword-optional FUNCTION
  dispatch can't be exercised.
- **P1 — ref-mod on a function result** `FUNCTION REVERSE(x)(1:3)` — grammar rejects the trailing `(1:3)`.
- ✅ **DONE (DEVLOG 337) — ref-modded data item AS a function argument**: `ExpressionLowerer` now lowers a
  `BoundReferenceModificationExpression` arg to `IrAlphanumericArg` (ISO §8.4.2.3 — always alphanumeric). Test:
  `SpecFixTests.RefModdedAlphanumericFunctionArg_ReadsAsString`.
- **P0 — table-ref `ALL` over an ODO table** ranges over OCCURS MAXIMUM, not the DEPENDING-ON current value
  (`FUNCTION SUM(T(ALL))` summed all 5 slots, not the active 3). §15.3.
- ✅ **DONE (DEVLOG 334) — `CONCAT`** added to `BindingContext.AlphanumericFunctions` (was typed numeric →
  InvalidCastException). Test: `SpecFixTests.Concat_IsAlphanumeric_ReturnsConcatenation`.
- ✅ **DONE (DEVLOG 335) — variadic string fns with SPACE-separated literal args**: added SUB_STRINGLIT/
  SUB_DECIMALLIT to `SplitSubscriptTokens` split triggers. Test: `SpecFixTests.Concatenate_SpaceSeparatedLiteralArgs_PassesAll`.
- **P1 — `HIGHEST-ALGEBRAIC`/`LOWEST-ALGEBRAIC`** return 0 — runtime expects a digit count but the binder passes
  the item VALUE; must derive the range from the argument PICTURE. §15.43/§15.58.

## Data Division
- ◐ **PARTIAL (DEVLOG 338) — `USAGE COMP-1`/`COMP-2` arithmetic** no longer truncates the fraction
  (`StoreArithmeticResult` bypasses scaling for float receivers, mirroring the MOVE guard). Test:
  `SpecFixTests.Compute_IntoFloatReceiver_PreservesFraction`. **Still TODO:** raw `DISPLAY` of a COMP-2 renders
  as an 18-digit integer (a separate float-DISPLAY-formatting fix in `GetDisplayString`).
- ✅ **DONE (DEVLOG 334) — `BLANK WHEN ZERO`** zero value now renders a PICTURE-width blank field (was `TrimEnd`-ed
  to ""). Test: `SpecFixTests.BlankWhenZero_ZeroValue_RendersBlankField`.
- **P2 — `USAGE COMP-4`** has no lexer token (accidentally accepted via lenient fall-through; invalid `COMP-9`
  also "compiles"). Add a real COMP-4≡BINARY token and reject unknown `COMP-n`.

## Environment Division
- **P0 — multi-character `CURRENCY SIGN` truncated to one char** during editing (`CURRENCY SIGN IS 'EUR ' WITH
  PICTURE SYMBOL '@'`, `PIC @99.99` value 10.00 → "E10.00", expected "EUR 10.00"). §D.14.2.3.
- ✅ **DONE (DEVLOG 334) — `CURRENCY … WITH PICTURE SYMBOL`** now rejects only the spec-forbidden letters
  (A,B,C,D,E,N,P,R,S,V,X,Z); other letters (U,M,Q,…) accepted. Test: `SpecFixTests.CurrencySign_NonReservedLetterSymbol_IsAccepted`.

## Source Text (COPY/REPLACE)
- **P1 — `COPY … REPLACING LEADING/TRAILING ==…==`** partial-word substitution: no LEADING/TRAILING handling;
  `ApplyReplacements` only whole-word matches. §7.2.3 GR 9 b.
- ✅ **DONE (DEVLOG 336) — `COPY "literal"`** quoted-literal text-name: added `ReadTextNameOrLiteral` (strips
  quotes) for the text-name + IN/OF library-name. Test: `SpecFixTests.Copy_QuotedLiteralTextName_Resolves`.
- **P1 — `REPLACE … ALSO` / `REPLACE OFF LAST`** LIFO queue not implemented (every REPLACE clears all; `ALSO`
  mis-parsed; `OFF LAST` cancels all instead of popping one). §7.2.4 GR 4.

## Inter-Program Communication
- **P0 — `CALL … RETURNING identifier` rejected when the target is in WORKING-STORAGE/LOCAL-STORAGE**
  (`BoundTreeValidator.ValidateCall` ~385-388 emits CBL3304). Per §14.9.4.3 SR 7 the *caller's* RETURNING target
  is unrestricted; the LINKAGE restriction applies only to the *callee's* PROCEDURE DIVISION RETURNING item.
- **P1 — `CALL … USING BY VALUE arithmetic-expression`** crashes (ArgumentNullException in PicRuntime) — the temp
  holding the computed value is never materialized/passed.

## Sort-Merge
- **P1 — varying-length record SORT** (`RELEASE`/`RETURN` through a VARYING SD) does not preserve each record's
  released length (all returned at the last/max length). §14.9.40.4.
- **P1 — varying-length record MERGE** (USING+GIVING) same — GIVING records get the max length, not source. §14.9.24.4.
- **P1 — SORT Format-2 (table) with an elementary OCCURS item as both table and key** → runtime
  InvalidOperationException (key length applied to the wrong buffer). Group-element table sort works.

## Sequential/Relative/Indexed I-O
- **P1 — `READ … PREVIOUS RECORD` boundary rules** wrong: (1) READ PREVIOUS right after OPEN INPUT must raise AT
  END (returns highest key instead); (2) after `START KEY = EQUAL k`, first READ PREVIOUS must return key ≤ k
  (skips the equal key). §14.9.30 GR.
- **P1 — cross-program `USE GLOBAL` + GLOBAL FD inheritance for indexed I-O**: nested program doesn't share the
  outer GLOBAL FD connector (CBL0702 file-not-open) instead of propagating INVALID KEY. Single-program USE works.

## Report Writer  → feeds **WS-SPEC-RW (task #7)**
- **P1 — `TYPE REPORT HEADING`/`REPORT FOOTING` (RH/RF)** never presented (FileIoLowerer registers only
  Page heading/footing; INITIATE/TERMINATE don't emit RH/RF).
- **P1 — `CONTROL` + `CONTROL HEADING`/`FOOTING` + `SUM`** — no control-break detection and no SUM accumulator in
  `ReportWriterRuntime.EmitGroup` (the hierarchy is in the symbol model but never honored). *The headline RW gap.*
- **P1 — `SOURCE` of arbitrary WS data in a PAGE HEADING/FOOTING** — `FileIoLowerer.ClassifyPageField` only
  composes VALUE literals + LINE/PAGE-COUNTER; a data SOURCE falls through to blank-fill.
- **P1 — `SOURCE LINE-COUNTER` (special register) inside a DETAIL group** — `LowerGenerate` skips fields whose
  Source resolves to null (the special-register path exists only for page groups).
- **P1 — `VALUE` literal field inside a DETAIL group** — `LowerGenerate` (~310-315) skips fields with no `Source`,
  dropping detail VALUE literals. (NIST RW102A exercises this but is baselined only on its DISPLAY audit.)
