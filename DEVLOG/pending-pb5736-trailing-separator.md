## A semicolon ending a compile-time directive operand is refused like its comma twin

kb/Work PB5736. The cce fragment lexer hides a `;` with no space after it (`SEMICOLON`, channel HIDDEN, so SeparatorRule can see it), and the fragment grammar never looks at hidden tokens, so `>>IF 1 = 1;`, `>>EVALUATE 1;`, `>>WHEN 1;` and `>>DEFINE X AS 1;` compiled while `>>IF 1 = 1,` was COBOL1619 and `>>ELSE;` was COBOL1911.

Re-probe on this build (main 9cb3484ca6) reproduced every row of the note at `--std 2002` and `--std 2023`. Reading decided once: a directive "may be followed only by space characters and an optional inline comment" (§2023:7.3.3 SR3, cite.py OK), and a trailing comma or semicolon is neither, which is the reading `>>ELSE;` already took. A `; ` inside an operand stays the separator space (§2023:8.3.5 SR2) and is unchanged.

Fix at the one funnel: `FragmentParse.Parse` takes `trailingSeparatorIsError`, and a hidden `SEMICOLON` as the last token before EOF returns null (the "malformed" result every caller already reports as COBOL1619). `DirectiveExpressionFragment.Parse` sets it for all three fragment kinds (cce, operand, DISPLAY), so the DEFINE and DISPLAY siblings are fixed with the same change. Re-probe afterwards: IF, IF with trailing space, EVALUATE, WHEN, EVALUATE TRUE and DEFINE with a trailing `;` all COBOL1619; `>>IF 1 = 1; AND 2 = 2` still accepted.

Goldens: `pb5736-if-directive-trailing-semicolon` and `pb5736-evaluate-when-trailing-semicolon` (refused at 2002, 2014, 2023), `pb5736-if-directive-trailing-comment` (accepted at 2002, prints A). Rows SR-7.3.16.3-2 and SR-7.3.13.3-4 PARTIAL to CONFORMS (GAP 4 to 2, the two 2002 rows chain to them).
