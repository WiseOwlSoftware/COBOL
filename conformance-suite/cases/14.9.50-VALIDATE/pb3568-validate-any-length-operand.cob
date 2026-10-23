      *>--- conformance-case
      *> id: pb3568-validate-any-length-operand
      *> rules: [SR-14.9.50.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> An item described with the ANY LENGTH clause (a linkage item of a CONTAINED program, where §13.18.2.3 SR2
      *> admits the clause): VALIDATE K. Expected: COBOLNET4779.
      *> ISO §14.9.50.3 SR5 - "Identifier-1 shall not be described with the ANY LENGTH clause."
      *>   cite.py: OK  §14.9.50.3 5)  (Syntax rules)
      *> The VALIDATE facility is DECLINED (COBOLNET1580, docs/CONFORMANCE.md section 4 item 3): the statement is
      *> accepted inert. Declining the facility declines its BEHAVIOUR, never its syntax rules - the operand of the
      *> statement is still an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568H.
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB3568HI" USING "A".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568HI.
       DATA DIVISION.
       LINKAGE SECTION.
       01 K PIC X ANY LENGTH.
       PROCEDURE DIVISION USING K.
       INNER-P.
           VALIDATE K.
           GOBACK.
       END PROGRAM PB3568HI.
       END PROGRAM PB3568H.
