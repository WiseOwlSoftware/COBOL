      *>--- conformance-case
      *> id: g85_f20_01
      *> rules: [1985-GR-VII.4.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR1 - a sequential READ executes only on a
      *> file open in the input or I-O mode: "The file referenced by
      *> file-name-1 must be open in the input or I-O mode at the time
      *> this statement is executed." The permitted modes are shown
      *> succeeding and the others shown refused with the status the
      *> 1985 text gives them, §1985:VII.1.3.5: "The input-output
      *> statement is successfully executed and no further information
      *> is available" = 00, and "The execution of a READ statement is
      *> attempted on a file not open in the input or I-O mode" = 47.
      *> NOTOPEN, OUTPUT and EXTEND are not input or I-O: 47 each, and
      *> the AT END imperative does not run (47 is a logic error, not
      *> the at end condition). INPUT and I-O deliver REC1 then REC2
      *> with 00. A compiler that let READ through in OUTPUT or EXTEND
      *> mode, or refused it in INPUT or I-O mode, fails a line.
      *> Continuing after a 4x status with no USE procedure is the
      *> implementor's choice (§1985:VII.1.3.5); WiseOwl continues.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2001.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85f2001.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           READ F AT END DISPLAY "NOTOPEN-AT-END" END-READ
           DISPLAY "NOTOPEN-READ=" ST
           OPEN OUTPUT F
           MOVE "REC1" TO F-REC
           WRITE F-REC
           MOVE "REC2" TO F-REC
           WRITE F-REC
           READ F AT END DISPLAY "OUTPUT-AT-END" END-READ
           DISPLAY "OUTPUT-READ=" ST
           CLOSE F
           OPEN INPUT F
           READ F AT END DISPLAY "INPUT-AT-END" END-READ
           DISPLAY "INPUT-READ=" ST " " F-REC
           READ F AT END DISPLAY "INPUT-AT-END" END-READ
           DISPLAY "INPUT-READ=" ST " " F-REC
           CLOSE F
           OPEN I-O F
           READ F AT END DISPLAY "IO-AT-END" END-READ
           DISPLAY "IO-READ=" ST " " F-REC
           READ F AT END DISPLAY "IO-AT-END" END-READ
           DISPLAY "IO-READ=" ST " " F-REC
           CLOSE F
           OPEN EXTEND F
           READ F AT END DISPLAY "EXTEND-AT-END" END-READ
           DISPLAY "EXTEND-READ=" ST
           CLOSE F
           STOP RUN.
