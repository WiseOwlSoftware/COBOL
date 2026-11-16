      *>--- conformance-case
      *> id: g2if06_text_debugging_lines
      *> rules: [2002-SR-7.2.13.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.13.2 SR6 - "Text-1 and text-2 may be any kind of
      *>   source lines, including compiler directives. Text-1 and
      *>   text-2 may consist of multiple lines." In 2002 the kinds of
      *>   source line include debugging lines, by a fixed debugging
      *>   indicator (D or d) or the floating debugging indicator >>D
      *>   ("a debugging line when followed by at least one space
      *>   character", §2002:6.1.2). Every text-1 and text-2 below is
      *>   multi-line and mixes D, d and >>D lines and source lines;
      *>   the first text-1 holds a nested >>IF. All must be ACCEPTED.
      *> With WITH DEBUGGING MODE an included debugging line is compiled:
      *>   only without it does "a debugging line [have] the
      *>   characteristics of a comment line" (§2002:6.2.7).
      *> EXPECTED OUTPUT, DERIVED:
      *>   >>IF 1 = 1 is TRUE: "all lines of text-1 are included in the
      *>   resultant text and all lines of text-2 are omitted"
      *>   (§2002:7.2.13.3 GR2): IF1-T1-DLINE, IF1-T1-SRC, IF1-T1-FLOAT,
      *>   IF1-T1-LOWER-D, and IF1-T1-NESTED-FLOAT from the nested
      *>   >>IF "A" = "A".
      *>   >>IF 1 = 2 is FALSE: "all lines of text-2 are included ... and
      *>   all lines of text-1 are omitted" (§2002:7.2.13.3 GR3):
      *>   IF2-T2-DLINE, IF2-T2-FLOAT, IF2-T2-SRC.
      *> How each leg can fail: refusing a debugging line or a nested
      *> directive in the text fails the compile; a debugging line kept
      *> from an omitted text prints a *-BAD line; an included one lost
      *> (or made a comment) drops its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IF06.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>IF 1 = 1
      D    DISPLAY "IF1-T1-DLINE"
           DISPLAY "IF1-T1-SRC"
       >>D DISPLAY "IF1-T1-FLOAT"
      d    DISPLAY "IF1-T1-LOWER-D"
       >>IF "A" = "A"
       >>D DISPLAY "IF1-T1-NESTED-FLOAT"
       >>ELSE
       >>D DISPLAY "IF1-T1-NESTED-BAD"
       >>END-IF
       >>ELSE
      D    DISPLAY "IF1-T2-DLINE-BAD"
       >>D DISPLAY "IF1-T2-FLOAT-BAD"
           DISPLAY "IF1-T2-SRC-BAD"
       >>END-IF
       >>IF 1 = 2
      D    DISPLAY "IF2-T1-DLINE-BAD"
       >>D DISPLAY "IF2-T1-FLOAT-BAD"
       >>ELSE
      D    DISPLAY "IF2-T2-DLINE"
       >>D DISPLAY "IF2-T2-FLOAT"
           DISPLAY "IF2-T2-SRC"
       >>END-IF
           STOP RUN.
