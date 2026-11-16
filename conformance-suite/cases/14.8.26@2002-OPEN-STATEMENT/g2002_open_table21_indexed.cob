      *>--- conformance-case
      *> id: g2002_open_table21_indexed
      *> rules: [2002-GR-14.8.26.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.3 GR6 - "For a given file connector, an OPEN
      *> statement shall be successfully executed prior to the execution
      *> of any other permissible input-output statement. In table 21,
      *> ... 'X' at an intersection indicates that the specified
      *> statement, used in the access mode given for that row, may be
      *> used with the open mode given at the top of the column."
      *> Every cell of Table 21 for the organizations below is executed
      *> after a successful OPEN in that mode: an X cell succeeds ('00',
      *> §2002:9.1.12.1 item 1) and a blank cell is refused with the
      *> logic-error status §2002:9.1.12.6 gives it:
      *>   READ or START not open input or I-O ............ item 7, '47'
      *>   WRITE, sequential access, not extend or output .. item 8a, '48'
      *>   WRITE, random or dynamic, not I-O or output ..... item 8b, '48'
      *>   DELETE or REWRITE not open I-O .................. item 9, '49'
      *> (the WRITE value is also §2002:14.8.47.3 GR1 outright).
      *> In SEQUENTIAL access, item 3 ('43': the last I-O statement before
      *> a DELETE or REWRITE was not a successful READ) can apply beside
      *> item 9, and "If more than one value applies, the implementor
      *> determines which" (§2002:9.1.12). The input-mode REWRITE and
      *> DELETE are therefore each placed right after a successful READ,
      *> so item 9 alone applies ('49'); in output and extend mode no READ
      *> can succeed, so those cells display only the class digit '4'.
      *> Random and dynamic access are outside item 3 and pin '49'.
      *> A format 1 USE declarative ending in RESUME AT NEXT STATEMENT
      *> (§2002:14.8.31.3 GR2) takes every refused statement, so continuing
      *> past a fatal status is not left to the implementor (§2002:9.1.12).
      *> The Random/START cell is a compile-time matter (§2002:14.8.37.2
      *> SR1) and is not written.
      *> Organization: INDEXED in sequential, random and dynamic access
      *> (EXTEND only in sequential access, §2002:14.8.26.2 SR2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN6B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XS ASSIGN TO "g2opn6b-xs.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST-S.
           SELECT XR ASSIGN TO "g2opn6b-xr.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XR-KEY
               FILE STATUS IS ST-R.
           SELECT XV ASSIGN TO "g2opn6b-xv.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XV-KEY
               FILE STATUS IS ST-V.
       DATA DIVISION.
       FILE SECTION.
       FD XS.
       01 XS-REC.
          05 XS-KEY PIC X(4).
          05 XS-VAL PIC X(3).
       FD XR.
       01 XR-REC.
          05 XR-KEY PIC X(4).
          05 XR-VAL PIC X(3).
       FD XV.
       01 XV-REC.
          05 XV-KEY PIC X(4).
          05 XV-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-V PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON XS XR XV.
       IO-ERR-P.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT XS
           DISPLAY "XS-O-OPEN=" ST-S
           MOVE "K001" TO XS-KEY
           MOVE "V01" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-O-W=" ST-S
           MOVE "K002" TO XS-KEY
           MOVE "V02" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-O-W2=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-O-R=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-O-ST=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-O-RW=" ST-S(1:1)
           DELETE XS RECORD
           DISPLAY "XS-O-D=" ST-S(1:1)
           CLOSE XS
           OPEN INPUT XS
           DISPLAY "XS-I-OPEN=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-I-R=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-I-RW=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-I-R2=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-I-D=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-I-ST=" ST-S
           WRITE XS-REC
           DISPLAY "XS-I-W=" ST-S
           CLOSE XS
           OPEN I-O XS
           DISPLAY "XS-IO-OPEN=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-IO-R=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-IO-RW=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-IO-R2=" ST-S
           DELETE XS RECORD
           DISPLAY "XS-IO-D=" ST-S
           MOVE "K001" TO XS-KEY
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-IO-ST=" ST-S
           WRITE XS-REC
           DISPLAY "XS-IO-W=" ST-S
           CLOSE XS
           OPEN EXTEND XS
           DISPLAY "XS-E-OPEN=" ST-S
           MOVE "K003" TO XS-KEY
           MOVE "V03" TO XS-VAL
           WRITE XS-REC
           DISPLAY "XS-E-W=" ST-S
           READ XS AT END CONTINUE END-READ
           DISPLAY "XS-E-R=" ST-S
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XS-E-ST=" ST-S
           REWRITE XS-REC
           DISPLAY "XS-E-RW=" ST-S(1:1)
           DELETE XS RECORD
           DISPLAY "XS-E-D=" ST-S(1:1)
           CLOSE XS
           OPEN OUTPUT XR
           DISPLAY "XR-O-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           MOVE "V01" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-O-W=" ST-R
           MOVE "K002" TO XR-KEY
           MOVE "V02" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-O-W2=" ST-R
           READ XR
           DISPLAY "XR-O-R=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-O-RW=" ST-R
           DELETE XR RECORD
           DISPLAY "XR-O-D=" ST-R
           CLOSE XR
           OPEN INPUT XR
           DISPLAY "XR-I-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           READ XR
           DISPLAY "XR-I-R=" ST-R
           WRITE XR-REC
           DISPLAY "XR-I-W=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-I-RW=" ST-R
           DELETE XR RECORD
           DISPLAY "XR-I-D=" ST-R
           CLOSE XR
           OPEN I-O XR
           DISPLAY "XR-IO-OPEN=" ST-R
           MOVE "K001" TO XR-KEY
           READ XR
           DISPLAY "XR-IO-R=" ST-R
           REWRITE XR-REC
           DISPLAY "XR-IO-RW=" ST-R
           MOVE "K003" TO XR-KEY
           MOVE "V03" TO XR-VAL
           WRITE XR-REC
           DISPLAY "XR-IO-W=" ST-R
           MOVE "K002" TO XR-KEY
           DELETE XR RECORD
           DISPLAY "XR-IO-D=" ST-R
           CLOSE XR
           OPEN OUTPUT XV
           DISPLAY "XV-O-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           MOVE "V01" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-O-W=" ST-V
           MOVE "K002" TO XV-KEY
           MOVE "V02" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-O-W2=" ST-V
           READ XV
           DISPLAY "XV-O-R=" ST-V
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-O-ST=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-O-RW=" ST-V
           DELETE XV RECORD
           DISPLAY "XV-O-D=" ST-V
           CLOSE XV
           OPEN INPUT XV
           DISPLAY "XV-I-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           READ XV
           DISPLAY "XV-I-R=" ST-V
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-I-ST=" ST-V
           WRITE XV-REC
           DISPLAY "XV-I-W=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-I-RW=" ST-V
           DELETE XV RECORD
           DISPLAY "XV-I-D=" ST-V
           CLOSE XV
           OPEN I-O XV
           DISPLAY "XV-IO-OPEN=" ST-V
           MOVE "K001" TO XV-KEY
           READ XV
           DISPLAY "XV-IO-R=" ST-V
           REWRITE XV-REC
           DISPLAY "XV-IO-RW=" ST-V
           MOVE "K003" TO XV-KEY
           MOVE "V03" TO XV-VAL
           WRITE XV-REC
           DISPLAY "XV-IO-W=" ST-V
           MOVE "K001" TO XV-KEY
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY CONTINUE
           END-START
           DISPLAY "XV-IO-ST=" ST-V
           MOVE "K002" TO XV-KEY
           DELETE XV RECORD
           DISPLAY "XV-IO-D=" ST-V
           CLOSE XV
           STOP RUN.
