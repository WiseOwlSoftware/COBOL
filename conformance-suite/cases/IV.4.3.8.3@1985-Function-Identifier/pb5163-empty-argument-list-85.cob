      *>--- conformance-case
      *> id: pb5163-empty-argument-list-85
      *> rules: [1985-FMT-IV.4.3.8.3.2, 1985-SR-IV.4.2.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5163: §1985:IV.4.3.8.3.2 writes the parentheses of a
      *> function-identifier only around a braced { argument-1 } ...,
      *> and "one of the options contained within the braces must be
      *> either explicitly specified or implicitly selected"
      *> (§1985:IV.2.1.4); "parentheses may appear only in balanced
      *> pairs of left and right parentheses delimiting subscripts, a
      *> list of function arguments" (§1985:IV.4.2.1 SR4). An empty
      *> list is refused at 85 with COBOL0900 and accepted from 2002
      *> (cases/8.4.3.2-Function-identifier/pb5163_empty_argument_list_
      *> 2002). The argument-less form is FUNCTION CURRENT-DATE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5163R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  D PIC X(21).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FUNCTION CURRENT-DATE () TO D.
           DISPLAY D (1:2).
           STOP RUN.
