*>--- conformance-case
*> id: pb3794-order-reserved-from-2002
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3794 - the edition half of pb3794_order_data_item_duplicates_in_order. 8.9 reserves ORDER from
      *> COBOL-2002 on (cite.py: OK 8.9 "ORDER"), so 8.3.2.1 1) - "Reserved words shall not be used as
      *> user-defined words or system-names" - refuses the declaration the COBOL-85 twin makes. Expected:
      *> COBOL0901 naming ORDER. It fails if the gate admits the word as a name where 8.9 reserves it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794ORDR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ORDER PIC 9 VALUE 7.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY ORDER.
           STOP RUN.
