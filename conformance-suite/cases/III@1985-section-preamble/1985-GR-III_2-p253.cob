      *>--- conformance-case
      *> id: 1985-GR-III_2-p253
      *> rules: [1985-GR-III.2-p253, 1985-GR-III.2-p255, 1985-GR-III.2-p283]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p253; §1985:III.2 p255; §1985:III.2 p283 ((section
      *> preamble)). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/g-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILES85.
      * 1985 III.2 Relative Key / Relative Record Number /
      * Sequential Access: records written sequentially get ordinal
      * numbers 1,2,3; relative key 2 then names the 2nd record.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELW ASSIGN TO "G05REL.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WK.
           SELECT RELR ASSIGN TO "G05REL.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT SF ASSIGN TO "G05SEQ.DAT"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RELW.
       01  WREC       PIC X(5).
       FD  RELR.
       01  RREC       PIC X(5).
       FD  SF.
       01  SREC       PIC X(5).
       WORKING-STORAGE SECTION.
       01  WK         PIC 9(4) VALUE 0.
       01  RK         PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RELW.
           MOVE "AAAAA" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W-INV".
           DISPLAY "WK=" WK.
           MOVE "BBBBB" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W-INV".
           DISPLAY "WK=" WK.
           MOVE "CCCCC" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W-INV".
           DISPLAY "WK=" WK.
           CLOSE RELW.
           OPEN INPUT RELR.
           MOVE 2 TO RK.
           READ RELR INVALID KEY DISPLAY "REL-INVALID".
           DISPLAY "REL2=" RREC.
           MOVE 3 TO RK.
           READ RELR INVALID KEY DISPLAY "REL-INVALID".
           DISPLAY "REL3=" RREC.
           MOVE 1 TO RK.
           START RELR KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "S-INV".
           READ RELR NEXT AT END DISPLAY "REL-END".
           DISPLAY "NEXT=" RREC " KEY=" RK.
           READ RELR NEXT AT END DISPLAY "REL-END".
           DISPLAY "NEXT=" RREC " KEY=" RK.
           CLOSE RELR.
           OPEN OUTPUT SF.
           MOVE "11111" TO SREC. WRITE SREC.
           MOVE "22222" TO SREC. WRITE SREC.
           MOVE "33333" TO SREC. WRITE SREC.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END DISPLAY "SEQ-END".
           DISPLAY "SEQ=" SREC.
           READ SF AT END DISPLAY "SEQ-END".
           DISPLAY "SEQ=" SREC.
           READ SF AT END DISPLAY "SEQ-END".
           DISPLAY "SEQ=" SREC.
           READ SF AT END DISPLAY "SEQ-END".
           CLOSE SF.
           STOP RUN.
