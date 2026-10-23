      *>--- conformance-case
      *> id: pb3568-validate-66-level-operand
      *> rules: [SR-14.9.50.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A 66-level entry (RENAMES): VALIDATE R. Expected: COBOLNET4779.
      *> ISO §14.9.50.3 SR4 - "Identifier-1 shall not reference a 66-level entry."
      *>   cite.py: OK  §14.9.50.3 4)  (Syntax rules)
      *> The VALIDATE facility is DECLINED (COBOLNET1580, docs/CONFORMANCE.md section 4 item 3): the statement is
      *> accepted inert. Declining the facility declines its BEHAVIOUR, never its syntax rules - the operand of the
      *> statement is still an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 G1 PIC X(3).
       66 R RENAMES G1.
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE R.
           STOP RUN.
