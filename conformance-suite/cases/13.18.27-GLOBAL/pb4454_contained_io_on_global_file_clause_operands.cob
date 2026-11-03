      *>--- conformance-case
      *> id: pb4454_contained_io_on_global_file_clause_operands
      *> rules: [GR-13.18.27.4-2, GR-12.4.5.13.4-2, GR-14.9.51.4-29,
      *>    GR-14.9.30.4-25, GR-13.18.43.4-13, GR-13.18.43.4-14,
      *>    GR-13.18.43.4-15, GR-13.18.34.4-6, GR-14.9.40.4-15]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4454. A program CONTAINED in the one that describes a
      *> GLOBAL file runs input-output statements on that file whose
      *> operands are NOT global: the file's own clauses name them
      *> (RELATIVE KEY, RECORD ... DEPENDING ON, LINAGE) in the
      *> container, where they are ordinary local items the contained
      *> program may not name. The compiler used to stop with an
      *> InvalidOperationException from the bound place table.
      *>
      *>  python scripts/spec/cite.py --check 8.4.5 "Name resolution for
      *>   any implicitly referenced data item in a procedure division
      *>   statement always occurs during processing of the source
      *>   element that contains the explicit specification establishing
      *>   that implicit reference"  -> OK  8.4.5
      *>   python scripts/spec/cite.py --check 8.4.5 "it might not be
      *>   possible to reference explicitly that same data item in the
      *>   contained program that has an implicit reference to it"
      *>                             -> OK  8.4.5 (NOTE 1)
      *>  python scripts/spec/cite.py --check 13.18.27.4 "A statement in
      *>   a program contained directly or indirectly within a program
      *>   that describes a global name may reference that name without
      *>   describing it again"      -> OK  13.18.27.4 2)
      *> The clause operand is the FILE's, resolved in the container;
      *> the contained program's READ, WRITE and SORT reach its storage
      *> through the file, never through a name it writes.
      *>
      *> EXPECTED OUTPUT, LINE BY LINE (every value from the rule cited;
      *> the container's items are the ones the checks read):
      *> 10: random WRITE of RND at RND-KEY = 5.
      *>   python scripts/spec/cite.py --check 14.9.51.4 "prior to the
      *>   execution of the WRITE statement the value of the relative
      *>   key data item shall be initialized by the runtime element
      *>   with the relative record number to be associated with the
      *>   record that is to be written"  -> OK  14.9.51.4 29) b)
      *>   No record 5 exists: "10 WRITTEN", status 00. The same WRITE
      *>   again finds record 5:
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the execution
      *>   of the WRITE statement is unsuccessful, the invalid key
      *>   condition exists, and the I-O status for the write file
      *>   connector is set to '22'"  -> OK  14.9.51.4 29) b)
      *>   "10 INVALID KEY", status 22.
      *> 11: random READ at RND-KEY = 5 finds "ZZZ" (status 00); at
      *>   RND-KEY = 7 there is none: status 23
      *>   python scripts/spec/cite.py --check 9.1.13.5 "an attempt is
      *>   made to randomly access a record that does not exist in the
      *>   physical file"            -> OK  9.1.13.5 3) a)
      *>   The owner then READs record 5 itself and sees the contained
      *>   program's "ZZZ": the WRITE used the key 5, the container's.
      *> 20/21/22: SEQR, sequential. Three WRITEs release records 1, 2
      *>   and 3 and
      *>   python scripts/spec/cite.py --check 14.9.51.4 "the relative
      *>   record number of the record being released is moved into the
      *>   relative key data item by the operating environment during
      *>   execution of the WRITE statement"  -> OK  14.9.51.4 29) a)
      *>   so SEQ-KEY is 0003; three READ NEXTs return ONE, TWO, THR and
      *>   python scripts/spec/cite.py --check 14.9.30.4 "the execution
      *>   of a READ statement moves the relative record number of the
      *>   record made available to the relative key data item"
      *>                             -> OK  14.9.30.4 25)
      *>   leave SEQ-KEY at 0003; a fourth READ finds no next record:
      *>   python scripts/spec/cite.py --check 9.1.13.4 "A sequential
      *>   READ statement is attempted and no next or prior logical
      *>   record exists in the physical file"  -> OK  9.1.13.4 1)
      *>   "22 AT END" with status 10, shown after the READ.
      *> 30/31: VARF, RECORD 3 TO 5. The length written is the content
      *>   of the DEPENDING item
      *>  python scripts/spec/cite.py --check 13.18.43.4 "If data-name-1
      *>   is specified, by the content of the data item referenced by
      *>   data-name-1"              -> OK  13.18.43.4 13) a)
      *>   so 3 and 5 are written (status 00, 00); 9 is above integer-3
      *>   python scripts/spec/cite.py --check 13.18.43.4 "the execution
      *>   of the REWRITE or WRITE statement is unsuccessful"
      *>                             -> OK  13.18.43.4 14)
      *>   python scripts/spec/cite.py --check 9.1.13.7 "an attempt is
      *>   made to write or rewrite a record that is larger than the
      *>   largest or smaller than the smallest record allowed by the
      *>   RECORD IS VARYING clause"  -> OK  9.1.13.7 4) a)
      *>   so the third WRITE gives status 44. The two READs give back
      *>   python scripts/spec/cite.py --check 13.18.43.4 "the contents
      *>   of the data item referenced by data-name-1 will indicate the
      *>   number of bytes in the record just read"
      *>                             -> OK  13.18.43.4 15)
      *>   VAR-LEN 03 and then 05 (status 00).
      *> 40: PGE, LINAGE IS PGE-SIZE LINES WITH FOOTING AT PGE-FOOT, the
      *>   container's 4 and 3, read when OPEN OUTPUT completed
      *>   python scripts/spec/cite.py --check 13.18.34.4 "At the
      *>   completion of an OPEN statement with the OUTPUT phrase"
      *>                             -> OK  13.18.34.4 6) b) 1.
      *>   Four plain WRITEs, page size 4 and footing start 3, give
      *>   the answers docs/CONFORMANCE.md pins for the GR26 boundary
      *>   (pb686_linage_gr26_boundary, the PLN file): NO-EOP, EOP, EOP,
      *>   EOP. A footing start or page size read wrongly changes them.
      *> 50: SORT RIN to ROUT, both relative, KEYs IN-KEY and OUT-KEY.
      *>  python scripts/spec/cite.py --check 14.9.40.4 "After execution
      *>   of the SORT statement, the content of the relative key data
      *>   item indicates the last record returned to the file"
      *>                             -> OK  14.9.40.4 15)
      *>   OUT-KEY is 0003 afterwards; the owner reads ROUT back in key
      *>   order: AAA 0001, BBB 0002, CCC 0003 (the GR25 move again).
      *>
      *> WHY EACH LEG CAN FAIL: a place read by item that the contained
      *> program's table does not hold stops the compilation (RND-KEY,
      *> SEQ-KEY, VAR-LEN, PGE-SIZE, PGE-FOOT, IN-KEY, OUT-KEY); a wrong
      *> storage would store the key, the length or the status into
      *> the contained program's own member, so the owner would show
      *> 0000 or the old value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4454.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQR ASSIGN TO "pb4454-seqr.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS SEQ-KEY FILE STATUS IS SEQ-STAT.
           SELECT RND ASSIGN TO "pb4454-rnd.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RND-KEY FILE STATUS IS RND-STAT.
           SELECT VARF ASSIGN TO "pb4454-var.dat"
               FILE STATUS IS VAR-STAT.
           SELECT PGE ASSIGN TO "pb4454-pge.dat".
           SELECT RIN ASSIGN TO "pb4454-rin.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS IN-KEY.
           SELECT ROUT ASSIGN TO "pb4454-rout.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS OUT-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD SEQR IS GLOBAL.
       01 SEQR-REC PIC X(3).
       FD RND IS GLOBAL.
       01 RND-REC PIC X(3).
       FD VARF IS GLOBAL
           RECORD IS VARYING IN SIZE FROM 3 TO 5 CHARACTERS
           DEPENDING ON VAR-LEN.
       01 VARF-REC PIC X(5).
       FD PGE IS GLOBAL LINAGE IS PGE-SIZE LINES
           WITH FOOTING AT PGE-FOOT.
       01 PGE-REC PIC X(4).
       FD RIN IS GLOBAL.
       01 RIN-REC PIC X(3).
       FD ROUT IS GLOBAL.
       01 ROUT-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01 SEQ-KEY PIC 9(4) VALUE 0.
       01 SEQ-STAT PIC XX.
       01 RND-KEY PIC 9(4) VALUE 0.
       01 RND-STAT PIC XX.
       01 VAR-LEN PIC 9(2) VALUE 0.
       01 VAR-STAT PIC XX.
       01 PGE-SIZE PIC 9(2) VALUE 4.
       01 PGE-FOOT PIC 9(2) VALUE 3.
       01 IN-KEY PIC 9(4) VALUE 0.
       01 OUT-KEY PIC 9(4) VALUE 0.
       01 PHASE PIC 9(2).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RND
           CLOSE RND
           OPEN I-O RND
           MOVE 10 TO PHASE
           MOVE 5 TO RND-KEY
           CALL "PB4454I" USING PHASE
           DISPLAY "RND-STAT=" RND-STAT
           CALL "PB4454I" USING PHASE
           DISPLAY "RND-STAT=" RND-STAT
           MOVE 11 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "RND-STAT=" RND-STAT
           MOVE 7 TO RND-KEY
           CALL "PB4454I" USING PHASE
           DISPLAY "RND-STAT=" RND-STAT
           MOVE 5 TO RND-KEY
           READ RND
           DISPLAY "OWNER READ " RND-KEY " " RND-REC
           CLOSE RND
           MOVE 20 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "SEQ-KEY=" SEQ-KEY " SEQ-STAT=" SEQ-STAT
           MOVE 21 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "SEQ-KEY=" SEQ-KEY " SEQ-STAT=" SEQ-STAT
           MOVE 22 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "SEQ-STAT=" SEQ-STAT
           MOVE 23 TO PHASE
           CALL "PB4454I" USING PHASE
           OPEN OUTPUT VARF
           MOVE 3 TO VAR-LEN
           MOVE 30 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "VAR-STAT=" VAR-STAT
           MOVE 5 TO VAR-LEN
           CALL "PB4454I" USING PHASE
           DISPLAY "VAR-STAT=" VAR-STAT
           MOVE 9 TO VAR-LEN
           CALL "PB4454I" USING PHASE
           DISPLAY "VAR-STAT=" VAR-STAT
           CLOSE VARF
           OPEN INPUT VARF
           MOVE 0 TO VAR-LEN
           MOVE 31 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "VAR-LEN=" VAR-LEN " VAR-STAT=" VAR-STAT
           CALL "PB4454I" USING PHASE
           DISPLAY "VAR-LEN=" VAR-LEN " VAR-STAT=" VAR-STAT
           CLOSE VARF
           OPEN OUTPUT PGE
           MOVE 40 TO PHASE
           CALL "PB4454I" USING PHASE
           CLOSE PGE
           OPEN OUTPUT RIN
           MOVE "CCC" TO RIN-REC
           WRITE RIN-REC
           MOVE "AAA" TO RIN-REC
           WRITE RIN-REC
           MOVE "BBB" TO RIN-REC
           WRITE RIN-REC
           CLOSE RIN
           MOVE 50 TO PHASE
           CALL "PB4454I" USING PHASE
           DISPLAY "OUT-KEY=" OUT-KEY
           OPEN INPUT ROUT
           PERFORM 3 TIMES
               READ ROUT
               DISPLAY ROUT-REC " " OUT-KEY
           END-PERFORM
           CLOSE ROUT
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4454I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-WORK ASSIGN TO "pb4454-sw.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SORT-WORK.
       01 SORT-REC PIC X(3).
       LINKAGE SECTION.
       01 PHASE PIC 9(2).
       PROCEDURE DIVISION USING PHASE.
       MAIN-PARA.
           EVALUATE PHASE
           WHEN 10
               MOVE "ZZZ" TO RND-REC
               WRITE RND-REC
                   INVALID KEY DISPLAY "10 INVALID KEY"
                   NOT INVALID KEY DISPLAY "10 WRITTEN"
               END-WRITE
           WHEN 11
               READ RND
                   INVALID KEY DISPLAY "11 NO RECORD"
                   NOT INVALID KEY DISPLAY "11 READ " RND-REC
               END-READ
           WHEN 20
               OPEN OUTPUT SEQR
               MOVE "ONE" TO SEQR-REC
               WRITE SEQR-REC
               MOVE "TWO" TO SEQR-REC
               WRITE SEQR-REC
               MOVE "THR" TO SEQR-REC
               WRITE SEQR-REC
               CLOSE SEQR
           WHEN 21
               OPEN INPUT SEQR
               PERFORM 3 TIMES
                   READ SEQR NEXT
                   DISPLAY "21 READ " SEQR-REC
               END-PERFORM
           WHEN 22
               READ SEQR NEXT
                   AT END DISPLAY "22 AT END"
                   NOT AT END DISPLAY "22 READ " SEQR-REC
               END-READ
           WHEN 23
               CLOSE SEQR
           WHEN 30
               MOVE "ABCDE" TO VARF-REC
               WRITE VARF-REC
           WHEN 31
               READ VARF
                   AT END DISPLAY "31 AT END"
                   NOT AT END DISPLAY "31 READ"
               END-READ
           WHEN 40
               MOVE "AAAA" TO PGE-REC
               WRITE PGE-REC
                   AT END-OF-PAGE DISPLAY "P1 EOP"
                   NOT AT END-OF-PAGE DISPLAY "P1 NO-EOP"
               END-WRITE
               MOVE "BBBB" TO PGE-REC
               WRITE PGE-REC
                   AT END-OF-PAGE DISPLAY "P2 EOP"
                   NOT AT END-OF-PAGE DISPLAY "P2 NO-EOP"
               END-WRITE
               MOVE "CCCC" TO PGE-REC
               WRITE PGE-REC
                   AT END-OF-PAGE DISPLAY "P3 EOP"
                   NOT AT END-OF-PAGE DISPLAY "P3 NO-EOP"
               END-WRITE
               MOVE "DDDD" TO PGE-REC
               WRITE PGE-REC
                   AT END-OF-PAGE DISPLAY "P4 EOP"
                   NOT AT END-OF-PAGE DISPLAY "P4 NO-EOP"
               END-WRITE
           WHEN 50
               SORT SORT-WORK ON ASCENDING KEY SORT-REC
                   USING RIN GIVING ROUT
           END-EVALUATE
           EXIT PROGRAM.
       END PROGRAM PB4454I.
       END PROGRAM PB4454.
