*>--- conformance-case
*> id: message-tag-refused-call-prototype-argument
*> rules: [GR-14.8.2.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-5 - D-MCS-ARG (docs/CONFORMANCE.md): a message-tag formal accepts a message-tag argument and
      *> no other class. Where a program prototype makes the pairing checkable at compile time, 14.8.2.3.2 rule 4 (cite.py
      *> --check 14.8.2.3.2 OK, rule 4) leaves a message tag the rules of no other class, so the implementation applies the
      *> pointer rule and the CALL of the prototype with a numeric argument is COBOL1688 (the call-argument-conformance
      *> diagnostic its pointer twin pb1063-restricted-pointer-call-by-content draws). The run-time twin is
      *> message-tag-call-argument-mismatch.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTPROTO IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-T USAGE MESSAGE-TAG.
       PROCEDURE DIVISION USING L-T.
       END PROGRAM MTPROTO.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTPMAIN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM MTPROTO.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL MTPROTO USING W-N
           STOP RUN.
       END PROGRAM MTPMAIN.
