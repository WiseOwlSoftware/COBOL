*>--- conformance-case
*> id: pb6228_bare_condition_object_subscript_refused_85
*> rules: [GR-14.9.13.4-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6228 - the program of pb6228_bare_condition_object_subscript_not_hoisted below COBOL 2002: its
      *> arithmetic-expression subscript is not a COBOL-85 subscript (ISO 1989:2002 Annex F.2 item 133), so it is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6228B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 9 VALUE 1.
       01 T-TAB.
          05 T-FLAG PIC X OCCURS 3 VALUE "Y".
             88 T-OK VALUE "Y".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE TRUE
              WHEN X = 1
                 DISPLAY "ONE"
              WHEN T-OK (FUNCTION INTEGER (FUNCTION RANDOM * 3) + 1)
                 DISPLAY "OK"
           END-EVALUATE.
           STOP RUN.
