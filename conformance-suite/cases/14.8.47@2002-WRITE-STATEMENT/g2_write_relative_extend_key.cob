      *>--- conformance-case
      *> id: g2_write_relative_extend_key
      *> rules: [2002-GR-14.8.47.3-26]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR26 - WRITE to a relative file opened EXTEND:
      *>   the first record gets the highest existing relative record
      *>   number plus one, the next ones consecutive numbers, and each
      *>   number is moved into the RELATIVE KEY item of the write file,
      *>   both through record-name-1 and through FILE file-name-1.
      *>   Each cite.py --check OK:
      *>   §2002:14.8.47.3 GR26 "The first record released to the
      *>     operating environment has a relative record number one
      *>     greater than the highest relative record number existing in
      *>     the file"
      *>   §2002:14.8.47.3 GR26 "Subsequent records released to the
      *>     operating environment have consecutively higher relative
      *>     record numbers"
      *>   §2002:14.8.47.3 GR26 "If the RELATIVE KEY clause is specified
      *>     for file-name-1 or the file-name associated with
      *>     record-name-1"
      *>   §2002:14.8.47.3 GR26 "the relative record number of the record
      *>     being released is moved into the relative key data item by
      *>     the operating environment during execution of the WRITE
      *>     statement according to the rules for the MOVE statement"
      *>   §2002:14.8.26.2 SR2 "The EXTEND phrase shall be specified only
      *>     if the access mode of the file connector referenced by
      *>     file-name-1 is sequential" - so FX is ACCESS SEQUENTIAL.
      *> DERIVATION. FR (random) writes records 2 and 5 only, so the
      *> highest existing relative record number is 5 and 1, 3, 4 are
      *> empty. FX (sequential, RELATIVE KEY RK2 PIC 9(3)) is the same
      *> physical file opened EXTEND:
      *>   E1 WRITE X-REC "EXT6" (record-name-1), RK2 preset 0
      *>        5 + 1                          -> E1 00 RK2=006
      *>   E2 WRITE FILE FX FROM "EXT7", RK2 preset 999: file-name-1;
      *>        the number replaces the preset  -> E2 00 RK2=007
      *>   E3 WRITE X-REC "EXT8"                -> E3 00 RK2=008
      *> A sequential read back lists the records in relative record
      *> number order (the read sets RK2 too):
      *>   R 002 REC2 / R 005 REC5 / R 006 EXT6 / R 007 EXT7 /
      *>   R 008 EXT8, then END.
      *> Each leg fails if extend numbering counts records rather than
      *> starting after the highest number (it would give 003), if the
      *> FILE form skips the key move (999 would stay), or if the
      *> numbers are not consecutive.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WREX01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FR ASSIGN TO "g2wrex.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK1 FILE STATUS IS FS.
           SELECT FX ASSIGN TO "g2wrex.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK2 FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FR.
       01 R-REC PIC X(4).
       FD FX.
       01 X-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK1 PIC 9(3).
       01 RK2 PIC 9(3).
       01 FS  PIC XX.
       01 EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT FR
           MOVE 2 TO RK1
           MOVE "REC2" TO R-REC
           WRITE R-REC
           MOVE 5 TO RK1
           MOVE "REC5" TO R-REC
           WRITE R-REC
           CLOSE FR
           OPEN EXTEND FX
           DISPLAY "OPEN " FS
           MOVE 0 TO RK2
           MOVE "EXT6" TO X-REC
           WRITE X-REC
           DISPLAY "E1 " FS " RK2=" RK2
           MOVE 999 TO RK2
           WRITE FILE FX FROM "EXT7"
           DISPLAY "E2 " FS " RK2=" RK2
           MOVE "EXT8" TO X-REC
           WRITE X-REC
           DISPLAY "E3 " FS " RK2=" RK2
           CLOSE FX
           OPEN INPUT FX
           PERFORM UNTIL EOF-SW = "Y"
               READ FX NEXT RECORD
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY "R " RK2 " " X-REC
               END-READ
           END-PERFORM
           DISPLAY "END"
           CLOSE FX
           STOP RUN.
