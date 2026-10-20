## PB3466 discharged: the BYTE-LENGTH "parse error" was text past margin R

Wave 1135 A took PB3466, filed from the PB3441 implementer report: `DISPLAY FUNCTION BYTE-LENGTH (G1) " " FUNCTION
BYTE-LENGTH (K1)` was said to fail to parse when BYTE-LENGTH is the last operand, while the same line with LENGTH
parses. The note suspected the function-call rule in `CobolExpressions.g4` and the constant entry's
`constantByteLengthWord`.

**The re-probe says otherwise.** The repro is fixed-form source and its DISPLAY line is 74 characters long; the last
two, `1)`, sit in positions 73-74. §6.3.1 puts margin R at an implementor-defined position immediately right of the
program-text area, and this compiler's is after position 72 (`docs/CONFORMANCE.md` DOC-A.1-158: text beyond it is not
program text). The parser therefore sees `FUNCTION BYTE-LENGTH (K` with no closing parenthesis and correctly reports
a parse error at the next statement. LENGTH is five characters shorter, so its line ends inside the program-text area.
The function name plays no part.

The probes, on this worktree's Debug build: the T13 line is refused in fixed form; the same file under
`--source-format free` parses; the same two BYTE-LENGTH calls wrapped onto two lines inside positions 8-72 parse and
display `3 1`; a LENGTH form padded to 73 characters is refused the same way; and a token and parse-tree dump of the
raw text (no margin) parses the DISPLAY with both function calls. Twelve single-call and multi-call variants inside the
margin all parse.

No code changed. The note is retired unlanded with `closes_rows: []` and a reason; it claimed no inventory row. The
margin is already witnessed by `l1_margin_r_program_text_area`, so no golden was added. The gate's verdict line is in
the wave report.
