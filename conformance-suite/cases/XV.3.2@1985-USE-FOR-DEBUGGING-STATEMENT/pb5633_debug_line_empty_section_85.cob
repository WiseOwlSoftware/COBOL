      *>--- conformance-case
      *> id: pb5633_debug_line_empty_section_85
      *> rules: [1985-GR-XV.3.2.4-12, 1985-GR-XV.3.2.4-16]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5633. DEBUG-LINE when the subject is a section of
      *> no paragraphs (§1985:IV.6.4.1.2: "zero, one, or more
      *> successive paragraphs"). §1985:XV.3.2.4 GR16a: for the first
      *> execution of the first nondeclarative procedure "DEBUG-LINE
      *> identifies the first statement of that procedure"; S0 has
      *> none, and GR12 makes the identification the implementor's
      *> ("the implementor-defined means of identifying a particular
      *> source statement"). The choice (docs/conformance/3/
      *> D-DEBUG-PROCEDURE.md) is the section header's line: 38, the
      *> line of "S0 SECTION." in this file. GR20a: for the PERFORM
      *> of S2 "DEBUG-LINE identifies the PERFORM statement", line 42;
      *> GR22a: for the fall-through into S2 "DEBUG-LINE identifies
      *> the previous statement", DISPLAY "BACK" on line 43. The
      *> compiler's means is the statement's line in its file (the
      *> 1985-GR-XV.3.2.4-12 record), right-justified in DEBUG-LINE.
      *> Why it can fail: a change to the empty section's line (0, the
      *> next statement's line, the PERFORM's) moves the first value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5633DL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DS SECTION.
           USE FOR DEBUGGING ON S0 S2.
       DS-1.
           DISPLAY "DS " DEBUG-NAME(1:2) " " DEBUG-CONTENTS(1:5)
               " [" DEBUG-LINE "]".
       END DECLARATIVES.
       S0 SECTION.
       S1 SECTION.
       M1.
           DISPLAY "M1".
           PERFORM S2.
           DISPLAY "BACK".
       S2 SECTION.
       S3 SECTION.
       P3.
           DISPLAY "P3".
           STOP RUN.
