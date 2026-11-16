      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-4
      *> rules: [2002-GR-A.1.2.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR4 - "The data items referenced by data-name-1,
      *> data-name-2, data-name-3, and data-name-4 contain symbolic
      *> names designating queues, sub-queues, ... , respectively.
      *> These symbolic names shall follow the rules for the formation
      *> of system-names, and shall have been previously defined to the
      *> message control system (MCS)."
      *> The program defines to the MCS (its mcs.json) a four-level
      *> queue structure PAY_Q / HOUR_LY / WEEK-1 / DAY_2 holding M1
      *> and M2. The names use the 2002 COBOL-word characters, hyphen
      *> and underscore (§2002:8.3.1.1, system-names §2002:8.3.1.1.2).
      *> Legs: (L4) all four data items name the defined levels:
      *> ACCEPT MESSAGE COUNT gives 00 and 2 (§2002:A.2.1.3 GR1) -
      *> fails if a level is not read from its data item or an
      *> underscore name is not matched; (L1) the queue alone: the
      *> whole structure under PAY_Q holds 2 - fails if the lower
      *> levels must be named; (S3) an undefined third sub-queue
      *> DAY_9: status 20, "No action taken" (Table A.1), so the
      *> preset count 777777 stays - fails if an undefined name is
      *> treated as defined or the count is changed; (NQ) an undefined
      *> queue: 20; (RV) RECEIVE on the four defined names returns M1
      *> with status 00 - fails if the names do not designate the
      *> queue the message comes from (§2002:A.1.2.3 GR5). The facility
      *> is obsolete at 2002 (§2002:G.2) and draws warning COBOL0903.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD09.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       WORKING-STORAGE SECTION.
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
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""queues"":[{""name"":""PAY_Q"",""subQueues"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[{""name"":""HOUR_LY"",""subQueues"":[{""name"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """WEEK-1"",""subQueues"":[{""name"":""DAY_2"","
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE """messages"":[""M1"",""M2""]}]}]}]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "PAY_Q" TO IN-Q.
           MOVE "HOUR_LY" TO IN-S1.
           MOVE "WEEK-1" TO IN-S2.
           MOVE "DAY_2" TO IN-S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "L4 " IN-STAT " " IN-CNT.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "L1 " IN-STAT " " IN-CNT.
           MOVE "HOUR_LY" TO IN-S1.
           MOVE "WEEK-1" TO IN-S2.
           MOVE "DAY_9" TO IN-S3.
           MOVE 777777 TO IN-CNT.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "S3 " IN-STAT " " IN-CNT.
           MOVE "NOQ" TO IN-Q.
           MOVE SPACES TO IN-S1 IN-S2 IN-S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "NQ " IN-STAT.
           MOVE "PAY_Q" TO IN-Q.
           MOVE "HOUR_LY" TO IN-S1.
           MOVE "WEEK-1" TO IN-S2.
           MOVE "DAY_2" TO IN-S3.
           MOVE SPACES TO BUF.
           RECEIVE CIN MESSAGE INTO BUF.
           DISPLAY "RV " IN-STAT " [" BUF "]".
           STOP RUN.
