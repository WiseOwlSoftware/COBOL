*>--- conformance-case
*> id: message-tag-external
*> rules: [SR-13.18.22.3-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-2 - 13.18.22.3 SR4: "The EXTERNAL clause shall not be specified for a data item of
      *> class object or pointer." (cite.py --check 13.18.22.3 OK, SR 4.) A message tag is of class message-tag, which
      *> the rule does not list, so USAGE MESSAGE-TAG EXTERNAL is legal source; the screen once refused it with
      *> COBOL1796 because the tag rides the same managed slot as a pointer. Both programs name the same external item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTTAG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG EXTERNAL.
       PROCEDURE DIVISION.
       MAIN.
           SET T TO NULL
           IF T = NULL
              DISPLAY "EXT NULL"
           END-IF
           CALL "EXTTAG2"
           STOP RUN.
       END PROGRAM EXTTAG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXTTAG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG EXTERNAL.
       PROCEDURE DIVISION.
       MAIN.
           IF T = NULL
              DISPLAY "SHARED NULL"
           END-IF
           GOBACK.
       END PROGRAM EXTTAG2.
