      *>--- conformance-case
      *> id: pb5123-not-is-not-operator-2002
      *> rules: [2002-SR-8.8.4.2.4.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5123, review R1-1: the sibling of NOT IS > C.
      *> §2002:8.8.4.2.4.2 SR3: "The word NOT shall not be followed
      *> immediately by the word NOT or the words IS NOT." So
      *> NOT IS NOT > C, the logical NOT of 85 (§1985:VI.6.3.3
      *> GRL2.2), is refused from 2002: COBOL0902, construct
      *> abbreviated-not-logical-removed-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5123N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       01  B PIC 9 VALUE 9.
       01  C PIC 9 VALUE 6.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A > B AND NOT IS NOT > C DISPLAY "X"
           ELSE DISPLAY "Y".
           STOP RUN.
