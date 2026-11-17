      *>--- conformance-case
      *> id: pb5888-external-relative-key-two-records-as-literal
      *> rules: [GR-12.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5888, review R1-1. §2023:13.18.22.4 GR5: "Literal-1, if
      *> specified, is the name of the file connector or record that is
      *> externalized to the operating environment." R1 is the record
      *> externalized as "A.B.C"; C is an item of ANOTHER record,
      *> externalized as "A.B". The two RELATIVE KEY items are therefore
      *> two external data items, not "the same corresponding storage"
      *> (§2023:14.8.4.2), and §2023:12.4.5.3 GR1 h) refuses the run
      *> unit (COBOL1575). A compiler that spells the record and item as
      *> one dotted name ("A.B.C" either way) accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb5888g.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS R1.
       DATA DIVISION.
       FILE SECTION.
       FD RLF IS EXTERNAL.
       01 RL-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 R1 IS EXTERNAL AS "A.B.C" PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           CALL "PB5888H".
           GOBACK.
       END PROGRAM PB5888G.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5888H.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLF ASSIGN TO "pb5888g.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS C.
       DATA DIVISION.
       FILE SECTION.
       FD RLF IS EXTERNAL.
       01 RL-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01 R2 IS EXTERNAL AS "A.B".
          05 C PIC 9(4).
       PROCEDURE DIVISION.
       P1.
           DISPLAY "B".
           GOBACK.
       END PROGRAM PB5888H.
