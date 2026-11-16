      *>--- conformance-case
      *> id: g85_f27_01
      *> rules: [1985-GR-XI.4.1.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.1.4 GR1 - MERGE merges all records contained on
      *> file-name-2 and every file-name-3: three ordered input files
      *> (3, 2 and 1 records) are merged GIVING a fourth file, which is
      *> read back. Fails if any record of any USING file is dropped or
      *> duplicated (the COUNT line and the record lines), or if the
      *> records of the second or third file are not merged in. The
      *> keys are distinct, so GR4's tie order is never consulted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2701.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g85f2701a.dat".
           SELECT F2 ASSIGN TO "g85f2701b.dat".
           SELECT F3 ASSIGN TO "g85f2701c.dat".
           SELECT F4 ASSIGN TO "g85f2701d.dat".
           SELECT SM ASSIGN TO "g85f2701s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC        PIC X(2).
       FD  F2.
       01  F2-REC        PIC X(2).
       FD  F3.
       01  F3-REC        PIC X(2).
       FD  F4.
       01  F4-REC        PIC X(2).
       SD  SM.
       01  SM-REC.
           05  S-KEY     PIC X.
           05  S-FILE    PIC X.
       WORKING-STORAGE SECTION.
       01  EOF-SW        PIC X VALUE "N".
       01  REC-COUNT     PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           MOVE "A1" TO F1-REC WRITE F1-REC.
           MOVE "C1" TO F1-REC WRITE F1-REC.
           MOVE "E1" TO F1-REC WRITE F1-REC.
           CLOSE F1.
           OPEN OUTPUT F2.
           MOVE "B2" TO F2-REC WRITE F2-REC.
           MOVE "F2" TO F2-REC WRITE F2-REC.
           CLOSE F2.
           OPEN OUTPUT F3.
           MOVE "D3" TO F3-REC WRITE F3-REC.
           CLOSE F3.
           MERGE SM ON ASCENDING KEY S-KEY
               USING F1 F2 F3
               GIVING F4.
           OPEN INPUT F4.
           PERFORM UNTIL EOF-SW = "Y"
               READ F4
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END
                       ADD 1 TO REC-COUNT
                       DISPLAY F4-REC
               END-READ
           END-PERFORM.
           CLOSE F4.
           DISPLAY "COUNT " REC-COUNT.
           STOP RUN.
