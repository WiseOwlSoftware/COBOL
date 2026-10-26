*>--- conformance-case
*> id: pb3605-content-space-into-numeric-edited
*> rules: [GR-14.8.2.3.3-L2.2, SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3605 - a figurative constant BY CONTENT takes 14.8.2.3.3 rule 2 d)'s MOVE, the WHOLE
      *> 14.9.25.3 question, and SR5 keeps SPACE out of a numeric-edited item at every edition. So SPACE into the
      *> PIC ZZ9 formal of a contained program activated with the NESTED phrase does not conform -> COBOL1688
      *> naming SR5. (A numeric formal is rule 2 a)'s COMPUTE, which takes no SPACE either.)
      *>   cite.py --check 14.9.25.3 "In all other cases, the move of an alphanumeric figurative constant (SPACE,
      *>     QUOTE, HIGH-VALUE, LOW-VALUE, ALL "literal", or ALL symbolic-character) to either a numeric item or a
      *>     numeric-edited item is prohibited" -> OK §14.9.25.3 5)
      *>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a MOVE statement" -> OK
      *>     §14.8.2.3.3 2) d)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3605C.
       PROCEDURE DIVISION.
           CALL "N3605CA" AS NESTED USING BY CONTENT SPACE
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3605CA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING E.
           DISPLAY E
           GOBACK.
       END PROGRAM N3605CA.
       END PROGRAM N3605C.
