      *>--- conformance-case
      *> id: pb5189_sync_index_item_2002
      *> rules: [SR-13.18.55.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5189 - from COBOL-2002 SYNCHRONIZED may be written on a usage
      *> INDEX item: §2002:13.16.53.2 SR1 admits the clause for "an elementary
      *> item" with no usage excluded, and §2002:13.16.58.3 GR9 (cite.py --check
      *> OK) leaves an index data item's alignment and representation to the
      *> implementor ("Each implementor specifies the precise effect of the
      *> USAGE INDEX clause upon ..."). At COBOL-85 §1985:VI.5.14.3 SR6 bars it
      *> (pb5189-sync-index-item-1985, pb5189-sync-inherited-index-item-1985).
      *> DERIVATION: SET I TO 3 gives the index-name occurrence number 3 and
      *> SET IX TO I copies it to the index data item (an index data item
      *> takes an index-name, not an integer, §2023:14.9.39.3 SR3); SET I TO 1
      *> moves the index-name away and SET I TO IX brings it back to 3, so
      *> E (I) is E (3), which the MOVE sets to "Z": the program displays Z.
      *> Fails if: the clause is refused at 2002, 2014 or 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5189OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E PIC X OCCURS 4 TIMES INDEXED BY I.
       01  IX USAGE IS INDEX SYNCHRONIZED.
       01  G USAGE INDEX.
           05  J SYNC LEFT.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCD" TO T.
           SET I TO 3.
           SET IX TO I.
           SET I TO 1.
           SET I TO IX.
           MOVE "Z" TO E (I).
           DISPLAY E (3).
           STOP RUN.
