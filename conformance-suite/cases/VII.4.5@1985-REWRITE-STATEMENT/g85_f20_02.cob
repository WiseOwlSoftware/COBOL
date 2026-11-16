      *>--- conformance-case
      *> id: g85_f20_02
      *> rules: [1985-GR-VII.4.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR1 - a sequential REWRITE executes only on a
      *> mass storage file open in the I-O mode: "must be a mass
      *> storage file and must be open in the I-O mode at the time of
      *> execution of this statement." The file is a disk file (mass
      *> storage). In the I-O mode the REWRITE after a successful READ
      *> succeeds, §1985:VII.1.3.5 "The input-output statement is
      *> successfully executed and no further information is
      *> available" = 00, and the read-back in the input mode shows
      *> NEW1 in place of REC1 with REC2 untouched. In the input mode,
      *> straight after a successful READ (so status 43's "not a
      *> successfully executed READ" does not also apply), the REWRITE
      *> gets "The execution of a REWRITE statement is attempted on a
      *> file not open in the I-O mode" = 49. A compiler that refused
      *> the I-O REWRITE, did not replace the record, or let the
      *> input-mode REWRITE through fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2002.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85f2002.dat"
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
           OPEN OUTPUT F
           MOVE "REC1" TO F-REC
           WRITE F-REC
           MOVE "REC2" TO F-REC
           WRITE F-REC
           CLOSE F
           OPEN I-O F
           READ F AT END DISPLAY "IO-AT-END" END-READ
           DISPLAY "IO-READ=" ST " " F-REC
           MOVE "NEW1" TO F-REC
           REWRITE F-REC
           DISPLAY "IO-REWRITE=" ST
           CLOSE F
           OPEN INPUT F
           READ F AT END DISPLAY "IN-AT-END" END-READ
           DISPLAY "INPUT-READ=" ST " " F-REC
           READ F AT END DISPLAY "IN-AT-END" END-READ
           DISPLAY "INPUT-READ=" ST " " F-REC
           MOVE "BAD2" TO F-REC
           REWRITE F-REC
           DISPLAY "INPUT-REWRITE=" ST
           CLOSE F
           STOP RUN.
