# M407 Design Document: CURRENCY SIGN WITH PICTURE SYMBOL (PICMODE Refactor)

**Status**: Design complete, awaiting implementation authorization
**Date**: 2026-03-30
**Ledger item**: M407
**Severity**: P1
**Subsystem**: Grammar / Semantics / Runtime

---

## 1. Exact COBOL-85 Requirements

### 1.1 Syntax

Per ISO 1989:1985 14.2.3 (SPECIAL-NAMES paragraph):

```
CURRENCY SIGN IS literal-7 [WITH PICTURE SYMBOL literal-8]
```

### 1.2 Semantic Rules

**Without PICTURE SYMBOL phrase:**
- literal-7 serves as both the currency string (what appears in formatted output) and
  the currency symbol (what appears in PICTURE clauses)
- literal-7 must be a single character
- literal-7 must NOT be: digits 0-9, letters A B C D E N P R S V X Z (or lowercase),
  space, `*` `+` `-` `,` `.` `;` `(` `)` `"` `'` `=`
- Default when no CURRENCY SIGN clause: `$` is both string and symbol

**With PICTURE SYMBOL phrase:**
- literal-7 = the currency string (placed in formatted output at runtime)
- literal-8 = the currency symbol (used in PICTURE clauses at compile time)
- literal-7: single character in COBOL-85, must not be digits 0-9, `+` `-` `,` `.` `*`
- literal-8: single character, must not be digits 0-9, letters A-Z/a-z, space

### 1.3 Valid Examples

**Example 1 -- Basic custom currency (no PICTURE SYMBOL):**
```cobol
SPECIAL-NAMES.
    CURRENCY SIGN IS "L".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-AMOUNT PIC LLL,LL9.99.
    *> PIC uses L, output uses L
    *> Value 1234.56 displays "  L1,234.56"
```

**Example 2 -- Decoupled symbol and output (WITH PICTURE SYMBOL):**
```cobol
SPECIAL-NAMES.
    CURRENCY SIGN IS "L" WITH PICTURE SYMBOL "@".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-AMOUNT PIC @@@,@@9.99.
    *> PIC uses @, output uses L
    *> Value 1234.56 displays "  L1,234.56"
```

**Example 3 -- Fixed currency with PICTURE SYMBOL:**
```cobol
SPECIAL-NAMES.
    CURRENCY SIGN IS "#" WITH PICTURE SYMBOL "!".
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-PRICE PIC !99.99.
    *> Fixed currency: ! in PIC -> # in output
    *> Value 12.34 displays "#12.34"
```

### 1.4 Invalid Examples

**Invalid 1 -- Reserved PIC character as symbol:**
```cobol
CURRENCY SIGN IS "L" WITH PICTURE SYMBOL "9"
*> INVALID: 9 is a digit, cannot be PIC symbol
```

**Invalid 2 -- Alphabetic letter as symbol:**
```cobol
CURRENCY SIGN IS "L" WITH PICTURE SYMBOL "A"
*> INVALID: A is a reserved PICTURE character
```

**Invalid 3 -- Space as currency sign:**
```cobol
CURRENCY SIGN IS " "
*> INVALID: space cannot be currency sign
```

---

## 2. PICMODE Architecture Plan

### 2.1 The Core Insight: Exploiting Existing PICMODE

The `PICTURE` keyword in `WITH PICTURE SYMBOL` triggers the lexer's PICMODE, which
captures `SYMBOL` as a PIC_STRING token. This is not a bug -- it is exploitable:

**Token stream for** `WITH PICTURE SYMBOL "$"`:

| Input       | Lexer action                          | Token emitted          |
|-------------|---------------------------------------|------------------------|
| `WITH`      | default mode                          | WITH                   |
| `PICTURE`   | match PIC rule -> push PICMODE        | PIC                    |
| ` `         | PICMODE: PIC_WS -> skip               | (none)                 |
| `SYMBOL`    | PICMODE: PIC_STRING -> pop            | PIC_STRING("SYMBOL")   |
| ` `         | default mode: WS -> skip              | (none)                 |
| `"$"`       | default mode                          | STRINGLIT              |

The parser sees: `WITH PIC PIC_STRING literal`

**Semantic validation**: Assert that `PIC_STRING.getText().equalsIgnoreCase("SYMBOL")` --
reject otherwise.

**Key advantage**: Zero lexer changes. No new modes, no predicates, no token conflicts.
The existing PICMODE architecture handles this naturally.

### 2.2 New Internal Representation

**PicEnvironment** gains one new field:

| Field                | Type   | Default | Meaning                                              |
|----------------------|--------|---------|------------------------------------------------------|
| `CurrencySign`       | `char` | `'$'`   | The character used in PICTURE clauses (the *symbol*)  |
| **`CurrencyOutputChar`** | **`char`** | **`'$'`** | **The character placed in formatted output (the *string*)** |
| `DecimalPointIsComma`| `bool` | `false` | DECIMAL-POINT IS COMMA                                |

**Without PICTURE SYMBOL**: `CurrencyOutputChar == CurrencySign` (same char, current
behavior preserved)

**With PICTURE SYMBOL**: `CurrencySign = literal-8`, `CurrencyOutputChar = literal-7`
(decoupled)

### 2.3 How Picture Symbols Map to Runtime Formatting

**PIC parsing** (PicDescriptorFactory): Uses `env.CurrencySign` to identify currency
positions in the PIC string. This is unchanged -- the symbol character tells the factory
which positions are currency positions.

**Runtime formatting** (PicRuntime.FormatByEditPattern): Currently uses `env.CurrencySign`
in two roles:
1. **Pattern scanning** (identifying currency positions): Uses `CurrencySign` (the PIC symbol)
2. **Output placement** (writing the character): Uses `CurrencyOutputChar` (the output char)

There are exactly 3 output-placement sites in FormatByEditPattern:
- Line 231: `output[i] = env.CurrencySign;` (fixed currency)
- Line 436: `output[i] = env.CurrencySign;` (floating currency)
- Both change to `env.CurrencyOutputChar`

The pattern-scanning code (lines 171, 181, 195, 228, 357, 434) continues using
`CurrencySign` -- no change needed since patterns are in terms of the PIC symbol.

### 2.4 How CURRENCY SIGN Interacts with Editing

| Editing type           | Currency interaction                                         |
|------------------------|--------------------------------------------------------------|
| Fixed currency ($99.99) | Single currency char in pattern -> literal output char        |
| Floating currency ($$$9.99) | Multiple currency chars -> zero-suppression + floating output char |
| BLANK WHEN ZERO        | Overrides: field blanked when zero regardless of currency     |
| JUSTIFIED RIGHT        | Not applicable to numeric-edited (diagnostic: illegal combo)  |
| SIGN clause            | Compatible: sign and currency can coexist (PIC $$$9.99-)      |
| CR/DB                  | Compatible: PIC $$$9.99CR                                     |
| Zero-suppress Z/*      | Cannot mix with floating currency (spec rule)                 |

### 2.5 How the Binder Resolves Picture Modes

Current flow (unchanged except for environment construction):

```
SemanticBuilder.VisitSpecialNamesParagraph
  -> parse currencySignClause
  -> extract literal-7 (and now literal-8 if present)
  -> set _currencySign and _currencyOutputChar

SemanticBuilder.VisitDataDescriptionEntry
  -> create PicEnvironment(_currencySign, _currencyOutputChar, _decimalPointIsComma)
  -> pass to PicUsageResolver.ResolveForDataItem
    -> pass to PicDescriptorFactory.FromPicBody
      -> uses env.CurrencySign to identify currency in PIC string
      -> stores env in PicDescriptor
```

### 2.6 How Diagnostics Should Behave

| Condition                                    | Diagnostic                                           |
|----------------------------------------------|------------------------------------------------------|
| PIC_STRING text is not "SYMBOL"              | Error: "Expected SYMBOL after WITH PICTURE"           |
| literal-8 is a digit (0-9)                   | Error: "Currency symbol cannot be a digit"            |
| literal-8 is a reserved PIC char (A-Z)       | Error: "Currency symbol cannot be an alphabetic letter"|
| literal-8 is space                           | Error: "Currency symbol cannot be a space"            |
| literal-7 is multi-character (COBOL-85 mode) | Error: "Multi-character currency strings require COBOL-2002+" |
| literal-7 contains digit, +, -, comma, period, * | Error: "Currency string contains reserved character" |

---

## 3. Integration Plan

### 3.1 Files Touched

| File                        | Change                                                   |
|-----------------------------|----------------------------------------------------------|
| `CobolSpecialNames.g4`      | Add optional `WITH PIC PIC_STRING literal` to currencySignClause |
| `PicEnvironment.cs`         | Add `CurrencyOutputChar` parameter (char, default '$')    |
| `SemanticBuilder.cs`        | Parse WITH PICTURE SYMBOL phrase; set `_currencyOutputChar`; add validation |
| `SemanticModel.cs`          | Update `SetPicEnvironment` signature to accept `currencyOutputChar` |
| `Compilation.cs`            | Pass `CurrencyOutputChar` through to `SetPicEnvironment`  |
| `PicUsageResolver.cs`       | Pass updated PicEnvironment through (no logic change)     |
| `PicRuntime.cs`             | Lines 231, 436: `env.CurrencySign` -> `env.CurrencyOutputChar` |
| `CilExpressionEmitter.cs`   | Emit additional `CurrencyOutputChar` parameter in PicEnvironment ctor |
| `PicDescriptor.cs`          | Constructor gains `CurrencyOutputChar` passthrough via PicEnvironment |

### 3.2 Interaction with BLANK WHEN ZERO

No conflict. BLANK WHEN ZERO blanks the entire field when the value is zero, regardless of
currency symbols. PicRuntime already handles this before currency placement. No change needed.

### 3.3 Interaction with JUSTIFIED

JUSTIFIED RIGHT is illegal on numeric-edited fields (spec 13.18.31). This is already
validated. No change needed.

### 3.4 Interaction with SIGN

Compatible. The SIGN clause controls how the operational sign is stored. Currency editing
is orthogonal. Fixed and floating sign symbols (+/-) interact with currency only through
the established precedence rules in FormatByEditPattern. No change needed.

### 3.5 Interaction with Dialect Gating

The WITH PICTURE SYMBOL phrase is valid COBOL-85 -- no dialect gate needed.
Multi-character currency strings (literal-7 length > 1) are COBOL-2002+ and should be gated.

---

## 4. Test Plan

### 4.1 Parser Tests (positive)

| Test                                       | Input                                                |
|--------------------------------------------|------------------------------------------------------|
| Basic CURRENCY SIGN without PICTURE SYMBOL | `CURRENCY SIGN IS "L".`                              |
| CURRENCY SIGN with PICTURE SYMBOL          | `CURRENCY SIGN IS "L" WITH PICTURE SYMBOL "@".`      |
| CURRENCY SIGN with default `$`             | `CURRENCY SIGN IS "$".`                              |
| Verify PICMODE exploit produces correct tokens | Assert: WITH, PIC, PIC_STRING("SYMBOL"), STRINGLIT |

### 4.2 Parser Tests (negative)

| Test                          | Input                                              | Expected      |
|-------------------------------|----------------------------------------------------|---------------|
| Missing literal after SYMBOL  | `CURRENCY SIGN IS "L" WITH PICTURE SYMBOL.`        | Parse error   |
| PIC_STRING not "SYMBOL"       | `CURRENCY SIGN IS "L" WITH PICTURE FOOBAR "@".`    | Semantic error|

### 4.3 Binder Tests

| Test                                 | Verification                                   |
|--------------------------------------|-------------------------------------------------|
| CurrencySign set from literal-8      | PicEnvironment.CurrencySign == '@'              |
| CurrencyOutputChar set from literal-7| PicEnvironment.CurrencyOutputChar == 'L'        |
| Without PICTURE SYMBOL, both same    | CurrencySign == CurrencyOutputChar == 'L'       |
| Reserved PIC char rejected           | Diagnostic emitted for literal-8 = "A"          |
| Digit rejected                       | Diagnostic emitted for literal-8 = "9"          |

### 4.4 Runtime Formatting Tests

| Test                         | PicEnvironment          | PIC           | Value   | Expected output  |
|------------------------------|-------------------------|---------------|---------|------------------|
| Fixed custom output          | Sign='@', Output='L'    | `@99.99`      | 12.34   | `L12.34`         |
| Floating custom output       | Sign='@', Output='L'    | `@@@,@@9.99`  | 1234.56 | `  L1,234.56`    |
| Zero suppression with custom | Sign='@', Output='L'    | `@@@,@@9.99`  | 0.05    | `      L0.05`    |
| BLANK WHEN ZERO + custom     | Sign='@', Output='L'    | `@@@,@@9.99`  | 0       | (all spaces)     |
| Default $ unaffected         | Sign='$', Output='$'    | `$$$,$$9.99`  | 1234.56 | `  $1,234.56`    |

### 4.5 Integration Tests (end-to-end compile + run)

| Test                                              | Program                                            |
|---------------------------------------------------|----------------------------------------------------|
| Currency with PICTURE SYMBOL, MOVE + DISPLAY      | Declare field with custom PIC symbol, MOVE value, DISPLAY, verify output char is literal-7 |
| Currency without PICTURE SYMBOL                   | Existing behavior preserved                        |
| Multiple currency-edited fields                   | Mix fixed and floating with custom symbol          |

### 4.6 NIST Impact

No NIST NC-series tests exercise CURRENCY SIGN WITH PICTURE SYMBOL. The NIST test suite
uses only the default `$`. This change has zero direct NIST impact but enables future
NIST coverage expansion (e.g., SM suite for SPECIAL-NAMES features).
