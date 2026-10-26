      *>--- conformance-case
      *> id: pb3568-validate-undefined-operand
      *> rules: [SR-14.9.50.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> An operand that names nothing is not a data item described in any section: VALIDATE WS-A NOSUCH-ITEM.
      *> SETTING (fuzz fz02791, `VALIDATE WS-ALPHA SETTING WS-STATUS`) is no word of the statement, so it is a
      *> third operand and undefined. Expected: COBOL1639 for the undefined name (the one every identifier gets).
      *> ISO §14.9.50.3 SR1 - "Identifier-1 shall reference a data item described in the file, linkage,
      *> local-storage, or working-storage section. Identifier-1 shall not be reference-modified."
      *>   cite.py: OK  §14.9.50.3 1)  (Syntax rules)
      *> The VALIDATE facility is DECLINED (COBOL1580, docs/CONFORMANCE.md section 4 item 3): the statement is
      *> accepted inert. Declining the facility declines its BEHAVIOUR, never its syntax rules - the operand of the
      *> statement is still an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE WS-A NOSUCH-ITEM.
           STOP RUN.
