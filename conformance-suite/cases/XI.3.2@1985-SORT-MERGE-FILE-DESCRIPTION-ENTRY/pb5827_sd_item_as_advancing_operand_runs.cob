      *>--- conformance-case
      *> id: pb5827_sd_item_as_advancing_operand_runs
      *> rules: [1985-SR-XI.3.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5827G1.
      *> kb/Work PB5827.  §1985:XI.3.2.3 SR3 forbids only that "no
      *> input-output statements may be executed for this sort or merge
      *> file" (cite.py OK).  WRITE OF-REC is a statement for the file
      *> OF1, not for the sort file SW, and its ADVANCING operand needs
      *> only to be an integer item (§1985:VII.4.7.3 SR4: "Identifier-2
      *> must reference an integer data item"), so SW-N, an item of the
      *> record of SW, is a legal operand at 85.  The rule that an SD
      *> record's items appear in an input-output statement only after
      *> FROM or INTO is §2002:13.3.5.2 SR4 and later, so this program is
      *> accepted at 85 alone (the program refused from 2002 is
      *> pb1291-sd-record-advancing).  The program prints WROTE; the
      *> test fails if the compiler refuses the WRITE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb5827sw.tmp".
           SELECT OF1 ASSIGN TO "pb5827of.txt".
       DATA DIVISION.
       FILE SECTION.
       SD  SW.
       01  SW-REC.
           05  SW-N PIC 9.
           05  SW-K PIC X(3).
       FD  OF1.
       01  OF-REC PIC X(4).
       PROCEDURE DIVISION.
       P0.
           MOVE 2 TO SW-N.
           OPEN OUTPUT OF1.
           MOVE "AAAA" TO OF-REC.
           WRITE OF-REC AFTER ADVANCING SW-N LINES.
           CLOSE OF1.
           DISPLAY "WROTE".
           STOP RUN.
