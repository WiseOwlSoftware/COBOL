## PB5472: a debugging line in pseudo-text-2 that replaces a word mid-line no longer hides the library text before it

At --std 85 with WITH DEBUGGING MODE, `COPY LQ19 REPLACING ==XX== BY ==` followed by a `D` line holding `QQ==` into the library line `DISPLAY "[" XX "]".` was refused COBOL7061, though the program is correct with and without the debugging line (§1985:VI.7.3, §1985:XII.2.4 GR7). The same text written by hand compiled.

Re-probe: reproduced (exit 65). The note blamed the origin `OriginWriter.Append` gives a replacement. A dump of the resultant text showed the real cause: COPY keeps pseudo-text-2's debugging carrier, and `ApplyReplacements` wrote it in the middle of the library line, so the line read `DISPLAY "[" <carrier> QQ`. The lexer makes every token after a carrier on its line debugging text, and the COBOL7061 check hides by resultant line, so `DISPLAY "["` was hidden with `QQ`. The text after the match already started a line of its own; the text before it did not.

Fix: a replacement that begins with a carrier after non-blank text of its line starts a resultant line (`CopyProcessor.ApplyReplacements`). The carrier is dropped wherever the replaced line decides, so this is the only place it can land mid-line. LEADING and TRAILING cannot reach it at 85.

Goldens: `pb5472-copy-debugging-line-replaces-mid-line` (accept 85, `[Q]`), and the refusal `pb5472-copy-debugging-line-replacement-needed` (the debugging line holds the MOVE operand, COBOL7061 stays). Rows: `1985-GR-XII.2.4-7` PARTIAL to CONFORMS (GAP 178 to 177). Self-review: the origin the replacement carries is unchanged (PB5313's area); no allocation on a hot path (one blank test, only when a carrier leads the replacement).
