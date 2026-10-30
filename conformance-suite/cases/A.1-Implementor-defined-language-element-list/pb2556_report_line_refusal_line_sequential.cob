      *>--- conformance-case
      *> id: pb2556_report_line_refusal_line_sequential
      *> rules: [DOC-A.1-159, GR-13.4.5.4-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2556RL.
      *> ISO/IEC 1989:2023 §13.4.5 FD entry GR4 (§13.4.5.4: the report
      *> writer logical record structure is defined by the implementor,
      *> Annex A.1 item 159), with the file coded character set of
      *> Annex A.1 item 31 (owner decision kb/Work R47) and kb/Work
      *> PB2556.
      *>
      *> The determination (docs/CONFORMANCE.md DOC-A.1-159): a print
      *> line holding a character above U+00FF is NOT written, and the
      *> status the refusal sets in the report file's connector is '91'
      *> for EVERY report file, whatever ORGANIZATION it names - a
      *> report file is never a line sequential file to its connector,
      *> so the '71' of §14.9.51.4 GR23 (a line sequential file's WRITE)
      *> is never the report writer's answer. A program cannot observe
      *> either value: GENERATE is not among the statements §9.1.13.1
      *> lists as setting an I-O status, so the FILE STATUS item is not
      *> stored by it, and the refusal raises no exception condition, so
      *> no USE procedure of the file runs. What a program CAN observe,
      *> and what this test pins, is that on a report file declared
      *> ORGANIZATION LINE SEQUENTIAL with a FILE STATUS item and a USE
      *> AFTER STANDARD ERROR procedure:
      *>  - FILE STATUS reads '00' after every GENERATE and after CLOSE;
      *>  - the USE procedure never runs;
      *>  - the refused line leaves a BLANK line (§14.9.51.4 GR15: the
      *>    unsuccessful write does not take place, its advance
      *>    included, so the next line's advance covers its slot).
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb2556rl.txt"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS WS-FS.
           SELECT CHK ASSIGN TO "pb2556rl.txt"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-A.
       FD  CHK.
       01  CHK-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  WS-FS  PIC XX VALUE "zz".
       01  WS-EOF PIC X  VALUE "N".
       01  WS-A   PIC X(3).
       REPORT SECTION.
       RD  R-A PAGE LIMIT 20 LINES.
       01  DET TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(3) SOURCE WS-A.
       PROCEDURE DIVISION.
       DECLARATIVES.
       PRT-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON PRT.
       PRT-ERR-P.
           DISPLAY "USE RAN FS=" WS-FS.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           DISPLAY "OPEN FS=" WS-FS.
           INITIATE R-A.
           MOVE "A1B" TO WS-A.
           GENERATE DET.
           DISPLAY "GEN1 FS=" WS-FS.
           MOVE "A€B" TO WS-A.
           GENERATE DET.
           DISPLAY "GEN2 FS=" WS-FS.
           MOVE "C3D" TO WS-A.
           GENERATE DET.
           DISPLAY "GEN3 FS=" WS-FS.
           TERMINATE R-A.
           CLOSE PRT.
           DISPLAY "CLOSE FS=" WS-FS.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "[" CHK-REC "]"
               END-READ
           END-PERFORM.
           CLOSE CHK.
           STOP RUN.
