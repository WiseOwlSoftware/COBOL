*>--- conformance-case
*> id: pb3682-typedef-usage-national-without-picture
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.16.3 SR8: "For any other entry describing an elementary item, a PICTURE clause shall be specified except as
      *> indicated in Syntax rule 9" (cite.py: OK  13.16.3 8)); USAGE NATIONAL is not among the picture-less usages the rule's first
      *> sentence names, and a member takes it from its TYPEDEF group (13.18.60.4 GR1), so a member with no PICTURE is refused in
      *> the declaration.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE NATIONAL.
          05 A.
       PROCEDURE DIVISION.
           STOP RUN.
