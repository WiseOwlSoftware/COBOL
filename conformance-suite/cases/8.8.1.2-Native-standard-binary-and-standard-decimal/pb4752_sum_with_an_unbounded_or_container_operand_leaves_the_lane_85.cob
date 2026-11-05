      *>--- conformance-case
      *> id: pb4752_sum_with_an_unbounded_or_container_operand_leaves_the_lane_85
      *> rules: [GR-8.8.1.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4752: the refused twin of pb4752_sum_with_an_unbounded_or_container_operand_leaves_the_lane. The program is
      *> witnessed against items of 19 to 31 digits, which COBOL-2002 introduced (ISO 8.3.3.3.2), so under --std 85 it is refused at the
      *> item and the lane is never reached.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4752AD85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D  PIC 9(19) VALUE 1000000000000000001.
       01 F  PIC 9V9(30) VALUE 1.010000000000000000000000000000.
       01 R1 PIC 9(19)V99.
       PROCEDURE DIVISION.
           COMPUTE R1 = FUNCTION INTEGER (D) + F
           DISPLAY "S1 " R1
           STOP RUN.
       END PROGRAM PB4752AD85.
