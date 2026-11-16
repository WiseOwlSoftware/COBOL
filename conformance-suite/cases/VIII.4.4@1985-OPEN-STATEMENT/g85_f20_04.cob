      *>--- conformance-case
      *> id: g85_f20_04
      *> rules: [1985-GR-VIII.4.4.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR4 - "An OPEN statement must be successfully
      *> executed prior to the execution of any of the permissible
      *> input-output statements", and its Table 2 (Permissible
      *> Statements) gives, for each access mode, the open modes in
      *> which READ, WRITE, REWRITE, START and DELETE may be used.
      *> Every X cell exercised here succeeds, §1985:VIII.1.3.4 "The
      *> input-output statement is successfully executed and no further
      *> information is available" = 00; every blank cell exercised
      *> gets the status §1985:VIII.1.3.4 names for it:
      *>   "The execution of a READ or START statement is attempted on
      *>   a file not open in the input or I-O mode" = 47;
      *>   "The execution of a WRITE statement is attempted on a file
      *>   not open in the I-O, output, or extend mode" = 48;
      *>   "The execution of a DELETE or REWRITE statement is attempted
      *>   on a file not open in the I-O mode" = 49.
      *> Each REWRITE or DELETE refused in the input mode follows a
      *> successful READ (sequential access) or is keyed (dynamic
      *> access), so status 43 never also applies. The never-opened
      *> READ is the "successfully executed prior" leg (47). Lines:
      *>   R  sequential access: NOTOPEN READ 47; OUTPUT WRITE x3 00,
      *>      READ 47, START 47; INPUT READ, START, READ 00, WRITE 48,
      *>      REWRITE 49, DELETE 49; I-O READ, REWRITE, READ, DELETE,
      *>      START, READ 00; EXTEND WRITE 00, READ 47; read-back
      *>      NEW1 SEQ3 SEQ4 then at end 10 (SEQ2 deleted, SEQ1
      *>      rewritten, SEQ4 appended).
      *>   D  dynamic access: OUTPUT WRITE 00, READ 47, START 47; I-O
      *>      WRITE 00, random READ 00, START 00, READ NEXT 00; INPUT
      *>      WRITE 48, REWRITE 49, DELETE 49.
      *>   N  random access: OUTPUT WRITE 00, READ 47; I-O WRITE 00,
      *>      READ 00; INPUT READ 00, WRITE 48.
      *> An INVALID KEY imperative that ran on a 4x status would add an
      *> unexpected line: 4x is a logic error, not the invalid key
      *> condition. Continuing after a 4x status with no USE procedure
      *> is the implementor's choice (§1985:VIII.1.3.4); WiseOwl
      *> continues.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2004.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R ASSIGN TO "g85f2004.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL RELATIVE KEY IS RK
               FILE STATUS IS ST-R.
           SELECT D ASSIGN TO "g85f2004d.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC RELATIVE KEY IS DK
               FILE STATUS IS ST-D.
           SELECT N ASSIGN TO "g85f2004n.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM RELATIVE KEY IS NK
               FILE STATUS IS ST-N.
       DATA DIVISION.
       FILE SECTION.
       FD R.
       01 R-REC PIC X(4).
       FD D.
       01 D-REC PIC X(4).
       FD N.
       01 N-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4).
       01 DK PIC 9(4).
       01 NK PIC 9(4).
       01 ST-R PIC XX.
       01 ST-D PIC XX.
       01 ST-N PIC XX.
       01 S1 PIC XX.
       01 S2 PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-NOTOPEN-READ=" ST-R
           OPEN OUTPUT R
           MOVE "SEQ1" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-WINV" END-WRITE
           MOVE ST-R TO S1
           MOVE "SEQ2" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-WINV" END-WRITE
           MOVE ST-R TO S2
           MOVE "SEQ3" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-WINV" END-WRITE
           DISPLAY "R-OUTPUT-WRITE=" S1 S2 ST-R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-OUTPUT-READ=" ST-R
           MOVE 1 TO RK
           START R KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "R-SINV" END-START
           DISPLAY "R-OUTPUT-START=" ST-R
           CLOSE R
           OPEN INPUT R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-INPUT-READ=" ST-R " " R-REC
           MOVE 3 TO RK
           START R KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "R-SINV" END-START
           DISPLAY "R-INPUT-START=" ST-R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-INPUT-READ=" ST-R " " R-REC
           MOVE "BAD9" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-WINV" END-WRITE
           DISPLAY "R-INPUT-WRITE=" ST-R
           CLOSE R
           OPEN INPUT R
           READ R AT END DISPLAY "R-AT-END" END-READ
           REWRITE R-REC
           DISPLAY "R-INPUT-REWRITE=" ST-R
           CLOSE R
           OPEN INPUT R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DELETE R RECORD
           DISPLAY "R-INPUT-DELETE=" ST-R
           CLOSE R
           OPEN I-O R
           READ R AT END DISPLAY "R-AT-END" END-READ
           MOVE ST-R TO S1
           MOVE "NEW1" TO R-REC
           REWRITE R-REC
           DISPLAY "R-IO-READ-REWRITE=" S1 ST-R
           READ R AT END DISPLAY "R-AT-END" END-READ
           MOVE ST-R TO S1
           DELETE R RECORD
           DISPLAY "R-IO-READ-DELETE=" S1 ST-R
           MOVE 3 TO RK
           START R KEY IS EQUAL TO RK
               INVALID KEY DISPLAY "R-SINV" END-START
           MOVE ST-R TO S1
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-IO-START-READ=" S1 ST-R " " R-REC
           CLOSE R
           OPEN EXTEND R
           MOVE "SEQ4" TO R-REC
           WRITE R-REC INVALID KEY DISPLAY "R-WINV" END-WRITE
           DISPLAY "R-EXTEND-WRITE=" ST-R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-EXTEND-READ=" ST-R
           CLOSE R
           OPEN INPUT R
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-CHECK=" ST-R " " R-REC
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-CHECK=" ST-R " " R-REC
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-CHECK=" ST-R " " R-REC
           READ R AT END DISPLAY "R-AT-END" END-READ
           DISPLAY "R-CHECK=" ST-R
           CLOSE R
           OPEN OUTPUT D
           MOVE 2 TO DK
           MOVE "DYN2" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D-WINV" END-WRITE
           DISPLAY "D-OUTPUT-WRITE=" ST-D
           READ D INVALID KEY DISPLAY "D-RINV" END-READ
           DISPLAY "D-OUTPUT-READ=" ST-D
           START D KEY IS EQUAL TO DK
               INVALID KEY DISPLAY "D-SINV" END-START
           DISPLAY "D-OUTPUT-START=" ST-D
           CLOSE D
           OPEN I-O D
           MOVE 5 TO DK
           MOVE "DYN5" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D-WINV" END-WRITE
           DISPLAY "D-IO-WRITE=" ST-D
           MOVE 2 TO DK
           READ D INVALID KEY DISPLAY "D-RINV" END-READ
           DISPLAY "D-IO-READ=" ST-D " " D-REC
           MOVE 3 TO DK
           START D KEY IS NOT LESS THAN DK
               INVALID KEY DISPLAY "D-SINV" END-START
           DISPLAY "D-IO-START=" ST-D
           READ D NEXT RECORD AT END DISPLAY "D-AT-END" END-READ
           DISPLAY "D-IO-READNEXT=" ST-D " " D-REC
           CLOSE D
           OPEN INPUT D
           MOVE 7 TO DK
           MOVE "BAD7" TO D-REC
           WRITE D-REC INVALID KEY DISPLAY "D-WINV" END-WRITE
           DISPLAY "D-INPUT-WRITE=" ST-D
           MOVE 2 TO DK
           REWRITE D-REC INVALID KEY DISPLAY "D-RWINV" END-REWRITE
           DISPLAY "D-INPUT-REWRITE=" ST-D
           DELETE D RECORD INVALID KEY DISPLAY "D-DINV" END-DELETE
           DISPLAY "D-INPUT-DELETE=" ST-D
           CLOSE D
           OPEN OUTPUT N
           MOVE 1 TO NK
           MOVE "RAN1" TO N-REC
           WRITE N-REC INVALID KEY DISPLAY "N-WINV" END-WRITE
           DISPLAY "N-OUTPUT-WRITE=" ST-N
           READ N INVALID KEY DISPLAY "N-RINV" END-READ
           DISPLAY "N-OUTPUT-READ=" ST-N
           CLOSE N
           OPEN I-O N
           MOVE 3 TO NK
           MOVE "RAN3" TO N-REC
           WRITE N-REC INVALID KEY DISPLAY "N-WINV" END-WRITE
           DISPLAY "N-IO-WRITE=" ST-N
           MOVE 1 TO NK
           READ N INVALID KEY DISPLAY "N-RINV" END-READ
           DISPLAY "N-IO-READ=" ST-N " " N-REC
           CLOSE N
           OPEN INPUT N
           MOVE 3 TO NK
           READ N INVALID KEY DISPLAY "N-RINV" END-READ
           DISPLAY "N-INPUT-READ=" ST-N " " N-REC
           MOVE 4 TO NK
           WRITE N-REC INVALID KEY DISPLAY "N-WINV" END-WRITE
           DISPLAY "N-INPUT-WRITE=" ST-N
           CLOSE N
           STOP RUN.
