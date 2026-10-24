      *>--- conformance-case
      *> id: pb670_cobol85_no_sharing_facility
      *> rules: [DOC-A.1-77, DOC-A.1-131]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB670S85.
      * kb/Work PB670, the owner's determination of 2026-10-23 (option
      * (i)), docs/CONFORMANCE.md DOC-A.1-77 and DOC-A.1-131.
      * ISO 9.1.15: "If no specification is made in either location,
      * the implementor defines the sharing mode in which the file is
      * opened; the implementor-defined sharing mode may be one of the
      * modes specified in this Working Draft International Standard or
      * may be a mode completely specified by the implementor."
      * File sharing and record locking is an OPTIONAL module (Annex
      * A.4.7), and A.4.1 makes "any associated syntax rules, general
      * rules, other rules, exception conditions, and I-O status
      * values" optional with it: the SHARING and LOCK MODE clauses are
      * COBOL-2002 introductions (COBOLNET0900 below 2002) and a
      * COBOL-85 program is compiled for a processor on which the
      * module is not in effect. The mode a connector holds there is
      * the implementor's own, which restricts nobody, so Table 19
      * (14.9.27.4) is not asked and the file sharing conflict
      * condition (9.1.13.9 item 1, I-O status 61) does not exist:
      * every OPEN of a file another connector of the run unit holds
      * open is a normal open, whatever the two modes. The same
      * sources at --std 2002 and later answer 61 where Table 19
      * prints "Unsuccessful open"
      * (cases/14.9.27-OPEN/pb322_default_sharing_by_open_mode).
      * Each leg fails if the OPEN is refused (a status other than
      * 00), including with the 61 that 2002 and later answer, or if
      * the data a leg wrote is not in the file after its CLOSE.
      * Three clause-less SELECTs and one reader share one file.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb670s85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb670s85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb670s85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
           SELECT F-C ASSIGN TO "pb670s85.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS C-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-C.
       01 C-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST    PIC XX.
       01 B-ST    PIC XX.
       01 C-ST    PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED.
           DISPLAY "SEED-OUT=" SEED-ST.
           MOVE "AAAAA" TO SEED-REC.
           WRITE SEED-REC.
           CLOSE F-SEED.
      * (1) Two readers, then an updater, an appender and a writer
      * beside them: Table 19 would refuse all three with 61 (the
      * clause-less default of 2002 and later is READ ONLY for
      * INPUT and NO OTHER for the rest).
           OPEN INPUT F-A.
           OPEN INPUT F-B.
           DISPLAY "A-IN=" A-ST " B-IN=" B-ST.
           OPEN I-O F-C.
           DISPLAY "C-IO=" C-ST.
           CLOSE F-C.
           OPEN EXTEND F-C.
           DISPLAY "C-EXT=" C-ST.
           MOVE "BBBBB" TO C-REC.
           WRITE C-REC.
           DISPLAY "C-WRITE=" C-ST.
           CLOSE F-C.
           CLOSE F-B.
           CLOSE F-A.
      * (2) The appended record is in the file after its CLOSE, and
      * a second updater opens beside the first.
           OPEN INPUT F-A.
           READ F-A AT END DISPLAY "A-EOF-1".
           DISPLAY "A-REC1=" A-REC " " A-ST.
           READ F-A AT END DISPLAY "A-EOF-2".
           DISPLAY "A-REC2=" A-REC " " A-ST.
           CLOSE F-A.
           OPEN I-O F-A.
           OPEN I-O F-B.
           DISPLAY "A-IO=" A-ST " B-IO=" B-ST.
           CLOSE F-B.
           CLOSE F-A.
      * (3) OPEN OUTPUT creates the file (14.9.27.4 GR18) beside a
      * reader of it: Table 19 prints "Unsuccessful open" for every
      * OUTPUT request (9.1.13.9 item 1 e), and here it is 00.
           OPEN INPUT F-A.
           OPEN OUTPUT F-C.
           DISPLAY "C-OUT=" C-ST.
           CLOSE F-C.
           CLOSE F-A.
      * (4) What the OUTPUT created is an empty file.
           OPEN INPUT F-A.
           READ F-A AT END DISPLAY "EMPTY=" A-ST.
           CLOSE F-A.
           STOP RUN.
