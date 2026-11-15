      *>--- conformance-case
      *> id: g2ev05_other_debugging_lines
      *> rules: [2002-GR-7.2.10.3-5, 2002-GR-7.2.10.3-9, 2002-GR-7.2.10.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.2.10.3 GR5 (format 1) and GR9 (format 2) - "If no WHEN
      *>   phrase evaluates to TRUE, all lines of text-2 associated with
      *>   the WHEN OTHER phrase, if specified, are included in the
      *>   resultant text. All lines of text-1 associated with other
      *>   WHEN phrases are omitted from the resultant text."
      *>   In 2002 text-1 and text-2 "may be any kind of source lines"
      *>   (§2002:7.2.10.2 SR8), so they may be debugging lines; an
      *>   included debugging line stays one and, WITH DEBUGGING MODE
      *>   specified, is compiled (§2002:6.2.7), while an omitted one
      *>   never reaches the resultant text.
      *> §2002:7.2.10.3 GR1 - text words of text-2 are subject to the
      *>   REPLACE statement; "A debugging indicator shall be treated as
      *>   if it did not appear in the line" (§2002:7.1.3.3 GR8c).
      *> EXPECTED OUTPUT, DERIVED:
      *>   F1-OTH-DLINE-REPLACED  format 1: 9 matches no WHEN, so the
      *>                          WHEN OTHER D line is included (GR5)
      *>                          and REPLACE acted on G2TAG (GR1).
      *>   F1-OTH-SRC             format 1 text-2 ordinary line (GR5).
      *>   F2-OTH-DLINE           format 2: 1 = 2 is FALSE, the WHEN
      *>                          OTHER D line is included (GR9).
      *>   F2-OTH-SRC             format 2 text-2 ordinary line (GR9).
      *> No line containing BAD: the WHEN text-1 lines, D lines among
      *> them, are omitted (GR5, GR9 second sentence).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EV05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. G2-COMPUTER WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       MAIN-PARA.
           REPLACE ==G2TAG== BY =="F1-OTH-DLINE-REPLACED"==.
       >>EVALUATE 9
       >>WHEN 1
      D    DISPLAY "F1-W1-DLINE-BAD"
           DISPLAY "F1-W1-SRC-BAD"
       >>WHEN OTHER
      D    DISPLAY G2TAG
           DISPLAY "F1-OTH-SRC"
       >>END-EVALUATE
       >>EVALUATE TRUE
       >>WHEN 1 = 2
      D    DISPLAY "F2-W1-DLINE-BAD"
           DISPLAY "F2-W1-SRC-BAD"
       >>WHEN OTHER
      D    DISPLAY "F2-OTH-DLINE"
           DISPLAY "F2-OTH-SRC"
       >>END-EVALUATE
           STOP RUN.
