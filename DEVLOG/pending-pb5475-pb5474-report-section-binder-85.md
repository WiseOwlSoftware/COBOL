## PB5475 and PB5474: the 85 CODE clause reads a figurative constant by its value, and SUM UPON stays in its own report

Two defects of `ReportSectionBinder` found by the 1985 code read (read 4), one in each direction.

**PB5475 (rejected legal source).** `CODE ALL "AB"` was refused COBOL2713 as "not an alphanumeric literal": `BindCodeClause`
asked the literal's class and every figurative constant has none. §1985:IV.4.2.2.2.3 admits a figurative constant wherever a
format says literal and ALL literal unassociated "is the length of the literal"; §2023:8.3.3.6.3 SR1 says the same, and neither
CODE syntax rule bars it. New `CodeFigurativeCharacters` reads the figurative as the string it denotes (ALL literal-1 through
`ConcatFolder.FoldAllLiteral`; ZERO, SPACE, QUOTE, HIGH-VALUE, LOW-VALUE the one `ConcatFolder.FigurativeChar` character), then
`Screen85CodeLiteral` applies the two-character rule to that value: ALL "AB" passes, SPACE (one character) is refused by length.
The admission holds at every edition; a symbolic-character figurative stays refused (registered in the report).

**PB5474 (under-rejection).** `SUM ... UPON DET2 OF RPT2` from RPT's footing compiled and totalled RPT2's details at 85, where
§1985:XIII.3.19.3 SR2 requires a detail of the same report. `ResolveUponDetail` now resolves below 2002 against the footing's own
report alone, so an unqualified name two reports share is not falsely ambiguous, and a detail of another report is refused
COBOL2046 naming its report. The 2002 cross-report trigger of PB1294 is untouched.

Goldens: `pb5475_code_all_literal` (85, read back: AB / HELLO), `pb5475_code_figurative_one_character` (refused 85),
`pb5475_code_figurative_constant` (2002+, CODE SPACE, HELLO at position 2), `pb5474_upon_own_report_detail` (85, 00010 / 00005),
`pb5474_upon_detail_of_another_report` (refused 85). Rows `1985-SR-XIII.3.6.3-1` and `1985-SR-XIII.3.19.3-2` PARTIAL to CONFORMS
(GAP 178 to 176). docs/REPORT_WRITER_DESIGN.md updated.

The gate's first run was RED on three drift tests, each a real finding: `ReportSectionBinder` reached into `CodeGen.FigurativeConstants`
(a Binding to CodeGen pair the architecture edges refuse), so the fill character now comes from the Binding layer's own
`ConcatFolder.FigurativeChar`; `CODE NULL` had been refused only as a literal whose class is unknown, so `BindCodeClause` now
decides the `predefinedNull` arm through `PredefinedNullRule.Report` (COBOL2576, as every other literal decoder does); and the new
`DataBinder.LiteralEnv` reader is listed in `binder-member-readers.json`.
