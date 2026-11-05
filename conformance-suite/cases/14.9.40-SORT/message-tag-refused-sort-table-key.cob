*>--- conformance-case
*> id: message-tag-refused-sort-table-key
*> rules: [SR-14.9.40.3-14]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-3 - A TABLE SORT KEY OF CLASS MESSAGE-TAG IS REFUSED ON THE ORDERING GROUND.
      *>   cite.py --check 14.9.40.3 "Key data items shall not be of class boolean, object, or pointer." ->
      *>     OK §14.9.40.3 14)
      *>   cite.py --check 8.8.4.2.14 "An operand of class message-tag may be compared with another operand of class
      *>     message-tag." -> OK §8.8.4.2.14
      *> SR14 c) lists only boolean, object and pointer, and a message tag is none of them; but a sort key is ORDERED
      *> and 8.8.4.2.14 defines the comparison of two message-tag operands as the equality of the messages' parties (no
      *> relational order, see message-tag-refused-relation-ordering), so the key has no ordering to sort by. TGT is a
      *> message tag under a strongly-typed table element (the placement 13.18.60.3 SR14 permits). Before the fix the
      *> program passed the binder and Roslyn refused the emitted C# (CS1503).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTSORTTK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB-T IS TYPEDEF STRONG.
          05 TE OCCURS 3.
             10 TGK PIC 9.
             10 TGT USAGE MESSAGE-TAG.
       01 V TYPE TB-T.
       PROCEDURE DIVISION.
       MAIN.
           SORT TE IN V ASCENDING KEY TGT IN V
           STOP RUN.
