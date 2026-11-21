      *>--- conformance-case
      *> id: pb5976-own-data-name-hides-global-report-generate-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5976, review lead R1-2: GENERATE names the hider.
      *> §1985:X.1.3.8 (cite.py: OK): "a program's reference to such a
      *> name, even when it is a different type of user-defined word,
      *> is to the object which that program describes rather than to
      *> the object, possessing the same name, described in another
      *> program"; §1985:X.1.3.8.2 GR3a (cite.py: OK): "If the name is
      *> declared in program B, the object in program B is the
      *> referenced object."
      *> The container declares RD R1 GLOBAL; the contained program
      *> declares its own 01 R1. There R1 names that data item, so
      *> GENERATE R1 names neither a report nor a detail group and
      *> is refused; the message names the hiding data item.
      *> (INITIATE and TERMINATE have twins in this directory.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P76GO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p76g.rpt".
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
           CALL "P76GI"
           CLOSE RPF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P76GI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R1 PIC X VALUE "L".
       PROCEDURE DIVISION.
       I-1.
           GENERATE R1
           EXIT PROGRAM.
       END PROGRAM P76GI.
       END PROGRAM P76GO.
