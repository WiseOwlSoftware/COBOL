      *>--- conformance-case
      *> id: 1985-SR-XI_2_4_3-1
      *> rules: [1985-SR-XI.2.4.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.2.4.3 SR1 (THE I-O-CONTROL PARAGRAPH). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SAMEREC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "SORTWK".
           SELECT OFILE ASSIGN TO "OUTF".
       I-O-CONTROL.
           SAME RECORD AREA FOR SFILE OFILE.
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  S-REC PIC X(5).
       FD  OFILE LABEL RECORDS ARE STANDARD.
       01  O-REC PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT OFILE.
           SORT SFILE ON ASCENDING KEY S-REC
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           CLOSE OFILE.
           STOP RUN.
       IN-PROC.
           MOVE "HELLO" TO O-REC.
           DISPLAY S-REC.
       OUT-PROC.
           RETURN SFILE AT END DISPLAY "END".
