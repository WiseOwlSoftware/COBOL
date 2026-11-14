      *>--- conformance-case
      *> id: pb5210_close_lock_exception_paths_85
      *> rules: [1985-GR-VII.4.2.4-3, 1985-GR-VII.1.3.5-L2.3, 1985-GR-VII.1.3.5-L2.4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5210 -- the abnormal paths of the COBOL-85 physical-
      *> file lock. §1985:VII.4.2.4 GR1: "A CLOSE statement may only be
      *> executed for a file in an open mode", and a CLOSE of a file not
      *> in the open mode is I-O status 42 (§1985:VII.1.3.5 item b, "A
      *> CLOSE statement is attempted for a file not in the open mode").
      *> PART 1: a CLOSE WITH LOCK of a never-opened file-name N1 is 42,
      *> closes nothing and locks nothing, so the physical file opens
      *> through the second file-name N2 (OUTPUT, 00) and through N1
      *> itself (INPUT, 00) and the record N2 wrote reads back.
      *> PART 2: S1 and S2 name the same physical file and are both open
      *> INPUT. CLOSE S1 WITH LOCK locks the physical file (§1985:II.2,
      *> "mean the physical file regardless of the file-name used in the
      *> COBOL program"), and §1985:VII.4.2.4 GR3's lock is that the
      *> file "cannot be opened again during this execution of this run
      *> unit": S2, already open, still reads (00) and closes (00); its
      *> next OPEN, and S1's, is 38 (§1985:VII.1.3.5 item e, "an OPEN
      *> statement is attempted on a file previously closed with lock").
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5210X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT N1 ASSIGN TO "pb5210x.dat" FILE STATUS IS ST.
           SELECT N2 ASSIGN TO "pb5210x.dat" FILE STATUS IS ST.
           SELECT S1 ASSIGN TO "pb5210x.dat" FILE STATUS IS ST.
           SELECT S2 ASSIGN TO "pb5210x.dat" FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  N1.
       01  N1-REC PIC X(4).
       FD  N2.
       01  N2-REC PIC X(4).
       FD  S1.
       01  S1-REC PIC X(4).
       FD  S2.
       01  S2-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CLOSE N1 WITH LOCK.
           DISPLAY "CLOSE N1 WITH LOCK (NOT OPEN) " ST.
           OPEN OUTPUT N2.
           DISPLAY "OPEN OUTPUT N2 " ST.
           MOVE "AAAA" TO N2-REC.
           WRITE N2-REC.
           DISPLAY "WRITE N2 " ST.
           CLOSE N2.
           DISPLAY "CLOSE N2 " ST.
           OPEN INPUT N1.
           DISPLAY "OPEN INPUT N1 " ST.
           READ N1 AT END DISPLAY "N1 AT END".
           DISPLAY "READ N1 " ST " " N1-REC.
           CLOSE N1.
           DISPLAY "CLOSE N1 " ST.
           OPEN INPUT S1.
           DISPLAY "OPEN INPUT S1 " ST.
           OPEN INPUT S2.
           DISPLAY "OPEN INPUT S2 " ST.
           CLOSE S1 WITH LOCK.
           DISPLAY "CLOSE S1 WITH LOCK " ST.
           READ S2 AT END DISPLAY "S2 AT END".
           DISPLAY "READ S2 " ST " " S2-REC.
           CLOSE S2.
           DISPLAY "CLOSE S2 " ST.
           OPEN INPUT S2.
           DISPLAY "OPEN INPUT S2 " ST.
           OPEN INPUT S1.
           DISPLAY "OPEN INPUT S1 " ST.
           STOP RUN.
