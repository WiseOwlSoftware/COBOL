      *>--- conformance-case
      *> id: pb5615-alphabetic-operands-refused
      *> rules: [SR-14.9.48.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2023:14.9.48.3 SR2: "Identifier-1,
      *>   identifier-2, identifier-3, and identifier-5 shall reference
      *>   data items of category alphanumeric or national." NM is PIC A,
      *>   a category of its own (§2023:8.5.2.1 Table 2), and from
      *>   COBOL-2014 no sentence treats it as alphanumeric (§2014:E.2).
      *>   Expected: COBOL1651 at 2014 and 2023 (the UNSTRING sender);
      *>   the 2002 twin pb5615-alphabetic-operands-2002 accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615OR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NM  PIC A(8) VALUE "NOSUCHPG".
       01  P1  PIC X(4).
       01  P2  PIC X(5).
       PROCEDURE DIVISION.
       MAIN-P.
           UNSTRING NM DELIMITED BY "S" INTO P1 P2
           STOP RUN.
