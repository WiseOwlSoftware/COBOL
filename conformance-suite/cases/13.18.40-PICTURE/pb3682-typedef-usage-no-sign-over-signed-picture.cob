*>--- conformance-case
*> id: pb3682-typedef-usage-no-sign-over-signed-picture
*> rules: [SR-13.18.40.3-31]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.40.3 SR31: "The symbol 'S' shall not be specified in character-string-1 when the NO SIGN phrase of the
      *> USAGE Clause is specified for the subject of the entry" (cite.py: OK  13.18.40.3 31)). The phrase is part of the clause
      *> 13.18.60.4 GR1 applies to each elementary item of a group, a TYPEDEF group included.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE PACKED-DECIMAL WITH NO SIGN.
          05 A PIC S9.
       PROCEDURE DIVISION.
           STOP RUN.
