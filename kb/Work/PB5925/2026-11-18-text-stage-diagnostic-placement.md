## Lead: a text stage's diagnostic in the second copy of a copybook is placed in the first block (found by listing step 3)

The listing places a diagnostic by the resultant line its location was located from, but only when that location was
located through the LISTED pass's line map (`SourceLocation.Frame` equals the map's `Frame`; DESIGN-listing.md section 6.3).
The parser's, the lexer's, the post-parse screens' and every bind diagnostic qualify. A diagnostic a TEXT STAGE reports
(COPY, REPLACE, the directive stages, the reference-format conversion) is located through the stage's own numbering
(`MappedText.LocateAt`, or a `SourceLineMap` the stage builds, such as `Frontend.cs`'s directive-stage map), so it carries
another frame or none, and the listing places it on the FIRST block that lists its physical line. For a copybook copied
once, or for the source, that is the right line. For a copybook copied twice whose SECOND copy draws a text-stage
diagnostic (a REPLACE or COPY REPLACING error that only the second copy's operands produce, a directive inside the copybook
that only the second copy's conditional state reaches), the diagnostic is printed under the first copy's line.

Harm: misleads diagnosis (the summary names the right file, line and column; only the inline block is the wrong copy). No
wrong answer, no crash.

The structure that would make it automatic: carry the INCLUSION the text stage is expanding with the location, or map a
stage's resultant position to the final frame (each later stage already carries origins forward through `OriginWriter`),
so a text stage's location names the final frame's resultant line too. Either belongs with step 4a, which records
`Replaced` events in the same stages (`CopyProcessor.ApplyReplacements`, `ApplyReplaceStatements`) and so already threads
the inclusion through them. A witness: a copybook copied twice whose second COPY REPLACING produces a text-stage error, the
diagnostic expected in the second block.
