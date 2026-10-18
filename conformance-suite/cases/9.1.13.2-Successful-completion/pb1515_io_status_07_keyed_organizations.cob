      *>--- conformance-case
      *> id: pb1515_io_status_07_keyed_organizations
      *> rules: [GR-9.1.13.2-6, GR-9.1.13.2-1, GR-9.1.13.4-1,
      *>    GR-9.1.13.6-5, GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1515KO.
      *> kb/Work PB1515. THE RELATIVE AND INDEXED ARM OF THE SAME
      *> RULE AS pb1515_io_status_07_open_close_only. ISO 9.1.13.2
      *> item 6 gives '07' to an OPEN or CLOSE with a NO REWIND,
      *> REEL/UNIT or FOR REMOVAL phrase, and Annex E.2 item 16 says
      *> the setting "is now restricted to the execution of the OPEN
      *> and CLOSE statements"; the format of the OPEN and CLOSE
      *> phrases admits them for files of sequential organization only
      *> (14.9.6.3 SR1, 14.9.27.3 SR5), so a relative or indexed file
      *> has no statement that can answer '07' at any edition
      *> (docs/CONFORMANCE.md section 3). Every statement a keyed file
      *> accepts therefore answers its own status, on the success path
      *> and on each failure path. A line shows the status each
      *> statement leaves and how often the Format 1 USE declarative
      *> has run: it runs "upon the unsuccessful execution of an
      *> input-output operation unless an AT END or INVALID KEY phrase
      *> takes precedence" (14.9.49.4 GR6), so a failure that names its
      *> phrase does not run it.
      *> EXPECTED VALUES, EACH FROM ITS RULE (9.1.13.x item):
      *>   OPEN INPUT of an absent file that is not optional: 35
      *>   (9.1.13.6 item 5), the declarative runs. OPEN OUTPUT creates
      *>   the file: 00 (9.1.13.2 item 1). A WRITE of a new key: 00.
      *>   A WRITE whose key exists: 22 (9.1.13.5 item 2 a, b) and its
      *>   INVALID KEY imperative runs; without the phrase the
      *>   declarative runs instead. A random READ or a START of a key
      *>   that does not exist: 23 (item 3 a, c). A REWRITE and a
      *>   DELETE of an existing record: 00. A READ NEXT past the last
      *>   record: 10 (9.1.13.4 item 1 a) and its AT END imperative
      *>   runs. A plain CLOSE: 00.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "pb1515kr.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-R.
           SELECT I ASSIGN TO "pb1515ki.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS I-KEY
               FILE STATUS IS WS-I.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(4).
       FD I.
       01 I-REC.
           05 I-KEY PIC XX.
           05 I-DATA PIC XX.
       WORKING-STORAGE SECTION.
       01 WS-R PIC XX.
       01 WS-I PIC XX.
       01 WS-RK PIC 9.
       01 WS-USE PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON R I.
       IO-ERR-P.
           ADD 1 TO WS-USE.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN INPUT R
           DISPLAY "R-ABSENT=" WS-R " USE=" WS-USE
           OPEN INPUT I
           DISPLAY "I-ABSENT=" WS-I " USE=" WS-USE
           OPEN OUTPUT R
           DISPLAY "R-OUT=" WS-R " USE=" WS-USE
           OPEN OUTPUT I
           DISPLAY "I-OUT=" WS-I " USE=" WS-USE
           MOVE 1 TO WS-RK
           MOVE "R001" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-W1 INVALID KEY RAN"
           END-WRITE
           DISPLAY "R-W1=" WS-R " USE=" WS-USE
           MOVE 2 TO WS-RK
           MOVE "R002" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-W2 INVALID KEY RAN"
           END-WRITE
           DISPLAY "R-W2=" WS-R " USE=" WS-USE
           WRITE R-REC INVALID KEY DISPLAY "R-W3 INVALID KEY RAN"
           END-WRITE
           DISPLAY "R-W3=" WS-R " USE=" WS-USE
           WRITE R-REC
           DISPLAY "R-W4=" WS-R " USE=" WS-USE
           MOVE "K1A1" TO I-REC
           WRITE I-REC INVALID KEY DISPLAY "I-W1 INVALID KEY RAN"
           END-WRITE
           DISPLAY "I-W1=" WS-I " USE=" WS-USE
           MOVE "K2B2" TO I-REC
           WRITE I-REC INVALID KEY DISPLAY "I-W2 INVALID KEY RAN"
           END-WRITE
           DISPLAY "I-W2=" WS-I " USE=" WS-USE
           WRITE I-REC INVALID KEY DISPLAY "I-W3 INVALID KEY RAN"
           END-WRITE
           DISPLAY "I-W3=" WS-I " USE=" WS-USE
           WRITE I-REC
           DISPLAY "I-W4=" WS-I " USE=" WS-USE
           CLOSE R
           DISPLAY "R-CLOSE=" WS-R " USE=" WS-USE
           CLOSE I
           DISPLAY "I-CLOSE=" WS-I " USE=" WS-USE
           OPEN I-O R
           DISPLAY "R-IO=" WS-R " USE=" WS-USE
           OPEN I-O I
           DISPLAY "I-IO=" WS-I " USE=" WS-USE
           MOVE 2 TO WS-RK
           READ R INVALID KEY DISPLAY "R-R1 INVALID KEY RAN" END-READ
           DISPLAY "R-R1=" WS-R " " R-REC " USE=" WS-USE
           MOVE "R2RW" TO R-REC
           REWRITE R-REC INVALID KEY DISPLAY "R-RW INVALID KEY RAN"
           END-REWRITE
           DISPLAY "R-RW=" WS-R " USE=" WS-USE
           MOVE 9 TO WS-RK
           READ R INVALID KEY DISPLAY "R-R2 INVALID KEY RAN" END-READ
           DISPLAY "R-R2=" WS-R " USE=" WS-USE
           READ R
           DISPLAY "R-R3=" WS-R " USE=" WS-USE
           START R KEY IS EQUAL TO WS-RK
               INVALID KEY DISPLAY "R-ST INVALID KEY RAN"
           END-START
           DISPLAY "R-ST=" WS-R " USE=" WS-USE
           MOVE 1 TO WS-RK
           DELETE R RECORD INVALID KEY DISPLAY "R-DEL INVALID KEY RAN"
           END-DELETE
           DISPLAY "R-DEL=" WS-R " USE=" WS-USE
           MOVE "K2" TO I-KEY
           READ I INVALID KEY DISPLAY "I-R1 INVALID KEY RAN" END-READ
           DISPLAY "I-R1=" WS-I " " I-REC " USE=" WS-USE
           MOVE "K2X2" TO I-REC
           REWRITE I-REC INVALID KEY DISPLAY "I-RW INVALID KEY RAN"
           END-REWRITE
           DISPLAY "I-RW=" WS-I " USE=" WS-USE
           MOVE "K9" TO I-KEY
           READ I INVALID KEY DISPLAY "I-R2 INVALID KEY RAN" END-READ
           DISPLAY "I-R2=" WS-I " USE=" WS-USE
           READ I
           DISPLAY "I-R3=" WS-I " USE=" WS-USE
           START I KEY IS EQUAL TO I-KEY
               INVALID KEY DISPLAY "I-ST INVALID KEY RAN"
           END-START
           DISPLAY "I-ST=" WS-I " USE=" WS-USE
           MOVE "K1" TO I-KEY
           DELETE I RECORD INVALID KEY DISPLAY "I-DEL INVALID KEY RAN"
           END-DELETE
           DISPLAY "I-DEL=" WS-I " USE=" WS-USE
           CLOSE R
           DISPLAY "R-CLOSE2=" WS-R " USE=" WS-USE
           CLOSE I
           DISPLAY "I-CLOSE2=" WS-I " USE=" WS-USE
           OPEN INPUT R
           OPEN INPUT I
           READ R NEXT AT END DISPLAY "R-N1 AT END RAN" END-READ
           DISPLAY "R-N1=" WS-R " " R-REC " USE=" WS-USE
           READ R NEXT AT END DISPLAY "R-N2 AT END RAN" END-READ
           DISPLAY "R-N2=" WS-R " USE=" WS-USE
           READ I NEXT AT END DISPLAY "I-N1 AT END RAN" END-READ
           DISPLAY "I-N1=" WS-I " " I-REC " USE=" WS-USE
           READ I NEXT AT END DISPLAY "I-N2 AT END RAN" END-READ
           DISPLAY "I-N2=" WS-I " USE=" WS-USE
           CLOSE R
           CLOSE I
           DISPLAY "END=" WS-R WS-I " USE=" WS-USE
           STOP RUN.
