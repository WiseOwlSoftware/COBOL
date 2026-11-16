      *>--- conformance-case
      *> id: g85_f18_03
      *> rules: [1985-GR-VII.3.8.4-8, 1985-GR-VII.3.8.4-9,
      *>    1985-GR-VII.3.8.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.4 GR8 - with DEPENDING ON data-name-1, the
      *> record's number of character positions is placed in it
      *> before each RELEASE, REWRITE and WRITE; GR10 a makes that
      *> content the length released, rewritten or written.
      *> §1985:VII.3.8.4 GR9 - RELEASE, REWRITE and WRITE do not
      *> alter data-name-1, nor does a failed REWRITE (RX 06).
      *> §1985:VII.3.8.4 GR11 - after a successful READ or RETURN,
      *> data-name-1 is the length of the record just read (it is
      *> zeroed before each READ and RETURN, so a stale value
      *> cannot pass).
      *> DERIVATION. RC = "ABCDEFGHIJ". LC=4: record 1 is ABCD;
      *> LC=7: record 2 is ABCDEFG. OPEN I-O, READ: R1 00 04 ABCD.
      *> "WXYZ" over RC(1:4), LC=4 (the same size as the record
      *> replaced), REWRITE: RW 00 04. READ: R2 00 07 ABCDEFG.
      *> LC=6, REWRITE: the size comes from LC (GR10 a) and differs
      *> from the 7 of the record replaced, a boundary violation on
      *> a sequential file (§1985:VII.1.3.5, status 44): RX 44 06,
      *> and record 2 is not replaced. A REWRITE that ignored LC
      *> and used the replaced record's length would print RX 00.
      *> Reopened INPUT: R3 00 04 WXYZ, R4 00 07 ABCDEFG, then the
      *> AT END condition, status 10. A WRITE that ignored LC
      *> (writing all 10 positions) reads back 10.
      *> RELEASE leg (§1985:XI.3.2.4 GR2: the Sort-Merge RECORD
      *> clause is the Sequential I-O one). SD SF varies from 1 to
      *> 10 DEPENDING ON LS; key SK is its first position. Released
      *> "XYZDEFGHIJ" with LS=6, then "ABCDEFGHIJ" with LS=3; in
      *> ascending key order RETURN gives S1 03 ABC, S2 06 XYZDEF,
      *> then AT END. A RELEASE that ignored LS returns 10 for both.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1803.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FC ASSIGN TO "g85f1803.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT SF ASSIGN TO "g85f1803s.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  FC
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON LC.
       01  RC PIC X(10).
       SD  SF
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON LS.
       01  SR.
           02  SK PIC X.
           02  SREST PIC X(9).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  LC PIC 99.
       01  LS PIC 99.
       PROCEDURE DIVISION.
       P0 SECTION.
       P0-A.
           OPEN OUTPUT FC.
           MOVE "ABCDEFGHIJ" TO RC.
           MOVE 4 TO LC.
           WRITE RC.
           DISPLAY "W1 " FS " " LC.
           MOVE 7 TO LC.
           WRITE RC.
           DISPLAY "W2 " FS " " LC.
           CLOSE FC.
           OPEN I-O FC.
           MOVE 0 TO LC.
           READ FC AT END DISPLAY "R1 END".
           DISPLAY "R1 " FS " " LC " " RC (1:LC).
           MOVE "WXYZ" TO RC (1:4).
           MOVE 4 TO LC.
           REWRITE RC.
           DISPLAY "RW " FS " " LC.
           MOVE 0 TO LC.
           READ FC AT END DISPLAY "R2 END".
           DISPLAY "R2 " FS " " LC " " RC (1:LC).
           MOVE 6 TO LC.
           REWRITE RC.
           DISPLAY "RX " FS " " LC.
           CLOSE FC.
           OPEN INPUT FC.
           MOVE 0 TO LC.
           READ FC AT END DISPLAY "R3 END".
           DISPLAY "R3 " FS " " LC " " RC (1:LC).
           MOVE 0 TO LC.
           READ FC AT END DISPLAY "R4 END".
           DISPLAY "R4 " FS " " LC " " RC (1:LC).
           READ FC AT END DISPLAY "END " FS.
           CLOSE FC.
           SORT SF ON ASCENDING KEY SK
               INPUT PROCEDURE IS S-IN
               OUTPUT PROCEDURE IS S-OUT.
           STOP RUN.
       S-IN SECTION.
       S-IN-A.
           MOVE "XYZDEFGHIJ" TO SR.
           MOVE 6 TO LS.
           RELEASE SR.
           MOVE "ABCDEFGHIJ" TO SR.
           MOVE 3 TO LS.
           RELEASE SR.
       S-OUT SECTION.
       S-OUT-A.
           MOVE 0 TO LS.
           RETURN SF AT END DISPLAY "S1 END"
               NOT AT END DISPLAY "S1 " LS " " SR (1:LS)
           END-RETURN.
           MOVE 0 TO LS.
           RETURN SF AT END DISPLAY "S2 END"
               NOT AT END DISPLAY "S2 " LS " " SR (1:LS)
           END-RETURN.
           RETURN SF AT END DISPLAY "SORT END".
