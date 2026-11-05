*>--- conformance-case
*> id: message-tag-refused-call-by-value
*> rules: [SR-14.9.4.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - ISO 14.9.4.3 SR22: "If identifier-4 or its corresponding formal parameter is specified with a BY
      *> VALUE phrase, identifier-4 shall be of class numeric, object, or pointer." (cite.py --check 14.9.4.3 OK, SR 22.)
      *> The rule does not name class message-tag (14.2.2 SR2 and 14.9.23.3 SR15 do, for the formal and for INVOKE), so a
      *> CALL cannot pass a message tag by value; the argument is classified by 8.5.2.1 Table 2 as class message-tag.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMTCALLBYVALUE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY VALUE T
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION USING BY VALUE L-T.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM NEGMTCALLBYVALUE.
