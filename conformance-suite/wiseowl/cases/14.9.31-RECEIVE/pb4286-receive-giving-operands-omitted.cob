      *>--- conformance-case
      *> id: pb4286-receive-giving-operands-omitted
      *> rules: [FMT-14.9.31.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4286 - ISO 14.9.31.2 prints
      *>   RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2
      *>   cite.py: OK  14.9.31.2  (General format) - "identifier-1 data-name-2"
      *> and neither operand after GIVING is bracketed, so a bare GIVING followed by the next statement omits both
      *> and the RECEIVE is in no format. The one fault is the DISPLAY where identifier-1 is due. The single-token
      *> deletion used to drop the DISPLAY because W-Y fits identifier-1, and the period then drew a second, false
      *> COBOL0001 where data-name-2 was due. Expected: the one COBOL0001 at DISPLAY (the count is pinned by
      *> ParseRecoveryDeletionTests.TheVerbOfACompleteNextStatement_IsNotDeleted). SEND and RECEIVE are the
      *> declined asynchronous messaging facility (docs/CONFORMANCE.md section 4 item 1), and declining it
      *> declines its behaviour, not the syntax of its statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4286R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM W-X GIVING
           DISPLAY W-Y.
           STOP RUN.
