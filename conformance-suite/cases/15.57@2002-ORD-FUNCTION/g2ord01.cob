      *>--- conformance-case
      *> id: g2ord01
      *> rules: [2002-AR-15.57.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.57.2 AR1 - every argument form AR1 grants is accepted
      *> and evaluated: a one-position item of category alphabetic,
      *> alphanumeric and national, an alphanumeric and a national
      *> literal of one character, a one-position reference
      *> modification and a one-position alphanumeric group (class
      *> alphanumeric per §2002:8.5.2 and §2002:13.16.27.3 GR3).
      *> §2002:15.57.3 RV1/RV2 give each value as the ordinal position in
      *> the alphanumeric or national program collating sequence; both
      *> are pinned by user alphabets (§2002:12.2.5.3 GR9) whose literal
      *> order fixes the ordinals (§2002:12.2.6.3 GR7k), so no native
      *> code value is assumed. Each leg fails if the form is refused
      *> (AR1 granted it) or if the value comes from the native sequence
      *> or the wrong (alphanumeric vs national) sequence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ORD01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE FOR ALPHANUMERIC IS REV
                                      FOR NATIONAL IS NREV.
       SPECIAL-NAMES.
           ALPHABET REV FOR ALPHANUMERIC IS "Z" "Y" "X" "B" "A"
           ALPHABET NREV FOR NATIONAL IS N"B" N"A" N"Z".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 AL PIC A VALUE "A".
       01 AN PIC X VALUE "Z".
       01 A3 PIC X(3) VALUE "QBY".
       01 NA PIC N VALUE N"A".
       01 G.
          05 GC PIC X VALUE "Y".
       01 R PIC 99.
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION ORD(AL)
           DISPLAY "AL=" R
           COMPUTE R = FUNCTION ORD(AN)
           DISPLAY "AN=" R
           COMPUTE R = FUNCTION ORD("X")
           DISPLAY "LIT=" R
           COMPUTE R = FUNCTION ORD(A3(2:1))
           DISPLAY "REF=" R
           COMPUTE R = FUNCTION ORD(G)
           DISPLAY "GRP=" R
           COMPUTE R = FUNCTION ORD(NA)
           DISPLAY "NA=" R
           COMPUTE R = FUNCTION ORD(N"Z")
           DISPLAY "NLIT=" R
           STOP RUN.
