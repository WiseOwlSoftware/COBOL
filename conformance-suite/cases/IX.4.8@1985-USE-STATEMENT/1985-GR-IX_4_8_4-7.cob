      *>--- conformance-case
      *> id: 1985-GR-IX_4_8_4-7
      *> rules: [1985-GR-IX.4.8.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.4 GR7 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USETEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT AFILE ASSIGN TO AIDX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS AKEY
               FILE STATUS IS AFS.
           SELECT BFILE ASSIGN TO BIDX
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS BKEY
               FILE STATUS IS BFS.
       DATA DIVISION.
       FILE SECTION.
       FD  AFILE
           LABEL RECORDS ARE STANDARD.
       01  AREC.
           02  AKEY    PIC X.
           02  ADATA   PIC X(9).
       FD  BFILE
           LABEL RECORDS ARE STANDARD.
       01  BREC.
           02  BKEY    PIC X.
           02  BDATA   PIC X(9).
       WORKING-STORAGE SECTION.
       77  AFS         PIC XX.
       77  BFS         PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       A-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON AFILE.
       A-PARA.
           DISPLAY "USE A " AFS.
           MOVE "Z" TO BKEY.
           READ BFILE.
           DISPLAY "USE A END".
       B-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON BFILE.
       B-PARA.
           DISPLAY "USE B " BFS.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P1.
           OPEN OUTPUT AFILE BFILE.
           MOVE "A" TO AKEY.
           MOVE "DATA" TO ADATA.
           WRITE AREC.
           MOVE "B" TO BKEY.
           MOVE "DATA" TO BDATA.
           WRITE BREC.
           CLOSE AFILE BFILE.
           OPEN INPUT AFILE BFILE.
           MOVE "X" TO AKEY.
           READ AFILE.
           DISPLAY "DONE".
           CLOSE AFILE BFILE.
           STOP RUN.
