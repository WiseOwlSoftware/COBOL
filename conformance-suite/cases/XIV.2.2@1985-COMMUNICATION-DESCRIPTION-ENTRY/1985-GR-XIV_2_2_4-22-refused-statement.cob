      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-22-refused-statement
      *> rules: [1985-GR-XIV.2.2.4-22]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR22 (THE COMMUNICATION DESCRIPTION ENTRY): when the
      *> MCS finds an error in DISABLE OUTPUT, ENABLE OUTPUT, PURGE or SEND,
      *> "each occurrence of data-name-4, up to and including the occurrence
      *> specified by the content of data-name-1, [is] updated". Here the
      *> statement is refused before any destination is acted on: a wrong KEY
      *> (status 40), text longer than the item (50), no text (60). Each error
      *> key is preset to "*", so a key the MCS leaves fails the test; each
      *> then holds a value of Table 2 (§1985:XIV.2.2.6): 0 for a destination
      *> with no error, 1 for an unknown one, 2 for a disabled one.
      *> The program defines D3 (disabled, password PW) and D4 (enabled,
      *> password PW) to the MCS (the file mcs.json of the MCS directory).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR22REF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "ABCDE".
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
           MOVE 2 TO OUT-CNT.
           MOVE 5 TO OUT-LEN.
           MOVE "D3" TO OUT-DEST (1).
           MOVE "NOPE" TO OUT-DEST (2).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           ENABLE OUTPUT COUT WITH KEY "BAD".
           DISPLAY "ENABLE KEY " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE "D4" TO OUT-DEST (1).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           DISABLE OUTPUT COUT WITH KEY "BAD".
           DISPLAY "DISABLE KEY " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           MOVE 6 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "TOO LONG " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           MOVE 0 TO OUT-LEN.
           SEND COUT FROM MSG.
           DISPLAY "NO TEXT " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           MOVE "D3" TO OUT-DEST (1).
           MOVE ALL "*" TO OUT-ERR (1) OUT-ERR (2) OUT-ERR (3).
           MOVE 6 TO OUT-LEN.
           SEND COUT FROM MSG WITH EMI.
           DISPLAY "DISABLED " OUT-STAT " " OUT-ERR (1) OUT-ERR (2)
               OUT-ERR (3).
           STOP RUN.
