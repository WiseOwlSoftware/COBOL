*>--- conformance-case
*> id: pb3691_relation_national_function_controls
*> rules: [GR-8.8.4.2.1-6]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3691 - THE NATIONAL-FORM CONTROL for pb3691-relation-integer-function-national.
      *> ISO 8.8.4.2.5: "The numeric integer operand shall be an integer literal or an integer numeric data item of
      *> usage display or national. The other operand may be a literal or data item of class alphanumeric or
      *> national." A NATIONAL function (15.2 3): "an implicit usage national") is a data item of class national, the
      *> "other operand", and an integer item of usage national is the numeric integer operand.
      *>
      *> 01 FUNCTION NATIONAL-OF(S) = U    items 3/5: both class national, "abcd" = "abcd"          EQ
      *> 02 NU = FUNCTION NATIONAL-OF(X)   item 6: the integer national item NU (PIC 9(4), value 7) is moved to a
      *>                                   national item of 4 positions, "0007" = "0007"             EQ
      *> 03 NU = U                         item 6: "0007" against "abcd"                              NE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3691-NATIONAL-CONTROLS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(4) VALUE "abcd".
       01 X PIC X(4) VALUE "0007".
       01 U PIC N(4) VALUE N"abcd".
       01 NU PIC 9(4) USAGE NATIONAL VALUE 7.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF FUNCTION NATIONAL-OF(S) = U DISPLAY "01 EQ" ELSE DISPLAY "01 NE" END-IF
           IF NU = FUNCTION NATIONAL-OF(X) DISPLAY "02 EQ" ELSE DISPLAY "02 NE" END-IF
           IF NU = U DISPLAY "03 EQ" ELSE DISPLAY "03 NE" END-IF
           STOP RUN.
