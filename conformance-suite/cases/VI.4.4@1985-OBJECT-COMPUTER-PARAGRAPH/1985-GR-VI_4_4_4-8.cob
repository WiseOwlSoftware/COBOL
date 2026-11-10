      *>--- conformance-case
      *> id: 1985-GR-VI_4_4_4-8
      *> rules: [1985-GR-VI.4.4.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.4.4.4 GR8 (THE OBJECT-COMPUTER PARAGRAPH). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. COLSEQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X.
       OBJECT-COMPUTER. X
           PROGRAM COLLATING SEQUENCE IS REVSEQ.
       SPECIAL-NAMES.
           ALPHABET REVSEQ IS "C" "B" "A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO "SORTWK".
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE.
       01  SREC.
           05  SKEY PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-FLAG PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       MAIN-PARA.
           SORT SFILE ON ASCENDING KEY SKEY
               INPUT PROCEDURE IS IN-PROC
               OUTPUT PROCEDURE IS OUT-PROC.
           STOP RUN.
       IN-PROC SECTION.
       IN-PARA.
           MOVE "A" TO SKEY.
           RELEASE SREC.
           MOVE "B" TO SKEY.
           RELEASE SREC.
           MOVE "C" TO SKEY.
           RELEASE SREC.
       OUT-PROC SECTION.
       OUT-PARA.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           DISPLAY SKEY.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           DISPLAY SKEY.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           DISPLAY SKEY.
