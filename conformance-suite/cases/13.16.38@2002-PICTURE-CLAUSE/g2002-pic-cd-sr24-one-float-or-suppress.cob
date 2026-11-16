      *>--- conformance-case
      *> id: g2002-pic-cd-sr24-one-float-or-suppress
      *> rules: [2002-SR-13.16.38.2-24]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR24 - no more than one of: a string of two
      *> or more '+', of two or more '-', of two or more currency
      *> symbols, of one or more '*', of one or more 'Z'. Each item
      *> below writes exactly ONE such string, so the rule leaves it
      *> legal; the program must compile (a refusal fails the case) and
      *> each item edits as §2002:13.16.38.4 GR6 and GR7 define.
      *> The items are record description entries under an OUTPUT CD
      *> (an ordinary data description entry there, §2002:A.1.1.2), so
      *> the rule reaches them. COBOL0903 (obsolete facility) is a
      *> warning and does not fail the case.
      *> P1 +$$$  - the NOTE's own valid form: one '+' (not a string
      *>            of two or more) and one currency string. 5 -> "+ $5"
      *>            (GR5 fixed insertion: a positive value gives '+';
      *>            floating '$' placed left of the first nonzero digit,
      *>            GR6a; the leftmost '$' of a string is never a digit
      *>            position, so the item edits 2 digits: " $5").
      *> P2 $$$9  - one currency string. 12 -> " $12".
      *> P3 +++9  - one '+' string. -12 -> " -12" (Table 10).
      *> P4 ---9  - one '-' string. 12 -> "  12" (Table 10: space).
      *> P5 ***9  - one '*' string. 12 -> "**12" (GR7a).
      *> P6 $ZZ9  - one 'Z' string with a FIXED '$' (one currency
      *>            symbol, not a string of two or more). 12 -> "$ 12".
      *> The refusals g2002-pic-cd-sr24a/b/c-* write two strings.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PIC24.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 P1 PIC +$$$.
           05 P2 PIC $$$9.
           05 P3 PIC +++9.
           05 P4 PIC ---9.
           05 P5 PIC ***9.
           05 P6 PIC $ZZ9.
       PROCEDURE DIVISION.
           MOVE 5 TO P1
           MOVE 12 TO P2
           MOVE -12 TO P3
           MOVE 12 TO P4
           MOVE 12 TO P5
           MOVE 12 TO P6
           DISPLAY "P1=[" P1 "] P2=[" P2 "] P3=[" P3 "]"
           DISPLAY "P4=[" P4 "] P5=[" P5 "] P6=[" P6 "]"
           STOP RUN.
