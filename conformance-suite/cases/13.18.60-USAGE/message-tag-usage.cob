*>--- conformance-case
*> id: message-tag-usage
*> rules: [GR-13.18.60.4-9, GR-13.18.63.4-4, DOC-A.1-212]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 - USAGE MESSAGE-TAG (ISO 13.18.60.4 GR9; the message-tag class of 8.5.2.1 Table 2).
      *> GR9: "The MESSAGE-TAG clause specifies that a data item is a message-tag data item and contains an
      *> implementor-defined value that identifies a message and a server or requestor. The class and category
      *> of a message-tag data item are message-tag." (cite.py --check 13.18.60.4 OK, General rules 9.)
      *> 13.18.63.4 GR4: "data items of class message-tag, class object, and class pointer are initialized to
      *> null" (cite.py --check 13.18.63.4 OK, General rules 4), so a message tag that no statement has touched
      *> is NULL: the relation condition `T1 = NULL` (8.8.4.2.1 item 9; 8.4.3.10.3 SR1 b) is true.
      *> A message tag is declarable at level 1 and inside a strongly-typed group (13.18.60.3 SR14); both are here.
      *> A strongly-typed group's items are initialized the same way, so the tag inside G1 is NULL too.
      *> DOC-A.1-212 (Annex A.1 item 212, docs/CONFORMANCE.md section 7): the size of a message-tag data item
      *> is implementor-defined and is eight bytes here, the slot every class-pointer and class-object item
      *> takes (DOC-A.1-210), so FUNCTION BYTE-LENGTH of the strongly-typed group X(3) + tag + 9(2) is 3 + 8 + 2 = 13
      *> and of the tag alone is 8.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MSGTAGUSAGE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(3).
          05 TGT USAGE MESSAGE-TAG.
          05 TGB PIC 9(2).
       01 G1 TYPE TG.
       01 T1 USAGE MESSAGE-TAG.
       01 BLEN PIC 9(3).
       PROCEDURE DIVISION.
       MAIN.
           IF T1 = NULL
              DISPLAY "T1 IS NULL"
           ELSE
              DISPLAY "T1 NOT NULL"
           END-IF
           IF TGT OF G1 = NULL
              DISPLAY "TGT IS NULL"
           ELSE
              DISPLAY "TGT NOT NULL"
           END-IF
           MOVE FUNCTION BYTE-LENGTH(T1) TO BLEN
           DISPLAY "TAG BYTES " BLEN
           MOVE FUNCTION BYTE-LENGTH(G1) TO BLEN
           DISPLAY "GROUP BYTES " BLEN
           STOP RUN.
       END PROGRAM MSGTAGUSAGE.
