      *>--- conformance-case
      *> id: g85_f20_05
      *> rules: [1985-GR-VIII.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR1 - a READ of a relative file executes only
      *> on a file open in the input or I-O mode: "The file referenced
      *> by file-name-1 must be open in the input or I-O mode at the
      *> time this statement is executed." Both READ formats are
      *> exercised: the sequential READ of S (sequential access) and
      *> the keyed READ of K (random access). In the input and the I-O
      *> mode each READ succeeds, §1985:VIII.1.3.4 "The input-output
      *> statement is successfully executed and no further information
      *> is available" = 00, delivering the record written. Never
      *> opened, and open in the output mode (and, for S, the extend
      *> mode), each READ gets §1985:VIII.1.3.4 "The execution of a
      *> READ or START statement is attempted on a file not open in the
      *> input or I-O mode" = 47, and neither its AT END nor its
      *> INVALID KEY imperative runs (47 is a logic error). A compiler
      *> that refused the READ in the input or I-O mode, or let it
      *> through in another mode, fails a line. Continuing after a 4x
      *> status with no USE procedure is the implementor's choice
      *> (§1985:VIII.1.3.4); WiseOwl continues.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2005.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "g85f2005s.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-S.
           SELECT K ASSIGN TO "g85f2005k.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS KK
               FILE STATUS IS ST-K.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       FD K.
       01 K-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 KK PIC 9(4).
       01 ST-S PIC XX.
       01 ST-K PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           READ S AT END DISPLAY "S-AT-END" END-READ
           DISPLAY "S-NOTOPEN-READ=" ST-S
           OPEN OUTPUT S
           MOVE "SSS1" TO S-REC
           WRITE S-REC INVALID KEY DISPLAY "S-WINV" END-WRITE
           MOVE "SSS2" TO S-REC
           WRITE S-REC INVALID KEY DISPLAY "S-WINV" END-WRITE
           READ S AT END DISPLAY "S-AT-END" END-READ
           DISPLAY "S-OUTPUT-READ=" ST-S
           CLOSE S
           OPEN INPUT S
           READ S AT END DISPLAY "S-AT-END" END-READ
           DISPLAY "S-INPUT-READ=" ST-S " " S-REC
           CLOSE S
           OPEN I-O S
           READ S AT END DISPLAY "S-AT-END" END-READ
           READ S AT END DISPLAY "S-AT-END" END-READ
           DISPLAY "S-IO-READ=" ST-S " " S-REC
           CLOSE S
           OPEN EXTEND S
           READ S AT END DISPLAY "S-AT-END" END-READ
           DISPLAY "S-EXTEND-READ=" ST-S
           CLOSE S
           MOVE 2 TO KK
           READ K INVALID KEY DISPLAY "K-RINV" END-READ
           DISPLAY "K-NOTOPEN-READ=" ST-K
           OPEN OUTPUT K
           MOVE "KKK2" TO K-REC
           WRITE K-REC INVALID KEY DISPLAY "K-WINV" END-WRITE
           READ K INVALID KEY DISPLAY "K-RINV" END-READ
           DISPLAY "K-OUTPUT-READ=" ST-K
           CLOSE K
           OPEN INPUT K
           MOVE SPACES TO K-REC
           READ K INVALID KEY DISPLAY "K-RINV" END-READ
           DISPLAY "K-INPUT-READ=" ST-K " " K-REC
           CLOSE K
           OPEN I-O K
           MOVE SPACES TO K-REC
           READ K INVALID KEY DISPLAY "K-RINV" END-READ
           DISPLAY "K-IO-READ=" ST-K " " K-REC
           CLOSE K
           STOP RUN.
