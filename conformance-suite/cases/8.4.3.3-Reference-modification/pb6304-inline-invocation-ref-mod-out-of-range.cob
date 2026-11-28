*>--- conformance-case
*> id: pb6304-inline-invocation-ref-mod-out-of-range
*> rules: [GR-8.4.3.3.4-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6304. ISO 8.4.3.3.4 GR5b: leftmost-position is a position of the item, and GR5c: "The sum of
      *> leftmost-position and length minus the value one shall be less than or equal to the number of positions in
      *> the data item referenced by identifier-1" (cite.py OK); a violation is the fatal EC-BOUND-REF-MOD, and
      *> with checking not enabled a literal violation is refused at compile time (COBOL2670, kb/Work PB1707 part 1).
      *> The invocation's temporary is PIC X(8), so (20:5) starts past it, as W (20:5) over a PIC X(8) item is refused.
      *> The inline path never asked the literal range screen and compiled this clean.
      *> WHY THE LEG FAILS. A reference modifier read without the screen accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6304RANGE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB6304ACCC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A1 USAGE OBJECT REFERENCE PB6304ACCC.
       01 W3 PIC X(3).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB6304ACCC "NEW" RETURNING A1.
           MOVE A1 :: "GETNAME" (20:5) TO W3.
           STOP RUN.
       END PROGRAM PB6304RANGE.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB6304ACCC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. GETNAME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-NAME PIC X(8).
       PROCEDURE DIVISION RETURNING LK-NAME.
       MAIN.
           MOVE "ACCOUNT" TO LK-NAME.
       END METHOD GETNAME.
       END OBJECT.
       END CLASS PB6304ACCC.
