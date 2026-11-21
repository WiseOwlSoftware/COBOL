- **D-85-NUMVAL-C-CURRENCY-SPACES — the leading and trailing spaces of NUMVAL-C's argument-2 at COBOL-85 (kb/Work PB5344).**
  This entry speaks for COBOL-85 (ANSI X3.23-1985 with Amendment 1, whose functions are cited `§1989:`). The 1989 text makes
  cs "the string of one or more characters specified by argument-2" (`§1989:A.2.30.3 AR1`) and trims nothing, so a `PIC X(3)`
  argument-2 holding `$` is the three-character string `$  `. WiseOwl COBOL removes argument-2's leading and trailing spaces at
  every edition, as the later text states (`§2023:15.68.3 AR2`: "Any leading or trailing spaces in argument-2 are ignored"),
  and the choice conforms at 85: argument-1's formats put `[space]` around cs, so every argument-1 that conforms with cs `$  `
  also conforms with cs `$` and has the same value (`$  123.45` is 123.45 at either reading). The two readings differ only on
  an argument-1 that does not comply with cs `$  `, such as `$123.45`, and "the result of such a reference is undefined"
  (`§1989:A.1.2.2`). GnuCOBOL 3.2 trims too (`-std=cobol85`). Decided by the controller (account 2) on 2026-11-21 from the
  adjudication of wave 20262131 (decisions6). The test is
  `conformance-suite/cases/A.2.30@1985-NUMVAL-C-FUNCTION/pb5344-numval-c-argument-2-edge-spaces-85`. Source: `kb/Work/PB5344.md`.
