      *>--- conformance-case
      *> id: 2002-GR-A_1_2_3-31
      *> rules: [2002-GR-A.1.2.3-31]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:A.1.2.3 GR31 - for an input-output CD without INITIAL
      *> "the program shall specify the symbolic name of the source or
      *> destination in data-name-3 prior to the execution of the
      *> first statement referencing cd-name-1." "After executing the
      *> first statement referencing cd-name-1, the content of the
      *> data item referenced by data-name-3 shall not be changed by
      *> the program. If this content is changed, the execution of any
      *> statement referencing cd-name-1 is unsuccessful, and the data
      *> item referenced by data-name-6 is set to indicate unknown
      *> source or destination, as applicable." §2002:A.1.2.4 Table
      *> A.1: SEND on an input-output CD 20 (destinations unknown);
      *> RECEIVE, ENABLE and DISABLE I-O TERMINAL 21 ("Symbolic source
      *> is unknown. No action taken."). Whether the failed RECEIVE runs
      *> its NO DATA phrase is not decided by §2002:A.2.5.3 GR5 (its
      *> c) names queues and sub-queues only), so the phrase is
      *> CONTINUE and both readings print the same lines.
      *> Terminals T1 (message MSG1) and T2 (message WORLD) are both
      *> defined (mcs.json), so a 20 or 21 here comes from the change
      *> alone. Legs: SEND to T1 first, 00, delivered to T1's device;
      *> after the change to T2, SEND 20 with nothing sent to T2's
      *> device, RECEIVE 21 (fails if WORLD is returned), ENABLE and
      *> DISABLE I-O TERMINAL 21.
      *> Control: a second CD whose first statement names T2 sends to
      *> it with 00, so T2 itself is a known destination.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CD31.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT MCS-DEFS ASSIGN TO "mcs.json".
           SELECT DEV1 ASSIGN TO "T1.out".
           SELECT OPTIONAL DEV2 ASSIGN TO "T2.out".
       DATA DIVISION.
       FILE SECTION.
       FD  MCS-DEFS.
       01  DEFS-REC PIC X(60).
       FD  DEV1.
       01  DEV1-REC PIC X(5).
       FD  DEV2.
       01  DEV2-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(5) VALUE "HELLO".
       01  BUF PIC X(5).
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       CD  IOCD2 FOR I-O
           MESSAGE DATE IS IO2-DATE
           MESSAGE TIME IS IO2-TIME
           SYMBOLIC TERMINAL IS IO2-TERM
           TEXT LENGTH IS IO2-LEN
           END KEY IS IO2-END
           STATUS KEY IS IO2-STAT.
       PROCEDURE DIVISION.
       SEED-MCS.
           OPEN OUTPUT MCS-DEFS.
           MOVE "{""terminals"":[{""name"":""T1"",""messages"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[""MSG1""]},{""name"":""T2"",""messages"":"
               TO DEFS-REC.
           WRITE DEFS-REC.
           MOVE "[""WORLD""]}]}"
               TO DEFS-REC.
           WRITE DEFS-REC.
           CLOSE MCS-DEFS.
       MAIN-PARA.
           MOVE "T1" TO IO-TERM.
           MOVE 5 TO IO-LEN.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SEND T1 " IO-STAT.
           MOVE "T2" TO IO-TERM.
           SEND IOCD FROM MSG WITH EMI.
           DISPLAY "SEND T2 " IO-STAT.
           MOVE SPACES TO BUF.
           RECEIVE IOCD MESSAGE INTO BUF
               NO DATA CONTINUE.
           DISPLAY "RECEIVE " IO-STAT " [" BUF "]".
           ENABLE I-O TERMINAL IOCD.
           DISPLAY "ENABLE " IO-STAT.
           DISABLE I-O TERMINAL IOCD.
           DISPLAY "DISABLE " IO-STAT.
           OPEN INPUT DEV1.
           READ DEV1 AT END MOVE "EMPTY" TO DEV1-REC.
           DISPLAY "T1 DEVICE [" DEV1-REC "]".
           CLOSE DEV1.
           OPEN INPUT DEV2.
           READ DEV2 AT END MOVE "EMPTY" TO DEV2-REC.
           DISPLAY "T2 DEVICE [" DEV2-REC "]".
           CLOSE DEV2.
           MOVE "T2" TO IO2-TERM.
           MOVE 5 TO IO2-LEN.
           SEND IOCD2 FROM MSG WITH EMI.
           DISPLAY "CONTROL T2 " IO2-STAT.
           STOP RUN.
