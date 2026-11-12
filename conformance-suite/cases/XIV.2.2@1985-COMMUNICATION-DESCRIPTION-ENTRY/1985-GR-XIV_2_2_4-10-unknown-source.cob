      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-10-unknown-source
      *> rules: [1985-GR-XIV.2.2.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR10 (THE COMMUNICATION DESCRIPTION ENTRY): data-name-7
      *> receives the symbolic name of the source of the message, and "if the
      *> symbolic name of the communication terminal is not known to the MCS,
      *> the content of the data item referenced by data-name-7 will contain
      *> spaces". D2 delivers to Q1 and names no source, so the MCS knows no
      *> source for what D2 delivers: the received source is spaces. D1
      *> delivers to Q1 as the source SRC1, which the MCS knows: SRC1.
      *> The item is preset to "#", so a name left unwritten fails the test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR10SRC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
       01  W PIC X(4) VALUE "WXYZ".
       01  BUF PIC X(10).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       CD  COUT FOR OUTPUT
           DESTINATION COUNT IS OUT-CNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-STAT
           DESTINATION TABLE OCCURS 1 TIMES
           ERROR KEY IS OUT-ERR
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """sources"":[{""name"":""SRC1""}],"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """destinations"":[{""name"":""D1"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """deliverTo"":""Q1"",""source"":""SRC1""},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""D2"",""deliverTo"":""Q1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE 1 TO OUT-CNT.
           MOVE 4 TO OUT-LEN.
           MOVE "D2" TO OUT-DEST (1).
           SEND COUT FROM W WITH EMI.
           PERFORM RCV-ONE.
           MOVE "D1" TO OUT-DEST (1).
           SEND COUT FROM W WITH EMI.
           PERFORM RCV-ONE.
           STOP RUN.
       RCV-ONE.
           MOVE "Q1" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE ALL "#" TO IN-SRC.
           MOVE ALL "*" TO BUF.
           RECEIVE CIN MESSAGE INTO BUF
               NO DATA DISPLAY "NO DATA"
               WITH DATA DISPLAY "RCV " IN-STAT " [" BUF "] SRC["
                   IN-SRC "]".
