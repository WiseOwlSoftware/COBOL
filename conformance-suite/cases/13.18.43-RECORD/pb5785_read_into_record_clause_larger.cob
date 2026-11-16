      *>--- conformance-case
      *> id: pb5785_read_into_record_clause_larger
      *> rules: [GR-13.18.43.4-6, GR-14.9.30.4-14, GR-9.1.13.2-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> READ ... INTO where the RECORD clause sizes the record LARGER than the only record description.
      *> §1985:VII.3.8.3 SR1 ("No record description entry for the file may specify a number of character
      *>   positions greater than integer-1") and §2023:13.18.43.3 SR3 ("No record description entry for the
      *>   file may specify a number of bytes greater than integer-1") allow a shorter description.
      *> §1985:VII.3.8.4 GR3: "Integer-1 specifies the number of character positions contained in each record
      *>   in the file" (§2023:13.18.43.4 GR6: "Integer-1 specifies the number of bytes contained in each
      *>   record in the file"), so each record is 12 positions.
      *> §1985:VII.4.4.4 GR8b: "The size of the current record is determined by rules specified in the RECORD
      *>   clause" (§2023:14.9.30.4 GR4 b: the same sentence), so READ INTO moves all 12 positions.
      *> A record of the size the clause states conforms to the file's fixed attributes: no length conflict, so
      *> the status is 00 and not 04 (§1985:VII.1.3.5 GRL2.1b, §2023:9.1.13.2, I-O status 04).
      *> EXPECTED OUTPUT, derived: the file holds one record ABCDEFGHIJKL.
      *>   FS=00 W=[ABCDEFGHIJKL] RR=[ABCDE]   the record area RR holds the leading five positions.
      *> Each leg can fail: the compiler once moved the five-position description (W=[ABCDE       ]).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5785A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "PB5785A.DAT".
           SELECT FR ASSIGN TO "PB5785A.DAT" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FW.
       01 RW PIC X(12).
       FD FR RECORD CONTAINS 12 CHARACTERS.
       01 RR PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 W PIC X(12).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FW.
           MOVE "ABCDEFGHIJKL" TO RW.
           WRITE RW.
           CLOSE FW.
           OPEN INPUT FR.
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "FS=" FS " W=[" W "] RR=[" RR "]".
           CLOSE FR.
           STOP RUN.
