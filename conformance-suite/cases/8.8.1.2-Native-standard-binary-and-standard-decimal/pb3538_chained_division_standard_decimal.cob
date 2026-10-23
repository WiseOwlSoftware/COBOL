      *>--- conformance-case
      *> id: pb3538_chained_division_standard_decimal
      *> rules: [GR-8.8.1.2-3, GR-14.7.4.3-2, GR-8.8.1.5.2-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3538's chains under ARITHMETIC IS STANDARD-DECIMAL,
      *> where 8.8.1.5.2 fixes the intermediate: every operation is an
      *> SDIDI value of 34 significant digits, whatever the expression's
      *> depth. 8.8.1.2 3) evaluates left to right, so 8 / 2 / 2 / 2 / 2
      *> is 0.5, three quotients 4 * 4 * 4 = 64, and 8 halved ten times
      *> is 0.0078125; 14.7.4.3 2) truncates each into its receiver.
      *> 1 / 3 ten times is 1 / 59049 rounded at each step to 34 digits
      *> (11.9.11.2 3) a) and b), NEAREST-AWAY-FROM-ZERO): its first 12
      *> places, 0.000016935087, are those of the exact value.
      *> Why each leg can fail: the native lane's compounding scale
      *> (PB3538) must not exist here either - a size error, an abort or
      *> a lost digit in any leg is the defect.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3538SDD.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
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
           COMPUTE R = A / B / B / B / B
               ON SIZE ERROR DISPLAY "FOUR-CHECKED SIZE ERROR"
               NOT ON SIZE ERROR DISPLAY "FOUR-CHECKED NO SIZE ERROR"
           END-COMPUTE.
           MOVE R TO E4.
           DISPLAY "FOUR-CHECKED " E4.
           COMPUTE R = (A / B) * (A / B) * (A / B).
           MOVE R TO E4.
           DISPLAY "P3 " E4.
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
