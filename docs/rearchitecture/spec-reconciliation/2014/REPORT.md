# Spec reconciliation ledger, 2014 edition (generated)

> Merged from the per-agent files in `2014/` by `scripts/spec/merge_reconciliation.py --edition 2014 --write`.
> Each agent writes its own file before returning, so an interrupted run loses nothing.

**952 pages swept · 0 unread · 0 unswept · 74 confirmed · 60 refuted · 0 unverified**

## Confirmed — repair these

### p166-1 · p166 · ocr-error · **normative**

- **PDF:** 10^-999 (1.000 000 000 000 000 000 000 000 000 000 0E-999) through 10^1000 - 10^968 (9.999 999 999 999 999 999 999 999 999 999 9E+999)  [exponents -999, 1000 and 968 printed as superscripts]
- **Markdown:** 10–999 (1.000 000 000 000 000 000 000 000 000 000 0E-999)

through

   101000 – 10968 (9.999 999 999 999 999 999 999 999 999 999 9E+999)
- **Why:** Superscripts were flattened, so the range bounds read as the numbers '10–999', '101000' and '10968' instead of 10 to the power -999 and 10 to the power 1000 minus 10 to the power 968. The parenthesised E-notation still carries the right values, but the leading expression states a wrong magnitude.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 142 (PDF page 166), section 8.8.1.3.1.1, shows '10' followed by a superscript '-999', and '10' superscript '1000' minus '10' superscript '968'. The transcription page-166.md lines 15 and 19 read '10–999' and '101000 – 10968' as flat text, with no superscript markup or caret. Read literally, '101000 – 10968' is the integer 101000 minus 10968 = 90032, which is a wrong value for the upper bound of the normative range of a standard intermediate data item. The parenthesised E-notation that follows each bound is transcribed correctly and gives the intended values, so a careful reader can recover the meaning. The leading expression is still wrong as written, so this is a genuine content discrepancy and not formatting only. Both sources are quoted accurately.

### p192-1 · p192 · incorrect-text · **normative**

- **PDF:** FLOAT-NOT-A-NUMBER-
   SIGNALING   (one reserved word, FLOAT-NOT-A-NUMBER-SIGNALING, wrapped onto an indented continuation line)
- **Markdown:** | FLOAT-NOT-A-NUMBER- | LESS | PAGE |
| SIGNALING | LIMIT | PAGE-COUNTER |
- **Why:** The 8.9 reserved-word list shows the wrapped word FLOAT-NOT-A-NUMBER-SIGNALING as two table cells, 'FLOAT-NOT-A-NUMBER-' and 'SIGNALING'. That turns one reserved word into a malformed word (it ends in a hyphen) and makes SIGNALING look like a reserved word on its own. The PDF's indented continuation shows a single entry, and the class condition (8.8.4.1.3.1) prints FLOAT-NOT-A-NUMBER-SIGNALING as one word.
- **Repair:** —
- **Verifier:** Accurate. The markdown table for this reserved-word page splits the one reserved word FLOAT-NOT-A-NUMBER-SIGNALING into two rows, 'FLOAT-NOT-A-NUMBER-' and 'SIGNALING', because it drops the PDF's indented continuation. It should be a single entry, FLOAT-NOT-A-NUMBER-SIGNALING.

### p306-1 · p306 · incorrect-figure · **normative**

- **PDF:** { 1 / 01 } constant-name-1 CONSTANT ... -- both level numbers in the leading brace are underlined (a short underline is printed under the "1" as well as under "01")
- **Markdown:** ╭ 1  ╮
┤    ├ constant-name-1 <u>CONSTANT</u> ...
╰ <u>01</u> ╯
- **Why:** The markdown drops the underline from the level number 1. By §2014:5.2.1/5.2.2 an un-underlined word is optional, and a brace alternative that is optional can be implicitly selected (§2014:5.2.5.2), so the transcription reads as if the level number could be omitted entirely; the printed format requires 1 or 01 to be written.
- **Repair:** —
- **Verifier:** Rendered printed page 282 (PDF p306) at 300 dpi and cropped the leading brace of the 13.10.1 general format at 600 dpi: the brace stacks '1' over '01', and a short horizontal underline is clearly printed beneath the '1' (width of the single digit) as well as the longer underline beneath '01'. The markdown (page-306.md line 13) has '1' with no <u> markup while line 15 has '<u>01</u>'. The underline is the format's required-word marking, so dropping it from one brace alternative makes that alternative read as optional/implicitly selectable under the brace rule (§2014:5.2.5.2), whereas the printed format requires the level number 1 or 01 to be written. Not scaffolding, wrapping or formatting-only; the claim quotes both sources accurately.

### p370-1 · p370 · incorrect-text · **normative**

- **PDF:** OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1 / [ { ASCENDING | DESCENDING } KEY IS { data-name-2 } ... ] ... [ INDEXED BY { index-name-1 } ... ]  (the first line is plain text with no delimiters; the brace encloses only ASCENDING/DESCENDING, followed by KEY IS, all inside the bracket on the second line)
- **Markdown:**        ╭              ╮ KEY
       │              │
<u>OCCURS</u> │ integer-1 <u>TO</u> │ integer-2 TIMES <u>DEPENDING</u> ON data-name-1
       │              │
┌      │ <u>ASCENDING</u>    │                      ┐
│      ┤              ├ IS { data-name-2 } … │ … [ <u>INDEXED</u> BY { index-name-1 } … ]
└      ╰ <u>DESCENDING</u>   ╯                      ┘
- **Why:** The two lines of the printed format were merged: the ASCENDING/DESCENDING brace now spans both lines and encloses 'integer-1 TO' as a third alternative, the keyword KEY was moved to the top line beside the brace, and the brace opens outside the bracket that should contain it. Read literally, the figure makes 'integer-1 TO', ASCENDING and DESCENDING mutually exclusive alternatives and detaches KEY from IS, which misstates the syntax of the occurs-depending OCCURS clause.
- **Repair:** —
- **Verifier:** Rendered PDF page 370 (printed p.346), 13.18.38.1 Format 2: line 1 reads 'OCCURS integer-1 TO integer-2 TIMES DEPENDING ON data-name-1' with no delimiters (OCCURS, TO, DEPENDING underlined). Line 2 is a bracket enclosing a brace of stacked ASCENDING/DESCENDING (no choice bars) followed by 'KEY IS { data-name-2 } ...', then '... [ INDEXED BY { index-name-1 } ... ]'. The transcription (page-370.md lines 22-28) draws a brace spanning three rows with 'integer-1 TO' inside it as a third alternative, puts KEY on the top row outside the brace, and opens the bracket outside the brace. Under §2014:5.2.5.2 a brace of stacked alternatives means exactly one is chosen, so the markdown says 'integer-1 TO' excludes ASCENDING/DESCENDING and makes KEY a separate element apart from IS. That changes the syntax, so it is normative. The claim quotes both sources accurately. The same merge defect also appears in Format 4 on the same page (DYNAMIC [ CAPACITY ... inside the brace, KEY on the top row; md lines 40-46), which this claim does not cover. Format 1 is not affected.

### p370-2 · p370 · incorrect-text · **normative**

- **PDF:** OCCURS DYNAMIC [ CAPACITY IN data-name-3 ] [FROM integer-4 ] [ TO integer-5 ] [ INITIALIZED ] / [ { ASCENDING | DESCENDING } KEY IS { data-name-2 } ... ] ... [ INDEXED BY { index-name-1 } ... ]  (first line plain, no brace; the brace encloses only ASCENDING/DESCENDING on the second line)
- **Markdown:**        ╭            ╮ KEY
       │            │
<u>OCCURS</u> │ <u>DYNAMIC</u> [  │ <u>CAPACITY</u> IN data-name-3 ] [<u>FROM</u> integer-4 ] [ <u>TO</u> integer-5 ] [ <u>INITIALIZED</u> ]
       │            │
┌      │ <u>ASCENDING</u>  │                      ┐
│      ┤            ├ IS { data-name-2 } … │ … [ <u>INDEXED</u> BY { index-name-1 } … ]
└      ╰ <u>DESCENDING</u> ╯                      ┘
- **Why:** Same merge defect as Format 2: the ASCENDING/DESCENDING brace spans both lines, encloses 'DYNAMIC [' as an alternative (splitting the CAPACITY bracket across the brace boundary), KEY is moved to the top line, and the brace opens outside its bracket. The figure thereby makes DYNAMIC an alternative to ASCENDING/DESCENDING and leaves the [ CAPACITY ... ] bracket opened inside the brace and closed outside it.
- **Repair:** —
- **Verifier:** Rendered PDF page 370 (printed 346) at 300 dpi. Format 4 prints line 1 as plain text: 'OCCURS DYNAMIC [ CAPACITY IN data-name-3 ] [FROM integer-4 ] [ TO integer-5 ] [ INITIALIZED ]', with no brace. Line 2 is a bracket that encloses a brace around stacked ASCENDING/DESCENDING, followed by 'KEY IS { data-name-2 } ...', then the closing bracket, then '... [ INDEXED BY { index-name-1 } ... ]'. Format 1 on the same page is transcribed correctly in exactly that shape. In the transcription the brace runs over both lines and contains 'DYNAMIC [' as an alternative. KEY is detached to the top line, and the bracket's left edge sits outside the brace. Read under §2014:5.2.5.2, the transcribed figure makes DYNAMIC a brace alternative to ASCENDING/DESCENDING and splits the CAPACITY bracket. That changes the syntax, so it is a normative defect. The claim quotes both sources accurately.

### p491-1 · p491 · ocr-error · **normative**

- **PDF:** (operand-1<sub>1</sub> + operand-1<sub>2</sub> + ... + operand-1<sub>n</sub>)  [general rule 1; subscripts 1, 2, n printed as subscripts]
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n)
- **Why:** The subscripts are flattened onto the baseline, so the text reads 'operand-11' and 'operand-12' (apparently operands numbered eleven and twelve) instead of the first and second instances of operand-1. The surrounding prose ('the values of operand-1 are ...') shows what is meant, but the expression as written is wrong.
- **Repair:** —
- **Verifier:** On page 491 (§2014:14.9.2.3 GR1 and GR2 of the ADD statement), the markdown flattens the printed subscripts 1, 2, n on operand-1 to the baseline in both arithmetic expressions: GR1 '(operand-11 + operand-12 + ... + operand-1n)' and GR2 '(operand-11 + operand-12 + ... + operand-1n + operand-2)' should read operand-1 subscript 1, subscript 2, ..., subscript n. Meaning is recoverable from the surrounding prose, so the severity is closer to editorial than normative.

### p491-2 · p491 · ocr-error · **normative**

- **PDF:** (operand-1<sub>1</sub> + operand-1<sub>2</sub> + ... + operand-1<sub>n</sub> + operand-2)  [general rule 2; subscripts printed as subscripts]
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n + operand-2)
- **Why:** Same subscript flattening as in general rule 1: 'operand-11' and 'operand-12' read as different operand names instead of operand-1 subscript 1 and subscript 2.
- **Repair:** —
- **Verifier:** Rendered PDF page 491 (printed page 467), 14.9.2.3 ADD statement general rule 2, shows the expression '(operand-1 + operand-1 + ... + operand-1 + operand-2)' with the 1, 2 and n set as typographic subscripts after each 'operand-1'. page-491.md line 29 reads '(operand-11 + operand-12 + ... + operand-1n + operand-2)': the subscripts are flattened inline, so the transcription names the metavariables operand-11, operand-12 and operand-1n instead of the 1st, 2nd and nth instance of operand-1. Both sources are quoted accurately. Formatting alone does not explain this: the subscript carries the meaning, and the flattened text reads as different operand names. The following sentence ('the values of operand-1 are ...') lets a careful reader recover the intent, but the transcribed expression itself is wrong. The same defect occurs in general rule 1 (md line 17), which the claim notes.

### p559-1 · p559 · table-restructured · **normative**

- **PDF:** Table 18, row HIGH-VALUE, HIGH-VALUES; LOW-VALUE, LOW-VALUES; and QUOTE, QUOTES: receiving-operand column lists nine lines 'Alphabetic / Alphanumeric / Alphanumeric-edited / National / National-edited / Numeric / Numeric-edited / — if usage is display / — if usage is national' against category column 'Alphanumeric / Alphanumeric / Alphanumeric / National / National / Alphanumeric / (blank) / Alphanumeric / National' - Numeric-edited has no category of its own; '— if usage is display' maps to Alphanumeric and '— if usage is national' maps to National.
- **Markdown:** | HIGH-VALUE, HIGH-VALUES; LOW-VALUE, LOW-VALUES; and QUOTE, QUOTES | Alphabetic<br>Alphanumeric<br>Alphanumeric-edited<br>National<br>National-edited<br>Numeric<br>Numeric-edited<br>— if usage is display — if usage is national | Alphanumeric<br>Alphanumeric<br>Alphanumeric<br>National<br>National<br>Alphanumeric<br>Alphanumeric<br>National |
- **Why:** The two numeric-edited sub-lines are merged onto one line and the blank category cell is dropped, so the line-by-line pairing now reads Numeric-edited -> Alphanumeric and 'if usage is display / if usage is national' -> National, losing that a numeric-edited receiver of usage display gets an alphanumeric figurative constant and one of usage national gets a national one.
- **Repair:** —
- **Verifier:** Rendered page 559 (printed 535), Table 18: the HIGH-VALUE/LOW-VALUE/QUOTE row shows nine receiving-operand lines, with 'Numeric-edited' followed by indented sub-lines '— if usage is display' and '— if usage is national'; the category column has a blank line opposite 'Numeric-edited', then 'Alphanumeric' and 'National' opposite the two sub-lines. page-559.md line 9 joins the two sub-lines into one <br> segment (8 segments) and drops the blank category cell (8 segments), so the positional pairing becomes Numeric-edited -> Alphanumeric and the merged usage line -> National. The national-usage numeric-edited case loses its own pairing, which changes the meaning of the table. Claim quotes both sources accurately.

### p625-1 · p625 · ocr-error · **normative**

- **PDF:** (operand-1₁ + operand-1₂ + ... + operand-1ₙ) ... (operand-2 – (operand-1₁ + operand-1₂ + ... + operand-1ₙ))
- **Markdown:** (operand-11 + operand-12 + ... + operand-1n) ... (operand-2 – (operand-11 + operand-12 + ... + operand-1n))
- **Why:** In 14.9.40.3 SUBTRACT general rules 1 and 2 the subscripts 1, 2, n on operand-1 are flattened into the operand name, so 'operand-11' and 'operand-12' read as operands eleven and twelve instead of the first and second occurrences of operand-1; the arithmetic-expression equivalence that defines the initial evaluation is garbled.
- **Repair:** —
- **Verifier:** Printed page 601 (PDF p625), 14.9.40.3 GR1 and GR2: the expressions are printed '(operand-1 sub1 + operand-1 sub2 + ... + operand-1 sub n)' and '(operand-2 – (operand-1 sub1 + operand-1 sub2 + ... + operand-1 sub n))' with 1, 2, n as visibly lowered subscripts. page-625.md lines 27 and 39 read 'operand-11 + operand-12 + ... + operand-1n', the subscripts flattened into the name. The claim quotes both sources accurately. The flattening makes the text name nonexistent operands 'operand-11'/'operand-12' instead of the 1st/2nd occurrences of operand-1, a genuine content loss in the rule text. The surrounding prose ('the values of operand-1 are the values of literal-1 and ... identifier-1 in the order ... specified') lets a careful reader recover the meaning, so the practical impact is limited, but the transcribed expression itself is wrong.

### p718-1 · p718 · ocr-error · **normative**

- **PDF:** ((argument-1₁ + argument-1₂) / 2)  [1 and 2 printed as subscripts]
- **Markdown:** ((argument-11 + argument-12) / 2)
- **Why:** The subscripts that index the occurrences of argument-1 were flattened into the name, so the equivalent arithmetic expression for MEAN now reads as operands named argument-11 and argument-12.
- **Repair:** —
- **Verifier:** §2014:15.56.3 1b and 1c: the PDF prints argument-1 with occurrence subscripts (₁, ₂, ₙ). The markdown flattens them into the name: 'argument-11 + argument-12' in item b (line 29), and 'argument-11 + argument-12 +... + argument-1n' in item c (line 33). The claim names only item b, but item c has the same defect.

### p718-2 · p718 · ocr-error · **normative**

- **PDF:** ((argument-1₁ + argument-1₂ +... + argument-1ₙ) / n)  [1, 2 and n printed as subscripts]
- **Markdown:** ((argument-11 + argument-12 +... + argument-1n) / n)
- **Why:** The subscripts 1, 2 and n were flattened into the operand names, so the general MEAN expression reads as argument-11, argument-12 and argument-1n instead of the 1st, 2nd and nth occurrences of argument-1.
- **Repair:** —
- **Verifier:** Rendered PDF page 718 (printed page 694, §2014:15.56.3 Returned values, rule 1c) shows '((argument-1' followed by a lowered, smaller-font subscript 1, '+ argument-1' with subscript 2, '+... + argument-1' with subscript n, ') / n)'. page-718.md line 33 reads '((argument-11 + argument-12 +... + argument-1n) / n)' with no subscript markup, so the occurrence indices are fused into the operand name and the text literally names argument-11, argument-12 and argument-1n, which are not the formats' operands. Both sources are quoted correctly by the claim. This is a loss of notation in normative returned-value text, not scaffolding or wrapping. Sibling: line 29 (rule 1b, '((argument-11 + argument-12) / 2)') has the same flattening and is not covered by this claim.

### p746-1 · p746 · ocr-error · **normative**

- **PDF:** (argument-1₁ + argument-1₂) ... (argument-1₁ + argument-1₂ + ... + argument-1ₙ)
- **Markdown:** (argument-11 + argument-12) ... (argument-11 + argument-12 + ... + argument-1n)
- **Why:** In 15.81.3 SUM returned values 1b and 1c the subscripts 1, 2 and n on argument-1 are flattened into the operand name, so 'argument-11' and 'argument-12' read as arguments eleven and twelve instead of the first and second occurrences of argument-1; the equivalent arithmetic expression that defines SUM is garbled.
- **Repair:** —
- **Verifier:** Printed page 746 (printed folio 722), 15.81.3 Returned values 1b and 1c, shows 'argument-1' followed by subscript glyphs 1, 2 and n set lower and smaller: (argument-1_1 + argument-1_2) and (argument-1_1 + argument-1_2 + ... + argument-1_n). page-746.md lines 34 and 38 read '(argument-11 + argument-12)' and '(argument-11 + argument-12 + ... + argument-1n)', with no subscript markup. The subscripts are fused into the operand name, so the text names operands 'argument-11' and 'argument-12' instead of occurrences 1 and 2 of argument-1. That is a content loss in the expression that defines SUM's result, not reflow or formatting. The intended meaning can still be recovered from the lead-ins ('For two occurrences of argument-1'), so the practical impact is small, but the transcribed expression is wrong as written. The claim quotes both sources accurately.

### p756-1 · p756 · ocr-error · **normative**

- **PDF:** (((argument-1₁ – FUNCTION MEAN (argument-list)) ** 2 + (argument-1₂– FUNCTION MEAN (argument-list)) ** 2) / 2) ... ((argument-1ₙ – FUNCTION MEAN (argument-list)) ** 2)) / n) where argument-list is the argument-1 list for the VARIANCE function itself and argument-1ᵢ is the ith argument
- **Markdown:** (((argument-11 – FUNCTION MEAN (argument-list)) \*\* 2 +\ (argument-12– FUNCTION MEAN (argument-list)) \*\* 2) / 2) ... ((argument-1n – FUNCTION MEAN (argument-list)) \*\* 2)) / n) where argument-list is the argument-1 list for the VARIANCE function itself and argument-1i is the ith argument
- **Why:** In 15.91.3 VARIANCE returned values 1b and 1c, and in the closing 'where' sentence, the subscripts 1, 2, n and i on argument-1 are flattened into the name ('argument-11', 'argument-12', 'argument-1n', 'argument-1i'), so the definition of argument-1-sub-i and the equivalent arithmetic expression for VARIANCE read as references to differently named arguments.
- **Repair:** —
- **Verifier:** §2014:15.91.3 VARIANCE returned values: subscripts on argument-1 (sub 1 and 2 in 1b; sub 1 and n in 1c; sub i in the 'where' sentence) are flattened to inline text in markdown lines 29, 30, 36, 40 and 42 ('argument-11', 'argument-12', 'argument-1n', 'argument-1i'). They should be argument-1<sub>1</sub> and so on.

### p49-1 · p49 · misplaced-content · **structural**

- **PDF:** Expression | 8.8, Expressions | arithmetic-expression-n boolean-expression-n conditional-expression-n  ...  c) Exception name | 14.6.12.1.5, Exception-names and exception conditions | exception-name-1
- **Markdown:** | Expression | 8.8, Expressions | arithmetic-expression-n |

boolean-expression-n conditional-expression-n

| Integer | ...
| c) Exception name | 14.6.12.1.5, Exception-names and | exception-name-1 |

exception conditions
- **Why:** The operand-type table is broken in two places: the terms boolean-expression-n and conditional-expression-n are ejected from the Expression row into a stray paragraph, and the tail of the cross-reference 'exception conditions' is ejected from the Exception name row. The user-defined-word row is also split across three table rows ('qualification and subscription if' / 'needed'), each looking like a separate operand type.
- **Repair:** —
- **Verifier:** Printed page 49 (2014 p.25) shows the operand-type table with the Expression row's Term cell holding three stacked terms (arithmetic-expression-n, boolean-expression-n, conditional-expression-n), the user-defined-word row as ONE row whose Described-in cell lists 8.3.1.1.1, 8.4.1.1 and 8.4.1.2 and whose Term cell reads 'Any of the types listed in 8.3.1.1.1 suffixed by -n', and the Exception name row's Described-in cell reading '14.6.12.1.5, Exception-names and exception conditions'. The markdown closes the table after arithmetic-expression-n and puts 'boolean-expression-n conditional-expression-n' in a free paragraph, so the table drops two of the Expression terms; it splits the user-defined-word row into three rows ('qualification and subscription if' with 8.4.1.1 and 'suffixed by -n'; 'needed' with 8.4.1.2), which reads as separate operand types; and it puts 'exception conditions' in a paragraph after the table. The words are all present, but their cell membership is lost, and that changes what the table says about which terms belong to which operand type. The claim quotes both sources accurately. This is more than line wrapping: it is a genuine structural defect, not a normative one.

### p51-1 · p51 · ocr-error · **structural**

- **PDF:** When an operand is textually subscripted, as operand-j_n (n printed as a subscript), the term 'operand-j' identifies a specific operand and 'n' refers to the nth position or occurrence of operand-j.
- **Markdown:** When an operand is textually subscripted, as operand-jn, the term 'operand-j' identifies a specific operand and 'n' refers to the nth position or occurrence of operand-j.
- **Why:** The clause defines the textual-subscript notation itself, and the subscript n was flattened onto the baseline, so the example reads as a term named 'operand-jn' and no longer shows the notation it defines.
- **Repair:** —
- **Verifier:** Rendered page 51 (printed page 27, §2014:5.4.1) at 300 dpi: the example is printed as 'operand-j' followed by a lowered, smaller 'n' (a true subscript). page-51.md line 9 reads 'as operand-jn' on the baseline. Both sources are quoted accurately. This is not mere styling: §2014:5.4.1 exists to define the subscript notation, and the subscript is the only thing that distinguishes 'operand-j sub n' from a term literally named 'operand-jn'. The surrounding prose still names 'operand-j' and 'n' separately, so a careful reader can recover the intent, which keeps the severity structural rather than normative; the defect is a lost notation mark, not lost rule text.

### p55-1 · p55 · misplaced-content · **structural**

- **PDF:** Figure 1: Margin L at the left of position 1; Margin C between positions 6 and 7; Margin A between positions 7 and 8; Margin R after the last position (...). Positions 1-6 braced as 'Sequence Number Area'; position 7 arrowed as 'Indicator Area'; positions 8 through ... braced as 'Program-text Area'.
- **Markdown:** | Margin |  |  |  |  | Margin |  | Margin |  | Margin |
|---|---|---|---|---|---|---|---|---|---|
| L |  |  |  |  | C |  | A |  | R |
| 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 10 11 12 13 | ... |

Program-text Area Sequence Number Area

Indicator Area
- **Why:** Figure 1 is garbled: the table puts Margin C over position 6 and Margin A over position 8 rather than on the boundaries, positions 9-13 are merged into one cell, Margin R sits over '...', and the three area labels are detached from their spans and listed out of order, so the figure no longer shows which positions form which area. The prose below the figure restates the margins, so it does not lose the normative content.
- **Repair:** —
- **Verifier:** Rendered PDF page 55 (printed page 31) at 300 dpi shows Figure 1 as a ruler of cells 1..13 and '...': Margin L bar at the left of cell 1, Margin C bar on the boundary between 6 and 7, Margin A bar on the boundary between 7 and 8, Margin R bar at the right of '...'; a brace under 1-6 labelled 'Sequence Number Area', a downward arrow to 7 labelled 'Indicator Area', and a brace under 8 through '...' labelled 'Program-text Area'. The markdown table puts 'Margin C' in the column of cell 6 and 'Margin A' in the column of cell 8 (on cells, not boundaries), merges 9-13 into one cell, and emits the area labels as detached text in the wrong order ('Program-text Area Sequence Number Area' then 'Indicator Area') with no span association. The claim quotes both sources accurately. The figure's content (which positions form which area, where the margins fall) is lost from the transcription of the figure itself; the normative prose immediately below restates all of it correctly, so the defect is structural, not normative.

### p60-2 · p60 · misplaced-content · **structural**

- **PDF:** COPY statement   Text manipulation
      SUPPRESS phrase   Implementor-defined
REPLACE statement   Text manipulation
- **Markdown:** | COPY statement | Text manipulation |
|---|---|
| SUPPRESS phrase | Implementor-defined |
| REPLACE statement | Text manipulation |
- **Why:** In the PDF 'SUPPRESS phrase' is indented under COPY statement as the COPY statement's SUPPRESS phrase; the markdown puts it at the same level as COPY and REPLACE, so it reads as a separate compiler directing statement.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 36 (PDF p60, §2014:7.1) shows 'SUPPRESS phrase' clearly indented (about 0.3 in) under 'COPY statement' in the compiler-directing-statements table, between COPY statement and REPLACE statement. That nesting marks it as a sub-entry of COPY: the COPY statement's SUPPRESS phrase is implementor-defined in stage while COPY itself is text manipulation. The markdown table has COPY statement, SUPPRESS phrase and REPLACE statement as sibling rows, so the nesting is lost. Also, COPY statement is wrongly made the table's header row, and the 'Compiler directives | Stage' heading becomes a data row. All the words and stage values are present and correctly paired, so this is a structural loss, not a normative one. The word 'phrase' still hints that it belongs to a statement. The claim quotes both sources accurately.

### p95-1 · p95 · table-restructured · **structural**

- **PDF:** Basic special characters | [blank glyph: the space character] + – * / = $ , ; . " ' ( ) > < & : _ | space plus sign minus sign (hyphen) asterisk slant (slash, solidus) equal sign currency sign comma semicolon period quotation mark apostrophe left parenthesis right parenthesis greater than less than ampersand colon underscore  (19 rows, the first character being the space, printed as an empty line aligned with 'space')
- **Markdown:** | Basic special characters | +<br>–<br>\*<br>/<br>=<br>$<br>,<br>;<br>.<br>"<br>'<br>(<br>)<br>><br>&lt;<br>&<br>:<br>_ | space<br>plus sign<br>minus sign (hyphen)<br>asterisk<br>slant (slash, solidus)<br>equal sign<br>currency sign<br>comma<br>semicolon<br>period<br>quotation mark<br>apostrophe<br>left parenthesis<br>right parenthesis<br>greater than<br>less than<br>ampersand<br>colon<br>underscore |
- **Why:** The Character column has 18 entries but the Meaning column 19: the leading empty line that stands for the space character was dropped. Read line by line, every character is now paired with the meaning of the character before it ('+' with 'space', '-' with 'plus sign', ... '_' with 'colon'), and 'underscore' has no character. The space must be restored as a leading (empty or '&nbsp;') entry so the 19 rows align as printed.
- **Repair:** —
- **Verifier:** Rendered page 95 at 300 dpi (Table 2 continued, printed page 71). In the Character column the first line is blank, level with 'space' in the Meaning column; '+' is on the second line, level with 'plus sign', and '_' is on the 19th, level with 'underscore'. page-95.md line 7 has 18 <br>-separated characters (from + to _) against 19 meanings, with no leading empty entry. Read row by row, every glyph is offset by one: '+' lines up with 'space' and '_' with 'colon', and 'underscore' has no glyph. That changes what the table says each character means, so this is a real content defect. The claim quotes both sources accurately.

### p107-1 · p107 · misplaced-content · **structural**

- **PDF:** =    Relational operator - equal
       and assignment operator in COMPUTE
==   Pseudo-text delimiter in COPY and REPLACE statements
- **Markdown:** | = | Relational operator - equal |

and assignment operator in COMPUTE

| == | Pseudo-text delimiter in COPY and REPLACE statements |
|---|---|
- **Why:** The '=' row of the special character words table loses its second meaning ('and assignment operator in COMPUTE'), which is detached as a loose paragraph that splits the table in two; the second table fragment then starts with '==' as its header row.
- **Repair:** —
- **Verifier:** Printed page 107 (ISO/IEC 1989:2014 p.83, 8.3.1.1.3.1) shows one continuous Word/Meaning table; the '=' entry's meaning wraps onto an indented continuation line 'and assignment operator in COMPUTE' within the Meaning column, immediately followed by the '==' row. The markdown ends the table after '| = | Relational operator - equal |', emits the continuation as a standalone paragraph, then opens a second table whose HEADER row is '| == | Pseudo-text delimiter ... |' followed by '|---|---|'. Both quotes in the claim match the sources exactly. The words are all present, but the structure changes meaning: the COMPUTE-assignment meaning is no longer attached to '=' (it reads as free text), and '==' becomes a column header rather than a special-character-word entry, so any reader or tool consuming the table loses the '=' assignment meaning and the '==' word. This is a genuine structural transcription defect, not mere wrapping.

### p140-1 · p140 · missing-heading · **structural**

- **PDF:** 8.4.5.1.1    Scope of condition-names, constant-names, data-names, file-names, record-names, report-names, screen-names, and type-names
- **Markdown:** ###### 8.4.5.1.1 Scope of condition-names, constant-names, data-names, file-names, record-names,

report-names, screen-names, and type-names
- **Why:** A reader or tool keying on the heading text sees a truncated title, and the stray continuation line reads as body prose of the clause.
- **Repair:** —
- **Verifier:** Rendered PDF page 140 (printed p.116) shows the bold heading '8.4.5.1.1 Scope of condition-names, constant-names, data-names, file-names, record-names, report-names, screen-names, and type-names' wrapped over two lines, both in the same bold heading face. page-140.md lines 44-46 hold '###### 8.4.5.1.1 Scope of ... record-names,' as the heading, then a blank line, then 'report-names, screen-names, and type-names' as a separate plain paragraph. No words are lost, but the heading's own text is truncated and its tail is demoted to body prose. That is a genuine structural defect in the heading, beyond ordinary line wrapping inside a paragraph. It does not change normative meaning, so the severity stays structural.

### p179-1 · p179 · missing-heading · **structural**

- **PDF:** 8.8.4.1.1.4 Comparison of a numeric integer operand with an operand of class alphanumeric or national
- **Markdown:** ###### 8.8.4.1.1.4 Comparison of a numeric integer operand with an operand of class alphanumeric or

national
- **Why:** A reader or tool indexing headings sees a truncated section title for 8.8.4.1.1.4 (dropping the national case) and a stray one-word paragraph in its body.
- **Repair:** —
- **Verifier:** Split heading, not a missing one: the 8.8.4.1.1.4 heading is wrapped across two markdown blocks. The heading line ends at 'or', and 'national' is a separate paragraph. The heading should be one line ending in 'alphanumeric or national'.

### p188-1 · p188 · table-restructured · **structural**

- **PDF:** Logical Operator | Meaning ; AND | Logical conjunction; the truth value is true if both of the conjoined conditions are true; false if one or both of the conjoined conditions is false. ; OR | Logical inclusive OR; the truth value is true if one or both of the included conditions is true; false if both included conditions are false. ; NOT | Logical negation or reversal of truth value; the truth value is true if the condition is false; false if the condition is true.
- **Markdown:** | AND | Logical conjunction; the truth value is true if both of the conjoined conditions are |

true; false if one or both of the conjoined conditions is false.

   OR Logical inclusive OR; the truth value is true if one or both of the included conditions is true; false if both included conditions are false.

   NOT Logical negation or reversal of truth value; the truth value is true if the condition is false; false if the condition is true.
- **Why:** The 8.8.4.2 logical-operator table is broken: only the AND row is a table row, and its Meaning cell is cut mid-sentence ('are |'), with the rest of the sentence a loose paragraph. The OR and NOT rows fall out of the table as indented prose, so a reader or a tool parsing the table sees one operator with a truncated definition.
- **Repair:** —
- **Verifier:** Printed PDF page 188 (folio 164), 8.8.4.2: a two-column table 'Logical Operator | Meaning' with three rows AND, OR, NOT, each Meaning cell wrapped over two lines. The markdown page-188.md has the header row and an AND row whose cell ends at '...conjoined conditions are |'; the continuation 'true; false if one or both of the conjoined conditions is false.' is a separate paragraph outside the table, and the OR and NOT rows are indented prose lines, not table rows. Both quotes in the claim match the sources. Every word of the PDF text is still in the markdown, so no normative wording is lost. The table structure is genuinely broken, though: the AND cell is truncated, and OR/NOT are not table rows. That is a structural defect, as the claim says.

### p195-1 · p195 · misplaced-content · **structural**

- **PDF:** ONLY    Object-view, SHARING clause, SHARING phrase, and USAGE clause
- **Markdown:** | ONLY | Object-view, SHARING clause, SHARING phrase, and |

USAGE clause
- **Why:** The wrapped 'USAGE clause' was detached from the ONLY row into a stray paragraph, so the table row for ONLY omits the USAGE clause context (USAGE ... OBJECT REFERENCE ... ONLY) and the table is split.
- **Repair:** —
- **Verifier:** The ONLY row's wrapped continuation 'USAGE clause' sits outside the table as a stray paragraph. The table is split there, and the second fragment uses the PARAGRAPH / EXIT statement row as its header row, so PARAGRAPH is no longer a data row. Fix: '| ONLY | Object-view, SHARING clause, SHARING phrase, and USAGE clause |' as one continuous table with PARAGRAPH as a data row.

### p231-1 · p231 · table-restructured · **structural**

- **PDF:** Term Subclause / data-division 13, Data division / environment-division 12, Environment division / options-paragraph 11.9, OPTIONS paragraph / procedure-division 14, Procedure division
- **Markdown:** | data-division | 13, Data division |

environment-division 12, Environment division

| options-paragraph | 11.9, OPTIONS paragraph |
|---|---|
| procedure-division | 14, Procedure division |
- **Why:** The one four-row meta-language term table of 10.6.1 is split into two tables with the environment-division row left as a loose paragraph outside any table, and the second fragment turns options-paragraph into a header row; the content is all present but a table-reading tool would miss the environment-division mapping.
- **Repair:** —
- **Verifier:** The printed page (PDF p231, printed page 207, end of 10.6.1) shows one continuous Term/Subclause table with four rows: data-division 13; environment-division 12; options-paragraph 11.9; procedure-division 14. The rendered page-231.md has a table with header Term|Subclause and the single data-division row, then environment-division 12, Environment division as a loose paragraph, then a second table whose header row is options-paragraph|11.9 and whose only body row is procedure-division. The claim quotes both sources accurately. All the text is present and a human reader gets the same meaning, so this is not normative. It is a real structural defect, though: the table is split, one mapping sits outside any table, and a data row has become a header. A tool that reads the tables would lose or mislabel the environment-division and options-paragraph mappings. Severity structural is correct.

### p294-1 · p294 · table-restructured · **structural**

- **PDF:** Term Subclause / constant-entry 13.10, Constant entry / record-description-entry 13.11, Record description entry / type-declaration-entry 13.12, Type declaration entry
- **Markdown:** | Term | Subclause |
|---|---|
| constant-entry | 13.10, Constant entry |

record-description-entry 13.11, Record description entry\
type-declaration-entry 13.12, Type declaration entry
- **Why:** The single meta-language term table of 13.4.1 is cut short: the record-description-entry and type-declaration-entry rows fall out of the table into a loose paragraph joined by a hard line break. All text is present, but a table-reading tool would miss those two term-to-subclause mappings.
- **Repair:** —
- **Verifier:** The printed page 294 (page 270 in the PDF's own numbering), 13.4.1, shows one two-column term table: the headings Term and Subclause, then three aligned rows: constant-entry 13.10, record-description-entry 13.11 and type-declaration-entry 13.12. In page-294.md the markdown table holds only the header and the constant-entry row. The other two rows come after a blank line as a plain paragraph joined by a backslash hard break. The claim quotes both sources accurately. All the words and numbers are present, so no normative content changes. The defect is purely structural: the table loses two of its three rows. The severity 'structural' is correct.

### p299-1 · p299 · table-restructured · **structural**

- **PDF:** Term Subclause / 77-level-entry 13.13, 77-level data description entry / constant-entry 13.10, Constant entry / record-description-entry 13.11, Record description entry / type-declaration-entry 13.12, Type declaration entry
- **Markdown:** | Term | Subclause |
|---|---|
| 77-level-entry | 13.13, 77-level data description entry |
| constant-entry | 13.10, Constant entry |

record-description-entry 13.11, Record description entry\
type-declaration-entry 13.12, Type declaration entry
- **Why:** The single meta-language term table of 13.5.1 is cut short: the record-description-entry and type-declaration-entry rows fall out of the table into a loose paragraph joined by a hard line break. All text is present, but a table-reading tool would miss those two term-to-subclause mappings.
- **Repair:** —
- **Verifier:** The printed page 299 (PDF page label 275), 13.5.1, shows one uninterrupted Term/Subclause list of four rows: 77-level-entry 13.13; constant-entry 13.10; record-description-entry 13.11; type-declaration-entry 13.12. The markdown page-299.md renders only the first two rows as a pipe table, then ends the table and puts the last two rows in a plain paragraph joined by a backslash hard break. Both sources are quoted correctly. All words survive, so the normative wording is unchanged, but the one table is split into a table plus loose prose, and a table reader would lose two term-to-subclause mappings. The claim's description and its structural severity are accurate.

### p300-1 · p300 · table-restructured · **structural**

- **PDF:** Term Subclause / 77-level-entry 13.13, 77-level data description entry / constant-entry 13.10, Constant entry / record-description-entry 13.11, Record description entry / type-declaration-entry 13.12, Type declaration entry
- **Markdown:** | Term | Subclause |
|---|---|
| 77-level-entry | 13.13, 77-level data description entry |
| constant-entry | 13.10, Constant entry |

record-description-entry 13.11, Record description entry\
type-declaration-entry 13.12, Type declaration entry
- **Why:** The single meta-language term table of 13.6.1 is cut short: the record-description-entry and type-declaration-entry rows fall out of the table into a loose paragraph joined by a hard line break. All text is present, but a table-reading tool would miss those two term-to-subclause mappings.
- **Repair:** —
- **Verifier:** Rendered PDF page 300 (printed 276, 2014 13.6.1) shows ONE Term/Subclause table with four aligned rows: 77-level-entry 13.13; constant-entry 13.10; record-description-entry 13.11; type-declaration-entry 13.12. The markdown page-300.md holds a pipe table with only the first two rows, then a blank line and the last two rows as a plain paragraph joined by a backslash hard break. Both quotes match their sources exactly. No text is lost and the term-to-subclause pairings remain readable to a human, so it is not normative; but the table itself is split, which is a genuine structural transcription defect (a table parser sees two mappings, not four), not mere line wrapping or furniture. Severity structural is correct.

### p301-1 · p301 · table-restructured · **structural**

- **PDF:** Term / Subclause table with four rows: 77-level-entry 13.13, 77-level data description entry; constant-entry 13.10, Constant entry; record-description-entry 13.11, Record description entry; type-declaration-entry 13.12, Type declaration entry
- **Markdown:** | 77-level-entry | 13.13, 77-level data description entry |
| constant-entry | 13.10, Constant entry |

record-description-entry 13.11, Record description entry\
type-declaration-entry 13.12, Type declaration entry
- **Why:** The last two rows of the term table fell out of the markdown table into a loose paragraph, so a reader or tool parsing the table sees only two of the four terms. Wording is intact.
- **Repair:** —
- **Verifier:** Printed page 277 (PDF p301), 13.7.1: the Term/Subclause table has four aligned rows in one block (77-level-entry, constant-entry, record-description-entry, type-declaration-entry), with no break between rows 2 and 3. In page-301.md, only the first two rows are inside the pipe table; record-description-entry and type-declaration-entry follow a blank line as a loose paragraph joined by a backslash line break. Both sources are quoted accurately. The wording of every row is intact, so this does not change normative meaning. It is a genuine structural defect, though, not just wrapping: anything that parses the markdown table, whether a tool or a reader, gets two of the four term-to-subclause mappings. The severity is correctly 'structural', not 'normative'. The diagram above it (a bracket around four stacked alternatives, then an ellipsis) matches the PDF apart from the box-drawing rendering, and it is not part of this claim.

### p303-1 · p303 · table-restructured · **structural**

- **PDF:** Term / Subclause table with two rows: 77-level-entry 13.13, 77-level data description entry; report-group-description-entry 13.8.4, Report group description entry
- **Markdown:** | 77-level-entry | 13.13, 77-level data description entry |

report-group-description-entry 13.8.4, Report group description entry
- **Why:** The second row of the term table fell out of the markdown table into a loose paragraph; a table parser sees one term instead of two. Wording is intact.
- **Repair:** —
- **Verifier:** Printed page 303 (2014 p.279), 13.8.1: the Term/Subclause table has two aligned rows, '77-level-entry | 13.13, 77-level data description entry' and 'report-group-description-entry | 13.8.4, Report group description entry'. In page-303.md the markdown table holds only the 77-level-entry row; the report-group-description-entry row follows a blank line as a loose paragraph outside the table, with no column separator. Both sources are quoted correctly. The wording is intact, so this is not a normative defect. It is a real structural defect, though, because the table now has one row where the standard prints two, and anything that reads the table as structured data loses the second term-to-subclause mapping. It is not page furniture or line wrapping. Side observation outside this claim: the printed table lists 77-level-entry even though the 13.8.1 format uses constant-entry, not 77-level-entry; the markdown copies the printed text faithfully, so this is not a transcription difference.

### p305-1 · p305 · table-restructured · **structural**

- **PDF:** Term / Subclause table with two rows: constant-entry 13.10, Constant entry; screen-description-entry 13.17, Screen description entry
- **Markdown:** | constant-entry | 13.10, Constant entry |

screen-description-entry 13.17, Screen description entry
- **Why:** The second row of the term table fell out of the markdown table into a loose paragraph; a table parser sees one term instead of two. Wording is intact.
- **Repair:** —
- **Verifier:** The 300 dpi render of printed page 281 (PDF p305), 13.9.1, shows a Term/Subclause table with two rows set in the same aligned columns: 'constant-entry | 13.10, Constant entry' and 'screen-description-entry | 13.17, Screen description entry'. In page-305.md, lines 17-19 are a markdown table with only the constant-entry row. Line 20 is blank, which ends the table, and line 21 holds 'screen-description-entry 13.17, Screen description entry' as a loose paragraph with no cell separators. Both sources are quoted accurately. The wording is intact, so the meaning a person reads does not change. The structure is genuinely wrong, though: anything that parses the table finds one meta-language term where the standard defines two. This is a real structural defect, not a normative one.

### p461-1 · p461 · missing-heading · **structural**

- **PDF:** 14.6.8.4 Receiving data items of categories alphabetic, alphanumeric, alphanumeric-edited, national, and national edited
- **Markdown:** ###### 14.6.8.4 Receiving data items of categories alphabetic, alphanumeric, alphanumeric-edited, national,

and national edited
- **Why:** The heading for 14.6.8.4 is truncated after 'national,' so a reader or heading index loses the national-edited category, and 'and national edited' appears as a stray body paragraph.
- **Repair:** —
- **Verifier:** The rendered PDF page (printed page 437) sets 14.6.8.4 as one bold heading that wraps onto a second line ending 'and national edited'. page-461.md lines 7-9 put '###### 14.6.8.4 ... national,' and then, after a blank line, 'and national edited' as a separate plain paragraph. The words are all present, but the markdown heading element is cut short and its tail becomes a stray body paragraph. This goes beyond line wrapping because it changes the document structure that heading indexes and section lookups read. Both sources are quoted accurately. The defect is structural only: no normative rule text changes.

### p486-1 · p486 · table-restructured · **structural**

- **PDF:** TIME table (general rule 11): rows '1-2 Two numeric characters of the hours past midnight in the range 00 through 23.', '3-4 ...', '5-6 Two numeric characters of the seconds past the minute in the range: a) 00 through 59 when a LEAP-SECOND directive with the OFF phrase is in effect b) 00 through nn, where nn is defined by the implementor, when a LEAP-SECOND directive with the ON phrase is in effect.', '7-8 Two numeric characters of the hundredths of a second past the second in the range 00 through 99. 00 is returned if the system on which the ACCEPT statement is executed does not have the facility to provide the fractional part of a second.' - a) and b) are inside the Contents cell of row 5-6, and 7-8 is a fourth row of the same table.
- **Markdown:** | 5-6 | Two numeric characters of the seconds past the minute in the range: |

a) 00 through 59 when a LEAP-SECOND directive with the OFF phrase is in effect

b) 00 through nn, where nn is defined by the implementor, when a LEAP-SECOND directive with the ON phrase is in effect.

   7-8 Two numeric characters of the hundredths of a second past the second in the range\
- **Why:** The table ends after the 5-6 row. Sub-items a) and b) are left at the top level of the page, outside both the 5-6 cell and general rule 11, and the 7-8 row turns into an indented paragraph outside the table. The text is all present, but the table structure that ties the LEAP-SECOND ranges to positions 5-6 is lost.
- **Repair:** —
- **Verifier:** Printed page 462 (PDF p486), GR 11 TIME table: a) and b) are visibly indented inside the Contents column of row 5-6, and 7-8 is a fourth row aligned with 1-2/3-4/5-6. The markdown table stops at the 5-6 row; a) and b) appear as unindented top-level paragraphs and 7-8 is an indented free paragraph with backslash line breaks. All words are present and quoted accurately, so this is not normative, but the table structure is genuinely broken: the 7-8 row is no longer a table row and the LEAP-SECOND ranges are detached from positions 5-6. Claim as described is accurate; structural severity is right.

### p487-1 · p487 · table-restructured · **structural**

- **PDF:** One table 'Screen item | Figurative constant' with eight rows: Alphabetic / Alphanumeric / Alphanumeric-edited / Boolean / National / National-edited / Numeric / Numeric-edited, each paired with its constant (Alphanumeric-edited -> Alphanumeric SPACES; Boolean -> ZEROS).
- **Markdown:** | Alphanumeric | Alphanumeric SPACES |

   Alphanumeric-edited Alphanumeric SPACES

| Boolean | ZEROS |
|---|---|
| National | National SPACES |
- **Why:** The single table is split in two. The Alphanumeric-edited row is pulled out as a loose paragraph, and the Boolean row becomes the header row of a second, headerless table. The pairings are still readable, but the table is broken.
- **Repair:** —
- **Verifier:** The printed page (PDF p487, printed page 463, ACCEPT statement general rule 13 table) shows one continuous two-column table under the headings 'Screen item' and 'Figurative constant' with eight rows in this order: Alphabetic/Alphanumeric SPACES, Alphanumeric/Alphanumeric SPACES, Alphanumeric-edited/Alphanumeric SPACES, Boolean/ZEROS, National/National SPACES, National-edited/National SPACES, Numeric/ZEROS, Numeric-edited/ZEROS. The markdown page-487.md holds a first table with the header row and only the Alphabetic and Alphanumeric rows. After it comes an indented loose line 'Alphanumeric-edited Alphanumeric SPACES' that is not a table row. Then a second table whose header row is '| Boolean | ZEROS |' followed by the remaining four rows. The claim quotes both sources accurately. No text is missing and every pairing is still present, so no normative content changed. The table structure is still a real transcription defect, beyond line wrapping or scaffolding: as markdown, the Alphanumeric-edited row is not a table row, and the Boolean/ZEROS data row is marked up as a column header. Anything that reads the table by its structure would drop or mislabel those two rows. Confirmed as structural, with the description as given.

### p514-1 · p514 · incorrect-figure · **structural**

- **PDF:** Format 2 (into-giving): DIVIDE { identifier-1 / literal-1 } INTO { identifier-2 / literal-2 } -- INTO printed on the middle line between the two two-row braces
- **Markdown:**        ╭              ╮ <u>INTO</u>
       │              │
       │ identifier-1 │ ╭ identifier-2 ╮
<u>DIVIDE</u> ┤              ├ ┤              ├
       ╰ literal-1    ╯ ╰ literal-2    ╯
- **Why:** The drawn figure moves the keyword INTO above the brace pair and stretches the first brace to five rows with an empty top half, so INTO no longer sits between the dividend-choice brace and the divisor-choice brace. The token order can still be recovered, but a reader of the diagram sees INTO detached from its position; the figure should match Format 3's layout (DIVIDE {..} INTO {..} on one baseline).
- **Repair:** —
- **Verifier:** In 14.9.11.1 Formats 2 (into-giving) and 4 (into-remainder) the PDF prints DIVIDE {identifier-1 | literal-1} INTO {identifier-2 | literal-2} on one baseline with INTO between the two braces (as Format 3 prints BY); the markdown (page-514.md lines 26-30 and 60-64) draws INTO on the top row above an empty-topped five-row first brace and drops the second brace to rows 3-5, detaching INTO from its position between the operands. The alternatives are all present; the figure layout is wrong in both formats.

### p514-2 · p514 · incorrect-figure · **structural**

- **PDF:** Format 4 (into-remainder): DIVIDE { identifier-1 / literal-1 } INTO { identifier-2 / literal-2 } -- INTO printed on the middle line between the two two-row braces
- **Markdown:**        ╭              ╮ <u>INTO</u>
       │              │
       │ identifier-1 │ ╭ identifier-2 ╮
<u>DIVIDE</u> ┤              ├ ┤              ├
       ╰ literal-1    ╯ ╰ literal-2    ╯
- **Why:** Same layout defect as Format 2: INTO is drawn above the braces instead of between them, and the identifier-1/literal-1 brace is stretched with an empty upper portion, misrepresenting where INTO sits in the DIVIDE ... INTO ... GIVING ... REMAINDER format.
- **Repair:** —
- **Verifier:** Rendered PDF page 514 (printed p.490), Format 4 (into-remainder): one baseline reads DIVIDE, a two-row brace {identifier-1 / literal-1}, then INTO (underlined) at mid-height, then a two-row brace {identifier-2 / literal-2}; it matches Format 2 on the same page. The markdown page-514.md Format 4 block is quoted accurately in the claim: the first brace is stretched to five rows with two empty upper rows, INTO sits alone on the top row to the right of that brace, and the second brace starts two rows lower with no keyword between the braces. Format 1 and Format 3 on the same page are drawn correctly (DIVIDE {..} INTO/BY {..} on one baseline), so this is a defect, not a house convention. Reading order (DIVIDE, brace 1, INTO, brace 2) and the brace alternatives can still be recovered, so this is a structural figure defect and not a normative one: the syntax is unchanged, but the drawing misplaces INTO and distorts the first brace.

### p677-1 · p677 · misplaced-content · **structural**

- **PDF:** Character Positions | Contents (two-column table header) ... rows 1-4, 5-6, 7-8, 9-10, 11-12, 13-14 (with its two dash sub-items '— 00 through 59 when a LEAP-SECOND directive with the OFF phrase is in effect' and '— 00 through nn, where nn is defined by the implementor, when a LEAP-SECOND directive with the ON phrase is in effect.'), 15-16, 17, 18-19, 20-21 all as rows of one table
- **Markdown:** Character Contents\
   Positions

| 1-4 | Four numeric digits of the year in the Gregorian calendar. |
|---|---|
... | 13-14 | Two numeric characters of the seconds past the minute in the range: |

— 00 through 59 when a LEAP-SECOND directive with the OFF phrase is in effect\
— 00 through nn, ...

   15-16 Two numeric digits of the hundredths of a second past the second, in the range 00 through

99\. The value 00 is returned if the system on which the function is evaluated does not have the facility to provide the fractional part of a second.

   17 Either the character '–', ...
- **Why:** The CURRENT-DATE character-position table is broken: the column header is plain text outside the table, the table ends after row 13-14, the 13-14 dash sub-items are detached outside the table, and rows 15-16, 17, 18-19 and 20-21 become loose paragraphs. Row 15-16 is further split mid-sentence so '99. The value 00 is returned...' stands as a separate paragraph detached from its position label. All words are present; the table structure that ties each content description to its character positions is lost.
- **Repair:** —
- **Verifier:** Printed page 653 (PDF p677) shows one two-column table, header 'Character Positions' / 'Contents', with rows 1-4 through 20-21; the two em-dash LEAP-SECOND sub-items sit inside the 13-14 Contents cell, and row 15-16 reads continuously '...in the range 00 through 99. The value 00 is returned...'. page-677.md has the header as indented plain text outside the table, a markdown table holding only rows 1-4 to 13-14, the dash sub-items as a separate paragraph after the table, rows 15-16, 17, 18-19 and 20-21 as indented loose paragraphs, and row 15-16 broken by a blank line before '99\. The value 00...', so that sentence is a paragraph with no position label. The claim quotes both sources correctly. Every word is present and each loose row still begins with its position number, so no normative meaning changes. The table structure is still broken and one row's text is split across paragraphs, so this is a genuine structural defect, as the claim describes.

### p721-1 · p721 · misplaced-content · **structural**

- **PDF:** Argument Type / Function type table, last row: "Numeric (some arguments may be integer)    Numeric"
- **Markdown:** | National | National |

   Numeric (some arguments may be integer) Numeric
- **Why:** The last row of the argument-type/function-type table is detached from the markdown table and rendered as a loose indented paragraph, so the table loses its Numeric row and the two columns of that row run together.
- **Repair:** —
- **Verifier:** Printed page 697 (PDF p721), 15.59: the argument-type table has six rows, the last being 'Numeric (some arguments may be integer)' | 'Numeric', aligned in the same two columns as Alphabetic..National. The markdown table (page-721.md) ends at '| National | National |'; the sixth row follows after a blank line as an indented loose paragraph '   Numeric (some arguments may be integer) Numeric'. Both sources are quoted accurately. The words are present, so nothing is lost to a careful human reader, but the table structurally omits its Numeric row and the column boundary between argument type and function type is gone, so any reading of the table as a table (by a tool or a reader scanning rows) loses the numeric-argument -> numeric-function mapping. Genuine structural transcription defect, correctly classified as structural rather than normative.

### p734-1 · p734 · ocr-error · **structural**

- **PDF:** (argument-2₁ / (1 + argument-1) + argument-2₂ / (1 + argument-1) ** 2) ... (argument-2₁ / (1 + argument-1) ** 1) ... (argument-2ₙ / (1 + argument-1) ** n))) where argument-1 and argument-2ᵢ in the terms of the SUM function (subscripts 1, 2, n and i printed below the line)
- **Markdown:** (argument-21 / (1 + argument-1) + argument-22 / (1 + argument-1) \*\* 2) ... (argument-21 / (1 + argument-1) \*\* 1) ... (argument-2n / (1 + argument-1) \*\* n))) ... where argument-1 and argument-2i in the terms of the SUM function
- **Why:** 15.69.3 PRESENT-VALUE returned values: the subscripts that index the occurrences of argument-2 are flattened onto the baseline, so the equivalent expressions read as references to differently named operands 'argument-21', 'argument-22', 'argument-2n', 'argument-2i' instead of the 1st, 2nd, nth and ith occurrence of argument-2.
- **Repair:** —
- **Verifier:** Rendered page 734 (printed 710), 15.69.3 rule 1 b) and c): the PDF prints argument-2 with true subscripts 1, 2, 1, n and i (visibly smaller and lowered glyphs). page-734.md lines 31, 37, 41 and 43 read 'argument-21', 'argument-22', 'argument-21', 'argument-2n' and 'argument-2i' on the baseline with no subscript markup. Quotes in the claim match both sources. The flattening turns an occurrence index into what reads as a distinct operand name (argument-21 vs the first occurrence of argument-2), which changes how the equivalent expression is read; it is not mere wrapping or furniture. Structural severity is appropriate: the intended meaning is recoverable from context and no syntax rule is affected.

### p791-1 · p791 · misplaced-content · **structural**

- **PDF:** Telugu: (combining) 0C01-0C03, 0C3E-0C40, 0C41-0C44, 0C46-0C48, 0C4A-0C4D ... Kannada: (combining) ... Malayalam: (combining) ... Sinhala: (combining) ... Thai: (combining) 0E31, 0E34-0E3A, 0E47-0E4E ... Tibetan: (combining) ... Myanmar: (combining) 102C, 102D-1030, 1031, 1032, 1036-1037, 1038, 1039, 1056-1057, 1058-1059
- **Markdown:** | Telugu: | 0C01-0C03, 0C3E-0C40, 0C41-0C44, 0C46-0C48, 0C4A-0C4D |

(combining)

... | Thai: | 0E31, 0E34-0E3A, 0E47-0E4E |

(combining)

... | Myanmar: | 102C, 102D-1030, 1031, 1032, 1036-1037, 1038, 1039, 1056-1057, 1058-1059 |

(combining)
- **Why:** For Telugu, Kannada, Malayalam, Sinhala, Thai, Tibetan and Myanmar the '(combining)' qualifier is detached from its row and left as a stray paragraph after each table fragment, so each script shows two identically-labelled rows and the combining-mark ranges are indistinguishable from the letter ranges.
- **Repair:** —
- **Verifier:** The printed page 791 (ISO/IEC 1989:2014 p.767, repertoire of characters permitted in user-defined words) labels each second row 'Telugu: (combining)', 'Kannada: (combining)', 'Malayalam: (combining)', 'Sinhala: (combining)', 'Thai: (combining)', 'Tibetan: (combining)', 'Myanmar: (combining)', the qualifier on the line under the script name in the label column. In page-791.md each of these seven scripts is a two-row table whose rows are both labelled bare '<Script>:', and '(combining)' is a separate paragraph after the table. The quotes in the claim match both sources. Tamil and Lao on the same page keep the qualifier with their label, so the transcription is inconsistent within the page. The word is present but sits outside the row it qualifies. A reader or tool that reads the table gets two identically labelled rows and cannot tell the combining-mark ranges from the letter ranges. The misplacement changes how the table reads, so it is a real structural defect. The description is accurate.

### p810-1 · p810 · image-lost-or-restructured · **structural**

- **PDF:** Figure D.1 is a flowchart: Entrance -> 'Index Setting: exceeds highest permissible occurrence or is zero or negative' --True (AT END)*--> 'imperative-statement-1'; --False--> 'Condition-1' --True--> 'imperative-statement-2'; --False--> 'Condition-2' (marked *) --True--> 'imperative-statement-3' (marked *); --False--> 'Increment index-name for identifier-1' -> 'Increment index-name-1 (for a different table) or identifier-2' (marked *) -> loops back to the entrance; a brace marked ** groups the three exits from the imperative-statements.
- **Markdown:** Index Setting:\ exceeds highest True (AT END)\* imperative-\ permissible occur-\ ... statement-1\ rence or is zero or\ negative

False

True imperative- \*\* Condition-1

statement-2

False

| \* |  | \* |
|---|---|---|
| True | imperative- |  |

Condition-2\ ... statement-3
- **Why:** The OCR scattered the flowchart's box labels into interleaved fragments and a spurious table; the flow edges (which test leads where, the True/False branches, the loop back from the increment boxes to the index test) and which operations carry the * (optional) and ** (control-transfer) marks are not recoverable from the markdown. The figure is informative (Annex D), but the SEARCH serial-search order it illustrates is lost.
- **Repair:** —
- **Verifier:** I checked the 300 dpi render of printed page 786 (PDF p810). Figure D.1 is a flowchart, and the claim describes it accurately. The index test's True (AT END)* branch goes to imperative-statement-1 and its False branch to Condition-1. Condition-1's True branch goes to imperative-statement-2 and its False branch to Condition-2, which carries a *. Condition-2's True branch goes to imperative-statement-3, which also carries a *, and its False branch to 'Increment index-name for identifier-1'. That box leads to 'Increment index-name-1 (for a different table) or identifier-2', which carries a *, and a line loops from it back to the entrance above the index test. A brace marked ** groups the three exit arrows. In the markdown every box label survives, but the labels are interleaved: 'True (AT END)* imperative-' sits inside the index-test text, and the ** lands between 'True imperative-' and 'Condition-1'. A spurious table holds '* | | *' and 'True | imperative-'. The markdown has no edges, no loop-back and no brace, so it does not say which * marks which operation, and the serial-search order cannot be recovered from it. The figure is informative (Annex D). The footnotes and caption are transcribed correctly. The claim's markdown quote shortens the text with '...' but does not misrepresent it.

### p818-1 · p818 · image-lost-or-restructured · **structural**

- **PDF:** Figure D.3 is a drawn box diagram: a heavy-bordered Compilation Group holding three compilation units; the middle one holds source unit P-1 containing a dashed source element, source unit P-1-1 (with a dashed element) and source unit P-1-2 (with a dashed element and nested units P-1-2-1 and P-1-2-2, each with a dashed element). Arrows lead from the outer compilation units 'To other run unit', and a bracket with an arrow maps the middle compilation unit to a Runtime Module in a heavy-bordered Run Unit below, whose outer modules receive arrows 'From other comp. group'. A legend maps border styles to Compilation Group, Compilation Unit, Source Unit, Source Element, Run Unit, Runtime Module and Runtime Element.
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
- **Why:** The diagram's content (the containment of P-1, P-1-1, P-1-2, P-1-2-1, P-1-2-2, the mapping of compilation units to runtime modules, and the legend's border-style key) is lost; the markdown keeps only scattered labels, merges legend entries with unit names ('P-1-1 P-1-2 Compilation Group'), and invents a table out of arrow captions. Informative annex D, so not normative, but the figure the preceding paragraph describes is unreadable from the transcription.
- **Repair:** —
- **Verifier:** Accurate as stated. One addition: the middle runtime module in the run unit holds unlabelled dashed runtime elements that mirror the source elements of P-1, P-1-1, P-1-2, P-1-2-1 and P-1-2-2. The markdown also loses this one-to-one correspondence.

### p867-1 · p867 · image-lost · **structural**

- **PDF:** Figure D.5 draws an Account box (account-number, balance, date-opened in the data-items compartment; deposit, withdraw, balance in the methods compartment), with labels "Data items" and "Methods" pointing at the compartments, and two subclass boxes with arrows up to Account: CheckingAccount (charges; displayCharges, calculateCharges) and SavingAccount (interest-rate; calculateInterest).
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
- **Why:** The figure is flattened and scrambled: the "Data items" label is run into the data item "balance", the "Methods" label sits between the method names deposit and withdraw, calculateCharges is detached from the CheckingAccount column, and the inheritance arrows are gone. Informative annex, so structural.
- **Repair:** —
- **Verifier:** Rendered page 867 (printed p.843) at 300 dpi. The PDF draws Figure D.5 as a UML-style diagram: an Account box with a data compartment (account-number, balance, date-opened) and a methods compartment (deposit, withdraw, balance), external labels 'Data items' and 'Methods' with arrows to those compartments, and two subclass boxes, CheckingAccount (charges | displayCharges, calculateCharges) and SavingAccount (interest-rate | calculateInterest), each with an inheritance arrow up to Account. The markdown (page-867.md) quotes exactly what the claim says: 'Data items' is fused into the line 'Data items balance', 'Methods' is wedged between deposit and withdraw, calculateCharges sits as a loose paragraph below the table rather than in the CheckingAccount methods cell, and nothing records that the two classes inherit from Account. All the words are present, but the figure's structure (which names are labels and which are members, which method belongs to which class, and the inheritance relation) is lost or wrong. The surrounding prose restates the inheritance and membership, and Annex D is informative, so structural severity is right. Claim is accurate.

### p880-1 · p880 · incorrect-figure · **structural**

- **PDF:** Figure D.6 is a three-column layout: a form-zone column ("Physical Top of Form", "Logical Top of Form", "Logical Bottom of Form", "Physical Bottom of Form"), a PAGE clause phrase column ("Heading", "First Detail", "Last Control Heading", "Last Detail", "Footing", "Page Limit") and a report-content column; e.g. row "Logical Top of Form | Heading | the first line of the report or page heading <(first page only) report header lines> <page heading lines>" and row "Logical Bottom of Form | Page Limit | the last line on a logical page".
- **Markdown:** | Logical Top Heading | the first line of the report or page heading |
|---|---|
| of Form | &lt;(first page only) report header lines> |
 ... 
   Logical Page Limit the last line on a logical page\
   Bottom of\
   Form
- **Why:** The column structure of Figure D.6 is lost: form-zone labels are fused with PAGE clause phrase names ("Logical Top Heading", "Logical Page Limit"), "of Form" lands in the phrase column of a partial two-column table, and the rest of the figure is free text, so the association of each PAGE clause phrase with its form position and report content is garbled. Informative annex, no normative effect.
- **Repair:** —
- **Verifier:** Figure D.6 (informative Annex D) loses its three-column structure in the markdown: zone labels are fused with PAGE phrase names ('Logical Top Heading', 'Logical Page Limit'), and a stray two-column pipe table puts 'of Form' in a row-label cell beside report-content text. All words are present and in order; only the column association is garbled. Structural, no normative effect.

### p890-1 · p890 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.7, AND evaluation): each 'Condition-n false' decision's 'yes' branch joins one vertical line with an arrow to 'Truth value is false'; each 'no' branch continues down to the next 'Evaluate condition'; the last 'no' arrows to 'Truth value is true'.
- **Markdown:** Condition-1 yes\
&emsp;false

no
...
| Truth value | Truth value |
|---|---|
| is true | is false |
- **Why:** The drawn flowchart is flattened to its box labels; the lines and arrows that say which branch leads to which outcome (all 'yes' exits converge on 'false') are lost, and the two outcomes are rendered as a 2-column table.
- **Repair:** —
- **Verifier:** D.22 (page 890, printed 866) flowchart: the markdown keeps only the node labels (mangled into 'Condition-1 yes / false', 'no') and a 2-column table for the outcomes, and loses the connecting lines. In the PDF every 'Condition-k false' yes-exit converges on one arrow to 'Truth value is false', each no-exit leads to the next 'Evaluate condition', and the last no-exit arrows to 'Truth value is true'. No figure number or caption is printed on this page; the 'Figure D.7, AND evaluation' label in the claim is not from the page.

### p891-1 · p891 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.8, OR evaluation): each 'Condition-n true' 'yes' branch joins one line arrowed to 'Truth value is true'; 'no' continues down; the last 'no' arrows to 'Truth value is false'.
- **Markdown:** Condition-1 yes\
&emsp;true

no
...
| Truth value | Truth value |
|---|---|
| is false | is true |
- **Why:** Flowchart flattened to labels; the branch-to-outcome connections are lost and the outcomes rendered as a table.
- **Repair:** —
- **Verifier:** Rendered PDF page 891 (printed 867) shows the Figure D.8 flowchart: Evaluate condition-1/2/n boxes alternating with 'Condition-k true' decision ovals; each 'yes' exits right into a single vertical line arrowed down to 'Truth value is true'; each 'no' continues down to the next evaluation, and the last 'no' arrows to 'Truth value is false'. The markdown keeps only the box and label text in sequence ('Condition-1 yes\ true', 'no', ...) and ends with a two-column table 'Truth value | Truth value / is false | is true'. No edge information survives: nothing in the markdown says that any 'yes' leads to true or that only the final 'no' leads to false, and the table falsely presents the two outcomes as a header/data grid. The edges are the figure's entire content (OR short-circuit evaluation), so this is a genuine content loss, not mere formatting. The Figure D.7 caption at the top of the markdown is faithful (the PDF page carries that caption above this figure too), so it is not part of the defect. Informative Annex D figure, so structural rather than normative. Claim is accurately described.

### p892-1 · p892 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.9, condition-1 OR condition-2 AND condition-3): condition-1 true 'yes' goes right to 'Truth value is true'; condition-2 false 'yes' and condition-3 false 'yes' go left to 'Truth value is false'; condition-3 false 'no' goes right to 'Truth value is true'.
- **Markdown:** yes Condition-2\
&emsp;&emsp;&emsp;&emsp;false
...
yes Condition-3 no\
&emsp;&emsp;&emsp;&emsp;&emsp;false

| Truth value | Truth value |
|---|---|
| is false | is true |
- **Why:** Flowchart flattened to labels; which branch reaches which truth value (the AND-before-OR precedence the figure illustrates) is lost.
- **Repair:** —
- **Verifier:** Rendered PDF page 892 (printed 868) shows Figure D.9 as a flowchart: Evaluate condition-1 -> 'Condition-1 true' decision; 'yes' exits right on a line that runs down to the arrow 'Truth value is true'; 'no' goes down to Evaluate condition-2 -> 'Condition-2 false'; 'yes' exits left down to the arrow 'Truth value is false'; 'no' goes down to Evaluate condition-3 -> 'Condition-3 false'; 'yes' exits left joining the false line, 'no' exits right joining the true line. The claim's pdf_says matches this exactly. The markdown keeps every box and label text but no edges or arrows: the decision outcomes are loose 'yes'/'no' words and the two terminals are recast as a two-column table, so the routing of each branch to a truth value (the figure's whole content: condition-2 AND condition-3 evaluated as a unit, condition-1 true short-circuiting to true) must be guessed from word order. Condition-1's 'yes' is not even tied to a direction. This is lost figure content, not furniture or wrapping. The figure is in informative Annex D, so severity structural (not normative) is right.

### p897-1 · p897 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.11): Entrance -> Set identifier-2 equal to current FROM value -> Condition-1 (True -> Exit; False -> Execute specified set of statements -> Augment identifier-2 with current BY value), with a line from 'Augment identifier-2' back to Condition-1.
- **Markdown:** Condition-1 True Exit

False

Execute specified\
set of statements

Augment identifier-2\
with current BY value
- **Why:** Flowchart flattened to labels; the loop-back from 'Augment identifier-2' to Condition-1, the point of the PERFORM VARYING TEST BEFORE illustration, is lost.
- **Repair:** —
- **Verifier:** I rendered page 897 (printed p.873) at 300 dpi. Figure D.11 shows Entrance -> box 'Set identifier-2 equal to current FROM value' -> rounded node 'Condition-1'. From Condition-1, 'True' goes right to 'Exit' and 'False' goes down to box 'Execute specified set of statements', then to box 'Augment identifier-2 with current BY value'. A line leaves the left side of the Augment box, runs up the left margin and enters Condition-1 with an arrowhead. The markdown keeps every label in reading order: Entrance, the Set box, 'Condition-1 True Exit', False, Execute, Augment. It has no arrows and nothing for the loop-back edge. The order of the labels implies the straight-line flow, but nothing in the text recovers the edge from Augment back to Condition-1, which is what makes this an iteration. The claim quotes both sources correctly. The figure sits in informative Annex D ('not intended to dictate implementation'), so it is not normative, and structural severity fits.

### p898-1 · p898 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.12): Condition-2 False -> Execute specified set of statements -> Augment identifier-5 -> back to Condition-2; Condition-2 True -> Augment identifier-2 -> Set identifier-5 to current FROM value -> back to Condition-1; Condition-1 True -> Exit.
- **Markdown:** | Execute specified set | Augment identifier-2 |
|---|---|
| of statements | with current BY value |
| Augment identifier-5 | Set identifier-5 to |
| with current BY value | current FROM value |
- **Why:** Flowchart flattened; its boxes are rendered as a 2-column table with a header row, and both loop-backs (to Condition-2 and to Condition-1) are lost.
- **Repair:** —
- **Verifier:** Rendered printed page 874 (PDF p898) at 300 dpi. Figure D.12 is a flowchart: Entrance -> Set id-2 to FROM -> Set id-5 to FROM -> Condition-1 (True -> Exit; False -> Condition-2); Condition-2 False -> Execute specified set of statements -> Augment id-5 with BY -> loop back into Condition-2; Condition-2 True -> Augment id-2 with BY -> Set id-5 to FROM -> loop back into Condition-1. The pdf_says summary matches the printed edges. The markdown keeps every box label, but it puts the four lower boxes into a 2-column table. The first row of that table is a header holding half of each of two boxes, so box text is split across rows. It carries no arrows, so neither loop-back appears, and the True/False labels hang loose with nothing to say which branch leads where. The figure's content is its control flow, which is the nesting of the two VARYING loops, and that cannot be recovered from the markdown. This is a genuine loss of figure content, not just formatting. The figure is in informative Annex D, so the loss is structural, not normative. The markdown_says quote matches the transcription exactly.

### p899-1 · p899 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.13): Entrance -> Set identifier-2 -> Execute specified set of statements -> Condition-1 (True -> Exit; False -> Augment identifier-2 with current BY value), with a line from 'Augment identifier-2' back to above 'Execute specified set of statements'.
- **Markdown:** Execute specified\
set of statements

True\
Condition-1 Exit

False

Augment identifier-2\
with current BY value
- **Why:** Flowchart flattened to labels; the loop-back that distinguishes TEST AFTER (augment, then execute again before testing) is lost.
- **Repair:** —
- **Verifier:** Figure D.13 (informative Annex D) is transcribed as a flat list of its box labels with no edges. All labels and the caption are present (markdown_says in the claim quotes only part of them; Entrance and the Set-identifier-2 box are also present). The flowchart's arrows are lost: Entrance -> Set -> Execute -> Condition-1; True -> Exit; False -> Augment; and Augment loops back to just before Execute. This is a structural, non-normative loss. The normative semantics are in the PERFORM general rules.

### p900-1 · p900 · incorrect-figure · **structural**

- **PDF:** Flowchart (Figure D.14): Augment identifier-5 loops back to above 'Execute specified set of statements'; Condition-2 True goes to Condition-1; Condition-1 False -> Augment identifier-2, which loops back to above 'Set identifier-5 to current FROM value'; Condition-1 True -> Exit.
- **Markdown:** True\
Condition-2

False

Augment identifier-5\
with current BY value

Condition-1 True\
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;Exit
- **Why:** Flowchart flattened to labels; both loop-backs and the Condition-2 True -> Condition-1 edge are lost.
- **Repair:** —
- **Verifier:** Rendered printed page 876 (PDF p900) at 300 dpi. Figure D.14 shows: Entrance -> Set identifier-2 -> (join) -> Set identifier-5 -> (join) -> Execute specified set of statements -> Condition-2; Condition-2 False -> Augment identifier-5, whose line returns to the join above 'Execute specified set of statements'; Condition-2 True -> line right and down to Condition-1; Condition-1 True -> Exit; Condition-1 False -> Augment identifier-2, whose line returns to the join above 'Set identifier-5 to current FROM value'. The claim's pdf_says matches exactly. page-900.md holds only the box/label texts in sequence with no edges or arrows, so the two loop-back targets and the Condition-2 True -> Condition-1 edge are absent; the flattened sequence even implies the wrong flow (Augment identifier-5 followed by Condition-1). The topology of the loop is the figure's content (the TEST AFTER varying semantics it illustrates), so this is a genuine content loss, not formatting. Informative annex figure, so structural rather than normative is appropriate.

### p929-1 · p929 · misplaced-content · **structural**

- **PDF:** WRITE statement  619 (indented sub-entry, continuing AT EOP phrase) ... ATTRIBUTE phrase / SET statement, attribute setting  574 (indented) ... AUTOMATIC phrase / LOCK MODE clause  259 (indented) ... AWAY-FROM-ZERO phrase / DEFAULT ROUNDED clause  219 / ROUNDED phrase  447 (indented)
- **Markdown:**    WRITE statement 619\
   ATAN function 646\
   ATTRIBUTE phrase\
   SET statement, attribute setting 574\ ... AUTOMATIC phrase\
   LOCK MODE clause 259\
- **Why:** The column's first twelve lines lose the index hierarchy: sub-entries and headwords are printed at the same level, so 'SET statement, attribute setting 574' reads as a top-level entry. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Lost indentation, not misplaced content. On p929 (printed 905), the transcription gives the first twelve left-column index lines the same three-space indent instead of the &emsp; sub-entry marker the rest of the page uses. As a result, the sub-entries 'WRITE statement 619' (continued from the previous page), 'SET statement, attribute setting 574' (under ATTRIBUTE phrase), 'LOCK MODE clause 259' (under AUTOMATIC phrase), and 'DEFAULT ROUNDED clause 219' and 'ROUNDED phrase 447' (under AWAY-FROM-ZERO phrase) render at the same level as the headwords. The fix is to prefix those five lines with &emsp; and strip the leading spaces from the seven headword lines. The text itself is complete and correct. Severity: structural (informative index).

### p929-2 · p929 · misplaced-content · **structural**

- **PDF:** Binary encoding
    ISO/IEC/IEEE 60559 definition reference  410
BINARY phrase
    USAGE clause  405
- **Markdown:** Binary encoding

   ISO/IEC/IEEE 60559 definition reference 410 BINARY phrase

   USAGE clause 405\
- **Why:** The sub-entry of 'Binary encoding' is merged with the next headword 'BINARY phrase', and the whole right column (BINARY phrase through BY CONTENT phrase / Procedure division header 102) loses its sub-entry indentation. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Printed page 905 (PDF p929), 300 dpi render: left column ends with headword 'Binary encoding'; the right column opens with the indented sub-entry 'ISO/IEC/IEEE 60559 definition reference 410', then headword 'BINARY phrase' on its own line at the left margin, then indented 'USAGE clause 405'. The pattern continues down the right column, with headwords such as BINARY-CHAR phrase, Boolean expressions and BY CONTENT phrase at the margin and their sub-entries indented under them. In page-929.md, line 61 reads '   ISO/IEC/IEEE 60559 definition reference 410 BINARY phrase', so the headword is joined onto the previous sub-entry's line. Lines 61-118 then put every right-column line at the same three-space indent with no &emsp; markers, while the left column keeps its &emsp; sub-entry markers on lines 25-57. Headwords and sub-entries are therefore indistinguishable: 'COMPUTE statement 482' under 'Boolean expressions' reads like a headword of its own, and 'BINARY phrase' reads as part of the 60559 sub-entry. All the words and numbers are present and in the right order, so no text is lost. What is lost is the index hierarchy, which is content in an index because it says which headword a page reference belongs to. The claim quotes both sources accurately. Informative annex, so structural rather than normative.

### p930-1 · p930 · misplaced-content · **structural**

- **PDF:** Calendar date format  629
CALL statement  471
- **Markdown:** Calendar date format 629 CALL statement 471
- **Why:** Two index entries merged on one line. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Index (printed p.906, PDF p930): the separate top-level entries 'Calendar date format 629' and 'CALL statement 471' are fused into one markdown line with no hard line break. Both page numbers are present and in the right order, so no reference is lost. The problem is that two headwords read as a single entry. The fix is to split them into two lines, with the backslash break used everywhere else on the page.

### p930-2 · p930 · misplaced-content · **structural**

- **PDF:** (right column) National  17 / Numeric  18 indented under Character; CHARACTER CLASSIFICATION clause  229 / OBJECT-COMPUTER paragraph  228 (indented); Class / BASE  735 / Parameterized  200 (indented); Class of data / Alphanumeric 129 ... Pointer 129 (indented)
- **Markdown:**    National 17\
   Numeric 18\
   CHARACTER CLASSIFICATION clause 229\
   OBJECT-COMPUTER paragraph 228\ ... Class\
   BASE 735\
   Parameterized 200\
- **Why:** The whole right column loses the index hierarchy: sub-entries print at the same level as headwords. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Rendered page 930 (printed 906, index B/C). In the PDF's right column, sub-entries are visibly indented under their headwords: National 17 / Numeric 18 continue the Character sub-list from the left column; OBJECT-COMPUTER paragraph 228 is under CHARACTER CLASSIFICATION clause 229; ALLOCATE/BLOCK CONTAINS/INSPECT/RECORD under CHARACTERS phrase; BASE 735 / Parameterized 200 under Class; SPECIAL-NAMES 231 under CLASS clause; BOOLEAN phrase under Class condition; Alphanumeric..Pointer 129 under Class of data; USAGE clause under CLASS phrase; CLASS clause..Truth value under Class-name; Implied under CLOSE statement; Report description entry under CODE clause; SPECIAL-NAMES 232 under Code-name. The markdown gives every right-column line the same 3-space prefix with no &emsp; marker (the left column uses &emsp; for sub-entries), so headwords and sub-entries are at one level and e.g. 'BASE 735' or 'OBJECT-COMPUTER paragraph 228' read as independent headwords. The claim quotes both sources accurately. This is more than whitespace: in an index the nesting carries which headword a page reference belongs to, so the meaning of entries changes. Informative annex content only, so structural not normative.

### p931-1 · p931 · misplaced-content · **structural**

- **PDF:** CODE-SET clause  68, 309, 387
    File description entry  271
Collating sequence  231
- **Markdown:** CODE-SET clause 68, 309, 387 File description entry 271 Collating sequence 231
- **Why:** A headword, its sub-entry and the next headword are merged on one line. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** The rendered page (printed 907, PDF p931) shows three separate index lines at the top of the left column: headword 'CODE-SET clause 68, 309, 387', an indented sub-entry 'File description entry 271', and a new headword 'Collating sequence 231'. page-931.md line 3 holds all three as one paragraph with no '\' line break and no '&emsp;' indent, while every other entry on the page uses that encoding (e.g. 'COLLATING SEQUENCE clause 256\' then '&emsp;FILE-CONTROL paragraph 248\'). No words or numbers are missing, but the index hierarchy is lost: the markdown reads as a single entry, so 'Collating sequence 231' is no longer a headword and 271 is not shown as a sub-entry of CODE-SET clause. The claim quotes both sources accurately. This is an informative annex, so structural (not normative) severity is right.

### p931-2 · p931 · misplaced-content · **structural**

- **PDF:** (right column) REPLACE statement  37 (indented, continuing Compiler directing statement); Complex conditions 164 / AND operator 164 / NOT operator 164 / OR operator 164 (indented); Condition 152 / Abbreviated combined relation condition 165 ... (indented)
- **Markdown:**    REPLACE statement 37\
   Compiler directive 46\ ... Complex conditions 164\
   AND operator 164\
   NOT operator 164\
- **Why:** The whole right column loses the index hierarchy: sub-entries print at the same level as headwords. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Rendered page 931 (printed 907) at 300 dpi. In the PDF's right column, sub-entries are visibly indented under their headwords: REPLACE statement 37 (continuing Compiler directing statement / COPY statement from the left column), AND/NOT/OR operator 164 under Complex conditions, ADD/DIVIDE/MULTIPLY/SUBTRACT under Composite of operands, USAGE clause 405 under COMPUTATIONAL phrase, OBJECT-COMPUTER/SOURCE-COMPUTER paragraph under Computer-name, Abbreviated combined relation condition ... Switch-status condition under Condition 152, EVALUATE statement 493 under Conditional expression, Definition 114 under Conditional variable, Qualified with subscripts ... Switch status under Condition-name. The markdown renders every right-column line with the same 3-space prefix and no &emsp; marker (unlike the left column, which uses &emsp; for sub-entries), so headwords and sub-entries sit at one level and the hierarchy is lost; e.g. 'USAGE clause 405' and 'EVALUATE statement 493' read as top-level entries. The claim's quotes are accurate (markdown quote abbreviated with '...'). Content is informative index, so structural, not normative.

### p933-1 · p933 · misplaced-content · **structural**

- **PDF:**     SET statement, pointer arithmetic  574
DUPLICATES phrase
    ALTERNATE RECORD KEY clause  255
    SORT statement  585
- **Markdown:**    SET statement, pointer arithmetic 574 DUPLICATES phrase

   ALTERNATE RECORD KEY clause 255\
   SORT statement 585\
- **Why:** A sub-entry (continuing DOWN phrase) is merged with the headword 'DUPLICATES phrase', and the right column through 'Scope of 115' loses its sub-entry indentation. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Printed page 933 (folio 909), top of the right column, shows the indented sub-entry 'SET statement, pointer arithmetic 574' (continuing DOWN phrase from the left column) on its own line, then the headword 'DUPLICATES phrase' flush left, then the indented sub-entries 'ALTERNATE RECORD KEY clause 255' and 'SORT statement 585'. The markdown (page-933.md line 62) joins them into one line, 'SET statement, pointer arithmetic 574 DUPLICATES phrase', so the headword reads as trailing text of the previous sub-entry. Lines 64-83 also give every right-column line the same 3-space prefix, so headwords such as 'DYNAMIC 346', 'DYNAMIC phrase' and 'Dynamic-length-structure-name 76, 79' cannot be told apart from their sub-entries ('ACCESS MODE clause 248, 254', 'Scope of 115'). The left column keeps the distinction with &emsp;. The defect is real. It affects only the index, which is informative, so it is structural and not normative.

### p934-2 · p934 · misplaced-content · **structural**

- **PDF:** EC-PROGRAM-RESOURCES exception  103, 444, 474
EC-RAISING exception  444
- **Markdown:** EC-PROGRAM-RESOURCES exception 103, 444, 474 EC-RAISING exception 444
- **Why:** Two index entries merged on one line. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Index entries 'EC-PROGRAM-RESOURCES exception 103, 444, 474' and 'EC-RAISING exception 444' are merged into one transcription line (page-934.md line 62) without a line break. Every page reference is still there and in order, so the only problem is that the entry boundary is unclear. The index is informative.

### p935-2 · p935 · misplaced-content · **structural**

- **PDF:** Equivalent arithmetic expression  634
ERASE clause  322
- **Markdown:** Equivalent arithmetic expression 634 ERASE clause 322
- **Why:** Two index entries merged on one line. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** The rendered PDF page 935 (printed page 911, index) shows at the top of the right column two separate index entries on separate lines: 'Equivalent arithmetic expression  634' and 'ERASE clause  322'. The markdown (page-935.md line 53) has them as one paragraph line, 'Equivalent arithmetic expression 634 ERASE clause 322', with no line break between them, while every neighbouring entry ends with a '\' hard break. The entry boundary is lost, so a reader or parser sees one headword 'Equivalent arithmetic expression 634 ERASE clause' with page 322. No text is missing and both page numbers are present in order, so this is a structural defect in the informative index only, with no normative effect. The claim quotes both sources accurately.

### p936-1 · p936 · misplaced-content · **structural**

- **PDF:** FACTORIAL function  668
FACTORY clause
    METHOD-ID paragraph  215
- **Markdown:** FACTORIAL function 668 FACTORY clause

   METHOD-ID paragraph 215\
- **Why:** Two headwords merged on one line, and the right column (through Files / Relative 178) loses its sub-entry indentation. The index is informative, but a flattened or merged index entry misattributes page references to the wrong headword.
- **Repair:** —
- **Verifier:** Rendered page 936 (printed index page 912), right column under 'F': 'FACTORIAL function 668' and 'FACTORY clause' are two separate headword lines, with 'METHOD-ID paragraph 215' indented as a sub-entry of FACTORY clause. page-936.md line 61 reads 'FACTORIAL function 668 FACTORY clause' as one line. From line 63 to 'Relative 178', every right-column line has the same 3-space indent, so headwords such as 'Factory data 199, 839', 'FALSE phrase', 'File' and 'Files 176, 773' cannot be told apart from their sub-entries ('EVALUATE statement 493', 'logical 176', 'Indexed 178'). The PDF shows two indent levels. The merge makes '668' look like it belongs to a combined 'FACTORIAL function ... FACTORY clause' entry, and the flattening hides which headword owns each sub-entry's page reference. This is a real structural defect in the informative index, and the claim describes it accurately. A separate artifact on the same page, outside this claim: the markdown repeats 'FILE-CONTROL paragraph 248' twice, while the PDF has the sub-entry 'FILE-CONTROL paragraph 248' under FILE STATUS clause and then the headword 'FILE-CONTROL paragraph 248'.

### p939-1 · p939 · misplaced-content · **structural**

- **PDF:** (indented) CLASS clause 159 / Incompatibility flagging 55, 57 / Incompatible data 446 / Index / (indented) Definition 349 / (indented) Index-name 97
- **Markdown:**    CLASS clause 159 Incompatibility flagging 55, 57 Incompatible data 446 Index

   Definition 349\
   Index-name 97\
- **Why:** Four separate index entries (a sub-entry continued from the previous page and three headwords) are run together on one line, and the rest of the left column loses its sub-entry indentation, so sub-entries such as 'Definition 349' and 'Index-name 97' under Index read as top-level terms. Informative index only.
- **Repair:** —
- **Verifier:** Accurate as stated. The lost sub-entry indentation covers the whole left column of the page (CLASS clause through INSPECT statement examples), not only the Index entry. The right column is correct.

### p940-1 · p940 · misplaced-content · **structural**

- **PDF:** (indented) SET statement, locale 574 / LEADING phrase / (indented) INSPECT statement 515 / (indented) SIGN clause 388 / LEAP-SECOND directive 61
- **Markdown:**    SET statement, locale 574 LEADING phrase

   INSPECT statement 515\
   SIGN clause 388\
   LEAP-SECOND directive 61\
- **Why:** The continued sub-entry of LC_TIME and the headword LEADING phrase are merged onto one line, and the whole right column loses its sub-entry indentation, so sub-entries (e.g. 'INSPECT statement 515', 'Qualified 96') are indistinguishable from headwords. Informative index only.
- **Repair:** —
- **Verifier:** Printed page 940 (folio 916), right column, top: 'SET statement, locale 574' is indented as LC_TIME's continued sub-entry (LC_TIME 74, 435 ends the left column), then the headword 'LEADING phrase' sits flush left with the indented sub-entries 'INSPECT statement 515' and 'SIGN clause 388', then the headword 'LEAP-SECOND directive 61'. The markdown at page-940.md has '   SET statement, locale 574 LEADING phrase' on one line, then a blank line, and the rest of the right column (from 'INSPECT statement 515' to 'Alphanumeric 84') all carries the same 3-space indent with no &emsp; sub-entry markers. The left column does keep its &emsp; markers. So the two lines really are merged and the headword/sub-entry hierarchy really is lost in the right column. For example, 'Qualified 96' under LINAGE-COUNTER and LINE-COUNTER, and 'Data 133'/'Object 200' under 'Life cycle', can no longer be told apart from headwords. This is a transcription defect in the informative index, not a wrapping or formatting artifact. It changes which entry a locator belongs to, but it has no normative effect.

### p941-1 · p941 · misplaced-content · **structural**

- **PDF:** Locale 74 / (indented) Category names 74 / (indented) Field names 74, 75 / LOCALE clause 132 / (indented) SPECIAL-NAMES paragraph 231
- **Markdown:**    Locale 74\
   Category names 74\
   Field names 74, 75\
   LOCALE clause 132\
   SPECIAL-NAMES paragraph 231\
- **Why:** The whole left column loses its sub-entry indentation, so sub-entries read as top-level index terms. Informative index only.
- **Repair:** —
- **Verifier:** Accurate as stated. A sibling defect on the same page: the first right-column line has been merged into the M section as '   Variable-length groups 128 MAX function 693', which joins the sub-entry under Matching with the top-level MAX function entry on one line.

### p941-2 · p941 · misplaced-content · **structural**

- **PDF:** Matching / (indented, top of right column) Variable-length groups 128 / MAX function 693
- **Markdown:** Matching

   Variable-length groups 128 MAX function 693
- **Why:** The sub-entry of 'Matching' and the separate headword 'MAX function 693' are run together on one line. Informative index only.
- **Repair:** —
- **Verifier:** Printed page 941 (folio 917): the left column ends with 'Matching'. The right column opens with the indented sub-entry 'Variable-length groups 128', and below it, at headword indentation, is the separate entry 'MAX function 693'. The transcription (page-941.md line 61) has '   Variable-length groups 128 MAX function 693' on one indented line. All the text is present, but the merge makes the headword 'MAX function 693' read as part of the 'Matching' sub-entry, so the index structure is wrong. This is more than wrapping. It is a structural defect in informative index matter only and has no normative effect.

### p946-1 · p946 · misplaced-content · **structural**

- **PDF:** (indented) File description entry 271 / RECORD DELIMITER clause 262 / (indented) FILE-CONTROL paragraph 250 / Record description entry 284 / RECORD IS VARYING clause 623 / Record key 255, 263
- **Markdown:**    File description entry 271 RECORD DELIMITER clause 262 FILE-CONTROL paragraph 250 Record description entry 284 RECORD IS VARYING clause 623 Record key 255, 263
- **Why:** Six index entries (headwords and sub-entries) are collapsed into one line, losing entry boundaries and hierarchy. Informative index only.
- **Repair:** —
- **Verifier:** Rendered page 946 (printed 922, index, left column top) shows six separate lines: indented 'File description entry 271' (sub-entry continuing the previous page's headword), 'RECORD DELIMITER clause 262', indented 'FILE-CONTROL paragraph 250' (sub-entry of RECORD DELIMITER clause), 'Record description entry 284', 'RECORD IS VARYING clause 623', 'Record key 255, 263'. page-946.md line 3 holds all six as one space-joined line with three leading spaces, while the following entries (line 5 onward) are separate lines with &emsp; marking sub-entries. No words or page numbers are missing, but entry boundaries and the sub-entry hierarchy are lost: FILE-CONTROL paragraph 250 is no longer attributable to RECORD DELIMITER clause, and the numbers cannot be reliably attached to their headwords. This is more than line wrapping; the claim quotes both sources accurately. Informative index only, so structural, not normative.

### p948-1 · p948 · misplaced-content · **structural**

- **PDF:** (indented) FILE-CONTROL paragraph 248 / Sharing data 136
- **Markdown:**    FILE-CONTROL paragraph 248 Sharing data 136
- **Why:** The continued sub-entry of SHARING clause and the headword 'Sharing data 136' are run together on one line. Informative index only.
- **Repair:** —
- **Verifier:** Rendered PDF page 948 (printed 924), left column top: line 1 is an indented sub-entry 'FILE-CONTROL paragraph  248' (continuing the previous page's headword), line 2 is a separate unindented headword 'Sharing data  136'. The markdown line 3 is '   FILE-CONTROL paragraph 248 Sharing data 136': two distinct index entries joined into one line. That is more than reflow: the headword 'Sharing data' now reads as part of the FILE-CONTROL sub-entry, and its page 136 can be misread as a second locator for FILE-CONTROL paragraph. Nothing is missing; all text and page numbers are present. Informative annex index only, no normative effect.

### p948-2 · p948 · misplaced-content · **structural**

- **PDF:** SPECIAL-NAMES paragraph 160, 162, 231, 578 / (indented) ACCEPT statement 461
- **Markdown:** SPECIAL-NAMES paragraph 160, 162, 231, 578 ACCEPT statement 461
- **Why:** The sub-entry 'ACCEPT statement 461' is merged onto its headword's line, so its locator reads as part of the headword. Informative index only.
- **Repair:** —
- **Verifier:** The rendered page 948 (printed 924), right column, top: 'SPECIAL-NAMES paragraph 160, 162, 231, 578' is on its own line, and 'ACCEPT statement 461' sits on the next line, indented as a sub-entry. page-948.md line 62 reads 'SPECIAL-NAMES paragraph 160, 162, 231, 578 ACCEPT statement 461' as a single paragraph, with no line break and no &emsp; indent. Every other sub-entry in the transcription uses the '\' break plus an &emsp; indent (for example line 87, '&emsp;SPECIAL-NAMES paragraph 232'). So the index loses its hierarchy here: 'ACCEPT statement 461' reads as part of the SPECIAL-NAMES headword's locator list, when it is really a sub-entry under it. Both sources are quoted accurately. This is a genuine structural defect in an informative index, not a normative one.

### p950-1 · p950 · misplaced-content · **structural**

- **PDF:** UCS  10, 11, 22, 68, 85, 105, 532, 814, 824
    Definition  22
- **Markdown:** UCS 10, 11, 22, 68, 85, 105, 532, 814, 824 Definition 22
- **Why:** The indented sub-entry 'Definition 22' is merged onto its parent's line, losing the index hierarchy and making 22 read as another page locator. Index only.
- **Repair:** —
- **Verifier:** Rendered PDF page 950 (printed page 926, index letter U, left column) shows 'UCS  10, 11, 22, 68, 85, 105, 532, 814, 824' on one line, then 'Definition  22' indented on the next line as a sub-entry. page-950.md line 37 reads 'UCS 10, 11, 22, 68, 85, 105, 532, 814, 824 Definition 22' on one line, with no line break and no &emsp; indent. The neighbouring entries keep both: 'UCS-4 phrase\' is followed by '&emsp;SPECIAL-NAMES paragraph 232'. So the sub-entry is merged into its parent's locator list, and the index hierarchy is lost. The claim quotes both sources correctly. The defect is in the index only and has no normative effect, so the structural severity is right.

### p950-2 · p950 · misplaced-content · **structural**

- **PDF:** UNTIL phrase
    PERFORM statement  544
UP phrase
    SET statement, index arithmetic  573, 575
    SET statement, pointer arithmetic  574
UPON phrase
    DISPLAY statement  487
    SUM clause  391
UPPER-CASE function  731
- **Markdown:** UNTIL phrase

   PERFORM statement 544 UP phrase

   SET statement, index arithmetic 573, 575\
   SET statement, pointer arithmetic 574\
   UPON phrase\
   DISPLAY statement 487\
   SUM clause 391\
   UPPER-CASE function 731\
- **Why:** The whole right column (PERFORM statement 544 through UTF-8 / SPECIAL-NAMES paragraph 232) is flattened to one uniform 3-space indent, so main entries and sub-entries are indistinguishable, and the sub-entry 'PERFORM statement 544' is run together with the next main entry 'UP phrase'. Index hierarchy lost; index only.
- **Repair:** —
- **Verifier:** In the rendered PDF page (printed page 926), 'UNTIL phrase' ends the left column. The right column starts with the indented sub-entry 'PERFORM statement 544', then the main entry 'UP phrase' at the margin, followed by its indented sub-entries. Main entries (UPON phrase, UPPER-CASE function, Uppercase letters, USAGE clause, ... UTF-16, UTF-8) sit at the margin, and their sub-entries (DISPLAY statement, SUM clause, SET statement 574, CLASS-ID paragraph, CALL statement, SPECIAL-NAMES paragraph, ...) are indented. In page-950.md, every line from 'PERFORM statement 544' to the final 'SPECIAL-NAMES paragraph 232' has the same 3-space indent and no &emsp; markers, unlike the rest of the page. As a result, main entries and sub-entries can no longer be told apart, and 'PERFORM statement 544 UP phrase' is merged into one line. Both sources are quoted correctly. No text or page number is missing, so this is a structural fault in the index only, with no normative effect.

### p951-2 · p951 · misplaced-content · **structural**

- **PDF:** Weakly-typed items  132, 806
Week date format  630
Week-of-year subfield  630
WHEN phrase
    EVALUATE directive  52
- **Markdown:** Weakly-typed items 132, 806 Week date format 630 Week-of-year subfield 630 WHEN phrase

   EVALUATE directive 52\
- **Why:** Four main entries are run together on one line, and from 'EVALUATE directive 52' through 'WRITE statement 233, 619' every line (main entries such as 'WHEN SET TO FALSE phrase', 'WITH LOCK phrase', 'Word 76' and their sub-entries alike) is flattened to one uniform 3-space indent, so the W-section index hierarchy is lost. Index only.
- **Repair:** —
- **Verifier:** Rendered page 951 (printed 927) at 300 dpi. The PDF's W index section prints 'Weakly-typed items 132, 806', 'Week date format 630', 'Week-of-year subfield 630' and 'WHEN phrase' as four separate flush-left main entries, then two levels: main entries flush left (WHEN SET TO FALSE phrase, WHEN-COMPILED function 733, WITH DUPLICATES phrase, WITH LOCK phrase, ..., Word 76, Working-storage section 275, WRITE statement 233, 619) and indented sub-entries under them (EVALUATE directive 52, CLOSE statement 479, and so on). In page-951.md the first four are joined into one paragraph line, and every line from 'EVALUATE directive 52\' to 'WRITE statement 233, 619' carries the same 3-space indent, so main entries and sub-entries look identical. The Y and Z sections on the same page keep the hierarchy (with &emsp;), which shows the W section has a transcription defect and not a style choice. No text is missing, but the parent of each sub-entry (for example, which phrase 'CLOSE statement 479' belongs to) can no longer be read reliably from the markdown. The claim quotes both sources accurately. It is a real defect, structural, and affects the index only (no normative content).

### p219-1 · p219 · ocr-error · **cosmetic**

- **PDF:** b) If the parameter or returning item in the invoked method is described with an interface-name,
- **Markdown:** b) Ιf the parameter or returning item in the invoked method is described with an interface-name,
- **Why:** The first letter of 'If' in item 2)b) is the Greek capital iota (U+0399), not the Latin 'I'. The meaning is unchanged, but a text search for 'If the parameter' misses the line.
- **Repair:** —
- **Verifier:** Confirmed mechanically: line 17 of page-219.md begins '   b) ' followed by code point 0x399 (Greek capital iota) then 'f'. The printed page (PDF p.219, printed 195, exact-match rule 2)b)) shows an ordinary Latin 'If'. This is a genuine character-level transcription defect, not reflow or furniture. Meaning for a human reader is unchanged, so severity cosmetic is right, but the homoglyph breaks exact text matching, including scripts/spec/cite.py --check quoting this sentence. Side note: the odd wording 'returning item in is described' and 'interface- name' are in the PDF itself, so they are not transcription defects.

### p662-1 · p662 · ocr-error · **cosmetic**

- **PDF:** LOWER-CASE | Alph1 or Anum1 or Nat1 | Depends upon argument^a (superscript footnote marker a)
- **Markdown:** | LOWER-CASE | Alph1 or<br>Anum1 or<br>Nat1 | Depends<br>upon<br>argumenta |
- **Why:** The superscript footnote reference 'a' (pointing to Table 22's footnote 'A function that has only alphabetic arguments is type alphanumeric.') is flattened into the word, producing 'argumenta'; the link from the Type cell to the footnote is no longer marked.
- **Repair:** —
- **Verifier:** The 300 dpi render of PDF page 662 (printed page 638, Table 22 continued) shows the LOWER-CASE row's Type cell as 'Depends / upon / argument' followed by a raised, smaller 'a', which is a footnote reference marker. Line 19 of page-662.md reads 'Depends<br>upon<br>argumenta'. The superscript is merged into the word, so the transcription contains a word the standard does not have and loses the footnote reference. The quotes in the claim match both sources. This changes no normative rule, so 'cosmetic' is the right severity. The same flattening appears in the REVERSE, TRIM and UPPER-CASE rows of the 2014 markdown (specs lines 25932, 25955, 25956), but those rows are on other pages and this verdict does not cover them.

### p664-1 · p664 · ocr-error · **cosmetic**

- **PDF:** REVERSE | Alph1 or Anum1 or Nat1 | Depends upon argument^a (superscript footnote marker a)
- **Markdown:** | REVERSE | Alph1 or<br>Anum1 or<br>Nat1 | Depends<br>upon<br>argumenta |
- **Why:** The superscript footnote reference 'a' is flattened into 'argumenta', losing the visible link to Table 22's footnote.
- **Repair:** —
- **Verifier:** Rendered page 664 (printed p. 640, Table 22 continued) at 300 dpi: the REVERSE row's Type cell reads 'Depends / upon / argument' with a clearly raised, smaller superscript 'a' after 'argument'. No other row on this page (RANGE 'argument', SUM 'arguments') carries a marker. page-664.md line 10 has 'Depends<br>upon<br>argumenta', fusing the footnote marker into the word, so the transcription shows a nonword and loses the footnote reference. The quotes in the claim match both sources. This is not a wrap or furniture difference: the token is wrong and the reference to the table footnote is gone. It is cosmetic, because the footnote's text lives elsewhere and the row's type ('depends upon argument') can still be recovered.

### p810-2 · p810 · misplaced-content · **cosmetic**

- **PDF:** Increment index-name-1 (for a different table) or identifier-2   * (an asterisk beside the box, marking it optional)
- **Markdown:** Increment

### \*

   index-name-1\
- **Why:** The asterisk mark beside the last flowchart box became a level-3 markdown heading ('### \*') that splits the box label 'Increment index-name-1' in two and inserts a bogus heading into the document outline.
- **Repair:** —
- **Verifier:** Printed page 810 (2014 p.786, Figure D.1): the last flowchart box reads 'Increment / index-name-1 / (for a different / table) / or identifier-2' with a lone '*' to its right, the footnote mark meaning the operation is optional. The markdown renders that box as 'Increment', then a level-3 heading '### \*', then 'index-name-1\ (for a different table) or identifier-2'. Both quotes are accurate. No text is lost (the asterisk and the whole label survive), so the meaning of the figure is recoverable, but the transcription injects a spurious heading into the document outline and splits the box label across it. That is a real structural transcription defect beyond wrapping; it has no normative effect (informative Annex D flowchart), so cosmetic is the right severity.

## Refuted — deliberately NOT repaired

- p51-2 · misplaced-content — Printed page 27 (PDF p51), 5.5 rule 2) b): items '1. an integer intrinsic function,' and '2. a fixed-point numeric data item, other than an intrinsic function, whose description does not include any digit positions to the right of the radix
- p52-1 · incorrect-text — The printed page 52 (Table 1, Verbal forms, in clause 5.8) shows each equivalent expression on its own line: 'there is a possibility of' / 'it is possible to', 'it is recommended that' / 'ought to', and so on. The markdown keeps every word 
- p53-1 · misplaced-content — Printed page 53 (2014 p.29, 6.2.1) shows a three-row table: '*' comment indicator / a comment line; '/' comment indicator / a comment line with page ejection; '- (hyphen)' continuation indicator / a continuation line. The markdown keeps the
- p54-1 · misplaced-content — The rendered PDF page 54 (printed p.30) opens with the carried-over last row of the 6.2.1 fixed-indicators table: 'space' | 'source indicator' | 'any line that is not a comment line or a continuation line', with no column headings on this p
- p54-2 · misplaced-content — Checked the 300 dpi render of PDF page 54 (printed p.30, 6.2.2). The table has three entries: *> comment indicator (items 1 and 2), >> compiler directive indicator, and "- / '- literal continuation indicator. The markdown keeps every word o
- p60-1 · misplaced-content — Checked against the printed page (PDF p60, printed page 36, §2014:7.1). The printed page has two plain two-column lists, each under an underlined header: 'Compiler directing statements / Stage' (COPY statement, an indented SUPPRESS phrase, 
- p79-1 · misplaced-content — The printed page 79 (PDF p.55) does indent '1.'/'2.' one level under b) and d), and the markdown places them at the same 3-space indent as the lettered items. But every word is present, in order, and correctly placed: each numbered item imm
- p80-1 · misplaced-content — The 300 dpi render of PDF page 80 (printed page 56) shows item '2.' indented one level deeper than e) and f), and the markdown puts it at the same 3-space indent as e). That much of the claim is accurate. But the text of item 2 is present w
- p94-1 · table-restructured — Printed page 94 (p.70, Table 2) shows the Basic letters row with two stacked character groups (capitals, then small letters) and two stacked meanings aligned with them. Markdown line 27 holds every word of both: the capitals, <br>, the smal
- p98-1 · misplaced-content — The printed page (PDF p98, printed page 74, §2014:8.2 Locales) shows the LC_ALL 'Behavior affected' cell wrapping onto a second printed line: 'LC_NUMERIC, and LC_TIME, and any other categories included in the locale.' page-98.md lines 15-17
- p99-1 · table-restructured — Rendered page 99 (printed p.75) at 300 dpi. The printed 8.2.1 list is an unruled layout: a bold header line, category names (LC_MONETARY, LC_TIME) on their own lines, then indented field-name/description pairs. The markdown holds every one 
- p99-2 · misplaced-content — The printed page 99 (8.2.1 Locale field names, PDF p.75) shows the p_cs_precedes description wrapping onto a second line, 'a nonnegative quantity'. The markdown (page-99.md lines 33-35) carries every word, in order: the table cell ends at '
- p140-2 · misplaced-content — On printed page 140 (sheet 116), items 1. and 2. sit one indent level deeper than a) and b) under rule 3) b) of 8.4.5.1. The markdown (page-140.md lines 36-42) gives a), b), 1\. and 2\. the same 3-space indent, so the claim describes the di
- p161-1 · misplaced-content — The rendered PDF page 161 (printed p.137, 8.7.2) shows the B-XOR meaning 'Exclusive OR operation (boolean exclusive' wrapping onto a second line, 'disjunction)', inside the same table cell. The markdown (page-161.md lines 28-30) ends the ta
- p194-1 · misplaced-content — Printed page 170 (PDF p194), section 8.10, shows the EXPANDS row with its context cell wrapped over two lines: 'class-specifier and interface-specifier of the REPOSITORY' / 'paragraph'. The markdown keeps every word in the same order: the r
- p194-2 · misplaced-content — The printed page (2014 p.170, §8.10 table) shows HIGH-ORDER-LEFT and HIGH-ORDER-RIGHT each with the context 'FLOAT-BINARY clause, FLOAT-DECIMAL clause, and USAGE clause', where the cell wraps onto a second line 'USAGE clause'. page-194.md l
- p204-1 · misplaced-content — The rendered page (printed p.180, §2014:9.1.12) shows a plain indented two-column list of seven exception-name/digit pairs with no table rules. The markdown carries all seven pairs, each name paired with its correct digit ('1','2','3','4','
- p234-1 · table-restructured — The printed page 234 (PDF folio 210) shows one plain two-column list under 'Term / Subclause' with eight rows: class-id 11.3, factory 11.4, function-id 11.5, interface-id 11.6, method-id 11.7, object 11.8, options 11.9, program-id 11.10. Th
- p265-1 · table-restructured — Printed page 241 (PDF p265), syntax rule 18: an unruled two-column layout. The heading line 'DYNAMIC LENGTH STRUCTURE phrase   value' is set apart from the four data rows by a blank gap, and the markdown keeps it the same way, as a line of 
- p283-1 · misplaced-content — The observation is accurate but it is only indentation. The printed page (ISO/IEC 1989:2014 p.259, PDF p.283) indents items 1. and 2. one level deeper than a) and b). In page-283.md lines 23-29, a), b), 1\. and 2\. all carry the same 3-spac
- p313-1 · table-restructured — The printed page (ISO/IEC 1989:2014 p.289, PDF p.313) shows one borderless two-column Term/Subclause list of nine rows; the occurs-clause subclause text wraps in the PDF itself at 'dynamic-capacity-' / 'table-format)'. The markdown carries 
- p571-1 · misplaced-content — Rendered page 571 (printed 547) at 300 dpi. The printed page indents steps 1.-4. under c), and the NOTE further under step 4. The markdown has every word of c), 1.-4. and the NOTE, in the same order, verbatim. The only difference is indenta
- p572-1 · misplaced-content — Printed page 548 (PDF p572) and page-572.md agree word for word: steps 1 and 2, 'If the current condition is true:' a.-d., 'otherwise:' a.-b. with their em-dash actions, the NOTE, the 'During the execution...' paragraph and rule 10). The on
- p629-1 · incorrect-figure — The printed page (PDF p629, printed 605) shows 'UNLOCK file-name-1' with UNLOCK underlined, followed by a square bracket that stacks RECORD over RECORDS, with no choice-indicator bars. The markdown keeps every element: UNLOCK is underlined,
- p698-1 · table-restructured — The printed page (PDF p698, printed 674, §2014:15.39.3 NOTE) shows a two-line bold column header 'Argument-1 / characteristics' over the column S999, S9(4) BINARY, 99V9(3), $**,**9.99BCR, $**,**9.99, with 'Value returned' over +999, +9999, 
- p699-1 · table-restructured — The rendered page 699 (printed 675) holds only two lines: 'BINARY-CHAR SIGNED  +127 (assuming an 8-bit representation)' and 'BINARY-CHAR UNSIGNED  +255 (assuming an 8-bit representation)'. The markdown has every word and value in the same o
- p715-1 · misplaced-content — The 300-dpi render of printed page 691 (PDF p715, 15.54.3 NOTE) shows a two-line bold column heading 'Argument-1 / characteristics' above 'Value returned'. The markdown quotes are accurate: 'Argument-1' sits on its own line directly above a
- p716-1 · misplaced-content — The printed page 716 (rendered at 300 dpi) holds only three two-column rows continuing the LOWEST-ALGEBRAIC example table: '$**,**9.99 | 0', 'BINARY-CHAR SIGNED | -128 (assuming an 8-bit twos-complement representation)', 'BINARY-CHAR UNSIGN
- p789-1 · misplaced-content — The printed page 789 (printed folio 765) shows a two-column layout: the header 'Script or / character / classification' on three lines with 'Letters or range of letters', then 'Latin:' followed by two wrapped lines of ranges. The markdown k
- p790-1 · misplaced-content — Checked the 300-dpi render of PDF page 790 (printed p.766, Annex repertoire table). In the PDF each combining row label wraps onto a second line ('Hebrew' / '(combining):', 'Arabic:' / '(Combining', etc.). The markdown keeps every word and 
- p791-2 · misplaced-content — The printed page (p791, printed 767) wraps the Ethiopic list at '128A-' / '128D' because of a line break. Markdown lines 57-59 contain every range in the same order: '... 1288, 128A-' in the table cell, then '128D, 1290-12AE, ... 1348-135A'
- p792-1 · misplaced-content — The printed page (2014 p.768, PDF p792) shows the labels 'Khmer:' and 'Mongolian:' with '(combining)' wrapped onto a second line of the label cell, beside 17B4-17D3 and 18A9. The markdown keeps every code range in the correct row and puts '
- p792-2 · misplaced-content — The printed page (2014 p.768, PDF page 792, Annex B.3.1) sets the label on two lines: 'CJK Unified' on the first line and 'Ideographs:' on the second, with the code-point ranges beside the second line. The markdown keeps both words in order
- p792-3 · misplaced-content — Printed page 768 (PDF p792) shows the Digits entry as a three-line wrapped value: '0030-0039, ... 0B66-0B6F,' / '0BE7-0BEF, ... 0F20-0F29, 1040-' / '1049, 1369-1371, 17E0-17E9, 1810-1819'. The markdown (page-792.md lines 28-30) carries ever
- p866-1 · image-lost — The 300 dpi render of printed page 842 (PDF p866) does show Figure D.4 as three boxes stacked vertically, Person, Employee and Manager, with filled arrowheads pointing up from Manager to Employee and from Employee to Person. The claim descr
- p893-1 · incorrect-figure — Rendered PDF page 893 (printed p.869) shows Figure D.10. The claim describes its edges correctly: c1 yes and c2 yes go to Evaluate condition-3; c2 no, c3 yes and c4 yes go to 'Truth value is false'; c4 no goes to 'true'. The markdown keeps 
- p907-1 · misplaced-content — The claim describes the markdown accurately. On the printed page (PDF p907, printed 883) the first-column header of each table is wrapped over two lines: 'Integer-seconds' / '  time formats' and 'Fractional-seconds' / '    time formats'. Th
- p915-1 · table-restructured — Printed page 915 (folio 891, Annex E item 18) shows an unruled, borderless two-column list: header 'New exception-names' / 'Exception-names in the previous COBOL standard', then three rows EC-FUNCTION-NOT-FOUND/EC-PROGRAM-NOT-FOUND, EC-FUNC
- p925-1 · misplaced-content — The rendered page 925 (printed 901) does end after 'This error has not been fixed because doing so may cause unacceptable incompatibilities.' and the markdown carries all of its text. But '# Bibliography and index' is not page content mispl
- p927-1 · misplaced-content — The rendered page (printed page 903, top of the right column) has three index lines: 'Abbreviated combined relation conditions 165', 'Abnormal run unit termination 438, 440', 'ABS function 642'. page-927.md holds them as one line with no '\
- p932-1 · misplaced-content — The printed page (PDF p932, printed index page 908, left column top) shows two lines: 'Constant conditional expression 48' and 'Constant entry 282'. The markdown line 3 reads 'Constant conditional expression 48 Constant entry 282': every wo
- p934-1 · misplaced-content — The printed page (PDF p934, printed folio 910, informative index) shows EC-FLOW-SEARCH exception 442, 583 and EC-FLOW-USE exception 442, 611 as two consecutive lines at the top of the left column. Line 3 of the markdown holds both entries o
- p935-1 · misplaced-content — Printed page 911 (PDF p935), left column top, shows the eleven entries EC-STORAGE-IMP 445; EC-STORAGE-NOT-ALLOC 445, 502; EC-STORAGE-NOT-AVAIL 445, 470; EC-USER 425, 439, 446, 499, 500, 825; EC-VALIDATE 446, 617; EC-VALIDATE-CONTENT 446, 61
- p936-2 · incorrect-text — The printed page (index p.912, PDF p.936, right column) shows 'FILE STATUS clause 258', an indented sub-entry 'FILE-CONTROL paragraph 248', then the headword 'FILE-CONTROL paragraph 248'. The markdown (page-936.md lines 109-111) carries all
- p937-1 · misplaced-content — Checked the 300-dpi render of PDF page 937 (printed page 913, informative index) against page-937.md. Every index term and every page number on the page is present in the markdown, in the printed order: the left column (Sequential 177 throu
- p943-1 · misplaced-content — Printed page 943 (folio 919, informative index) left column reads 'In complex conditions 164', 'In negated conditions 164' (indented), 'NOT phrase', 'EVALUATE statement 493' (indented), 'NULL', 'Predefined object 107' (indented), and so on.
- p945-1 · misplaced-content — Both quotes are accurate. The printed page (index p. 921, PDF p. 945) shows the indented sub-entry 'SPECIAL-NAMES paragraph 231', then the headword 'Pointer 473', the indented sub-entry 'Initialization 419, 470' and 'Pointer class 129'. The
- p947-1 · misplaced-content — Printed page 947 (folio 923) is index text. In the PDF's left column, sub-entries such as ALLOCATE/CALL/INVOKE under RETURNING phrase and ADD/COMPUTE/... under ROUNDED phrase are indented. The markdown gives every line before the S heading 
- p949-1 · misplaced-content — Printed page 925 (PDF p949) shows the six index entries at the top of the left column, one per line. page-949.md line 3 holds every word and every page number of all six entries, in the same order, with no omission or substitution. The only
- p951-1 · misplaced-content — The printed page (index p.927, PDF p.951) shows 'VARYING phrase' with indented sub-entries PERFORM statement 545, RECORD clause 374, SEARCH statement 568. Markdown line 3 holds every word and every page number in the same order. Nothing is 
- p122-1 · misplaced-content — The printed page (p.98 of the 2014 PDF, page 122) shows general rule 2 of 8.4.1.2.3 as one paragraph. The markdown carries every word of it in order: the blank line after 'The highest permissible' and the hard line breaks only change line w
- p191-1 · table-restructured — The 300-dpi render of printed page 167 (PDF page 191) shows the third column of the 8.9 list ending FLOAT-BINARY-32, FLOAT-BINARY-64, FLOAT-BINARY-128, FLOAT-DECIMAL-16. The first and second columns are one line shorter (they end at CHARACT
- p192-2 · table-restructured — The printed page (ISO/IEC 1989:2014 p.168, PDF p192) is an untabled three-column word list; column two reads MODE, MOVE, MULTIPLY, blank, NATIONAL. The markdown (page-192.md lines 29-36) keeps MULTIPLY, in order, between the MOVE row and th
- p391-1 · ocr-error — Quotes are accurate: page-391.md lines 9, 18 and 24 carry the word-reversed/scrambled rotated row-group labels, and the printed page (367) shows them rotated as 'Simple, special, and fixed insertion symbols', 'Zero-suppression and floating 
- p486-2 · table-restructured — The printed page (p486, folio 462) shows each GR 8-11 table as a bold 'Character Positions   Contents' heading line followed by position/contents rows. The markdown carries the same heading text ('Character Positions Contents') immediately 
- p537-1 · table-restructured — The rendered 300-dpi page 537 shows one carried-over table row, 'Program-pointer' and 'Predefined address NULL', in two columns with no rules. The markdown keeps both cells word for word on one line in the same order. Only the column spacin
- p663-1 · ocr-error — Rendered PDF p663 (printed 639) shows the Type cell of MAX and MIN as 'Depends upon arguments' with a superscript 'a'. Markdown lines 7 and 11 read 'Depends<br>upon<br>argumentsa'. Both sources are quoted accurately. The footnote marker 'a'
- p665-1 · ocr-error — Printed page 641 (PDF p665), Table 22 continued: the TRIM and UPPER-CASE Type cells read 'Depends upon argument' followed by a superscript 'a', and the table's last row is the footnote starting with a superscript 'a': 'A function that has o
- p756-2 · misplaced-content — Printed page 732 (PDF p756), 15.91.3 1c: the PDF shows '(FUNCTION SUM (', '((argument-1_1 – FUNCTION MEAN (argument-list)) ** 2)', '...', '((argument-1_n – FUNCTION MEAN (argument-list)) ** 2)) / n)', set in deeper display indentation. page
- p944-1 · misplaced-content — The printed page (index p.920, PDF p.944, right column under the 'P' letter heading) shows 'P  PICTURE symbol 354' and 'p_cs_precedes 75' on two lines. The markdown (page-944.md line 60) carries every word and page number of both entries, i

