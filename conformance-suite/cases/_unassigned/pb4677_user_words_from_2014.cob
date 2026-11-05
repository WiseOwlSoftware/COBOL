      *>--- conformance-case
      *> id: pb4677_user_words_from_2014
      *> rules: []
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4677 - ISO 8.9 does not reserve the communication facility's words
      *> from 2014, so each new lexer token is an ordinary user-defined word there:
      *> a data-name and a paragraph-name, and a section of the old name still parses
      *> as the data item it now is. A token outside the nameSlot funnel fails here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4677UW.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMMUNICATION PIC X VALUE "A".
       01 CD PIC X VALUE "B".
       01 DISABLE PIC X VALUE "C".
       01 EGI PIC X VALUE "D".
       01 EMI PIC X VALUE "E".
       01 ENABLE PIC X VALUE "F".
       01 ESI PIC X VALUE "G".
       01 PURGE PIC X VALUE "H".
       01 QUEUE PIC X VALUE "I".
       01 SEGMENT PIC X VALUE "J".
       01 SUB-QUEUE-1 PIC X VALUE "K".
       01 SUB-QUEUE-2 PIC X VALUE "L".
       01 SUB-QUEUE-3 PIC X VALUE "M".
       01 TERMINAL PIC X VALUE "N".
       01 TEXT PIC X VALUE "O".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY COMMUNICATION CD DISABLE EGI EMI ENABLE ESI PURGE.
           DISPLAY QUEUE SEGMENT SUB-QUEUE-1 SUB-QUEUE-2 SUB-QUEUE-3.
           DISPLAY TERMINAL TEXT.
           MOVE ENABLE TO DISABLE.
           DISPLAY DISABLE.
           STOP RUN.
