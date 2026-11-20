      *>--- conformance-case
      *> id: pb5973-own-report-name-hides-global-file-85
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
      *> The container declares FD F GLOBAL; the contained program
      *> declares its own RD F. There F names that report, so OPEN
      *> OUTPUT F names no file and is refused (COBOL1639). A compiler
      *> that hides an inherited file only behind the data classes
      *> opens the container's file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73HO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p73h.dat".
       DATA DIVISION.
       FILE SECTION.
       FD F GLOBAL.
       01 F-REC PIC X(3).
       PROCEDURE DIVISION.
       P-1.
           CALL "P73HI"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73HI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73h.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS F.
       REPORT SECTION.
       RD F.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       I-1.
           OPEN OUTPUT F
           EXIT PROGRAM.
       END PROGRAM P73HI.
       END PROGRAM P73HO.
