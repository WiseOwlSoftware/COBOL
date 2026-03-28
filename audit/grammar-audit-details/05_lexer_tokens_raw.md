Now I have enough information for the comprehensive audit. Let me also check the spec for what MINUS/PLUS look like in the reserved words list:

Looking at the spec reserved word list (section 8.9, lines 10772-10786), the "reserved words" at the bottom include special characters: `+`, `–` (minus), `*`, `/`, `**`, `>`, `<`, `<>`, `=`, `>=`, `<=`, `&`, `*>`, `::`, `>>`.

Also checking the spec for `MINUS` - it appears as a reserved word at line 10572, and `PLUS` at line 10625. Let me verify this:

The spec reserved word list (from lines 10310-10786) includes `MINUS` and `PLUS` as words, and `+` and `–` as special characters. Both are reserved.

Now I have all the information needed for the complete audit. Let me compile the findings:

---

## COBOL Lexer Audit — Complete Findings

### 1. RESERVED WORDS: Spec words missing from the lexer

Going through the spec's complete reserved word list (§8.9) against what the lexer defines as named tokens. Words handled as quoted strings in sub-grammars but not lexer tokens are noted.

**Completely absent from both lexer and parser grammars:**

```
MISSING RESERVED WORD: ACTIVE-CLASS — spec §8.9, no lexer token (OO class hierarchy)
MISSING RESERVED WORD: ALIGNED — spec §8.9, no lexer token (data alignment clause)
MISSING RESERVED WORD: ALLOCATE — spec §8.9, no lexer token (ALLOCATE statement)
MISSING RESERVED WORD: ANYCASE — spec §8.9, no lexer token (INSPECT ANYCASE)
MISSING RESERVED WORD: AREA — spec §8.9, no lexer token (AREAS clause)
MISSING RESERVED WORD: AREAS — spec §8.9, no lexer token (AREAS clause)
MISSING RESERVED WORD: AS — spec §8.9, no lexer token (used in OO: CLASS-ID ... AS "...")
MISSING RESERVED WORD: B-AND — spec §8.9, no lexer token (boolean bit operations)
MISSING RESERVED WORD: B-NOT — spec §8.9, no lexer token (boolean bit operations)
MISSING RESERVED WORD: B-OR — spec §8.9, no lexer token (boolean bit operations)
MISSING RESERVED WORD: B-SHIFT-L — spec §8.9, no lexer token
MISSING RESERVED WORD: B-SHIFT-R — spec §8.9, no lexer token
MISSING RESERVED WORD: B-SHIFT-LC — spec §8.9, no lexer token
MISSING RESERVED WORD: B-SHIFT-RC — spec §8.9, no lexer token
MISSING RESERVED WORD: B-XOR — spec §8.9, no lexer token
MISSING RESERVED WORD: BASED — spec §8.9, no lexer token (BASED storage)
MISSING RESERVED WORD: BINARY-CHAR — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: BINARY-DOUBLE — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: BINARY-LONG — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: BINARY-SHORT — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: BIT — spec §8.9, no lexer token (BIT usage)
MISSING RESERVED WORD: BLOCK — spec §8.9, no lexer token (BLOCK CONTAINS clause)
MISSING RESERVED WORD: BOOLEAN — spec §8.9, no lexer token (BOOLEAN class/usage)
MISSING RESERVED WORD: CF — spec §8.9, no lexer token (report group type: column footing)
MISSING RESERVED WORD: CH — spec §8.9, no lexer token (report group type: control heading)
MISSING RESERVED WORD: CODE — spec §8.9, no lexer token (REPORT CODE clause)
MISSING RESERVED WORD: CODE-SET — spec §8.9, no lexer token (FILE section CODE-SET clause)
MISSING RESERVED WORD: COL — spec §8.9, no lexer token (screen/report COLUMN)
MISSING RESERVED WORD: COLS — spec §8.9, no lexer token (screen/report COLUMNS)
MISSING RESERVED WORD: COLUMN — spec §8.9, no lexer token (screen/report COLUMN)
MISSING RESERVED WORD: COLUMNS — spec §8.9, no lexer token (screen/report COLUMNS)
MISSING RESERVED WORD: COMMA — spec §8.9, reserved word (distinct from separator comma); no named COMMA keyword token
MISSING RESERVED WORD: COMMIT — spec §8.9, no lexer token (transaction COMMIT)
MISSING RESERVED WORD: CONDITION — spec §8.9, no lexer token (CONDITION name in SPECIAL-NAMES)
MISSING RESERVED WORD: CONFIGURATION — spec §8.9, no lexer token (CONFIGURATION SECTION — parser uses IDENTIFIER match)
MISSING RESERVED WORD: CONSTANT — spec §8.9, no lexer token (CONSTANT level-01 entry)
MISSING RESERVED WORD: CONTAINS — spec §8.9, no lexer token (BLOCK CONTAINS, RECORD CONTAINS)
MISSING RESERVED WORD: CONTROL — spec §8.9, no lexer token (report CONTROL clause)
MISSING RESERVED WORD: CONTROLS — spec §8.9, no lexer token (report CONTROLS clause)
MISSING RESERVED WORD: COPY — spec §8.9, no lexer token (handled in preprocessor, not main lexer)
MISSING RESERVED WORD: CORR — spec §8.9, no lexer token (abbreviation of CORRESPONDING)
MISSING RESERVED WORD: DATA-POINTER — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: DE — spec §8.9, no lexer token (report group type: detail)
MISSING RESERVED WORD: DEFAULT — spec §8.9, no lexer token (DEFAULT VALUE clause)
MISSING RESERVED WORD: DESTINATION — spec §8.9, no lexer token (MESSAGE facility)
MISSING RESERVED WORD: DETAIL — spec §8.9, present only as quoted string 'DETAIL' in CobolParserJsonXml.g4, not a lexer token
MISSING RESERVED WORD: EC — spec §8.9, no lexer token (exception condition prefix EC-)
MISSING RESERVED WORD: EDITING — spec §8.9, no lexer token (PICTURE editing)
MISSING RESERVED WORD: EMD-START — spec §8.9 typo for END-START; END_START IS defined; this is a spec printing artifact
MISSING RESERVED WORD: END-RECEIVE — spec §8.9, no lexer token (message facility)
MISSING RESERVED WORD: END-SEND — spec §8.9, no lexer token (message facility)
MISSING RESERVED WORD: EO — spec §8.9, no lexer token (exception object)
MISSING RESERVED WORD: EXCEPTION-OBJECT — spec §8.9, no lexer token (OO exception object)
MISSING RESERVED WORD: EXCLUSIVE-OR — spec §8.9, no lexer token (abbreviated condition)
MISSING RESERVED WORD: FACTORY — spec §8.9, no lexer token (OO FACTORY paragraph)
MISSING RESERVED WORD: FARTHEST-FROM-ZERO — spec §8.9, no lexer token (ROUNDED phrase mode)
MISSING RESERVED WORD: FINAL — spec §8.9, no lexer token (FINAL reserved word for control break / OO method modifier)
MISSING RESERVED WORD: FINALLY — spec §8.9, no lexer token (exception handling)
MISSING RESERVED WORD: FLOAT-BINARY-32 — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-BINARY-64 — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-BINARY-128 — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-DECIMAL-16 — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-DECIMAL-34 — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-EXTENDED — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-INFINITY — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-LONG — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-NOT-A-NUMBER — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-NOT-A-NUMBER-QUIET — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-NOT-A-NUMBER-SIGNALING — spec §8.9, no lexer token
MISSING RESERVED WORD: FLOAT-SHORT — spec §8.9, no lexer token
MISSING RESERVED WORD: FORMAT — spec §8.9, no lexer token (XML/JSON format clause)
MISSING RESERVED WORD: FREE — spec §8.9, no lexer token (FREE statement)
MISSING RESERVED WORD: FUNCTION-ID — spec §8.9, no lexer token (user-defined function header)
MISSING RESERVED WORD: FUNCTION-POINTER — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: GENERATE — spec §8.9, present only as quoted string 'GENERATE' in CobolParserJsonXml.g4
MISSING RESERVED WORD: GET — spec §8.9, no lexer token (OO property GET accessor)
MISSING RESERVED WORD: GROUP — spec §8.9, no lexer token (GROUP-USAGE, report GROUP)
MISSING RESERVED WORD: GROUP-USAGE — spec §8.9, no lexer token
MISSING RESERVED WORD: HEADING — spec §8.9, no lexer token (report HEADING)
MISSING RESERVED WORD: I-O-CONTROL — note: lexer has I_O_CONTROL matching 'I-O-CONTROL' ✓
MISSING RESERVED WORD: IN-ARITHMETIC-RANGE — spec §8.9, no lexer token (exception name)
MISSING RESERVED WORD: INDICATE — spec §8.9, no lexer token (report INDICATE)
MISSING RESERVED WORD: INHERITS — spec §8.9, no lexer token (OO class INHERITS)
MISSING RESERVED WORD: INITIATE — spec §8.9, no lexer token (REPORT WRITER INITIATE statement)
MISSING RESERVED WORD: INPUT-OUTPUT — spec §8.9, no lexer token (INPUT-OUTPUT SECTION — parser uses IDENTIFIER match)
MISSING RESERVED WORD: INTERFACE — spec §8.9, no lexer token (OO INTERFACE definition body keyword)
MISSING RESERVED WORD: LAST — spec §8.9, no lexer token (LAST DETAIL clause in report)
MISSING RESERVED WORD: LENGTH — spec §8.9, no lexer token (LENGTH OF special register)
MISSING RESERVED WORD: LIMIT — spec §8.9, no lexer token (PAGE LIMIT clause)
MISSING RESERVED WORD: LIMITS — spec §8.9, no lexer token (PAGE LIMITS clause)
MISSING RESERVED WORD: LINAGE-COUNTER — spec §8.9, no lexer token (special register)
MISSING RESERVED WORD: LINE-COUNTER — spec §8.9, no lexer token (report special register)
MISSING RESERVED WORD: LOCALE — spec §8.9, no lexer token (LOCALE clause)
MISSING RESERVED WORD: LOCATION — spec §8.9, no lexer token (compiler directive context)
MISSING RESERVED WORD: MESSAGE-TAG — spec §8.9, no lexer token (message facility)
MISSING RESERVED WORD: MINUS — spec §8.9 lists MINUS as a reserved word; lexer has only operator MINUS : '-'. No named MINUS token distinct from '-'
MISSING RESERVED WORD: NATIONAL — spec §8.9, no lexer token (NATIONAL usage/category)
MISSING RESERVED WORD: NATIONAL-EDITED — spec §8.9, no lexer token (NATIONAL-EDITED category)
MISSING RESERVED WORD: NATIVE — spec §8.9, no lexer token (NATIVE collating sequence)
MISSING RESERVED WORD: NEAREST-TO-ZERO — spec §8.9, no lexer token (ROUNDED phrase mode)
MISSING RESERVED WORD: NESTED — spec §8.9, no lexer token (NESTED programs)
MISSING RESERVED WORD: NUMBER — spec §8.9, no lexer token (PAGE NUMBER clause)
MISSING RESERVED WORD: OBJECT — spec §8.9, present only as quoted string 'OBJECT' in CobolParserOO.g4
MISSING RESERVED WORD: OBJECT-REFERENCE — spec §8.9, no lexer token (OO usage OBJECT REFERENCE)
MISSING RESERVED WORD: OPTIONS — spec §8.9, no lexer token (OPTIONS paragraph)
MISSING RESERVED WORD: ORDER — spec §8.9, no lexer token (COLLATING SEQUENCE ORDER)
MISSING RESERVED WORD: OVERRIDE — spec §8.9, present only as quoted string 'OVERRIDE' in CobolParserOO.g4
MISSING RESERVED WORD: PAGE-COUNTER — spec §8.9, no lexer token (report special register)
MISSING RESERVED WORD: PF — spec §8.9, no lexer token (report group type: page footing)
MISSING RESERVED WORD: PH — spec §8.9, no lexer token (report group type: page heading)
MISSING RESERVED WORD: PLUS — spec §8.9 lists PLUS as a reserved word; lexer has only operator PLUS : '+'. No named PLUS token
MISSING RESERVED WORD: PRESENT — spec §8.9, no lexer token (PRESENT WHEN clause)
MISSING RESERVED WORD: PRINTING — spec §8.9, no lexer token (PRINTING clause)
MISSING RESERVED WORD: PROGRAM-POINTER — spec §8.9, no lexer token (usage type)
MISSING RESERVED WORD: PROPERTY — spec §8.9, no lexer token (OO PROPERTY clause)
MISSING RESERVED WORD: PROTOTYPE — spec §8.9, no lexer token (OO PROTOTYPE)
MISSING RESERVED WORD: RAISE — spec §8.9, no lexer token (RAISE statement)
MISSING RESERVED WORD: RAISING — spec §8.9, no lexer token (EXIT PROGRAM RAISING)
MISSING RESERVED WORD: RECEIVE — spec §8.9, no lexer token (message RECEIVE statement)
MISSING RESERVED WORD: REPLACE — spec §8.9, no lexer token (handled in preprocessor)
MISSING RESERVED WORD: REPORTS — spec §8.9, no lexer token (REPORTS clause in FD)
MISSING RESERVED WORD: REPOSITORY — spec §8.9, no lexer token (REPOSITORY paragraph)
MISSING RESERVED WORD: RESET — spec §8.9, no lexer token (SUM RESET)
MISSING RESERVED WORD: RESUME — spec §8.9, no lexer token (RESUME statement)
MISSING RESERVED WORD: RETRY — spec §8.9, no lexer token (RETRY phrase)
MISSING RESERVED WORD: RF — spec §8.9, no lexer token (report group type: report footing)
MISSING RESERVED WORD: RH — spec §8.9, no lexer token (report group type: report heading)
MISSING RESERVED WORD: ROLLBACK — spec §8.9, no lexer token (transaction ROLLBACK)
MISSING RESERVED WORD: SAME — spec §8.9, no lexer token (SAME RECORD AREA clause)
MISSING RESERVED WORD: SCREEN — spec §8.9, present only as quoted string 'SUPPRESS SPACES' in CobolParserJsonXml.g4, SCREEN itself absent
MISSING RESERVED WORD: SEND — spec §8.9, no lexer token (message SEND statement)
MISSING RESERVED WORD: SHARING — spec §8.9, no lexer token (SHARING clause on OPEN)
MISSING RESERVED WORD: SORT-MERGE — spec §8.9, no lexer token (SD-level sort file)
MISSING RESERVED WORD: SOURCE — spec §8.9, no lexer token (report SOURCE clause / SOURCE FORMAT)
MISSING RESERVED WORD: SOURCES — spec §8.9, no lexer token
MISSING RESERVED WORD: STANDARD-1 — spec §8.9, no lexer token (STANDARD-1 alphabet)
MISSING RESERVED WORD: STANDARD-2 — spec §8.9, no lexer token (STANDARD-2 alphabet)
MISSING RESERVED WORD: SUPPRESS — spec §8.9, present only as quoted string 'SUPPRESS' in CobolParserJsonXml.g4
MISSING RESERVED WORD: SYSTEM-DEFAULT — spec §8.9, no lexer token (ALPHABET SYSTEM-DEFAULT)
MISSING RESERVED WORD: TABLE — spec §8.9, no lexer token (reserved for future use / COBOL 2014 TABLE clause)
MISSING RESERVED WORD: TERMINATE — spec §8.9, no lexer token (REPORT WRITER TERMINATE)
MISSING RESERVED WORD: UNIVERSAL — spec §8.9, no lexer token (OO UNIVERSAL)
MISSING RESERVED WORD: UNLOCK — spec §8.9, no lexer token (UNLOCK statement)
MISSING RESERVED WORD: UPON — spec §8.9, no lexer token (DISPLAY UPON mnemonic)
MISSING RESERVED WORD: USER-DEFAULT — spec §8.9, no lexer token (ALPHABET USER-DEFAULT)
MISSING RESERVED WORD: VAL-STATUS — spec §8.9, no lexer token (VALIDATE)
MISSING RESERVED WORD: VALID — spec §8.9, no lexer token (VALIDATE)
MISSING RESERVED WORD: VALIDATE — spec §8.9, no lexer token (VALIDATE statement)
MISSING RESERVED WORD: VALIDATE-STATUS — spec §8.9, no lexer token (VALIDATE)
MISSING RESERVED WORD: XOR — spec §8.9, no lexer token (abbreviated condition XOR)
```

**Spec special-character reserved words (§8.9 bottom of list):**
```
MISSING RESERVED WORD: & (ampersand) — spec §8.9 lists '&' as reserved (string concatenation operator); no lexer token
MISSING RESERVED WORD: :: (double colon) — spec §8.9 lists '::' as reserved (OO invocation operator); no lexer token
MISSING RESERVED WORD: >> (double angle) — spec §8.9 lists '>>' as reserved (compiler directive prefix); no lexer token
```

---

### 2. EXTRA TOKENS: Lexer tokens not in the spec reserved word list (extensions)

```
EXTRA TOKEN: COMP_1 ('COMP-1') — not in spec reserved word list; IBM extension (binary float)
EXTRA TOKEN: COMP_2 ('COMP-2') — not in spec reserved word list; IBM extension (binary float)
EXTRA TOKEN: COMP_3 ('COMP-3') — not in spec reserved word list; IBM extension (packed decimal abbreviation)
EXTRA TOKEN: COMP_5 ('COMP-5') — not in spec reserved word list; IBM extension (native binary)
EXTRA TOKEN: COMPUTATIONAL_1 ('COMPUTATIONAL-1') — not in spec; IBM extension
EXTRA TOKEN: COMPUTATIONAL_2 ('COMPUTATIONAL-2') — not in spec; IBM extension
EXTRA TOKEN: COMPUTATIONAL_3 ('COMPUTATIONAL-3') — not in spec; IBM extension
EXTRA TOKEN: COMPUTATIONAL_5 ('COMPUTATIONAL-5') — not in spec; IBM extension
EXTRA TOKEN: YYYYMMDD — spec lists this as a context-sensitive word (§8.10), not a reserved word; treated as reserved lexer token
EXTRA TOKEN: YYYYDDD — spec lists this as a context-sensitive word (§8.10), not a reserved word; treated as reserved lexer token
EXTRA TOKEN: DATE_WRITTEN ('DATE-WRITTEN') — not in spec reserved word list; archaic/obsolete paragraph header
EXTRA TOKEN: DATE_COMPILED ('DATE-COMPILED') — not in spec reserved word list; archaic/obsolete paragraph header
EXTRA TOKEN: CHANNEL — not in spec reserved word list; IBM/vendor extension (mnemonic)
EXTRA TOKEN: PROCEED — not in spec reserved word list; archaic COBOL-68 word (ALTER ... PROCEED TO)
EXTRA TOKEN: GENERIC — not in spec reserved word list; vendor extension
EXTRA TOKEN: GOBACK — not in spec reserved word list; IBM extension (GOBACK statement)
EXTRA TOKEN: PACKED ('PACKED') — spec has PACKED-DECIMAL as a single reserved word; bare PACKED is not reserved
EXTRA TOKEN: EDITED — not in spec reserved word list as a standalone word (ALPHANUMERIC-EDITED and NUMERIC-EDITED are compound tokens)
EXTRA TOKEN: REMARKS — not in spec reserved word list; archaic identification paragraph
EXTRA TOKEN: CYCLE — spec §8.10 lists CYCLE as a context-sensitive word (EXIT statement context), not a reserved word
EXTRA TOKEN: PREVIOUS — spec §8.10 lists PREVIOUS as a context-sensitive word (READ statement context), not a reserved word
EXTRA TOKEN: RECURSIVE — spec §8.10 lists RECURSIVE as a context-sensitive word (PROGRAM-ID context), not a reserved word
EXTRA TOKEN: PARAGRAPH — spec §8.10 lists PARAGRAPH as a context-sensitive word (EXIT statement context), not a reserved word
EXTRA TOKEN: END_JSON ('END-JSON') — not in spec reserved word list (JSON is a vendor/2023 extension area)
EXTRA TOKEN: END_XML ('END-XML') — not in spec reserved word list
EXTRA TOKEN: END_METHOD ('END-METHOD') — not in spec reserved word list (OO extension per this implementation; spec uses END METHOD as two words)
```

**Note on INSTALLATION, SECURITY, AUTHOR:** These are not in the spec §8.9 reserved word list but the lexer has them as tokens. They are archaic/obsolete identification paragraph headers in COBOL-85 but were not carried forward as reserved in ISO 2002/2023.

```
EXTRA TOKEN: INSTALLATION — not in spec §8.9 reserved word list (archaic identification paragraph)
EXTRA TOKEN: SECURITY — not in spec §8.9 reserved word list (archaic identification paragraph)
EXTRA TOKEN: AUTHOR — not in spec §8.9 reserved word list (archaic identification paragraph)
```

---

### 3. NUMERIC LITERALS: Missing forms

```
MISSING LITERAL FORM: floating-point numeric literal (e.g., 1.5E+10, -3.2E-4) — spec §8.3.3.3.3 defines format as <significand>E<exponent>; the significand must include a decimal point and 1-36 digits; exponent up to 4 digits, optionally signed. No FLOATLIT rule exists in CobolLexer.g4.

MISSING LITERAL FORM: signed fixed-point numeric literal in main mode — spec §8.3.3.3.2 rule 2 allows a leading '+' or '-' sign as the leftmost character. The lexer has DECIMALLIT ([0-9]+'.'[0-9]+) and INTEGERLIT ([0-9]+) with no sign. Signed literals rely on PLUS/MINUS operator tokens being adjacent to INTEGERLIT/DECIMALLIT in the parser. In SUBSCRIPT mode, SIGNED_INTEGERLIT handles the adjacent-sign case. In DEFAULT mode, there is no SIGNED_DECIMALLIT and no SIGNED_INTEGERLIT — signed numeric literals are composed of two tokens (PLUS/MINUS + INTEGERLIT/DECIMALLIT) which may cause parse ambiguity in VALUE clauses and arithmetic contexts.

NOTE: DECIMALLIT also allows the form '.NNN' (leading-dot decimal, e.g. '.5') — spec §8.3.3.3.2 allows this (decimal point not the rightmost character, may appear anywhere except rightmost). This form is present in the lexer ✓ but it does not match spec rule 3 exactly: spec says the decimal point shall not appear as the rightmost character, but there is no prohibition on it being the leftmost — the DECIMALLIT '.' [0-9]+ form is correct.
```

---

### 4. ALPHANUMERIC LITERALS: Missing forms

```
MISSING LITERAL FORM: national literal N"..." / N'...' — spec §8.3.3.5 Format 1. No NATIONALLIT lexer rule. The STRINGLIT rule does not handle the N" prefix.

MISSING LITERAL FORM: hexadecimal-national literal NX"..." / NX'...' — spec §8.3.3.5 Format 2. No lexer rule.

MISSING LITERAL FORM: boolean literal B"..." / B'...' — spec §8.3.3.4 Format 1. No BOOLEANLIT lexer rule.

MISSING LITERAL FORM: hexadecimal-boolean literal BX"..." / BX'...' — spec §8.3.3.4 Format 2. No lexer rule.

MISSING LITERAL FORM: zero-length literals ("" or '') — spec §8.3.3.1 explicitly defines zero-length literals (opening and closing delimiters contiguous). The STRINGLIT rule uses (~["\r\n] | '""')* which allows zero iterations, so zero-length "" is matched ✓. The regex also allows '' ✓. This is correct.

PARTIAL: HEXLIT rule ([x]'"'[0-9a-f]+'"') has two issues:
  (a) HEXLIT requires at least one hex digit ([0-9a-f]+) — spec §8.3.3.2.3 NOTE 2 says hexadecimal-alphanumeric literals can be of zero length, so X"" should be valid. The lexer uses + not *.
  (b) HEXLIT only matches lowercase hex digits [0-9a-f]. Spec says hexadecimal digits are '0'-'9' and 'A'-'F'. While the lexer has caseInsensitive=true, the character class [0-9a-f] still only explicitly lists lowercase. With caseInsensitive=true this is effectively [0-9a-fA-F], so this is technically correct due to the lexer option ✓.
  (c) HEXLIT has no zero-length form — zero-length hex literal X"" would fail the [0-9a-f]+ requirement.

MISSING LITERAL FORM: Continuation of literals across lines — spec §6.3.5 (fixed form) and §6.4.2 (free form) describe literal continuation. The lexer comment states the input is preprocessed (fixed→free normalized), so this may be handled at the preprocessor level. However, the lexer does not support continuation indicators or multi-line string matching for the floating continuation indicator ('&' at end of line per spec §6.2.3). This is a gap if the preprocessor does not fully normalize all continuation forms.
```

---

### 5. FIGURATIVE CONSTANTS: Status

```
PRESENT ✓: ZERO / ZEROS / ZEROES — single token ZERO with three alternatives ✓
PRESENT ✓: SPACE / SPACES — single token SPACE ✓
PRESENT ✓: HIGH-VALUE / HIGH-VALUES — single token HIGH_VALUE ✓
PRESENT ✓: LOW-VALUE / LOW-VALUES — single token LOW_VALUE ✓
PRESENT ✓: QUOTE / QUOTES — single token QUOTE_ ✓
PRESENT ✓: ALL — token ALL present ✓ (used in parser as "ALL literal-1")

NOTE: NULL is in the spec §8.9 reserved word list and lexer has NULL_ : 'NULL'. However, NULL is not listed in §8.3.3.6 figurative constant formats — it is reserved as a USAGE NULL pointer value. ✓

NOTE: The spec §8.3.3.6.2 Format 7 (symbolic-character) is ALL symbolic-character-1 — this is handled at parse/bind level, not lexer level. ✓ by design.
```

---

### 6. PICTURE CHARACTER STRING

```
CORRECT BY DESIGN: The PIC/PICTURE keyword pushes into PICMODE, which captures the entire PIC string as a single PIC_STRING token. This is architecturally sound and avoids needing the grammar to tokenize individual PIC symbols.

POTENTIAL GAP: PIC_STRING rule is: ( ~[ \t\r\n.] | '.' ~[ \t\r\n] )+
This means a PIC string must contain at least one character (+, not *). A zero-character PIC string is invalid per spec, so this is correct ✓.
The rule stops at space or at a period followed by whitespace/EOF, which correctly distinguishes an embedded decimal point in PIC 9.99 from a sentence-ending period ✓.

NOTE: The PIC_STRING rule does not restrict characters to valid PIC symbols (9, X, A, N, S, V, P, Z, B, +, -, $, *, ,, ., 0, CR, DB, etc.). It accepts any non-whitespace non-period sequence (or period followed by non-whitespace). Validation of PIC symbol correctness happens at the semantic analysis layer, not the lexer. This is acceptable.
```

---

### 7. SEPARATOR RULES

```
CORRECT: COMMA_SEP : ',' [ \t\r\n]+ -> skip — spec §8.3.5 rule 2: comma immediately followed by space is a separator equivalent to space. The lexer skips it ✓.

CORRECT: SEMICOLON : ';' -> skip — spec §8.3.5 rule 2: semicolon immediately followed by space is a separator. The lexer unconditionally skips semicolons. NOTE: The spec says "semicolon immediately followed by a space" — the lexer skips bare ';' even without following space. This is a minor overapproximation (lenient), not a correctness problem in practice.

CORRECT: DOT : '.' — sentence-ending period ✓.

CORRECT: COMMA : ',' — bare comma (not followed by space) preserved for DECIMAL-POINT IS COMMA context ✓.

MISSING: The spec §8.3.5 rule 6 defines pseudo-text delimiters '==' as separators. There is no PSEUDO_TEXT_DELIM token in the lexer. This is relevant for COPY ... REPLACING == ... == BY == ... == syntax. Since COPY is handled by the preprocessor, this may be intentional.

MISSING: The spec §8.3.5 rule 7 mentions '::' (double colon / invocation operator). There is no DOUBLE_COLON token — only a single COLON token. The '::' is listed in §8.9 as a reserved special character (OO invocation operator). No lexer rule covers it.

NOTE on SEMICOLON in SUBSCRIPT mode: SUB_SEMICOLON : ';' is present and not skipped — the comment says "§8.3.5: semicolon is interchangeable with comma." This is consistent with the intent but contradicts the default mode where semicolons are unconditionally skipped. The asymmetry is intentional: inside subscripts, the semicolon can serve as subscript separator. This is fine.

NOTE on period as separator: The spec §8.3.5 rule 3 says period followed by a space is a separator. The lexer's DOT token does not enforce the "followed by space" constraint — it matches any period. The DECIMALLIT rule grabs period-flanked-by-digits before DOT can match, so the distinction between embedded decimal and sentence terminator is correctly handled by maximal munch ✓.
```

---

### Summary Counts

- **Missing reserved words from spec §8.9:** approximately 110 words absent as named lexer tokens. Many of these are for unimplemented features (OO, report writer, message facility, exception facility, transaction control, VALIDATE, ALLOCATE/FREE, float usage types, boolean/national data types). Some important ones that affect core COBOL-85/2002 programs: `CONFIGURATION`, `INPUT-OUTPUT`, `AREA/AREAS`, `BLOCK`, `CODE-SET`, `CONTAINS`, `NUMBER`, `UPON`, `SAME`, `NATIVE`, `STANDARD-1`, `STANDARD-2`, `GENERATE` (report writer), `INITIATE`, `TERMINATE`, `SOURCE`, `SUPPRESS`, `REPLACE`, `CORR`.

- **Extra tokens not in spec §8.9:** ~20 tokens that are IBM extensions (`COMP-1/2/3/5`, `COMPUTATIONAL-1/2/3/5`), archaic words (`DATE-WRITTEN`, `DATE-COMPILED`, `AUTHOR`, `INSTALLATION`, `SECURITY`, `REMARKS`, `PROCEED`, `GOBACK`), or context-sensitive words promoted to full tokens (`CYCLE`, `PREVIOUS`, `RECURSIVE`, `PARAGRAPH`, `YYYYMMDD`, `YYYYDDD`).

- **Missing literal forms:** floating-point literals, boolean literals (`B"..."`), hexadecimal-boolean literals (`BX"..."`), national literals (`N"..."`), hexadecimal-national literals (`NX"..."`), zero-length hex literals (`X""`), signed numeric literals as single tokens in default mode.

- **Figurative constants:** all 6 spec formats covered ✓.

- **Separator rules:** mostly correct; `::` (double-colon OO invocation operator) absent; `==` pseudo-text delimiter absent (acceptable if COPY handled in preprocessor); semicolon-skipping is slightly overly permissive but harmless.