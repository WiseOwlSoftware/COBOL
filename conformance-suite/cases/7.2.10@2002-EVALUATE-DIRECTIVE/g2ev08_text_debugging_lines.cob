      *>--- conformance-case
      *> id: g2ev08_text_debugging_lines
      *> rules: [2002-SR-7.2.10.2-8]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.10.2 SR8 - "Text-1 and text-2 may be any kind of
      *>   source lines, including compiler directives. Text-1 and
      *>   text-2 may consist of multiple lines." In 2002 the kinds of
      *>   source line include debugging lines, by a fixed debugging
      *>   indicator (D or d) or the floating debugging indicator >>D
      *>   ("a debugging line when followed by at least one space
      *>   character", §2002:6.1.2). Every text-1 and text-2 below is
      *>   multi-line and mixes D, d and >>D lines, source lines and a
      *>   nested >>IF directive; all must be ACCEPTED.
      *> With WITH DEBUGGING MODE an included debugging line is compiled:
      *>   only without it does "a debugging line [have] the
      *>   characteristics of a comment line" (§2002:6.2.7).
      *> EXPECTED OUTPUT, DERIVED:
      *>   EVALUATE 3: 3 is not in 1 THRU 2; WHEN 3 is TRUE, so "all lines
      *>   of text-1 associated with that WHEN phrase are included in the
      *>   resultant text" (§2002:7.2.10.3 GR4) and the others omitted:
      *>     EV1-W2-DLINE, EV1-W2-SRC, EV1-W2-FLOAT, EV1-W2-LOWER-D, and
      *>     EV1-W2-NESTED-DLINE from the nested >>IF 1 = 1 text-1
      *>     (§2002:7.2.13.3 GR2).
      *>   EVALUATE "AB": no WHEN matches, so "all lines of text-2
      *>   associated with the WHEN OTHER phrase ... are included"
      *>   (§2002:7.2.10.3 GR5): EV2-OTH-FLOAT, EV2-OTH-DLINE.
      *>   EVALUATE TRUE: WHEN 2 = 2 is TRUE: EV3-W1-FLOAT, EV3-W1-DLINE.
      *> How each leg can fail: refusing a debugging line or a nested
      *> directive in the text fails the compile; a debugging line kept
      *> from an omitted text prints a *-BAD line; an included one lost
      *> (or made a comment) drops its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EV08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
       >>EVALUATE 3
       >>WHEN 1 THRU 2
      D    DISPLAY "EV1-W1-DLINE-BAD"
       >>D DISPLAY "EV1-W1-FLOAT-BAD"
       >>WHEN 3
      D    DISPLAY "EV1-W2-DLINE"
           DISPLAY "EV1-W2-SRC"
       >>D DISPLAY "EV1-W2-FLOAT"
      d    DISPLAY "EV1-W2-LOWER-D"
       >>IF 1 = 1
      D    DISPLAY "EV1-W2-NESTED-DLINE"
       >>ELSE
      D    DISPLAY "EV1-W2-NESTED-BAD"
       >>END-IF
       >>WHEN OTHER
      D    DISPLAY "EV1-OTH-DLINE-BAD"
       >>D DISPLAY "EV1-OTH-FLOAT-BAD"
       >>END-EVALUATE
       >>EVALUATE "AB"
       >>WHEN "XY"
       >>D DISPLAY "EV2-W1-FLOAT-BAD"
       >>WHEN OTHER
       >>D DISPLAY "EV2-OTH-FLOAT"
      D    DISPLAY "EV2-OTH-DLINE"
       >>END-EVALUATE
       >>EVALUATE TRUE
       >>WHEN 2 = 2
       >>D DISPLAY "EV3-W1-FLOAT"
      D    DISPLAY "EV3-W1-DLINE"
       >>WHEN OTHER
      D    DISPLAY "EV3-OTH-DLINE-BAD"
       >>END-EVALUATE
           STOP RUN.
