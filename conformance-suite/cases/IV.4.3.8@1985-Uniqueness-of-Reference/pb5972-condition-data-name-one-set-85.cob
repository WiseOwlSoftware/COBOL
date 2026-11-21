      *>--- conformance-case
      *> id: pb5972-condition-data-name-one-set-85
      *> rules: [1985-SR-IV.4.3.8.1-L2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5972. §1985:IV.4.2.2.1.1 (cite.py: OK) groups the
      *> user-defined words into disjoint sets, the fourth
      *> "condition-names, data-names, and record-names", and "all
      *> user-defined words within a given disjoint set must be
      *> unique, except as specified in the rules for uniqueness of
      *> reference"; §1985:IV.4.3.8.1 SRL2.1 (cite.py: OK): "For each
      *> nonunique user-defined name that is explicitly referenced,
      *> uniqueness must be established through a sequence of
      *> qualifiers which precludes any ambiguity of reference."
      *> The level-88 X under F of R1 and the data item X of R2 are
      *> one name of one set, legal because every reference below
      *> qualifies it to one of them: X OF F and X IN R1 reach the
      *> level-88 alone (F is 1, so the condition is true twice), X
      *> OF R2 the data item alone (7). A compiler that refuses the
      *> pair at declaration (two types of user-defined word) fails
      *> at translation; one that binds X OF R2 to the level-88's
      *> variable shows 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5972A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05  F           PIC 9 VALUE 1.
               88  X       VALUE 1.
       01  R2.
           05  X           PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       MAIN-1.
           IF X OF F DISPLAY "COND".
           IF X IN R1 DISPLAY "COND2".
           DISPLAY X OF R2.
           STOP RUN.
