*>--- conformance-case
*> id: pb3682-typedef-group-usage-national-over-bit
*> rules: [SR-13.18.29.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.29.3 SR3: under GROUP-USAGE NATIONAL "All subordinate group items shall be explicitly or implicitly
      *> described as GROUP-USAGE NATIONAL" (cite.py: OK  13.18.29.3 3)). A TYPE DECLARATION's group is a group item whose
      *> clauses are source (13.18.57.4 GR1: "as though the data description identified by type-name-1 had been coded in
      *> place"), so the rule holds of the declaration although nothing references it. The usage walk ran over the storage
      *> roots only, so this program compiled clean while the same two groups in storage drew COBOL1653.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF GROUP-USAGE NATIONAL.
          05 S GROUP-USAGE BIT.
             10 B PIC 1.
       PROCEDURE DIVISION.
           STOP RUN.
