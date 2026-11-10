      *>--- conformance-case
      *> id: 1985-GR-XI_2_5_4-1
      *> rules: [1985-GR-XI.2.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.5.4 GR1 (THE SAME RECORD/SORT/SORT-MERGE AREA
      *> CLAUSE). An output test: the program must translate and run at
      *> 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from the confirmation pass's redraft
      *> (WEAK original) by scripts/spec/place_witnesses.py (kb/Work
      *> PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRA85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT-FILE ASSIGN TO SORTWK.
           SELECT FILE-B ASSIGN TO FILEB.
       I-O-CONTROL.
           SAME RECORD AREA FOR SRT-FILE FILE-B.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT-FILE.
       01  SRT-REC.
           05  SRT-KEY PIC X(10).
       FD  FILE-B
           LABEL RECORDS ARE STANDARD.
       01  REC-B PIC X(5).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P1.
           OPEN OUTPUT FILE-B.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           CLOSE FILE-B.
           STOP RUN.
       IN-SEC SECTION.
       I1.
           MOVE "ABCDEFGHIJ" TO SRT-REC.
           DISPLAY REC-B.
           MOVE "XY" TO REC-B.
           DISPLAY SRT-REC.
           RELEASE SRT-REC.
           MOVE "MNOPQRSTUV" TO SRT-REC.
           RELEASE SRT-REC.
       OUT-SEC SECTION.
       O1.
           RETURN SRT-FILE AT END GO TO O9.
           DISPLAY REC-B.
           GO TO O1.
       O9.
           EXIT.
