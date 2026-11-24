      *>--- conformance-case
      *> id: pb6161_second_select_opens_an_open_file_85
      *> rules: [GR-14.9.27.4-2, GR-9.1.13.7-1, GR-14.9.27.4-4,
      *>    1985-GR-VII.4.3.4-5, 1985-GR-VII.1.3.5-L2.4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6161S.
      * kb/Work PB6161 (the owner's decision of 2026-11-24). Three
      * clause-less SELECTs whose ASSIGN literals are one host path,
      * which WiseOwl maps to one physical file (1985 VII.2.3.3 SR3).
      * Each edition's own text decides the second connector's OPEN:
      * COBOL-85 (this test; its twin at 2002 and later is
      * pb6161_second_select_opens_an_open_file): "file" is "the
      * physical file regardless
      * of the file-name used" (1985 II.2), a second OPEN of it must
      * follow a CLOSE (1985 VII.4.3.4 GR5), and status 41 is "An
      * OPEN statement is attempted for a file in the open mode"
      * (1985 VII.1.3.5): every such OPEN answers 41, in every mode.
      * 2002, 2014, 2023 (the twin): 41 is keyed to the FILE CONNECTOR
      * (2002 9.1.12.6, 2023 9.1.13.7 GR1), so it answers only the
      * connector's own re-OPEN. The sharing facility arbitrates the
      * second connector's OPEN under the implementor default (INPUT
      * READ ONLY, every other mode NO OTHER: DOC-A.1-77), and a
      * conflict is status 61 (2002 9.1.12.8, 2023 9.1.13.9 GR1): two
      * readers share; any other pair is refused.
      * An unsuccessful OPEN leaves the file unaffected (the record
      * is still there after the refused OPEN OUTPUT), and the OPEN
      * succeeds once the holder has closed.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "pb6161s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "pb6161s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb6161s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
           SELECT F-C ASSIGN TO "pb6161s.dat"
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
      * (1) A reader holds the file; a second reader, then a third
      * SELECT in each other mode.
           OPEN INPUT F-A.
           OPEN INPUT F-B.
           DISPLAY "A-IN=" A-ST " B-IN=" B-ST.
           OPEN I-O F-C.
           DISPLAY "C-IO=" C-ST.
           OPEN EXTEND F-C.
           DISPLAY "C-EXT=" C-ST.
           OPEN OUTPUT F-C.
           DISPLAY "C-OUT=" C-ST.
      * (2) The refused OUTPUT did not create the file anew.
           READ F-A AT END DISPLAY "A-EOF-1".
           DISPLAY "A-REC1=" A-REC " " A-ST.
           IF B-ST = "00"
               CLOSE F-B
           END-IF.
           CLOSE F-A.
           DISPLAY "A-CLOSE=" A-ST.
      * (3) An updater holds the file; another updater is refused,
      * and opens once the holder has closed.
           OPEN I-O F-B.
           DISPLAY "B-IO=" B-ST.
           OPEN I-O F-A.
           DISPLAY "A-IO=" A-ST.
           CLOSE F-B.
           OPEN I-O F-A.
           DISPLAY "A-IO2=" A-ST.
           CLOSE F-A.
      * (4) The connector's own re-OPEN answers 41 at every edition;
      * the appender's record is in the file after its CLOSE.
           OPEN EXTEND F-C.
           DISPLAY "C-EXT2=" C-ST.
           OPEN EXTEND F-C.
           DISPLAY "C-EXT3=" C-ST.
           MOVE "BBBBB" TO C-REC.
           WRITE C-REC.
           DISPLAY "C-WRITE=" C-ST.
           CLOSE F-C.
           OPEN INPUT F-A.
           READ F-A AT END DISPLAY "A-EOF-2".
           DISPLAY "A-REC2=" A-REC " " A-ST.
           READ F-A AT END DISPLAY "A-EOF-3".
           DISPLAY "A-REC3=" A-REC " " A-ST.
           READ F-A AT END DISPLAY "A-END=" A-ST.
           CLOSE F-A.
           STOP RUN.
