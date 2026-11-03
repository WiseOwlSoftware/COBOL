      *>--- conformance-case
      *> id: pb4454_global_file_operand_beside_intermediate_same_name
      *> rules: [GR-13.18.27.4-2, GR-12.4.5.13.4-2, GR-14.9.51.4-29]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4454, the file operand two programs out and a name
      *> in between. The RECURSIVE container OUTERDEEP describes the
      *> GLOBAL relative file RFL with RELATIVE KEY RK and FILE STATUS
      *> FS, both local to it. Its contained program MIDDLE describes
      *> its OWN RK (value 77); INNERMOST, contained in MIDDLE, writes
      *> RFL twice.
      *>   python scripts/spec/cite.py --check 12.4.5.13.4 "The relative
      *>   key data item associated with the execution of an
      *>   input-output statement is the data item referenced by
      *>   data-name-1"            -> OK  12.4.5.13.4 2)
      *> data-name-1 is the RK of OUTERDEEP's SELECT, whatever a program
      *> between the two names RK.
      *>   python scripts/spec/cite.py --check 8.4.6.1 "a source
      *>   element's reference to such a name, even when it is a
      *>   different type of user-defined word, is to the item which
      *>   that source element describes rather than to the item,
      *>   possessing the same name, described in another source
      *>   element"                -> OK  8.4.6.1
      *> so MIDDLE's DISPLAY RK shows MIDDLE's own 0077, untouched.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the relative
      *>   record number of the record being released is moved into
      *>   the relative key data item by the operating environment
      *>   during execution of the WRITE statement"
      *>                           -> OK  14.9.51.4 29) a)
      *> The sequential WRITEs release records 1 and 2, so the
      *> container's RK is 0002 afterwards, with status 00. The
      *> container is RECURSIVE: WiseOwl keeps a recursive program's
      *> working-storage once, on its class (the static data of 13.5.4
      *> GR1), so the contained programs reach it through the class.
      *>
      *> WHY EACH LEG CAN FAIL: a key stored into a member named for
      *> MIDDLE's RK would leave 0000 in the container and change
      *> MIDDLE's 0077.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OUTERDEEP RECURSIVE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFL ASSIGN TO "pb4454-deep.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS RK FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD RFL IS GLOBAL.
       01 RFL-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) VALUE 0.
       01 FS PIC XX.
       PROCEDURE DIVISION.
           OPEN OUTPUT RFL
           CALL "MIDDLE"
           CLOSE RFL
           DISPLAY "RK=" RK " FS=" FS
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. MIDDLE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RK PIC 9(4) VALUE 77.
       PROCEDURE DIVISION.
           CALL "INNERMOST"
           DISPLAY "MIDDLE RK=" RK
           EXIT PROGRAM.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. INNERMOST.
       PROCEDURE DIVISION.
           MOVE "ABC" TO RFL-REC
           WRITE RFL-REC
           WRITE RFL-REC
           EXIT PROGRAM.
       END PROGRAM INNERMOST.
       END PROGRAM MIDDLE.
       END PROGRAM OUTERDEEP.
