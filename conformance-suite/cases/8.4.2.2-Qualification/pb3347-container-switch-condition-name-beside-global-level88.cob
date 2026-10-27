      *>--- conformance-case
      *> id: pb3347-container-switch-condition-name-beside-global-level88
      *> rules: [SR-8.4.2.2.1-1, SR-8.4.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3347 (review round 1, R1-1). The outer program's SPECIAL-NAMES declares the switch condition-name S-ON
      *> and its data division a GLOBAL level-88 S-ON: both belong to the SAME source element, so the name is not unique
      *> there (8.4.2.2.1 1: "No other name has the identical spelling") and the contained program, which describes
      *> neither, reaches that same pair (8.4.6.2.1 3 b 2: "The item in the containing source element if the name is not
      *> declared in source element A and is declared in the source element containing source element A"). Its
      *> unqualified `IF S-ON` establishes no uniqueness (a switch condition-name takes no qualifier, 8.8.4.6.2) and is
      *> refused (COBOL1639) exactly as the same reference inside the outer program is; it used to bind the level-88
      *> silently and print READ-88 with the switch OFF. Refused at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347OUTERG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SWITCH-31 ON STATUS IS S-ON.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9 VALUE 1 GLOBAL.
          88 S-ON VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB3347INNERG".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3347INNERG.
       PROCEDURE DIVISION.
       MAIN.
           IF S-ON DISPLAY "READ-88" ELSE DISPLAY "READ-SWITCH-OFF".
           EXIT PROGRAM.
       END PROGRAM PB3347INNERG.
       END PROGRAM PB3347OUTERG.
