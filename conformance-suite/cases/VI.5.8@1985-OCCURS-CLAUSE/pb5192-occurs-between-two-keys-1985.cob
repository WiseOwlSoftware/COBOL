      *>--- conformance-case
      *> id: pb5192-occurs-between-two-keys-1985
      *> rules: [1985-SR-VI.5.8.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5192 - §1985:VI.5.8.3 SR12 (cite.py --check OK): "There must not
      *> be any entry that contains an OCCURS clause between the descriptions of
      *> the data items identified by the data-names in the KEY IS phrase and
      *> the subject of the entry." S is written between the two keys KA and KB, so it is between KB and the subject.
      *> From 2002 the entry may carry the clause unless it is a group in the
      *> key's hierarchy (§2002:F.2, item 84; pb5192_occurs_between_key_and_subject_2002).
      *> Fails if: the program compiles at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5192KK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           05  E OCCURS 3 TIMES ASCENDING KEY IS KA KB INDEXED BY I.
               10  KA PIC 9.
               10  S PIC X OCCURS 2 TIMES.
               10  KB PIC 9.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
