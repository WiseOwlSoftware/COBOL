*>--- conformance-case
*> id: pb3682-typedef-usage-index-with-value
*> rules: [SR-13.16.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.16.3 SR10: "The VALUE clause shall not be specified for data items of class index, message-tag, object, or
      *> pointer" (cite.py: OK  13.16.3 10)). The member takes USAGE INDEX from its TYPEDEF group (13.18.60.4 GR1), so its VALUE
      *> is refused in the declaration as it is in storage (COBOL2168).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE INDEX.
          05 A VALUE 1.
       PROCEDURE DIVISION.
           STOP RUN.
