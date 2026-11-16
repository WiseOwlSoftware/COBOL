      *>--- conformance-case
      *> id: g2002-pic-cd-locale-symbols-and-locale
      *> rules: [2002-GR-13.16.38.3-17, 2002-GR-13.16.38.4-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every PICTURE ... LOCALE item below is a record description
      *> entry under an OUTPUT CD, which may implicitly redefine the
      *> CD record area (§2002:A.1.1.2); the rules reach it there. The
      *> communication facility is obsolete at 2002 (COBOL0903
      *> warning); the warning does not fail the case.
      *> §2002:13.16.38.3 GR17 - the format 2 symbol meanings:
      *>   Z  ZZ9.99 <- 5.25 gives "  5.25": each Z holding a leading
      *>      zero becomes a space (Z1 line).
      *>   9  999.99 <- 5.25 gives "005.25": a 9 always holds a digit,
      *>      even a leading zero (N9 line).
      *>   .  ZZ9.99 <- 12.5 gives " 12.50": the decimal separator
      *>      ("." in en-US) is inserted and the value is aligned on
      *>      the '.' (D1 line).
      *>   +  +$ZZ9.99 is signed: -5.25 edits with the locale's
      *>      negative sign "-" before the currency string, 5.25 with
      *>      "+" (S- and S+ lines), and de-editing -5.25 back gives a
      *>      negative value (a de-editing MOVE, §2002:14.8.24.3 GR3).
      *>      $ZZ9.99 has no '+' and is unsigned: -5.25
      *>      edits as "$  5.25" and de-edits to +5.25 (U line).
      *>   cs the currency string comes from the locale: "$" for en-US
      *>      (every $ line).
      *> §2002:13.16.38.4 GR11 - LOCALE IS US names locale-name-1, so
      *>   en-US edits ("$1") and de-edits (the S- round trip) it; $9
      *>   LOCALE with no locale-name uses the CURRENT locale, which
      *>   the suite harness pins to the invariant locale
      *>   (COBOL_USER_LOCALE=INVARIANT), whose currency string is
      *>   U+00A4: ORD = code point + 1 = 165, never printed. Fails if
      *>   either item used the other's locale.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PICLC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE US IS "en-US".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS     PIC S999V99.
       01  WSHOW  PIC -999.99.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 LZ  PIC ZZ9.99 LOCALE IS US SIZE IS 6.
           05 LN  PIC 999.99 LOCALE IS US SIZE IS 6.
           05 LS  PIC +$ZZ9.99 LOCALE IS US SIZE IS 8.
           05 LU  PIC $ZZ9.99 LOCALE IS US SIZE IS 7.
           05 LN1 PIC $9 LOCALE IS US SIZE IS 2.
           05 LC1 PIC $9 LOCALE SIZE IS 2.
       PROCEDURE DIVISION.
           MOVE 5.25 TO LZ
           DISPLAY "Z1=[" LZ "]"
           MOVE 5.25 TO LN
           DISPLAY "N9=[" LN "]"
           MOVE 12.5 TO LZ
           DISPLAY "D1=[" LZ "]"
           MOVE -5.25 TO LS
           DISPLAY "S-=[" LS "]"
           MOVE LS TO WS
           MOVE WS TO WSHOW
           DISPLAY "S- BACK=" WSHOW
           MOVE 5.25 TO LS
           DISPLAY "S+=[" LS "]"
           MOVE -5.25 TO LU
           MOVE LU TO WS
           MOVE WS TO WSHOW
           DISPLAY "U=[" LU "] BACK=" WSHOW
           MOVE 1 TO LN1
           DISPLAY "NAMED=[" LN1 "]"
           MOVE 1 TO LC1
           IF FUNCTION ORD(LC1(1:1)) = 165 AND LC1(2:1) = "1"
               DISPLAY "CURRENT LOCALE USED"
           ELSE
               DISPLAY "CURRENT LOCALE WRONG " FUNCTION ORD(LC1(1:1))
           END-IF
           MOVE LC1 TO WS
           MOVE WS TO WSHOW
           DISPLAY "CURRENT BACK=" WSHOW
           STOP RUN.
