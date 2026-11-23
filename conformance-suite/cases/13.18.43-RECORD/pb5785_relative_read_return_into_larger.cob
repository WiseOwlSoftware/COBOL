      *>--- conformance-case
      *> id: pb5785_relative_read_return_into_larger
      *> rules: [GR-13.18.43.4-6, GR-14.9.30.4-14,
      *>    GR-9.1.13.2-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The keyed READ ... INTO (relative organization) and the SORT's RETURN ... INTO, where the RECORD clause
      *> sizes the record larger than the only record description: the rule of
      *> pb5785_read_into_record_clause_larger, through the other two arms of the implicit move.
      *> §1985:VII.3.8.4 GR3 / §2023:13.18.43.4 GR6: each record has the clause's size, 12.
      *> §1985:VII.4.4.4 GR8b / §2023:14.9.30.4 GR4 b and §2023:14.9.34.4 GR5 b (sized "by rules specified for the RECORD clause"): the current record, sized by
      *>   the RECORD clause, is what INTO moves.
      *> EXPECTED OUTPUT, derived: the relative file's record and the sorted record are both ABCDEFGHIJKL.
      *> Each leg can fail: the five-position description cut either move to W=[ABCDE       ].
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5785C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "PB5785C.DAT" ORGANIZATION RELATIVE
               ACCESS SEQUENTIAL.
           SELECT FR ASSIGN TO "PB5785C.DAT" ORGANIZATION RELATIVE
               ACCESS SEQUENTIAL FILE STATUS IS FS.
           SELECT SF ASSIGN TO "PB5785C.WRK".
       DATA DIVISION.
       FILE SECTION.
       FD FW RECORD CONTAINS 12 CHARACTERS.
       01 RW PIC X(12).
       FD FR RECORD CONTAINS 12 CHARACTERS.
       01 RR PIC X(5).
       SD SF RECORD CONTAINS 12 CHARACTERS.
       01 SR PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 W PIC X(12).
       PROCEDURE DIVISION.
       MAIN-SECTION SECTION.
       P0.
           OPEN OUTPUT FW.
           MOVE "ABCDEFGHIJKL" TO RW.
           WRITE RW INVALID KEY DISPLAY "INVALID".
           CLOSE FW.
           OPEN INPUT FR.
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "REL FS=" FS " W=[" W "]".
           CLOSE FR.
           SORT SF ON ASCENDING KEY SR
               USING FR OUTPUT PROCEDURE IS OUTP.
           STOP RUN.
       OUTP SECTION.
       O1.
           MOVE ALL "*" TO W.
           RETURN SF INTO W AT END DISPLAY "EOF".
           DISPLAY "RET W=[" W "]".
