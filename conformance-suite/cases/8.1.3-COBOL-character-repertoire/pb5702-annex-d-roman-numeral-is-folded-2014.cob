*>--- conformance-case
*> id: pb5702-annex-d-roman-numeral-is-folded-2014
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5702 - §2002:8.1.2.1 GR4c: "An uppercase extended letter is treated as though it were folded to its
      *> corresponding lowercase extended letter in accordance with the case mapping described in Annex D, if any." 2002
      *> Annex D (D.2.1, D.2.2) lists no mapping for U+2160 ROMAN NUMERAL ONE, so at COBOL 2002 X<U+2160>Y and X<U+2170>Y
      *> are two names and the program has two items. 2014 and 2023 fold U+2160 to U+2170: one name, two declarations of it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5702S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XⅠY PIC X(3) VALUE "UP".
       01 XⅰY PIC X(3) VALUE "LO".
       PROCEDURE DIVISION.
           DISPLAY XⅠY " " XⅰY.
           STOP RUN.
