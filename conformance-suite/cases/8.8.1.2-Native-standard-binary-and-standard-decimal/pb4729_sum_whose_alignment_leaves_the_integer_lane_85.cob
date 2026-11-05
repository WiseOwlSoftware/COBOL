      *>--- conformance-case
      *> id: pb4729_sum_whose_alignment_leaves_the_integer_lane_85
      *> rules: [GR-8.8.1.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4729 (review round 1, R1-7): the refused twin of pb4729_sum_whose_alignment_leaves_the_integer_lane. The program is
      *> witnessed against items of 19 to 31 digits, which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85 it is refused at the
      *> item and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4729AD85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9(19) VALUE 1000000000000000001.
       01 F  PIC 9V9(30) VALUE 1.010000000000000000000000000000.
       01 R2 PIC 9(9).
       PROCEDURE DIVISION.
           COMPUTE R2 = D + F
           DISPLAY "S1 " R2
           STOP RUN.
       END PROGRAM PB4729AD85.
