      *>--- conformance-case
      *> id: pb3794-multiple-reserved-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3794 - the edition half of pb3794_multiple_data_item_lock_on_multiple. 8.9 reserves MULTIPLE
      *> at COBOL-85 (the MULTIPLE FILE TAPE clause's word; reserved-words.json r85 true), so 8.3.2.1 1) -
      *> "Reserved words shall not be used as user-defined words or system-names" - refuses the declaration that
      *> 2002, 2014 and 2023 accept. Expected: COBOL0901 naming MULTIPLE. It fails if the gate admits the word as
      *> a name where 8.9 reserves it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794MULTR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MULTIPLE PIC 9 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY MULTIPLE.
           STOP RUN.
