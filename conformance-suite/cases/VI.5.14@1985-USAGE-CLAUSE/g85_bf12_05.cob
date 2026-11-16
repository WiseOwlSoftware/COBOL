      *>--- conformance-case
      *> id: g85_bf12_05
      *> rules: [1985-GR-VI.5.14.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR3 - USAGE IS BINARY: radix 2; the implementor
      *> specifies alignment and representation, sign included; and
      *> "Sufficient computer storage must be allocated by the
      *> implementor to contain the maximum range of values implied by
      *> the associated decimal PICTURE character-string."
      *> DERIVED FROM THE 1985 TEXT:
      *>  RANGE: each item holds its PICTURE's extreme values both
      *>   ways: S9(4) 9999/-9999, S9(9) 999999999, S9(18)
      *>   999999999999999999 and its negative. Each compares equal to
      *>   the literal moved in; any narrower storage fails a leg.
      *>  EDIT: the S9(18) extreme moved to -9(18) prints all 18 nines.
      *>  CEIL: §1985:VI.6.4.2 GR3 - for BINARY "the largest value that
      *>   can be contained in the resultant identifier is the maximum
      *>   value implied by the associated decimal PICTURE
      *>   character-string", so ADD 1 to S9(4) BINARY 9999 is a size
      *>   error (YES) and the item keeps 9999 (KEEP=YES).
      *> DERIVED FROM THE IMPLEMENTOR'S STATEMENT GR3 CALLS FOR
      *> (docs/conformance/7/DOC-A.1-205): two's complement, most
      *> significant byte first, 2 bytes for 3-4 digits, no alignment.
      *>  WIDTH: group X + S9(4) BINARY + X is 4 positions; moved to a
      *>   PIC X(6) item (§1985:VI.6.19.4 GR5, an alphanumeric move with
      *>   space fill) "z" lands in position 4 and 5-6 are spaces.
      *>  BE: -1234 is X"FB2E"; FUNCTION ORD (§1989:A.2.31.1, "The
      *>   lowest ordinal position is 1"; written in COMPUTE because an
      *>   integer function "may be used only in an arithmetic
      *>   expression", §1985:IV.4.3.8.3.3 SR4) of bytes 2 and 3 of that
      *>   image is 251+1 = 252 and 46+1 = 47 in the native collating
      *>   sequence (byte value order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1205.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B4   PIC S9(4)  USAGE IS BINARY.
       01  B9   PIC S9(9)  USAGE IS BINARY.
       01  B18  PIC S9(18) USAGE IS BINARY.
       01  E18  PIC -9(18).
       01  GB.
           05  GB-A  PIC X VALUE "a".
           05  GB-N  PIC S9(4) USAGE IS BINARY VALUE -1234.
           05  GB-Z  PIC X VALUE "z".
       01  XB   PIC X(6).
       01  O1   PIC 999.
       01  O2   PIC 999.
       01  FLG  PIC X(3).
       01  FL2  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "NO" TO FLG.
           MOVE 9999 TO B4.
           IF B4 = 9999
               MOVE -9999 TO B4
               IF B4 = -9999
                   MOVE "YES" TO FLG
               END-IF
           END-IF.
           DISPLAY "R4=" FLG.
           MOVE "NO" TO FLG.
           MOVE 999999999 TO B9.
           IF B9 = 999999999
               MOVE -999999999 TO B9
               IF B9 = -999999999
                   MOVE "YES" TO FLG
               END-IF
           END-IF.
           DISPLAY "R9=" FLG.
           MOVE "NO" TO FLG.
           MOVE 999999999999999999 TO B18.
           IF B18 = 999999999999999999
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "R18=" FLG.
           MOVE -999999999999999999 TO B18.
           MOVE B18 TO E18.
           DISPLAY "EDIT=" E18.
           MOVE 9999 TO B4.
           MOVE "NO" TO FLG.
           ADD 1 TO B4
               ON SIZE ERROR MOVE "YES" TO FLG
           END-ADD.
           MOVE "NO" TO FL2.
           IF B4 = 9999
               MOVE "YES" TO FL2
           END-IF.
           DISPLAY "CEIL=" FLG " KEEP=" FL2.
           MOVE GB TO XB.
           MOVE "NO" TO FLG.
           IF XB(1:1) = "a" AND XB(4:1) = "z" AND XB(5:2) = SPACES
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "WIDTH=" FLG.
           COMPUTE O1 = FUNCTION ORD(XB(2:1)).
           COMPUTE O2 = FUNCTION ORD(XB(3:1)).
           DISPLAY "BE=" O1 " " O2.
           STOP RUN.
