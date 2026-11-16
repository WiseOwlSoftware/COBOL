      *>--- conformance-case
      *> id: g2ins41-sr3-cd-figurative-class
      *> rules: [2002-SR-14.8.21.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.2 SR3 - a figurative constant operand of INSPECT
      *> whose identifier-1 is a record item under a 2002 output CD (the
      *> record description entries after a CD are ordinary data
      *> description entries, §2002:A.1.1.2). The CD facility is
      *> obsolete at 2002 (COBOL0903 warning; the case is accepted).
      *> "If literal-1, literal-2, or literal-4 is a figurative
      *> constant, it refers to an implicit one character data item.
      *> When identifier-1 is of class national, the class of the
      *> figurative constant is national; when identifier-1 is of class
      *> boolean, the figurative constant is of class boolean and only
      *> the figurative constant ZERO may be specified; otherwise, the
      *> class of the figurative constant is alphanumeric."
      *> EXPECTED (each derived from that text):
      *>   1 NATIONAL SPACE OK  CN (PIC N(3), N"A B"): SPACE is a
      *>                        one-character national space, so
      *>                        REPLACING ALL SPACE BY N"Z" gives N"AZB"
      *>   2 BOOLEAN ZERO=02    CB (PIC 1(4), B"1010"): ZERO is the
      *>                        boolean B"0", one position, found twice
      *>   3 ALNUM SPACE=02     CX ("A  BC"): SPACE is one alphanumeric
      *>                        character, each of the two counted
      *>   4 BEFORE SPACE=01    literal-2 SPACE is one character: one
      *>                        character ("A") precedes the first space
      *>   5 CY=AB*C            literal-4 SPACE is one character, so
      *>                        CONVERTING SPACE TO "*" maps the one
      *>                        space of "AB C"
      *> Why each leg can fail: a CD record item refused as
      *> identifier-1, a figurative constant given the wrong class (no
      *> national or boolean match: 1 WRONG, 2=00) or a length other
      *> than one character (3, 4 and 5 change).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS41.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C1 PIC 99 VALUE 0.
       01 C2 PIC 99 VALUE 0.
       01 C3 PIC 99 VALUE 0.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 CN PIC N(3) VALUE N"A B".
          05 CB PIC 1(4) VALUE B"1010".
          05 CX PIC X(5) VALUE "A  BC".
          05 CY PIC X(4) VALUE "AB C".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT CN REPLACING ALL SPACE BY N"Z".
           IF CN = N"AZB"
               DISPLAY "1 NATIONAL SPACE OK"
           ELSE
               DISPLAY "1 NATIONAL SPACE WRONG"
           END-IF.
           INSPECT CB TALLYING C1 FOR ALL ZERO.
           DISPLAY "2 BOOLEAN ZERO=" C1.
           INSPECT CX TALLYING C2 FOR ALL SPACE.
           DISPLAY "3 ALNUM SPACE=" C2.
           INSPECT CX TALLYING C3 FOR CHARACTERS BEFORE INITIAL SPACE.
           DISPLAY "4 BEFORE SPACE=" C3.
           INSPECT CY CONVERTING SPACE TO "*".
           DISPLAY "5 CY=" CY.
           STOP RUN.
       END PROGRAM G2INS41.
