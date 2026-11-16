      *>--- conformance-case
      *> id: g2eva04_cd_range_alphabet_numeric
      *> rules: [2002-SR-14.8.12.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.2 SR3 - "Alphabet-name-1 may be specified only
      *> when the literals or identifiers specified in the THROUGH
      *> phrase are of class alphabetic, alphanumeric, or national".
      *> TL (TEXT LENGTH) and DC (DESTINATION COUNT) of an output CD are
      *> integers (§2002:A.1.2.3 GR16a, GR16b), class numeric, so IN
      *> ALPHA1 is refused at the EVALUATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVA04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET ALPHA1 IS NATIVE
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET NREV FOR NATIONAL IS N"CBA".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(12) VALUE "M".
       01 W PIC 9(4) VALUE 5.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE W
             WHEN TL THRU DC IN ALPHA1 DISPLAY "IN"
             WHEN OTHER DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM G2EVA04.
