# Spec reconciliation ledger, 2002 edition (generated)

> Merged from the per-agent files in `2002/` by `scripts/spec/merge_reconciliation.py --edition 2002 --write`.
> Each agent writes its own file before returning, so an interrupted run loses nothing.

**882 pages swept · 0 unread · 0 unswept · 83 confirmed · 40 refuted · 0 unverified**

## Confirmed — repair these

### p83-1 · p83 · misplaced-content · **normative**

- **PDF:** Basic special character row: the Character column's first line is blank (the space character) opposite the Meaning 'space', then '+' opposite 'plus sign', '–' opposite 'minus sign (hyphen)', ... ':' opposite 'colon', '_' opposite 'underscore' (19 meanings, 19 character lines including the blank one for space).
- **Markdown:** | Basic special character | +<br>–<br>\*<br>/<br>=<br>$<br>,<br>;<br>.<br>"<br>'<br>(<br>)<br>><br>&lt;<br>&<br>:<br>_ | space<br>plus sign<br>minus sign (hyphen)<br>asterisk<br>...<br>colon<br>underscore |
- **Why:** The Character cell lost the leading blank line that stands for the space character, so it holds 18 lines against 19 meanings. Read line by line, every character is paired with the wrong meaning ('+' with 'space', '–' with 'plus sign', ..., '_' with 'colon') and 'underscore' has no character. The table defines the basic special characters of the COBOL character repertoire, so the pairing is normative.
- **Repair:** —
- **Verifier:** The rendered page 83 (printed p.61), Table 2, Basic special character row, shows the Character column's first line blank opposite 'space', with '+' opposite 'plus sign' on the next line and so on down to '_' opposite 'underscore': 19 lines in each column. In page-83.md line 9 the Character cell has 18 <br>-separated entries starting with '+', and the Meaning cell has 19 entries starting with 'space'. Paired by position, every character lines up with the meaning one row above its own, and 'underscore' has no character. The missing blank line is the space character's entry, which is table content and not layout, so the claim is accurate as stated.

### p144-1 · p144 · ocr-error · **normative**

- **PDF:** 10^-999 (1.000 000 000 000 000 000 000 000 000 000 0E-999) ... 10^1000 – 10^968 (9.999 999 999 999 999 999 999 999 999 999 9E+999) [exponents printed as superscripts]
- **Markdown:** 10–999 (1.000 000 000 000 000 000 000 000 000 000 0E-999)

through

   101000 – 10968 (9.999 999 999 999 999 999 999 999 999 999 9E+999)
- **Why:** Superscript exponents flattened in 8.8.1.3.1.1: '10–999' reads as 10 minus 999, '101000 – 10968' reads as one hundred one thousand minus ten thousand nine hundred sixty-eight; the bounds of the standard intermediate data item magnitude range are wrong as written (the parenthesised E-notation is intact).
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 144 (printed page 122), 8.8.1.3.1.1 Precision and allowable magnitude, shows the lower bound as 10 with superscript '-999' and the upper bound as 10 with superscript '1000', an en dash, then 10 with superscript '968'. The transcription (page-144.md lines 15 and 19) reads '10–999' and '101000 – 10968', so the superscript markup is gone and the run-together digits read as different numbers ('10 minus 999', '101000 minus 10968'). Both quotes in the claim match their sources. The parenthesised E-notation equivalents survive and keep the intended values recoverable, but the primary normative expression of the range bounds of a standard intermediate data item is wrong as written. This is a genuine content defect, not scaffolding or wrapping.

### p234-1 · p234 · incorrect-text · **normative**

- **PDF:** SELECT [ OPTIONAL ] file-name-1 / ASSIGN { TO { device-name-1 | literal-1 } ... [ USING data-name-1 ] | USING data-name-1 } / [ [ ORGANIZATION IS ] SEQUENTIAL ] .
- **Markdown:**                   ╭               ╮ …
<u>SELECT</u> [ <u>OPTIONAL</u> │ ] file-name-1 │
                  │               │
       ╭          │ device-name-1 │                       ╮
       │ TO       ┤               ├ [ <u>USING</u> data-name-1 ] │
<u>ASSIGN</u> ┤          ╰ literal-1     ╯                       ├
- **Why:** The Format 4 diagram is scrambled: the brace around device-name-1/literal-1 is drawn spanning up into the SELECT line (cutting through '[ OPTIONAL ] file-name-1'), and the ellipsis that the PDF places after that brace (repeating TO's operand) is moved to the top of the brace above the SELECT line. Read literally, the markdown no longer says that device-name-1/literal-1 may be repeated after TO, and it splices the SELECT line into the TO choice. Formats 1-3 on pages 232-234 render the same ASSIGN clause correctly and can serve as the model.
- **Repair:** —
- **Verifier:** Rendered page 234 (printed 212) at 300 dpi. The printed Format 4 shows a clean 'SELECT [ OPTIONAL ] file-name-1' line, then ASSIGN with an outer brace over two alternatives: 'TO { device-name-1 / literal-1 } ... [ USING data-name-1 ]' and 'USING data-name-1', then '[ [ ORGANIZATION IS ] SEQUENTIAL ] .'. The ellipsis sits immediately right of the inner brace, on the TO line. The markdown page-234.md lines 46-51 draw the inner brace's top (╭ ╮ with the … ) above the SELECT line, run its │ bars through the SELECT line (splitting '[ OPTIONAL │ ] file-name-1 │'), and leave no ellipsis after the brace on the TO line. Read literally, the transcription drops the repetition of TO's operand and corrupts the SELECT line. Format 3 directly above on the same page renders the same ASSIGN correctly. A genuine normative notation defect, described accurately.

### p259-1 · p259 · incorrect-figure · **normative**

- **PDF:** RECORD { CONTAINS integer-1 CHARACTERS / IS VARYING IN SIZE [ [ FROM integer-2 ] [ TO integer-3 ] CHARACTERS ] (continuation line, indented:) [ DEPENDING ON data-name-1 ] / CONTAINS integer-4 TO integer-5 CHARACTERS }
- **Markdown:** │        │ IS <u>VARYING</u> IN SIZE [ [ FROM integer-2 ] [ <u>TO</u> integer-3 ] CHARACTERS ] │ │
│ <u>RECORD</u> ┤                                                                       ├ │ .
│        │ [ <u>DEPENDING</u> ON data-name-1 ]                                          │ │
- **Why:** In the SD general format (13.3.5.1), the printed [ DEPENDING ON data-name-1 ] is an indented continuation of the IS VARYING alternative. The markdown places it flush with the alternatives, separated by blank lines like the other alternatives, so the brace reads as four alternatives and RECORD [ DEPENDING ON data-name-1 ] alone looks legal (falsely permissive syntax).
- **Repair:** —
- **Verifier:** Printed page 237 (PDF p259), 13.3.5.1: inside the RECORD brace (no choice-indicator bars) the four stacked lines are set tight. 'CONTAINS integer-1', 'IS VARYING ...', and 'CONTAINS integer-4 TO integer-5' start at one left column, and '[ DEPENDING ON data-name-1 ]' is visibly indented about 4 characters further right, which marks it as a continuation of the IS VARYING alternative. So the brace holds three alternatives. In page-259.md lines 12-18, '[ DEPENDING ON data-name-1 ]' starts at the same column as the other alternatives. Each alternative sits in its own row, with an empty spacer row between rows (rows 13 and 17 are blank, and row 15 holds only the RECORD label). The brace therefore reads as four alternatives and allows RECORD DEPENDING ON data-name-1 on its own, or without IS VARYING. The DEPENDING text is present, so nothing is missing; the defect is the lost indentation, which changes the syntax the figure describes.

### p267-1 · p267 · incorrect-figure · **normative**

- **PDF:** { 1 / 01 } constant-name-1 CONSTANT [ IS GLOBAL ] ... (both level numbers 1 and 01 printed underlined)
- **Markdown:** ╭ 1  ╮
┤    ├ constant-name-1 <u>CONSTANT</u> [ IS <u>GLOBAL</u>] ┤
╰ <u>01</u> ╯
- **Why:** In the 13.9.1 constant-entry general format the printed level number '1' carries an underline just like '01'; the markdown underlines only 01, so the figure's notation for the required level-number alternative is lost for '1'.
- **Repair:** —
- **Verifier:** A 6x zoom crop of the 300 dpi render of PDF page 267 (printed page 245, §2002:13.9.1 General format) shows a short horizontal rule printed directly under the '1' in the top alternative of the level-number brace, and a longer rule under '01' in the bottom alternative. Both are underlined. The transcription (page-267.md line 13) writes the top alternative as a plain '1' and only line 15 has '<u>01</u>'. The quotes of both sources are accurate. In this notation an underline marks a required word or literal that must be written, so the transcription drops that mark from one of the two brace alternatives. The rest of the figure (CONSTANT, GLOBAL, the AS/FROM braces) matches the print.

### p328-1 · p328 · incorrect-text · **normative**

- **PDF:** OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1
[ { ASCENDING / DESCENDING } KEY IS { data-name-2 } ... ] ... [ INDEXED BY { index-name-1 } ... ]
- **Markdown:**        ╭              ╮ KEY
       │              │
<u>OCCURS</u> │ integer-1 <u>TO</u> │ integer-2 TIMES <u>DEPENDING</u> ON data-name-1
       │              │
┌      │ <u>ASCENDING</u>    │                      ┐
│      ┤              ├ IS { data-name-2 } … │ … [ <u>INDEXED</u> BY { index-name-1 } … ]
└      ╰ <u>DESCENDING</u>   ╯                      ┘
- **Why:** A grammar or reader working from the markdown would see 'integer-1 TO' as an alternative to ASCENDING/DESCENDING and a KEY-less 'IS { data-name-2 }' phrase, so it could reject the legal source 'OCCURS 1 TO 10 TIMES DEPENDING ON N ASCENDING KEY IS K' or accept malformed variants.
- **Repair:** —
- **Verifier:** Printed page 306 (PDF p328), Format 2: line 1 is the plain 'OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1' with no brace. Line 2 is the same as Format 1: a bracket that holds a brace stacking ASCENDING over DESCENDING, then 'KEY IS { data-name-2 } ...', the bracket closing, then '... [ INDEXED BY { index-name-1 } ... ]'. No choice-indicator bars are printed. Markdown lines 22-28 draw one tall brace that spans 'integer-1 TO' and ASCENDING/DESCENDING, and they put KEY at the top right of that brace, apart from 'IS { data-name-2 }'. Read literally, this makes 'integer-1 TO' an alternative inside the ASCENDING/DESCENDING brace and moves KEY out of the KEY IS phrase. That changes the syntax the diagram expresses, so it is a normative content defect, not wrapping or layout. Format 1 in the same markdown is rendered correctly, which shows the Format 2 version is a transcription error. The claim quotes both sources accurately.

### p438-1 · p438 · ocr-error · **normative**

- **PDF:** (operand-1₁ + operand-1₂ + ... + operand-1ₙ)
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n)
- **Why:** ADD GR 1 (standard arithmetic): the subscripts 1, 2 and n on operand-1 are flattened into the name, so the expression reads as operands named operand-11, operand-12 and operand-1n instead of the 1st, 2nd and nth instances of operand-1.
- **Repair:** —
- **Verifier:** Accurate for GR1. The same subscript flattening also occurs in GR2 on this page: the PDF prints '(operand-1_1 + operand-1_2 + ... + operand-1_n + operand-2)' and the markdown has '(operand-11 + operand-12 + ... + operand-1n + operand-2)'.

### p438-2 · p438 · ocr-error · **normative**

- **PDF:** (operand-1₁ + operand-1₂ + ... + operand-1ₙ + operand-2)
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n + operand-2)
- **Why:** ADD GR 2 (format 2, standard arithmetic): the same lost-subscript error turns the indexed operand-1 instances into names such as operand-11 and operand-12, which could be mistaken for distinct operands.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 438 (printed page 416, 14.8.2.3 General rules, GR 2) shows the expression '(operand-1 + operand-1 + ... + operand-1 + operand-2)' with clearly lowered subscripts 1, 2 and n on each operand-1. page-438.md line 27 reads '(operand-11 + operand-12 + ... + operand-1n + operand-2)': the subscripts are flattened into the base name, so the text literally names operand-11, operand-12 and operand-1n, which are not the indexed instances of operand-1. This is a content loss in a normative general rule, not wrapping or furniture. The claim quotes both sources accurately. The following prose ('the values of operand-1 are the values of literal-1 and the data items referenced by identifier-1 in the order...') lets a careful reader recover the intent, so the practical impact is low, but the transcribed formula is wrong as written.

### p503-1 · p503 · incorrect-text · **normative**

- **PDF:** HIGH-VALUE, HIGH-VALUES; LOW-VALUE, LOW-VALUES; and QUOTE, QUOTES | Alphabetic / Alphanumeric / Alphanumeric-edited / National / National-edited / Numeric / Numeric-edited / — if usage is display / — if usage is national | Alphanumeric / Alphanumeric / Alphanumeric / National / National / Alphanumeric / (blank) / Alphanumeric / National
- **Markdown:** | HIGH-VALUE, HIGH-VALUES; LOW-VALUE, LOW-VALUES; and QUOTE, QUOTES | Alphabetic<br>Alphanumeric<br>Alphanumeric-edited<br>National<br>National-edited<br>Numeric<br>Numeric-edited<br>— if usage is display — if usage is national | Alphanumeric<br>Alphanumeric<br>Alphanumeric<br>National<br>National<br>Alphanumeric<br>Alphanumeric<br>National |
- **Why:** Table 18 prints Numeric-edited with no category of its own and splits it into '— if usage is display' -> Alphanumeric and '— if usage is national' -> National. The markdown merges the two sub-rows into one line and drops the blank, so the lines pair positionally as Numeric-edited -> Alphanumeric and the merged usage line -> National: the category of HIGH-VALUE/LOW-VALUE/QUOTE moved to a numeric-edited item of usage national reads as Alphanumeric.
- **Repair:** —
- **Verifier:** The 300-dpi render of PDF page 503 (printed page 481), Table 18, HIGH-VALUE/LOW-VALUE/QUOTE row: the receiving-operand column prints nine lines (Alphabetic, Alphanumeric, Alphanumeric-edited, National, National-edited, Numeric, Numeric-edited, '— if usage is display', '— if usage is national'). The figurative-constant column prints Alphanumeric x3, National x2, Alphanumeric, then a BLANK line opposite Numeric-edited, then Alphanumeric (display) and National (national). Markdown line 9 of page-503.md has 8 <br>-separated entries per column: the two usage sub-rows are merged into one entry and the blank is dropped, so positional pairing gives Numeric-edited -> Alphanumeric and the merged usage entry -> National. The national-usage numeric-edited case loses its distinct category (National), which is a normative MOVE semantics change. The claim quotes both sources accurately.

### p562-1 · p562 · ocr-error · **normative**

- **PDF:** (operand-1₁ + operand-1₂ + ... + operand-1ₙ)
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n)
- **Why:** 14.8.40.3 SUBTRACT general rule 1 (format 1): the printed subscripts 1, 2 and n on operand-1 (the first, second and n-th occurrence of operand-1) were flattened onto the baseline, so the equivalent arithmetic expression for the initial evaluation reads as references to nonexistent operands named operand-11, operand-12 and operand-1n.
- **Repair:** —
- **Verifier:** Confirmed as described for 14.8.40.3 GR1. The same subscript flattening also occurs in GR2's expression on the same page: '(operand-2 – (operand-1₁ + operand-1₂ + ... + operand-1ₙ))' is transcribed as '(operand-2 – (operand-11 + operand-12 + ... + operand-1n))'.

### p562-2 · p562 · ocr-error · **normative**

- **PDF:** (operand-2 – (operand-1₁ + operand-1₂ + ... + operand-1ₙ))
- **Markdown:** (operand-2 – (operand-11 + operand-12 + ... + operand-1n))
- **Why:** 14.8.40.3 SUBTRACT general rule 2 (format 2): the same flattened subscripts on operand-1 make the expression defining the GIVING result reference operand-11, operand-12 and operand-1n instead of the indexed occurrences of operand-1.
- **Repair:** —
- **Verifier:** The rendered PDF page 562 (printed page 540), §2002:14.8.40.3 GR2, shows the expression (operand-2 – (operand-1 + operand-1 + ... + operand-1)) with the 1, 2 and n set as true subscripts on each operand-1. Line 37 of page-562.md reads '(operand-2 – (operand-11 + operand-12 + ... + operand-1n))', so the subscripts are flattened into the operand name. That turns the indexed occurrences of operand-1 into what read as distinct operands named operand-11, operand-12 and operand-1n, which changes the meaning of the normative rule's defining expression. The claim quotes both sources accurately. The same flattening also occurs in GR1 (line 25, the operand-1 sum), which is presumably a sibling claim.

### p566-1 · p566 · misplaced-content · **normative**

- **PDF:** UNLOCK file-name-1 [ RECORD | RECORDS ]  (one line: UNLOCK, then file-name-1, then a bracket stacking RECORD over RECORDS)
- **Markdown:** <u>UNLOCK</u>      ┌ RECORD  ┐
            │         │
file-name-1 │         │
            └ RECORDS ┘
- **Why:** A reader or grammar author working from the transcription could take file-name-1 for an alternative stacked with UNLOCK, or misorder it relative to the optional RECORD/RECORDS bracket, and so derive the wrong UNLOCK syntax.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 566 (printed page 544), §2002:14.8.43.1, shows UNLOCK (underlined) and file-name-1 on one baseline, followed by a plain bracket with no choice bars that stacks RECORD over RECORDS. The bracket means at most one of them, and it is optional. page-566.md lines 10-13 put UNLOCK on row 1 and file-name-1 on row 3, both starting at column 0, so the two are vertically stacked to the left of the bracket. In this notation, vertically stacked items read as alternatives, so the transcription suggests UNLOCK and file-name-1 are alternatives, which changes the syntax. The bracket itself is transcribed correctly: plain, with RECORD/RECORDS. The claim quotes both sources accurately.

### p639-1 · p639 · ocr-error · **normative**

- **PDF:** ((argument-1₁ + argument-1₂) / 2) ... ((argument-1₁ + argument-1₂ +... + argument-1ₙ) / n)
- **Markdown:** ((argument-11 + argument-12) / 2)
...
((argument-11 + argument-12 +... + argument-1n) / n)
- **Why:** Subscripts 1, 2, n on argument-1 lost in 15.48.3 rule 1 b) and c); 'argument-11' reads as a different argument name (argument eleven) instead of the first occurrence of argument-1.
- **Repair:** —
- **Verifier:** In §2002:15.48.3 rule 1 b) and c), the transcription drops the subscript markup on the occurrence indices of argument-1. It renders argument-1₁, argument-1₂ and argument-1ₙ as 'argument-11', 'argument-12' and 'argument-1n'. The lead-in text makes the intended meaning clear, so the severity should be editorial, not normative.

### p653-1 · p653 · ocr-error · **normative**

- **PDF:** (argument-2₁ / (1 + argument-1) +  argument-2₂ / (1 + argument-1) ** 2) ... (argument-2₁ / (1 + argument-1) ** 1) ... (argument-2ₙ / (1 + argument-1) ** n))) where argument-1 and argument-2ᵢ in the terms of the SUM function
- **Markdown:** (argument-21 / (1 + argument-1) + argument-22 / (1 + argument-1) \*\* 2) ... (argument-21 / (1 + argument-1) \*\* 1) ... (argument-2n / (1 + argument-1) \*\* n))) ... where argument-1 and argument-2i in the terms of the SUM function
- **Why:** 15.61.3 PRESENT-VALUE returned-value rule 1 b) and c): the printed subscripts 1, 2, n and i on argument-2 (denoting the first, second, n-th and i-th occurrence of argument-2) were flattened onto the baseline, so the equivalent arithmetic expressions read as references to nonexistent operands named argument-21, argument-22, argument-2n and argument-2i. The expression defining the function's result loses its occurrence indexing.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 653 (printed page 631, §2002:15.61.3 rule 1 b and c) shows argument-2 followed by clearly lowered, smaller subscript glyphs 1, 2 (rule b), 1 and n (rule c SUM terms) and i (the 'where' sentence). The markdown (page-653.md lines 31, 37, 41, 43) renders these as plain baseline text 'argument-21', 'argument-22', 'argument-2n', 'argument-2i' with no <sub> markup. The claim quotes both sources accurately. The loss is not scaffolding or wrapping: the transcription now names operands 'argument-21'/'argument-22' rather than the first/second occurrence of argument-2, which changes how the defining expression of the returned value reads in normative text. A reader can infer the intent from context, but the occurrence indexing is genuinely lost.

### p663-1 · p663 · ocr-error · **normative**

- **PDF:** (argument-1₁ + argument-1₂) ... (argument-1₁ + argument-1₂ + ... + argument-1ₙ)  [subscripts 1, 2, n printed below the baseline]
- **Markdown:** (argument-11 + argument-12)

   c) For n occurrences of argument-1,

   (argument-11 + argument-12 + ... + argument-1n)
- **Why:** The returned-value rule defines the function's result by an equivalent expression; with the subscripts flattened, a reader sees operands named argument-11/argument-12/argument-1n rather than the successive occurrences of argument-1, which misstates the normative definition.
- **Repair:** —
- **Verifier:** Rendered page 663 (printed 641), §2002:15.71.3 rule 1 b) and c): the PDF prints '(argument-1 + argument-1)' and '(argument-1 + argument-1 + ... + argument-1)' with clearly lowered, smaller subscript glyphs 1, 2 and n after each 'argument-1'. page-663.md lines 34 and 38 read 'argument-11 + argument-12' and 'argument-11 + argument-12 + ... + argument-1n' on the baseline, with no <sub> markup. The text is present, but the subscript markers that make these the 1st, 2nd and nth occurrences of argument-1 are lost, so the markdown names operands argument-11, argument-12 and argument-1n that do not exist in the format (the general format names only argument-1). That changes how the defining equivalent expression reads, so it is a real transcription defect. The claim quotes both sources accurately.

### p671-1 · p671 · ocr-error · **normative**

- **PDF:** ( ((argument-1₁ – FUNCTION MEAN (argument-list)) ** 2 + (argument-1₂– FUNCTION MEAN (argument-list)) ** 2) / 2 ) ... ((argument-1₁ – FUNCTION MEAN (argument-list)) ** 2) ... ((argument-1ₙ – FUNCTION MEAN (argument-list)) ** 2)) / n) ... argument-1ᵢ is the ith argument  [subscripts 1, 2, n, i printed below the baseline]
- **Markdown:** ( ((argument-11 – FUNCTION MEAN (argument-list)) \*\* 2 +\
   &emsp;&emsp;(argument-12– FUNCTION MEAN (argument-list)) \*\* 2) / 2 ) ... ((argument-11 – FUNCTION MEAN (argument-list)) \*\* 2) ... ((argument-1n – FUNCTION MEAN (argument-list)) \*\* 2)) / n) ... argument-1i is the ith argument
- **Why:** The returned-value rule defines the function's result by an equivalent expression; with the subscripts flattened, a reader sees operands named argument-11/argument-12/argument-1n rather than the successive occurrences of argument-1, which misstates the normative definition.
- **Repair:** —
- **Verifier:** Rendered p671 (printed 649) at 300 dpi: in 15.79.3 1) b) and c) and the closing 'where' sentence, the occurrence indices 1, 2, 1, n and i on argument-1 are printed as true subscripts below the baseline. The markdown (page-671.md lines 29, 30, 36, 40, 42) has them on the baseline with no <sub> markup, yielding 'argument-11', 'argument-12', 'argument-1n', 'argument-1i'. 'argument-11' reads as a distinct operand name rather than the first occurrence of argument-1, so the equivalent expression that defines the returned value is misstated. Quotes of both sources are accurate. The indentation remark is formatting only and not part of the content discrepancy.

### p677-1 · p677 · incorrect-figure · **normative**

- **PDF:** COMMUNICATION SECTION . [ communication-description-entry [ constant-entry / record-description-entry ] ... ] ...   (COMMUNICATION and SECTION underlined; an outer bracket enclosing communication-description-entry and an inner bracket around the stacked constant-entry / record-description-entry alternatives; one ellipsis after the inner bracket, a second after the outer bracket)
- **Markdown:** COMMUNICATION SECTION .

   communication-description-entry constant-entry . . .\
   &emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;record-description-entry
- **Why:** The A.1.1 communication section general format lost both brackets, one of the two ellipses, and the underlining of the required keywords COMMUNICATION SECTION. As transcribed it reads as one mandatory communication-description-entry followed by a repeatable constant-entry, with record-description-entry dangling. The standard makes the whole CD group optional and repeatable, and makes the constant-entry/record-description-entry alternatives optional, one per repetition, and repeatable. Read literally, the transcription rejects an empty communication section and several CD entries in one section, both of which are legal.
- **Repair:** —
- **Verifier:** The rendered page (printed p.655, PDF p.677, A.1.1) shows COMMUNICATION and SECTION underlined, then an outer square bracket enclosing 'communication-description-entry' followed by an inner square bracket (no choice-indicator bars) around the stacked alternatives 'constant-entry' / 'record-description-entry'; an ellipsis follows the inner bracket and a second ellipsis follows the outer bracket. The markdown has no brackets, only one ellipsis (placed after constant-entry), no underlining, and record-description-entry on a separate line. Per §2002:5.1.5.1 the inner bracket means at most one of the two alternatives per repetition, repeated by its ellipsis; the outer bracket plus ellipsis makes the CD group optional and repeatable. The transcription loses that structure, so the claim is accurate and normative.

### p40-1 · p40 · misplaced-content · **structural**

- **PDF:** Expression | 8.8, Expressions | arithmetic-expression-n boolean-expression-n conditional-expression-n  ...  c) Exception name | 14.5.12.1.5, Exception-names and exception conditions | exception-name-1
- **Markdown:** | Expression | 8.8, Expressions | arithmetic-expression-n |

boolean-expression-n conditional-expression-n

| Integer | 5.4, Integer operands | integer-n |
|---|---|---|
...
| c) Exception name | 14.5.12.1.5, Exception-names and | exception-name-1 |

exception conditions
- **Why:** The operand-type table is split into two tables: the Expression row's terms boolean-expression-n and conditional-expression-n fall out of the table as a stray paragraph, and the tail of the 'Described in' cell for Exception name ('exception conditions') becomes a detached paragraph; the multi-line User-defined word cell is also split across three rows ('a) User-defined word, including' / 'qualification and subscription if' / 'needed'). A reader of the table sees Expression mapping only to arithmetic-expression-n.
- **Repair:** —
- **Verifier:** Rendered PDF page 40 (printed page 18) shows one three-column table. The Expression row's Term cell holds three stacked terms: arithmetic-expression-n, boolean-expression-n, conditional-expression-n. The Exception name row's 'Described in' cell reads '14.5.12.1.5, Exception-names and exception conditions'. Row a) is ONE row: its operand-type cell 'User-defined word, including qualification and subscription if needed', its Described-in cell lists 8.3.1.1.1, 8.4.1.1 and 8.4.1.2, and its Term cell reads 'Any of the types listed in 8.3.1.1.1 suffixed by -n'. page-40.md matches the claim exactly. The Expression row ends after arithmetic-expression-n, and 'boolean-expression-n conditional-expression-n' follows as a bare paragraph outside the table. The table then restarts with Integer as a new header row. Row a) is split into three rows, so 'suffixed by -n' and 8.4.1.1/8.4.1.2 appear to belong to separate operand types. 'exception conditions' is a detached paragraph. No words are lost, but the table no longer pairs the Expression operand type with two of its three terms, and row a)'s cells are attached to the wrong rows. That changes what the table says. The claim quotes both sources correctly.

### p43-1 · p43 · misplaced-content · **structural**

- **PDF:** shall | is required to / is, is to ; shall not | is not permitted / is not allowed ; may | is permitted / is allowed ; need not | is not required to ; should | it is recommended that / is recommended ; should not | (none) ; can | is able to / it is possible to / is possible ; cannot | (none)
- **Markdown:** | ... | shall<br>shall not | is required to<br>is, is to<br>is not permitted<br>is not allowed |
| a permission expressed by the standard | may<br>need not | is permitted<br>is allowed<br>is not required to |
| ... | should<br>should not | it is recommended that<br>is recommended |
| ... | can<br>cannot | is able to<br>it is possible to<br>is possible |
- **Why:** Table 1 aligns each alternative expression with its verbal form (e.g. 'is not permitted' is the alternative for 'shall not', 'is not required to' for 'need not', and 'should not' / 'cannot' have none). The markdown collapses each row's forms and alternatives into undivided lists, so which alternative expression is equivalent to which verbal form (positive vs negative) is lost.
- **Repair:** —
- **Verifier:** Table 1 (§2002:5.7) is accurate as described: the markdown merges each row's positive and negative verbal forms, and their alternative expressions, into undivided <br> lists, which loses the PDF's line alignment (shall: is required to / is, is to; shall not: is not permitted / is not allowed; may: is permitted / is allowed; need not: is not required to; should: it is recommended that / is recommended; should not: none; can: is able to / it is possible to / is possible; cannot: none). The full-width table note is also misplaced into the Provision column. This is informative drafting-convention text with no effect on COBOL syntax or semantics.

### p45-1 · p45 · misplaced-content · **structural**

- **PDF:** "-  '-   literal continuation indicator   continuation of a literal when specified in an un-terminated literal with the same quotation symbol in its opening delimiter, subject to additional rules in 6.1.2.1, Syntax Rules.
- **Markdown:** | "- | literal continuation | continuation of a literal when specified in an un-terminated |
|---|---|---|
| '- | indicator | literal with the same quotation symbol in its opening delimiter, |

subject to additional rules in 6.1.2.1, Syntax Rules.
- **Why:** The floating-indicator table is broken up: '- is presented as a separate row whose indicator name is 'indicator' and whose meaning is a sentence fragment, instead of both "- and '- being literal continuation indicators with one shared description; the *> row's two numbered meanings and the >> and >>D rows also fall out of the table as loose paragraphs, losing the column association.
- **Repair:** —
- **Verifier:** Structural only, no text lost. The 6.1.2 floating-indicator table is transcribed by visual line, not by entry. The "- / '- entry is split into two rows, so '- appears with the name 'indicator' and a description fragment, and the description's last line drops out of the table. The *> entry's second line and item 2) fall out of the table as paragraphs, and the >> and >>D entries are untabled paragraphs. The fix is one table with five entries: *> (both numbered meanings), >>, "- and '- (one entry, 'literal continuation indicator'), and >>D.

### p46-1 · p46 · incorrect-text · **structural**

- **PDF:** Figure 1: Margin L | positions 1 2 3 4 5 6 | Margin C | 7 | Margin A | 8 9 10 11 12 13 ... | Margin R; brace under 1-6 labelled 'Sequence Number Area'; arrow at 7 labelled 'Indicator Area'; brace under 8-... labelled 'Program-text Area'
- **Markdown:** | Margin |  |  |  |  | Margin |  | Margin |  | Margin |
|---|---|---|---|---|---|---|---|---|---|
| L |  |  |  |  | C |  | A |  | R |
| 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 10 11 12 13 | ... |

Program-text Area Sequence Number Area

Indicator Area
- **Why:** The figure's table places Margin C over position 6 (it is between 6 and 7), Margin A over position 8 (it is between 7 and 8) and Margin R over '...'; the area labels are listed in the wrong order ('Program-text Area Sequence Number Area') with no mapping to positions. The figure as transcribed misstates the fixed-form layout; only the prose below it is correct.
- **Repair:** —
- **Verifier:** Accurate as described. Figure 1 (§2002:6.2) is mistranscribed as a table that places Margin C over position 6, Margin A over position 8 and Margin R over '...' instead of on the boundaries 6|7, 7|8 and after the last position, and lists the area labels in reversed order without mapping them to positions 1-6, 7 and 8+. The prose following the figure is correct and restates every position, so the defect is limited to the figure's depiction (no normative rule is lost).

### p51-1 · p51 · table-restructured · **structural**

- **PDF:** Compiler directing statements    Stage

COPY statement    Text manipulation
    SUPPRESS option    Implementor-defined
REPLACE statement    Text manipulation

Compiler directives    Stage

CALL-CONVENTION    Compilation ...
- **Markdown:**    Compiler directing statements Stage

| COPY statement | Text manipulation |
|---|---|
| SUPPRESS option | Implementor-defined |
| REPLACE statement | Text manipulation |
| Compiler directives | Stage |
| CALL-CONVENTION | Compilation |
- **Why:** The printed two-part listing (each part with its own underlined 'Stage' column header) became one markdown table whose header row is 'COPY statement | Text manipulation': the first column header sits outside the table as a run-on line, the second header 'Compiler directives | Stage' became a data row, and the indentation that makes 'SUPPRESS option' a sub-entry of the COPY statement is lost, so SUPPRESS reads as a peer statement. The stage values themselves are all correct.
- **Repair:** —
- **Verifier:** The printed page 51 (§2002:7, page 29) shows two separate listings, each with underlined column headers ('Compiler directing statements / Stage' and 'Compiler directives / Stage'), and 'SUPPRESS option' visibly indented under 'COPY statement'. The markdown has the first header as a loose indented line outside the table, makes 'COPY statement | Text manipulation' the table header row, demotes 'Compiler directives | Stage' to a data row, and drops the SUPPRESS indentation so it reads as a peer compiler directing statement rather than an option of COPY. All entries and stage values (including the printed 'Text-manipulation' hyphen variants) are transcribed correctly. The loss of the two-part structure and of the SUPPRESS sub-entry relation is a genuine structural discrepancy, as described.

### p126-1 · p126 · missing-heading · **structural**

- **PDF:** 8.4.5.1.1 Scope of condition-names, constant-names, data-names, file-names, record-names, report-names, screen-names, and type-names
- **Markdown:** ###### 8.4.5.1.1 Scope of condition-names, constant-names, data-names, file-names, record-names,

report-names, screen-names, and type-names
- **Why:** The two-line printed heading was split: its second line 'report-names, screen-names, and type-names' became a separate body paragraph, so the heading is truncated and a stray non-normative fragment appears as prose.
- **Repair:** —
- **Verifier:** The 8.4.5.1.1 heading is split, not missing. The markdown heading stops at 'record-names,', and the heading's printed second line, 'report-names, screen-names, and type-names', became a separate body paragraph. The fix is to join it back into the heading line. No normative text is lost or changed.

### p150-1 · p150 · missing-heading · **structural**

- **PDF:** 8.8.4.1.1.4 Comparison of a numeric integer operand with an operand of class alphanumeric or national
- **Markdown:** ###### 8.8.4.1.1.4 Comparison of a numeric integer operand with an operand of class alphanumeric or

national
- **Why:** The two-line printed heading was split: its last word 'national' became a separate body paragraph, so the heading is truncated (reads as comparison with class 'alphanumeric or') and a stray fragment appears as prose.
- **Repair:** —
- **Verifier:** On the 300-dpi render of PDF page 150 (printed page 128), the bold heading '8.8.4.1.1.4 Comparison of a numeric integer operand with an operand of class alphanumeric or' wraps, and 'national' is printed in the same bold heading face on the next line before the body text begins. page-150.md line 31 holds the heading markup '###### 8.8.4.1.1.4 ... alphanumeric or', then a blank line, then 'national' as a plain body paragraph on line 33. This is more than a line wrap. The markdown heading node ends at 'or', and the heading's last word has become a stray prose paragraph, so the heading text is wrong as transcribed. The claim quotes both sources accurately.

### p157-1 · p157 · table-restructured · **structural**

- **PDF:** Logical Operator | Meaning: AND | Logical conjunction; the truth value is true if both of the conjoined conditions are true; false if one or both of the conjoined conditions is false. / OR | Logical inclusive OR; the truth value is true if one or both of the included conditions is true; false if both included conditions are false. / NOT | Logical negation or reversal of truth value; the truth value is true if the condition is false; false if the condition is true.
- **Markdown:** | AND | Logical conjunction; the truth value is true if both of the conjoined conditions are |

true; false if one or both of the conjoined conditions is false.

   OR Logical inclusive OR; the truth value is true if one or both of the included conditions is true; false if both included conditions are false.

   NOT Logical negation or reversal of truth value; the truth value is true if the condition is false; false if the condition is true.
- **Why:** The 8.8.4.2 logical-operator table keeps only the AND row, and that row's Meaning cell is cut mid-sentence; the rest of the AND meaning and the OR and NOT rows fall out of the table as loose paragraphs. All words survive, but a reader of the table sees an AND definition ending at 'are' and no OR/NOT rows.
- **Repair:** —
- **Verifier:** The printed page (2002 p.135, PDF p157) shows the 8.8.4.2 table with header 'Logical Operator | Meaning' and three rows: AND, OR, NOT, each with a two-line Meaning cell. page-157.md lines 17-25 hold a markdown table whose only row is '| AND | Logical conjunction; ... conjoined conditions are |'; the cell ends at 'are', the continuation 'true; false if one or both of the conjoined conditions is false.' is a loose paragraph after a blank line (which ends the table), and the OR and NOT rows are indented loose paragraphs outside the table. The claim quotes both sources accurately. Every word survives, so no normative text is lost, but the table structure is genuinely broken (truncated cell, two rows missing from the table), which is more than wrapping or furniture. Real as a structural defect, correctly labeled structural rather than normative.

### p163-1 · p163 · misplaced-content · **structural**

- **PDF:** EXPANDS | class-specifier and interface-specifier of the REPOSITORY paragraph
- **Markdown:** | EXPANDS | class-specifier and interface-specifier of the REPOSITORY |

paragraph

| FOREGROUND-COLOR | screen description entry |
- **Why:** The wrapped second line of the EXPANDS context cell is detached from the table as a stray paragraph 'paragraph', so the EXPANDS row's context reads 'of the REPOSITORY' with no noun and the table is split in two.
- **Repair:** —
- **Verifier:** Accurate as stated, with one addition: because the table restarts after the stray 'paragraph' line, the '|---|---|' separator follows the FOREGROUND-COLOR row, so FOREGROUND-COLOR is promoted to the header row of a second table rather than being a data row. Fix: put 'paragraph' back into the EXPANDS context cell and join the two tables into one.

### p164-1 · p164 · misplaced-content · **structural**

- **PDF:** ONLY | Object-view, SHARING clause, SHARING phrase, and USAGE clause
- **Markdown:** | ONLY | Object-view, SHARING clause, SHARING phrase, and |

USAGE clause

| PARAGRAPH | EXIT statement |
- **Why:** The ONLY row's context list loses its last item: 'USAGE clause' is detached from the table as a stray paragraph, so the row reads as ending in 'and' and a table-reader would not see that ONLY is context-sensitive in the USAGE clause (USAGE OBJECT REFERENCE ... ONLY).
- **Repair:** —
- **Verifier:** Accurate. One addition: the break also starts a second table that uses 'PARAGRAPH | EXIT statement' as its header row (a '|---|---|' separator follows it), so PARAGRAPH is demoted from a data row to a column header.

### p194-1 · p194 · table-restructured · **structural**

- **PDF:** Term Subclause / data-division 13, Data division / environment-division 12, Environment division / options-paragraph 11.8, OPTIONS paragraph / procedure-division 14, Procedure division
- **Markdown:** | Term | Subclause |
|---|---|
| data-division | 13, Data division |

environment-division 12, Environment division

| options-paragraph | 11.8, OPTIONS paragraph |
|---|---|
| procedure-division | 14, Procedure division |
- **Why:** The single four-row meta-language term table of 10.5.1 is split into two tables with the environment-division row left as a loose paragraph between them, and options-paragraph is promoted to a second table's header row. All four mappings survive textually, but a reader or tool parsing the table sees only data-division and procedure-division as rows.
- **Repair:** —
- **Verifier:** Printed page 172 (PDF p194), end of 10.5.1: one table with header 'Term | Subclause' and four contiguous rows: data-division 13, Data division; environment-division 12, Environment division; options-paragraph 11.8, OPTIONS paragraph; procedure-division 14, Procedure division. page-194.md lines 38-46 match the claim's quotation exactly: a table holding only data-division, then environment-division as a loose paragraph, then a second table whose HEADER row is options-paragraph, with procedure-division as its only body row. All text is present and correctly paired, so no normative meaning is lost, but the table structure itself is broken (one row demoted to prose, one row promoted to a header). That is a genuine structural transcription defect, not wrapping or scaffolding. The claim quotes both sources accurately and its severity (structural, not normative) is correct.

### p260-1 · p260 · table-restructured · **structural**

- **PDF:** Term / Subclause table with three rows, the last "record-description-entry 13.10, Record description entry"
- **Markdown:** | constant-entry | 13.9, Constant entry |

record-description-entry 13.10, Record description entry
- **Why:** The last row of the 13.4.1 meta-language term table fell out of the markdown table and became a loose paragraph. Text present; table structure lost.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 238 (PDF p260) shows the 13.4.1 Term/Subclause table with three aligned rows: 77-level-entry / 13.11, 77-level data description entry; constant-entry / 13.9, Constant entry; record-description-entry / 13.10, Record description entry. The markdown table holds only the first two rows. The third row follows after a blank line as a plain paragraph, 'record-description-entry 13.10, Record description entry', outside the table. Both sources are quoted correctly. All the words are still there, so nothing normative is lost to a human reader. But the row is no longer a table row, so any tool that reads the term-to-subclause mapping from the markdown table loses the record-description-entry -> 13.10 entry. That makes this a real structural transcription defect, as claimed, and not a normative one.

### p261-1 · p261 · table-restructured · **structural**

- **PDF:** Term / Subclause table with three rows, the last "record-description-entry 13.10, Record description entry"
- **Markdown:** | constant-entry | 13.9, Constant entry |

record-description-entry 13.10, Record description entry
- **Why:** The last row of the 13.5.1 meta-language term table fell out of the markdown table and became a loose paragraph. Text present; table structure lost.
- **Repair:** —
- **Verifier:** Rendered PDF page 261 (printed 239), 13.5.1: the Term/Subclause table prints three aligned rows: 77-level-entry / 13.11, 77-level data description entry; constant-entry / 13.9, Constant entry; record-description-entry / 13.10, Record description entry. The markdown table holds only the first two rows. The third appears after a blank line as a plain paragraph, 'record-description-entry 13.10, Record description entry', so it sits outside the table. The claim quotes both sources accurately. The words are all present, so a human reader loses nothing. A tool that parses the term table loses the record-description-entry -> 13.10 mapping, which is why this counts as a real structural defect and not normative wording. The general-format bracket itself is transcribed correctly: three stacked alternatives inside a plain bracket with no choice-indicator bars, followed by an ellipsis.

### p262-1 · p262 · table-restructured · **structural**

- **PDF:** Term / Subclause table with three rows, the last "record-description-entry 13.10, Record description entry"
- **Markdown:** | constant-entry | 13.9, Constant entry |

record-description-entry 13.10, Record description entry
- **Why:** The last row of the 13.6.1 meta-language term table fell out of the markdown table and became a loose paragraph. Text present; table structure lost.
- **Repair:** —
- **Verifier:** The printed page (PDF p262, printed 240, 13.6.1) shows one Term/Subclause table of three rows: 77-level-entry / 13.11, constant-entry / 13.9, record-description-entry / 13.10. page-262.md lines 23-26 hold the first two as markdown table rows, then a blank line, then 'record-description-entry 13.10, Record description entry' as a loose paragraph. The row is cut off from the table. That is a real structural defect: anything that parses the table gets two of the three term-to-subclause mappings. The claim quotes both sources correctly. No wording is lost and the term and subclause still sit side by side in the prose, so the meaning a human reader takes away is unchanged. The defect is structural only, as the claim's severity already says.

### p274-1 · p274 · misplaced-content · **structural**

- **PDF:** validate-status-clause 13.16.60, VALIDATE-STATUS clause
value-clause 13.16.61, VALUE clause (data-item or table format)
- **Markdown:** | usage-clause | 13.16.58, USAGE clause |

validate-status-clause 13.16.60, VALIDATE-STATUS clause\
value-clause 13.16.61, VALUE clause (data-item or table format)
- **Why:** The last two rows of the 13.14.1 meta-language term table fell out of the table into a loose paragraph, so a table parser loses where validate-status-clause and value-clause (data-item or table format) are defined.
- **Repair:** —
- **Verifier:** On the printed page (PDF p274, printed 252) the Term/Subclause table under 'where the following meta-language terms are described' lists nine rows in one uniform column layout, running from class-clause to value-clause, with validate-status-clause (13.16.60) and value-clause (13.16.61) as its last two rows. In page-274.md the pipe table stops at '| usage-clause | 13.16.58, USAGE clause |', and those two rows appear after a blank line as a plain paragraph joined by a backslash line break. The words and clause numbers match the PDF exactly, so no text is lost. What is lost is table membership, which is a real structural defect: anything that reads the table gets 7 rows where the page has 9. The claim quotes both sources accurately, and 'structural' is the right severity because the defect is not normative.

### p432-1 · p432 · table-restructured · **structural**

- **PDF:** Character Positions    Contents
1-2    The two low-order digits of the year in the Gregorian calendar.
3-4    Two numeric characters of the month of the year in the range 01 through 12.
5-6    Two numeric characters of the day of the month in the range 01 through 31.
- **Markdown:**    Character Positions Contents

| 1-2 | The two low-order digits of the year in the Gregorian calendar. |
|---|---|
| 3-4 | Two numeric characters of the month of the year in the range 01 through 12. |
- **Why:** The printed table's header row (Character Positions / Contents) is detached from the table as a plain run-on line, and the first data row (1-2, year digits) is made the table's header row. No words are lost, but the table structure is wrong: a reader or tool treating the header row as column names drops the 1-2 row from the data.
- **Repair:** —
- **Verifier:** Accurate, with one note: the transcription does include the 5-6 row (the claim's markdown_says excerpt stops at 3-4). The defect is that the 'Character Positions / Contents' headings sit outside the table as a loose indented line and the 1-2 data row is the pipe table's header row.

### p434-1 · p434 · misplaced-content · **structural**

- **PDF:** Screen item Figurative constant / Alphabetic Alphanumeric SPACES / Alphanumeric Alphanumeric SPACES / Alphanumeric-edited Alphanumeric SPACES / Boolean ZEROS / National National SPACES / ...
- **Markdown:** | Alphanumeric | Alphanumeric SPACES |

   Alphanumeric-edited Alphanumeric SPACES

| Boolean | ZEROS |
|---|---|
- **Why:** The ACCEPT GR 13 table of screen-item categories and their figurative constants is one table in the PDF. The transcription splits it in two: the Alphanumeric-edited row falls out as loose text, and the second table makes 'Boolean | ZEROS' its header row. A reader or tool that parses the tables loses the Alphanumeric-edited mapping and treats Boolean as a column heading.
- **Repair:** —
- **Verifier:** Printed page 434 (folio 412) at 300 dpi shows ONE two-column table under the heading 'Screen item / Figurative constant' with eight rows in order: Alphabetic, Alphanumeric, Alphanumeric-edited (all Alphanumeric SPACES), Boolean ZEROS, National and National-edited (National SPACES), Numeric and Numeric-edited (ZEROS). page-434.md lines 3-15 hold a first table (header + Alphabetic, Alphanumeric), then the line '   Alphanumeric-edited Alphanumeric SPACES' as a loose paragraph outside any table, then a second table whose HEADER row is '| Boolean | ZEROS |' followed by the separator and the remaining four rows. No words are lost, so a human reading in order still recovers every mapping, but the table structure is genuinely corrupted: the Alphanumeric-edited row is no longer a table row and the Boolean row is demoted to a column heading. Any mechanical table parse loses the Alphanumeric-edited -> Alphanumeric SPACES mapping and misreads Boolean/ZEROS as headers. This is more than wrapping or furniture; the claim quotes both sources accurately and its 'structural' severity (not normative) is right.

### p637-1 · p637 · misplaced-content · **structural**

- **PDF:** Argument-1 characteristics | Value returned ... BINARY-CHAR SIGNED -128 (assuming an 8-bit twos-complement representation) / BINARY-CHAR UNSIGNED 0 (assuming an 8-bit twos-compliment representation)
- **Markdown:**    Argument-1

| characteristics | Value returned |
... | $\*\*,\*\*9.99 | 0 |

   BINARY-CHAR SIGNED -128 (assuming an 8-bit twos-complement representation)\
   BINARY-CHAR UNSIGNED 0 (assuming an 8-bit twos-compliment representation)
- **Why:** The NOTE example table is restructured: the two-line column header 'Argument-1 characteristics' is split so 'Argument-1' sits outside the table, and the last two rows (BINARY-CHAR SIGNED, BINARY-CHAR UNSIGNED) are detached from the table as loose text.
- **Repair:** —
- **Verifier:** The 300-dpi render of printed page 615 (PDF p637, §2002:15.46.3 NOTE) shows one seven-row, two-column table. Its bold header is 'Argument-1 / characteristics' (two lines) over 'Value returned', and its rows run from S999 to BINARY-CHAR UNSIGNED. In page-637.md, 'Argument-1' is a loose line above a pipe table whose header cell reads only 'characteristics'. The pipe table stops after the '$**,**9.99 | 0' row, and the BINARY-CHAR SIGNED and BINARY-CHAR UNSIGNED rows follow as loose prose lines with no column split. The claim quotes both sources accurately. No text is missing ('twos-compliment' is the PDF's own spelling), and a human reader can still pair each characteristic with its value. But the table is broken: two of its rows and half its header are outside it, so any tool that reads the table sees five rows, not seven. The damage is structural only. It is in an informative NOTE, so it carries no normative effect.

### p642-1 · p642 · misplaced-content · **structural**

- **PDF:** Numeric (some arguments may be integer)    Numeric   (last row of the Argument Type / Function type table)
- **Markdown:** | National | National |

   Numeric (some arguments may be integer) Numeric
- **Why:** The last row of the MIN function type table is detached from the table and rendered as a loose paragraph.
- **Repair:** —
- **Verifier:** The printed page 642 (printed folio 620, §2002:15.51 MIN function) shows a single two-column table with six rows: Alphabetic/Alphanumeric, Alphanumeric/Alphanumeric, Index/Index, All arguments integer/Integer, National/National, Numeric (some arguments may be integer)/Numeric. All six are in the same block with the same alignment. The markdown table has only the first five rows. The sixth is set after a blank line as an indented loose paragraph, '   Numeric (some arguments may be integer) Numeric', outside the pipe table. The words are all there and the claim quotes both sources accurately. The defect is structural: anything that reads the table (a reader scanning the rows, or a tool parsing the markdown table) loses the Numeric -> Numeric function-type mapping, and the loose line no longer separates the two cells.

### p687-1 · p687 · incorrect-text · **structural**

- **PDF:** Column headings (printed rotated): RECEIVE | SEND input-output-cd | SEND output-cd | PURGE | ACCEPT MESSAGE COUNT | ENABLE INPUT | ENABLE INPUT I-O TERMINAL | ENABLE OUTPUT | ENABLE INPUT | DISABLE INPUT I-O TERMINAL | DISABLE OUTPUT | Status key value
- **Markdown:** | RECEIVE | input-output-cd SEND | output-cd SEND | PURGE | COUNT MESSAGE ACCEPT | INPUT ENABLE | TERMINAL I-O INPUT ENABLE | OUTPUT ENABLE | INPUT ENABLE | TERMINAL I-O INPUT DISABLE | OUTPUT DISABLE | value<br>key<br>Status |  |
- **Why:** The rotated column headings of Table A.1 were transcribed with their words in reverse order (e.g. 'COUNT MESSAGE ACCEPT' for the ACCEPT MESSAGE COUNT statement, 'TERMINAL I-O INPUT DISABLE' for DISABLE INPUT I-O TERMINAL, 'value key Status' for Status key value). The headings no longer read as the statement forms they name; the X cells themselves match the PDF.
- **Repair:** —
- **Verifier:** The 300-dpi render of printed page 665 (PDF page 687) shows Table A.1 with rotated column headings reading bottom-to-top: RECEIVE, SEND input-output-cd, SEND output-cd, PURGE, ACCEPT MESSAGE COUNT, ENABLE INPUT, ENABLE INPUT I-O TERMINAL, ENABLE OUTPUT, ENABLE INPUT, DISABLE INPUT I-O TERMINAL, DISABLE OUTPUT, Status key value. The page-687 markdown header row reverses the word order in every multi-word heading ('input-output-cd SEND', 'COUNT MESSAGE ACCEPT', 'TERMINAL I-O INPUT ENABLE', 'TERMINAL I-O INPUT DISABLE', 'OUTPUT DISABLE', 'value<br>key<br>Status'). Both sources are quoted accurately. The cause is that the rotated text was OCR'd in the wrong direction. This is more than formatting: the headings name statement forms (for example ACCEPT MESSAGE COUNT, DISABLE INPUT I-O TERMINAL), and in reversed order they no longer read as those statements. The column order and every X cell and status row match the PDF, so the defect is limited to the header text. The ninth column really is printed as ENABLE INPUT, the same as the sixth, so the claim reproduces the PDF correctly there.

### p688-1 · p688 · incorrect-text · **structural**

- **PDF:** Column headings (printed rotated): SEND | PURGE | ENABLE OUTPUT | DISABLE OUTPUT | Error key value
- **Markdown:** | SEND | PURGE | OUTPUT ENABLE | OUTPUT DISABLE | value<br>key<br>Error |  |
- **Why:** The rotated column headings of Table A.2 were transcribed in reverse word order ('OUTPUT ENABLE', 'OUTPUT DISABLE', 'value key Error'), so they no longer name the ENABLE OUTPUT / DISABLE OUTPUT statement forms or the 'Error key value' column; the X cells match the PDF.
- **Repair:** —
- **Verifier:** Rendered page 688 (printed page 666, A.1.2.5, Table A.2) at 300 dpi. The rotated column headings, read bottom to top, are 'SEND', 'PURGE', 'ENABLE OUTPUT', 'DISABLE OUTPUT', 'Error key value'. Line 9 of page-688.md has 'OUTPUT ENABLE', 'OUTPUT DISABLE' and 'value<br>key<br>Error': the words of each multi-word rotated heading are in reverse order. The statement names ENABLE OUTPUT and DISABLE OUTPUT no longer read correctly, and neither does the 'Error key value' column. I compared every X cell, key value (0, 1, 2, 4, 5, 6, 7-9, A-Z) and description row by row, and all of them match the PDF. The claim quotes both sources accurately. This is a real text defect in the table headings and not a layout difference.

### p740-1 · p740 · table-or-figure-lost · **structural**

- **PDF:** Figure E.1 is a flowchart: Entrance -> 'Index Setting: exceeds highest permissible occurrence or is zero or negative' --True (AT END)*--> 'imperative-statement-1'; --False--> 'Condition-1' --True--> 'imperative-statement-2'; --False--> 'Condition-2' (*) --True--> 'imperative-statement-3' (*); --False--> 'Increment index-name for identifier-1' -> 'Increment index-name-1 (for a different table) or identifier-2' (*) -> loop back to above the Index Setting test; the three imperative-statement exits are grouped by a brace marked '**'.
- **Markdown:** Entrance

   Index Setting:\
   exceeds highest True (AT END)\* imperative-\
   permissible occur- statement-1\
   rence or is zero or\
   &emsp;negative

False

True imperative- \*\* Condition-1

statement-2

False

| \* |  | \* |
|---|---|---|
| True | imperative- |  |

   Condition-2\
 ... statement-3

False

Increment\
index-name for\
identifier-1

Increment

### \*

   index-name-1\ ...
- **Why:** The flowchart's structure (which test leads where, the True/False branches, the loop back to the index test, and which boxes the '*' and '**' notes apply to) is lost: box labels are interleaved with branch labels, a spurious markdown table and a spurious '### *' heading were invented, and the loop-back is absent. The figure is informative (Annex E) but the transcription no longer conveys the serial SEARCH control flow.
- **Repair:** —
- **Verifier:** In the printed page 740 (printed page 718, E.2.4.3), Figure E.1 is a flowchart. Entrance leads to the Index Setting test. Its True (AT END)* branch goes to imperative-statement-1, and its False branch goes to Condition-1. Condition-1 True goes to imperative-statement-2, and False goes to Condition-2 (*). Condition-2 True goes to imperative-statement-3 (*), and False goes to 'Increment index-name for identifier-1', then to 'Increment index-name-1 (for a different table) or identifier-2' (*). A line from that box returns to just above the Index Setting test. A right brace marked ** groups the three imperative-statement exits. Lines 14-56 of page-740.md keep every word label, but they interleave box text with branch labels ('exceeds highest True (AT END)* imperative-'). They place '**' beside Condition-1 rather than on the brace. They invent a markdown table ('| * |  | * |' / 'True | imperative-') and a '### *' heading. Nothing records the loop-back edge. A reader cannot recover the control flow (that Index Setting is re-tested after the increments, or which boxes the * footnote marks) from the markdown. That is a real loss of figure content, not just reflow. The claim quotes both sources accurately. The figure is informative (Annex E), so this does not affect normative semantics.

### p745-1 · p745 · image-lost · **structural**

- **PDF:** Figure E.3 — Compilation group and run unit structures: a drawn diagram of nested boxes (a thick-bordered compilation group holding three compilation units; the middle one a source unit P-1 containing source elements P-1-1 and P-1-2, with P-1-2 containing P-1-2-1 and P-1-2-2), arrows labelled "To other run unit" and "From other comp. group", a lower thick-bordered run unit with runtime modules and dashed runtime elements mirroring the upper structure, and a Legend mapping border styles to Compilation Group, Compilation Unit, Source Unit, Source Element, Run Unit, Runtime Module, Runtime Element.
- **Markdown:** P-1

Legend

P-1-1 P-1-2 Compilation Group

P-1-2-1 P-1-2-2

Compilation Unit

Source Unit

Source Element

| To other run unit | To other run unit |
|---|---|
| From other comp. group | From other comp. group |

Run Unit

Runtime Module

Runtime Element
- **Why:** The figure was reduced to a scatter of its text labels (legend entries interleaved with box labels, the arrow captions turned into a two-row table). The nesting that the figure exists to show (which source elements sit in which source unit, and how compilation units map onto runtime modules in a run unit) and the legend's border-style key are lost. Annex E is informative, so no normative rule is affected.
- **Repair:** —
- **Verifier:** Figure E.3 is reduced to scrambled text labels. Printed figure: a thick-bordered compilation group holds three compilation units (thin double-line boxes). The middle unit holds source unit P-1, which has its own dashed source element and the nested source units P-1-1 and P-1-2. P-1-2 in turn holds the nested source units P-1-2-1 and P-1-2-2. Each source unit has a dashed source element. Arrows labelled 'To other run unit' lead down from the outer compilation units. A bracketed arrow leads from the middle compilation unit to the middle runtime module. Arrows labelled 'From other comp. group' lead into the outer runtime modules. The thick-bordered run unit below holds three runtime modules, and the middle one carries dashed runtime elements that mirror the source elements. A legend maps the border styles to Compilation Group, Compilation Unit, Source Unit, Source Element, Run Unit, Runtime Module and Runtime Element. All of the structure and the legend key are lost. This is informative Annex E, so it has no normative impact.

### p758-1 · p758 · image-restructured · **structural**

- **PDF:** Figure E.4 — COBOL communication environment: a drawn diagram with four columns (COBOL Program box listing RECEIVE, RECEIVE, SEND, ..SEND, .....RECEIVE, SEND; a vertical 'Communication Descriptions (CD)' bar labelled 'COBOL/MCS Interface'; the Message Control System (MCS) with an 'Input Queues' box and an 'Output Queues' box, '(communication using queues)' and '(transaction communication)'; a vertical 'ENABLE / DISABLE' bar labelled 'MCS/Communication Device Interface'; and five Communication Devices), with arrows showing data flowing devices -> input queues -> RECEIVE, SEND -> output queues -> devices, and the transaction-communication RECEIVE/SEND paths bypassing the queues.
- **Markdown:** | COBOL Program | Message Control System | Communication |
|---|---|---|
|  | (MCS) | Devices |

(communication using queues)

   . . . Input Queues\
   . . .
...
| SEND | (CD) |  |
|---|---|---|
| . . .<br>. . .<br>. . . | Descriptions . . . Output Queues | DISABLE |

| . . . | . . . | / |
|---|---|---|
| . . .<br>. . .<br>. . . | . . .<br>. . .<br>. . .<br>. . . | ENABLE |
| . . . | . . . | ENABLE |
...
| Communication ..SEND | . . . |
- **Why:** The figure was OCR'd into fragmentary tables: the arrows (all of the data-flow information), the device column and the CD and ENABLE/DISABLE bars are lost or scrambled ('Communication' and 'Descriptions' split into separate cells, 'ENABLE' duplicated, 'DISABLE' detached). The informative text on page 757 points the reader to this figure for the relationships among RECEIVE/SEND, the MCS queues and ENABLE/DISABLE; the markdown cannot convey them. Annex E is informative, so no normative rule is lost.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 736 (PDF p758) shows Figure E.4 as described: the COBOL Program box (RECEIVE, RECEIVE, SEND, ..SEND, .....RECEIVE, SEND), the vertical 'Communication Descriptions (CD)' bar over 'COBOL/MCS Interface', Input Queues and Output Queues boxes under '(communication using queues)', '(transaction communication)', the vertical 'ENABLE / DISABLE' bar over 'MCS/Communication Device Interface', five terminal icons, and arrows running devices -> input queues -> RECEIVE, SEND -> output queues -> devices, plus a direct RECEIVE/SEND path between the program and the bottom device. The markdown breaks this into scattered tables: it has no arrows, so it carries none of the data flow. 'Communication' and 'Descriptions' land in different cells ('Communication ..SEND', 'Descriptions . . . Output Queues'), ENABLE appears twice and DISABLE stands apart, and the device images are gone. That loss of content is genuine for anyone reading the markdown alone. The figure is in informative Annex E, so it is structural and no normative rule is lost. The claim quotes both sources accurately.

### p761-1 · p761 · image-restructured · **structural**

- **PDF:** Figure E.5 — Hierarchy of queues: a tree with levels QUEUE (A), SUB-QUEUE (1) (B, C), SUB-QUEUE (2) (D, E under B; F, G under C), SUB-QUEUE (3) (H, I under D; J, K under E; L, M under F; N, O under G) and MESSAGE (H: Z1 X2; I: X3 X4 X5; J: X1 Y3 Y5 Z5; K: Z6 Z7 Y6; L: Y7 Y8; M: Y1 Y2; N: X6; O: Z2 Z3 Z4 Y4).
- **Markdown:**    QUEUE

A

SUB-QUEUE (1)

| B |  |  | C |  |
|---|---|---|---|---|
| SUB-QUEUE (2)<br>D | E | F |  | G |

| I | O |
|---|---|

| Z1 | X3 | X1 | Z6 | Y7 | Y1 | X6 | Z2 |
|---|---|---|---|---|---|---|---|
| X2 | X4 | Y3 | Z7 | Y8 | Y2 |  | Z3 |
| MESSAGE | X5 | Y5 | Y6 |  |  |  | Z4 |
|  |  | Z5 |  |  |  |  | Y4 |
- **Why:** The tree structure is lost: the SUB-QUEUE (3) level label and sub-queue-3 names H, J, K, L, M and N are missing, the parent/child edges are gone, and 'MESSAGE' displaced X2's column so the message lists no longer line up under their sub-queues. The worked examples on page 762 (e.g. Sub-queue-3 N returns Message X6; Queue A returns Z1) cannot be followed from the markdown. Annex E is informative, so no normative rule is lost.
- **Repair:** —
- **Verifier:** Printed page 739 (PDF p761) shows Figure E.5 as a tree: QUEUE A; SUB-QUEUE (1) B, C; SUB-QUEUE (2) D, E under B and F, G under C; SUB-QUEUE (3) H, I under D, J, K under E, L, M under F, N, O under G; message lists Z1 X2 / X3 X4 X5 / X1 Y3 Y5 Z5 / Z6 Z7 Y6 / Y7 Y8 / Y1 Y2 / X6 / Z2 Z3 Z4 Y4. The markdown keeps only I and O of the sub-queue-3 names, omits the SUB-QUEUE (3) level label and all edges, and places 'MESSAGE' in the first column of the third message row, so the hierarchy needed to follow the page-762 worked examples cannot be recovered. The claim quotes both sources accurately. The content loss is genuine but sits in informative Annex E.

### p796-1 · p796 · image-lost · **structural**

- **PDF:** Figure E.7 draws an Account box (data items account-number, balance, date-opened; methods deposit, withdraw, balance) with callouts "Data items" and "Methods" pointing at its two compartments, and arrows up to it from a CheckingAccount box (charges; displayCharges, calculateCharges) and a SavingAccount box (interest-rate; calculateInterest).
- **Markdown:** account-number\
Data items balance\
date-opened

deposit\
Methods\
withdraw\
balance

| CheckingAccount | SavingAccount |
|---|---|
| charges | interest-rate |
| displayCharges | calculateInterest |

calculateCharges
- **Why:** The diagram is restructured lossily: the callout labels "Data items" and "Methods" are interleaved into the member lists (reading as if "Methods" were a method name), the CheckingAccount method calculateCharges is detached below the table instead of sitting in the CheckingAccount box, and the inheritance arrows are lost. Informative annex E; the prose below restates the members.
- **Repair:** —
- **Verifier:** Figure E.7 (informative annex E) is transcribed lossily. The callouts 'Data items' and 'Methods' are interleaved into the Account member lists, calculateCharges is detached from the CheckingAccount column, and the inheritance arrows from CheckingAccount and SavingAccount up to Account are lost. The 'Account' heading is present. No normative content is affected, and the following prose restates the members and the inheritance.

### p818-1 · p818 · image-restructured · **structural**

- **PDF:** Figure E.9 flowchart: each 'Condition-1 false' / 'Condition-2 false' / 'Condition-n false' decision has a 'yes' line joining one vertical line that ends in an arrow to 'Truth value is false'; the 'no' path from Condition-n false arrows to 'Truth value is true'.
- **Markdown:** Condition-1 yes\
&emsp;false

no
...
| Truth value | Truth value |
|---|---|
| is true | is false |
- **Why:** The flowchart's connectors are lost: the markdown lists box labels and a two-cell table, so it no longer states that every 'yes' branch leads to 'Truth value is false' and only the final 'no' leads to 'true' (the AND short-circuit the figure illustrates). Informative annex.
- **Repair:** —
- **Verifier:** Rendered printed page 796 (PDF p818) at 300 dpi. The figure shows Evaluate condition-1 -> rounded decision 'Condition-1 false'; its 'yes' exit goes right to a vertical line on the right; 'no' continues down to Evaluate condition-2 -> 'Condition-2 false' (yes joins the same right line, no continues) -> Evaluate condition-n -> 'Condition-n false' (yes joins the right line, which ends in an arrow to 'Truth value is false'; no arrows down to 'Truth value is true'). The markdown (page-818.md) keeps only the box and branch labels as loose paragraphs ('Condition-1 yes\ false', 'no', ...) and a two-column table 'Truth value | Truth value / is true | is false'. No connector or routing is stated, so the transcription does not say where any yes/no branch goes: the figure's whole content (any false condition makes the AND false; only all-true reaches true) is lost. The claim quotes both sources accurately. Labels are present, so nothing textual is missing; the loss is the diagram's structure. Informative annex E, so no normative impact, but a genuine content loss of the figure.

### p819-1 · p819 · image-restructured · **structural**

- **PDF:** Figure E.10 flowchart: each 'Condition-1 true' / 'Condition-2 true' / 'Condition-n true' decision has a 'yes' line joining one vertical line to 'Truth value is true'; the 'no' path from Condition-n true arrows to 'Truth value is false'.
- **Markdown:** Condition-1 yes\
&emsp;true

no
...
| Truth value | Truth value |
|---|---|
| is false | is true |
- **Why:** Connectors lost: nothing in the markdown says every 'yes' leads to 'Truth value is true' (the OR short-circuit). Informative annex.
- **Repair:** —
- **Verifier:** Rendered PDF page 819 (printed 797) at 300 dpi shows Figure E.10 as a flowchart: Evaluate condition-1 -> 'Condition-1 true' rounded decision; its 'yes' exits right to a vertical line that also collects the 'yes' exits of 'Condition-2 true' and 'Condition-n true' and ends in an arrow to 'Truth value is true'; each 'no' goes down to the next Evaluate box, and the 'no' of 'Condition-n true' arrows to 'Truth value is false'. The markdown (page-819.md) keeps every text label in order but no edges: the 'yes' labels are fused into the decision text ('Condition-1 yes / true'), and the two terminals are a two-column table with no statement of which path reaches which. The fact that any 'yes' ends evaluation with a true result (the short-circuit the figure exists to show) is not recoverable from the markdown except by inference from the caption. The claim quotes both sources accurately. It is a genuine content loss, but in the informative Annex E, so it has no normative effect.

### p820-1 · p820 · image-restructured · **structural**

- **PDF:** Figure E.11 flowchart: 'Condition-1 true' yes goes right to 'Truth value is true'; 'Condition-2 false' yes goes LEFT to 'Truth value is false'; 'Condition-3 false' yes goes left to 'Truth value is false' and its 'no' goes right to 'Truth value is true'.
- **Markdown:** yes Condition-2\
&emsp;&emsp;&emsp;&emsp;false

no
...
yes Condition-3 no\
&emsp;&emsp;&emsp;&emsp;&emsp;false

| Truth value | Truth value |
|---|---|
| is false | is true |
- **Why:** Connectors lost: only word position hints which branch reaches which truth value, so the figure's evaluation order for condition-1 OR condition-2 AND condition-3 cannot be recovered from the markdown. Informative annex.
- **Repair:** —
- **Verifier:** Rendered printed page 798 (PDF p820) at 300 dpi. The figure shows: Evaluate condition-1 -> 'Condition-1 true' decision; 'yes' exits right along a line that runs down to the arrow at 'Truth value is true'; 'no' goes down to Evaluate condition-2 -> 'Condition-2 false'; its 'yes' exits LEFT to a vertical line that runs down to the arrow at 'Truth value is false'; its 'no' goes down to Evaluate condition-3 -> 'Condition-3 false'; 'yes' exits left joining the false line, 'no' exits right joining the true line. The claim's description of the PDF is accurate. The markdown (page-820.md) keeps the box labels and the words yes/no but no connectors at all: 'Condition-1 yes\ true' gives no destination for yes, and the two truth values are rendered as a two-column table with no link to any branch. Which outcome each branch produces (the actual content of the figure: OR short-circuits to true on condition-1, AND short-circuits to false on condition-2) cannot be recovered from the markdown. This is a genuine loss of figure content, not reflow or furniture. It is in informative Annex E, so it carries no normative weight.

### p821-1 · p821 · image-restructured · **structural**

- **PDF:** Figure E.12 flowchart: 'Condition-1 true' yes and 'Condition-2 false' yes both go to 'Evaluate condition-3'; 'Condition-2 false' no goes down to 'Truth value is false'; 'Condition-3 false' yes and 'Condition-4 false' yes go to 'Truth value is false'; 'Condition-4 false' no goes to 'Truth value is true'.
- **Markdown:** Condition-2 yes\
&emsp;false

no Evaluate\
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;condition-3
- **Why:** Connectors lost; the markdown juxtaposes 'no Evaluate condition-3', which suggests the Condition-2 'no' branch evaluates condition-3, the opposite of the figure (its 'no' goes to 'Truth value is false'). Informative annex.
- **Repair:** —
- **Verifier:** Figure E.12 (informative Annex E) flowchart transcribed as flattened labels with every connector lost: the PDF routes Condition-1 true 'yes' and Condition-2 false 'yes' to 'Evaluate condition-3', Condition-2 'no', Condition-3 'yes' and Condition-4 'yes' to 'Truth value is false', and Condition-4 'no' to 'Truth value is true'; the markdown's 'no Evaluate condition-3' juxtaposition misreads as Condition-2's 'no' branch leading to condition-3. All labels present; topology missing.

### p825-1 · p825 · image-restructured · **structural**

- **PDF:** Figure E.13 flowchart: a line from 'Augment identifier-2 with current BY value' loops back with an arrow into 'Condition-1'.
- **Markdown:** Condition-1 True Exit

False

Execute specified\
set of statements

Augment identifier-2\
with current BY value
- **Why:** The loop-back edge, which makes this a loop with the test before execution, is lost; the markdown reads as a straight-line sequence ending at Augment. Informative annex.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 803 (PDF p825) shows Figure E.13 as a flowchart: Entrance -> 'Set identifier-2 equal to current FROM value' -> 'Condition-1' (True -> Exit; False -> 'Execute specified set of statements' -> 'Augment identifier-2 with current BY value'), and a line leaves the left side of the Augment box, runs up the left margin and enters Condition-1 with an arrowhead. page-825.md carries only the box labels as flat paragraphs, ending at 'Augment identifier-2 with current BY value' with nothing marking the return to Condition-1, so the figure reads as a straight-line sequence. The loop edge is the figure's content: it is what makes the figure show a repeated test-before-execution iteration. This is not page furniture, reflow or formatting. The claim quotes both sources accurately. Impact is limited: Annex E is informative, and the normative PERFORM VARYING rules elsewhere state the iteration.

### p826-1 · p826 · image-restructured · **structural**

- **PDF:** Figure E.14 flowchart: 'Condition-2' False -> 'Execute specified set of statements' -> 'Augment identifier-5 with current BY value' -> back to Condition-2; 'Condition-2' True -> 'Augment identifier-2 with current BY value' -> 'Set identifier-5 to current FROM value' -> back to Condition-1.
- **Markdown:** | Execute specified set | Augment identifier-2 |
|---|---|
| of statements | with current BY value |
| Augment identifier-5 | Set identifier-5 to |
| with current BY value | current FROM value |
- **Why:** The two loop-back edges and the True branch of Condition-2 are lost; the four boxes are put in a 2x2 table that implies no flow, so the nested-varying order cannot be read from the markdown. Informative annex.
- **Repair:** —
- **Verifier:** Rendered PDF page 826 (printed 804), Figure E.14: Condition-2 False goes down to 'Execute specified set of statements', then to 'Augment identifier-5 with current BY value', which loops back by a left-hand line into Condition-2. Condition-2 True goes right and down to 'Augment identifier-2 with current BY value', then to 'Set identifier-5 to current FROM value', which loops back by a bottom and far-left line into Condition-1. Condition-1 True goes to Exit. The markdown keeps every box label, but it puts the four lower boxes in a 2x2 table with a spurious header row. It has no arrows, no loop-back edges, and nothing ties 'True' to the right-hand column. The figure's content is the control flow, the order of the nested VARYING steps, and the markdown loses it. This is not just reflow. The claim quotes both sources correctly. The annex is informative, so it carries no normative weight.

### p827-1 · p827 · image-restructured · **structural**

- **PDF:** Figure E.15 flowchart: a line from 'Augment identifier-2 with current BY value' loops back with an arrow to the entry of 'Execute specified set of statements'.
- **Markdown:** Execute specified\
set of statements

True\
Condition-1 Exit

False

Augment identifier-2\
with current BY value
- **Why:** The loop-back edge (test after execution) is lost; the markdown reads as a straight-line sequence. Informative annex.
- **Repair:** —
- **Verifier:** Figure E.15 is transcribed as its node labels only: every edge is lost, including the meaningful loop-back from 'Augment identifier-2 with current BY value' to the entry of 'Execute specified set of statements', and the True/Exit branch off Condition-1 is collapsed into the text 'True Condition-1 Exit'. Informative annex E; no normative impact.

### p828-1 · p828 · image-restructured · **structural**

- **PDF:** Figure E.16 flowchart: 'Augment identifier-5' loops back to before 'Execute specified set of statements'; 'Condition-2' True goes to 'Condition-1'; 'Augment identifier-2' loops back to before 'Set identifier-5 to current FROM value'.
- **Markdown:** True\
Condition-2

False

Augment identifier-5\
with current BY value

Condition-1 True\
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;Exit

False

Augment identifier-2\
with current BY value
- **Why:** Both loop-back edges and the Condition-2 True edge to Condition-1 are lost; the markdown reads as a straight-line sequence. Informative annex.
- **Repair:** —
- **Verifier:** The rendered 300 dpi page (printed p.806) shows Figure E.16 with three non-sequential edges: a line from the left of 'Augment identifier-5 with current BY value' back to the arrow entering 'Execute specified set of statements'; a line from the left of 'Augment identifier-2 with current BY value' back to the arrow entering 'Set identifier-5 to current FROM value'; and the 'True' exit of Condition-2 running right, down and into Condition-1. Condition-1 True goes to Exit. page-828.md lists only the box labels and True/False words in reading order, with no arrows or loop targets, and puts 'True' before 'Condition-2'. A reader of the markdown cannot recover the nested loop or where Condition-2 True leads, and these are what the figure shows about PERFORM VARYING ... TEST AFTER execution order. The claim quotes both sources accurately. The figure is in an informative annex, so the loss does not affect normative rules.

### p859-1 · p859 · misplaced-content · **structural**

- **PDF:** Abbreviated combined relation conditions  136
Abnormal run unit termination  391, 392, 393
ABS function  575
- **Markdown:** Abbreviated combined relation conditions 136 Abnormal run unit termination 391, 392, 393 ABS function 575
- **Why:** Three separate index entries at the top of the right-hand column are merged into one line, which makes "136 Abnormal run unit termination" read as one entry. The page numbers can no longer be matched to their entries without the PDF.
- **Repair:** —
- **Verifier:** Three consecutive index entries (the first three of the right-hand column) are merged into one markdown paragraph without line breaks; the text is complete but the entry boundaries are lost. Fix: split line 59 into three lines ending in '\' like the neighbouring entries.

### p860-2 · p860 · misplaced-content · **structural**

- **PDF:** ALPHABETIC phrase
     CLASS clause  269
     INITIALIZE statement  457 (sub-entries indented under their headword throughout both columns)
- **Markdown:**    ALPHABETIC phrase\
   CLASS clause 269\
   INITIALIZE statement 457\
- **Why:** On page 860 nearly every index line has the same indentation, so sub-entries such as "CLASS clause 269" read as top-level headwords and can no longer be told apart from their parents. The only exceptions are the "Alphabetic 111" and "meric items 833" lines. The printed page indents every sub-entry under its headword.
- **Repair:** —
- **Verifier:** The index hierarchy on page 860 (printed p. 838) is flattened. Every entry in the markdown has the same indentation, so sub-entries such as 'CLASS clause 269' and 'INITIALIZE statement 457' under 'ALPHABETIC phrase' cannot be told apart from top-level headwords. Only the third-level 'Alphabetic 111' and one wrap continuation keep an indent. Separately, the first lines of the markdown are garbled, running 'SORT statement 526 ALLOCATE statement ... ALPHABET clause' together on one line. The page is a non-normative index.

### p861-1 · p861 · misplaced-content · **structural**

- **PDF:** WRITE statement  558
ATAN function  579 ... B  PICTURE symbol  313
BACKGROUND-COLOR clause  262
B-AND operator  118 ... File description entry  234
Boolean category  111, 112, 292, 316
Boolean character  8
- **Markdown:**    WRITE statement 558 ATAN function 579
...
B PICTURE symbol 313 BACKGROUND-COLOR clause 262 B-AND operator 118
...
   File description entry 234 Boolean category 111, 112, 292, 316 Boolean character 8
- **Why:** Three places merge separate index entries into one line. One of them joins a sub-entry (File description entry, under BLOCK CONTAINS clause) to two top-level headwords, so the entry boundaries and the parent/child structure are lost.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 839 (PDF page 861) shows each of these as a separate index line: 'WRITE statement 558' (indented sub-entry continuing from the previous page) then 'ATAN function 579' at top level; 'B PICTURE symbol 313', 'BACKGROUND-COLOR clause 262', 'B-AND operator 118' as three top-level lines; and at the top of the right column the indented sub-entry 'File description entry 234' (child of 'BLOCK CONTAINS clause 268', which ends the left column) followed by top-level 'Boolean category 111, 112, 292, 316' and 'Boolean character 8'. The markdown runs each group together on one line with no break or \ separator, and gives the merged lines a leading indent, so 'ATAN function', 'Boolean category' and 'Boolean character' look like sub-entries and the entry boundaries are lost. No words or page numbers are missing; the defect is lost entry and hierarchy structure. The claim quotes both sources accurately. The page is an informative index, so the defect does not affect normative content.

### p863-1 · p863 · misplaced-content · **structural**

- **PDF:** Comparison
     Between object reference identifiers  131
     Locale-based  129, 130 (sub-entries indented under their headword throughout both columns)
- **Markdown:**    Comparison\
   Between object reference identifiers 131\
   Locale-based 129, 130\
- **Why:** On page 863 every index line has the same indentation. Sub-entries (for example the ten "Of ... operands" lines under Comparison, or "Class condition 131" under Condition) can no longer be told apart from top-level headwords.
- **Repair:** —
- **Verifier:** The PDF index on page 863 is two-level (sub-entries indented under headwords, including entries continuing across the column break such as 'Class condition 131' under Condition); the markdown flattens every line to the same indentation, so sub-entries such as 'USAGE clause 361', 'Scope of 104' and the eleven sub-entries under Comparison are indistinguishable from headwords. Informative index; structural, not normative.

### p864-1 · p864 · misplaced-content · **structural**

- **PDF:** COUNT IN phrase
     UNSTRING statement  545 (left column above the D heading, and the entire right column, indent sub-entries under their headword)
- **Markdown:**    COUNT IN phrase\
   UNSTRING statement 545\
- **Why:** In the left column above the D heading and in the whole right column, every index line has the same indentation, so the parent/child structure is lost. The lines under the D heading are indented correctly.
- **Repair:** —
- **Verifier:** Printed page 842 (PDF p864) is a two-column index. Sub-entries are visibly indented under their headwords: ADD statement..SUBTRACT statement under the continued heading, UNSTRING statement 545 under COUNT IN phrase, ACCEPT MESSAGE COUNT statement 667 under COUNT phrase, SPECIAL-NAMES paragraph 194 under CRT STATUS / CURRENCY SIGN / CURSOR clause, EXIT statement 446 under CYCLE phrase; in the right column Definintion 365 (continuing Data-pointer), INITIALIZE statement 457, ACCEPT statement 409, TYPE clause 354, Alignment 389 under Decimal point, USE statement 549, Normal completion 392, and the DEPENDING ON / DESCENDING KEY sub-entries. In the markdown every one of those lines sits at the same three-space indent as its headword (e.g. '   COUNT IN phrase\' then '   UNSTRING statement 545\'), while the block under the D heading uses &emsp; for sub-entries correctly. The claim quotes both sources accurately. The loss changes what the index says: 'Alignment 389' and 'Definintion 365' read as top-level index terms rather than sub-entries of 'Decimal point' and 'Data-pointer', and 'UNSTRING statement 545' reads as its own term. Index content is non-normative, so this does not affect compiler semantics, but it is a genuine structural discrepancy, not mere whitespace.

### p865-1 · p865 · misplaced-content · **structural**

- **PDF:** EC-FLOW-RETURN exception 395, 505
EC-FLOW-USE exception 395, 550
- **Markdown:** EC-FLOW-RETURN exception 395, 505 EC-FLOW-USE exception 395, 550
- **Why:** Two separate index entries (top of the right column) are run together on one line, so the second reads as page references of the first. Informative index, but the list is restructured.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 865 (printed 843) shows EC-FLOW-RETURN exception 395, 505 and EC-FLOW-USE exception 395, 550 as the first two lines of the right column. They are two separate index entries, each on its own line. Line 61 of page-865.md joins them with a space and has no hard break, so the markdown renders them as one paragraph and one entry, while every neighbouring entry ends in a backslash hard break. No text is missing and the page numbers are correct, so the defect is only the lost boundary between two entries. That boundary is the index's structure, not a reflowed wrap of a single entry. The claim quotes both sources accurately. The index is informative, so the severity is structural, not normative.

### p866-2 · p866 · misplaced-content · **structural**

- **PDF:** EGI phrase
    SEND statement 673
Elementary items 107
Ellipses 18, 20
- **Markdown:** EGI phrase

   SEND statement 673 Elementary items 107 Ellipses 18, 20
- **Why:** The sub-entry SEND statement 673 (of EGI phrase) and the top-level entries Elementary items and Ellipses are merged on one line, detaching the sub-entry from its parent and hiding two top-level entries.
- **Repair:** —
- **Verifier:** Printed page 844 (PDF p866): the left column ends 'EGI phrase'; the right column begins with the indented sub-entry 'SEND statement 673', then the separate top-level entries 'Elementary items 107' and 'Ellipses 18, 20'. The markdown (page-866.md lines 60-62) has 'EGI phrase' with no trailing backslash, a blank line, then one line '   SEND statement 673 Elementary items 107 Ellipses 18, 20'. Everywhere else on the page the transcription keeps one entry per line ('\' line breaks) and marks sub-entries with '&emsp;' (for example 'EMI phrase\' / '&emsp;SEND statement 673\'). Here the sub-entry loses its indentation and its parent (a blank line splits it from 'EGI phrase'), and two top-level entries merge into it, so a reader sees one entry 'SEND statement 673 Elementary items 107 Ellipses 18, 20'. All the words are present, but the entry boundaries and the parent/child structure are lost. The claim quotes both sources accurately. This is a structural index defect, not a normative one.

### p867-2 · p867 · misplaced-content · **structural**

- **PDF:** FACTORIAL function 600
FACTORY clause
    METHOD-ID paragraph 181
Factory data 165, 770
...
FACTORY phrase 405
    USAGE clause 361
...
FALSE phrase
    EVALUATE statement 441
    SET statement, condition setting 516
...
Feature-name 72
    FILE-CONTROL paragraph 212
    RECORD DELIMITER clause 225
- **Markdown:** FACTORIAL function 600 FACTORY clause

   METHOD-ID paragraph 181\
   Factory data 165, 770\
...
   FACTORY phrase 405\
   USAGE clause 361\
...
   FALSE phrase\
   EVALUATE statement 441\
   SET statement, condition setting 516\
...
   Feature-name 72\
   FILE-CONTROL paragraph 212\
   RECORD DELIMITER clause 225
- **Why:** FACTORIAL function and FACTORY clause are merged on one line, and the whole F block is flattened to one indent level so sub-entries (METHOD-ID paragraph, USAGE clause, EVALUATE statement, FILE-CONTROL paragraph ...) are indistinguishable from top-level entries.
- **Repair:** —
- **Verifier:** In the F block, the transcription joins 'FACTORIAL function 600' and 'FACTORY clause' into one line. It also flattens every following entry to the same plain three-space indent, dropping the '&emsp;' sub-entry markers used on the rest of the page. As a result, the children METHOD-ID paragraph, Life cycle for, USAGE clause, EVALUATE statement, SET statement (condition setting), FILE-CONTROL paragraph and RECORD DELIMITER clause can no longer be told apart from top-level entries.

### p868-1 · p868 · misplaced-content · **structural**

- **PDF:**     SPECIAL-NAMES paragraph 194
Figurative constant 78
    INITIALIZE statement 459
    Symbolic characters in 81
File
    logical 146
...
FILE STATUS clause 220
    FILE-CONTROL paragraph 210
FILE-CONTROL paragraph 210
...
    Processor-dependent elements 4
- **Markdown:**    SPECIAL-NAMES paragraph 194 Figurative constant 78

   INITIALIZE statement 459\
   Symbolic characters in 81\
   File\
   logical 146\
...
   FILE STATUS clause 220\
   FILE-CONTROL paragraph 210\
   FILE-CONTROL paragraph 210\
...
   Processor-dependent elements 4
- **Why:** The sub-entry SPECIAL-NAMES paragraph 194 and the top-level entry Figurative constant 78 are merged on one line, and the whole left column is flattened to one indent level, losing the entry/sub-entry hierarchy (e.g. the two FILE-CONTROL paragraph 210 lines, one a sub-entry of FILE STATUS clause and one top-level, now look like a duplicate).
- **Repair:** —
- **Verifier:** Rendered page 868 (printed 846, Index) at 300 dpi. The PDF left column begins with the indented sub-entry 'SPECIAL-NAMES paragraph 194' (continuing a head from the previous page), then the top-level entry 'Figurative constant 78' on its own line, with indented sub-entries 'INITIALIZE statement 459' and 'Symbolic characters in 81'. Under 'FILE STATUS clause 220' the indented sub-entry 'FILE-CONTROL paragraph 210' is followed by a top-level 'FILE-CONTROL paragraph 210'. page-868.md line 3 reads '   SPECIAL-NAMES paragraph 194 Figurative constant 78' (two index entries fused on one line), and every following line carries the same 3-space indent, so head/sub-entry nesting is gone; lines 28-29 are two identical 'FILE-CONTROL paragraph 210' lines that read as a duplicate. Both sources are quoted accurately. This is index (informative) content, so it is structural rather than normative, but the fusion and lost nesting change which head a locator belongs to.

### p868-2 · p868 · misplaced-content · **structural**

- **PDF:**     Prototype versus repository 145
    Syntax violations 3
- **Markdown:**    Prototype versus repository 145 Syntax violations 3
- **Why:** Two sub-entries (of Flagging and warning mechanisms) are merged into one line.
- **Repair:** —
- **Verifier:** Index sub-entries 'Prototype versus repository 145' and 'Syntax violations 3' (under 'Flagging and warning mechanisms', continued at the top of the right column) are on one line in the markdown with no line break between them. Both sets of text and page numbers are present.

### p869-1 · p869 · misplaced-content · **structural**

- **PDF:**     Reserved words 4
    Standard extensions 4
Implementor-defined
    Intermediate data item 122
    Language element list 677
    Language elements 3
    Native arithmetic 121
    Record types 706
IMPLEMENTS clause 178, 183
Implicit attributes 232
Implicit reference 102
Implicit scope terminators 384
Implicit transfer of control 387
IN phrase
- **Markdown:**    Reserved words 4 Standard extensions 4 Implementor-defined Intermediate data item 122 Language element list 677 Language elements 3 Native arithmetic 121 Record types 706 IMPLEMENTS clause 178, 183 Implicit attributes 232 Implicit reference 102 Implicit scope terminators 384 Implicit transfer of control 387 IN phrase
- **Why:** Fourteen index entries and sub-entries are run together into a single paragraph, destroying the list structure and the parent/child relationships.
- **Repair:** —
- **Verifier:** Index p.847, right column: 14 index lines (2 sub-entries of Implementation, Implementor-defined with its 5 sub-entries, 5 top-level IMPLEMENTS/Implicit entries, and IN phrase) are collapsed into one indented paragraph in the markdown. All text and page numbers are present, but the entry and sub-entry hierarchy is lost. This is a list-structure collapse, not misplaced content.

### p869-2 · p869 · misplaced-content · **structural**

- **PDF:** IN phrase
    COPY statement 32
    With identifiers 87
    With qualification 85
Incompatibility flagging 49
...
INITIAL clause
    CD entry 656
    PROGRAM-ID paragraph 186
Initial data 115, 238, 386
- **Markdown:**    COPY statement 32\
   With identifiers 87\
   With qualification 85\
   Incompatibility flagging 49\
...
   INITIAL clause\
   CD entry 656\
   PROGRAM-ID paragraph 186\
   Initial data 115, 238, 386
- **Why:** The rest of the right column is flattened to one indent level, so sub-entries cannot be told from top-level entries.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 869 (printed page 847, Index) shows the right column with two indent levels: top-level entries such as 'IN phrase', 'Incompatibility flagging 49', 'Index', 'INDEXED BY phrase 86, 512, 521', 'Indicators 22', 'INITIAL clause', 'Initial data 115, 238, 386', and indented sub-entries such as 'COPY statement 32', 'With identifiers 87', 'With qualification 85', 'Definition 308', 'CD entry 656', 'OCCURS clause 306', 'Comment 22, 23' ... 'Source 23', 'PROGRAM-ID paragraph 186'. In page-869.md every right-column line from 'COPY statement 32' to 'Initial program 117' carries the same three-space indent and no &emsp; marker, while the left column correctly uses &emsp; for sub-entries. The text is all present, but the parent/child relation that carries meaning in an index is lost: for example, 'Initial data' and 'Incompatibility flagging' read the same as the sub-entries 'CD entry' and 'PROGRAM-ID paragraph' of INITIAL clause. Both sources are quoted accurately. This is a structural index-hierarchy defect with no normative effect.

### p871-2 · p871 · misplaced-content · **structural**

- **PDF:**     START statement 531
Last-used state 386
LC_ALL 64
    SET statement, locale 517
...
LINE (LINES) keyword
    SEND statement 673
- **Markdown:**    START statement 531\
   Last-used state 386\
   LC_ALL 64\
   SET statement, locale 517\
...
   LINE (LINES) keyword\
   SEND statement 673
- **Why:** The rest of the left column is flattened to one indent level, losing the entry/sub-entry hierarchy.
- **Repair:** —
- **Verifier:** Hierarchy flattened, not misplaced. In the left column of index page 871, from 'START statement 531' through 'SEND statement 673', main entries and sub-entries carry the same 3-space indent, so the parent/child nesting the PDF prints is lost (for example 'LC_ALL 64' / 'SET statement, locale 517'). The text and page numbers are all there. The informative index is the only part affected.

### p872-1 · p872 · misplaced-content · **structural**

- **PDF:**     SET statement, switch setting 516
    SPECIAL-NAMES paragraph 194
    WRITE statement 558
- **Markdown:**    SET statement, switch setting 516 SPECIAL-NAMES paragraph 194 WRITE statement 558
- **Why:** Three sub-entries (continuing Mnemonic-name) are merged into one line and separated from their parent by a blank line.
- **Repair:** —
- **Verifier:** Printed page 850 (PDF p872), index: the left column ends with 'Mnemonic-name 67, 70, 194' and indented sub-entries ACCEPT 409, DISPLAY 435, Scope of 103, SEND 673; the right column continues at the top with three indented sub-entries on separate lines: 'SET statement, switch setting 516', 'SPECIAL-NAMES paragraph 194', 'WRITE statement 558', then 'MOD function 621'. The markdown (page-872.md lines 59-63) ends the &emsp;-indented Mnemonic-name list at 'SEND statement 673', then after a blank line has a single line '   SET statement, switch setting 516 SPECIAL-NAMES paragraph 194 WRITE statement 558' with no &emsp; indent and no line breaks, then a blank line. All words and page numbers are present, but the three index sub-entries are fused into one paragraph and detached from their parent entry, so the index structure (which entries are sub-entries of Mnemonic-name, and where one entry ends) is lost. The claim quotes both sources accurately. Structural, not normative (index only).

### p873-1 · p873 · misplaced-content · **structural**

- **PDF:**     NEXT GROUP clause 304
NEXT phrase
    READ statement 496
NEXT RECORD phrase
    READ statement 496
...
NOT ON SIZE ERROR phrase 400
    ADD statement 415
    COMPUTE statement 431
    DIVIDE statement 438
- **Markdown:**    NEXT GROUP clause 304 NEXT phrase

   READ statement 496\
   NEXT RECORD phrase\
   READ statement 496\
...
   NOT ON SIZE ERROR phrase 400\
   ADD statement 415\
   COMPUTE statement 431\
   DIVIDE statement 438
- **Why:** The sub-entry NEXT GROUP clause 304 and top-level NEXT phrase are merged on one line, and the left column is flattened to one indent level, losing the hierarchy.
- **Repair:** —
- **Verifier:** Accurate as filed, with one addition. The same merge happens at the top of the right column: the PDF prints the sub-entries 'MULTIPLY statement 482' and 'SUBTRACT statement 539' (still under NOT ON SIZE ERROR phrase) and then the top-level heading 'NOT operator', each on its own line, but the markdown joins them into the single line 'MULTIPLY statement 482 SUBTRACT statement 539 NOT operator'. In both columns the markdown drops the difference between heading and sub-entry levels. Severity: structural (index only).

### p873-2 · p873 · misplaced-content · **structural**

- **PDF:**     MULTIPLY statement 482
    SUBTRACT statement 539
NOT operator
    In complex conditions 135
    In negated conditions 135
NOT phrase
...
NUMVAL-F function 626
- **Markdown:**    MULTIPLY statement 482 SUBTRACT statement 539 NOT operator

   In complex conditions 135\
   In negated conditions 135\
   NOT phrase\
...
   NUMVAL-F function 626
- **Why:** Two sub-entries and the top-level NOT operator are merged on one line, and the right column down to NUMVAL-F is flattened to one indent level.
- **Repair:** —
- **Verifier:** Accurate for the right column. The same defect also affects the left column: line 3 fuses 'NEXT GROUP clause 304' (a sub-entry) with the headword 'NEXT phrase', and lines 5-60 are flattened to one indent level as well. Only the O section (lines 102-117) keeps its hierarchy, using &emsp;.

### p874-1 · p874 · misplaced-content · **structural**

- **PDF:**     Initialization 372, 419
OBJECT-COMPUTER paragraph 191
...
OBJECT-REFERENCE phrase
    INITIALIZE statement 457
...
OVERRIDE clause
    METHOD-ID paragraph 181
- **Markdown:**    Initialization 372, 419\
   OBJECT-COMPUTER paragraph 191\
...
   OBJECT-REFERENCE phrase\
   INITIALIZE statement 457\
...
   OVERRIDE clause\
   METHOD-ID paragraph 181
- **Why:** Both columns of the page are flattened to one indent level, so every sub-entry reads as a top-level entry.
- **Repair:** —
- **Verifier:** The two-level index hierarchy on this page (sub-entries indented under head entries in both columns) is flattened in the markdown: every entry carries the same indent, so sub-entries such as 'INITIALIZE statement 457' (under OBJECT-REFERENCE phrase) cannot be told apart from head entries. All of the text is present. Only the nesting is lost.

### p874-2 · p874 · misplaced-content · **structural**

- **PDF:** P    PICTURE symbol 313
p_cs_precedes 65
- **Markdown:** P PICTURE symbol 313 p_cs_precedes 65
- **Why:** Two separate index entries are merged into one line.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 852 (PDF p874), right column under the 'P' letter heading, shows two separate index lines: 'P    PICTURE symbol 313' and then 'p_cs_precedes 65', followed by 'PACKED-DECIMAL phrase'. In page-874.md line 116 these are one line, 'P PICTURE symbol 313 p_cs_precedes 65', with no line break or backslash, unlike the surrounding entries, which each end in a hard break. No word is lost, but two index entries become one, so a reader sees p_cs_precedes 65 as part of the 'P PICTURE symbol' entry. The page reference 65 then reads as belonging to the wrong entry. Both sources are quoted accurately. This changes the index structure, not the normative text.

### p875-1 · p875 · misplaced-content · **structural**

- **PDF:**     USAGE clause 361
PADDING CHARACTER clause 224
    FILE-CONTROL paragraph 212
Page advance 452
- **Markdown:**    USAGE clause 361 PADDING CHARACTER clause 224 FILE-CONTROL paragraph 212 Page advance 452
- **Why:** Two entries and two sub-entries are run together on one line.
- **Repair:** —
- **Verifier:** The rendered page 875 (printed index page 853) shows four separate index lines at the top of the left column: the indented sub-entry 'USAGE clause 361' (carried over from the previous page), the main entry 'PADDING CHARACTER clause 224', its indented sub-entry 'FILE-CONTROL paragraph 212', and the main entry 'Page advance 452'. Line 3 of page-875.md runs all four together on one line with no line breaks, no &emsp; indentation and no trailing backslashes, while every other entry on the page keeps its own line and indentation. The words and page numbers are all present, but the index hierarchy is lost. The merged line reads as one entry, and it no longer shows that FILE-CONTROL paragraph 212 is a sub-entry of PADDING CHARACTER clause or that Page advance 452 is a separate main entry. This is a structural defect in index content, not wrapping that changes nothing. It is not normative.

### p876-1 · p876 · misplaced-content · **structural**

- **PDF:**     REWRITE statement 507
    UNLOCK statement 544
Record locking 155, 156, 221, 229, 544, 711
Record locks 532, 544
- **Markdown:**    REWRITE statement 507 UNLOCK statement 544 Record locking 155, 156, 221, 229, 544, 711 Record locks 532, 544
- **Why:** Two sub-entries (continuing RECORD keyword) and two top-level entries are run together on one line.
- **Repair:** —
- **Verifier:** Rendered page 876 (printed 854), right column top: 'REWRITE statement 507' and 'UNLOCK statement 544' are indented sub-entries continuing 'RECORD keyword' from the bottom of the left column; 'Record locking 155, 156, 221, 229, 544, 711' and 'Record locks 532, 544' are separate flush-left top-level entries. page-876.md line 61 holds all four as one space-indented line, so no text is lost but the index hierarchy is: the two top-level entries read as part of the RECORD keyword sub-entry, and the sub-entries lack the &emsp; marker every other sub-entry carries (e.g. line 59 'RETURN statement 505', line 74 'UNLOCK statement 544'). The claim quotes both sources accurately. Index-only structure; no normative effect.

### p877-1 · p877 · misplaced-content · **structural**

- **PDF:** Relative organization  705
RELATIVE phrase
    ORGANIZATION clause  223
- **Markdown:** Relative organization 705 RELATIVE phrase

   ORGANIZATION clause 223\
- **Why:** Two separate index entries (top-level 'Relative organization' and top-level 'RELATIVE phrase') are run together on one line, so the 'RELATIVE phrase' heading entry is lost as an entry of its own.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 855 (PDF p877), left column top, shows 'Relative organization 705' on one line, then 'RELATIVE phrase' as a separate top-level entry on the next line, then the indented sub-entry 'ORGANIZATION clause 223'. The markdown's line 3 reads 'Relative organization 705 RELATIVE phrase': the two top-level entries are merged into one line, so 'RELATIVE phrase' appears as trailing text after the page number 705 and is no longer a heading of its own. The 'ORGANIZATION clause 223' sub-entry then seems to belong to the merged line. This goes beyond line wrapping: the index structure changes. It is index content only, so it has no normative effect.

### p877-2 · p877 · misplaced-content · **structural**

- **PDF:** Relative record number  148, 227
RELEASE statement  503
REM function  634
REMAINDER phrase
    DIVIDE statement  438
- **Markdown:**    Relative record number 148, 227\
   RELEASE statement 503\
   REM function 634\
   REMAINDER phrase\
   DIVIDE statement 438\
- **Why:** The whole left column (from 'ORGANIZATION clause 223' through 'INVOKE statement 468') is given one uniform indent, so main entries and their sub-entries are indistinguishable; the right column keeps the hierarchy with &emsp;. The index's two-level structure is lost for that column.
- **Repair:** —
- **Verifier:** In the printed page (index p.855), the left column has two levels: main entries are flush (Relative record number, RELEASE statement, REM function, REMAINDER phrase) and sub-entries are indented (ORGANIZATION clause 223 under RELATIVE phrase, DIVIDE statement 438 under REMAINDER phrase, and so on through ALLOCATE/CALL/INVOKE under RETURNING phrase). In page-877.md every line from 'ORGANIZATION clause 223' to 'INVOKE statement 468' carries the same 3-space prefix, which Markdown drops when it renders. So 'DIVIDE statement 438' reads as a main entry with the same standing as 'REMAINDER phrase'. The right column marks sub-entries with &emsp;, which shows what the transcription should look like. The claim quotes both sources correctly and names the affected range correctly. The defect is real: the index's parent/child relationships are lost. It is not just whitespace. It is index-only and affects no normative rule.

### p877-3 · p877 · misplaced-content · **structural**

- **PDF:** RETURNING phrase
    ...
    INVOKE statement  468
    Procedure division header  91, 377
REVERSE function  635
- **Markdown:**    INVOKE statement 468

   Procedure division header 91, 377 REVERSE function 635
- **Why:** The sub-entry 'Procedure division header 91, 377' (under RETURNING phrase, continued at the top of the right column) is merged with the separate top-level entry 'REVERSE function 635' on one line.
- **Repair:** —
- **Verifier:** On the printed page (855, PDF p877), the left column ends with RETURNING phrase sub-entries down to 'INVOKE statement 468'. The right column starts with the indented sub-entry 'Procedure division header 91, 377' and then, on its own line, the top-level entry 'REVERSE function 635'. In page-877.md, line 62 reads '   Procedure division header 91, 377 REVERSE function 635', a single indented line. The top-level index entry REVERSE function has been folded into a RETURNING-phrase sub-entry, so its page 635 reads as a third page reference for that sub-entry. The claim quotes both sources accurately. The defect is in the index only, not in normative text.

### p879-1 · p879 · misplaced-content · **structural**

- **PDF:** STATUS KEY clause
    CD entry  656
STOP  387
...
STRONG phrase
    TYPEDEF clause  359
- **Markdown:**    STATUS KEY clause\
   CD entry 656\
   STOP 387\
...
   STRONG phrase\
   TYPEDEF clause 359\
- **Why:** The whole left column (from 'Declarative 382' through 'System-name 71') carries one uniform indent, so main entries and sub-entries are indistinguishable (e.g. 'CD entry 656' reads as a top-level entry); the right column keeps the hierarchy with &emsp;.
- **Repair:** —
- **Verifier:** Lost index hierarchy, not misplaced content. In the left column of the markdown (Declarative 382 through System-name 71), every line has the same 3-space indent, which Markdown ignores, so the 17 sub-entries printed indented in the PDF (Declarative 382, Procedural 382, CD entry 656 x5, TYPEDEF clause 359, Data-name 86, Condition-name 86, CORRESPONDING phrase 401, Definition 16, Condition-name 102, SPECIAL-NAMES paragraph 194/195, Figurative constant 81, Scope of 103, SET statement 517) cannot be told apart from main entries. They should carry the &emsp; prefix that the right column uses. Every word and page number is present and in order. Structural, informative index only.

### p880-1 · p880 · misplaced-content · **structural**

- **PDF:**     SET statement, switch setting  516
TO PROGRAM phrase
    USAGE clause  361
TO VALUE clause
    INITIALIZE statement  457
- **Markdown:**    SET statement, switch setting 516\
   TO PROGRAM phrase\
   USAGE clause 361\
   TO VALUE clause\
   INITIALIZE statement 457\
- **Why:** The whole left column above the U heading (from 'ADD statement 415' through 'Types of functions 566') carries one uniform indent, so top-level entries (TO PROGRAM phrase, TRAILING phrase, TRUE phrase, TYPE clause ...) and their sub-entries are indistinguishable.
- **Repair:** —
- **Verifier:** Index hierarchy flattened, not misplaced content: on page 880 (printed 858), markdown lines 3-41 (ADD statement 415 through Types of functions 566) all carry an identical 3-space indent, while the PDF distinguishes flush-left headwords (TO PROGRAM phrase, TO VALUE clause, TRAILING phrase, Transaction communication, Transfer of control, TRUE phrase, TURN directive, TYPE clause, TYPEDEF clause, Type-name, Types, Types of functions) from their indented sub-entries. The rest of the page uses &emsp; for sub-entries; these lines should too, with headwords flush left. Text content is otherwise complete and correct.

### p880-2 · p880 · misplaced-content · **structural**

- **PDF:** UCS  9, 10, 16, 59, 75, 93, 479, 751, 760, 824
    Definition  16
- **Markdown:** UCS 9, 10, 16, 59, 75, 93, 479, 751, 760, 824 Definition 16
- **Why:** The sub-entry 'Definition 16' is run into the page list of its parent 'UCS', so it reads as part of the UCS locator list and the sub-entry is lost.
- **Repair:** —
- **Verifier:** The printed page (index p. 858, PDF p. 880, left column under 'U') shows 'UCS  9, 10, 16, 59, 75, 93, 479, 751, 760, 824' on one line and an indented sub-entry 'Definition  16' on the next. page-880.md line 45 reads 'UCS 9, 10, 16, 59, 75, 93, 479, 751, 760, 824 Definition 16' as one paragraph, with no line break ('\') and no '&emsp;' indent. Every other sub-entry on the page uses those markers, for example 'UCS-4 phrase\' then '&emsp;SPECIAL-NAMES paragraph 195\'. No text is missing, but the entry's structure is lost: the sub-entry reads as part of the UCS locator list. This is a real structural defect in informative index content, and it has no normative effect.

### p881-2 · p881 · misplaced-content · **structural**

- **PDF:** Weakly-typed items  114, 742
WHEN phrase
    EVALUATE directive  46
...
WHEN SET TO FALSE phrase
    VALUE clause  369
- **Markdown:** Weakly-typed items 114, 742 WHEN phrase

   EVALUATE directive 46\
...
   WHEN SET TO FALSE phrase\
   VALUE clause 369\
- **Why:** 'Weakly-typed items' and 'WHEN phrase' are merged on one line, and the rest of the W section (through 'WRITE statement 196, 558') carries one uniform indent, so top-level entries and sub-entries are indistinguishable.
- **Repair:** —
- **Verifier:** The rendered PDF page (printed page 859, Index, W section, left column) shows 'Weakly-typed items 114, 742' and 'WHEN phrase' on separate lines at the left margin, with sub-entries (EVALUATE directive 46, EVALUATE statement 441, INVALID clause 294, SEARCH statement 512) indented below WHEN phrase. Top-level entries such as WHEN SET TO FALSE phrase, WHEN-COMPILED function 650, WITH ... phrase, Word 66, Working-storage section 238 and WRITE statement 196, 558 sit at the margin, and their sub-entries are indented. page-881.md line 7 reads 'Weakly-typed items 114, 742 WHEN phrase', so two headwords are joined into one line. Lines 9-53 then give every line the same three-space indent, so a reader cannot tell which entries are headwords and which are sub-entries. For example, 'WHEN-COMPILED function 650' looks like a sub-entry of 'WHEN SET TO FALSE phrase'. The claim quotes both sources correctly. The page is the index, which is informative and not normative, so 'structural' is the right severity.

### p881-3 · p881 · misplaced-content · **structural**

- **PDF:** YEAR-TO-YYYY function  651
YYYYDDD phrase
    ACCEPT statement  409
YYYYMMDD phrase
    ACCEPT statement  409
- **Markdown:** YEAR-TO-YYYY function 651 YYYYDDD phrase

   ACCEPT statement 409\
   YYYYMMDD phrase\
   ACCEPT statement 409
- **Why:** 'YEAR-TO-YYYY function' and 'YYYYDDD phrase' are merged on one line, and the following entry/sub-entry hierarchy is flattened to one indent.
- **Repair:** —
- **Verifier:** Index (p.859): 'YYYYDDD phrase' is run onto the 'YEAR-TO-YYYY function 651' line, and the top-level headword 'YYYYMMDD phrase' is indented as a sub-entry alongside the two 'ACCEPT statement 409' sub-entries. In the PDF, YEAR-TO-YYYY function, YYYYDDD phrase and YYYYMMDD phrase are three separate top-level entries, and each of the two phrases has its own 'ACCEPT statement 409' sub-entry. This is a non-normative index structure defect only.

### p3-1 · p3 · missing-text · **cosmetic**

- **PDF:** INTERNATIONAL STANDARD ISO/IEC 1989
- **Markdown:** STANDARD 1989
- **Why:** The ISO title-page banner lost the words "INTERNATIONAL" and "ISO/IEC", so the transcription's title page reads "STANDARD 1989" instead of naming the document as International Standard ISO/IEC 1989. Front matter only; no normative content is affected.
- **Repair:** —
- **Verifier:** The 300 dpi render of page 3 prints a two-column title banner: 'INTERNATIONAL / STANDARD' on the left and 'ISO/IEC / 1989' on the right. The page-3 markdown's first content line is just 'STANDARD 1989'. Neither 'INTERNATIONAL' nor 'ISO/IEC' appears anywhere else in the banner text. 'ISO/IEC 1989:2002(E)' does appear lower down, but only in the reference-number line. The OCR dropped the top line of each column. Both sources are quoted accurately. The words are genuinely missing; this is not a case of reflow or hyphenation. The banner is title-page content, not running header or footer furniture. The cosmetic severity is right: the page is front matter with no normative effect, and the reference number still identifies the document.

### p42-1 · p42 · ocr-error · **cosmetic**

- **PDF:** When an operand is textually subscripted, as operand-j<sub>n</sub>, the term 'operand-j' identifies a specific operand and 'n' refers to the nth position or occurrence of operand-j.
- **Markdown:** When an operand is textually subscripted, as operand-jn, the term 'operand-j' identifies a specific operand and 'n' refers to the nth position or occurrence of operand-j.
- **Why:** The subscript n is printed as a subscript in the PDF; the markdown flattens it to 'operand-jn', so the very notation 5.3.1 defines is not shown (it reads as a different operand name).
- **Repair:** —
- **Verifier:** Rendered printed page 20 (PDF p42) at 300 dpi: §2002:5.3.1 reads 'as operand-j' followed by a lowered, smaller 'n' (a true subscript glyph). page-42.md line 9 reads 'as operand-jn' with the n on the baseline, no subscript markup. Both sources are quoted accurately by the claim. Ordinarily lost subscript styling is mere formatting, but here the subscript IS the content: 5.3.1 exists to define the textual-subscript notation, and the flattened 'operand-jn' is indistinguishable from an operand named 'operand-jn', so the defining example no longer shows the notation it defines. Genuine but cosmetic transcription loss; no normative rule text is missing.

### p348-1 · p348 · ocr-error · **cosmetic**

- **PDF:** Simple, special, and fixed insertion symbols / Zero-suppression and floating insertion symbols / Other symbols
- **Markdown:** symbols<br>insertion<br>fixed<br>and<br>special,<br>Simple, | ... | floating<br>andsymbols Zero-suppression<br>insertion | ... | symbols<br>Other
- **Why:** The rotated row-group labels of Table 11 were OCR'd with their words in reversed and scrambled order ("andsymbols Zero-suppression"). Every x cell of the 24-column by 24-row matrix was checked against the printed table and matches; only the group labels are garbled, and the column header row carries the same group names correctly, so no precedence rule is lost.
- **Repair:** —
- **Verifier:** The printed page 348 (PDF page 326, Table 11) shows the vertical row-group labels 'Simple, special, and fixed insertion symbols', 'Zero-suppression and floating insertion symbols' and 'Other symbols' rotated 90 degrees. The markdown (lines 9, 18, 24) transcribes them word-reversed ('symbols<br>insertion<br>fixed<br>and<br>special,<br>Simple,', 'symbols<br>Other') and, for the middle group, scrambled with a fused token ('floating<br>andsymbols Zero-suppression<br>insertion'). This is word-order garbling, not mere line wrapping, so the label text as written is wrong. Spot-checks of the x cells (rows B0/, 9, E, Z*) against the image match, and the column-header row in the markdown carries the three group names correctly, so no precedence rule is lost: cosmetic, as claimed.

### p795-1 · p795 · image-lost · **cosmetic**

- **PDF:** Figure E.6 draws three boxes, Person, Employee and Manager, stacked vertically, with an arrow from Manager up to Employee and from Employee up to Person (the inheritance direction).
- **Markdown:** Person

Employee

Manager

Figure E.6 — Manager class
- **Why:** The figure's arrows (the subclass-to-superclass relationships) are lost; only the three box labels survive as loose paragraphs. Informative annex E; the following prose restates the hierarchy, so no normative content is lost.
- **Repair:** —
- **Verifier:** The rendered page (printed p.773, PDF p.795) shows Figure E.6 as three boxed labels, Person, Employee and Manager, stacked vertically, with a filled arrowhead from Manager pointing up to Employee and from Employee pointing up to Person. page-795.md keeps only the three labels as separate paragraphs and the caption. It has no boxes, no arrows and no description of them, so the figure's relationship content is missing from the transcription. Both sources are quoted correctly. The paragraph after the figure states the same hierarchy in words, and Annex E is informative, so the loss is cosmetic, as the claim says. No normative content is lost.

## Refuted — deliberately NOT repaired

- p225-1 · misplaced-content — Formatting only: the markdown flattens the nesting of rule 14's sub-paragraphs (NOTE 1 and the grouping-separator sentence printed as continuations of b)), but the text is complete and the following 'no effect on ... locale format' sentence
- p720-1 · misplaced-content — The printed page (p.698 of ISO/IEC 1989:2002, PDF page 720) shows the Latin row wrapping after '0250-' onto a second line '02A8, 1E00-1E9B, 1EA0-1EF9, 207F'. page-720.md line 36 ends the table cell with '0250-' and line 38 carries '02A8, 1E
- p720-2 · table-restructured — The printed page (p720, folio 698) sets this as an unruled two-column list, not a ruled table: a three-line bold underlined heading 'Script or / character / classification' and 'Letters or range of letters', then 'Latin:' and 'Greek:' rows.
- p56-1 · misplaced-content — Printed page 56 (PDF p34) shows item 4 'For alphanumeric, boolean and national literals:' with sub-items a. and b. and the NOTE indented beneath it. The markdown (page-56.md lines 15-21) carries every word of item 4, a., the NOTE and b. in 
- p59-1 · misplaced-content — Printed page 37 (PDF p59), GR7: a) ends 'results in the following:', and steps 1. and 2. are indented one level deeper than a)-d). page-59.md lines 21-31 carry the same words in the same order: a) with its colon, then '1\.' and '2\.', then 
- p60-1 · misplaced-content — The printed page (p.38, REPLACE GR8c) nests c) > 1.-7. > a./b. with the NOTE indented under 4.a. The markdown (page-60.md lines 5-27) holds every item with its full wording, in printed order, with distinct label styles (c)/d)-g), 1\.-7\., a
- p86-1 · misplaced-content — The printed page (2002 p.64, PDF p.86, §2002:8.2) shows the LC_ALL description wrapping onto a second line: 'Locale-categories LC_COLLATE, LC_CTYPE, LC_MESSAGES, LC_MONETARY, / LC_NUMERIC, and LC_TIME, and any other categories included in t
- p87-1 · table-restructured — Formatting only: the markdown renders the borderless 8.2.1 locale-field list as a mix of pipe tables and loose lines and splits the p_cs_precedes description at the printed line wrap; all content and its order (and so the category grouping)
- p95-1 · misplaced-content — The printed page 73 (PDF p95), 8.3.1.1.3.1 table, shows '=  Relational operator - equal' with the indented continuation line 'and assignment operator in COMPUTE' directly below it, then '=='. The markdown (page-95.md lines 36-40) holds the 
- p105-1 · misplaced-content — Printed page 83 (PDF p105), 8.4.1.1 rule 6 continuation paragraph: the line wraps after 'rules 2 through' and the next line begins '5.  All available qualifiers...'. The markdown (page-105.md lines 31-33) holds every word in the same order:
- p126-2 · misplaced-content — The printed page (p.104, 8.4.5.1 rule 3) shows 1. and 2. indented one level deeper than a) and b), and the markdown puts all four at a 3-space indent. That part of the claim is accurate. Every word is present and in order, though. The items
- p140-1 · misplaced-content — The printed page (8.7.2, printed p.118) wraps the B-XOR meaning across two lines: 'Exclusive OR operation (boolean exclusive' / 'disjunction)'. The markdown (page-140.md lines 28-30) carries every word in the same order: the table row ends 
- p154-1 · misplaced-content — Printed page 132 (PDF p154), 8.8.4.1.3.3 GR2: a) > 1. > a./b., then 2., indented in three levels. The markdown (page-154.md lines 41-49) has every item, word for word, in the same order and with the same labels: 'a)', '1.', 'a.', 'b.', '2.'
- p155-1 · misplaced-content — Printed page 133 (PDF p155) indents items 1./2. one level deeper than b), c) and d). The markdown puts them at the same 3-space indent, but nothing is missing or misplaced. Every 1./2. pair follows its own parent item in the same order as t
- p172-1 · misplaced-content — The printed page 172 (p.150, 9.1.12) shows an untabled, indented two-column list of seven pairs: EC-I-O-AT-END '1', EC-I-O-INVALID-KEY '2', EC-I-O-PERMANENT-ERROR '3', EC-I-O-LOGIC-ERROR '4', EC-I-O-RECORD-OPERATION '5', EC-I-O-FILE-SHARING
- p197-1 · table-restructured — Printed page 197 (ISO/IEC 1989:2002 p.175, 11.1.1) shows one borderless two-column Term/Subclause list of eight rows: class-id 11.2, factory 11.3, function-id 11.4, interface-id 11.5, method-id 11.6, object 11.7, options 11.8, program-id 11
- p255-1 · table-restructured — The claim describes the two sources accurately. Printed page 233 (PDF p255) shows a plain two-column Term/Subclause list with two rows. The markdown has the first row as a pipe-table row and the second row ('record-description-entry 13.10, 
- p258-1 · misplaced-content — Printed page 236 (PDF p258), 13.3.4.3 GR2 c): the sub-items '1. the same corresponding values for any literals specified' and '2. the same corresponding external data items.' are indented one level deeper than c). The markdown (page-258.md 
- p264-1 · table-restructured — The printed page (PDF p264, printed 242, 13.7.1) shows a two-column Term/Subclause list with two rows: '77-level-entry | 13.11, 77-level data description entry' and 'report-group-description-entry | 13.7.4, Report group description entry'. 
- p266-1 · misplaced-content — Printed page 244 (PDF p266) shows the 13.8.1 term table with two rows: constant-entry -> 13.9, Constant entry; screen-description-entry -> 13.15, Screen description entry. The markdown (page-266.md lines 17-21) has the first row in a pipe t
- p323-1 · misplaced-content — The printed page (p.301 of the 2002 standard) does indent the two trial-sum paragraphs under c) and outdents 'If the page fit was declared successful...' to the rule-4 body margin, as claimed. The markdown carries every paragraph, word for 
- p433-1 · misplaced-content — Printed page 411 (PDF p433), ACCEPT GR 11: the TIME table shows 5-6 '...seconds past the minute in the range:' with a) and b) indented under its Contents column, then 7-8 '...hundredths of a second ... 00 through 99. 00 is returned if ...'.
- p460-1 · misplaced-content — The printed page (PDF p.460, printed 438) shows Formats 2 and 4 as DIVIDE {identifier-1 / literal-1} INTO {identifier-2 / literal-2} on one baseline, each brace two lines tall, exactly like Format 3's DIVIDE {..} BY {..}. The markdown does 
- p502-1 · misplaced-content — The printed page (p.480 of ISO/IEC 1989:2002, PDF page 502) does indent a.-c. one level under item 3., and items 1.-3. one level under rule b), and the markdown flattens every list to the same 3-space indent. But all text is present and cor
- p607-1 · table-restructured — Compared the 300-dpi render of PDF page 607 (printed 585) with page-607.md. Every word of the 15.16.2 table is present in the markdown in the printed order: the headings 'Character Positions' / 'Contents', rows 1-4 through 13-14 as table ro
- p624-1 · table-restructured — The printed page (2002 p.602, PDF p.624, §2002:15.33.3 NOTE) shows the bold two-line column heading 'Argument-1 / characteristics' over 'Value returned'. The markdown keeps every word: 'Argument-1' sits on the line directly above the table 
- p624-2 · table-restructured — The printed page (PDF p.624, printed 602, §2002:15.33.3 NOTE) shows seven rows: S999, S9(4) BINARY, 99V9(3), $**,**9.99BCR, $**,**9.99, BINARY-CHAR SIGNED +127 (assuming an 8-bit representation), BINARY-CHAR UNSIGNED +255 (assuming an 8-bit
- p808-1 · table-restructured — Checked against the 300 dpi render of PDF page 808 (printed page 786). Every label and content line of Figure E.8 is present in the markdown, in the printed order: Physical Top of Form / the first line that can be printed / <blank>; Logical
- p850-1 · misplaced-content — The printed page 850 (folio 828) shows item 78 as an unheaded three-column list: 12 names in column 1 ending EXCEPTION-LOCATION, EXCEPTION-LOCATION-N, and 11 in each of columns 2 and 3. The markdown keeps all 34 names in their printed order
- p857-1 · misplaced-content — The printed page 857 (folio 835) does end with H.2 item 4 and prints no further heading, and every word of the page is present in the markdown. The '# Bibliography and index' line is not transcribed content. It is one of the transcription's
- p860-1 · misplaced-content — The rendered page (printed page 838, Index) shows four lines: indented 'SORT statement 526' (a sub-entry continued from the previous page), 'ALLOCATE statement 116, 372, 418, 450', 'ALPHABET clause', indented 'SPECIAL-NAMES paragraph 195'. 
- p866-1 · misplaced-content — Both sources are quoted accurately. The rendered page (printed 844, Index) shows the two entries on separate lines, and markdown line 3 joins them with a space. Every word and page number is present and in order, and nothing is moved anywhe
- p867-1 · misplaced-content — The printed page (index p.845, right column top) shows three separate lines: 'Explicit scope terminators 384', 'Explicit transfer of control 387', 'Exponent 76'. The markdown (page-867.md lines 62-63) holds all three entries with the same w
- p871-1 · misplaced-content — The printed page (index p.849, PDF page 871, left column) shows LAST DETAIL phrase / PAGE clause 311 / LAST EXCEPTION phrase 454 / EXIT statement 446 / Last exception status 762 / Definition 391 / LAST phrase, each on its own line with sub-
- p881-1 · misplaced-content — The rendered page 881 (printed Index p. 859) shows exactly these six lines at the top of the left column, and the markdown's first paragraph carries every word and every page number (649, 375, 490, 333, 512) in the same order with nothing m
- p45-2 · misplaced-content — The printed page 45 shows three borderless rows (d / debugging indicator / a debugging line; - (hyphen) / continuation indicator / a continuation line; space / source indicator / any line that is not a comment line, a debugging line, or a c
- p56-2 · misplaced-content — The printed page 56 (ISO/IEC 1989:2002 p.34, COPY statement) holds items a) through f) with nested 1.-7. and a./b. sub-items and two NOTEs. The markdown transcribes every item with the same text, letters and order, and keeps the nested 1.-7
- p392-1 · ocr-error — Both quotes are accurate: the printed page 392 (folio 370), syntax rule 6 under FORMATS 1, 2, 3, AND 5, reads 'Boolean literals in the VALUE clause of a bit group item' with a narrow but visible space, and page-392.md line 11 reads 'theVALU
- p676-1 · misplaced-content — Observation confirmed but not a content discrepancy. The printed page 676 (printed folio 654, Annex A) shows the 'Table 13, Procedural statements...' entry indented at the same level as the 13.16.61 entry above it; page-676.md line 47 carri
- p683-1 · misplaced-content — The printed page 683 (ISO 1989:2002 p.661) shows GR 14 as one paragraph wrapping 'only as' / 'part of the execution of an ACCEPT MESSAGE COUNT statement.' The markdown (page-683.md lines 5-7) holds every word in order; the blank line is a l

