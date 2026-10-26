*>--- conformance-case
*> id: pb3682-typedef-usage-group-contradiction
*> rules: [SR-13.18.60.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3682 - ISO 13.18.60.3 SR2: a USAGE clause written for a group item "may also be written in the data description entry
      *> for any subordinate elementary item or group item, but the same usage shall be specified in both entries" (cite.py: OK
      *> 13.18.60.3 2)). Asked of a TYPEDEF group whose member writes the OTHER usage; storage draws COBOLNET1927.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3682C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TG TYPEDEF USAGE BINARY.
          05 S USAGE PACKED-DECIMAL PIC 9.
       PROCEDURE DIVISION.
           STOP RUN.
