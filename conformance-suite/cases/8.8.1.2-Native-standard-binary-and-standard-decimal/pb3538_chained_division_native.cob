      *>--- conformance-case
      *> id: pb3538_chained_division_native
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2, GR-14.7.5-5]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538 - the DEPTH of an arithmetic expression never
      *> changes its value, and a size error is raised only for a VALUE
      *> outside the intermediate's range, under native arithmetic.
      *> 8.8.1.2 3): "When the sequence of execution is not specified by
      *> parentheses, the order of execution of consecutive operations of
      *> the same hierarchical level is from left to right", so
      *> 8 / 2 / 2 / 2 = 1 and 8 / 2 / 2 / 2 / 2 = 0.5.
      *> 14.7.4.3 2): "If the ROUNDED phrase is not specified, execution
      *> is as if ROUNDED MODE IS TRUNCATION had been specified", so 0.5
      *> stores 0 in PIC 9(4), and 8 halved ten times (0.0078125) 0.0078.
      *> 14.7.5 5): a size error arises "when an arithmetic operation on
      *> the intermediate data item would cause the new value to be
      *> outside of the allowed range" - and no value here is near the
      *> documented range (CONFORMANCE.md DOC-A.1-123 / DOC-A.1-179).
      *> Why each leg can fail: the nested quotient's working scale used
      *> to grow by 14 guard digits per division (14, 28, 42), so the
      *> fourth division left the 128-bit carrier: FOUR-CHECKED took ON
      *> SIZE ERROR with R unchanged, FOUR ended the run unit with
      *> EC-SIZE-OVERFLOW, P3 (three quotients multiplied, scale 42) did
      *> the same, and the relation and the depth-10 chains could not be
      *> evaluated at all. T10 (1 / 3 ten times, 1 / 59049 =
      *> 0.0000169350878...) checks that the bounded intermediate still
      *> carries the digits a 12-place receiver keeps, truncated.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538NAT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9 VALUE 8.
       01 B    PIC 9 VALUE 2.
       01 T    PIC 9 VALUE 3.
       01 ONE  PIC 9 VALUE 1.
       01 R    PIC 9(4) VALUE 1234.
       01 R4   PIC 9V9(4) VALUE 0.
       01 R12  PIC 9V9(12) VALUE 0.
       01 E4   PIC Z(3)9.
       01 F4   PIC 9.9(4).
       01 F12  PIC 9.9(12).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = A / B / B / B.
           MOVE R TO E4.
           DISPLAY "THREE " E4.
           MOVE 1234 TO R.
           COMPUTE R = A / B / B / B / B
               ON SIZE ERROR DISPLAY "FOUR-CHECKED SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "FOUR-CHECKED NO SIZE ERROR"
           END-COMPUTE.
           MOVE R TO E4.
           DISPLAY "FOUR-CHECKED " E4.
           MOVE 1234 TO R.
           COMPUTE R = A / B / B / B / B.
           MOVE R TO E4.
           DISPLAY "FOUR " E4.
           COMPUTE R = (A / B) * (A / B) * (A / B).
           MOVE R TO E4.
           DISPLAY "P3 " E4.
           IF A / B / B / B / B > 0
               DISPLAY "REL GREATER"
           ELSE
               DISPLAY "REL NOT GREATER"
           END-IF.
           COMPUTE R4 = A / B / B / B / B / B / B / B / B / B / B.
           MOVE R4 TO F4.
           DISPLAY "D10 " F4.
           COMPUTE R4 = ONE / T / T / T / T / T / T / T / T / T / T.
           MOVE R4 TO F4.
           DISPLAY "T10-4 " F4.
           COMPUTE R12 = ONE / T / T / T / T / T / T / T / T / T / T.
           MOVE R12 TO F12.
           DISPLAY "T10-12 " F12.
           STOP RUN.
