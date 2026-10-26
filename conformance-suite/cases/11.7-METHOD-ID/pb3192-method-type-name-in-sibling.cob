*>--- conformance-case
*> id: pb3192-method-type-name-in-sibling
*> rules: [GR-11.7.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3192 - a type-name declared in a method definition
      *> is a local name: 8.4.6.2.2 makes global only a type-name
      *> "described with a GLOBAL clause" or "declared in a source
      *> element for an object definition" (-> OK 8.4.6.2.2), and
      *> 13.18.27.3 SR4 bars GLOBAL in a method definition (-> OK
      *> 13.18.27.3 4)). "A local name may be referenced only in the
      *> source element in which it is declared" (8.4.6.2.1 -> OK
      *> 8.4.6.2.1), so SHOWB's TYPE TU names no type declaration it
      *> can see: refused COBOL1530. The type-name sat in one
      *> class-wide table, so SHOWB used SHOWA's TU silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3192G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3192H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3192H.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3192H "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3192G.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3192H INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 TU TYPEDEF PIC X(4).
       01 X TYPE TU VALUE "ABCD".
       PROCEDURE DIVISION.
           DISPLAY "A " X.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Y TYPE TU.
       PROCEDURE DIVISION.
           DISPLAY "B " Y.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3192H.
