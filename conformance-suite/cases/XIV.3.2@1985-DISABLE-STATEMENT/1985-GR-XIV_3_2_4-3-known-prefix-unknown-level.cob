      *>--- conformance-case
      *> id: 1985-GR-XIV_3_2_4-3-known-prefix-unknown-level
      *> rules: [1985-GR-XIV.3.2.4-3, 1985-GR-XIV.3.3.4-2, 1985-GR-XIV.2.2.4-34]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:B.2.2: "The new communication status key value of 80 is
      *> also returned in the case of an ENABLE or DISABLE statement where
      *> new communication status key condition 15 and communication
      *> status key condition 20 both apply". Table 1 (§1985:XIV.2.2.5)
      *> marks 80 possible for ENABLE INPUT and DISABLE INPUT. Q1 is
      *> enabled and so is Q1/S2, so ENABLE INPUT of Q1/SX (SX unknown)
      *> finds condition 15 for the levels it knows and 20 for SX: 80.
      *> After DISABLE INPUT of Q1 (every level and path below it
      *> disabled), DISABLE INPUT of Q1/SX and of Q1/S2/S9 is 80 again;
      *> ENABLE INPUT of Q1/SX is then only 20: Q1 is not already enabled.
      *> No action is taken for 15, 20 or 80 (§1985:XIV.3.2.4 GR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR3243K80.
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
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""Q1"",""subQueues"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[{""name"":""S1""},{""name"":""S2""}]},"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "{""name"":""Q2""}],""sources"":[{""name"":""T1""}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           MOVE "Q1" TO IN-Q.
           MOVE "SX" TO IN-S1.
           ENABLE INPUT CIN.
           DISPLAY "ENA Q1/SX       " IN-STAT.
           MOVE "S1" TO IN-S1.
           ENABLE INPUT CIN.
           DISPLAY "ENA Q1/S1       " IN-STAT.
           MOVE "QZ" TO IN-Q.
           MOVE SPACES TO IN-S1.
           ENABLE INPUT CIN.
           DISPLAY "ENA QZ          " IN-STAT.
           MOVE "Q1" TO IN-Q.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1          " IN-STAT.
           MOVE "SX" TO IN-S1.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1/SX       " IN-STAT.
           MOVE "S2" TO IN-S1.
           MOVE "S9" TO IN-S2.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1/S2/S9    " IN-STAT.
           MOVE "SX" TO IN-S1.
           MOVE SPACES TO IN-S2.
           ENABLE INPUT CIN.
           DISPLAY "ENA Q1/SX AFTER " IN-STAT.
           MOVE "S2" TO IN-S1.
           DISABLE INPUT CIN.
           DISPLAY "DIS Q1/S2       " IN-STAT.
           STOP RUN.
