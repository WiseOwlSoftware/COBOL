# COBOL Grammar Reference (from ISO/IEC 1989:2023)

Extracted from the spec for lexer/parser implementation. Page references are to the PDF (physical = logical + 30).

---

## ANTLR4 Grammar Architecture (CobolSharp)

CobolSharp uses a layered ANTLR4 grammar in `src/CobolSharp.Compiler/Grammar/`:

### Grammar Files

| File | Purpose |
|------|---------|
| `CobolLexer.g4` | Shared lexer — tokens, keywords, literals, punctuation |
| `CobolParserCore.g4` | Procedural core — all divisions, statements, expressions |
| `CobolParserOO.g4` | OO extension — CLASS, METHOD, INVOKE |
| `CobolParserGenerics.g4` | Generics extension — TYPEDEF GENERIC |
| `CobolParserJsonXml.g4` | JSON/XML extension — JSON PARSE/GENERATE, XML PARSE/GENERATE |
| `CobolDialect.g4` | Dialect overlays — COBOL-85 compatibility (e.g., bare END as imperative) |

### Architecture

```
Raw COBOL Source
      │
      ▼
┌─────────────────────┐
│ 1. Preprocessor     │  ReferenceFormatProcessor + CopyProcessor
│    (not ANTLR)       │  Fixed→free normalization, COPY, REPLACE
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 2. ANTLR4 Lexer     │  CobolLexer.g4
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 3. ANTLR4 Parser    │  CobolParserCore.g4 + extensions
│                     │  Produces parse tree (CST)
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 4. Semantic Analysis│  Context-sensitive validation
│                     │  Name resolution, type checking
└─────────┬───────────┘
          ▼
┌─────────────────────┐
│ 5. CIL Code Gen     │  Walks parse tree → .NET assembly
└─────────────────────┘
```

### Statement Coverage (CobolParserCore.g4)

**Fully expanded** (with all clauses, exception phrases, END-xxx terminators):
- Arithmetic: ADD, SUBTRACT, MULTIPLY, DIVIDE, COMPUTE (with ON SIZE ERROR)
- Data movement: MOVE (simple + CORRESPONDING)
- String: STRING (DELIMITED BY, POINTER, OVERFLOW), UNSTRING (DELIMITER IN, COUNT IN, TALLYING)
- Control flow: IF/END-IF, PERFORM (TIMES/UNTIL/VARYING)/END-PERFORM, EVALUATE (WHEN/OTHER)/END-EVALUATE
- File I/O: READ (AT END, INVALID KEY)/END-READ, WRITE, OPEN, CLOSE, REWRITE/END-REWRITE, DELETE/END-DELETE, START
- Sort/Merge: SORT (keys, USING/GIVING, INPUT/OUTPUT PROCEDURE)/END-SORT, MERGE/END-MERGE
- Table: SEARCH (WHEN, AT END)/END-SEARCH, SEARCH ALL
- Inter-program: CALL (BY REFERENCE/VALUE/CONTENT, RETURNING, ON EXCEPTION)/END-CALL, CANCEL
- SET: TO value, TO TRUE/FALSE, ADDRESS OF, object reference, UP/DOWN BY
- Sort I/O: RETURN (AT END)/END-RETURN, RELEASE
- File management: DELETE FILE (2023, ON EXCEPTION)/END-DELETE
- Flow: CONTINUE, NEXT SENTENCE, EXIT, GOBACK, GO TO, STOP
- Other: ACCEPT, DISPLAY, INITIALIZE, INSPECT
- 2023: JSON PARSE/GENERATE, XML PARSE/GENERATE, inline method invocation

**Stub** (to be expanded): ACCEPT, DISPLAY, EXIT, GOBACK, GO TO, INITIALIZE, INSPECT

### Expression Grammar (precedence, highest to lowest)

1. NOT (logical negation)
2. AND (logical conjunction)
3. OR (logical disjunction)
4. Relational: =, <>, <, <=, >, >=
5. Additive: +, -
6. Multiplicative: *, /
7. Exponentiation: **
8. Unary: +, -
9. Primary: literal, identifier, (expression)

### Dialect Support

The grammar accepts a superset. Dialect-specific validation (COBOL-85 vs 2023)
is handled in the semantic phase via a `CobolDialect` enum:

```csharp
enum CobolDialect { Cobol85, Cobol2002, Cobol2014, Cobol2023 }
```

COBOL-85 compatibility extensions (e.g., bare END as imperative statement,
ALTER, NEXT SENTENCE) are accepted by the parser and gated in semantics.

---

## Notation Conventions (§5)

In the General Format definitions:
- **UPPERCASE UNDERLINED** words are required reserved words (keywords)
- **UPPERCASE** words (not underlined) are optional reserved words (noise words)
- **lowercase** words are meta-language terms (e.g., identifier-1, literal-1)
- `{ }` braces = exactly one alternative must be chosen
- `[ ]` brackets = optional (zero or one)
- `...` ellipsis = preceding element may be repeated
- `{ } ...` = one or more occurrences
- `[ ] ...` = zero or more occurrences

---

## 1. Reference Format (§6)

### 1.1 Fixed-Form Reference Format (§6.3)

```
Columns:   1-6     7       8-11     12-72      73+
           SeqNum  Indicator  Area A   Area B    Ignored
```

- **Margin L**: immediately left of leftmost character position (col 1)
- **Margin C**: between cols 6 and 7 (col 7 indicator area)
- **Margin A**: between cols 7 and 8 (Area A starts at col 8)
- **Margin B**: between cols 11 and 12 (Area B starts at col 12)
- **Margin R**: immediately right of rightmost character position of the program-text area (col 72)

**Sequence number area (cols 1-6)**: Content is user-defined, ignored by compiler.

**Indicator area (col 7)**: Fixed indicators:
| Character | Meaning |
|-----------|---------|
| `*` | Comment line |
| `/` | Comment line with page ejection |
| `-` | Continuation line |
| `D` | Debugging line |
| (space) | Source indicator (normal code line) |

**Program-text area (cols 8-72)**: Where code is written.

**Area A (cols 8-11)**: Division headers, section headers, paragraph names, level indicators (FD, SD), 01/77 level numbers must begin here.

**Area B (cols 12-72)**: Statements, sentences, entries, continuation text.

### 1.2 Fixed Indicators (§6.2.2)

- `*` — comment indicator: a comment line
- `/` — comment indicator: a comment line with page ejection
- `-` — (hyphen) continuation indicator: a continuation line
- `D` — debugging line (archaic feature)

### 1.3 Floating Indicators (§6.2.3)

May be used in both fixed-form and free-form:
| Indicator | Meaning |
|-----------|---------|
| `*>` | Inline comment (to end of line) |
| `>>` | Compiler directive line |
| `-` (as literal continuation) | Continuation of alphanumeric/boolean/national literal |

Rules for `*>`:
- A floating comment indicator preceded by a separator space may be specified wherever a separator space may be specified
- A space is implied immediately following a floating comment indicator
- Everything after `*>` to end of line is a comment

### 1.4 Free-Form Reference Format (§6.4)

- No column restrictions. The entire line is the program-text area.
- No sequence number area, no indicator area
- Floating indicators (`*>`, `>>`) identify specific elements
- Source may be written anywhere on a line

### 1.5 Continuation of Lines (§6.3.5 fixed, §6.4.2 free)

**Fixed-form**: Any entry, sentence, statement, clause, phrase, or pseudo-text consisting of more than one character-string may be continued by writing some of the character strings and separators on the next line. The last nonblank character of each line is treated as if it were followed by a space.

**Continuation of an alphanumeric, boolean, or national literal** is indicated when either:
1. A line terminates within a literal without a closing delimiter AND the next line that is not a comment line or blank contains a fixed continuation indicator (`-`). The first non-space character in the program-text area of the continuation line shall be a quotation symbol matching the opening delimiter. Content continues after that quotation symbol.
2. Or a line terminates within a literal that ends with a floating literal continuation indicator.

**Free-form**: Same rules apply but using floating literal continuation indicators. The continuation starts with the character immediately after the quotation symbol on the continuation line.

### 1.6 Blank Lines (§6.3.6 / §6.4.3)

A blank line contains only space characters between margin C and margin R (fixed) or only spaces/no content (free). Blank lines may be written anywhere in a compilation group.

### 1.7 Comments (§6.3.7 / §6.4.4)

- **Comment line**: Identified by `*` or `/` in col 7 (fixed-form), or by `*>` at the first character-string position (free-form). All characters following the indicator are comment-text.
- **Inline comment**: Identified by the floating comment indicator `*>` preceded by a space in the program-text area. All characters following to end of line are comment-text.
- Comments have no effect on the meaning of the compilation group.

---

## 2. Lexical Elements (§8.3)

### 2.1 Character Sets (§8.1)

The computer's coded character set includes:
- Letters: A-Z, a-z (uppercase and lowercase are equivalent)
- Digits: 0-9
- Special characters: space, `+`, `-`, `*`, `/`, `=`, `$`, `,`, `;`, `.`, `"`, `'`, `(`, `)`, `>`, `<`, `:`, `&`, `_`

### 2.2 COBOL Words (§8.3.2)

A COBOL word is a character-string of maximum 30 characters from: letters (A-Z, a-z), digits (0-9), hyphens (-), underscores (_).

Rules:
- Cannot begin or end with a hyphen
- Case insensitive (MOVE = Move = move)
- Types: user-defined words, system-names, reserved words, context-sensitive words

**User-defined words**: paragraph-names, section-names, data-names, file-names, condition-names, mnemonic-names, etc. All follow the 30-character rule.

**Reserved words**: Keywords that cannot be used as user-defined names (MOVE, IF, ADD, etc.). See Annex E for the complete list.

**Context-sensitive words**: Words that are reserved only in specific contexts (e.g., EXCEPTION, OVERRIDE in certain clauses).

### 2.3 Separators (§8.3.5)

A separator delimits character-strings. The separators are:

| Separator | Rule |
|-----------|------|
| **Space** | The basic separator. More than one space may be used. |
| **Comma + space** (`, `) | May be used anywhere a separator space is used. For readability only. |
| **Semicolon + space** (`; `) | May be used anywhere a separator space is used. For readability only. |
| **Period + space** (`. `) | The separator period. Shall be used only to indicate end of a sentence, or as shown in formats. **The space after the period is REQUIRED** — the period character alone is not a separator. |
| **Parentheses** `(` `)` | Except in PICTURE strings. Must appear in balanced pairs. |
| **Colon** `:` | Required for reference modification. Except as part of the invocation operator (`::`). |
| **Quotation marks** `"` or `'` | Opening and closing delimiters of literals. |
| **Pseudo-text delimiters** `==` | Opening and closing pseudo-text delimiters (COPY/REPLACE). |

**IMPORTANT**: The separator period is `<period><space>`. At end-of-line in fixed-form, the period at column 72 (or last non-blank position) is treated as a separator period because the next line begins with spaces. In free-form, a period followed by end-of-line is also treated as followed by a space.

**Optional preceding space** (rule 8): A separator space may optionally precede all separators EXCEPT:
- As specified by reference format rules
- The closing delimiter of a literal (space would be part of the literal)
- The opening pseudo-text delimiter (space is required, not optional)

**Optional following space** (rule 9): A separator space may optionally follow any separator EXCEPT the opening delimiter of a literal (space would be part of the literal).

### 2.4 Literals (§8.3.3)

#### 2.4.1 Alphanumeric Literals (§8.3.3.2)

```
Format 1 (basic):    "character-string"  or  'character-string'
Format 2 (hex):      X"hex-digits"  or  X'hex-digits'
Format 3 (null):     Z"character-string"  or  Z'character-string'   (null-terminated)
```

Rules:
- Opening and closing delimiter must match (both `"` or both `'`)
- To include the delimiter character inside the literal, double it: `"He said ""hello"""` → `He said "hello"`
- Maximum length is implementor-defined
- An empty alphanumeric literal (`""` or `''`) is allowed; it is a zero-length literal.

#### 2.4.2 Numeric Literals (§8.3.3.3)

**Fixed-point numeric literal**: A character-string selected from digits 0-9, the plus sign, the minus sign, and the decimal point.

Rules:
1. Shall contain at least one digit
2. Shall not contain more than one sign character; if a sign is used, it shall appear as the leftmost character. If unsigned, the literal is nonnegative.
3. Shall not contain more than one decimal point. The decimal point is treated as an assumed decimal point.
4. The size of a fixed-point numeric literal is equal to the number of digits in the string.
5. An integer literal is a fixed-point numeric literal that contains no decimal point.

**Floating-point numeric literal**: Formed from two fixed-point numeric literals separated by the letter `E` without intervening spaces.

Format: `mantissa E exponent`
- The significand may be signed and shall include a decimal point
- The significand shall be from 1 to 16 digits in length (if signed, the floating-point numeric literal is considered to be signed)
- If unsigned, the literal is considered to be positive

#### 2.4.3 Boolean Literals (§8.3.3.4)

```
Format 1: B"0-and-1-string"  or  B'0-and-1-string'
Format 2: BX"hex-digits"  or  BX'hex-digits'
```

#### 2.4.4 National Literals (§8.3.3.5)

```
Format 1: N"character-string"  or  N'character-string'
Format 2: NX"hex-digits"  or  NX'hex-digits'
```

### 2.5 Figurative Constant Values (§8.3.6)

Figurative constants are generated by the compiler. They may be used wherever 'literal' appears in a format.

```
Format 1 (zero):      [ALL] { ZERO | ZEROES | ZEROS }
Format 2 (space):     [ALL] { SPACE | SPACES }
Format 3 (high):      [ALL] { HIGH-VALUE | HIGH-VALUES }
Format 4 (low):       [ALL] { LOW-VALUE | LOW-VALUES }
Format 5 (quote):     [ALL] { QUOTE | QUOTES }
Format 6 (all):       ALL literal-1
Format 7 (symbolic):  ALL symbolic-character-1
```

Rules:
- A figurative constant may be used wherever 'literal' appears in a format or when a rule allows it (except where explicitly prohibited, such as receiving operands)
- The singular and plural forms are interchangeable (ZERO = ZEROS = ZEROES)
- When used, the value occupies as many character positions as needed by context
- ZERO/ZEROS/ZEROES: numeric value 0 or the alphanumeric character "0" depending on context
- SPACE/SPACES: one or more space characters
- HIGH-VALUE/HIGH-VALUES: highest ordinal character in the collating sequence
- LOW-VALUE/LOW-VALUES: lowest ordinal character in the collating sequence
- QUOTE/QUOTES: the quotation mark character (`"`) — QUOTE shall NOT be used in place of a quotation mark to delimit a literal

### 2.6 PICTURE Character-Strings (§8.3.4)

A PICTURE character-string is a contiguous sequence of allowable PICTURE clause characters. It is NOT delimited by normal separator rules — it is terminated by a separator (space, period, comma, semicolon, etc.) that is not part of the PICTURE string.

The characters allowed in PICTURE strings include: `A`, `B`, `E`, `N`, `P`, `S`, `V`, `X`, `Z`, `9`, `0`, `/`, `,`, `.`, `+`, `-`, `*`, `CR`, `DB`, `$`, and parenthesized repeat counts like `9(5)`.

---

## 3. References and Identifiers (§8.4)

### 3.1 Qualification (§8.4.2.2)

```
Format 1 (qualified-data-name):
data-name-1 [ { IN | OF } data-qualifier ] ... [ file-report-qualifier ]

Format 2 (qualified-condition-name):
condition-name-1 [ { IN | OF } data-qualifier ] ... [ file-name-1 ]

Format 3 (qualified-index-name):
index-name-1 [ { IN | OF } data-qualifier ] ... [ file-name-1 ]

Format 4 (qualified-procedure-name):
paragraph-name-1 [ { IN | OF } section-name-1 ]

Format 5 (qualified-screen-name):
screen-name-1 [ { IN | OF } screen-name-2 ] ...

Format 6 (qualified-record-key-name):
record-key-name-1 [ { IN | OF } file-name-2 ]
```

IN and OF are interchangeable. Qualification proceeds from most specific to least specific (innermost to outermost).

### 3.2 Subscripts (§8.4.2.3)

```
General format:
qualified-data-name-with-subscripts:
  qualified-data-name-1 [ ( subscript ... ) ]

where subscript is:
  { ALL                          }
  { arithmetic-expression-1      }
  { index-name-1 [ { + | - } integer-1 ] }
```

Rules:
- Subscripts are enclosed in parentheses
- Multiple subscripts are separated by commas or spaces
- A subscript shall be a positive integer (1-based indexing)
- ALL subscript references all elements (used in specific contexts)

### 3.3 Identifiers (§8.4.3)

An identifier is a sequence of character-strings and separators used to reference a data item uniquely.

```
Format 1 (function-identifier):
  function-identifier-1

Format 2 (qualified-data-name-with-subscripts):
  qualified-data-name-with-subscripts-1

Format 3 (reference-modification):
  identifier-1 reference-modifier-1
```

Where reference-modifier is:
```
( leftmost-character-position : [ length ] )
```

**Identifier is defined recursively**: wherever the format for an identifier allows another identifier to be specified, that other identifier may be any of the formats.

### 3.4 Function-Identifier (§8.4.3.2)

```
General format:
FUNCTION { function-pointer-name-1  } [ ( argument-1 ) ... ]
         { function-name-1          }
         { intrinsic-function-name-1 }
```

Rules:
- If function-pointer-name-1 is specified, or the ALL phrase is specified in the REPOSITORY paragraph, or if function-prototype-name-1 or function-pointer-name-1 is specified, the word FUNCTION may be omitted from the function-identifier
- Argument-1 shall be an identifier, a literal, a boolean expression, or an arithmetic expression

### 3.5 Reference Modification (§8.4.3.3)

```
identifier-1 ( leftmost-character-position : [ length ] )
```

- leftmost-character-position: arithmetic expression evaluating to positive integer (1-based)
- length: arithmetic expression evaluating to positive integer (number of characters)
- If length is omitted, the reference extends to the end of the data item

---

## 4. Expressions (§8.8)

### 4.1 Arithmetic Expressions (§8.8.3)

An arithmetic expression can be:
1. An identifier of a numeric data item
2. A numeric literal
3. An identifier and a numeric literal separated by an arithmetic operator
4. Two arithmetic expressions separated by an arithmetic operator
5. An arithmetic expression preceded by a unary operator
6. An arithmetic expression enclosed in parentheses

**Operators and Precedence** (highest to lowest):

| Precedence | Operator | Meaning |
|------------|----------|---------|
| 1st | Unary `+`, `-` | Sign |
| 2nd | `**` | Exponentiation |
| 3rd | `*`, `/` | Multiplication, Division |
| 4th | `+`, `-` | Addition, Subtraction |

**Permitted combinations** — the following table shows where operators and operands may be adjacent:

| Left \ Right | identifier/literal | `+` `-` binary | `*` `/` `**` | Unary `+` `-` | `(` | `)` |
|---|---|---|---|---|---|---|
| identifier/literal | NO | YES | YES | NO | NO | YES |
| `+` `-` binary | YES | NO | NO | YES | YES | NO |
| `*` `/` `**` | YES | NO | NO | YES | YES | NO |
| Unary `+` `-` | YES | NO | NO | NO | YES | NO |
| `(` | YES | NO | NO | YES | YES | NO |
| `)` | NO | YES | YES | NO | NO | YES |

### 4.2 Conditional Expressions (§8.8.4)

A conditional expression evaluates to true or false. Types:

#### 4.2.1 Simple Conditions

**Relation condition** (§8.8.4.2):
```
operand-1  relational-operator  operand-2
```

Where relational-operator is one of (IS is optional throughout):
```
[IS] [NOT] GREATER [THAN]      or  [IS] [NOT] >
[IS] [NOT] LESS [THAN]         or  [IS] [NOT] <
[IS] [NOT] EQUAL [TO]          or  [IS] [NOT] =
[IS] GREATER [THAN] OR EQUAL [TO]  or  [IS] >=
[IS] LESS [THAN] OR EQUAL [TO]     or  [IS] <=
```

Note: `NOT >` is equivalent to `<=`, and `NOT <` is equivalent to `>=`.

Operand-1 and operand-2 can be: identifier, literal, arithmetic expression, index-name, or (in some cases) object reference.

**Class condition** (§8.8.4.3):
```
identifier-1 IS [NOT] { NUMERIC              }
                       { ALPHABETIC           }
                       { ALPHABETIC-LOWER     }
                       { ALPHABETIC-UPPER     }
                       { DBCS                 }
                       { KANJI                }
                       { class-name-1         }
```

**Condition-name condition** (§8.8.4.4):
```
condition-name-1
```
A condition-name (88 level) is evaluated as true if the associated data item has one of the values specified in the VALUE clause of the condition-name.

**Sign condition** (§8.8.4.5):
```
arithmetic-expression-1 IS [NOT] { POSITIVE }
                                  { NEGATIVE }
                                  { ZERO     }
```

#### 4.2.2 Complex Conditions (§8.8.4.9)

Formed by combining simple conditions with logical operators:

```
condition-1 { AND            } condition-2
             { OR             }
             { EXCLUSIVE-OR   }

NOT condition-1
```

**Logical operator precedence** (highest to lowest):
1. NOT
2. AND
3. OR
4. EXCLUSIVE-OR

Parentheses may override precedence.

#### 4.2.3 Abbreviated Combined Relation Conditions (§8.8.4.10)

When consecutive relation conditions are connected by AND or OR, the subject (operand-1) and optionally the relational operator may be omitted from subsequent conditions. The omitted elements are inherited from the nearest preceding complete relation condition.

**General form**:
```
subject relop object-1 { AND | OR } [NOT] [relop] object-2 ...
```

**Example 1** — subject and relational operator carried forward:
```
A > B AND C OR D
```
is equivalent to:
```
(A > B) AND (A > C) OR (A > D)
```

**Example 2** — new relational operator replaces the old one:
```
A > B AND < C
```
is equivalent to:
```
(A > B) AND (A < C)
```

**Example 3** — NOT negates the carried-forward relational operator:
```
A > B AND NOT C AND NOT D
```
is equivalent to:
```
(A > B) AND (A NOT > C) AND (A NOT > D)
```
which means:
```
(A > B) AND (A <= C) AND (A <= D)
```

**Example 4** — NOT with a new relational operator:
```
A > B OR NOT < C
```
is equivalent to:
```
(A > B) OR (A NOT < C)
```
which means:
```
(A > B) OR (A >= C)
```

**Rules**:
1. The subject is carried forward from the nearest preceding complete relation condition (the one that had an explicit subject)
2. The relational operator is carried forward from the nearest preceding relation condition (which may itself have been abbreviated)
3. If a new relational operator appears in the abbreviated form, it replaces the carried-forward operator going forward
4. NOT before an object (or before a relational operator) negates the relational operator for that particular comparison only — it does NOT negate the entire relation
5. Parentheses may NOT be used within an abbreviated combined relation condition to alter grouping — they may only be used around the entire abbreviated condition
6. The logical operators AND and OR within an abbreviated combined relation follow normal precedence (AND binds tighter than OR)

**CRITICAL PARSER NOTE**: The parser must track the "current subject" and "current relational operator" as state while parsing sequences of conditions connected by AND/OR. When a token after AND/OR is NOT a relational operator and NOT a keyword that begins a new statement, it may be an abbreviated object. This is one of the most complex parsing challenges in COBOL.

---

## 5. Program Structure

### 5.1 Compilation Group (§10)

A compilation group consists of one or more source units (compilation units).

A source unit begins with an identification division and ends with an end marker or the end of the compilation group.

Source units include:
- A program definition for an outermost program, including its nested programs
- A function definition
- A class definition (with factory and instance definitions)
- An interface definition
- Program/function/method prototypes

### 5.2 Source Unit Order

A source unit may contain one or more divisions, specified in this order:
1. Identification division (required)
2. Environment division (optional)
3. Data division (optional)
4. Procedure division (optional)

### 5.3 Identification Division (§11.2)

```
[ IDENTIFICATION ] DIVISION .
  { program-id-paragraph    }
  { function-id-paragraph   }
  { class-id-paragraph      }
  { factory-paragraph       }
  { object-paragraph        }
  { method-id-paragraph     }
  { interface-id-paragraph  }
[ options-paragraph ]
```

NOTE: IDENTIFICATION DIVISION header is optional in 2023 spec — a source unit can begin directly with PROGRAM-ID.

#### 5.3.1 PROGRAM-ID Paragraph (§11.10)

```
PROGRAM-ID . program-name-1 [ AS literal-1 ]
  [ { COMMON   } ]
  [ { INITIAL  } PROGRAM ]
  [ { RECURSIVE} ]
.
```

Rules:
- program-name-1 is a user-defined word
- AS literal-1 specifies an external name different from program-name-1
- COMMON: program can be called by programs in the same compilation group (nested programs)
- INITIAL: program is initialized each time it is called
- RECURSIVE: program may call itself
- The paragraph is terminated by a separator period

#### 5.3.2 End Program / End Function

```
END PROGRAM program-name-1 .
END FUNCTION function-name-1 .
```

### 5.4 Environment Division (§12.2)

```
ENVIRONMENT DIVISION .
[ configuration-section ]
[ input-output-section ]
```

#### 5.4.1 Configuration Section (§12.3)

```
CONFIGURATION SECTION .
[ SOURCE-COMPUTER . [ computer-name-1 [ WITH DEBUGGING MODE ] ] . ]
[ OBJECT-COMPUTER . [ ... ] . ]
[ SPECIAL-NAMES . [ special-names-content ] . ]
[ REPOSITORY . [ repository-content ] . ]
```

#### 5.4.2 Input-Output Section (§12.4)

```
INPUT-OUTPUT SECTION .
[ FILE-CONTROL .
  { file-control-entry } ... ]
[ I-O-CONTROL .
  [ i-o-control-content ] . ]
```

### 5.5 Data Division (§13.2)

```
DATA DIVISION .
[ FILE SECTION .
  { file-description-entry { record-description-entry } ... } ...
  { sort-merge-file-description-entry { record-description-entry } ... } ... ]
[ WORKING-STORAGE SECTION .
  { 77-level-description-entry | constant-entry | record-description-entry | type-declaration-entry } ... ]
[ LOCAL-STORAGE SECTION .
  { 77-level-description-entry | constant-entry | record-description-entry | type-declaration-entry } ... ]
[ LINKAGE SECTION .
  { 77-level-description-entry | constant-entry | record-description-entry | type-declaration-entry } ... ]
[ REPORT SECTION .
  { report-description-entry { report-group-description-entry } ... } ... ]
[ SCREEN SECTION .
  { screen-description-entry } ... ]
```

#### 5.5.1 Data Description Entry (§13.16)

```
Format 1 (data-description):
level-number  [ entry-name-clause ]
  [ REDEFINES data-name-1 ]
  [ IS TYPEDEF [ STRONG ] ]
  [ ALIGNED ]
  [ ANY LENGTH ]
  [ BASED ]
  [ BLANK WHEN ZERO ]
  [ CONSTANT RECORD ]
  [ DYNAMIC LENGTH [dynamic-length-structure-name-1] [LIMIT IS integer-1] ]
  [ IS EXTERNAL [ AS literal-1 ] ]
  [ IS GLOBAL ]
  [ GROUP-USAGE IS { BIT | NATIONAL } ]
  [ { JUSTIFIED | JUST } RIGHT ]
  [ occurs-clause ]
  [ picture-clause ]
  [ PROPERTY [WITH NO { GET | SET }] [IS FINAL] ]
  [ SAME AS data-name-2 ]
  [ select-when-clause ]
  [ [ SIGN IS ] { LEADING | TRAILING } [ SEPARATE CHARACTER ] ]
  [ { SYNCHRONIZED | SYNC } [ LEFT | RIGHT ] ]
  [ TYPE type-name-1 ]
  [ usage-clause ]
  [ validation-clauses ]
  [ value-clause ]
.

Format 2 (renames):
66  data-name-1  RENAMES  data-name-4  [ { THROUGH | THRU } data-name-5 ] .

Format 3 (condition-name):
88  condition-name-1  value-clause .

Format 4 (validation):
88  [ condition-name-2 ] value-clause .
```

**Level numbers**:
- `01-49`: Group and elementary items in a record hierarchy
- `66`: RENAMES entry
- `77`: Independent elementary item (Working-Storage, Local-Storage, Linkage)
- `88`: Condition-name (value condition)

**PICTURE clause** (§13.18.39):
```
{ PICTURE | PIC } IS character-string
```

**USAGE clause** (§13.18.60):
```
USAGE IS { BINARY              }
         { BINARY-CHAR         }
         { BINARY-DOUBLE       }
         { BINARY-LONG         }
         { BINARY-SHORT        }
         { BIT                 }
         { COMP | COMPUTATIONAL }
         { COMP-1              }    (implementation-defined)
         { COMP-2              }    (implementation-defined)
         { COMP-3              }    (implementation-defined)
         { COMP-4              }    (implementation-defined)
         { COMP-5              }    (implementation-defined)
         { DISPLAY             }
         { FLOAT-BINARY-32     }
         { FLOAT-BINARY-64     }
         { FLOAT-BINARY-128    }
         { FLOAT-DECIMAL-16    }
         { FLOAT-DECIMAL-34    }
         { FLOAT-EXTENDED      }
         { FLOAT-LONG          }
         { FLOAT-SHORT         }
         { INDEX               }
         { NATIONAL            }
         { OBJECT REFERENCE    }
         { PACKED-DECIMAL      }
         { POINTER             }
         { PROGRAM-POINTER     }
```

**VALUE clause** (§13.18.63):
```
VALUE IS { literal-1 | NULL }
```

**OCCURS clause** (§13.18.38):
```
Format 1 (fixed):
OCCURS integer-2 TIMES
  [ { ASCENDING | DESCENDING } KEY IS { data-name-2 } ... ] ...
  [ INDEXED BY { index-name-1 } ... ]

Format 2 (variable — DEPENDING ON):
OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1
  [ { ASCENDING | DESCENDING } KEY IS { data-name-2 } ... ] ...
  [ INDEXED BY { index-name-1 } ... ]
```

---

## 6. Procedure Division (§14)

### 6.1 Procedure Division Header (§14.2)

```
PROCEDURE DIVISION
  [ using-phrase ]
  [ RETURNING data-name-2 ]
.
```

Where using-phrase is:
```
USING { [ BY REFERENCE ] { [ OPTIONAL ] data-name-1 } ... } ...
      { BY VALUE { data-name-1 } ...                       }
```

### 6.2 Procedure Division Structure (§14.2.1)

```
Format 1 (with-sections):
procedure-division-header
[ DECLARATIVES .
  { section-name-1 SECTION .
    use-statement .
    [ sentence ] ... [ paragraph-name-1 . [ sentence ] ... ] ... } ...
  END DECLARATIVES . ]
[ { section-name-1 SECTION .
    [ sentence ] ... [ paragraph-name-1 . [ sentence ] ... ] ... } ... ]

Format 2 (without-sections):
procedure-division-header
[ sentence ] ... [ { paragraph-name-1 . [ sentence ] ... } ... ]
```

### 6.3 Procedures (§14.4)

**Section** (§14.4.2):
- section-name SECTION [segment-number] .
- Contains zero or more paragraphs
- Ends before next section or end of procedure division

**Paragraph** (§14.4.3):
- paragraph-name .
- Then zero or more sentences
- Ends before next paragraph-name or section-name

### 6.4 Sentences and Statements (§14.5)

A **sentence** = one or more procedural statements, the last of which is terminated by a **separator period**.

Statement types:
- **Imperative statement**: Unconditional action. A statement that either (a) has no conditional phrase, or (b) is delimited by its explicit scope terminator (making it a "delimited scope statement").
- **Conditional statement**: A statement with a conditional phrase that is NOT terminated by its explicit scope terminator.

### 6.5 Scope Termination (§14.5.3)

#### 6.5.1 Explicit Scope Termination (§14.5.3.2)

A statement may be explicitly terminated by its scope terminator (END-IF, END-PERFORM, etc.). A statement written with its explicit scope terminator is a **delimited scope statement** and is a subset of imperative statements.

#### 6.5.2 Implicit Scope Termination (§14.5.3.3)

A statement NOT explicitly terminated is implicitly terminated as follows:

1. **For an imperative statement NOT contained within another statement**:
   - a) Any element that follows the exhaustion of the statement's syntax
   - b) The next-encountered statement-name
   - c) A separator period

2. **For an imperative statement CONTAINED within another statement**:
   - a) Any element that terminates an imperative statement not contained within another statement (i.e., a period terminates ALL enclosing scopes)

3. **For a conditional statement NOT contained within another statement**:
   - By a separator period

4. **For a conditional statement CONTAINED within another statement**:
   - a) The termination of the containing statement, or
   - b) The next phrase of any containing statement

**CRITICAL**: A separator period terminates ALL open statement scopes — every containing IF, PERFORM, EVALUATE, etc. It does not just terminate the innermost statement.

---

## 7. Statement General Formats

### 7.1 ACCEPT Statement (§14.9.1)

```
Format 1 (device):
ACCEPT identifier-1 [ FROM mnemonic-name-1 ]
  [ ON EXCEPTION imperative-statement-1 ]
  [ NOT ON EXCEPTION imperative-statement-2 ]
  [ END-ACCEPT ]

Format 2 (temporal):
ACCEPT identifier-2 FROM { DATE [ YYYYMMDD ]  }
                          { DAY [ YYYYDDD ]    }
                          { DAY-OF-WEEK        }
                          { TIME               }
  [ END-ACCEPT ]

Format 3 (screen — extended):
ACCEPT identifier-1
  [ AT { LINE NUMBER { identifier-2 | integer-1 } }
       { COLUMN NUMBER { identifier-3 | integer-2 } } ]
  [ ON EXCEPTION imperative-statement-1 ]
  [ NOT ON EXCEPTION imperative-statement-2 ]
  [ END-ACCEPT ]
```

### 7.2 ADD Statement (§14.9.2)

```
Format 1 (simple):
ADD { identifier-1 | literal-1 } ...
  TO { identifier-2 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-ADD ]

Format 2 (giving):
ADD { identifier-1 | literal-1 } ...
  TO { identifier-2 | literal-2 }
  GIVING { identifier-3 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-ADD ]

Format 3 (corresponding):
ADD { CORRESPONDING | CORR } identifier-4
  TO identifier-5 [ rounded-phrase ]
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-ADD ]
```

### 7.3 CALL Statement (§14.9.4)

```
Format 1 (program):
CALL { identifier-1 | literal-1 }
  [ USING { [ BY REFERENCE ] { { identifier-2 } | OMITTED } ... } ...
          { BY CONTENT { { identifier-2 | literal-2 } | OMITTED } ... }
          { BY VALUE { identifier-2 | literal-2 } ...                  } ]
  [ RETURNING identifier-3 ]
  [ ON EXCEPTION imperative-statement-1 ]
  [ NOT ON EXCEPTION imperative-statement-2 ]
  [ END-CALL ]
```

Rules:
- BY REFERENCE is the default if no BY phrase is specified
- BY REFERENCE: the address of the data item is passed; the called program may modify it
- BY CONTENT: a copy of the data item is passed; the called program cannot modify the original
- BY VALUE: the value of the data item is passed as a binary value (used for interop with C/non-COBOL)
- OMITTED: indicates a parameter is not provided (can be used with BY REFERENCE or BY CONTENT)

### 7.4 CANCEL Statement (§14.9.5)

```
CANCEL { identifier-1 | literal-1 | program-prototype-name-1 } ...
```

### 7.5 CLOSE Statement (§14.9.6)

```
CLOSE { file-name-1 [ { REEL | UNIT } [ { FOR REMOVAL | WITH NO REWIND } ] ] } ...
```

### 7.6 COMPUTE Statement (§14.9.8)

```
Format 1 (arithmetic-compute):
COMPUTE { identifier-1 [ rounded-phrase ] } ... = arithmetic-expression-1
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-COMPUTE ]

Format 2 (boolean-compute):
COMPUTE { identifier-2 } ... = boolean-expression-1 [ END-COMPUTE ]
```

### 7.7 CONTINUE Statement (§14.9.9)

```
CONTINUE [ AFTER arithmetic-expression-1 SECONDS ]
```

Rules:
- Without AFTER: a no-operation statement, indicating that no executable statement is present. May be used as a conditional or imperative statement placeholder.
- With AFTER: execution is suspended for the specified number of seconds.

### 7.8 DISPLAY Statement (§14.9.11)

```
Format 1 (device):
DISPLAY { identifier-1 | literal-1 } ...
  [ UPON mnemonic-name-1 ] [ WITH NO ADVANCING ] [ END-DISPLAY ]

Format 2 (screen):
DISPLAY screen-name-1
  [ AT { LINE NUMBER { identifier-2 | integer-1 } }
       { COLUMN NUMBER { identifier-3 | integer-1 } } ]
  [ ON EXCEPTION imperative-statement-1 ]
  [ NOT ON EXCEPTION imperative-statement-2 ]
  [ END-DISPLAY ]
```

### 7.9 DIVIDE Statement (§14.9.12)

```
Format 1 (into):
DIVIDE { identifier-1 | literal-1 }
  INTO { identifier-2 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-DIVIDE ]

Format 2 (into-giving):
DIVIDE { identifier-1 | literal-1 }
  INTO { identifier-2 | literal-2 }
  GIVING { identifier-3 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-DIVIDE ]

Format 3 (by-giving):
DIVIDE { identifier-2 | literal-2 }
  BY { identifier-1 | literal-1 }
  GIVING { identifier-3 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-DIVIDE ]

Format 4 (into-remainder):
DIVIDE { identifier-1 | literal-1 }
  INTO { identifier-2 | literal-2 }
  GIVING identifier-3 [ rounded-phrase ]
  REMAINDER identifier-4
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-DIVIDE ]

Format 5 (by-remainder):
DIVIDE { identifier-2 | literal-2 }
  BY { identifier-1 | literal-1 }
  GIVING identifier-3 [ rounded-phrase ]
  REMAINDER identifier-4
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-DIVIDE ]
```

### 7.10 EVALUATE Statement (§14.9.13)

```
EVALUATE selection-subject [ ALSO selection-subject ] ...
  { { WHEN selection-object [ ALSO selection-object ] ... } ...
    imperative-statement-1 } ...
  [ WHEN OTHER imperative-statement-2 ]
  [ END-EVALUATE ]
```

Where selection-subject is:
```
{ identifier-1             }
{ literal-1                }
{ arithmetic-expression-1  }
{ boolean-expression-1     }
{ condition-1              }
{ TRUE                     }
{ FALSE                    }
```

Where selection-object is:
```
{ ANY                                                                        }
{ condition-2                                                                }
{ partial-expression-1                                                       }
{ TRUE                                                                       }
{ FALSE                                                                      }
{ [ NOT ] { identifier-2 | literal-2 | arithmetic-expression-2 }
    [ { THROUGH | THRU } { identifier-3 | literal-3 | arithmetic-expression-3 } ] }
```

Rules for selection-object:
- ANY: matches any value
- condition-2: a conditional expression (used when selection-subject is TRUE or FALSE)
- partial-expression-1: a relational operator followed by an operand (used when selection-subject is an identifier/expression, e.g., `WHEN > 10`)
- THROUGH/THRU: defines a range of values (inclusive)
- NOT: negates the match (including the range if THROUGH/THRU is specified)

### 7.11 EXIT Statement (§14.9.14)

```
Format 1 (simple):         EXIT
Format 2 (program):        EXIT PROGRAM [ RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION } ]
Format 3 (function):       EXIT FUNCTION [ RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION } ]
Format 4 (method):         EXIT METHOD [ RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION } ]
Format 5 (inline-perform): EXIT PERFORM [ CYCLE ]
Format 6 (procedure):      EXIT { PARAGRAPH | SECTION }
```

Rules:
- Format 1 (simple EXIT): A no-operation. When used as the only statement in a paragraph, it provides a procedure-name as an end-point for a PERFORM range.
- Format 2 (EXIT PROGRAM): Returns control from a called program to the calling program. Archaic — use GOBACK instead.
- Format 3 (EXIT FUNCTION): Returns control from a function to the invoker.
- Format 4 (EXIT METHOD): Returns control from a method to the invoker.
- Format 5 (EXIT PERFORM): Exits the innermost inline PERFORM. CYCLE causes the loop to skip to the next iteration.
- Format 6 (EXIT PARAGRAPH/SECTION): Transfers control to the end of the current paragraph or section.

### 7.12 GO TO Statement (§14.9.17)

```
Format 1 (unconditional):  GO TO procedure-name-1
Format 2 (depending):     GO TO { procedure-name-1 } ... DEPENDING ON identifier-1
```

Rules:
- A GO TO statement represented by Format 1 appearing in a consecutive sequence of imperative statements within a sentence, it shall appear as the last statement in that sequence
- A GO TO shall not be specified in a WHEN phrase of an exception-checking PERFORM statement

### 7.13 IF Statement (§14.9.19)

```
General Format:
IF condition-1 [ THEN ]
  { statement-1   }
  { NEXT SENTENCE }
[ ELSE
  { statement-2   }
  { NEXT SENTENCE } ]
[ END-IF ]
```

NOTE: NEXT SENTENCE is an archaic feature. THEN is optional (noise word).

Rules:
- Statement-1 and statement-2 represent either one or more imperative statements or a conditional statement optionally preceded by one or more imperative statements
- Statement-1 and statement-2 may each contain an IF statement (nesting)
- Nested IF statements may contain an ELSE phrase, and may also be terminated using an END-IF terminator
- Processing from left to right, whether an ELSE or END-IF matches a preceding IF is determined as follows:
  - a) any ELSE encountered is matched with the nearest preceding IF that either has not been already matched with an ELSE or has not been implicitly or explicitly terminated
  - b) any END-IF encountered is matched with the nearest preceding IF that has not been implicitly or explicitly terminated
- **PERIOD TERMINATION**: An IF statement without END-IF is a conditional statement. A separator period terminates the ENTIRE containing IF and all nested IFs — every open scope is closed. This is critical: `IF A THEN IF B THEN MOVE X TO Y.` — the period terminates BOTH IFs.
- When END-IF is present, the IF statement is a delimited-scope statement and is treated as an imperative statement (may appear inside other conditional phrases)
- NEXT SENTENCE transfers control to the implicit statement following the next separator period. It is NOT the same as CONTINUE — NEXT SENTENCE jumps past all enclosing statements to the next sentence

### 7.14 INITIALIZE Statement (§14.9.20)

```
INITIALIZE { identifier-1 } ... [ WITH FILLER ]
  [ { ALL | category-name } TO VALUE ]
  [ THEN REPLACING { category-name DATA BY { identifier-2 | literal-1 } } ... ]
  [ THEN TO DEFAULT ]
```

Where category-name is one of: ALPHABETIC, ALPHANUMERIC, ALPHANUMERIC-EDITED, BOOLEAN, DATA-POINTER, FUNCTION-POINTER, MESSAGE-TAG, NATIONAL, NATIONAL-EDITED, NUMERIC, NUMERIC-EDITED, OBJECT-REFERENCE, PROGRAM-POINTER

### 7.15 INSPECT Statement (§14.9.22)

```
Format 1 (tallying):
INSPECT [ BACKWARD ] identifier-1 TALLYING tallying-phrase

Format 2 (replacing):
INSPECT [ BACKWARD ] identifier-1 REPLACING replacing-phrase

Format 3 (tallying-and-replacing):
INSPECT [ BACKWARD ] identifier-1 TALLYING tallying-phrase REPLACING replacing-phrase

Format 4 (converting):
INSPECT [ BACKWARD ] identifier-1 CONVERTING
  { identifier-6 | literal-4 } TO { identifier-7 | literal-5 }
  [ after-before-phrase ]
```

Where tallying-phrase is:
```
{ identifier-2 FOR
  { CHARACTERS [ after-before-phrase ]                                    }
  { ALL     { { identifier-3 | literal-1 } [ after-before-phrase ] } ... }
  { LEADING { { identifier-3 | literal-1 } [ after-before-phrase ] } ... }
} ...
```

Where replacing-phrase is:
```
{ CHARACTERS BY replacement-item [ after-before-phrase ]                            }
{ ALL     { { identifier-3 | literal-1 } BY replacement-item [ after-before-phrase ] } ... }
{ LEADING { { identifier-3 | literal-1 } BY replacement-item [ after-before-phrase ] } ... }
{ FIRST   { { identifier-3 | literal-1 } BY replacement-item [ after-before-phrase ] } ... }
```

Where after-before-phrase is:
```
{ AFTER  } INITIAL { identifier-4 | literal-2 }
{ BEFORE }
```

Where replacement-item is: `{ identifier-5 | literal-3 }`

### 7.16 MOVE Statement (§14.9.25)

```
Format 1 (simple):
MOVE { identifier-1 | literal-1 } TO { identifier-2 } ...

Format 2 (corresponding):
MOVE { CORRESPONDING | CORR } identifier-3 TO identifier-4
```

### 7.17 MULTIPLY Statement (§14.9.26)

```
Format 1 (by):
MULTIPLY { identifier-1 | literal-1 }
  BY { identifier-2 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-MULTIPLY ]

Format 2 (giving):
MULTIPLY { identifier-1 | literal-1 }
  BY { identifier-2 | literal-2 }
  GIVING { identifier-3 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-MULTIPLY ]
```

### 7.18 OPEN Statement (§14.9.27)

```
OPEN { { INPUT  } { [ sharing-phrase ] [ retry-phrase ] file-name-1 [ WITH NO REWIND ] } ... } ...
     { { OUTPUT } }
     { { I-O    } }
     { { EXTEND } }

where sharing-phrase is:
SHARING WITH { ALL OTHER  }
             { NO OTHER   }
             { READ ONLY  }
```

### 7.19 PERFORM Statement (§14.9.28)

```
Format 1 (out-of-line):
PERFORM procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ]
  [ times-phrase | until-phrase | varying-phrase ]

Format 2 (inline):
PERFORM [ times-phrase | until-phrase | varying-phrase ]
  imperative-statement-1
END-PERFORM

Format 3 (exception-checking):
PERFORM [ WITH LOCATION ] imperative-statement-1
  { WHEN { { file-name-1 }                                              }
         { { exception-name-1 }                                          }
         { { exception-name-2 { FILE file-name-2 } ... }                }
    [ OTHER ] imperative-statement-2 } ...
  [ WHEN OTHER EXCEPTION imperative-statement-3 ]
  [ WHEN COMMON EXCEPTION imperative-statement-4 ]
  [ FINALLY imperative-statement-5 ]
END-PERFORM
```

Where times-phrase is:
```
{ identifier-1 | integer-1 } TIMES
```

Where until-phrase is:
```
[ WITH TEST { BEFORE | AFTER } ] UNTIL { condition-1 | EXIT }
```

Where varying-phrase is:
```
[ WITH TEST { BEFORE | AFTER } ]
VARYING { identifier-2    } FROM { identifier-3   } BY { identifier-4   } UNTIL condition-1
        { index-name-1    }      { index-name-2   }    { literal-2      }
                                  { literal-1      }
  [ AFTER { identifier-5  } FROM { identifier-6   } BY { identifier-7   } UNTIL condition-2 ] ...
          { index-name-3  }      { index-name-4   }    { literal-4      }
                                  { literal-3      }
```

**PERFORM traditional usage patterns (all 4)**:
1. **Basic** (no phrase): `PERFORM proc-1` or `PERFORM stmt END-PERFORM` — execute once
2. **TIMES**: `PERFORM proc-1 n TIMES` — execute n times
3. **UNTIL**: `PERFORM proc-1 UNTIL cond` — loop until condition is true
4. **VARYING**: `PERFORM proc-1 VARYING i FROM 1 BY 1 UNTIL i > 10` — counted loop

All 4 patterns work with both out-of-line (Format 1) and inline (Format 2).

**WITH TEST BEFORE** (default): Condition is tested before each execution. If true initially, the body is never executed.
**WITH TEST AFTER**: Condition is tested after each execution. The body always executes at least once.
**UNTIL EXIT**: Used only with inline PERFORM. Creates an infinite loop that must be exited via EXIT PERFORM.

### 7.20 READ Statement (§14.9.30)

```
Format 1 (sequential):
READ file-name-1 [ { NEXT | PREVIOUS } ] RECORD [ INTO identifier-1 ]
  [ { ADVANCING ON LOCK | IGNORING LOCK | retry-phrase } ]
  [ { WITH LOCK | WITH NO LOCK } ]
  [ AT END imperative-statement-1 ]
  [ NOT AT END imperative-statement-2 ]
  [ END-READ ]

Format 2 (random):
READ file-name-1 RECORD [ INTO identifier-1 ]
  [ { IGNORING LOCK | retry-phrase } ]
  [ { WITH LOCK | WITH NO LOCK } ]
  [ KEY IS { data-name-1 | record-key-name-1 } ]
  [ INVALID KEY imperative-statement-3 ]
  [ NOT INVALID KEY imperative-statement-4 ]
  [ END-READ ]
```

### 7.21 SEARCH Statement (§14.9.37)

```
Format 1 (serial):
SEARCH identifier-1 [ VARYING { identifier-2 | index-name-1 } ]
  [ AT END imperative-statement-1 ]
  { WHEN condition-1 { imperative-statement-2 | NEXT SENTENCE } } ...
  [ END-SEARCH ]

Format 2 (all — binary search):
SEARCH ALL identifier-1
  [ AT END imperative-statement-1 ]
  WHEN { data-name-1 { IS EQUAL TO | IS = } { identifier-3 | literal-1 | arithmetic-expression-1 } }
       { condition-name-1                                                                            }
    [ AND { data-name-2 { IS EQUAL TO | IS = } { identifier-4 | literal-2 | arithmetic-expression-2 } }
          { condition-name-2                                                                            } ] ...
  { imperative-statement-2 | NEXT SENTENCE }
  [ END-SEARCH ]
```

NOTE: NEXT SENTENCE is an archaic feature.

### 7.22 SET Statement (§14.9.39)

```
Format 1 (index-assignment):
SET { index-name-1 | identifier-1 } ...
  TO { arithmetic-expression-1 | index-name-2 | identifier-2 }

Format 2 (index-arithmetic):
SET { index-name-3 } ... { UP BY | DOWN BY } arithmetic-expression-2

Format 3 (condition-name):
SET { condition-name-1 } ... TO { TRUE | FALSE }

Format 4 (switch-setting):
SET { mnemonic-name-1 } ... TO { ON | OFF }

Format 5 (pointer):
SET { ADDRESS OF identifier-1 | pointer-name-1 } ...
  TO { ADDRESS OF identifier-2 | pointer-name-2 | NULL | NULLS }

Format 6 (program-pointer):
SET { program-pointer-name-1 } ...
  TO { ENTRY { identifier-3 | literal-1 } | pointer-name-2 | NULL | NULLS }
```

Rules:
- Format 3 (condition-name): Sets the associated data item to the value that makes the 88-level condition true. FALSE may only be specified if the FALSE phrase is present in the VALUE clause of the condition-name definition.
- Format 5/6: Used for pointer and program-pointer manipulation.

### 7.23 STOP Statement (§14.9.42)

```
Format 1 (run):
STOP RUN [ WITH { ERROR | NORMAL } STATUS { identifier-1 | literal-1 } ]

Format 2 (literal — archaic):
STOP literal-1
```

Rules:
- Format 1: Terminates the run unit and returns control to the operating system
- Format 2 (archaic): Suspends execution and displays literal-1. Execution may be resumed by the operator. This format is archaic and should not be used in new code.
- STOP RUN with ERROR STATUS returns a non-zero/error status to the operating system
- STOP RUN with NORMAL STATUS (or no STATUS phrase) returns a zero/success status

### 7.24 STRING Statement (§14.9.43)

```
STRING { { identifier-1 | literal-1 } ... DELIMITED BY { identifier-2 | literal-2 | SIZE } } ...
  INTO identifier-3
  [ WITH POINTER identifier-4 ]
  [ ON OVERFLOW imperative-statement-1 ]
  [ NOT ON OVERFLOW imperative-statement-2 ]
  [ END-STRING ]
```

### 7.25 SUBTRACT Statement (§14.9.44)

```
Format 1 (simple):
SUBTRACT { identifier-1 | literal-1 } ...
  FROM { identifier-2 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-SUBTRACT ]

Format 2 (giving):
SUBTRACT { identifier-1 | literal-1 } ...
  FROM { identifier-2 | literal-2 }
  GIVING { identifier-3 [ rounded-phrase ] } ...
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-SUBTRACT ]

Format 3 (corresponding):
SUBTRACT { CORRESPONDING | CORR } identifier-4
  FROM identifier-5 [ rounded-phrase ]
  [ ON SIZE ERROR imperative-statement-1 ]
  [ NOT ON SIZE ERROR imperative-statement-2 ]
  [ END-SUBTRACT ]
```

### 7.26 UNSTRING Statement (§14.9.48)

```
UNSTRING identifier-1
  [ DELIMITED BY [ ALL ] { identifier-2 | literal-1 }
    [ OR [ ALL ] { identifier-3 | literal-2 } ] ... ]
  INTO { identifier-4 [ DELIMITER IN identifier-5 ] [ COUNT IN identifier-6 ] } ...
  [ WITH POINTER identifier-7 ]
  [ TALLYING IN identifier-8 ]
  [ ON OVERFLOW imperative-statement-1 ]
  [ NOT ON OVERFLOW imperative-statement-2 ]
  [ END-UNSTRING ]
```

### 7.27 WRITE Statement (§14.9.51)

```
Format 1 (sequential):
WRITE { record-name-1 | FILE file-name-1 }
  [ FROM { identifier-1 | literal-1 } ]
  [ { { BEFORE | AFTER } ADVANCING
      { { identifier-2 | integer-1 } { LINE | LINES } }
      { { mnemonic-name-1 }                            }
      { PAGE                                            } } ]
  [ retry-phrase ]
  [ { WITH LOCK | WITH NO LOCK } ]
  [ AT { END-OF-PAGE | EOP } imperative-statement-1 ]
  [ NOT AT { END-OF-PAGE | EOP } imperative-statement-2 ]
  [ END-WRITE ]

Format 2 (random/indexed):
WRITE { record-name-1 | FILE file-name-1 }
  [ FROM { identifier-1 | literal-1 } ]
  [ retry-phrase ]
  [ { WITH LOCK | WITH NO LOCK } ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-WRITE ]
```

### 7.28 DELETE Statement (§14.9.10)

```
Format 1 (file):
DELETE file-name-1 RECORD
  [ retry-phrase ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-DELETE ]
```

### 7.29 REWRITE Statement (§14.9.35)

```
Format 1 (file):
REWRITE { record-name-1 | FILE file-name-1 }
  [ FROM { identifier-1 | literal-1 } ]
  [ retry-phrase ]
  [ { WITH LOCK | WITH NO LOCK } ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-REWRITE ]
```

### 7.30 START Statement (§14.9.41)

```
Format 1:
START file-name-1
  [ KEY IS { IS EQUAL TO       | IS =  | EQUALS       }
           { IS GREATER THAN   | IS >                  }
           { IS NOT LESS THAN  | IS NOT <              }
           { IS GREATER THAN OR EQUAL TO | IS >=       }
           { IS LESS THAN     | IS <                   }
           { IS NOT GREATER THAN | IS NOT >            }
           { IS LESS THAN OR EQUAL TO | IS <=          }
    { data-name-1 | record-key-name-1 | split-key-spec } ]
  [ { WITH SIZE { arithmetic-expression-1 } } ]
  [ INVALID KEY imperative-statement-1 ]
  [ NOT INVALID KEY imperative-statement-2 ]
  [ END-START ]
```

### 7.31 RETURN Statement (§14.9.34)

```
RETURN file-name-1 RECORD [ INTO identifier-1 ]
  AT END imperative-statement-1
  [ NOT AT END imperative-statement-2 ]
  [ END-RETURN ]
```

### 7.32 RELEASE Statement (§14.9.32)

```
RELEASE record-name-1 [ FROM { identifier-1 | literal-1 } ]
```

### 7.33 SORT Statement (§14.9.40)

```
Format 1 (file-sort):
SORT file-name-1
  { { ON } { ASCENDING | DESCENDING } KEY { data-name-1 } ... } ...
  [ WITH DUPLICATES IN ORDER ]
  [ COLLATING SEQUENCE IS alphabet-name-1
    [ ALPHANUMERIC IS alphabet-name-2 ]
    [ NATIONAL IS alphabet-name-3 ] ]
  { USING { file-name-2 } ...                                                     }
  { INPUT PROCEDURE IS procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ]   }
  { GIVING { file-name-3 } ...                                                     }
  { OUTPUT PROCEDURE IS procedure-name-3 [ { THROUGH | THRU } procedure-name-4 ]   }

Format 2 (table-sort):
SORT data-name-1
  { { ON } { ASCENDING | DESCENDING } KEY { data-name-2 } ... } ...
  [ WITH DUPLICATES IN ORDER ]
  [ COLLATING SEQUENCE IS alphabet-name-1 ]
```

### 7.34 MERGE Statement (§14.9.24)

```
MERGE file-name-1
  { { ON } { ASCENDING | DESCENDING } KEY { data-name-1 } ... } ...
  [ COLLATING SEQUENCE IS alphabet-name-1
    [ ALPHANUMERIC IS alphabet-name-2 ]
    [ NATIONAL IS alphabet-name-3 ] ]
  USING file-name-2 { file-name-3 } ...
  { GIVING { file-name-4 } ...                                                     }
  { OUTPUT PROCEDURE IS procedure-name-1 [ { THROUGH | THRU } procedure-name-2 ]   }
```

### 7.35 ALLOCATE Statement (§14.9.3)

```
Format 1 (data):
ALLOCATE { identifier-1 } [ INITIALIZED ] [ RETURNING identifier-2 ]

Format 2 (size):
ALLOCATE arithmetic-expression-1 CHARACTERS [ INITIALIZED ] RETURNING identifier-2
```

### 7.36 FREE Statement (§14.9.16)

```
FREE { identifier-1 } ...
```

### 7.37 RAISE Statement (§14.9.29)

```
RAISE { EXCEPTION exception-name-1 | identifier-1 }
```

### 7.38 RESUME Statement (§14.9.33)

```
RESUME [ AT ] NEXT STATEMENT
```

### 7.39 INVOKE Statement (§14.9.23)

```
INVOKE { identifier-1 | class-name-1 | SELF | SUPER }
  { literal-1 | identifier-2 | NEW }
  [ USING { [ BY REFERENCE ] { identifier-3 } ... } ...
          { BY CONTENT { identifier-3 } ...         }
          { BY VALUE { identifier-3 } ...           } ]
  [ RETURNING identifier-4 ]
  [ END-INVOKE ]
```

NOTE: INVOKE does not appear in the statement table as having conditional phrases or a scope terminator, but END-INVOKE is recognized by some implementations. The 2023 spec removed INVOKE in favor of inline method invocation syntax. For legacy COBOL, INVOKE is common.

### 7.40 GENERATE Statement (§14.9.15)

```
GENERATE { data-name-1 | report-name-1 }
```

### 7.41 INITIATE Statement (§14.9.21)

```
INITIATE { report-name-1 } ...
```

### 7.42 TERMINATE Statement (§14.9.47)

```
TERMINATE { report-name-1 } ...
```

### 7.43 SUPPRESS Statement (§14.9.45)

```
SUPPRESS PRINTING
```

### 7.44 UNLOCK Statement (§14.9.49)

```
UNLOCK file-name-1 { RECORD | RECORDS }
```

### 7.45 VALIDATE Statement (§14.9.50)

```
VALIDATE { identifier-1 } ...
```

### 7.46 RECEIVE Statement (§14.9.31)

```
Format 1 (message — legacy Communication):
RECEIVE cd-name-1 { MESSAGE | SEGMENT } INTO identifier-1
  [ NO DATA imperative-statement-1 ]
  [ WITH DATA imperative-statement-2 ]
  [ END-RECEIVE ]
```

NOTE: RECEIVE is part of the Communication facility. The statement table shows conditional phrases as `[NOT] ON EXCEPTION` for the modern form. In legacy COBOL, the conditional phrases are `NO DATA` / `WITH DATA` as shown above. Both forms use END-RECEIVE as the scope terminator.

### 7.47 SEND Statement (§14.9.38)

```
SEND { message-name-1 | identifier-1 }
  FROM identifier-2
  [ WITH { identifier-3 | ESI | EMI | EGI } ]
  [ { BEFORE | AFTER } ADVANCING
    { { identifier-4 | integer-1 } { LINE | LINES } }
    { PAGE                                           } ]
  [ END-SEND ]
```

NOTE: SEND is part of the Communication facility (archaic/deleted in 2023). For legacy code compatibility.

### 7.48 COMMIT Statement

```
COMMIT
```

### 7.49 ROLLBACK Statement

```
ROLLBACK
```

### 7.50 ALTER Statement (§14.9 — archaic)

```
ALTER { procedure-name-1 TO [ PROCEED TO ] procedure-name-2 } ...
```

NOTE: ALTER is an archaic feature. It dynamically modifies the target of a GO TO statement. Should not be used in new code, but may be encountered in legacy programs.

### 7.51 USE Statement (§14.9.46)

```
Format 1 (exception/error):
USE [ GLOBAL ] AFTER STANDARD { EXCEPTION | ERROR } PROCEDURE ON
  { file-name-1 } ...
  { INPUT          }
  { OUTPUT         }
  { I-O            }
  { EXTEND         }

Format 2 (reporting):
USE [ GLOBAL ] BEFORE REPORTING identifier-1

Format 3 (exception-object):
USE AFTER EXCEPTION OBJECT identifier-1
```

Rules:
- USE statements may only appear in the DECLARATIVES portion of the Procedure Division
- They define procedures to be executed automatically when certain conditions occur
- Format 1: Invoked when an I/O error occurs on the specified file(s) or file category
- Format 2: Invoked before a report group is presented (Report Writer)

### 7.52 GOBACK Statement (§14.9.18)

```
GOBACK [ RAISING { EXCEPTION exception-name-1 | identifier-1 | LAST EXCEPTION } ]
```

---

## 8. Common Phrases (§14.7)

### 8.1 ROUNDED Phrase (§14.7.4)

```
ROUNDED [ MODE IS { AWAY-FROM-ZERO       } ]
                   { NEAREST-AWAY-FROM-ZERO }
                   { NEAREST-EVEN           }
                   { NEAREST-TOWARD-ZERO    }
                   { PROHIBITED             }
                   { TOWARD-GREATER         }
                   { TOWARD-LESSER          }
                   { TRUNCATION             }
```

### 8.2 SIZE ERROR Phrase (§14.7.5)

```
[ ON SIZE ERROR imperative-statement-1 ]
[ NOT ON SIZE ERROR imperative-statement-2 ]
```

### 8.3 CORRESPONDING Phrase (§14.7.6)

D1 and D2 are identifiers that refer to alphanumeric group items, bit group items, national group items, or strongly typed group items, or variable-length groups.

### 8.4 RETRY Phrase (§14.7.3)

```
RETRY { { identifier-1 | integer-1 } TIMES    }
      { FOREVER                                 }
      { FOR { identifier-2 | arithmetic-expression-1 } SECONDS }
```

Rules:
- Specifies the retry action when an I/O statement fails due to a lock condition
- TIMES: retry the specified number of times
- FOREVER: retry indefinitely
- SECONDS: retry for the specified duration

### 8.5 THROUGH/THRU Phrase (§14.7.8)

A THROUGH/THRU phrase specifies a range of values, literal-1 through literal-2. The set of values included in the range is determined by the following rules:
1. When the range is defined by numeric literals, the range includes literal-1, literal-2, and all algebraic values between them.
2. When the range is defined by alphanumeric or national literals, the range depends on the collating sequence.

---

## 9. Statement Table — Complete Reference (Table 12, §14.5)

This table is critical for parser implementation. It determines:
- Which statements have conditional phrases (making them conditional statements when those phrases are present and no explicit scope terminator is used)
- Which statements have explicit scope terminators (END-xxx)

| Statement | Conditional Phrase(s) | Scope Terminator |
|-----------|----------------------|-----------------|
| ACCEPT | [NOT] ON EXCEPTION | END-ACCEPT |
| ADD | [NOT] ON SIZE ERROR | END-ADD |
| ALLOCATE | — | — |
| ALTER | — | — |
| CALL | [NOT] ON EXCEPTION | END-CALL |
| CANCEL | — | — |
| CLOSE | — | — |
| COMMIT | — | — |
| COMPUTE | [NOT] ON SIZE ERROR | END-COMPUTE |
| CONTINUE | — | — |
| DELETE | [NOT] INVALID KEY | END-DELETE |
| DISPLAY | [NOT] ON EXCEPTION | END-DISPLAY |
| DIVIDE | [NOT] ON SIZE ERROR | END-DIVIDE |
| EVALUATE | WHEN | END-EVALUATE |
| EXIT | — | — |
| FREE | — | — |
| GENERATE | — | — |
| GO TO | — | — |
| GOBACK | — | — |
| IF | THEN/ELSE | END-IF |
| INITIALIZE | — | — |
| INITIATE | — | — |
| INSPECT | — | — |
| INVOKE | — | — |
| MERGE | — | — |
| MOVE | — | — |
| MULTIPLY | [NOT] ON SIZE ERROR | END-MULTIPLY |
| OPEN | — | — |
| PERFORM | — (Format 1) / WHEN (Format 3) | END-PERFORM |
| RAISE | — | — |
| READ | [NOT] AT END, [NOT] INVALID KEY | END-READ |
| RECEIVE | [NOT] ON EXCEPTION | END-RECEIVE |
| RELEASE | — | — |
| RESUME | — | — |
| RETURN | [NOT] AT END | END-RETURN |
| REWRITE | [NOT] INVALID KEY | END-REWRITE |
| ROLLBACK | — | — |
| SEARCH | WHEN, AT END | END-SEARCH |
| SEND | [NOT] ON EXCEPTION | END-SEND |
| SET | — | — |
| SORT | — | — |
| START | [NOT] INVALID KEY | END-START |
| STOP | — | — |
| STRING | [NOT] ON OVERFLOW | END-STRING |
| SUBTRACT | [NOT] ON SIZE ERROR | END-SUBTRACT |
| SUPPRESS | — | — |
| TERMINATE | — | — |
| UNLOCK | — | — |
| UNSTRING | [NOT] ON OVERFLOW | END-UNSTRING |
| USE | — | — |
| VALIDATE | — | — |
| WRITE | [NOT] INVALID KEY, [NOT] AT END-OF-PAGE/EOP | END-WRITE |

---

## 10. Key Implementation Notes

### 10.1 Period Handling

The separator period (`. `) is the most critical parsing construct:
- It terminates ALL open scopes — every open IF, PERFORM, EVALUATE, etc.
- In fixed-form, a period at end-of-line (before col 73) is a separator period because the next line provides the required space
- In free-form, a period at end-of-line is also a separator period (end-of-line provides the space)
- **The space is part of the separator, not a separate token** — but for practical lexer implementation, the period can be recognized as a token and the space consumed

### 10.2 Statement Recognition

Statements are recognized by their leading keyword. The parser must:
1. Recognize the statement keyword (MOVE, IF, ADD, etc.)
2. Parse the statement's specific syntax
3. Determine whether the statement is imperative or conditional based on whether it has conditional phrases and whether it has an explicit scope terminator
4. Handle implicit scope termination by period

### 10.3 IF/ELSE Matching

ELSE and END-IF are matched left-to-right with the nearest preceding unmatched IF:
- ELSE → nearest preceding IF not already matched with ELSE or terminated
- END-IF → nearest preceding IF not already terminated

### 10.4 PERFORM Inline vs Out-of-line

The parser must distinguish:
- **Out-of-line**: `PERFORM procedure-name-1 [THRU procedure-name-2] ...` — no END-PERFORM
- **Inline**: `PERFORM [times/until/varying] imperative-statement-1 END-PERFORM`

The key disambiguator: if the token after PERFORM (and optional times/until/varying phrase) is a procedure-name followed by a period or another statement, it's out-of-line. If it's an imperative statement, it's inline (and must end with END-PERFORM).

### 10.5 EVALUATE WHEN Parsing

EVALUATE is complex because:
- Multiple ALSO clauses create multi-dimensional evaluation
- WHEN clauses match against selection-objects
- WHEN OTHER is the default case
- The WHEN keyword begins each case (one or more WHENs may precede a single imperative-statement)

### 10.6 Arithmetic Statement Pattern

ADD, SUBTRACT, MULTIPLY, DIVIDE, and COMPUTE all follow a common pattern:
1. Specific operand syntax
2. Optional ROUNDED phrase on receiving operands
3. Optional ON SIZE ERROR / NOT ON SIZE ERROR conditional phrases
4. Optional END-xxx scope terminator

When SIZE ERROR phrases are present and no END-xxx terminator is used, the statement is conditional.
