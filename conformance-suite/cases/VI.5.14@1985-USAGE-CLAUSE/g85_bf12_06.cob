      *>--- conformance-case
      *> id: g85_bf12_06
      *> rules: [1985-GR-VI.5.14.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR4 - USAGE IS COMPUTATIONAL: "a radix and
      *> format specified by the implementor", whose effect on
      *> alignment,
      *> representation (sign included) "and upon the range of values
      *> that the data item can hold" the implementor specifies.
      *> The rule's observable is the implementor's statement, and this
      *> case holds the implementation to it: docs/conformance/7/
      *> DOC-A.1-208 makes COMPUTATIONAL (and COMP, §1985:VI.5.14.3 SR4
      *> "COMP is an abbreviation for COMPUTATIONAL") identical to
      *> BINARY
      *> (DOC-A.1-205: two's complement, most significant byte first,
      *> 2 bytes for 3-4 digits, no alignment) with the PICTURE's digit
      *> count as its range.
      *> DERIVED:
      *>  WIDTH: group X + S9(4) COMPUTATIONAL + X is 4 positions; moved
      *>   to PIC X(6) (§1985:VI.6.19.4 GR5) "z" is position 4 and 5-6
      *>   are spaces.
      *>  BE: -1234 is X"FB2E"; FUNCTION ORD of bytes 2 and 3 is
      *>   251+1 = 252 and 46+1 = 47 (§1989:A.2.31.1 "The lowest ordinal
      *>   position is 1"; used in COMPUTE, §1985:IV.4.3.8.3.3 SR4).
      *>  IDENT: the same picture and value as COMPUTATIONAL, COMP and
      *>   BINARY give the same image, neighbours included.
      *>  HOLD: 9999 and -9999 are held (the PICTURE's range).
      *>  SIZE / KEEP: the largest value the item "can hold" is 9999, so
      *>   ADD 1 is a size error (§1985:VI.6.4.2 GR3) and with ON SIZE
      *>   ERROR the item keeps 9999.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1206.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GU.
           05  GU-A  PIC X VALUE "a".
           05  GU-N  PIC S9(4) USAGE IS COMPUTATIONAL VALUE -1234.
           05  GU-Z  PIC X VALUE "z".
       01  GC.
           05  GC-A  PIC X VALUE "a".
           05  GC-N  PIC S9(4) USAGE IS COMP VALUE -1234.
           05  GC-Z  PIC X VALUE "z".
       01  GB.
           05  GB-A  PIC X VALUE "a".
           05  GB-N  PIC S9(4) USAGE IS BINARY VALUE -1234.
           05  GB-Z  PIC X VALUE "z".
       01  XU   PIC X(6).
       01  XC   PIC X(6).
       01  XB   PIC X(6).
       01  RNG  PIC S9(4) COMPUTATIONAL.
       01  O1   PIC 999.
       01  O2   PIC 999.
       01  FLG  PIC X(3).
       01  FL2  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE GU TO XU.
           MOVE GC TO XC.
           MOVE GB TO XB.
           MOVE "NO" TO FLG.
           IF XU(1:1) = "a" AND XU(4:1) = "z" AND XU(5:2) = SPACES
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "WIDTH=" FLG.
           COMPUTE O1 = FUNCTION ORD(XU(2:1)).
           COMPUTE O2 = FUNCTION ORD(XU(3:1)).
           DISPLAY "BE=" O1 " " O2.
           MOVE "NO" TO FLG.
           IF XU = XC AND XU = XB
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "IDENT=" FLG.
           MOVE "NO" TO FLG.
           MOVE -9999 TO RNG.
           IF RNG = -9999
               MOVE 9999 TO RNG
               IF RNG = 9999
                   MOVE "YES" TO FLG
               END-IF
           END-IF.
           DISPLAY "HOLD=" FLG.
           MOVE "NO" TO FLG.
           ADD 1 TO RNG
               ON SIZE ERROR MOVE "YES" TO FLG
           END-ADD.
           MOVE "NO" TO FL2.
           IF RNG = 9999
               MOVE "YES" TO FL2
           END-IF.
           DISPLAY "SIZE=" FLG " KEEP=" FL2.
           STOP RUN.
