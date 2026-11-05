*>--- conformance-case
*> id: message-tag-relation
*> rules: [GR-8.8.4.2.1-9, GR-8.4.3.10.4-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - the message-tag relation condition (ISO 8.8.4.2.2 Format 3 over operands of class
      *> message-tag). 8.8.4.2.1 item 9 lists the comparison "Two operands of class message-tag" (cite.py --check
      *> 8.8.4.2.1 OK, item 9); 8.8.4.2.14: "The operands are equal if they reference the same message requestor
      *> or message server of the same name or one is NULL and the other is NULL." (cite.py --check 8.8.4.2.14 OK.)
      *> 8.4.3.10.4 GR4: "When associated with a message-tag, the predefined content NULL references a data item
      *> of category message-tag that contains the null content" (cite.py --check 8.4.3.10.4 OK, 4)), and
      *> 8.4.3.10.3 SR1 b) admits NULL "in a message-tag relation condition".
      *> Only NULL contents can be built by a program, so: a tag is equal to NULL after INITIALIZE (the null
      *> content, 13.18.63.4 GR4 / 14.9.20.4 GR4), two NULL tags are equal, NOT EQUAL and <> are their negations,
      *> and two strongly-typed groups of one type whose tags are both NULL compare equal (8.8.4.2.12 compares the
      *> elementary items in turn; 8.8.4.2.3 SR4 allows only equality for a group that contains a message-tag).
      *> The unequal case needs a live party and is part of the exchange tests (DESIGN-message-control.md 7.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGREL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(3).
          05 TGT USAGE MESSAGE-TAG.
       01 G1 TYPE TG.
       01 G2 TYPE TG.
       01 T1 USAGE MESSAGE-TAG.
       01 T2 USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN.
           INITIALIZE T1
           IF T1 = NULL DISPLAY "1 T1 = NULL" END-IF
           IF NULL = T1 DISPLAY "2 NULL = T1" END-IF
           IF T1 = T2 DISPLAY "3 T1 = T2" END-IF
           IF T1 NOT = NULL DISPLAY "4 WRONG" ELSE DISPLAY "4 T1 NOT = NULL IS FALSE" END-IF
           IF T1 <> T2 DISPLAY "5 WRONG" ELSE DISPLAY "5 T1 <> T2 IS FALSE" END-IF
           IF T1 IS NOT EQUAL TO T2 DISPLAY "6 WRONG" ELSE DISPLAY "6 NOT EQUAL IS FALSE" END-IF
           INITIALIZE G1 G2
           IF G1 = G2 DISPLAY "7 G1 = G2" END-IF
           IF G1 NOT = G2 DISPLAY "8 WRONG" ELSE DISPLAY "8 G1 NOT = G2 IS FALSE" END-IF
           STOP RUN.
       END PROGRAM MSGTAGREL.
