Good — that confirms none of those COBOL-85 words are lexer tokens. Now I have a complete, authoritative data set. Let me produce the categorized tables.

---

## Categorized Lexer Token Gaps

This analysis cross-references the ISO/IEC 1989:2023 §8.9 reserved word list and §8.10 context-sensitive word list against `CobolLexer.g4`. Version attribution uses the historical record: ISO 1989:1985 (COBOL-85), ISO 1989:2002 (COBOL-2002, first major OO/boolean/float edition), ISO 1989:2014 (second edition), and ISO 1989:2023 (third edition, current spec).

---

### TABLE 1 — COBOL-85 Reserved Words Missing as Lexer Tokens

These words appear in §8.9, are used in COBOL-85 syntax diagrams, and are absent from `CobolLexer.g4`.

| Word | Used in | Spec Section / Notes |
|---|---|---|
| `AREA` | RESERVE clause (`RESERVE n AREA/AREAS`) | §13.6.24 |
| `AREAS` | RESERVE clause | §13.6.24 |
| `BLOCK` | BLOCK CONTAINS clause in FD | §13.4.7 |
| `CF` | Report Writer group type (Control Footing) | §13.9 |
| `CH` | Report Writer group type (Control Heading) | §13.9 |
| `CODE` | Report Writer CODE clause | §13.9 |
| `CODE-SET` | FD clause | §13.4.9 |
| `COMMA` | DECIMAL-POINT IS COMMA in SPECIAL-NAMES | §13.6.27 — already lexed as punctuation `','`; needs keyword form |
| `CONFIGURATION` | CONFIGURATION SECTION header | §13.3.2 |
| `CONTAINS` | BLOCK CONTAINS, RECORD CONTAINS | §13.4.7–8 |
| `CONTROL` | Report Writer CONTROL/CONTROLS clause | §13.9 |
| `CONTROLS` | Report Writer CONTROLS clause | §13.9 |
| `COPY` | COPY statement (preprocessor) | §7.2 — may stay in preprocessor only |
| `CORR` | ADD/SUBTRACT/MOVE CORR abbreviation | §14.9.1, 14.9.40, 14.9.27 |
| `DE` | Report Writer group type (Detail) | §13.9 |
| `DETAIL` | Report Writer TYPE DETAIL | §13.9 |
| `FINAL` | Report Writer CONTROL FINAL | §13.9 — used as string literal in `CobolParserOO.g4` |
| `GENERATE` | GENERATE statement | §14.9.16 — used as string literal in `CobolParserJsonXml.g4` |
| `HEADING` | Report Writer group type (Heading) | §13.9 |
| `INDICATE` | Report Writer INDICATE clause | §13.9 |
| `INITIATE` | INITIATE statement | §14.9.21 |
| `INPUT-OUTPUT` | INPUT-OUTPUT SECTION header | §13.5.1 |
| `LAST` | Report Writer LAST | §13.9 |
| `LINAGE-COUNTER` | Special register | §13.4.13 |
| `LIMIT` | Report Writer LIMIT clause | §13.9 |
| `LIMITS` | Report Writer LIMITS clause | §13.9 |
| `LINE-COUNTER` | Report Writer special register | §13.9 |
| `NATIVE` | ALPHABET clause (`NATIVE`) | §13.6.4 |
| `NUMBER` | Report Writer NUMBER clause | §13.9 |
| `PAGE-COUNTER` | Report Writer special register | §13.9 |
| `PF` | Report Writer group type (Page Footing) | §13.9 |
| `PH` | Report Writer group type (Page Heading) | §13.9 |
| `PRINTING` | Report Writer PRINTING clause | §13.9 |
| `REPLACE` | REPLACE statement (preprocessor) | §7.3 — may stay in preprocessor only |
| `REPORTS` | REPORTS clause | §13.4.14 |
| `RESET` | Report Writer RESET clause | §13.9 |
| `RF` | Report Writer group type (Report Footing) | §13.9 |
| `RH` | Report Writer group type (Report Heading) | §13.9 |
| `SAME` | I-O-CONTROL SAME AREA/RECORD clause | §13.5.4 |
| `SOURCE` | Report Writer SOURCE clause | §13.9 |
| `STANDARD-1` | ALPHABET clause | §13.6.4 |
| `STANDARD-2` | ALPHABET clause | §13.6.4 |
| `SUPPRESS` | SUPPRESS statement | §14.9.45 — used as string literal in `CobolParserJsonXml.g4` |
| `TABLE` | TABLE section | §13.2 |
| `TERMINATE` | TERMINATE statement | §14.9.47 |
| `UPON` | DISPLAY UPON | §14.9.11 |

**Count: 45 words** (all COBOL-85 origin, §8.9 reserved word list)

---

### TABLE 2 — COBOL-2002 Reserved Words Missing as Lexer Tokens

Introduced in ISO 1989:2002 (first OO/boolean/float edition). All appear in §8.9.

#### Exception Management

| Word | Used in |
|---|---|
| `ALLOCATE` | ALLOCATE statement (dynamic memory) |
| `FREE` | FREE statement (dynamic memory) |
| `RAISE` | RAISE statement (exception) |
| `RAISING` | EXIT PROGRAM RAISING, GOBACK RAISING |
| `RESUME` | RESUME statement |
| `RETRY` | retry-phrase on I/O statements |

#### Transaction / File Locking

| Word | Used in |
|---|---|
| `COMMIT` | COMMIT statement |
| `ROLLBACK` | ROLLBACK statement |
| `SHARING` | SELECT SHARING clause, OPEN SHARING |
| `UNLOCK` | UNLOCK statement |

#### Data Types

| Word | Used in |
|---|---|
| `B-AND` | Boolean expression operator |
| `B-NOT` | Boolean expression operator |
| `B-OR` | Boolean expression operator |
| `B-SHIFT-L` | Boolean shift operator |
| `B-SHIFT-R` | Boolean shift operator |
| `B-SHIFT-LC` | Boolean circular shift operator |
| `B-SHIFT-RC` | Boolean circular shift operator |
| `B-XOR` | Boolean expression operator |
| `BINARY-CHAR` | USAGE clause |
| `BINARY-DOUBLE` | USAGE clause |
| `BINARY-LONG` | USAGE clause |
| `BINARY-SHORT` | USAGE clause |
| `BIT` | USAGE BIT |
| `BOOLEAN` | CLASS condition (IS BOOLEAN) |
| `EXCLUSIVE-OR` | Logical operator (synonym for XOR) |
| `FLOAT-BINARY-32` | USAGE clause |
| `FLOAT-BINARY-64` | USAGE clause |
| `FLOAT-BINARY-128` | USAGE clause |
| `FLOAT-DECIMAL-16` | USAGE clause |
| `FLOAT-DECIMAL-34` | USAGE clause |
| `FLOAT-EXTENDED` | USAGE clause |
| `FLOAT-INFINITY` | CLASS condition |
| `FLOAT-LONG` | USAGE clause |
| `FLOAT-NOT-A-NUMBER` | CLASS condition |
| `FLOAT-NOT-A-NUMBER-QUIET` | CLASS condition |
| `FLOAT-NOT-A-NUMBER-SIGNALING` | CLASS condition |
| `FLOAT-SHORT` | USAGE clause |
| `NATIONAL` | USAGE NATIONAL, class |
| `NATIONAL-EDITED` | USAGE NATIONAL-EDITED |
| `XOR` | Logical operator |

#### Data Description Clauses

| Word | Used in |
|---|---|
| `BASED` | BASED clause |
| `CONSTANT` | CONSTANT entry |
| `GROUP-USAGE` | GROUP-USAGE clause |
| `TYPEDEF` | Already present as `TYPEDEF` token — **PRESENT** |

#### OO / Repository

| Word | Used in |
|---|---|
| `ACTIVE-CLASS` | OO expression |
| `ANYCASE` | INSPECT ANYCASE |
| `DATA-POINTER` | USAGE DATA-POINTER |
| `EC` | Exception-condition prefix |
| `EO` | Exception-object |
| `EXCEPTION-OBJECT` | OO exception handling |
| `FACTORY` | CLASS-ID factory paragraph |
| `FUNCTION-ID` | FUNCTION-ID paragraph |
| `FUNCTION-POINTER` | USAGE FUNCTION-POINTER |
| `GET` | GET property accessor |
| `INHERITS` | CLASS-ID INHERITS |
| `INTERFACE` | INTERFACE division |
| `LOCALE` | LOCALE clause |
| `NESTED` | NESTED phrase |
| `OBJECT-REFERENCE` | USAGE OBJECT REFERENCE |
| `OPTIONS` | OPTIONS paragraph |
| `OVERRIDE` | METHOD OVERRIDE — used as string literal in `CobolParserOO.g4` |
| `PRESENT` | PRESENT WHEN |
| `PROGRAM-POINTER` | USAGE PROGRAM-POINTER |
| `PROPERTY` | PROPERTY clause |
| `PROTOTYPE` | PROTOTYPE |
| `RAISE` | *(listed above)* |
| `REPOSITORY` | REPOSITORY paragraph |
| `UNIVERSAL` | Universal class reference |

#### Validation Facility

| Word | Used in |
|---|---|
| `DEFAULT` | DEFAULT clause (VALIDATE — **obsolete in 2023**) |
| `DESTINATION` | DESTINATION clause (VALIDATE — obsolete) |
| `VAL-STATUS` | VAL-STATUS clause (VALIDATE — obsolete) |
| `VALID` | VALID clause (VALIDATE — obsolete) |
| `VALIDATE` | VALIDATE statement (obsolete) |
| `VALIDATE-STATUS` | VALIDATE-STATUS clause (obsolete) |

**Count: ~60 words** (COBOL-2002 origin)

---

### TABLE 3 — COBOL-2014 / COBOL-2023 Reserved Words Missing as Lexer Tokens

| Word | Introduced | Used in |
|---|---|---|
| `DYNAMIC` | Already present as token — **PRESENT** | |
| `FARTHEST-FROM-ZERO` | 2002/2014 | CLASS condition |
| `FORMAT` | 2014 | JSON/XML FORMAT clause |
| `IN-ARITHMETIC-RANGE` | 2014 | CLASS condition |
| `MESSAGE-TAG` | 2014 | USAGE MESSAGE-TAG |
| `NEAREST-TO-ZERO` | 2002/2014 | CLASS condition, ROUNDED phrase |
| `ORDER` | 2014 | ORDER TABLE |
| `SCREEN` | 2014 | SCREEN section |
| `SEND` | 2002 | SEND statement (message facility) |
| `SOURCES` | 2014 | SOURCES clause |
| `SYSTEM-DEFAULT` | 2014 | ALPHABET SYSTEM-DEFAULT |
| `USER-DEFAULT` | 2014 | ALPHABET USER-DEFAULT |
| `LOCATION` | 2002 | LOCATION special register |

**Count: ~13 words** (2014/2023 additions; some were COBOL-2002 but clearly post-85)

---

### TABLE 4 — Missing Literal Forms

| Literal Form | Example | Spec Version | Notes |
|---|---|---|---|
| Floating-point numeric | `1.5E+3`, `-2.7E-10` | **COBOL-85** | §8.3.3.4; used in USAGE FLOAT contexts; no `FLOATLIT` token exists |
| Zero-length hex | `X""` | **COBOL-85** | §8.3.3.3; `HEXLIT` rule requires `[0-9a-f]+` (one or more), should be `*` |
| Boolean literal | `B"0101"`, `BX"0F"` | **COBOL-2002** | §8.3.3.5 |
| National literal | `N"text"`, `NX"hexdigits"` | **COBOL-2002** | §8.3.3.6 |

---

### TABLE 5 — Context-Sensitive Words Currently Promoted to Reserved Tokens (Should Be Demoted or Noted)

The spec §8.10 lists these as **context-sensitive** — they should be parseable as user-defined names when not in their specific context. The lexer currently makes them reserved tokens, which is overly restrictive:

| Token in Lexer | Spec Status | Spec Context |
|---|---|---|
| `CYCLE` | Context-sensitive | EXIT statement only |
| `PARAGRAPH` | Context-sensitive | EXIT statement only |
| `PREVIOUS` | Context-sensitive | READ statement only |
| `RECURSIVE` | Context-sensitive | PROGRAM-ID paragraph only |
| `YYYYMMDD` | Context-sensitive | ACCEPT statement only |
| `YYYYDDD` | Context-sensitive | ACCEPT statement only |

---

### TABLE 6 — Extra Tokens (Non-Standard, IBM Extensions, or Archaic)

These tokens are in `CobolLexer.g4` but are **not in §8.9** (not ISO reserved words). They are either IBM extensions or identification division paragraph names (archaic but present in COBOL-85 programs).

| Token | Category | Notes |
|---|---|---|
| `COMP-1` | IBM extension | Single-precision float; not in ISO spec |
| `COMP-2` | IBM extension | Double-precision float; not in ISO spec |
| `COMP-3` | IBM extension | Synonym for PACKED-DECIMAL; not ISO |
| `COMP-5` | IBM extension | Binary with native byte order; not ISO |
| `COMPUTATIONAL-1` | IBM extension | Long form of COMP-1 |
| `COMPUTATIONAL-2` | IBM extension | Long form of COMP-2 |
| `COMPUTATIONAL-3` | IBM extension | Long form of COMP-3 |
| `COMPUTATIONAL-5` | IBM extension | Long form of COMP-5 |
| `DATE-WRITTEN` | Archaic ID division | §13.2 — identification paragraph; archaic feature (Annex F.1) |
| `DATE-COMPILED` | Archaic ID division | §13.2 — archaic feature |
| `AUTHOR` | Archaic ID division | §13.2 — archaic feature |
| `INSTALLATION` | Archaic ID division | §13.2 — archaic feature |
| `SECURITY` | Archaic ID division | §13.2 — archaic feature |
| `REMARKS` | Non-standard | Not in ISO spec at all |
| `PROCEED` | Archaic | Old ALTER PROCEED TO; not in current spec |
| `GOBACK` | Non-standard | §8.9 confirms GOBACK **is** in spec — **PRESENT** ✓ |
| `CHANNEL` | Non-standard / IBM | Not in ISO §8.9 reserved list |
| `GENERIC` | Non-standard | Not in ISO §8.9 — currently used as catch-all clause token |
| `EDITED` | Non-standard | Not in §8.9 — appears in lexer but not as spec reserved word |
| `PACKED` | Partial | `PACKED-DECIMAL` is spec; bare `PACKED` is not |
| `LABEL` | Archaic | LABEL RECORDS clause; removed in COBOL-85 Amendment |

**Notes:**
- `GOBACK` is confirmed in §8.9 — keep it
- `TYPEDEF` is confirmed in §8.9 — keep it (already present)
- `ENTRY` is not in §8.9 but widely supported as extension for ENTRY point
- `ALTER` is not in §8.9 reserved list (archaic, removed from modern spec)

---

### TABLE 7 — Words in §8.9 That Are Currently Lexed Correctly (Present)

For completeness: these are confirmed present and correct.

All statement keywords (ACCEPT, ADD, CALL, ..., WRITE), all END-xxx terminators, section/division keywords, WORKING-STORAGE, LOCAL-STORAGE, file-description keywords (FD, SD, RD), arithmetic/relational operators, figurative constants (ZERO, SPACE, HIGH-VALUE, LOW-VALUE, QUOTE), and core clause keywords are all correctly tokenized.

---

### Summary Count

| Category | Count |
|---|---|
| COBOL-85 reserved words missing as tokens | ~45 |
| COBOL-2002 reserved words missing as tokens | ~60 |
| COBOL-2014/2023 reserved words missing as tokens | ~13 |
| Missing literal forms | 4 |
| Context-sensitive words over-promoted to reserved | 6 |
| Extra non-standard / IBM extension tokens | ~12 |
| **Total missing from spec (all versions)** | **~118** |

---

### Key Findings for Implementation Priority

**Highest priority (block NIST COBOL-85 programs):**
- `UPON` — DISPLAY UPON mnemonic-name
- `CORR` — ADD/SUBTRACT/MOVE CORR
- `CONTAINS` — BLOCK CONTAINS
- `SAME` — SAME AREA clause
- `CONFIGURATION` — section header
- `INPUT-OUTPUT` — section header
- `NATIVE`, `STANDARD-1`, `STANDARD-2` — ALPHABET clause
- `CODE-SET` — FD clause
- `LINAGE-COUNTER` — special register in WRITE/LINAGE logic
- Zero-length hex literal (`X""`) — only requires changing `+` to `*` in `HEXLIT`
- Floating-point literals — needed for any FLOAT-* USAGE usage

**Medium priority (needed for Report Writer, which is currently stub):**
- All 14 Report Writer tokens: CF, CH, DE, PF, PH, RF, RH, DETAIL, HEADING, CONTROL, CONTROLS, FINAL, GENERATE, INITIATE, TERMINATE, SUPPRESS, SOURCE, NUMBER, PRINTING, REPORTS, RESET, LIMIT, LIMITS, LAST, INDICATE, LINE-COUNTER, PAGE-COUNTER

**Lower priority (COBOL-2002+ features not yet implemented):**
- All boolean/national/float type tokens
- All OO tokens (FACTORY, INHERITS, OVERRIDE, etc.)
- Transaction tokens (COMMIT, ROLLBACK, RETRY, SHARING, UNLOCK)
- Validation facility tokens (obsolete in 2023)

**Architectural note:** `FINAL`, `GENERATE`, `SUPPRESS`, `OVERRIDE`, and `DETAIL` are currently used as raw string literals (`'FINAL'`, `'GENERATE'`, etc.) in `CobolParserOO.g4` and `CobolParserJsonXml.g4` rather than as lexer tokens. This is a grammar anti-pattern — ANTLR requires that all string literals in parser rules that could conflict with other patterns be promoted to lexer tokens.