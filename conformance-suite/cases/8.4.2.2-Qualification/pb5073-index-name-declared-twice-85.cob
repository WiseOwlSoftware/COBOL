      *>--- conformance-case
      *> id: pb5073-index-name-declared-twice-85
      *> rules: [SR-8.4.2.2.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5073 - 1985:VI.5.8.3 SR14: "Index-name-1 must be a unique word within the program." (cite.py: OK). The rule is
      *>   unconditional: IX is the index-name of two tables and is never referenced. COBOL-2002 and later qualify an index-name
      *>   (2023:8.4.2.2.3 SR6), so conformance:pb5073_index_name_twice_qualified is the legal side at 2002 and later.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5073X.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TAB.
           05  ELT PIC X OCCURS 3 TIMES INDEXED BY IX.
           05  ELU PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
