      *>--- conformance-case
      *> id: g2eva05_cd_range_national_alphabet
      *> rules: [2002-SR-14.8.12.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.2 SR3 - "otherwise, alphabet-name-1 shall
      *> reference an alphabet that defines an alphanumeric collating
      *> sequence". SD1 and SK of an output CD are alphanumeric
      *> (§2002:A.1.2.3 GR16c, GR16d), not national, and NREV is
      *> declared FOR NATIONAL, so IN NREV is refused at the EVALUATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVA05.
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
           EVALUATE X
             WHEN SD1 THRU SK IN NREV DISPLAY "IN"
             WHEN OTHER DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM G2EVA05.
