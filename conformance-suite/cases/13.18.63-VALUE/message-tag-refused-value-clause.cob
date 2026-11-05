*>--- conformance-case
*> id: message-tag-refused-value-clause
*> rules: [SR-13.18.63.3-9, SR-13.16.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 13.16.3 SR10: "The VALUE clause shall not be specified for data items of class index, message-tag,
      *> object, or pointer." (cite.py --check 13.16.3 OK, SR 10.) 13.18.63.3 SR9 restates it for the MESSAGE-TAG
      *> usage.
      *> A message tag takes the null content with no clause (13.18.63.4 GR4), so a VALUE clause on one is
      *> refused.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDVALUECLAUSE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T USAGE MESSAGE-TAG VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CONTINUE
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDVALUECLAUSE.
