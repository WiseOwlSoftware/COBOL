      *>--- conformance-case
      *> id: pb5073_index_name_twice_qualified
      *> rules: [SR-8.4.2.2.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5073 - the LEGAL side at COBOL-2002 and later of 1985:VI.5.8.3 SR14 ("Index-name-1 must be a unique word within the
      *> program."): 2023:8.4.2.2.3 SR6 qualifies an index-name by its table, so two tables may each declare IX.
      *> conformance:negative/pb5073-index-name-declared-twice-85 is the refusal at COBOL-85.
      *> RULE: cite.py --check "§2023:8.4.2.2.3 SR6" "The qualification of an index-name may include the name of the table with
      *>   which the index-name is associated" -> OK.
      *> DERIVATION. IX OF TAB1 is the index of E1, set to occurrence 2, so E1(2) receives "B" and E1 reads ".B."; IX OF TAB2 is
      *> set to 3, so E2(3) receives "C" and E2 reads "..C".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073Y.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TAB1.
           05  E1 PIC X VALUE "." OCCURS 3 TIMES INDEXED BY IX.
       01  TAB2.
           05  E2 PIC X VALUE "." OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P1.
           SET IX OF TAB1 TO 2
           SET IX OF TAB2 TO 3
           MOVE "B" TO E1 (IX OF TAB1)
           MOVE "C" TO E2 (IX OF TAB2)
           DISPLAY E1 (1) E1 (2) E1 (3)
           DISPLAY E2 (1) E2 (2) E2 (3)
           STOP RUN.
