## A line that begins `>>` is text in 1985 pseudo-text (PB5803)

REPLACE and COPY REPLACING pseudo-text that held a line beginning `>>` was refused at `--std 85` (COBOL2572, COBOL2697, and COBOL2449 for COPY), because two places made such a line a compiler directive at every edition: `TextWordScanner` (a `DirectiveLine` word) and the directive loop of `ConditionalCompilationProcessor` (the SR8 b and SR10 refusals). COBOL-85 has no compiler directives, so `>>XYZ` there is a text word (`§1985:III.2` item 3) that pseudo-text-1 and pseudo-text-2 may hold (`§1985:XII.3.3` SR3, SR4).

Re-probe: `rp_pt1`, `rp_pt2` and `cp_pt1` exited 65 at 85 before; now they run (`[OK  ]`, `[OK  ]`, `[LIB ]`) and stay refused at 2023.

Fix: one predicate, `EditionInfo.HasCompilerDirectives` (2002 on). Every `TextWordScanner` call now takes it (`directiveLines`), so the scanner cannot be edition-blind again, and at 85 the directive loop treats a `>>` line inside the pseudo-text of an open COPY or REPLACE as text. A `>>` line outside pseudo-text at 85 still goes the old way (COBOL0900 names the directive, PB725). The logical-conversion division readers take the answer from the compilation's `ReferenceFormatDiagnostics`.

Goldens: `pb5803-replace-pseudo-text-1-line-initial-gt-at-85` (a match, `[HIT ]`), `pb5803-replace-pseudo-text-2-line-initial-gt-at-85`, `pb5803-copy-pseudo-text-1-line-initial-gt-at-85`; with the predicate forced true each exits 65. Rows `1985-SR-XII.3.3-3` and `-4` are KEPT bridges owed a code read, so the landing closes none.
