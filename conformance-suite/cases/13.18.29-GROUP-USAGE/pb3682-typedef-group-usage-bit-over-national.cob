*>--- conformance-case
*> id: pb3682-typedef-group-usage-bit-over-national
*> rules: [SR-13.18.29.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.29.3 SR2: under GROUP-USAGE BIT "All subordinate group items shall be explicitly or implicitly
      *> described as GROUP-USAGE BIT" (cite.py: OK  13.18.29.3 2)) - the BIT twin of pb3682-typedef-group-usage-national-over-bit,
      *> asked of a type declaration that a TYPE clause DOES reference: the defect is the declaration's and is reported once, at
      *> the declaration (both references copy it, and neither repeats the report).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF GROUP-USAGE BIT.
          05 S GROUP-USAGE NATIONAL.
             10 N PIC N(2).
       01 V1 TYPE TG.
       01 V2 TYPE TG.
       PROCEDURE DIVISION.
           STOP RUN.
