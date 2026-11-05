*>--- conformance-case
*> id: message-tag-call-returning
*> rules: [GR-14.8.3.3-1]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-1 - a program whose RETURNING item is a message tag (14.2.2 admits a linkage item
      *> as the returning item; the delivery is a SET of the activated returning item into the activating one,
      *> 14.8.3.3 1): "the conformance rules are the same as if a SET statement were performed in the activated runtime
      *> element with the returning item" (cite.py --check 14.8.3.3 OK, 1). Before the fix the generated delivery
      *> bound no overload and Roslyn refused the program (CS0315). The callee sets its returning item to NULL, so the
      *> caller's tag is NULL after the call.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGRETCALL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1 USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           CALL "MSGTAGRETSUB" RETURNING T1
           IF T1 = NULL
              DISPLAY "RETURNED NULL"
           END-IF
           STOP RUN.
       END PROGRAM MSGTAGRETCALL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGRETSUB.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-R USAGE MESSAGE-TAG.
       PROCEDURE DIVISION RETURNING L-R.
       MAIN.
           SET L-R TO NULL
           GOBACK.
       END PROGRAM MSGTAGRETSUB.
