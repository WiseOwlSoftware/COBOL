      *>--- conformance-case
      *> id: pb3568-validate-pointer-operand
      *> rules: [SR-14.9.50.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A data item of class pointer (USAGE POINTER): VALIDATE WS-P. Expected: COBOL4778.
      *> ISO §2023:14.9.50.3 SR2 - "Identifier-1 shall not reference a data item of class index, message-tag,
      *> object, or pointer."
      *>   cite.py: OK  §2023:14.9.50.3 2)  (Syntax rules)
      *> The VALIDATE statement is implemented (kb/Work PB5577) and its syntax rules are the statement's own: the operand of
      *> the statement is an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE WS-P.
           STOP RUN.
