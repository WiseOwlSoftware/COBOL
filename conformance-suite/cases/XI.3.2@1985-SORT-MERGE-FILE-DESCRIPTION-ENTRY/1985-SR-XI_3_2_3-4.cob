      *>--- conformance-case
      *> id: 1985-SR-XI_3_2_3-4
      *> rules: [1985-SR-XI.3.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.3.2.3 SR4 (THE SORT-MERGE FILE DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTVAR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SFILE ASSIGN TO SORTWK.
       DATA DIVISION.
       FILE SECTION.
       SD  SFILE
           RECORD IS VARYING IN SIZE FROM 3 TO 10 CHARACTERS.
       01  SREC.
           05  SKEY  PIC X(3).
           05  SDATA PIC X(7).
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
           MOVE "BBB" TO SKEY.
           MOVE SPACES TO SDATA.
           RELEASE SREC.
           MOVE "AAA" TO SKEY.
           MOVE SPACES TO SDATA.
           RELEASE SREC.
       OUT-PROC SECTION.
       OUT-PARA.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY SKEY.
           RETURN SFILE AT END MOVE "Y" TO EOF-FLAG.
           IF EOF-FLAG = "N" DISPLAY SKEY.
