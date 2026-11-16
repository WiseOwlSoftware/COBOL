      *>--- conformance-case
      *> id: g85_f20_03
      *> rules: [1985-GR-VII.4.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.7.4 GR1 - a sequential WRITE executes only on a
      *> file open in the output or (level 2) extend mode: "must be
      *> open in the output or extend mode at the time of the execution
      *> of this statement." OUTPUT writes AAAA and EXTEND appends BBBB,
      *> each §1985:VII.1.3.5 "The input-output statement is
      *> successfully executed and no further information is
      *> available" = 00; the read-back in the input mode shows AAAA,
      *> BBBB, then "no next logical record exists in the file" = 10
      *> with its AT END imperative run (the at end condition).
      *> The file never opened, and the file open in the input and in
      *> the I-O mode, each get "The execution of a WRITE statement is
      *> attempted on a file not open in the output or extend mode" =
      *> 48. Those attempts come after the read-back, so no line
      *> depends on what a refused WRITE leaves in the file. A compiler
      *> that refused the EXTEND write, overwrote instead of appending,
      *> or let WRITE through in INPUT or I-O mode fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2003.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85f2003.dat"
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
           MOVE "XXXX" TO F-REC
           WRITE F-REC
           DISPLAY "NOTOPEN-WRITE=" ST
           OPEN OUTPUT F
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           DISPLAY "OUTPUT-WRITE=" ST
           CLOSE F
           OPEN EXTEND F
           MOVE "BBBB" TO F-REC
           WRITE F-REC
           DISPLAY "EXTEND-WRITE=" ST
           CLOSE F
           OPEN INPUT F
           READ F AT END DISPLAY "AT-END" END-READ
           DISPLAY "CHECK=" ST " " F-REC
           READ F AT END DISPLAY "AT-END" END-READ
           DISPLAY "CHECK=" ST " " F-REC
           READ F AT END DISPLAY "AT-END" END-READ
           DISPLAY "CHECK=" ST
           MOVE "CCCC" TO F-REC
           WRITE F-REC
           DISPLAY "INPUT-WRITE=" ST
           CLOSE F
           OPEN I-O F
           MOVE "DDDD" TO F-REC
           WRITE F-REC
           DISPLAY "IO-WRITE=" ST
           CLOSE F
           STOP RUN.
