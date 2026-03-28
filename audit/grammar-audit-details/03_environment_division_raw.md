Now I have everything I need for the full audit. Let me compile the complete analysis.

---

# Grammar vs. Spec Audit: Environment Division

## Scope: ISO/IEC 1989:2023, §12.3–12.4

---

## PART 1 — CONFIGURATION SECTION (§12.3)

### §12.3.2 General Format

**Spec:**
```
CONFIGURATION SECTION.
[ source-computer-paragraph ]
[ object-computer-paragraph ]
[ special-names-paragraph ]
[ repository-paragraph ]
```

**Grammar (`CobolParserCore.g4` line 205):**
```antlr
configurationSection
    : IDENTIFIER SECTION DOT configurationParagraph*
    ;
configurationParagraph
    : sourceComputerParagraph
    | objectComputerParagraph
    | specialNamesParagraph
    | vendorConfigurationParagraph
    ;
```

---

```
MISMATCH: configurationSection — header keyword
  Spec: CONFIGURATION SECTION.
  Grammar: IDENTIFIER SECTION DOT
  Gap: CONFIGURATION is parsed as a raw IDENTIFIER, not a keyword token. The lexer
       must not produce a CONFIGURATION token. Any identifier whose value happens to
       be "CONFIGURATION" will match. This is functionally OK for a single-keyword
       match but is architecturally wrong: if another paragraph follows that starts
       with any word the lexer classifies as IDENTIFIER, the section header is
       ambiguous or silently consumed. This should be CONFIGURATION_SECTION or the
       token CONFIGURATION followed by SECTION.
```

```
MISMATCH: configurationSection — ordering is unordered in grammar
  Spec: paragraphs in a fixed order: source-computer, object-computer,
        special-names, repository
  Grammar: configurationParagraph* (any order, any repetition)
  Gap: The spec mandates a fixed sequence. The grammar allows them in any order and
       allows duplicate paragraphs. No semantic validation is visible in the grammar.
       (Can be deferred to a semantic check, but worth noting as a gap.)
```

```
MISMATCH: configurationSection — REPOSITORY paragraph missing
  Spec: [ repository-paragraph ] is part of CONFIGURATION SECTION
  Grammar: No repositoryParagraph alternative in configurationParagraph
  Gap: REPOSITORY paragraph (§12.3.8) is completely absent from the grammar.
```

---

### §12.3.5 SOURCE-COMPUTER paragraph

**Spec:**
```
SOURCE-COMPUTER. [ computer-name-1 ] .
```
(Syntax rule 1: second period may be omitted if computer-name-1 is not specified.)

**Grammar (`CobolParserCore.g4` line 217):**
```antlr
sourceComputerParagraph
    : SOURCE_COMPUTER DOT computerName computerAttributes? DOT
    ;
computerName
    : IDENTIFIER
    ;
computerAttributes
    : (IDENTIFIER | STRINGLIT | INTEGERLIT)+
```

---

```
MISMATCH: sourceComputerParagraph — computer-name is mandatory
  Spec: [ computer-name-1 ] — computer-name-1 is optional
  Grammar: SOURCE_COMPUTER DOT computerName computerAttributes? DOT
           computerName is NOT optional (no ? qualifier)
  Gap: The grammar requires a computer name. The spec allows SOURCE-COMPUTER. with
       no computer name and an optional trailing period (syntax rule 1). Valid COBOL:
         SOURCE-COMPUTER.
       will fail to parse.
```

```
MISMATCH: sourceComputerParagraph — WITH DEBUGGING MODE clause missing
  Spec: ISO 1989:2023 does not define this as a standard clause (it was in older
        COBOL-85 specs), but many real-world programs contain:
          SOURCE-COMPUTER. IBM-390 WITH DEBUGGING MODE.
        The grammar absorbs this silently via computerAttributes which consumes any
        sequence of IDENTIFIER/STRINGLIT/INTEGERLIT tokens — so this accidentally
        works, but is architecturally untyped.
  Gap: Not a spec gap per ISO 2023, but noteworthy: the grammar has no typed
       WITH DEBUGGING MODE clause.
```

```
MISMATCH: sourceComputerParagraph — trailing DOT
  Spec: syntax rule 1 states the second period MAY be omitted when computer-name
        is absent.
  Grammar: always requires a second DOT.
  Gap: The grammar unconditionally requires both DOTs. SOURCE-COMPUTER. (with
       only one period) would fail to parse.
```

---

### §12.3.6 OBJECT-COMPUTER paragraph

**Spec:**
```
OBJECT-COMPUTER.
[ computer-name-1 ]
[
  CHARACTER CLASSIFICATION IS { locale-phrase-1 [ locale-phrase-2 ]
                                | FOR ALPHANUMERIC IS locale-phrase-1
                                  FOR NATIONAL IS locale-phrase-2
                                }
  |
  PROGRAM COLLATING SEQUENCE IS { alphabet-name-1 [ alphabet-name-2 ]
                                  | FOR ALPHANUMERIC IS alphabet-name-1
                                    FOR NATIONAL IS alphabet-name-2
                                  }
]
.
```
Where `locale-phrase` is: `{ locale-name-1 | LOCALE | SYSTEM-DEFAULT | USER-DEFAULT }`

Syntax rule 4: second period may be omitted if neither computer-name-1 nor any optional clause is specified.

**Grammar (`CobolParserCore.g4` line 221):**
```antlr
objectComputerParagraph
    : OBJECT_COMPUTER DOT computerName computerAttributes?
      programCollatingSequenceClause? DOT
    ;
programCollatingSequenceClause
    : PROGRAM COLLATING? SEQUENCE IS? IDENTIFIER
    ;
```

---

```
MISMATCH: objectComputerParagraph — computer-name is mandatory
  Spec: [ computer-name-1 ] — optional
  Grammar: computerName is not optional (no ?)
  Gap: OBJECT-COMPUTER. with no name fails to parse.
```

```
MISMATCH: objectComputerParagraph — CHARACTER CLASSIFICATION clause missing
  Spec: CHARACTER CLASSIFICATION IS { locale-phrase-1 [...] | FOR ALPHANUMERIC IS ... FOR NATIONAL IS ... }
  Grammar: No CHARACTER CLASSIFICATION clause at all
  Gap: Entire CHARACTER CLASSIFICATION clause (§12.3.6.2, first alternative) is absent.
```

```
MISMATCH: programCollatingSequenceClause — FOR ALPHANUMERIC / FOR NATIONAL alternatives missing
  Spec:
    PROGRAM COLLATING SEQUENCE IS { alphabet-name-1 [ alphabet-name-2 ]
                                  | FOR ALPHANUMERIC IS alphabet-name-1
                                    FOR NATIONAL IS alphabet-name-2 }
  Grammar:
    PROGRAM COLLATING? SEQUENCE IS? IDENTIFIER
  Gap: (1) Only accepts a single identifier — no second alphabet-name-2 alternative.
       (2) No FOR ALPHANUMERIC IS / FOR NATIONAL IS form.
       (3) COLLATING is marked optional (COLLATING?) but the spec shows it as required:
           "PROGRAM COLLATING SEQUENCE". Omitting COLLATING is a non-spec extension.
       (4) IS is marked optional (IS?) but in the spec IS is required.
```

```
MISMATCH: objectComputerParagraph — MEMORY SIZE clause not present
  Spec: Not in ISO 2023 (was COBOL-85 archaic). Grammar does not have it either.
  Gap: OK — no mismatch. computerAttributes? provides a catch-all for legacy tokens.
```

```
MISMATCH: objectComputerParagraph — trailing DOT optionality
  Spec: rule 4 — second period MAY be omitted if nothing specified.
  Grammar: DOT always required at end of objectComputerParagraph.
  Gap: OBJECT-COMPUTER. (bare) fails to parse.
```

---

## PART 2 — SPECIAL-NAMES paragraph (§12.3.7)

### §12.3.7.2 General Format

**Spec (complete):**
```
SPECIAL-NAMES.
[ alphabet-name-clause ] ...
[ CLASS class-name-1 [ FOR { ALPHANUMERIC | NATIONAL } ]
  IS { literal-5 [ { THROUGH | THRU } literal-6 ] } ... [ IN alphabet-name-4 ] ] ...
[ CRT STATUS IS data-name-2 ]
[ CURRENCY SIGN IS literal-7 [ WITH PICTURE SYMBOL literal-8 ] ] ...
[ CURSOR IS data-name-1 ]
[ DECIMAL-POINT IS COMMA ]
[ dynamic-length-structure-clause ] ...
[ LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 } ] ...
[ switch-name-1 { IS mnemonic-name-1 [ [ ON STATUS IS cond-name-1 ] [ OFF STATUS IS cond-name-2 ] ]
                | { ON STATUS IS condition-name-1 | OFF STATUS IS condition-name-2 } } ] ...
[ feature-name-1 IS mnemonic-name-2 ]
[ device-name-1 IS mnemonic-name-3 ]
[ symbolic-characters-clause ] ...
[ ORDER TABLE ordering-name-1 IS literal-9 ]
.

where alphabet-name-clause:
  ALPHABET alphabet-name-1 [ FOR ALPHANUMERIC ] IS {
    LOCALE [ locale-name-2 ]
    | NATIVE | STANDARD-1 | STANDARD-2 | code-name-1
    | { literal-phrase } ...
  }
  |
  ALPHABET alphabet-name-2 FOR NATIONAL IS {
    LOCALE [ locale-name-2 ] | NATIVE | UCS-4 | UTF-8 | UTF-16 | code-name-2
    | { literal-phrase } ...
  }

where literal-phrase:
  literal-1 [ { THROUGH | THRU } literal-2 ] [ { ALSO literal-3 } ... ]

where symbolic-characters-clause:
  SYMBOLIC CHARACTERS
  [ FOR { ALPHANUMERIC | NATIONAL } ]
  { { symbolic-character-1 } ... { IS | ARE } { integer-1 } ... } ...
  [ IN alphabet-name-3 ]
```

---

#### specialNamesParagraph

```
MISMATCH: specialNamesParagraph — at least one entry required
  Spec: All clauses are optional (SPECIAL-NAMES. with nothing but a period is valid
        per syntax rule 31: "One of the separator periods may be omitted if none of
        the clauses is specified.")
  Grammar: specialNameEntry+  (one or more required)
  Gap: SPECIAL-NAMES. (empty paragraph) will fail to parse. Must be
       specialNameEntry* (zero or more).
```

---

#### implementorSwitchEntry

**Spec (switch-name form):**
```
switch-name-1
{ IS mnemonic-name-1
  [ [ ON STATUS IS condition-name-1 ] [ OFF STATUS IS condition-name-2 ] ]
| { ON STATUS IS condition-name-1 | OFF STATUS IS condition-name-2 }
}
```

**Grammar:**
```antlr
implementorSwitchEntry
    : IDENTIFIER IS IDENTIFIER
      switchOnClause?
      switchOffClause?
    ;
switchOnClause
    : ON STATUS IS IDENTIFIER
    | ON IS? IDENTIFIER      -- non-spec
    ;
switchOffClause
    : OFF STATUS IS? IDENTIFIER
    | OFF IS? IDENTIFIER     -- non-spec
    ;
```

```
MISMATCH: implementorSwitchEntry — second spec form missing
  Spec: switch-name-1 { ON STATUS IS cond-1 | OFF STATUS IS cond-2 }
        (no IS mnemonic-name preceding it; the switch directly introduces status names)
  Grammar: IDENTIFIER IS IDENTIFIER ... — always requires IS mnemonic-name first
  Gap: The form where switch-name-1 is followed immediately by ON/OFF STATUS IS
       without an IS mnemonic-name assignment is not parseable.
```

```
MISMATCH: implementorSwitchEntry — also covers feature-name/device-name forms
  Spec: feature-name-1 IS mnemonic-name-2
        device-name-1 IS mnemonic-name-3
  Grammar: Both these are absorbed by IDENTIFIER IS IDENTIFIER (structurally identical
           to the switch IS mnemonic-name form). Functionally OK at parse level, but no
           typed distinction between switch, feature, and device mnemonic assignments.
  Gap: Not a parse gap, but semantic validation is bypassed.
```

```
MISMATCH: switchOnClause — non-spec "ON IS? IDENTIFIER" form
  Spec: ON STATUS IS condition-name-1  (STATUS is required)
  Grammar: ON IS? IDENTIFIER  (accepts "ON identifier" without STATUS)
  Gap: Grammar is more permissive than spec. "ON MYSWITCH" would be accepted.
```

```
MISMATCH: switchOffClause — STATUS IS optionality mismatch
  Spec: OFF STATUS IS condition-name-2  (STATUS and IS are both required)
  Grammar: OFF STATUS IS? IDENTIFIER | OFF IS? IDENTIFIER
           IS is optional in first form; both STATUS and IS absent in second form
  Gap: Grammar accepts "OFF STATUS myname" (no IS) and "OFF myname" (neither STATUS
       nor IS). Spec requires STATUS IS in all cases.
```

---

#### currencySignClause

**Spec:**
```
CURRENCY SIGN IS literal-7 [ WITH PICTURE SYMBOL literal-8 ]
```

**Grammar:**
```antlr
currencySignClause
    : CURRENCY SIGN? IS? literal
    ;
```

```
MISMATCH: currencySignClause — WITH PICTURE SYMBOL phrase missing
  Spec: CURRENCY SIGN IS literal-7 [ WITH PICTURE SYMBOL literal-8 ]
  Grammar: CURRENCY SIGN? IS? literal
  Gap: The WITH PICTURE SYMBOL literal-8 phrase is completely absent. This is required
       to support multi-character currency strings (spec syntax rule 23) and hex
       literals used as currency strings (syntax rule 19).
```

```
MISMATCH: currencySignClause — SIGN is optional in grammar, required in spec
  Spec: CURRENCY SIGN IS literal-7  (SIGN is required)
  Grammar: CURRENCY SIGN? IS? literal  (both SIGN and IS are optional)
  Gap: "CURRENCY '$'" (omitting SIGN) would be accepted. The spec requires SIGN.
       Similarly IS is required in spec but optional in grammar.
```

---

#### decimalPointClause

**Spec:**
```
DECIMAL-POINT IS COMMA
```

**Grammar:**
```antlr
decimalPointClause
    : DECIMAL_POINT IS IDENTIFIER
    ;
```

```
MISMATCH: decimalPointClause — COMMA is IDENTIFIER in grammar
  Spec: DECIMAL-POINT IS COMMA  (COMMA is a specific keyword token)
  Grammar: DECIMAL_POINT IS IDENTIFIER  (comment says "COMMA is IDENTIFIER")
  Gap: The grammar accepts any identifier after IS, not just COMMA. Validator would
       need to check the value. Architecturally fragile — should be COMMA token.
       Also, if COMMA is tokenized as a separator (not IDENTIFIER) in some lexer modes,
       this rule would break.
```

---

#### classDefinitionClause

**Spec:**
```
CLASS class-name-1 [ FOR { ALPHANUMERIC | NATIONAL } ]
IS { literal-5 [ { THROUGH | THRU } literal-6 ] } ... [ IN alphabet-name-4 ]
```

**Grammar:**
```antlr
classDefinitionClause
    : CLASS IDENTIFIER IS? classValueSet
    ;
classValueSet
    : classValueItem (COMMA classValueItem)*
    ;
classValueItem
    : literal ((THRU | THROUGH) literal)?
    ;
```

```
MISMATCH: classDefinitionClause — FOR ALPHANUMERIC / FOR NATIONAL phrase missing
  Spec: CLASS class-name-1 [ FOR { ALPHANUMERIC | NATIONAL } ] IS ...
  Grammar: CLASS IDENTIFIER IS? classValueSet  (no FOR phrase)
  Gap: Cannot specify FOR NATIONAL or FOR ALPHANUMERIC to restrict class to a
       character type.
```

```
MISMATCH: classDefinitionClause — IN alphabet-name-4 phrase missing
  Spec: IS { literal-5 [ THRU literal-6 ] } ... [ IN alphabet-name-4 ]
  Grammar: classValueItem ... (no IN phrase)
  Gap: The IN alphabet-name clause for restricting character sets is absent.
```

```
MISMATCH: classDefinitionClause — IS is optional in grammar, required in spec
  Spec: CLASS class-name-1 ... IS ...
  Grammar: IS?
  Gap: IS is syntactically required in the spec. Grammar makes it optional.
```

---

#### symbolicCharactersClause

**Spec:**
```
SYMBOLIC CHARACTERS
[ FOR { ALPHANUMERIC | NATIONAL } ]
{ { symbolic-character-1 } ... { IS | ARE } { integer-1 } ... } ...
[ IN alphabet-name-3 ]
```
Multiple symbolic characters can map to multiple integers in one entry.
Multiple symbolic character-1 names before a single IS/ARE integer list.

**Grammar:**
```antlr
symbolicCharactersClause
    : SYMBOLIC CHARACTERS symbolicCharacterEntry (COMMA symbolicCharacterEntry)*
    ;
symbolicCharacterEntry
    : IDENTIFIER IS literal
    ;
```

```
MISMATCH: symbolicCharactersClause — FOR ALPHANUMERIC / FOR NATIONAL missing
  Spec: [ FOR { ALPHANUMERIC | NATIONAL } ] after SYMBOLIC CHARACTERS
  Grammar: No FOR phrase
  Gap: Cannot declare national symbolic characters.
```

```
MISMATCH: symbolicCharactersClause — multiple names before single IS/ARE missing
  Spec: { symbolic-character-1 } ...  { IS | ARE } { integer-1 } ...
        Multiple names may precede a single IS integer list.
  Grammar: symbolicCharacterEntry = IDENTIFIER IS literal (one name, one value)
  Gap: "SYMBOLIC CHARACTERS SYM1 SYM2 ARE 1 2" is legal spec syntax but fails
       to parse. Only one-to-one form supported.
```

```
MISMATCH: symbolicCharactersClause — ARE keyword missing
  Spec: { IS | ARE }  (either keyword is valid)
  Grammar: symbolicCharacterEntry: IDENTIFIER IS literal  (only IS)
  Gap: ARE is not supported.
```

```
MISMATCH: symbolicCharactersClause — integer values as integerLiteral, not literal
  Spec: integer-1 (ordinal position integer values)
  Grammar: IDENTIFIER IS literal  (uses generic literal, which would include strings)
  Gap: Only integers should be valid; grammar accepts string literals too. Minor
       semantic gap — not a parse gap, but incorrect acceptance.
```

```
MISMATCH: symbolicCharactersClause — IN alphabet-name-3 phrase missing
  Spec: [ IN alphabet-name-3 ] at end of symbolic-characters-clause
  Grammar: No IN phrase
  Gap: Cannot associate symbolic characters with a specific alphabet.
```

---

#### alphabetClause

**Spec:**
```
ALPHABET alphabet-name-1 [ FOR ALPHANUMERIC ] IS {
  LOCALE [ locale-name-2 ]
  | NATIVE
  | STANDARD-1
  | STANDARD-2
  | code-name-1
  | { literal-phrase } ...
}
|
ALPHABET alphabet-name-2 FOR NATIONAL IS {
  LOCALE [ locale-name-2 ] | NATIVE | UCS-4 | UTF-8 | UTF-16 | code-name-2
  | { literal-phrase } ...
}

where literal-phrase: literal-1 [ { THROUGH | THRU } literal-2 ] [ { ALSO literal-3 } ... ]
```

**Grammar:**
```antlr
alphabetClause
    : ALPHABET IDENTIFIER IS alphabetDefinition
    ;
alphabetDefinition
    : alphabetEntry (COMMA? alphabetEntry)*
    ;
alphabetEntry
    : (IDENTIFIER | literal) ((THRU | THROUGH) (IDENTIFIER | literal))?
      (ALSO (IDENTIFIER | literal))*
    ;
```

```
MISMATCH: alphabetClause — FOR ALPHANUMERIC / FOR NATIONAL phrases missing
  Spec: ALPHABET name [ FOR ALPHANUMERIC ] IS ... | ALPHABET name FOR NATIONAL IS ...
  Grammar: ALPHABET IDENTIFIER IS alphabetDefinition
  Gap: The FOR ALPHANUMERIC and FOR NATIONAL qualifiers are absent. National
       alphabets cannot be declared.
```

```
MISMATCH: alphabetClause — IS is required in grammar (no ?), matches spec
  Spec: IS is required
  Grammar: IS (no ?)
  Gap: OK — matches.
```

```
MISMATCH: alphabetClause — LOCALE [ locale-name-2 ] alternative missing
  Spec: IS { LOCALE [ locale-name-2 ] | NATIVE | STANDARD-1 | ... }
  Grammar: alphabetEntry: (IDENTIFIER | literal) ...
           LOCALE, NATIVE, STANDARD-1 etc. would all land in IDENTIFIER — OK at parse
           level, but LOCALE's optional locale-name-2 following it is not handled.
  Gap: "ALPHABET MY-ALPHA IS LOCALE MY-LOCALE" — the locale-name-2 after LOCALE
       keyword would be an additional IDENTIFIER and would attempt to start another
       alphabetEntry. This will parse incorrectly (as a second entry with no THRU).
       Actually because alphabetDefinition allows (COMMA? alphabetEntry)*, it might
       consume MY-LOCALE as a standalone alphabetEntry, which is semantically wrong.
```

```
MISMATCH: alphabetClause — STANDARD-2, UCS-4, UTF-8, UTF-16 not keyword-typed
  Spec: Specific keywords STANDARD-2, UCS-4, UTF-8, UTF-16 for national alphabets
  Grammar: All consumed as IDENTIFIER — functionally works if these are not reserved
           words, but type-unsafe
  Gap: If UCS-4, UTF-8, UTF-16 are not IDENTIFIER tokens in the lexer (e.g., they
       contain digits), they may not tokenize as IDENTIFIER at all. Requires
       verification against lexer.
```

```
MISMATCH: alphabetClause — literal-phrase ALSO clause positioning
  Spec: literal-1 [ THRU literal-2 ] [ { ALSO literal-3 } ... ]
        ALSO is subordinate to a single literal entry
  Grammar: alphabetEntry: (IDENTIFIER | literal) (THRU ...)? (ALSO ...)*
           ALSO is attached to the same alphabetEntry — matches spec
  Gap: OK — matches structure.
```

---

#### LOCALE clause (missing entirely)

**Spec:**
```
LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 }
```

**Grammar:**
No `localeClause` in `specialNameEntry`.

```
MISMATCH: localeClause — completely absent
  Spec: LOCALE locale-name-1 IS { external-locale-name-1 | literal-4 }
        This clause is part of SPECIAL-NAMES (§12.3.7.2)
  Grammar: No localeClause rule; no LOCALE alternative in specialNameEntry
  Gap: The LOCALE clause cannot be parsed at all. Falls through to genericClause.
```

---

#### ORDER TABLE clause (missing)

**Spec:**
```
ORDER TABLE ordering-name-1 IS literal-9
```

**Grammar:**
No `orderTableClause`.

```
MISMATCH: orderTableClause — completely absent
  Spec: [ ORDER TABLE ordering-name-1 IS literal-9 ] in SPECIAL-NAMES
  Grammar: No such clause
  Gap: Falls through to genericClause. Minor: ORDER TABLE is a newer COBOL 2023
       feature; acceptable if out of scope but should be documented.
```

---

#### dynamic-length-structure-clause (missing)

**Spec:**
```
DYNAMIC LENGTH STRUCTURE dynamic-length-structure-name-1 IS
  { { [ SIGNED ] [ SHORT ] PREFIXED | DELIMITED } | physical-structure-name-1 }
```

**Grammar:**
No such clause.

```
MISMATCH: dynamic-length-structure-clause — completely absent
  Spec: Present in §12.3.7.2 SPECIAL-NAMES paragraph
  Grammar: No rule for DYNAMIC LENGTH STRUCTURE
  Gap: Falls through to genericClause. COBOL 2023 feature; acceptable if out of scope.
```

---

#### channelClause and reserveClause (vendor extensions)

These are not in the ISO 2023 spec for SPECIAL-NAMES. Both are vendor-specific extensions (IBM). They exist in the grammar as non-spec items.

```
MISMATCH: channelClause / reserveClause — no ISO spec citation
  Spec: Not present in §12.3.7.2
  Grammar: channelClause and reserveClause present in specialNameEntry
  Gap: These are IBM extensions, not ISO standard. Not wrong to have them, but
       they should be marked as implementor extensions.
```

---

## PART 3 — INPUT-OUTPUT SECTION (§12.4)

### §12.4.2 General Format

**Spec:**
```
INPUT-OUTPUT SECTION.
[ file-control-paragraph ]
[ i-o-control-paragraph ]
```

**Grammar (`CobolIO.g4` line 17):**
```antlr
inputOutputSection
    : IDENTIFIER SECTION DOT
      fileControlParagraph?
      ioControlParagraph?
    ;
```

```
MISMATCH: inputOutputSection — header is IDENTIFIER, not INPUT_OUTPUT keyword
  Spec: INPUT-OUTPUT SECTION.
  Grammar: IDENTIFIER SECTION DOT
  Gap: Same issue as configurationSection — INPUT-OUTPUT is parsed as a raw
       IDENTIFIER. If INPUT-OUTPUT is a hyphenated token that the lexer tokenizes
       correctly as IDENTIFIER, this is functionally correct but architecturally
       fragile. Should use a dedicated INPUT_OUTPUT token (or INPUT MINUS OUTPUT).
```

---

### §12.4.4 FILE-CONTROL paragraph

**Spec:**
```
FILE-CONTROL. [ file-control-entry ] ...
```

**Grammar:**
```antlr
fileControlParagraph
    : FILE_CONTROL DOT fileControlClauseGroup+
    ;
```

```
MISMATCH: fileControlParagraph — entries are required (+ vs *)
  Spec: [ file-control-entry ] ... — zero or more entries
  Grammar: fileControlClauseGroup+  — one or more required
  Gap: FILE-CONTROL. with no SELECT entries fails to parse.
```

---

### §12.4.5.1 File control entry — Format 1 (Indexed)

**Spec:**
```
SELECT [ OPTIONAL ] file-name-1
ASSIGN { TO { device-name-1 / literal-1 } ... [ USING data-name-1 ]
        | USING data-name-1 }
[ ACCESS MODE IS { DYNAMIC | RANDOM | SEQUENTIAL } ]
[ ALTERNATE RECORD KEY IS { data-name-2
                           | record-key-name-1 SOURCE IS { data-name-3 } ... }
  [ WITH DUPLICATES ] [ SUPPRESS WHEN literal-2 ] ] ...
[ collating-sequence-clause ] ...
[ FILE STATUS IS data-name-4 ]
[ LOCK MODE IS { MANUAL | AUTOMATIC } [ WITH LOCK ON [ MULTIPLE ] { RECORD | RECORDS } ] ]
[ ORGANIZATION IS ] INDEXED
RECORD KEY IS { data-name-5
               | record-key-name-2 SOURCE IS { data-name-6 } ... }
[ RESERVE integer-1 [ AREA | AREAS ] ]
[ SHARING WITH { ALL OTHER | NO OTHER | READ ONLY } ] .
```

**Grammar (`CobolIO.g4` line 28):**
```antlr
fileControlClauseGroup
    : SELECT OPTIONAL? fileName
      ( ASSIGN TO assignTarget )?
      fileControlClauses*
      DOT
    ;
assignTarget
    : IDENTIFIER
    | STRINGLIT
    ;
fileControlClauses
    : organizationClause
    | accessModeClause
    | recordKeyClause
    | alternateKeyClause
    | relativeKeyClause
    | fileStatusClause
    | vendorFileControlClause
    ;
```

---

#### ASSIGN clause

```
MISMATCH: ASSIGN clause — ASSIGN is optional in grammar, required in spec
  Spec: ASSIGN { TO ... | USING ... } — ASSIGN clause is required (all formats)
  Grammar: ( ASSIGN TO assignTarget )?   — entire ASSIGN clause is optional
  Gap: Spec §12.4.5.1 shows ASSIGN as a required part of the file control entry
       in all four formats. Grammar makes it optional.
```

```
MISMATCH: ASSIGN clause — USING data-name-1 alternative missing
  Spec: ASSIGN { TO { device-name-1 | literal-1 } ... [ USING data-name-1 ]
               | USING data-name-1 }
        Two forms: (1) TO target(s) optionally followed by USING; (2) USING alone
  Grammar: ASSIGN TO assignTarget  — only TO form, no USING form at all
  Gap: ASSIGN USING DATA-NAME (without TO) is a valid spec form but fails to parse.
       Also ASSIGN TO "MYFILE" USING MY-FILE-PATH (TO + USING) is not supported.
```

```
MISMATCH: ASSIGN clause — multiple device-names/literals after TO missing
  Spec: TO { device-name-1 | literal-1 } ...  (ellipsis = one or more)
  Grammar: ASSIGN TO assignTarget  (exactly one target)
  Gap: Multiple assignment targets are not supported.
```

```
MISMATCH: assignTarget — device-name vs. literal distinction lost
  Spec: TO { device-name-1 | literal-1 }  (device-name = system-name identifier;
        literal = alphanumeric literal)
  Grammar: IDENTIFIER | STRINGLIT  — structurally equivalent but device-name could
           be a hyphenated system-name (e.g., "PRINTER", "TAPE-1") that tokenizes
           as IDENTIFIER. OK functionally.
  Gap: Functionally adequate given lexer behavior. Minor.
```

---

#### ORGANIZATION clause

**Spec:**
```
[ ORGANIZATION IS ] { LINE SEQUENTIAL | RECORD SEQUENTIAL | RELATIVE | INDEXED }
```
Note: plain SEQUENTIAL without LINE or RECORD is not in spec — it is implied RECORD SEQUENTIAL.

**Grammar:**
```antlr
organizationClause
    : ORGANIZATION IS? organizationType
    ;
organizationType
    : LINE SEQUENTIAL
    | SEQUENTIAL
    | RELATIVE
    | INDEXED
    ;
```

```
MISMATCH: organizationClause — ORGANIZATION IS? — IS is optional
  Spec: [ ORGANIZATION IS ] — the whole phrase is optional but when written, IS
        is part of the phrase (standard shows "ORGANIZATION IS")
  Grammar: ORGANIZATION IS?
  Gap: IS is optional in grammar. Actually looking at the spec diagram: "[ ORGANIZATION IS ]"
       means the entire two-word phrase is optional; when ORGANIZATION is written, IS
       should follow. Minor mismatch — grammar allows ORGANIZATION without IS.
```

```
MISMATCH: organizationClause — RECORD SEQUENTIAL missing
  Spec: { LINE SEQUENTIAL | RECORD SEQUENTIAL | RELATIVE | INDEXED }
        RECORD SEQUENTIAL is a distinct variant
  Grammar: LINE SEQUENTIAL | SEQUENTIAL | RELATIVE | INDEXED
  Gap: "ORGANIZATION IS RECORD SEQUENTIAL" fails to parse. RECORD is only
       recognized as standalone, and SEQUENTIAL alone without RECORD is a
       non-spec shorthand. The spec (§12.4.5.10.2) shows:
         { LINE } SEQUENTIAL
         { RECORD }
       Both LINE SEQUENTIAL and RECORD SEQUENTIAL are required. Plain SEQUENTIAL
       is not in the spec syntax at all — it's RECORD SEQUENTIAL abbreviated.
```

---

#### ACCESS MODE clause

**Spec:**
```
ACCESS MODE IS { SEQUENTIAL | RANDOM | DYNAMIC }
```

**Grammar:**
```antlr
accessModeClause
    : ACCESS MODE? IS? accessMode
    ;
```

```
MISMATCH: accessModeClause — MODE and IS are optional
  Spec: ACCESS MODE IS  (all three words required)
  Grammar: ACCESS MODE? IS?  (MODE and IS both optional)
  Gap: "ACCESS SEQUENTIAL" (no MODE, no IS) would be accepted. Spec requires MODE IS.
```

```
MISMATCH: accessModeClause — access modes match spec
  Spec: SEQUENTIAL | RANDOM | DYNAMIC
  Grammar: SEQUENTIAL | RANDOM | DYNAMIC
  Gap: OK — matches.
```

---

#### RECORD KEY clause

**Spec:**
```
RECORD KEY IS { data-name-5
               | record-key-name-2 SOURCE IS { data-name-6 } ... }
```

**Grammar:**
```antlr
recordKeyClause
    : RECORD KEY IS dataReference
    ;
```

```
MISMATCH: recordKeyClause — SOURCE IS { data-name } ... form missing
  Spec: RECORD KEY IS { data-name-5 | record-key-name-2 SOURCE IS { data-name-6 } ... }
  Grammar: RECORD KEY IS dataReference  (only simple data-name form)
  Gap: The split-key SOURCE IS form cannot be parsed. Only simple key supported.
```

---

#### ALTERNATE RECORD KEY clause

**Spec:**
```
ALTERNATE RECORD KEY IS { data-name-2
                         | record-key-name-1 SOURCE IS { data-name-3 } ... }
[ WITH DUPLICATES ] [ SUPPRESS WHEN literal-2 ]
```

**Grammar:**
```antlr
alternateKeyClause
    : ALTERNATE RECORD? KEY IS dataReference
      (WITH? DUPLICATES)?
    ;
```

```
MISMATCH: alternateKeyClause — SOURCE IS { data-name } ... form missing
  Spec: alternate key can be specified as: record-key-name-1 SOURCE IS { data-name-3 } ...
  Grammar: Only dataReference (simple form)
  Gap: Split-key form for alternate key cannot be parsed.
```

```
MISMATCH: alternateKeyClause — RECORD keyword is optional in grammar
  Spec: ALTERNATE RECORD KEY IS  (RECORD is required)
  Grammar: ALTERNATE RECORD? KEY IS  (RECORD is optional)
  Gap: "ALTERNATE KEY IS MY-KEY" would be accepted. Spec requires RECORD.
```

```
MISMATCH: alternateKeyClause — SUPPRESS WHEN literal-2 missing
  Spec: [ SUPPRESS WHEN literal-2 ]  (key suppression for WRITE/REWRITE)
  Grammar: No SUPPRESS WHEN phrase at all
  Gap: Key suppression cannot be expressed.
```

```
MISMATCH: alternateKeyClause — WITH is optional, but spec shows WITH DUPLICATES
  Spec: WITH DUPLICATES  (WITH is part of the phrase)
  Grammar: (WITH? DUPLICATES)?  (WITH is optional)
  Gap: Minor — grammar accepts DUPLICATES without WITH. Common in practice.
```

---

#### RELATIVE KEY clause

**Spec:**
```
RELATIVE KEY IS data-name-7
```

**Grammar:**
```antlr
relativeKeyClause
    : RELATIVE KEY IS? dataReference
    ;
```

```
MISMATCH: relativeKeyClause — IS is optional
  Spec: RELATIVE KEY IS data-name-7  (IS required)
  Grammar: RELATIVE KEY IS? dataReference  (IS optional)
  Gap: "RELATIVE KEY MY-KEY" (no IS) would be accepted.
```

---

#### FILE STATUS clause

**Spec:**
```
FILE STATUS IS data-name-1
```

**Grammar:**
```antlr
fileStatusClause
    : FILE STATUS IS dataReference
    ;
```

```
MISMATCH: fileStatusClause — matches spec
  Spec: FILE STATUS IS data-name-1
  Grammar: FILE STATUS IS dataReference
  Gap: OK — matches.
```

---

#### LOCK MODE clause (completely missing)

**Spec:**
```
LOCK MODE IS { MANUAL | AUTOMATIC }
[ WITH LOCK ON [ MULTIPLE ] { RECORD | RECORDS } ]
```

**Grammar:**
No `lockModeClause` in `fileControlClauses`.

```
MISMATCH: lockModeClause — completely absent
  Spec: [ LOCK MODE IS { MANUAL | AUTOMATIC } [ WITH LOCK ON [ MULTIPLE ] { RECORD | RECORDS } ] ]
        Present in all four file control entry formats
  Grammar: No lockModeClause in fileControlClauses alternatives
  Gap: LOCK MODE clause silently falls through to vendorFileControlClause → genericClause.
       No typed parsing, no semantic validation possible.
```

---

#### COLLATING SEQUENCE clause (completely missing)

**Spec (Format 1, file-level):**
```
collating-sequence-clause:
  COLLATING SEQUENCE { IS alphabet-name-1 [ alphabet-name-2 ]
                     | FOR ALPHANUMERIC IS alphabet-name-1
                       FOR NATIONAL IS alphabet-name-2 }
```
**Spec (Format 2, key-level):**
```
COLLATING SEQUENCE OF { data-name-1 | record-key-name-1 } ... IS alphabet-name-3
```

**Grammar:**
No `collatingSequenceClause` in `fileControlClauses`.

```
MISMATCH: collatingSequenceClause (file-level) — completely absent
  Spec: [ collating-sequence-clause ] ... present in Format 1 (Indexed)
  Grammar: No such clause in fileControlClauses
  Gap: File-level COLLATING SEQUENCE cannot be parsed in SELECT entry.
       (A different sortCollatingPhrase exists for SORT/MERGE but not file SELECT.)
```

---

#### RESERVE clause (completely missing from file control)

**Spec:**
```
[ RESERVE integer-1 [ AREA | AREAS ] ]
```
Present in Formats 1, 2, 3 (not sort-merge Format 4).

**Grammar:**
No `reserveClause` in `fileControlClauses`.

```
MISMATCH: reserveClause (file control) — completely absent
  Spec: [ RESERVE integer-1 [ AREA | AREAS ] ] in SELECT entry
  Grammar: No such clause in fileControlClauses
  Gap: RESERVE clause in SELECT falls to vendorFileControlClause.
       Note: there IS a reserveClause in SPECIAL-NAMES (channelClause area) —
       that is a different, non-spec clause. The spec's RESERVE is in FILE-CONTROL.
```

---

#### SHARING clause (completely missing)

**Spec:**
```
[ SHARING WITH { ALL OTHER | NO OTHER | READ ONLY } ]
```

**Grammar:**
No `sharingClause` in `fileControlClauses`.

```
MISMATCH: sharingClause — completely absent
  Spec: [ SHARING WITH { ALL OTHER | NO OTHER | READ ONLY } ]
        Present in Formats 1, 2, 3
  Grammar: No such clause in fileControlClauses
  Gap: SHARING clause silently falls to vendorFileControlClause.
```

---

#### RECORD DELIMITER clause (missing from Format 3)

**Spec (Format 3 — sequential):**
```
RECORD DELIMITER IS { STANDARD-1 | feature-name-1 }
```

**Grammar:**
No `recordDelimiterClause` in `fileControlClauses`.

```
MISMATCH: recordDelimiterClause — completely absent
  Spec: [ RECORD DELIMITER IS { STANDARD-1 | feature-name-1 } ]
  Grammar: No such clause
  Gap: RECORD DELIMITER falls to vendorFileControlClause.
```

---

### §12.4.6 I-O-CONTROL paragraph

**Spec:**
```
I-O-CONTROL . [ [ apply-commit-clause ] . ] [ [ { same-clause } ... ] . ]

apply-commit-clause:
  APPLY COMMIT ON [ [ file-name-1 ] [ identifier-1 ] ] ...

same-clause (Format 1 — file-area):
  SAME AREA FOR file-name-1 { file-name-2 } ...

same-clause (Format 2 — record-area):
  SAME RECORD AREA FOR file-name-1 { file-name-2 } ...

same-clause (Format 3 — sort-merge-area):
  SAME { SORT | SORT-MERGE } AREA FOR file-name-1 { file-name-2 } ...
```

**Grammar:**
```antlr
ioControlParagraph
    : I_O_CONTROL DOT ioControlEntry+
    ;
ioControlEntry
    : genericClause DOT
    ;
```

```
MISMATCH: ioControlParagraph — entirely genericClause (no typed parsing)
  Spec: APPLY COMMIT and SAME clauses have well-defined grammar
  Grammar: ioControlEntry = genericClause DOT  (completely untyped)
  Gap: No typed parsing for ANY I-O-CONTROL content. The SAME AREA,
       SAME RECORD AREA, SAME SORT AREA, and APPLY COMMIT clauses are all
       silently swallowed by genericClause. Semantic validation is impossible.
```

```
MISMATCH: ioControlParagraph — entries required (+ vs *)
  Spec: I-O-CONTROL . [ ... ]  (content is optional)
  Grammar: ioControlEntry+  (one or more required)
  Gap: I-O-CONTROL. (empty) fails to parse.
```

---

## SUMMARY TABLE

| Rule | Spec Feature | Status |
|------|-------------|--------|
| `configurationSection` | CONFIGURATION keyword token | IDENTIFIER (fragile) |
| `configurationSection` | REPOSITORY paragraph | Missing |
| `sourceComputerParagraph` | computer-name optional | Required in grammar |
| `sourceComputerParagraph` | second period optional | Always required |
| `objectComputerParagraph` | computer-name optional | Required in grammar |
| `objectComputerParagraph` | CHARACTER CLASSIFICATION clause | Missing entirely |
| `programCollatingSequenceClause` | FOR ALPHANUMERIC / FOR NATIONAL | Missing |
| `programCollatingSequenceClause` | COLLATING optional, IS optional | Non-spec optional |
| `specialNamesParagraph` | empty paragraph valid | + requires at least one |
| `implementorSwitchEntry` | direct ON/OFF STATUS form (no IS mnemonic) | Missing |
| `switchOnClause` | STATUS required | Optional in grammar |
| `switchOffClause` | STATUS IS required | Both optional |
| `currencySignClause` | WITH PICTURE SYMBOL | Missing entirely |
| `currencySignClause` | SIGN IS required | Both optional |
| `decimalPointClause` | COMMA must be COMMA token | Uses IDENTIFIER |
| `classDefinitionClause` | FOR ALPHANUMERIC/NATIONAL | Missing |
| `classDefinitionClause` | IN alphabet-name | Missing |
| `classDefinitionClause` | IS required | Optional |
| `symbolicCharactersClause` | FOR ALPHANUMERIC/NATIONAL | Missing |
| `symbolicCharactersClause` | multiple names before IS/ARE | Missing |
| `symbolicCharactersClause` | ARE keyword | Missing |
| `symbolicCharactersClause` | IN alphabet-name-3 | Missing |
| `alphabetClause` | FOR ALPHANUMERIC / FOR NATIONAL | Missing |
| `alphabetClause` | LOCALE [ locale-name ] handling | Structurally broken |
| SPECIAL-NAMES | LOCALE clause | Missing entirely |
| SPECIAL-NAMES | ORDER TABLE clause | Missing entirely |
| SPECIAL-NAMES | DYNAMIC LENGTH STRUCTURE clause | Missing entirely |
| `inputOutputSection` | INPUT-OUTPUT keyword token | IDENTIFIER (fragile) |
| `fileControlParagraph` | empty FILE-CONTROL valid | + requires at least one |
| `fileControlClauseGroup` | ASSIGN required | Optional (?) |
| `fileControlClauseGroup` | ASSIGN USING form | Missing |
| `fileControlClauseGroup` | ASSIGN TO with multiple targets | Only one target |
| `organizationClause` | RECORD SEQUENTIAL | Missing (only plain SEQUENTIAL) |
| `organizationClause` | IS required | Optional |
| `accessModeClause` | MODE IS required | Both optional |
| `recordKeyClause` | SOURCE IS split-key form | Missing |
| `alternateKeyClause` | SOURCE IS split-key form | Missing |
| `alternateKeyClause` | RECORD required | Optional |
| `alternateKeyClause` | SUPPRESS WHEN literal | Missing entirely |
| `relativeKeyClause` | IS required | Optional |
| FILE-CONTROL | LOCK MODE clause | Missing entirely |
| FILE-CONTROL | COLLATING SEQUENCE clause | Missing entirely |
| FILE-CONTROL | RESERVE clause | Missing entirely |
| FILE-CONTROL | SHARING clause | Missing entirely |
| FILE-CONTROL | RECORD DELIMITER clause | Missing entirely |
| `ioControlParagraph` | typed APPLY COMMIT / SAME clauses | genericClause only |
| `ioControlParagraph` | empty I-O-CONTROL valid | + requires at least one |

---

## Critical vs. Non-Critical Classification

**Critical (will cause parse failures or incorrect semantic behavior on real COBOL):**
1. `sourceComputerParagraph` — computer-name not optional → `SOURCE-COMPUTER.` fails
2. `objectComputerParagraph` — computer-name not optional → `OBJECT-COMPUTER.` fails
3. `specialNamesParagraph` — `+` instead of `*` → empty SPECIAL-NAMES fails
4. `fileControlParagraph` — `+` instead of `*` → FILE-CONTROL with no SELECTs fails
5. `ASSIGN` — optional and USING form missing → many real programs fail
6. `RECORD SEQUENTIAL` organization — missing → common organization clause fails
7. `SUPPRESS WHEN` in alternate key — missing → indexed file programs fail
8. `LOCK MODE` clause — missing → shared-file programs fail
9. `COLLATING SEQUENCE` in SELECT — missing → indexed files with non-native keys fail

**Medium (feature gaps, common in non-trivial programs):**
10. `currencySignClause` — no PICTURE SYMBOL → multi-char currency fails
11. `symbolicCharactersClause` — no multi-name / no ARE / no IN
12. `alphabetClause` — no FOR NATIONAL / LOCALE broken
13. `classDefinitionClause` — no FOR NATIONAL / no IN
14. `SHARING`, `RESERVE`, `RECORD DELIMITER` clauses — all missing from SELECT
15. `ioControlParagraph` — fully untyped (SAME/APPLY COMMIT)

**Architectural (fragile but functionally tolerable):**
16. `CONFIGURATION SECTION` / `INPUT-OUTPUT SECTION` headers parsed as IDENTIFIER
17. `decimalPointClause` — COMMA parsed as IDENTIFIER
18. Various IS/MODE/STATUS optional where spec requires them