      *>--- conformance-case
      *> id: g2-usage-pic-n-national
      *> rules: [2002-SR-13.16.58.2-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR19 - "Only the NATIONAL phrase may be
      *> specified in a USAGE clause associated with an elementary data
      *> item whose explicit or implicit picture character-string
      *> contains the symbol 'N'." The granted form: N1 (working
      *> storage) is PIC N(2) with an explicit USAGE NATIONAL; N2 is
      *> PIC N(2) with no USAGE clause. Both are accepted and are the
      *> same national item: each equals N"XY" ("EQ") and N1 has N2's
      *> BYTE-LENGTH ("LEN SAME"). Fails if the explicit NATIONAL
      *> phrase is refused, or gives an item of another width or
      *> content. The refused form (USAGE DISPLAY with PIC N, in a CD
      *> record) is g2-usage-cd-pic-n-display.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2USG05.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N1  PIC N(2) USAGE NATIONAL VALUE N"XY".
       01  N2  PIC N(2) VALUE N"XY".
       01  L1  PIC 99.
       01  L2  PIC 99.
       PROCEDURE DIVISION.
       MAIN-P.
           IF N1 = N"XY" AND N2 = N"XY"
               DISPLAY "EQ"
           ELSE
               DISPLAY "NE"
           END-IF
           MOVE FUNCTION BYTE-LENGTH(N1) TO L1
           MOVE FUNCTION BYTE-LENGTH(N2) TO L2
           IF L1 = L2
               DISPLAY "LEN SAME"
           ELSE
               DISPLAY "LEN DIFF"
           END-IF
           STOP RUN.
