      *>--- conformance-case
      *> id: g85_bf24_09
      *> rules: [1985-GR-IX.4.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR4 - "An OPEN statement must be successfully
      *> executed prior to the execution of any of the permissible
      *> input-output statements", and in Table 2 an 'X' means the
      *> statement, in that access mode, "may be used with the open
      *> mode given at the top of the column". Every X cell of Table 2
      *> is executed after a successful OPEN in its mode, and each
      *> shows successful completion, 00 (§1985:IX.1.3.4), with the
      *> records it produces read back:
      *>   Sequential: READ (Input, I-O), WRITE (Output, Extend),
      *>     REWRITE (I-O), START (Input, I-O), DELETE (I-O).
      *>   Random: READ (Input, I-O), WRITE (Output, I-O),
      *>     REWRITE (I-O), DELETE (I-O).
      *>   Dynamic: READ (Input, I-O), WRITE (Output, I-O),
      *>     REWRITE (I-O), START (Input, I-O), DELETE (I-O).
      *> Blank cells are not executed: a 4x status is critical and
      *> 1985 leaves what follows it to the implementor
      *> (§1985:IX.1.3.4), and the Sequential I-O WRITE cell has no
      *> status the 1985 list assigns it.
      *> Sequential access writes keys in ascending order and the
      *> Extend WRITE uses a key above every key in the file
      *> (§1985:IX.4.9.4 GR14); random and dynamic access write in any
      *> order (§1985:IX.4.9.4 GR15). Any unexpected condition shows
      *> as a USE line or an *-INVALID / *-EOF-EARLY line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2409.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XS ASSIGN TO "g85bf2409s.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS XS-KEY
               FILE STATUS IS ST-S.
           SELECT XR ASSIGN TO "g85bf2409r.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XR-KEY
               FILE STATUS IS ST-R.
           SELECT XV ASSIGN TO "g85bf2409v.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XV-KEY
               FILE STATUS IS ST-V.
       DATA DIVISION.
       FILE SECTION.
       FD  XS.
       01  XS-REC.
           05 XS-KEY PIC X(4).
           05 XS-VAL PIC X(3).
       FD  XR.
       01  XR-REC.
           05 XR-KEY PIC X(4).
           05 XR-VAL PIC X(3).
       FD  XV.
       01  XV-REC.
           05 XV-KEY PIC X(4).
           05 XV-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01  ST-S PIC XX.
       01  ST-R PIC XX.
       01  ST-V PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XS XR XV.
       ERR-PARA.
           DISPLAY "USE " ST-S " " ST-R " " ST-V.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       SEQ-PARA.
           OPEN OUTPUT XS.
           MOVE "K001V01" TO XS-REC.
           WRITE XS-REC INVALID KEY DISPLAY "S-W1-INVALID".
           DISPLAY "S-O-WRITE=" ST-S.
           MOVE "K002V02" TO XS-REC.
           WRITE XS-REC INVALID KEY DISPLAY "S-W2-INVALID".
           DISPLAY "S-O-WRITE=" ST-S.
           CLOSE XS.
           OPEN EXTEND XS.
           MOVE "K003V03" TO XS-REC.
           WRITE XS-REC INVALID KEY DISPLAY "S-W3-INVALID".
           DISPLAY "S-E-WRITE=" ST-S.
           CLOSE XS.
           OPEN INPUT XS.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-I-READ=" ST-S " " XS-REC.
           MOVE "K003" TO XS-KEY.
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY DISPLAY "S-ST1-INVALID".
           DISPLAY "S-I-START=" ST-S.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-I-READ=" ST-S " " XS-REC.
           CLOSE XS.
           OPEN I-O XS.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-IO-READ=" ST-S " " XS-REC.
           MOVE "W01" TO XS-VAL.
           REWRITE XS-REC INVALID KEY DISPLAY "S-RW-INVALID".
           DISPLAY "S-IO-REWRITE=" ST-S.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-IO-READ=" ST-S " " XS-REC.
           DELETE XS RECORD.
           DISPLAY "S-IO-DELETE=" ST-S.
           MOVE "K003" TO XS-KEY.
           START XS KEY IS EQUAL TO XS-KEY
               INVALID KEY DISPLAY "S-ST2-INVALID".
           DISPLAY "S-IO-START=" ST-S.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-IO-READ=" ST-S " " XS-REC.
           CLOSE XS.
           OPEN INPUT XS.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-FINAL=" XS-REC.
           READ XS AT END DISPLAY "S-EOF-EARLY".
           DISPLAY "S-FINAL=" XS-REC.
           READ XS AT END DISPLAY "S-EOF".
           CLOSE XS.
       RAN-PARA.
           OPEN OUTPUT XR.
           MOVE "K002V02" TO XR-REC.
           WRITE XR-REC INVALID KEY DISPLAY "R-W1-INVALID".
           DISPLAY "R-O-WRITE=" ST-R.
           MOVE "K001V01" TO XR-REC.
           WRITE XR-REC INVALID KEY DISPLAY "R-W2-INVALID".
           DISPLAY "R-O-WRITE=" ST-R.
           CLOSE XR.
           OPEN INPUT XR.
           MOVE "K002" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "R-R1-INVALID".
           DISPLAY "R-I-READ=" ST-R " " XR-REC.
           CLOSE XR.
           OPEN I-O XR.
           MOVE "K001" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "R-R2-INVALID".
           DISPLAY "R-IO-READ=" ST-R " " XR-REC.
           MOVE "W01" TO XR-VAL.
           REWRITE XR-REC INVALID KEY DISPLAY "R-RW-INVALID".
           DISPLAY "R-IO-REWRITE=" ST-R.
           MOVE "K003V03" TO XR-REC.
           WRITE XR-REC INVALID KEY DISPLAY "R-W3-INVALID".
           DISPLAY "R-IO-WRITE=" ST-R.
           MOVE "K002" TO XR-KEY.
           DELETE XR RECORD INVALID KEY DISPLAY "R-D-INVALID".
           DISPLAY "R-IO-DELETE=" ST-R.
           CLOSE XR.
           OPEN INPUT XR.
           MOVE "K001" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "R-K001-ABSENT".
           DISPLAY "R-FINAL=" XR-REC.
           MOVE "K002" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "R-K002-ABSENT".
           MOVE "K003" TO XR-KEY.
           READ XR INVALID KEY DISPLAY "R-K003-ABSENT".
           DISPLAY "R-FINAL=" XR-REC.
           CLOSE XR.
       DYN-PARA.
           OPEN OUTPUT XV.
           MOVE "K002V02" TO XV-REC.
           WRITE XV-REC INVALID KEY DISPLAY "V-W1-INVALID".
           DISPLAY "V-O-WRITE=" ST-V.
           MOVE "K001V01" TO XV-REC.
           WRITE XV-REC INVALID KEY DISPLAY "V-W2-INVALID".
           DISPLAY "V-O-WRITE=" ST-V.
           CLOSE XV.
           OPEN INPUT XV.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF-EARLY".
           DISPLAY "V-I-READ=" ST-V " " XV-REC.
           MOVE "K002" TO XV-KEY.
           READ XV INVALID KEY DISPLAY "V-R1-INVALID".
           DISPLAY "V-I-READ=" ST-V " " XV-REC.
           MOVE "K001" TO XV-KEY.
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY DISPLAY "V-ST1-INVALID".
           DISPLAY "V-I-START=" ST-V.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF-EARLY".
           DISPLAY "V-I-READ=" ST-V " " XV-REC.
           CLOSE XV.
           OPEN I-O XV.
           MOVE "K002" TO XV-KEY.
           READ XV INVALID KEY DISPLAY "V-R2-INVALID".
           DISPLAY "V-IO-READ=" ST-V " " XV-REC.
           MOVE "W02" TO XV-VAL.
           REWRITE XV-REC INVALID KEY DISPLAY "V-RW-INVALID".
           DISPLAY "V-IO-REWRITE=" ST-V.
           MOVE "K003V03" TO XV-REC.
           WRITE XV-REC INVALID KEY DISPLAY "V-W3-INVALID".
           DISPLAY "V-IO-WRITE=" ST-V.
           MOVE "K001" TO XV-KEY.
           START XV KEY IS EQUAL TO XV-KEY
               INVALID KEY DISPLAY "V-ST2-INVALID".
           DISPLAY "V-IO-START=" ST-V.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF-EARLY".
           DISPLAY "V-IO-READ=" ST-V " " XV-REC.
           DELETE XV RECORD INVALID KEY DISPLAY "V-D-INVALID".
           DISPLAY "V-IO-DELETE=" ST-V.
           CLOSE XV.
           OPEN INPUT XV.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF-EARLY".
           DISPLAY "V-FINAL=" XV-REC.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF-EARLY".
           DISPLAY "V-FINAL=" XV-REC.
           READ XV NEXT RECORD AT END DISPLAY "V-EOF".
           CLOSE XV.
           STOP RUN.
