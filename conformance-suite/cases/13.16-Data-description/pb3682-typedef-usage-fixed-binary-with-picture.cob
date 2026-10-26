*>--- conformance-case
*> id: pb3682-typedef-usage-fixed-binary-with-picture
*> rules: [SR-13.16.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.16.3 SR8: "The PICTURE clause shall not be specified for ... an item whose usage is binary-char, binary-short,
      *> binary-long, binary-double, float-short, ..." (cite.py: OK  13.16.3 8)). The member's usage is BINARY-LONG by 13.18.60.4
      *> GR1, so its PICTURE is refused in the TYPEDEF group exactly as it is in storage (COBOLNET0870).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE BINARY-LONG.
          05 A PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
