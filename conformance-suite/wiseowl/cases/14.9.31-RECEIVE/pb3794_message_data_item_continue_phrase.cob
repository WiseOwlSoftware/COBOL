      *>--- conformance-case
      *> id: pb3794_message_data_item_continue_phrase
      *> rules: [FMT-14.9.31.2, SR-8.3.2.1-1]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3794 - a data item named MESSAGE and the RECEIVE statement's CONTINUE ... MESSAGE RECEIVED phrase
      *> in one COBOL-2023 program. ISO 14.9.31.2 prints
      *>   [ CONTINUE AFTER { arithmetic-expression-1 SECONDS | MESSAGE RECEIVED } ]
      *>   cite.py: OK  14.9.31.2  (General format) - "MESSAGE RECEIVED"
      *> and the 2023 8.9 list reserves MESSAGE-TAG but not MESSAGE (cite.py: OK 8.9 "MESSAGE-TAG"), so 8.3.2.1 1)
      *> ("Reserved words shall not be used as user-defined words or system-names") does not bar the declaration.
      *> Each leg fails if the reservation gate's retype of the declared word reaches the phrase's keyword:
      *>   leg 1  CONTINUE AFTER MESSAGE RECEIVED - the keyword arm, every word written;
      *>   leg 2  CONTINUE MESSAGE RECEIVED - AFTER is an optional word of this format (kb/Work PB3751);
      *>   leg 3  CONTINUE AFTER MESSAGE SECONDS - the data item IS arithmetic-expression-1;
      *>   leg 4  CONTINUE MESSAGE + 1 - the data item inside an expression, SECONDS omitted.
      *> Before the fix leg 1 was refused with "unexpected 'RECEIVED'".
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md
      *> section 4 item 1; Annex A.3 item 4): each RECEIVE is accepted and performs no message I-O, so the
      *> program's output is its own DISPLAYs. EXPECTED VALUES, DERIVED: MESSAGE starts at its VALUE 2, the ADD
      *> makes it 3 (14.9.2), and the receiving buffer keeps its VALUE because no message is moved into it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794MSG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG-TAG   PIC X(8)  VALUE "TAG".
       01 MSG-BODY  PIC X(8)  VALUE "BODY".
       01 MSG-LEN   PIC 9(4)  VALUE 0.
       01 MESSAGE   PIC 9     VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE AFTER MESSAGE RECEIVED
           END-RECEIVE
           RECEIVE MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE MESSAGE RECEIVED
           END-RECEIVE
           RECEIVE MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE AFTER MESSAGE SECONDS
           END-RECEIVE
           RECEIVE MSG-TAG GIVING MSG-BODY MSG-LEN
               CONTINUE MESSAGE + 1
           END-RECEIVE
           ADD 1 TO MESSAGE
           DISPLAY "MESSAGE=" MESSAGE
           DISPLAY "BODY=" MSG-BODY
           STOP RUN.
