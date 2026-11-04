*>--- conformance-case
*> id: pb4572-cobol-words-substituted-exception-object-is-no-register
*> rules: [GR-7.3.10.4-4, SR-8.4.3.6.3-2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 7.3.10.4 GR4 (kb/Work PB4572): "When the SUBSTITUTE option is specified, the COBOL word that is the
*> content of literal-5 shall be used in any syntax where the COBOL word that is the content of literal-4 is
*> documented as required or optional. The COBOL word that is the content of literal-4 shall no longer be a reserved
*> word, a context-sensitive word, nor an intrinsic function name within this compilation group."
*>   cite.py: OK  §7.3.10.4 4)  (General rules)
*> EXCEPTION-OBJECT, substituted by EXO, is a user-defined word in this compilation group, so it is no longer the
*> predefined object reference of 8.4.3.6.3 SR2 and this program names a data item nobody declared (COBOL1639).
*> The register's spelling test read it as the register whatever the directive said.
       >>COBOL-WORDS SUBSTITUTE "EXCEPTION-OBJECT" BY "EXO"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4572NE.
       PROCEDURE DIVISION.
           IF EXCEPTION-OBJECT = NULL
              DISPLAY "NULL"
           END-IF
           STOP RUN.
