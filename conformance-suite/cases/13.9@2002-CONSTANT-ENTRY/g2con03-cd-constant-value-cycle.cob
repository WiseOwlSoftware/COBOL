      *>--- conformance-case
      *> id: g2con03-cd-constant-value-cycle
      *> rules: [2002-SR-13.9.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR5 - "Neither the value of literal-1 nor the value
      *>   of any of the literals in arithmetic-expression-1 shall be
      *>   dependent, directly or indirectly, upon the value of
      *>   constant-name-1." Constant entries in the COMMUNICATION SECTION,
      *>   under a CD: KA AS KB + 1 and KB AS KA + 1, so KA's value depends
      *>   on itself indirectly through KB. Expected: refused (COBOL1547).
      *>   Legal twin: g2con01_cd_constant_entries (KA AS KB + 1, KB AS 3).
      *> Fails if: the cycle check does not reach constants under a CD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CON03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  KA CONSTANT AS KB + 1.
       01  KB CONSTANT AS KA + 1.
       01  CDREC PIC X(10).
       PROCEDURE DIVISION.
           DISPLAY "COMPILED"
           STOP RUN.
