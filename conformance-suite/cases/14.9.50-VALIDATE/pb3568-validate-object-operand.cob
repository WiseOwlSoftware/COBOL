      *>--- conformance-case
      *> id: pb3568-validate-object-operand
      *> rules: [SR-14.9.50.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A data item of class object (USAGE OBJECT REFERENCE): VALIDATE WS-O. Expected: COBOLNET4778.
      *> ISO §14.9.50.3 SR2 - "Identifier-1 shall not reference a data item of class index, message-tag,
      *> object, or pointer."
      *>   cite.py: OK  §14.9.50.3 2)  (Syntax rules)
      *> The VALIDATE facility is DECLINED (COBOLNET1580, docs/CONFORMANCE.md section 4 item 3): the statement is
      *> accepted inert. Declining the facility declines its BEHAVIOUR, never its syntax rules - the operand of the
      *> statement is still an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-O USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE WS-O.
           STOP RUN.
