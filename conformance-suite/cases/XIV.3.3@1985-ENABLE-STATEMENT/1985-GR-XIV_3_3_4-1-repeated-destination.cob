      *>--- conformance-case
      *> id: 1985-GR-XIV_3_3_4-1-repeated-destination
      *> rules: [1985-GR-XIV.3.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.3.4 GR1: "No action is taken when an ENABLE statement
      *> is executed which specifies a source or destination which is
      *> already connected, except that the value in the status key data
      *> item indicates this condition". D4, defined disabled and named in
      *> two occurrences of the DESTINATION TABLE, is disconnected when
      *> ENABLE begins, so ENABLE connects it (status 00, error keys 0 0);
      *> its second occurrence does not find it "already connected". A
      *> second ENABLE finds it so (15, 5 5). D4, D9 (unknown), D4 gives
      *> 20 and the error keys 0 1 0 (Tables 1 and 2, §1985:XIV.2.2.5 and
      *> §1985:XIV.2.2.6).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR3341DUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       COMMUNICATION SECTION.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 3 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""destinations"":[{""name"":""D4"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":false}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 2 TO OUT-CNT.
           MOVE "D4" TO OUT-DEST (1) OUT-DEST (2).
           ENABLE OUTPUT COUT.
           DISPLAY "ENA D4 D4     " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           ENABLE OUTPUT COUT.
           DISPLAY "ENA D4 D4 AGN " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           DISABLE OUTPUT COUT.
           DISPLAY "DIS D4 D4     " OUT-STAT " " OUT-ERR (1) OUT-ERR (2).
           MOVE 3 TO OUT-CNT.
           MOVE "D9" TO OUT-DEST (2).
           MOVE "D4" TO OUT-DEST (3).
           ENABLE OUTPUT COUT.
           DISPLAY "ENA D4 D9 D4  " OUT-STAT " " OUT-ERR (1)
               OUT-ERR (2) OUT-ERR (3).
           STOP RUN.
