*>--- conformance-case
*> id: message-tag-call-by-reference
*> rules: [GR-14.8.2.3.2-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag as an argument (ISO 14.8.2.3.2, D-MCS-ARG in docs/CONFORMANCE.md).
      *> 14.8.2.3.2 rule 4: "If neither the formal parameter nor the argument is of class message-tag, object, or
      *> pointer, the conformance rules are the following:" (cite.py --check 14.8.2.3.2 OK, rule 4) - the rules for the
      *> other classes do not apply to a message tag, and the standard states none of its own; the implementation
      *> applies the pointer rule (a message-tag argument for a message-tag formal). BY REFERENCE passes the item and
      *> the activated program sees and changes the same tag (14.9.4.4 GR3); BY CONTENT passes a copy. Here the
      *> activated program sees the null content both ways (L-T = NULL) and SETs its formal to NULL.
      *> 14.2.2 SR2 admits a message tag as a BY VALUE formal: "Each data-name-1 specified in a BY VALUE phrase shall be
      *> defined as a data item of class numeric, message-tag, object, or pointer." (cite.py --check 14.2.2 OK, SR 2.)
      *> BYVAL below is that header; it compiles and is not activated (a CALL BY VALUE message-tag argument is refused
      *> by 14.9.4.3 SR22, which does not name the class).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGCALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 USAGE MESSAGE-TAG.
       01 N PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           CALL "MSGTAGSUB" USING T1 N
           IF T1 = NULL
              DISPLAY "CALLER SEES NULL"
           END-IF
           CALL "MSGTAGSUB" USING BY CONTENT T1 BY CONTENT N
           STOP RUN.
       END PROGRAM MSGTAGCALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       01 L-N PIC 9(4).
       PROCEDURE DIVISION USING L-T L-N.
       MAIN.
           IF L-T = NULL
              DISPLAY "CALLEE SEES NULL " L-N
           END-IF
           SET L-T TO NULL
           GOBACK.
       END PROGRAM MSGTAGSUB.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGBYVAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION USING BY VALUE L-T.
       MAIN.
           GOBACK.
       END PROGRAM MSGTAGBYVAL.
