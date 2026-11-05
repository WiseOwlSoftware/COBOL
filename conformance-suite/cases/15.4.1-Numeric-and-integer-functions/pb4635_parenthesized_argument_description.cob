*>--- conformance-case
*> id: pb4635_parenthesized_argument_description
*> rules: [GR-14.9.11.4-1, RV-15.4.1-L2.2, DOC-A.1-92, DOC-A.1-56]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4635 review R2-2 (DOC-A.1-92). The selecting functions (ABS, MAX, MIN, MEDIAN, MEAN) return a copy of the
      *> argument they select, and the description of that temporary is implementor-defined (§15.4.1: "the characteristics
      *> and representation of the returned value are defined by the implementor"; §14.9.11.4 GR1), which CLAUDE.md rule 1 and
      *> DOC-A.1-92 give to GnuCOBOL 3.2.0. Its parser makes a parenthesized sole item or literal the identifier or literal
      *> it encloses, so the temporary is the item's: ABS((I3)) shows what ABS(I3) shows. This compiler binds (I3) as the
      *> arithmetic expression it is (§8.8.1.1; it is refused where a data item is required, pb4635-findstr-parenthesized-item),
      *> and the image of the returned value must not follow: an expression such as I3 + 0 is a value-described temporary
      *> (000000003), which GnuCOBOL's identifier is not.
      *>
      *> Each parenthesized line is paired with its bare spelling, and the two print the same. The bare images are the ones
      *> pb3514_function_result_device_form_abs records as CHECKED against `cobc -x -std=default` 3.2.0: an unsigned PIC 9
      *> shows its one digit, a signed PIC S99 its sign and two digits, a literal its digits, and MAX / MIN / MEDIAN / MEAN of
      *> one selected argument copy that argument. The last line is the control for the distinction: a genuine expression keeps
      *> the value-described image.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4635DESC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I2 PIC 9 VALUE 2.
       01 I3 PIC S99 VALUE -3.
       01 F2 PIC 9V9 VALUE 2.5.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "ABS-I2=" FUNCTION ABS(I2) "|" FUNCTION ABS((I2)) "|"
           DISPLAY "ABS-I3=" FUNCTION ABS(I3) "|" FUNCTION ABS((I3)) "|"
           DISPLAY "ABS-LIT=" FUNCTION ABS(3) "|" FUNCTION ABS((3)) "|"
           DISPLAY "ABS-F2=" FUNCTION ABS(F2) "|" FUNCTION ABS((F2)) "|"
           DISPLAY "MAX-I2=" FUNCTION MAX(I2 1) "|" FUNCTION MAX((I2) 1) "|"
           DISPLAY "MAX-F2=" FUNCTION MAX(F2 1) "|" FUNCTION MAX((F2) 1) "|"
           DISPLAY "MIN-I2=" FUNCTION MIN(I2 5) "|" FUNCTION MIN((I2) 5) "|"
           DISPLAY "MEDIAN-I2=" FUNCTION MEDIAN(I2) "|" FUNCTION MEDIAN((I2)) "|"
           DISPLAY "MEAN-I2=" FUNCTION MEAN(I2) "|" FUNCTION MEAN((I2)) "|"
           DISPLAY "EXPR-I2=" FUNCTION ABS(I2 + 0) "|"
           STOP RUN.
       END PROGRAM PB4635DESC.
