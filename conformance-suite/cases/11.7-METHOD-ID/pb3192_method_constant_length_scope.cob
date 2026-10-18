*>--- conformance-case
*> id: pb3192_method_constant_length_scope
*> rules: [GR-11.7.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3192 - a method constant's LENGTH OF operand names
      *> the method's own data item when the method declares one, before
      *> or after the constant entry, and its object's otherwise.
      *> ISO 11.7.4 GR5: "If a given user-defined word is defined in the
      *>   data division of this method definition and in the data
      *>   division of the containing object definition, the use of that
      *>   word in this method refers to the declaration in this method"
      *>   -> OK 11.7.4 5); 13.10.4 GR6: the value "is determined as
      *>   specified in the LENGTH intrinsic function" -> OK 13.10.4 6).
      *>   No rule of 13.10 orders a constant entry after the item it
      *>   measures (13.10.3 SR4 forbids only a circular length).
      *> Expected values:
      *>   A 5 - SHOWA's W (X(5)) follows the constant; the constant
      *>         measured the object's W (X(3)) and showed 3;
      *>   B 6 - SHOWB's W (X(6)) precedes it; the constant drew "the
      *>         data-name is not defined" (two W's in one table);
      *>   C 3 - SHOWC declares no W: the object's W, X(3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3192B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3192D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3192D.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3192D "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           INVOKE OBJ "SHOWC"
           STOP RUN.
       END PROGRAM PB3192B.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3192D INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 C CONSTANT AS LENGTH OF W.
       01 W PIC X(5).
       PROCEDURE DIVISION.
           DISPLAY "A " C.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC X(6).
       01 C CONSTANT AS LENGTH OF W.
       PROCEDURE DIVISION.
           DISPLAY "B " C.
       END METHOD SHOWB.
       METHOD-ID. SHOWC.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 C CONSTANT AS LENGTH OF W.
       PROCEDURE DIVISION.
           DISPLAY "C " C.
       END METHOD SHOWC.
       END OBJECT.
       END CLASS PB3192D.
