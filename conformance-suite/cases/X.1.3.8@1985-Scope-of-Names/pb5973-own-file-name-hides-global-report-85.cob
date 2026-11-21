      *>--- conformance-case
      *> id: pb5973-own-file-name-hides-global-report-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5973, PB5976 (the refusal names the hider).
      *> §1985:X.1.3.8 (cite.py: OK): "a program's reference to such a
      *> name, even when it is a different type of user-defined word,
      *> is to the object which that program describes rather than to
      *> the object, possessing the same name, described in another
      *> program"; §1985:X.1.3.8.2 GR3a (cite.py: OK): "If the name is
      *> declared in program B, the object in program B is the
      *> referenced object."
      *> The container declares RD R1 GLOBAL; the contained program
      *> declares its own file R1. There R1 names that file, so
      *> INITIATE R1 names no report and is refused (COBOL1757). A
      *> compiler that hides an inherited report only behind an own
      *> report initiates the container's report.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73GO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p73g.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF GLOBAL REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9 VALUE 0 GLOBAL.
       REPORT SECTION.
       RD R1 GLOBAL.
       01 DET TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       P-1.
           OPEN OUTPUT RPF
           CALL "P73GI"
           CLOSE RPF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P73GI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R1 ASSIGN TO "p73g1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD R1.
       01 R1-REC PIC X(3).
       PROCEDURE DIVISION.
       I-1.
           INITIATE R1
           EXIT PROGRAM.
       END PROGRAM P73GI.
       END PROGRAM P73GO.
