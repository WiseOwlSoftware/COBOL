## The 1985 operand rules: integer operands, VALUE under an EXTERNAL record, the target-less GO TO and the segment-number ranges

Wave 20261973 group B, the version-conformance pass over the 1985 `--std=85` triage notes. Every note was re-probed on
7f4f67c7f1 with this tree's `cobol.exe`; each reproduced.

**PB5074 (landed).** At `--std 85`, `ELT (IDX)` over `PIC 9V9`, `ELT (-1)`, `ELT (1.5)`, `DISPLAY 1.5`, `SET IX UP BY N` over
`PIC 9V9`, `SET IX TO -1` and `SET IX TO 0`, `STOP 1.5`, `STOP -1` and `STOP ALL "A"` all compiled, and `STOP SPACE` and
`STOP ZERO` communicated the words. One mechanism: the 1985 general-format `integer` (§1985:III.2, "neither signed nor zero
unless explicitly allowed by the rules for that format") was never asked, only the operand's shape. It now has one reader,
`Cobol85OperandForms.IsInteger` (Frontend.Parsing, beside `GoToFormats`, so the edition gate and the binders share it with no
Binding-to-Validation edge; the gate's first run refused exactly those edges), which takes the sign each format admits
(`Cobol85IntegerSign`) and reads a sign the parser gave the unary operator when it touches the literal; the literal rule "an
unsigned integer" (DISPLAY SR2, STOP SR3) is `Cobol85OperandForms.IsUnsignedIntegerLiteral`. Where the 2002 text relaxes a rule, the fix is the edition window naming COBOL-2002 (COBOL0900):
the subscript row now covers §1985:IV.4.3.8.2.3 SR5 (the data-name's description, asked in `SubscriptBinder`, which asks the
parse arm's shape test first so one subscript draws one diagnostic) and SR6; the DISPLAY row, renamed
`display-numeric-literal-2002`, covers all of §1985:VI.6.10.3 SR2; the new row `set-arithmetic-expression-2002` covers
§1985:VI.6.23.3 SR2 to SR4 against the 2002 format's arithmetic expressions (VCR row 7.52 (c)). STOP literal is COBOL-85's
alone, so SR1 and SR3 are COBOL6817 there; its keyword figurative constant is now communicated as its character
(`ConcatFolder.FigurativeChar`). `edition_gates.py`'s `HELD_OPEN` loses the subscript row, which now rests on SR5 and SR6.

**PB5072 (landed).** A VALUE on an entry subordinate to `01 EXT-REC EXTERNAL` compiled at 85: the gate asked the VALUE's own
entry. The entries of a section are a flat list in the parse tree, so the walk decides the record once, at its level-01
(or 77) entry, and follows a level-01 REDEFINES to the record it redefines (§1985:VI.5.15.3 SR4's "redefinition"). Review
round 1 (R1-4) found the first shape, a backward scan of the section per VALUE clause, quadratic at every edition (20,000 VALUE
entries compiled in 60 s against 13 s on main); the per-walk state compiles them in 10-11 s, pinned by
`ExternalRecordValueGateTests`.

**PB5087 (landed).** A target-less `GO TO.` beside another statement compiled at 85. §1985:VI.6.15.3 SR3 is stated over the
statement sequence, so it is a row of `PlacementRules` (the funnel's sequence-placement rules, beside the bare EXIT's), with
COBOL6818; a row may now name its statement's construct row and is asked only where that row is available.

**PB5084 (in part; stays open).** The Segmentation module's three range rules are checked at 85 (COBOL6819). The MERGE
output-procedure flow rules and the independent-segment initial-state rules remain; the second contradicts VCR row 7.18's
documented posture and is flagged for an owner decision in the note.

**PB5069, PB5070 (not started).** Both need diagnostic codes beyond this wave's three; they return to the queue.

Goldens: `pb5074_*` (2002 to 2023) and `pb5074-*-at-85`, `pb5072_value_under_external_record_2002` and its refusal,
`pb5087_bare_go_to_single_statement_paragraph` (85) and its refusal, `pb5084_segment_numbers_in_range_85` and three refusals,
and `StopLiteralFigurativeTests` (the operator channel is standard error, which a `.out` does not read).
