*>--- conformance-case
*> id: sort-sd-record-with-managed-slots-refused-2014
*> rules: [GR-14.9.40.4-2, GR-13.18.60.4-9, GR-14.9.32.4-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4772 - the message-tag half of sort-sd-record-with-managed-slots is introduced by COBOL-2023
      *> (USAGE MESSAGE-TAG, 13.18.60.4 GR9; Annex E.2 item 25), so the SORT over an SD record that holds one is refused
      *> below it, on the tag's own introduction gate; the strongly-typed group and the sort statements are all older.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SORTSLOTSREFUSED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "sortslotsr.wrk".
       DATA DIVISION.
       FILE SECTION.
       SD SF.
       01 SREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 SKEY PIC 9(2).
          05 STAG USAGE MESSAGE-TAG.
       01 WS-SRC TYPE T-REC.
       PROCEDURE DIVISION.
       MAIN SECTION.
           SORT SF ON ASCENDING KEY SKEY OF SREC
               INPUT PROCEDURE IS INP
               OUTPUT PROCEDURE IS OUTP.
           STOP RUN.
       INP SECTION.
           MOVE 1 TO SKEY OF WS-SRC
           RELEASE SREC FROM WS-SRC.
       OUTP SECTION.
           RETURN SF AT END CONTINUE END-RETURN.
