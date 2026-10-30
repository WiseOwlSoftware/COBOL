      *>--- conformance-case
      *> id: pb4090-send-format2-undefined-data-name
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO NOSUCH-TAG FROM WS-MSG RAISING LAST EXCEPTION. Expected: COBOL1639.
      *> ISO 14.9.38.2 prints the RAISING phrase in Format 2 (message-server-response) alone, and Format 2's TO
      *> operand is data-name-2, which 14.9.38.3 SR4 makes "the name of a message-tag data item". A Format 1 TO
      *> name is a message-server-name-1, declared nowhere (SR2), and is not resolved; but a statement that writes
      *> RAISING is Format 2, its TO name is a data name, and 8.4.2.1 requires that "a statement shall contain a
      *> reference that uniquely identifies" the resource - NOSUCH-TAG identifies none.
      *>   cite.py: OK  14.9.38.3 4)  OK  14.9.38.3 2)  OK  14.9.38.2
      *> Before kb/Work PB4090 the TO operand was never resolved, in either format, so the name compiled.
      *> SEND is the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1);
      *> declining it declines its behaviour, not the identifiers its statements name (kb/Work PB3568).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO NOSUCH-TAG FROM WS-MSG
               RAISING LAST EXCEPTION
           END-SEND.
           STOP RUN.
