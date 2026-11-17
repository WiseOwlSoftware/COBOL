- **D-85-REPLACE-DEBUGGING-LINES — a debugging line written in REPLACE pseudo-text-2 stays a debugging line at COBOL-85
  (kb/Work PB5887).** This entry speaks for COBOL-85. When the text a REPLACE statement replaces begins on a source line,
  a line pseudo-text-2 writes as a debugging line is a debugging line of the resultant program, so without WITH DEBUGGING
  MODE it is a comment line ("After all COPY and REPLACE statements have been processed, a debugging line will be
  considered to have all the characteristics of a comment line, if the WITH DEBUGGING MODE clause is not specified",
  `§1985:VI.7.3`). The 1985 text admits two readings. Reading P (kb/Work PB5382's): every line a replacement brings in is
  an introduced line, and "the indicator area of the introduced lines contains the same character as the line on which the
  text being replaced begins" (`§1985:XII.3.4 GR11`), so such a line becomes a source line and runs. Reading Q: that
  sentence speaks of the additional lines a placement creates; the COPY statement carries the same sentence
  (`§1985:XII.2.4 GR11`) beside "only those text words that are specified on debugging lines where the debugging line is
  within pseudo-text-2 appear on debugging lines in the resultant program" (`§1985:XII.2.4 GR9`), which reading P would
  contradict. REPLACE has no sentence of its own on a debugging line of pseudo-text-2: "Debugging lines are permitted in
  pseudo-text" (`§1985:XII.3.4 GR8`) only permits one. No 1985 sentence chooses between P and Q for REPLACE. Decided by the
  precedence of CLAUDE.md rule 1 (the text leaves the choice, GnuCOBOL decides): GnuCOBOL 3.2.0, measured
  (`-std=cobol85 -fixed`), prints `END` alone for kb/Work PB5887's `rep_dbg_ctl.cob` and `DIRECT-DLINE`, `VIA-REPLACE`,
  `END` with `-fdebugging-line`, so it keeps the line a debugging line: reading Q, the reading COPY's GR9 states for COPY.
  What GR11 still decides is unchanged: a replacement whose replaced text begins on a debugging line puts every line it
  introduces on a debugging line, and a literal that would continue onto a debugging line is an error (COBOL7039). Code:
  `CopyProcessor.IntroducedText` applies one rule to COPY and REPLACE. Source:
  `kb/Work/PB5887/2026-11-16-the-text-is-open.md`. Witnesses:
  `conformance:cases/XII.3@1985-REPLACE-STATEMENT/pb5887-replace-pseudo-text-debugging-line` and
  `conformance:cases/XII.3@1985-REPLACE-STATEMENT/pb5382-replace-debugging-introduced-lines`.
