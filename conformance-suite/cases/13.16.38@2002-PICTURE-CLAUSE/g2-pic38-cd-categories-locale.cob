      *>--- conformance-case
      *> id: g2-pic38-cd-categories-locale
      *> rules: [2002-GR-13.16.38.3-1, 2002-GR-13.16.38.3-2, 2002-GR-13.16.38.3-3, 2002-GR-13.16.38.3-6, 2002-GR-13.16.38.4-14, 2002-GR-13.16.38.4-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every item below is a record description entry under an OUTPUT
      *> CD, which implicitly redefines the CD's record area
      *> (§2002:A.1.2.2 SR9); PICTURE applies there as anywhere else.
      *> The communication facility is obsolete at 2002 (COBOL0903
      *> warning); the warning does not fail the case.
      *> §2002:13.16.38.3 GR1 - PIC N(3) and NBN USAGE NATIONAL define
      *>   national character positions: LENGTH 3, reference modification
      *>   NA(2:2) = "BC" counts national positions, BYTE-LENGTH 6 (the
      *>   implementor's national representation is UTF-16, two bytes a
      *>   position); PIC X(3) defines alphanumeric positions: LENGTH 3,
      *>   BYTE-LENGTH 3. Fails if N symbols were sized as alphanumeric.
      *> §2002:13.16.38.3 GR2 - the 'B' inserted into national-edited NE
      *>   is the national space (NE(2:1) = N" ", byte length 6), the
      *>   'B' in alphanumeric-edited XE is the alphanumeric space.
      *> §2002:13.16.38.3 GR3 - one item per category: alphabetic (AAA:
      *>   INITIALIZE ... REPLACING ALPHABETIC changes AL and leaves the
      *>   alphanumeric XA alone, §2002:14.8.19.3 GR5c2), alphanumeric
      *>   (X(3)), alphanumeric-edited (XBX), boolean (111: MOVE B"1"
      *>   gives 100, zero fill, §2002:14.5.8 rule 6; an alphanumeric
      *>   item would give "1  "), national (N(3)), national-edited
      *>   (NBN), numeric (999: MOVE 12 gives 012), numeric-edited
      *>   (ZZ9.99; and 99 BLANK WHEN ZERO, which BWZ makes numeric-
      *>   edited: MOVE 0 gives spaces, a numeric item would give 00).
      *> §2002:13.16.38.3 GR6 - A9X (contains X) and A9 / 9XX (two
      *>   different symbols) are alphanumeric: MOVE 7 / 12 left-justify
      *>   with space fill (§2002:14.5.8) - a numeric item would
      *>   right-justify with zero fill (07, 012).
      *> §2002:13.16.38.4 GR14 - LOCALE editing in a hypothetical item
      *>   then moved: larger item right-justified with space fill
      *>   (L9 "    34.50"), right-end truncation (34.567 -> 34.56),
      *>   left truncation of a suppressed-zero space with no exception
      *>   (L5 "34.50"), and left truncation of a significant digit
      *>   setting EC-LOCALE-SIZE (the declarative runs).
      *> §2002:13.16.38.4 GR15 - Format 2 Z suppression: all-Z zero
      *>   value gives all spaces (LZ); leading-Z only (form a) puts
      *>   spaces before the decimal point (LA "     .05").
       >>TURN EC-LOCALE-SIZE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PIC38A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 NA  PIC N(3).
           05 NE  PIC NBN USAGE NATIONAL.
           05 XA  PIC X(3).
           05 XE  PIC XBX.
           05 AL  PIC AAA.
           05 AN  PIC A9X.
           05 A9  PIC A9.
           05 NX  PIC 9XX.
           05 BO  PIC 111.
           05 NU  PIC 999.
           05 BZ  PIC 99 BLANK WHEN ZERO.
           05 E1  PIC ZZ9.99.
           05 L9  PIC ZZ9.99 LOCALE IS US SIZE IS 9.
           05 L5  PIC ZZ9.99 LOCALE IS US SIZE IS 5.
           05 LZ  PIC ZZZ.ZZ LOCALE IS US SIZE IS 8.
           05 LA  PIC ZZZ.99 LOCALE IS US SIZE IS 8.
       PROCEDURE DIVISION.
       DECLARATIVES.
       LSIZE SECTION.
           USE AFTER EXCEPTION CONDITION EC-LOCALE-SIZE.
       LSIZE-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS(1:14).
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           MOVE N"ABC" TO NA
           MOVE N"XY" TO NE
           MOVE "PQR" TO XA
           MOVE "UV" TO XE
           MOVE "ABC" TO AL
           MOVE 12 TO AN
           MOVE 7 TO A9
           MOVE 12 TO NX
           MOVE B"1" TO BO
           MOVE 12 TO NU
           MOVE 0 TO BZ
           MOVE 1.5 TO E1
           DISPLAY "GR1 LEN NA=" FUNCTION LENGTH(NA)
                   " BYTES NA=" FUNCTION BYTE-LENGTH(NA)
                   " LEN XA=" FUNCTION LENGTH(XA)
                   " BYTES XA=" FUNCTION BYTE-LENGTH(XA)
           IF NA(2:2) = N"BC"
               DISPLAY "GR1 NA(2:2)=BC"
           ELSE
               DISPLAY "GR1 NA(2:2) WRONG"
           END-IF
           DISPLAY "GR1 NA=[" NA "] XA=[" XA "]"
           DISPLAY "GR2 NE=[" NE "] LEN NE=" FUNCTION LENGTH(NE)
                   " BYTES NE=" FUNCTION BYTE-LENGTH(NE)
                   " XE=[" XE "]"
           IF NE(2:1) = N" "
               DISPLAY "GR2 NE INSERT IS NATIONAL SPACE"
           ELSE
               DISPLAY "GR2 NE INSERT WRONG"
           END-IF
           IF XE(2:1) = " "
               DISPLAY "GR2 XE INSERT IS SPACE"
           ELSE
               DISPLAY "GR2 XE INSERT WRONG"
           END-IF
           DISPLAY "GR3 AL=[" AL "] BO=[" BO "] NU=[" NU "] BZ=[" BZ
                   "] E1=[" E1 "]"
           INITIALIZE AL REPLACING ALPHABETIC DATA BY "QQQ"
           INITIALIZE XA REPLACING ALPHABETIC DATA BY "QQQ"
           DISPLAY "GR3 INIT AL=[" AL "] XA=[" XA "]"
           IF NU IS NUMERIC
               DISPLAY "GR3 NU NUMERIC"
           END-IF
           DISPLAY "GR6 AN=[" AN "] A9=[" A9 "] NX=[" NX "]"
           MOVE 34.5 TO L9
           DISPLAY "GR14 L9=[" L9 "]"
           MOVE 34.567 TO L9
           DISPLAY "GR14 L9T=[" L9 "]"
           MOVE 34.5 TO L5
           DISPLAY "GR14 L5=[" L5 "]"
           MOVE 134.5 TO L5
           DISPLAY "GR14 AFTER L5 OVERFLOW"
           MOVE 0 TO LZ
           DISPLAY "GR15 LZ0=[" LZ "]"
           MOVE 5.25 TO LZ
           DISPLAY "GR15 LZ=[" LZ "]"
           MOVE 0.05 TO LA
           DISPLAY "GR15 LA=[" LA "]"
           STOP RUN.
