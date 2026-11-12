## PB5214 PB5212: a COPY on a debugging line introduces debugging lines; the two rules of the 1985 debugging-line presentation

WIP checkpoint (the fleet's stop file appeared before the gate ran; the gate is still to run on this branch).

PB5214. Re-probe on the branch build: `p1a` printed COPIED-ON-DLINE without the mode. Fix at the one place text is introduced,
`CopyProcessor.ExpandCopiesOneLevel`: when the COPY begins on a carrier line (`LineIsDebugging`), each non-blank line of the
library text is written behind the carrier (`AppendAsDebuggingLines`, after nested expansion and REPLACING), and the text after the
statement's period on a debugging line keeps the carrier. `DebuggingLineRewriter` still makes the one decision. Goldens
`pb5214-debugging-line-copy-no-mode` and `-mode` (nested COPY, REPLACING, tail text).

PB5212. `DebuggingLineRewriter.Rewrite` now also names the lines that begin before the end of the unit's OBJECT-COMPUTER paragraph
(or, with none, before its Data or Procedure Division): COBOL7060, below COBOL-2002. A program that parses with kept debugging
lines is parsed again with them hidden (`Frontend.ParseCopyOf`, which replaces `ParsesCleanly` and also returns the first
offending token): COBOL7061, at the last kept line before the first syntax error. Goldens `pb5212-debugging-line-*`.

Gate: not yet run.
