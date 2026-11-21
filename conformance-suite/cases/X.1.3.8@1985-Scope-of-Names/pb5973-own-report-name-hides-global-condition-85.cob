      *>--- conformance-case
      *> id: pb5973-own-report-name-hides-global-condition-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973.
      *> §1985:X.1.3.8 (cite.py: OK): "a program's reference to such a
      *> name, even when it is a different type of user-defined word,
      *> is to the object which that program describes rather than to
      *> the object, possessing the same name, described in another
      *> program"; §1985:X.1.3.8.2 GR3a (cite.py: OK): "If the name is
      *> declared in program B, the object in program B is the
      *> referenced object."
      *> The container declares a GLOBAL level-88 CR; the contained
      *> program declares its own RD CR. There CR names that report,
      *> so IF CR names no condition and is refused (COBOL1639). A
      *> compiler that ranks a condition-name only against the data
      *> classes tests the container's condition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73CO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CV PIC 9 VALUE 1 GLOBAL.
          88 CR VALUE 1.
       PROCEDURE DIVISION.
       P-1.
           CALL "P73CI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73CI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS CR.
       REPORT SECTION.
       RD CR.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       I-1.
           IF CR DISPLAY "I CR TRUE" ELSE DISPLAY "I CR FALSE".
           EXIT PROGRAM.
       END PROGRAM P73CI.
       END PROGRAM P73CO.
