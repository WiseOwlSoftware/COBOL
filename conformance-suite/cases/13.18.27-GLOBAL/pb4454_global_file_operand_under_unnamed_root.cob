      *>--- conformance-case
      *> id: pb4454_global_file_operand_under_unnamed_root
      *> rules: [GR-13.18.27.4-2, GR-12.4.5.13.4-2, GR-14.9.51.4-29]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4454, review round 1 R1-1. The file clause operands of
      *> a GLOBAL file sit under a level-01 entry that has no data-name:
      *> the container OUTERG describes the GLOBAL relative file RFL
      *> with RELATIVE KEY RK and FILE STATUS FS, and the GLOBAL
      *> sequential file SFL with FILE STATUS SFS. RK and FS are
      *> subordinate to one unnamed 01 group and SFS to another.
      *>   python scripts/spec/cite.py --check 13.18.20.3 "If the
      *>   entry-name clause is not specified in a data description
      *>   entry, a report group description entry, or a screen
      *>   description entry, the word FILLER is assumed"
      *>                           -> OK  13.18.20.3 2)
      *> so a level-01 entry written without a name is a legal home for
      *> a named elementary item, and RK, FS and SFS are ordinary named
      *> items of OUTERG that the contained programs may not name.
      *>   python scripts/spec/cite.py --check 12.4.5.13.4 "The relative
      *>   key data item associated with the execution of an
      *>   input-output statement is the data item referenced by
      *>   data-name-1"            -> OK  12.4.5.13.4 2)
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the relative
      *>   record number of the record being released is moved into
      *>   the relative key data item by the operating environment
      *>   during execution of the WRITE statement"
      *>                           -> OK  14.9.51.4 29) a)
      *> INNERG writes RFL once with RK = 2 set by the owner: record 2
      *> is released, status 00, RK stays 0002. INNERS writes SFL once:
      *> status 00 reaches SFS in the container.
      *>
      *> EXPECTED OUTPUT, LINE BY LINE:
      *> RK=0002 FS=00 (INNERG's WRITE used the container's key and
      *>   stored the status into the container's FS)
      *> SFS=00        (INNERS's WRITE stored the container's SFS)
      *>
      *> WHY EACH LEG CAN FAIL: a clause operand under an unnamed root
      *> that the contained program's table does not hold stops the
      *> compilation (RK, FS, SFS); a wrong storage would show 0000 or
      *> spaces in the container.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFL ASSIGN TO "pb4454-fl-rel.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK FILE STATUS IS FS.
           SELECT SFL ASSIGN TO "pb4454-fl-seq.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS SFS.
       DATA DIVISION.
       FILE SECTION.
       FD RFL IS GLOBAL.
       01 RFL-REC PIC X(3).
       FD SFL IS GLOBAL.
       01 SFL-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 FILLER.
           05 RK PIC 9(4) VALUE 0.
           05 FS PIC XX.
       01 FILLER.
           05 SFS PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RFL
           CLOSE RFL
           OPEN I-O RFL
           MOVE 2 TO RK
           CALL "INNERG"
           DISPLAY "RK=" RK " FS=" FS
           CLOSE RFL
           OPEN OUTPUT SFL
           CALL "INNERS"
           DISPLAY "SFS=" SFS
           CLOSE SFL
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FILLER.
           05 LOC PIC 9(4) VALUE 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ZZZ" TO RFL-REC
           WRITE RFL-REC
           EXIT PROGRAM.
       END PROGRAM INNERG.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABC" TO SFL-REC
           WRITE SFL-REC
           EXIT PROGRAM.
       END PROGRAM INNERS.
       END PROGRAM OUTERG.
