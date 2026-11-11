      *>--- conformance-case
      *> id: pb5161_numval_c_currency_content_85
      *> rules: [1989-AR-A.2.30.3-1, 1989-AR-A.2.30.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5161 - the 1989 intrinsic function module puts no
      *> rule on the CHARACTERS of NUMVAL-C's argument-2.
      *>
      *> §1989:A.2.30.3 AR4 (cite.py --check): "Argument-2, if
      *> specified, must be a nonnumeric literal or alphanumeric data
      *> item"; AR1: "cs is the string of one or more characters
      *> specified by argument-2", in the two formats [space] [+|-]
      *> [space] [cs] [space] digits and [space] [cs] [space] digits
      *> [space] [+|-|CR|DB] [space]; §1989:A.2.30.4: "The returned
      *> value is the numeric value represented by argument-1".
      *> So "Fr." (a period), "CRC" (the letters CR), "C1" (a digit)
      *> and "DB" are currency strings, as a literal and as a data
      *> item. Each line fails if the 2002 content rule (refused
      *> COBOL1627, or 0 for a data item) is applied at 85; the
      *> fourth and fifth also fail if the scan does not take cs
      *> before the digits and the sign.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5161A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC S9(5)V99.
       01 WS-E PIC -9(5).99.
       01 WS-F PIC X(3) VALUE "Fr.".
       01 WS-B PIC X(8) VALUE "Fr.12.50".
       01 WS-C PIC X(3) VALUE "CRC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE WS-N = FUNCTION NUMVAL-C ("Fr.12.50" "Fr.").
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           COMPUTE WS-N = FUNCTION NUMVAL-C (WS-B WS-F).
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           COMPUTE WS-N = FUNCTION NUMVAL-C ("CRC1,234.50" WS-C).
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           COMPUTE WS-N = FUNCTION NUMVAL-C ("C17.25CR" "C1").
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           COMPUTE WS-N = FUNCTION NUMVAL-C (" - DB 3.5" "DB").
           MOVE WS-N TO WS-E.
           DISPLAY WS-E.
           STOP RUN.
