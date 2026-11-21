      *>--- conformance-case
      *> id: pb5976-own-condition-name-hides-global-report-85
      *> rules: [1985-GR-X.1.3.8.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5976. A refusal at 85. §1985:X.1.3.8 (cite.py: OK):
      *> "a program's reference to such a name, even when it is a
      *> different type of user-defined word, is to the object which
      *> that program describes"; §1985:X.1.3.8.2 GR3a (cite.py: OK):
      *> "If the name is declared in program B, the object in program
      *> B is the referenced object." The container declares RD R1
      *> GLOBAL; the contained program declares its own level-88 R1
      *> (line 47). There R1 names that condition-name, so TERMINATE
      *> R1 names no report (§1985:XIII.4.7.3 SR1) and is refused
      *> (COBOL1757), and the refusal says that R1 here is the
      *> condition-name at line 47, which hides the container's
      *> report-name. A compiler that terminates the container's
      *> report, or refuses without naming the hider, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P76TO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "p76t.rpt".
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
           INITIATE R1
           CALL "P76TI"
           CLOSE RPF
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P76TI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FLAG PIC 9 VALUE 1.
           88 R1 VALUE 1.
       PROCEDURE DIVISION.
       I-1.
           TERMINATE R1
           EXIT PROGRAM.
       END PROGRAM P76TI.
       END PROGRAM P76TO.
