*>--- conformance-case
*> id: pb3755_content_figurative_numeric_edited_formal
*> rules: [GR-14.8.2.3.3-L2.2, SR-14.9.25.3-5]
*> accept: [2002, 2014]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3755 - a figurative constant BY CONTENT takes 14.8.2.3.3 rule 2 d)'s MOVE, the WHOLE 14.9.25.3
      *> question, so it is under the edition rows the written MOVE is under. HIGH-VALUE, LOW-VALUE and a non-digit
      *> ALL literal into a numeric-edited item were permitted through 2014 (14.9.25.3 SR5 removed them in 2023; Annex
      *> E.2 item 1 bullet 1), so each of the three activations below - CALL with the NESTED phrase, INVOKE of a
      *> factory method and a user-defined function - conforms and runs at 2002 and 2014. (A numeric formal would be rule
      *> 2 a)'s COMPUTE, which takes no figurative but ZERO at any edition.) The program does not print the formal: the
      *> pre-removal fill is the implementor's. The 2023 twins (pb3755-call-..., pb3755-invoke-..., pb3755-function-...)
      *> are refused COBOL0902 per lane.
      *>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a MOVE statement" -> OK
      *>     14.8.2.3.3 2) d)
      *>   cite.py --check 14.9.25.3 "In all other cases, the move of an alphanumeric figurative constant (SPACE, QUOTE,
      *>     HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL symbolic-character) to either a numeric item or a
      *>     numeric-edited item is prohibited" -> OK 14.9.25.3 5)
       IDENTIFICATION DIVISION.
       CLASS-ID. K3755P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3755P.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING BY REFERENCE E.
           DISPLAY "INVOKE LANE ENTERED"
           GOBACK.
       END METHOD MN.
       END FACTORY.
       END CLASS K3755P.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. F3755P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       01 R PIC 9.
       PROCEDURE DIVISION USING E RETURNING R.
           MOVE 1 TO R
           GOBACK.
       END FUNCTION F3755P.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3755P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS K3755P
           FUNCTION F3755P.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R PIC 9.
       PROCEDURE DIVISION.
           CALL "N3755P" AS NESTED USING BY CONTENT HIGH-VALUE
           INVOKE K3755P "MN" USING BY CONTENT LOW-VALUE
           COMPUTE WS-R = FUNCTION F3755P(ALL "X")
           DISPLAY "FUNCTION LANE R=" WS-R
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3755P.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING E.
           DISPLAY "CALL LANE ENTERED"
           GOBACK.
       END PROGRAM N3755P.
       END PROGRAM P3755P.
