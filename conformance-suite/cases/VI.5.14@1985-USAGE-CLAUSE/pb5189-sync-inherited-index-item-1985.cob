      *>--- conformance-case
      *> id: pb5189-sync-inherited-index-item-1985
      *> rules: [1985-SR-VI.5.14.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5189 - §1985:VI.5.14.3 SR6 (cite.py --check OK): "The BLANK
      *> WHEN ZERO, JUSTIFIED, PICTURE, SYNCHRONIZED, and VALUE clauses must not
      *> be specified for data items whose usage is INDEX." The other four are
      *> refused at every edition; SYNCHRONIZED was not. §1985:VI.5.14.4 GR1
      *> hands a group's USAGE down to its elementary items, so the clause on a
      *> subordinate of a USAGE INDEX group is on an item whose usage is INDEX.
      *> From 2002 the clause is legal there (pb5189_sync_index_item_2002).
      *> Fails if: the program compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5189INH.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G USAGE INDEX.
           05  J SYNC LEFT.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
