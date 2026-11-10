      *>--- conformance-case
      *> id: 1985-GR-VII_2_12_4-1
      *> rules: [1985-GR-VII.2.12.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.12.4 GR1 (THE RERUN CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. RERUNT.
000300 ENVIRONMENT DIVISION.
000400 INPUT-OUTPUT SECTION.
000500 FILE-CONTROL.
000600     SELECT OUTFILE ASSIGN TO OUTDD
000700         ORGANIZATION IS SEQUENTIAL.
000710     SELECT RRFILE ASSIGN TO RRDD
000720         ORGANIZATION IS SEQUENTIAL.
000800 I-O-CONTROL.
000900     RERUN ON RRFILE EVERY END OF REEL OF OUTFILE.
001000 DATA DIVISION.
001100 FILE SECTION.
001200 FD  OUTFILE
001300     LABEL RECORDS ARE STANDARD.
001400 01  OUT-REC PIC X(10).
001410 FD  RRFILE
001420     LABEL RECORDS ARE STANDARD.
001430 01  RR-REC PIC X(10).
001500 PROCEDURE DIVISION.
001600 MAIN-PARA.
001700     OPEN OUTPUT RRFILE OUTFILE.
001800     MOVE "RECORD 001" TO OUT-REC.
001900     WRITE OUT-REC.
002000     CLOSE OUTFILE RRFILE.
002100     DISPLAY "RERUN OK".
002200     STOP RUN.
