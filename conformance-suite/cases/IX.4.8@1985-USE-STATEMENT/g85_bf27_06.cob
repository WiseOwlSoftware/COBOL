      *>--- conformance-case
      *> id: g85_bf27_06
      *> rules: [1985-SR-IX.4.8.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.3 SR4 - "The words ERROR and EXCEPTION are
      *> synonymous and may be used interchangeably." Two indexed files
      *> in random access mode, declared alike and driven alike; one
      *> declarative is spelled ERROR, the other EXCEPTION.
      *> DERIVATION.
      *>  Each file holds the record with prime key K001 only. A format
      *>  2 READ of K001 succeeds (IX.4.5.4 GR17). A format 2 READ of
      *>  K005 with no INVALID KEY phrase finds no record: IX.1.3.4
      *>  "I-O Status = 23" (randomly access a record that does not
      *>  exist), and IX.1.3.5 GR3 executes the file's USE procedure,
      *>  which shows HANDLER-VIA-...=23. Status 23 is not critical
      *>  (only 3x, 4x and some 9x are), so IX.4.8.4 GR6 returns
      *>  control to the statement after the READ: AFTER-...=23. The
      *>  two legs are identical but for the tag.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2706.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g85bf2706a.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K1
               FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "g85bf2706b.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K2
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1.
           05  K1 PIC X(4).
           05  V1 PIC X(4).
       FD  F2.
       01  R2.
           05  K2 PIC X(4).
           05  V2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST1 PIC XX VALUE "??".
       01  ST2 PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SPELLED-ERROR-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F1.
       SPELLED-ERROR-PARA.
           DISPLAY "HANDLER-VIA-ERROR=" ST1.
       SPELLED-EXCEPTION-SECT SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       SPELLED-EXCEPTION-PARA.
           DISPLAY "HANDLER-VIA-EXCEPTION=" ST2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P-CREATE.
           OPEN OUTPUT F1 F2.
           MOVE "K001AAAA" TO R1.
           WRITE R1.
           MOVE "K001BBBB" TO R2.
           WRITE R2.
           CLOSE F1 F2.
       P-READ.
           OPEN INPUT F1 F2.
           MOVE "K001" TO K1.
           READ F1.
           DISPLAY "F1-REC=" R1.
           MOVE "K005" TO K1.
           READ F1.
           DISPLAY "AFTER-F1=" ST1.
           MOVE "K001" TO K2.
           READ F2.
           DISPLAY "F2-REC=" R2.
           MOVE "K005" TO K2.
           READ F2.
           DISPLAY "AFTER-F2=" ST2.
           CLOSE F1 F2.
           DISPLAY "DONE".
           STOP RUN.
