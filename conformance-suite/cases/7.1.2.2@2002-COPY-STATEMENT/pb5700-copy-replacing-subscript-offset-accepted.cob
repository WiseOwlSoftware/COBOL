      *>--- conformance-case
      *> id: pb5700-copy-replacing-subscript-offset-accepted
      *> rules: [2002-SR-7.1.2.2-9]
      *> files: [PB5700S.cpy]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.2 SR9 - "If subscripting is specified, it shall not
      *> include any arithmetic expressions as subscripts with the
      *> exception of a single literal or identifier, or an identifier
      *> plus or minus an integer".
      *> Text-1 is T(I + 1): an identifier plus an integer is a
      *> subscript the rule leaves, and text-1 matches the library text
      *> T(I + 1), which is replaced by text-2 T(3). The library text
      *> displays T(3) and T(J): with J 3 both are c, so the program
      *> displays cc, where an unreplaced T(I + 1) would display b.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5700F.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TT VALUE "abc".
          05 T PIC X OCCURS 3.
       01 I PIC 9 VALUE 1.
       01 J PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN-P.
           COPY PB5700S REPLACING T(I + 1) BY T(3).
           STOP RUN.
