*>--- conformance-case
*> id: pb3996_call_format2_omitted
*> rules: [FMT-14.9.4.2, GR-14.9.4.4-11]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 14.9.4.2 Format 2 (program-prototype) prints `[ BY REFERENCE ]
      *> { identifier-2 | OMITTED }`, bare or under BY REFERENCE: the control for the
      *> Format 1 refusals (kb/Work PB3996). 14.9.4.4 GR11: an OMITTED phrase makes
      *> "the omitted-argument condition for that parameter evaluate to true in the
      *> called program". 14.9.4.3 SR24: the callee's formal is OPTIONAL.
      *> EXPECTED, DERIVED FROM THE RULES: calls 1 and 2 omit B (bare, then BY
      *> REFERENCE), call 3 supplies it.
      *> kb/Work PB2103 - 12.3.8.3 SR14: PB3996CE's definition follows the REPOSITORY paragraph, so it is not "specified
      *> previously"; the program prototype definition ahead of every unit (10.6.2 SR1) supplies GR10 b)'s details.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996CE IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       PROCEDURE DIVISION USING A OPTIONAL B.
       END PROGRAM PB3996CE.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996OK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM PB3996CE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB3996CE" AS PB3996CE USING W OMITTED
           CALL "PB3996CE" AS PB3996CE USING BY REFERENCE W BY REFERENCE OMITTED
           CALL "PB3996CE" AS PB3996CE USING W W
           STOP RUN.
       END PROGRAM PB3996OK.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3996CE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       PROCEDURE DIVISION USING A OPTIONAL B.
       MAIN.
           DISPLAY "A=" A
           IF B IS OMITTED
               DISPLAY "B OMITTED"
           ELSE
               DISPLAY "B=" B
           END-IF
           GOBACK.
       END PROGRAM PB3996CE.
