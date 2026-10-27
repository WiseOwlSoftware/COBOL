*>--- conformance-case
*> id: pb3514_function_result_digits_form
*> rules: [GR-14.9.11.4-1, DOC-A.1-92, DOC-A.1-56]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.11.4 GR1 (Annex A.1 items 56 and 92, docs/CONFORMANCE.md DOC-A.1-92; kb/Work PB3514) under the
      *> NON-DEFAULT `--numeric-display digits`, set for this test in wiseowl/options.json. The option selects DISPLAY's
      *> DIGIT IMAGE for a numeric ITEM (pb3505_display_numeric_digits_form); a numeric FUNCTION's returned value then keeps
      *> DOC-A.1-92's LITERAL form - the significant digits with no padding, a leading `-` when negative and the scale's
      *> fraction digits - which is the form MOVE, STRING and the comparisons give it in every mode. The default form
      *> (pb3514_function_result_device_form*) shows the same calls by the description of their temporaries. Expected values
      *> are the literal form of each value: ABS(S1) is 12.50, MAX(U1 S1) 12.50, INTEGER(S1) -13, ORD("A") 66,
      *> NUMVAL("12.50") 12.50, SQRT(4) 2, LENGTH of a 3-character run-time item 3; the item lines are the digit image
      *> (0001250, and the signed -12.5 over-punched, `000125}`).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3514G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 U1 PIC 9(5)V99 VALUE 12.5.
       01 S1 PIC S9(5)V99 VALUE -12.5.
       01 CHR PIC X VALUE "A".
       01 N PIC 9 VALUE 3.
       01 VG.
          05 VE PIC X OCCURS 1 TO 9 DEPENDING ON N.
       PROCEDURE DIVISION.
           DISPLAY "ITEM-U=" U1 "|"
           DISPLAY "ITEM-S=" S1 "|"
           DISPLAY "ABS=" FUNCTION ABS(S1) "|"
           DISPLAY "ABS-U=" FUNCTION ABS(U1) "|"
           DISPLAY "MAX=" FUNCTION MAX(U1 S1) "|"
           DISPLAY "MIN=" FUNCTION MIN(U1 S1) "|"
           DISPLAY "INTEGER=" FUNCTION INTEGER(S1) "|"
           DISPLAY "SUM=" FUNCTION SUM(U1 S1) "|"
           DISPLAY "ORD=" FUNCTION ORD(CHR) "|"
           DISPLAY "NUMVAL=" FUNCTION NUMVAL("12.50") "|"
           DISPLAY "DATE=" FUNCTION DATE-OF-INTEGER(1) "|"
           DISPLAY "SQRT=" FUNCTION SQRT(4) "|"
           DISPLAY "LENGTH-FIXED=" FUNCTION LENGTH(U1) "|"
           DISPLAY "LENGTH-VAR=" FUNCTION LENGTH(VG) "|"
           DISPLAY "HIGH=" FUNCTION HIGHEST-ALGEBRAIC(U1) "|"
           STOP RUN.
