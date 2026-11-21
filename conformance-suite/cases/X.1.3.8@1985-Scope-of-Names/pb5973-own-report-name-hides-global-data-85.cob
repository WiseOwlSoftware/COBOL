      *>--- conformance-case
      *> id: pb5973-own-report-name-hides-global-data-85
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
      *> The container declares 01 XR GLOBAL; the contained program
      *> declares its own RD XR. There XR names that report, so MOVE
      *> "B" TO XR names no data item and is refused (COBOL1639). A
      *> compiler that resolves data-names apart from the report-names
      *> changes the container's item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73DO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XR PIC X VALUE "G" GLOBAL.
       PROCEDURE DIVISION.
       P-1.
           CALL "P73DI"
           DISPLAY "O XR=" XR
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73DI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73d.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS XR.
       REPORT SECTION.
       RD XR.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       I-1.
           MOVE "B" TO XR
           EXIT PROGRAM.
       END PROGRAM P73DI.
       END PROGRAM P73DO.
