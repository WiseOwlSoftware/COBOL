# The text-layer diff is built; three class checks are not gated (2026-11-23)

**Built.** `scripts/spec/text_layer_diff.py` compares the word sequence of each PDF page's text layer (pymupdf) with that
page's slice of `specs/ANSI_COBOL_1985.md` and writes `docs/rearchitecture/spec-reconciliation/TEXT-LAYER-DIFF-1985.json`
(committed). Cases: `scripts/selftests/spec/text_layer_diff/`. Gate audit TEXT LAYER DIFF 1985 (`--check`, no PDF): RED when
the Markdown, the ledger or the script changed since the JSON was written (their SHA-256 are stored in it), naming the
re-run command. It prints what is unexplained and does not fail on it; `--require-zero` is PB6051's closing condition.

**Round-1 review (R1-1 to R1-5) fixed.** The first version folded OCR confusions out of every word and so hid real errors
(`identifier-i`, `file-name-l`, `comer`, `date-name-1`, `B-1`/`B-2`, `of`/`or`). Now: words align on letters and digits only,
nothing folded; OCR-NOISE needs the PDF word to be the damaged side and the Markdown word the well-formed one (known: a
reserved word, or spelled twice in the Markdown, or a digit suffix on a stem spelled twice; not malformed such as a
hyphenated word ending in a lone `l`/`i`/`o`/`s`/`z`), the PDF word absent from the Markdown, and the two a confusion apart.
CORRECTION needs one ledger `was`/`now` pair or note quote of that page to hold this hunk (PDF words in `was`, Markdown
words in `now`, with a neighbouring word unless the hunk is three words or the whole entry). Punctuation and case are classes
PUNCT and CASE. Words cancel only as a run of two or more moved together; a dropped `not` against a gained one stays two
differences. The verdict line names unexplained, uncovered and punctuation/case.

**Measured over all 976 pages (head of this change):** OCR-NOISE 5269, CORRECTION 823, FIGURE 1186, PUNCT 2322, CASE 423,
UNCOVERED 823, UNEXPLAINED 2092 on 528 pages (was 2272, 647, 818, 667 and 128 on 87 pages before the review; the Markdown
and ledger also moved with main). The rise is the point: the 128 hid the other 1964 behind folds. Of the 2092, by reason:
1040 a different word, 539 a single word moved, 513 text-layer words the Markdown lacks. Sampled, most are the layer's own
damage beyond the tolerated confusions (`meferenced`, `normumeric`, `Commun .cation`, a lone `I` speck, running-foot labels
read as `X1-19`), a few are real candidates; none is called OCR by this tool, so each page goes to a scoped agent, never to
a whole pass. The Markdown is unchanged in this branch. PB6051 and PB6135 stay open.

**Fold-classes still tolerated, and why.** (1) l/1/I/|, O/0, rn/m, Z/2, S/5, ligatures, case-free letters of a PDF word that
is absent from the Markdown against a well-formed Markdown word: the layer's systematic misreadings, tolerated only on the
PDF side. (2) One letter lost, one confusable letter or glyph swapped, one stray mark added, in a word of four letters or more
with no digit, same conditions: single-glyph damage. (3) A stray mark (a glyph of no letter, or a short fragment with no
digit) that the Markdown does not spell: scan specks. (4) A split or join by the layer's spacing or a line-end hyphen:
`FILE - CONTROL`/`FILE-CONTROL` (a hyphen against a space is therefore not seen). (5) A run of two or more words the layer
reads in another place (columns). (6) Tokens of no letter or digit (a lone `-`, a box glyph) are dropped from both sides. Not
tolerated, and visible: any difference of digits, punctuation, case, or a word the document also spells.

**Second reading (`--arbitrate`, tesseract).** Tesseract 5 reads each page with an UNEXPLAINED row (300 dpi render,
`--psm 4`; the per-page text is cached outside the tree, `--ocr-cache`, and only the result is committed; the version is
stored in the JSON). A row is OCR-ARBITRATED when the second reading holds the Markdown hunk's words in order beside the
neighbouring word and does not hold the layer's, and LIKELY-ERROR when it holds the layer's and not the Markdown's (two
independent readings against the transcription); both or neither stays UNEXPLAINED. The match is positional, never a bag of
the page's words. Two reading-order cases are never a witness for the layer: a moved single word, and Markdown words the
layer lacks that the second reading has elsewhere on the page (a table read by column); they stay UNEXPLAINED. The planted
cases for every outcome are case_10. `--require-zero` counts UNEXPLAINED and LIKELY-ERROR.

**Measured after arbitration (all 976 pages):** OCR-NOISE 5269, OCR-ARBITRATED 1344, CORRECTION 823, FIGURE 1186, PUNCT 2322,
CASE 423, UNCOVERED 823, LIKELY-ERROR 141, UNEXPLAINED 607 (from 2092; the residual is on
233 pages). The LIKELY-ERROR rows are differences between the Markdown and the
print that two readings agree on: some are errors, some are deliberate amendments applied in place with no recorded note
(p7 terms, p323, p420, p459), some the transcription's own footnote renumbering (pp328 to 332, 428, 429), and the repeated
`ELEMENT STD` table header (pp725 to 749) that the Markdown holds once. Each is a page for one scoped agent, never a pass.

**Every LIKELY-ERROR (page, line of the Markdown: layer / Markdown / tesseract):**

- p7 L272: pdf `provides the definition of the features that make up` / md `entitled` / tesseract `This document provides the definition of the programming language features that make up American National`
- p7 L280: pdf `American National Standard X3 23-1974,` / md `ANSI X3.23-1974, ISO 1989:1978,` / tesseract `refers to American National Standard X3.23-1968 e“Second Standard COBOL”`
- p7 L282: pdf `or` / md `"Third` / tesseract `Standard COBOL” refers to American`
- p7 L282: pdf `American National` / md `ANSI COBOL X3.23-1985, ISO 1989:1985, which superseded second` / tesseract `refers to American National Standard X3.23-1968 e“Second Standard COBOL” refers to American`
- p7 L282: pdf `X3.23-1985, which supersedes American National` / md `COBOL. Both "Amendment 1" and "Intrinsic Function Amendment" refer to ANSI X3.23a-1989, ISO 1989:1985/Amd.1:1992. Both " ...` / tesseract `National Standard X3.23-1985. Within this document, the following terms are used’ e‘First Standard COBOL” refers to Amer ...`
- p7 L288: pdf `X3.23-1974` / md `COBOL" refers to third Standard COBOL updated first with the content of amendment 1 and then with the content of amendme ...` / tesseract `National Standard X3.23-1985. Within this document, the following terms are used’ e‘First Standard COBOL” refers to Amer ...`
- p21 L1169: pdf `11` / md `12` / tesseract `standard defines 11 functional processing`
- p21 L1169: pdf `Segmentation.` / md `Intrinsic Function.` / tesseract `SCOPE AND PURPOSE The scope of`
- p28 L1433: pdf `specification` / md `specifications` / tesseract `outside the source program to`
- p32 L1605: pdf `X` / md `` / tesseract `program header wecveccccccvcccccrccvencsrsecerssrevcseseees N xX`
- p118 L5125: pdf `0.` / md `O.` / tesseract `A through 0. Messages have`
- p131 L5618: pdf `reference modifiers,` / md `reference-modifier,` / tesseract `transmission and handling of data on`
- p132 L5660: pdf `position` / md `positions` / tesseract `any digit position to the`
- p132 L5662: pdf `When` / md `(2) A numeric defined in the Data Division that does not include any digit positions to the right of the decimal point.  ...` / tesseract `decimal point. When the term '‘integer' appears in general formats, integer must not be a numeric data item, and must no ...`
- p144 L6151: pdf `which` / md `that` / tesseract `operator or a relation character.`
- p152 L6407: pdf `reserved` / md `with user-defined` / tesseract `disjoint sets; reserved words and system-names`
- p152 L6407: pdf `system-names` / md `function-names. b. Function-names,` / tesseract `words and user-defined words form disjoint sets; reserved`
- p152 L6409: pdf `class` / md `classification` / tesseract `sets. The same COBOL word`
- p152 L6409: pdf `this` / md `such` / tesseract `class of a specific occurrence`
- p152 L6409: pdf `word` / md `words` / tesseract `same COBOL word may be`
- p152 L6423: pdf `'O',` / md `'0',` / tesseract `'C', 'Z', 'O', '9', and`
- p168 L7107: pdf `4.3.8.3.1` / md `4.3.8.4.1` / tesseract `Reference Modification any given dimension`
- p168 L7111: pdf `4.3.8.3.2` / md `4.3.8.4.2` / tesseract `the item as specified in`
- p168 L7133: pdf `4.3.8.3.4` / md `(5) The function referenced by function-name-1 and its arguments, if any, must be an alphanumeric function. 4.3.8.4.4` / tesseract `or subscripted. 4.3.8.3.4 General Rules (1) Each character of a data item referenced by data-name-1 is assigned an ordin ...`
- p169 L7187: pdf `The` / md `4.3.8.5.2` / tesseract `of Names.) The general format`
- p246 L10636: pdf `symbolic- character-l` / md `symbolic-character-1` / tesseract `the second symbolic~ character-l is paired`
- p246 L10638: pdf `symbolic-characters-l` / md `symbolic-characters-1` / tesseract `phrase of the ALPHABET clause:`
- p248 L10698: pdf `the value of` / md `` / tesseract `specified by the value of literal-l, and`
- p256 L10993: pdf `RIGHT` / md `` / tesseract `eee JUST RIGHT 5.6.3 Syntax`
- p262 L11223: pdf `'O',` / md `'0',` / tesseract `and b. If unsigned, its`
- p285 L11970: pdf `Literal` / md `` / tesseract `Identifier or/ Unary or Literal`
- p289 L12121: pdf `usage` / md `class condition does not include the word NOT and identifier-1 is a zero-length group item, the result` / tesseract `of the uppercase letters A, B, C, Z, and space. (5) An operand is in conformance to class-name-l, if it consists`
- p289 L12121: pdf `operand being tested must be described as DISPLAY.` / md `class test is always false.` / tesseract `of the uppercase letters A, B, C, Z, and space. (5) An`
- p292 L12239: pdf `following May element` / md `` / tesseract `is 'NOT', 'AND', 'OR'. Thus, specifying 'condition-1 OR NOT condition-2 AND condition-3'`
- p292 L12239: pdf `immediately` / md `` / tesseract `therefore, be used to depart from the normal precedence of logical operators as`
- p323 L13347: pdf `considered to apply to` / md `matched with` / tesseract `condition is true and the NEXT SENTENCE phrase`
- p323 L13347: pdf `immediately` / md `nearest` / tesseract `If the condition is true`
- p323 L13347: pdf `already paired` / md `implicitly` / tesseract `not been already paired with an ELSE or END-IF.`
- p323 L13347: pdf `END-IF.` / md `explicitly terminated.` / tesseract `statement~-l or its surrogate NEXT SENTENCE`
- p328 L13546: pdf `7. (2)` / md `8. (3)` / tesseract `5 through 7. (2) For use`
- p328 L13562: pdf `(3)` / md `(4)` / tesseract `INSPECT statement, the content of`
- p328 L13566: pdf `(4) Subscripting associated with` / md `(5) If` / tesseract `identifier-7 respectively. (4) Subscripting associated with any identifier`
- p328 L13574: pdf `(6)` / md `(7)` / tesseract `7. (2) For use in`
- p329 L13608: pdf `(7)` / md `(8)` / tesseract `comparison cycle is considered as`
- p329 L13614: pdf `6.` / md `7.` / tesseract `general rule 13. b. If no`
- p330 L13638: pdf `(8)` / md `(9) Identifier-1 is a sending data item. (10)` / tesseract `FORMAT 1: (8) The required words ALL and LEADING are adjectives that`
- p330 L13644: pdf `(9)` / md `(11)` / tesseract `adjective appears. (9) The content`
- p330 L13646: pdf `(10)` / md `(12)` / tesseract `INSPECT statement. (10) The rules`
- p331 L13664: pdf `(11)` / md `(13)` / tesseract `Nucleus INSPECT (11) If identifier-1,`
- p331 L13668: pdf `(12)` / md `(14)` / tesseract `FORMAT 2: (12) The required`
- p331 L13670: pdf `(13)` / md `(15)` / tesseract `adjective appears. (13) The rules`
- p331 L13680: pdf `(14)` / md `(16)` / tesseract `result of the execution of this`
- p331 L13686: pdf `(15)` / md `(17)` / tesseract `FORMAT 3: (15) A format`
- p331 L13690: pdf `(16)` / md `(18)` / tesseract `FORMAT 4: (16) A format`
- p332 L13702: pdf `(17)` / md `(19)` / tesseract `data item. (17) If identifier-4,`
- p338 L13957: pdf `ALPHANUMERIC` / md `` / tesseract `NUMERIC NONINTEGER ALPHANUMERIC NUMERIC EDITED`
- p338 L13962: pdf `NUMERIC` / md `` / tesseract `Yes/4a Yes/4b ALPHANUMERIC EDITED Yes/4c`
- p345 L14217: pdf `the associated index` / md `identifier-3, literal-1` / tesseract `set of statements is performed until the condition`
- p345 L14217: pdf `be set` / md `correspond` / tesseract `statement must be set to an`
- p345 L14217: pdf `the data item referenced by identifier-2` / md `identifier-6, index-name-4,` / tesseract `set of statements is performed until the condition specified by`
- p345 L14217: pdf `identifier-5` / md `literal-3` / tesseract `specified or implied,/no transfer to`
- p345 L14217: pdf `be equal` / md `correspond` / tesseract `statement must be set to an`
- p345 L14217: pdf `index-name-4.` / md `index-name-3.` / tesseract `associated with the operands specified in condition-1`
- p393 L15795: pdf `AREAlor` / md `AREA or` / tesseract `I~-O SAME 2.13 THE SAME CLAUSE`
- p407 L16266: pdf `Sequential I-O VALUE OF` / md `` / tesseract `OF 3.9 THE VALUE OF CLAUSE 3.9.1 Function`
- p410 L16388: pdf `Format Single-` / md `` / tesseract `Sequential I-O CLOSE File Category`
- p410 L16388: pdf `Non-Reel/Unit Reel/Unit` / md `` / tesseract `Single- Multi- Non-Reel/Unit Reel/Unit Reel/Unit CLOSE`
- p410 L16396: pdf `F` / md `` / tesseract `Non-Reel/Unit Reel/Unit Reel/Unit CLOSE Cc`
- p411 L16448: pdf `or the reel is on a non-reel/unit medium,` / md `` / tesseract `the file is positioned other than at its end, the closing operations`
- p414 L16548: pdf `records` / md `` / tesseract `be created OUTPUT Normal open;`
- p415 L16587: pdf `VII-5, The` / md `II-1,` / tesseract `(See page VII-5, The File Attribute`
- p415 L16587: pdf `Attribute Conflict Condition.)` / md `Attributes.)` / tesseract `A file may be opened with the`
- p420 L16781: pdf `phrase.` / md `imperative-statement-1.` / tesseract `at end condition. (See page`
- p420 L16795: pdf `which` / md `that` / tesseract `end condition. (See page VII-2,`
- p420 L16795: pdf `applicable to` / md `associated with the` / tesseract `label procedure. b. A reel/unit swap. The`
- p420 L16795: pdf `name-l.` / md `connector referenced by file-name-1.` / tesseract `the file. c. The standard beginning reel/unit label`
- p428 L17056: pdf `b.` / md `(10)` / tesseract `I-O WRITE b. If the`
- p428 L17056: pdf `after executing` / md `control is transferred according to` / tesseract `record-name-l, and after executing the procedure, if any, specified`
- p428 L17056: pdf `procedure, if` / md `rules of the USE statement following the execution of` / tesseract `If the execution of the WRITE statement is unsuccessful, after updating the I-O`
- p428 L17062: pdf `(11)` / md `(12)` / tesseract `of Statements.) (11) The successor`
- p428 L17066: pdf `(13)` / md `(14)` / tesseract `sequential file is established by`
- p428 L17074: pdf `(14)` / md `(15)` / tesseract `is undefined. (14) If the`
- p429 L17108: pdf `(16)` / md `(17)` / tesseract `the line is presented before_`
- p429 L17108: pdf `(17)` / md `(18)` / tesseract `accordance with an implementor-defined technique. If`
- p452 L17801: pdf `records` / md `` / tesseract `be created OUTPUT Normal open;`
- p454 L17862: pdf `VIII-6, The` / md `II-1,` / tesseract `(See page VIII-6, The File Attribute`
- p454 L17862: pdf `Attribute Conflict Condition.)` / md `Attributes.)` / tesseract `a file attribute conflict condition occurs, the`
- p458 L18023: pdf `phrase.` / md `imperative-statement-1.` / tesseract `at end condition. (See page`
- p459 L18043: pdf `applicable to` / md `associated with the file connector referenced by` / tesseract `EXCEPTION procedure applicable to file-name-l. (See page VIII-35, The USE Statement.) c.`
- p474 L18510: pdf `without` / md `when` / tesseract `Invalid Key Condition With Unsuccessful`
- p496 L19228: pdf `Attribute Conflict Condition.)` / md `Attributes.)` / tesseract `a file attribute conflict condition occurs, the`
- p500 L19397: pdf `phrase.` / md `imperative-statement-1.` / tesseract `at end condition. (See page`
- p501 L19417: pdf `which` / md `that` / tesseract `end condition occurs, execution of`
- p572 L21755: pdf `execution.` / md `executed. If the file referenced by file-name-3 is described with variable length records, the size of any record writte ...` / tesseract `has been sequenced by the SORT statement. The compiler inserts a return mechanism at the end of the last statement in th ...`
- p572 L21765: pdf `STANDARD EXCEPTION/ERROR` / md `EXCEPTION` / tesseract `it after the file referenced by`
- p572 L21765: pdf `specified for` / md `associated with` / tesseract `output ,procedure is specified, control passes`
- p578 L21912: pdf `pseudo-text-delimiter.` / md `the matching closing pseudo-text delimiter.` / tesseract `but before the matching closing pseudo-text-delimiter. (8) The syntactic`
- p605 L22850: pdf `ZERO INDICATE` / md `` / tesseract `SIGN LINE ZERO INDICATE M M`
- p610 L23010: pdf `Print Next LINE-` / md `Limit First` / tesseract `Upper Lower Print Next LINE- Print LINE-`
- p610 L23011: pdf `clause Limit Limit` / md `Position Next Group Final LINE-COUNTER Setting First Print` / tesseract `Final LINE NUMBER NEXT GROUP Upper Lower Print Next LINE- Print LINE-`
- p610 L23014: pdf `Group COUNTER Line COUNTER` / md `` / tesseract `Final LINE NUMBER NEXT GROUP Upper Lower Print`
- p610 L23019: pdf `Position Setting` / md `A R` / tesseract `Position Setting Position Setting AR Absolute`
- p613 L23176: pdf `Print Next LINE-` / md `Test First` / tesseract `Lower Fit Print Next LINE- Print LINE-`
- p613 L23176: pdf `clause Limit Limit Test` / md `Position Next Group Final LINE-COUNTER Setting First Print` / tesseract `Final LINE NUMBER NEXT GROUP Upper Lower Fit Print Next LINE- Print`
- p613 L23176: pdf `Group COUNTER Line COUNTER` / md `` / tesseract `Final LINE NUMBER NEXT GROUP Upper Lower Fit`
- p613 L23177: pdf `Position Setting` / md `A R` / tesseract `Position Setting Position Setting AR Absolute 1`
- p617 L23295: pdf `Next LINE-COUNTER clause Limit Limit` / md `` / tesseract `First Print Next LINE-COUNTER clauses* clause Limit Limit Line Position`
- p620 L23398: pdf `Print Next LINE-` / md `Limit First` / tesseract `Upper Lower Print Next LINE- Print LINE-`
- p620 L23398: pdf `Group COUNTER Line COUNTER` / md `` / tesseract `Next LINE- Print LINE- clause Limit Limit Line`
- p620 L23399: pdf `Position Setting` / md `A R` / tesseract `Position Setting Position Setting AR la 2 3a`
- p649 L24439: pdf `VII-5, The` / md `II-1,` / tesseract `(See page VII-5, The File Attribute`
- p649 L24439: pdf `Attribute Conflict Condition.)` / md `Attributes.)` / tesseract `report file, each subsequent OPEN statement execution`
- p686 L25614: pdf `by` / md `` / tesseract `referenced by identifier-2 indicates that the`
- p725 L26947: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD LANGUAGE`
- p726 L27006: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD User-defined`
- p727 L27067: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD Uniqueness`
- p728 L27126: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD IDENTIFICATION`
- p729 L27149: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD ENVIRONMENT`
- p731 L27257: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD File`
- p732 L27285: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD I-O-CONTROL`
- p733 L27306: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD DATA`
- p734 L27365: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD RECORD`
- p734 L27389: pdf `2` / md `Z` / tesseract `1 RPW 1 RPW REPORT clauSe cecceccccvcusccvccccace acme wena`
- p734 L27392: pdf `2` / md `Z Integer-1 CHARACTERS` / tesseract `1 SRT 1 SRT SD level indicator ..s.eeseseee swe`
- p735 L27416: pdf `STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD`
- p736 L27473: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD The`
- p739 L27626: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD sTD Conditional`
- p740 L27728: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD CALL`
- p741 L27786: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD DELETE`
- p742 L27846: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD EXIT`
- p743 L27905: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD MOVE`
- p744 L27966: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD PERFORM`
- p745 L28012: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD INVALID`
- p746 L28069: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD SORT`
- p747 L28112: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD STRING`
- p748 L28177: pdf `SEQ` / md `RPW N` / tesseract `cccecvcvccvecceccccccescssssvessecessssececsesevseese 1 SEQ 1 SEQ 1 REL 1 REL 1 INK 1 INX ON OUTPUT`
- p749 L28207: pdf `ELEMENT STD` / md `` / tesseract `2ND 3RD ELEMENT STD STD SEGMENTATION`
- p784 L29434: pdf `1.` / md `l.` / tesseract `this condition. The new I-O`
- p861 L31681: pdf `11` / md `I1` / tesseract `Al... or Depends Value of`
- p861 L31688: pdf `N2` / md `` / tesseract `of future N2... period-end amounts,`
- p861 L31690: pdf `11` / md `` / tesseract `number RANGE 11 or Depends`

**Class checks, not built (no precise, ungated-noise-free rule):**
- (a) Summary-table indentation: the text layer's x0 for a level-0 row ranges 57 to 80 across the table pages (scan skew and
  margin), and one level is about 6 pt with a jitter of 3 pt, so neither an absolute nor a per-page relative level is
  reliable; a lint on it would fire at random. Probe: rows matched by their first two words, x0 counted per `&nbsp;` depth.
- (b) Brace/bracket continuation column: no text-layer words for the braces and brackets (the layer is OCR of the glyphs, and
  the figures' columns are re-drawn in `<pre>`), so the alternatives' column has no measured source.
- (c) A boxed figure that ends its page: the class is named only in the fifth-pass note, which this tree does not hold, so
  the precise defect (a box left open at the page end, or a closing row on the next page) is not defined enough to gate.

They stay reported here, not gated.
