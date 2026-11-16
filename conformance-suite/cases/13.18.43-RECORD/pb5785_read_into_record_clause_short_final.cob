      *>--- conformance-case
      *> id: pb5785_read_into_record_clause_short_final
      *> rules: [GR-13.18.43.4-6, GR-14.9.30.4-14, GR-9.1.13.2-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> READ ... INTO of a SHORT FINAL record where the RECORD clause (12) sizes the record above the only
      *> record description (5): the abnormal (status 04) path of the PB5785 connector-image arm.
      *> §1985:VII.3.8.4 GR3 / §2023:13.18.43.4 GR6: each record is 12 positions, so the 19-byte file holds one
      *>   whole 12-position record and 7 positions left over.
      *> §1985:VII.1.3.5 status 04 / §2023:14.9.30.4 GR14: a READ whose record length does not conform to the
      *>   file's fixed attributes is successful with status 04; the record read is the 7 positions that remain,
      *>   and INTO moves those 7 positions (the receiving item pads with spaces).
      *> EXPECTED OUTPUT, derived: file ABCDEFGHIJKLMNOPQRS.
      *>   1 FS=00 W=[ABCDEFGHIJKL]   a whole record
      *>   2 FS=04 W=[MNOPQRS     ]   the 7 positions left, moved and padded
      *>   3 FS=10                    end of file
      *> Each leg can fail: the area image or a clause-sized window of the connector record gives another W.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5785D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FW ASSIGN TO "PB5785D.DAT".
           SELECT FR ASSIGN TO "PB5785D.DAT" FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD FW.
       01 RW PIC X(19).
       FD FR RECORD CONTAINS 12 CHARACTERS.
       01 RR PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 W PIC X(12).
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT FW.
           MOVE "ABCDEFGHIJKLMNOPQRS" TO RW.
           WRITE RW.
           CLOSE FW.
           OPEN INPUT FR.
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "1 FS=" FS " W=[" W "]".
           MOVE ALL "*" TO W.
           READ FR INTO W AT END DISPLAY "EOF".
           DISPLAY "2 FS=" FS " W=[" W "]".
           READ FR INTO W AT END DISPLAY "3 FS=" FS.
           CLOSE FR.
           STOP RUN.
