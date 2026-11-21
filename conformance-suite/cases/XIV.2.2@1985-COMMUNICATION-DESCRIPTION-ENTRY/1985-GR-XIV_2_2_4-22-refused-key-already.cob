      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-22-refused-key-already
      *> rules: [1985-GR-XIV.2.2.4-22]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR22 (THE COMMUNICATION DESCRIPTION ENTRY): when the
      *> MCS finds an error in ENABLE OUTPUT or DISABLE OUTPUT, "each
      *> occurrence of data-name-4, up to and including the occurrence
      *> specified by the content of data-name-1, [is] updated". The
      *> statement here is refused as a whole for a wrong KEY (status 40,
      *> Table 1 of §1985:XIV.2.2.5: "No enabling/disabling action taken"),
      *> and each occurrence still holds the value Table 2
      *> (§1985:XIV.2.2.6) gives its destination: 5, "Symbolic destination
      *> already enabled/disabled", for D4 (enabled) under ENABLE and for D3
      *> (disabled) under DISABLE; 0 for the destination the statement would
      *> have changed; 1 for the unknown one. Each error key is preset to
      *> "*", so a key the MCS leaves fails the test. The last statement,
      *> ENABLE with the right key, shows that the refused ones took no
      *> action: D3 is still disabled, so it is enabled now (0), D4 is
      *> already enabled (5), NOPE is unknown (1), and conditions 15 and 20
      *> together are status 80.
      *> The program defines D3 (disabled, password PW) and D4 (enabled,
      *> password PW) to the MCS (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR22KEY5.
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
           MOVE "{""destinations"":["
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""D3"",""key"":""PW"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """enabled"":false},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""D4"",""key"":""PW""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 3 TO OUT-CNT.
           MOVE "D4" TO OUT-DEST (1).
           MOVE "D3" TO OUT-DEST (2).
           MOVE "NOPE" TO OUT-DEST (3).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           ENABLE OUTPUT COUT WITH KEY "BAD".
           DISPLAY "ENABLE KEY " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           DISABLE OUTPUT COUT WITH KEY "BAD".
           DISPLAY "DISABLE KEY " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           ENABLE OUTPUT COUT WITH KEY "PW".
           DISPLAY "ENABLE OK " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           STOP RUN.
