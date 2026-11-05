*>--- conformance-case
*> id: message-tag-refused-strong-group-ordering
*> rules: [SR-8.8.4.2.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - a message tag (USAGE MESSAGE-TAG, ISO 13.18.60.4 GR9: class and category message-tag) in a position its rules refuse.
      *> ISO 8.8.4.2.3 SR4: "Strongly-typed group items that contain elementary items of class boolean, message-
      *> tag, object, or pointer, may be compared only for equality or inequality." (cite.py --check 8.8.4.2.3 OK,
      *> SR 4.)
      *> G1 > G2 over two strongly-typed groups of one type that contain a message tag is refused.
      *> Refused at 2023, where USAGE MESSAGE-TAG exists (8.9; Annex E.2 item 25); below it the entry draws the COBOL0900 introduction gate (pb916-message-tag-introduction).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGMESSAGETAGREFUSEDSTRONGGROUPOR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(3).
          05 TGT USAGE MESSAGE-TAG.
       01 G1 TYPE TG.
       01 G2 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF G1 > G2 DISPLAY "X" END-IF
           STOP RUN.
       END PROGRAM NEGMESSAGETAGREFUSEDSTRONGGROUPOR.
