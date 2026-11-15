## Entry 3694 — 2026-11-15 12:06 PST — PB5600: FLAG-85 and FLAG-NATIVE-ARITHMETIC are real directives with a warning mechanism

At `--std 2002` the two directives were recognized and ignored: `>>FLAG-85 ALL ON JUNK` compiled clean and `>>FLAG-85 NUMVAL ON`
flagged nothing (re-probed on the fresh Debug build before any change: A B rc 0, and `FUNCTION NUMVAL` printing 3 with no warning).
Their construct rows said `form: text`, "content unchecked by decision", because the 2023 text has no format for a removed
directive. The 2002 and 2014 texts print both formats (§2002:7.2.11.1, §2002:7.2.12.1; rendered PDF pp. 71 and 73 for the
un-underlined ALL and ON, so each is the default of its braces), so no decision licensed ignoring them.

The fix adds two values to the one flagging subsystem instead of a second mechanism: `FlagDirective.Flag85` and
`FlagDirective.FlagNativeArithmetic`, ten and one `FlagOption`s, one `FlagDirectiveLine` parse (FLAG-NATIVE-ARITHMETIC takes only
ON/OFF), the existing `FlagDirectiveProcessor` / `FlagState`, and a new partial of `FlagConformancePass` with a detector per GR4
sub-rule (b CORRESPONDING, c DE-EDITING, d DIVIDE, e FUNCTION-ARGUMENT, f MOVE, g NUMVAL, h SET, i/j STANDARD-1/-2, k ZERO-LENGTH)
and one for native arithmetic under `ARITHMETIC IS STANDARD` (unit or method OPTIONS; arithmetic statements, SUM clause and
arithmetic expressions with an operator, operands of binary-char/-short/-long/-double or float usages). The rows became
`stage` rows (owner `FlagDirectiveProcessor`) with a `directivePlacement`, so the omitted-branch syntax check and the SR1
placement warning apply by data. The two warnings are COBOL7390 and COBOL7391; COBOL7392 was returned. The one place that
spelled "FLAG-02 else FLAG-14" now asks `FlagDirectiveLine` (`OfWord`, `Row`, `WarningCode`, `ConstructId`, `FormatCitation`).

Both rows are now carried by `DirectiveStateRegistry` (the timeline replay needs it); PUSH exists only at 2023 and the
directives were removed there, so `DirectiveStateStackTests` drives the stack over the stage's events directly for them.
Goldens: an accepted program and a refused one for each directive (`cases/7.2.11@2002-FLAG-85/`,
`cases/7.2.12@2002-FLAG-NATIVE-ARITHMETIC/`, `wiseowl/diagnostics/pb5600-*.json`), and `Flag85DirectiveTests` (59 cases: parse,
fold, a positive and a control statement per option, the single warning per statement, native arithmetic ON/OFF/default/native
mode, fixed-form SR3). Rows closed (3, GAP 2497 to 2494): 2002-SR-7.2.2-3, 2002-SR-7.2.2-4, 2002-GR-7.2.3-4.

Self-review found one defect of mine, a text-mode Python rewrite that left CRLF in eleven LF files (it surfaced as four
DEFINE/PUSH tests failing on `\r\n` in a raw string); every touched file was converted back and the diff re-measured.

