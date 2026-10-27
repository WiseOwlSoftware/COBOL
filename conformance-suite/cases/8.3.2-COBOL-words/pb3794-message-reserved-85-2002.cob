      *>--- conformance-case
      *> id: pb3794-message-reserved-85-2002
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3794 - the edition half of pb3794_message_data_item_continue_phrase. MESSAGE is a reserved
      *> word at COBOL-85 and COBOL-2002 (the communication facility's word; reserved-words.json r85/r2002 true)
      *> and is not on the 2023 8.9 list, so the same declaration that is legal at 2023 is refused there by
      *> 8.3.2.1 1): "Reserved words shall not be used as user-defined words or system-names." Expected:
      *> COBOL0901 naming MESSAGE. It fails if the gate admits the word as a name where 8.9 reserves it, which
      *> would also make the 2023 success of the twin a property of no edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794MSGR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MESSAGE   PIC 9     VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY MESSAGE
           STOP RUN.
