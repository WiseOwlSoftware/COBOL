      *>--- conformance-case
      *> id: g2002_open_table21_seq_relative
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
      *> Organizations: SEQUENTIAL (sequential access; START with FIRST
      *> or LAST, §2002:14.8.37.2 SR2) and RELATIVE in sequential, random
      *> and dynamic access.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN6A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT QS ASSIGN TO "g2opn6a-q.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-Q.
           SELECT RS ASSIGN TO "g2opn6a-rs.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RKS
               FILE STATUS IS ST-S.
           SELECT RR ASSIGN TO "g2opn6a-rr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKR
               FILE STATUS IS ST-R.
           SELECT RV ASSIGN TO "g2opn6a-rd.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RKD
               FILE STATUS IS ST-D.
       DATA DIVISION.
       FILE SECTION.
       FD QS.
       01 Q-REC PIC X(6).
       FD RS.
       01 S-REC PIC X(6).
       FD RR.
       01 R-REC PIC X(6).
       FD RV.
       01 D-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01 RKS  PIC 9(4).
       01 RKR  PIC 9(4).
       01 RKD  PIC 9(4).
       01 ST-Q PIC XX.
       01 ST-S PIC XX.
       01 ST-R PIC XX.
       01 ST-D PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON QS RS RR RV.
       IO-ERR-P.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT QS
           DISPLAY "QS-O-OPEN=" ST-Q
           MOVE "Q00001" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-O-W=" ST-Q
           MOVE "Q00002" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-O-W2=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-O-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-O-RW=" ST-Q(1:1)
           START QS FIRST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-O-ST=" ST-Q
           CLOSE QS
           OPEN INPUT QS
           DISPLAY "QS-I-OPEN=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-I-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-I-RW=" ST-Q
           WRITE Q-REC
           DISPLAY "QS-I-W=" ST-Q
           START QS FIRST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-I-ST=" ST-Q
           CLOSE QS
           OPEN I-O QS
           DISPLAY "QS-IO-OPEN=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-IO-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-IO-RW=" ST-Q
           WRITE Q-REC
           DISPLAY "QS-IO-W=" ST-Q
           START QS LAST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-IO-ST=" ST-Q
           CLOSE QS
           OPEN EXTEND QS
           DISPLAY "QS-E-OPEN=" ST-Q
           MOVE "Q00003" TO Q-REC
           WRITE Q-REC
           DISPLAY "QS-E-W=" ST-Q
           READ QS AT END CONTINUE END-READ
           DISPLAY "QS-E-R=" ST-Q
           REWRITE Q-REC
           DISPLAY "QS-E-RW=" ST-Q(1:1)
           START QS LAST
               INVALID KEY CONTINUE
               NOT INVALID KEY CONTINUE
           END-START
           DISPLAY "QS-E-ST=" ST-Q
           CLOSE QS
           OPEN OUTPUT RS
           DISPLAY "RS-O-OPEN=" ST-S
           MOVE "R00001" TO S-REC
           WRITE S-REC
           DISPLAY "RS-O-W=" ST-S
           MOVE "R00002" TO S-REC
           WRITE S-REC
           DISPLAY "RS-O-W2=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-O-R=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-O-ST=" ST-S
           REWRITE S-REC
           DISPLAY "RS-O-RW=" ST-S(1:1)
           DELETE RS RECORD
           DISPLAY "RS-O-D=" ST-S(1:1)
           CLOSE RS
           OPEN INPUT RS
           DISPLAY "RS-I-OPEN=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-I-R=" ST-S
           REWRITE S-REC
           DISPLAY "RS-I-RW=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-I-R2=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-I-D=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-I-ST=" ST-S
           WRITE S-REC
           DISPLAY "RS-I-W=" ST-S
           CLOSE RS
           OPEN I-O RS
           DISPLAY "RS-IO-OPEN=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-IO-R=" ST-S
           REWRITE S-REC
           DISPLAY "RS-IO-RW=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-IO-R2=" ST-S
           DELETE RS RECORD
           DISPLAY "RS-IO-D=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-IO-ST=" ST-S
           WRITE S-REC
           DISPLAY "RS-IO-W=" ST-S
           CLOSE RS
           OPEN EXTEND RS
           DISPLAY "RS-E-OPEN=" ST-S
           MOVE "R00003" TO S-REC
           WRITE S-REC
           DISPLAY "RS-E-W=" ST-S
           READ RS AT END CONTINUE END-READ
           DISPLAY "RS-E-R=" ST-S
           MOVE 1 TO RKS
           START RS KEY IS EQUAL TO RKS
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RS-E-ST=" ST-S
           REWRITE S-REC
           DISPLAY "RS-E-RW=" ST-S(1:1)
           DELETE RS RECORD
           DISPLAY "RS-E-D=" ST-S(1:1)
           CLOSE RS
           OPEN OUTPUT RR
           DISPLAY "RR-O-OPEN=" ST-R
           MOVE 1 TO RKR
           MOVE "R00001" TO R-REC
           WRITE R-REC
           DISPLAY "RR-O-W=" ST-R
           MOVE 2 TO RKR
           MOVE "R00002" TO R-REC
           WRITE R-REC
           DISPLAY "RR-O-W2=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-O-R=" ST-R
           REWRITE R-REC
           DISPLAY "RR-O-RW=" ST-R
           DELETE RR RECORD
           DISPLAY "RR-O-D=" ST-R
           CLOSE RR
           OPEN INPUT RR
           DISPLAY "RR-I-OPEN=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-I-R=" ST-R
           WRITE R-REC
           DISPLAY "RR-I-W=" ST-R
           REWRITE R-REC
           DISPLAY "RR-I-RW=" ST-R
           DELETE RR RECORD
           DISPLAY "RR-I-D=" ST-R
           CLOSE RR
           OPEN I-O RR
           DISPLAY "RR-IO-OPEN=" ST-R
           MOVE 1 TO RKR
           READ RR
           DISPLAY "RR-IO-R=" ST-R
           REWRITE R-REC
           DISPLAY "RR-IO-RW=" ST-R
           MOVE 3 TO RKR
           MOVE "R00003" TO R-REC
           WRITE R-REC
           DISPLAY "RR-IO-W=" ST-R
           MOVE 2 TO RKR
           DELETE RR RECORD
           DISPLAY "RR-IO-D=" ST-R
           CLOSE RR
           OPEN OUTPUT RV
           DISPLAY "RV-O-OPEN=" ST-D
           MOVE 1 TO RKD
           MOVE "R00001" TO D-REC
           WRITE D-REC
           DISPLAY "RV-O-W=" ST-D
           MOVE 2 TO RKD
           MOVE "R00002" TO D-REC
           WRITE D-REC
           DISPLAY "RV-O-W2=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-O-R=" ST-D
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-O-ST=" ST-D
           REWRITE D-REC
           DISPLAY "RV-O-RW=" ST-D
           DELETE RV RECORD
           DISPLAY "RV-O-D=" ST-D
           CLOSE RV
           OPEN INPUT RV
           DISPLAY "RV-I-OPEN=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-I-R=" ST-D
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-I-ST=" ST-D
           WRITE D-REC
           DISPLAY "RV-I-W=" ST-D
           REWRITE D-REC
           DISPLAY "RV-I-RW=" ST-D
           DELETE RV RECORD
           DISPLAY "RV-I-D=" ST-D
           CLOSE RV
           OPEN I-O RV
           DISPLAY "RV-IO-OPEN=" ST-D
           MOVE 1 TO RKD
           READ RV
           DISPLAY "RV-IO-R=" ST-D
           REWRITE D-REC
           DISPLAY "RV-IO-RW=" ST-D
           MOVE 3 TO RKD
           MOVE "R00003" TO D-REC
           WRITE D-REC
           DISPLAY "RV-IO-W=" ST-D
           MOVE 1 TO RKD
           START RV KEY IS EQUAL TO RKD
               INVALID KEY CONTINUE
           END-START
           DISPLAY "RV-IO-ST=" ST-D
           MOVE 2 TO RKD
           DELETE RV RECORD
           DISPLAY "RV-IO-D=" ST-D
           CLOSE RV
           STOP RUN.
