# M411 Design Document: SCREEN SECTION (Grammar Island)

**Status**: Design complete, awaiting implementation authorization
**Date**: 2026-03-30
**Ledger item**: M411
**Severity**: P1
**Subsystem**: Grammar / Semantics

---

## 1. Exact COBOL SCREEN SECTION Syntax

### 1.1 Overview

SCREEN SECTION was standardized in ISO 1989:2002 13.9, later refined in ISO 1989:2023.
It was widely implemented as a de facto extension in COBOL-85 compilers (MicroFocus,
ACUCOBOL, RM/COBOL). It is NOT in the original ISO 1989:1985 core standard but is in
the 2002+ standard.

### 1.2 Structural Position

SCREEN SECTION appears in the DATA DIVISION after LINKAGE SECTION and REPORT SECTION:

```
DATA DIVISION.
  FILE SECTION.
  WORKING-STORAGE SECTION.
  LOCAL-STORAGE SECTION.
  LINKAGE SECTION.
  REPORT SECTION.
  SCREEN SECTION.        <-- here
```

### 1.3 Screen Description Entry Syntax

**Format 1 -- Group screen item:**
```
level-number [screen-name]
  [GLOBAL]
  [LINE NUMBER IS [PLUS] {identifier | integer}]
  [{COLUMN | COL} NUMBER IS [PLUS] {identifier | integer}]
  [BLANK SCREEN]
  [screen-attribute-clauses]
  .
```

**Format 2 -- Elementary screen item:**
```
level-number [screen-name]
  [GLOBAL]
  [LINE NUMBER IS [PLUS] {identifier | integer}]
  [{COLUMN | COL} NUMBER IS [PLUS] {identifier | integer}]
  [BLANK {LINE | SCREEN}]
  [ERASE {EOL | EOS}]
  [screen-attribute-clauses]
  [PICTURE IS pic-string]
  [{FROM {identifier | literal}} | {TO identifier} | {USING identifier}]
  [VALUE IS literal]
  [BLANK WHEN ZERO]
  [{JUSTIFIED | JUST} RIGHT]
  [SIGN IS {LEADING | TRAILING} [SEPARATE CHARACTER]]
  [FULL]
  [AUTO]
  [SECURE]
  [REQUIRED]
  [OCCURS integer TIMES]
  .
```

**Screen attribute clauses:**
```
  [BELL]
  [BLINK]
  [{HIGHLIGHT | LOWLIGHT}]
  [REVERSE-VIDEO]
  [UNDERLINE]
  [FOREGROUND-COLOR IS {identifier | integer}]
  [BACKGROUND-COLOR IS {identifier | integer}]
```

### 1.4 ACCEPT/DISPLAY Screen Forms

**ACCEPT Format 3 (Screen):**
```
ACCEPT screen-name
  [AT LINE NUMBER {identifier | integer}
      {COLUMN | COL} NUMBER {identifier | integer}]
  [ON EXCEPTION imperative-statement]
  [NOT ON EXCEPTION imperative-statement]
  [END-ACCEPT]
```

**DISPLAY Format 2 (Screen):**
```
DISPLAY screen-name
  [AT LINE NUMBER {identifier | integer}
      {COLUMN | COL} NUMBER {identifier | integer}]
  [ON EXCEPTION imperative-statement]
  [NOT ON EXCEPTION imperative-statement]
  [END-DISPLAY]
```

### 1.5 Valid Examples

**Example 1 -- Simple input screen:**
```cobol
SCREEN SECTION.
01 INPUT-SCREEN.
   05 VALUE "Enter name: " LINE 1 COL 1.
   05 NAME-FIELD PIC X(30) LINE 1 COL 14
      USING WS-NAME AUTO.
```

**Example 2 -- Colored display with attributes:**
```cobol
SCREEN SECTION.
01 HEADER-SCREEN.
   05 LINE 1 COL 1 BLANK SCREEN.
   05 VALUE "REPORT HEADER" LINE 1 COL 30
      HIGHLIGHT FOREGROUND-COLOR 7.
   05 LINE 3 COL 1 VALUE "Date: ".
   05 PIC X(10) LINE 3 COL 7 FROM WS-DATE.
```

**Example 3 -- Secure password entry:**
```cobol
SCREEN SECTION.
01 LOGIN-SCREEN.
   05 VALUE "Password: " LINE 5 COL 1.
   05 PIC X(20) LINE 5 COL 12
      TO WS-PASSWORD SECURE REQUIRED.
```

### 1.6 Invalid Examples

**Invalid 1 -- USING with VALUE:**
```cobol
05 PIC X(10) USING WS-FIELD VALUE "Hello".
*> INVALID: USING and VALUE are mutually exclusive
```

**Invalid 2 -- FROM and TO both on same item with USING:**
```cobol
05 PIC X(10) FROM WS-A TO WS-B USING WS-C.
*> INVALID: USING is shorthand for FROM+TO; cannot combine
```

**Invalid 3 -- Screen attributes on non-screen item:**
```cobol
WORKING-STORAGE SECTION.
01 WS-FIELD PIC X(10) BELL HIGHLIGHT.
*> INVALID: BELL/HIGHLIGHT are screen-only clauses
```

---

## 2. Grammar Island Design

### 2.1 Isolation Principle

SCREEN SECTION is a self-contained grammar island. Its rules:
- Are defined in a new parser fragment file (CobolScreen.g4)
- Import into CobolParserCore.g4
- Share lexer tokens with existing grammar but add new screen-specific tokens
- Do NOT affect parsing of any non-screen construct

### 2.2 New Lexer Tokens Required

The following tokens do NOT currently exist in CobolLexer.g4:

| Token                | Literal              | Purpose                   |
|----------------------|----------------------|---------------------------|
| `SCREEN`             | `'SCREEN'`           | SCREEN SECTION keyword    |
| `COL`                | `'COL'`              | Column abbreviation       |
| `BELL`               | `'BELL'`             | Audio alert attribute     |
| `BLINK`              | `'BLINK'`            | Text blink attribute      |
| `HIGHLIGHT`          | `'HIGHLIGHT'`        | High intensity            |
| `LOWLIGHT`           | `'LOWLIGHT'`         | Low intensity             |
| `REVERSE_VIDEO`      | `'REVERSE-VIDEO'`    | Reverse video attribute   |
| `UNDERLINE_`         | `'UNDERLINE'`        | Underline attribute       |
| `FOREGROUND_COLOR`   | `'FOREGROUND-COLOR'` | Foreground color          |
| `BACKGROUND_COLOR`   | `'BACKGROUND-COLOR'` | Background color          |
| `SECURE`             | `'SECURE'`           | Suppress echo             |
| `AUTO`               | `'AUTO'`             | Auto-advance              |
| `FULL_`              | `'FULL'`             | Require full field        |
| `ERASE`              | `'ERASE'`            | Erase screen/line         |
| `REQUIRED`           | `'REQUIRED'`         | Require input             |
| `EOL`                | `'EOL'`              | End of line               |
| `EOS`                | `'EOS'`              | End of screen             |

**Placement rules:**
- `SCREEN` goes in the "Division/section keywords" section (before IDENTIFIER)
- Hyphenated tokens (`REVERSE-VIDEO`, `FOREGROUND-COLOR`, `BACKGROUND-COLOR`) go in the
  "Hyphenated keywords" section (before IDENTIFIER)
- Simple keywords (`BELL`, `BLINK`, etc.) go in the "Clause/phrase keywords" section
  (before IDENTIFIER)

**Risk: Keyword shadowing:**
Keywords like SCREEN, AUTO, FULL, SECURE could be used as data-names in existing programs.
Making them reserved tokens would break those programs. Mitigation options:
- Option A: Make them reserved (simplest, may break programs)
- Option B: Use cobolWord for screen-name positions and semantic predicates for clauses
  (complex, preserves compatibility)
- Recommendation: Option A for now, with a known-limitation note. COBOL-85 reserves these
  words; programs using them as data-names are non-conforming.

### 2.3 Parser Structure

The grammar island follows existing patterns (like reportSection):

```
screenSection
    : SCREEN SECTION DOT screenDescriptionEntry*
    ;

screenDescriptionEntry
    : levelNumber screenName? screenDescriptionBody DOT
    ;

screenName
    : cobolWord
    | FILLER
    ;

screenDescriptionBody
    : screenClause*
    ;

screenClause (one of):
    screenLineClause
    screenColumnClause
    screenBlankClause
    screenEraseClause
    screenBellClause
    screenBlinkClause
    screenHighlightClause
    screenLowlightClause
    screenReverseVideoClause
    screenUnderlineClause
    screenForegroundColorClause
    screenBackgroundColorClause
    screenAutoClause
    screenSecureClause
    screenFullClause
    screenRequiredClause
    pictureClause           (reuse existing)
    screenFromClause
    screenToClause
    screenUsingClause
    valueClause             (reuse existing)
    blankWhenZeroClause     (reuse existing)
    justifiedClause         (reuse existing)
    signClause              (reuse existing)
    occursClause            (reuse existing, limited to OCCURS n TIMES)
    globalClause            (reuse existing)
```

### 2.4 Rule Reuse Strategy

Several existing parser rules are reused directly:
- `pictureClause` (PIC PIC_STRING)
- `valueClause` (VALUE IS literal)
- `blankWhenZeroClause` (BLANK WHEN ZERO)
- `justifiedClause` (JUSTIFIED RIGHT)
- `signClause` (SIGN IS LEADING/TRAILING)
- `occursClause` (OCCURS n TIMES -- screen items support only fixed OCCURS)
- `globalClause` (IS GLOBAL)

Screen-specific rules are new but follow the established clause pattern.

---

## 3. Binder Model

### 3.1 New Bound Node Types

| Type                | Purpose                                   | Parent                                      |
|---------------------|-------------------------------------------|---------------------------------------------|
| `BoundScreenSection`| Container for all screen entries          | Part of program's bound tree                |
| `BoundScreenItem`   | Individual screen entry (group or elem)   | Child of BoundScreenSection or another item |

### 3.2 BoundScreenItem Fields

| Field              | Type                | Meaning                       |
|--------------------|---------------------|-------------------------------|
| `Name`             | `string?`           | Screen item name (null=unnamed)|
| `Level`            | `int`               | Level number (01-49)          |
| `IsGroup`          | `bool`              | Has subordinate items         |
| `Line`             | `ScreenPosition?`   | LINE clause value             |
| `Column`           | `ScreenPosition?`   | COLUMN clause value           |
| `BlankScreen`      | `bool`              | BLANK SCREEN                  |
| `BlankLine`        | `bool`              | BLANK LINE                    |
| `EraseEol`         | `bool`              | ERASE EOL                     |
| `EraseEos`         | `bool`              | ERASE EOS                     |
| `Bell`             | `bool`              | BELL attribute                |
| `Blink`            | `bool`              | BLINK attribute               |
| `Highlight`        | `bool`              | HIGHLIGHT                     |
| `Lowlight`         | `bool`              | LOWLIGHT                      |
| `ReverseVideo`     | `bool`              | REVERSE-VIDEO                 |
| `Underline`        | `bool`              | UNDERLINE                     |
| `ForegroundColor`  | `int?`              | FOREGROUND-COLOR value        |
| `BackgroundColor`  | `int?`              | BACKGROUND-COLOR value        |
| `Secure`           | `bool`              | SECURE (suppress echo)        |
| `Auto`             | `bool`              | AUTO (auto-advance)           |
| `Full`             | `bool`              | FULL (require full field)     |
| `Required`         | `bool`              | REQUIRED                      |
| `PicString`        | `string?`           | PICTURE clause                |
| `FromSource`       | `DataSymbol?`       | FROM data reference           |
| `ToTarget`         | `DataSymbol?`       | TO data reference             |
| `UsingField`       | `DataSymbol?`       | USING data reference          |
| `Value`            | `string?`           | VALUE literal                 |
| `Children`         | `List<BoundScreenItem>` | Subordinate items         |

### 3.3 ScreenPosition Record

```
ScreenPosition { IsRelative: bool, Value: int or DataSymbol }
```
- Absolute: `LINE 5` -> `{ false, 5 }`
- Relative: `LINE PLUS 3` -> `{ true, 3 }`

---

## 4. Semantic Model

### 4.1 SemanticBuilder Changes

A new visitor method `VisitScreenSection` processes screen entries:
- Builds a tree of screen items using the same level-number stack pattern as
  `VisitDataDescriptionEntry`
- Resolves FROM/TO/USING references against WORKING-STORAGE and LINKAGE symbols
- Validates mutual exclusivity (USING vs FROM+TO, HIGHLIGHT vs LOWLIGHT)
- Stores screen items in a new `ScreenSection` property on `SemanticModel`

### 4.2 Validation Rules

| Rule                                    | Diagnostic                                |
|-----------------------------------------|-------------------------------------------|
| HIGHLIGHT and LOWLIGHT on same item     | Error: mutually exclusive                 |
| USING combined with FROM or TO          | Error: USING replaces FROM+TO             |
| FROM/TO/VALUE without PIC              | Error: elementary screen item requires PIC |
| BLANK LINE on group item                | Error: BLANK LINE only for elementary     |
| ERASE on group item                     | Error: ERASE only for elementary          |
| Color value outside 0-7                 | Error: invalid color value                |
| OCCURS on screen item > simple integer  | Error: screen OCCURS limited to fixed count|

### 4.3 Name Resolution

Screen names are registered in a separate namespace from data items. ACCEPT and DISPLAY
statements resolve screen-name references against this namespace. A screen-name is valid
if it was declared in the SCREEN SECTION.

---

## 5. ACCEPT/DISPLAY Interaction Model

### 5.1 Current State

ACCEPT and DISPLAY currently handle:
- ACCEPT FROM DATE/TIME/DAY/etc. (Format 1)
- ACCEPT identifier (Format 2 -- from terminal)
- DISPLAY identifiers/literals UPON mnemonic (Format 1)

### 5.2 New Screen Forms

ACCEPT and DISPLAY gain a third format that targets screen-names instead of data-names.

**Disambiguation**: When the parser sees `ACCEPT name` or `DISPLAY name`, it cannot
syntactically distinguish between a data-name and a screen-name. Both parse as
`dataReference` / `identifier`. Resolution happens in the binder:
- If the name resolves to a screen item -> screen I/O path
- If the name resolves to a data item -> standard I/O path

This means NO grammar change is needed for ACCEPT/DISPLAY -- the existing rules already
accept screen-names. The semantic layer handles dispatch.

### 5.3 Screen I/O Runtime Model (Future -- not in M411 scope)

When ACCEPT targets a screen-name:
1. Display the screen (all screen items rendered at their LINE/COL positions)
2. Position cursor at first input field (TO or USING)
3. Accept input from terminal
4. Transfer data to TO/USING targets
5. Update CRT STATUS if specified in SPECIAL-NAMES

When DISPLAY targets a screen-name:
1. Render all screen items at their positions
2. Transfer FROM/VALUE data to screen
3. Apply attributes (BELL, BLINK, colors, etc.)

**Note**: Full runtime implementation is a separate work item. For M411, the scope is
grammar acceptance + basic semantic model.

---

## 6. Dialect Gating Rules

SCREEN SECTION was NOT in the original ISO 1989:1985 (COBOL-85 core). It was widely
implemented as a vendor extension and standardized in ISO 1989:2002.

**Option A -- No gate (recommended for CobolSharp):** Accept SCREEN SECTION at all dialect
levels. This matches the behavior of virtually every COBOL-85 compiler in production.
Programs using SCREEN SECTION are common and should not require a dialect override.

**Option B -- Gate behind is2002():** Technically correct per ISO lineage but would reject
many valid COBOL-85 programs.

**Recommendation**: Option A. The grammar should accept SCREEN SECTION unconditionally.
If strict ISO 1985 compliance is later needed, a linter rule (not a parser gate) can flag it.

---

## 7. Test Plan

### 7.1 Parser Tests (positive)

| Test                                | Input                                                        |
|-------------------------------------|--------------------------------------------------------------|
| Empty SCREEN SECTION                | `SCREEN SECTION.` with no entries                            |
| Simple screen item                  | `01 MY-SCREEN. 05 VALUE "Hello" LINE 1 COL 1.`              |
| PIC + USING                         | `05 PIC X(10) USING WS-FIELD.`                              |
| PIC + FROM + TO                     | `05 PIC X(10) FROM WS-SRC TO WS-DST.`                       |
| All attributes                      | `05 BELL BLINK HIGHLIGHT REVERSE-VIDEO UNDERLINE.`           |
| Colors                              | `05 FOREGROUND-COLOR 7 BACKGROUND-COLOR 0.`                  |
| LINE/COL PLUS                       | `05 LINE PLUS 2 COL PLUS 5.`                                |
| BLANK SCREEN                        | `05 BLANK SCREEN.`                                           |
| ERASE EOL/EOS                       | `05 ERASE EOL.` / `05 ERASE EOS.`                           |
| SECURE + REQUIRED                   | `05 PIC X(20) TO WS-PWD SECURE REQUIRED.`                   |
| Nested levels                       | `01 OUTER. 05 INNER. 10 VALUE "X" LINE 1 COL 1.`            |
| OCCURS                              | `05 PIC X(10) OCCURS 5 TIMES.`                              |

### 7.2 Parser Tests (negative)

| Test                              | Input                      | Expected    |
|-----------------------------------|----------------------------|-------------|
| SCREEN SECTION in wrong position  | After PROCEDURE DIVISION   | Parse error |
| Unknown screen clause             | `05 FOOBAR.`               | Falls through to generic |

### 7.3 Binder Tests

| Test                                 | Verification                                    |
|--------------------------------------|-------------------------------------------------|
| Screen item tree built correctly     | Parent/child hierarchy mirrors level numbers    |
| FROM reference resolved              | DataSymbol resolved from WORKING-STORAGE        |
| TO reference resolved                | DataSymbol resolved                             |
| USING resolved as both FROM and TO   | Same DataSymbol for both roles                  |
| HIGHLIGHT + LOWLIGHT rejected        | Diagnostic emitted                              |
| USING + FROM rejected                | Diagnostic emitted                              |

### 7.4 Integration Tests (end-to-end compile)

| Test                                    | Program                                             |
|-----------------------------------------|-----------------------------------------------------|
| Program with SCREEN SECTION compiles    | Declare screen, no ACCEPT/DISPLAY -- verify no crash|
| Screen ACCEPT compiles                  | ACCEPT screen-name -- verify binder resolves        |
| Screen DISPLAY compiles                 | DISPLAY screen-name -- verify binder resolves       |

### 7.5 NIST Impact

No NIST NC-series tests use SCREEN SECTION. The SM (SPECIAL-NAMES) suite may contain
screen-related tests but has not been baselined. Zero direct NIST impact.
