*>--- conformance-case
*> id: pb3192-method-type-copy-member-in-sibling
*> rules: [GR-11.7.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3192 - the members a TYPE clause copies under a
      *> method's item are that method's local names (8.4.6.2.2 -> OK
      *> 8.4.6.2.2: a name not global "is a local name"), and "A local
      *> name may be referenced only in the source element in which it
      *> is declared" (8.4.6.2.1 -> OK 8.4.6.2.1). SHOWB declares no M
      *> and its object has none (the object's TT declares M only as a
      *> member of a type, 13.18.58.4 GR1 -> OK 13.18.58.4 1)), so
      *> SHOWB's DISPLAY M references nothing: refused COBOLNET1639. The
      *> copy's names were registered class-wide, so SHOWB resolved
      *> SHOWA's local X's M silently and the C# backend failed CS0103.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3192E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3192F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3192F.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3192F "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3192E.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3192F INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF.
          05 M PIC X(3).
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 X TYPE TT.
       PROCEDURE DIVISION.
           MOVE "ABC" TO M
           DISPLAY "A " M.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       PROCEDURE DIVISION.
           DISPLAY "B " M.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3192F.
