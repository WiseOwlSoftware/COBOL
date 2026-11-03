      *>--- conformance-case
      *> id: pb4454_contained_io_on_global_file_assign_using
      *> rules: [GR-13.18.27.4-2, GR-14.9.40.4-15]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4454, the ASSIGN ... USING operand. The container
      *> PB4454U describes the GLOBAL file DYN, assigned by the content
      *> of its local item FNAME. The contained program PB4454UI opens,
      *> writes and sorts DYN; it cannot name FNAME.
      *>   python scripts/spec/cite.py --check 9.1.21 "whose content at
      *>   the time an OPEN, SORT, or MERGE statement for that file is
      *>   executed uniquely identifies the specific physical file to
      *>   be accessed"             -> OK  9.1.21
      *>  python scripts/spec/cite.py --check 8.4.5 "Name resolution for
      *>   any implicitly referenced data item in a procedure division
      *>   statement always occurs during processing of the source
      *>   element that contains the explicit specification
      *>   establishing that implicit reference"  -> OK  8.4.5
      *> So the contained OPEN and the contained SORT name the physical
      *> file by the content of FNAME, the container's item.
      *>
      *> EXPECTED OUTPUT. FNAME holds "pb4454-using.dat", the literal
      *> the container's second file CHK is assigned to, so CHK is the
      *> same physical file:
      *> phase 1: OPEN OUTPUT DYN, three WRITEs, CLOSE: status 00
      *>   (DYN-STAT is DYN's FILE STATUS item in the container). CHK
      *>   reads the first record written: CCCCC.
      *> phase 2: SORT ... USING DYN GIVING OUTF opens DYN by the same
      *>   content; the records come back in ascending order,
      *>   AAAAA BBBBB CCCCC; status 00.
      *> phase 3: FNAME names a file that does not exist; OPEN INPUT of
      *>   a file that is not OPTIONAL gives status 35
      *>   python scripts/spec/cite.py --check 9.1.13.6 "an OPEN
      *>   statement with the INPUT, I-O, or EXTEND phrase is
      *>   attempted on a file that is not described as optional and
      *>   the physical file is not present"  -> OK  9.1.13.6 5)
      *>
      *> WHY EACH LEG CAN FAIL: with FNAME unread, phase 1 would not
      *> create the file CHK reads and phase 3 could not tell a missing
      *> file from an unnamed one.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4454U.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN ASSIGN USING FNAME
               FILE STATUS IS DYN-STAT.
           SELECT CHK ASSIGN TO "pb4454-using.dat".
       DATA DIVISION.
       FILE SECTION.
       FD DYN IS GLOBAL.
       01 DYN-REC PIC X(5).
       FD CHK.
       01 CHK-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 FNAME PIC X(16) VALUE "pb4454-using.dat".
       01 DYN-STAT PIC XX.
       01 PHASE PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 1 TO PHASE
           CALL "PB4454UI" USING PHASE
           DISPLAY "DYN-STAT=" DYN-STAT
           OPEN INPUT CHK
           READ CHK
           DISPLAY "CHK READ " CHK-REC
           CLOSE CHK
           MOVE 2 TO PHASE
           CALL "PB4454UI" USING PHASE
           DISPLAY "DYN-STAT=" DYN-STAT
           MOVE "pb4454-absent.d" TO FNAME
           MOVE 3 TO PHASE
           CALL "PB4454UI" USING PHASE
           DISPLAY "DYN-STAT=" DYN-STAT
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4454UI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUTF ASSIGN TO "pb4454-sorted.dat".
           SELECT SORT-WORK ASSIGN TO "pb4454-usw.dat".
       DATA DIVISION.
       FILE SECTION.
       FD OUTF.
       01 OUTF-REC PIC X(5).
       SD SORT-WORK.
       01 SORT-REC PIC X(5).
       LINKAGE SECTION.
       01 PHASE PIC 9.
       PROCEDURE DIVISION USING PHASE.
       MAIN-PARA.
           EVALUATE PHASE
           WHEN 1
               OPEN OUTPUT DYN
               MOVE "CCCCC" TO DYN-REC
               WRITE DYN-REC
               MOVE "AAAAA" TO DYN-REC
               WRITE DYN-REC
               MOVE "BBBBB" TO DYN-REC
               WRITE DYN-REC
               CLOSE DYN
           WHEN 2
               SORT SORT-WORK ON ASCENDING KEY SORT-REC
                   USING DYN GIVING OUTF
               OPEN INPUT OUTF
               PERFORM 3 TIMES
                   READ OUTF
                   DISPLAY "SORTED " OUTF-REC
               END-PERFORM
               CLOSE OUTF
           WHEN 3
               OPEN INPUT DYN
           END-EVALUATE
           EXIT PROGRAM.
       END PROGRAM PB4454UI.
       END PROGRAM PB4454U.
