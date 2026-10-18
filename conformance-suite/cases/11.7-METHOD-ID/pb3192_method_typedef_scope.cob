*>--- conformance-case
*> id: pb3192_method_typedef_scope
*> rules: [GR-11.7.4-5]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3192 - a method's type declarations and data
      *> description entries are the method's own: a TYPEDEF, a TYPE
      *> copy's members and a SAME AS operand are answered from the
      *> method first, and never from (or for) a sibling method.
      *> ISO 8.4.6.2.2: "A constant-name, data-name, record-name,
      *>   file-name, or type-name declared in a source element for an
      *>   object definition, whether factory or instance, is a global
      *>   name" and "When a condition-name, constant-name, data-name,
      *>   file-name, record-name, report-name, screen-name, or
      *>   type-name is not a global name, it is a local name"
      *>   -> OK 8.4.6.2.2; 13.18.27.3 SR4 bars GLOBAL in a method, so
      *>   a method's type-name is local; 8.4.6.2.1: "A local name may
      *>   be referenced only in the source element in which it is
      *>   declared" -> OK 8.4.6.2.1; 11.7.4 GR5: "If a given
      *>   user-defined word is defined in the data division of this
      *>   method definition and in the data division of the containing
      *>   object definition, the use of that word in this method refers
      *>   to the declaration in this method" -> OK 11.7.4 5).
      *> 13.18.58.4 GR1: the data-names of a type declaration's
      *>   subordinate entries "may be referenced only as subordinate
      *>   items of groups defined using the type-name" -> OK
      *>   13.18.58.4 1); 13.18.49.4 GR1: SAME AS is "as though the data
      *>   description identified by data-name-1 had been coded in
      *>   place" -> OK 13.18.49.4 1).
      *> Expected values (each leg fails if the class-wide tables
      *> answer instead of the method's):
      *>   A1 - SHOWA's TT is X(4), not the object's X(2): ABCD (the
      *>        object's TT made X two positions and the VALUE too
      *>        long, and the method's TT drew "duplicate type-name");
      *>   A2 - SHOWA's TU is referenced by no TYPE clause, so its
      *>        member M names nothing and M is the object's: OBJ;
      *>   A3 - E, F, F-ON and IX of V (a copy of SHOWA's TV) are
      *>        SHOWA's: QQ;
      *>   A4 - B SAME AS A copies SHOWA's A, X(5): ABCDE (it copied
      *>        the object's A, X(2), and showed AB);
      *>   B1 - SHOWB's TT is the object's X(2): AB;
      *>   B2 - SHOWB sees the object's M, IX and A, never SHOWA's
      *>        (V's members were registered class-wide): OBJ 7 OB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3192A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3192C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB3192C.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE PB3192C "NEW" RETURNING OBJ
           INVOKE OBJ "SHOWA"
           INVOKE OBJ "SHOWB"
           STOP RUN.
       END PROGRAM PB3192A.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3192C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT TYPEDEF PIC X(2).
       01 M PIC X(3) VALUE "OBJ".
       01 IX PIC 9 VALUE 7.
       01 A PIC X(2) VALUE "OB".
       PROCEDURE DIVISION.
       METHOD-ID. SHOWA.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 TT TYPEDEF PIC X(4).
       01 X TYPE TT VALUE "ABCD".
       01 TU TYPEDEF.
          05 M PIC X(5).
       01 TV TYPEDEF.
          05 E PIC X(2) OCCURS 3 INDEXED BY IX.
          05 F PIC 9.
             88 F-ON VALUE 1.
       01 V TYPE TV.
       01 A PIC X(5).
       01 B SAME AS A.
       PROCEDURE DIVISION.
           DISPLAY "A1 " X
           DISPLAY "A2 " M
           SET IX TO 2
           MOVE "QQ" TO E (IX)
           MOVE 1 TO F
           IF F-ON
               DISPLAY "A3 " E (2)
           END-IF
           MOVE "ABCDE" TO B
           DISPLAY "A4 " B.
       END METHOD SHOWA.
       METHOD-ID. SHOWB.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 Y TYPE TT VALUE "AB".
       PROCEDURE DIVISION.
           DISPLAY "B1 " Y
           DISPLAY "B2 " M " " IX " " A.
       END METHOD SHOWB.
       END OBJECT.
       END CLASS PB3192C.
