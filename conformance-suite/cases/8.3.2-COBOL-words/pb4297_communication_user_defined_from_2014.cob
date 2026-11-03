      *>--- conformance-case
      *> id: pb4297_communication_user_defined_from_2014
      *> rules: [SR-8.3.2.1-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4297 - 8.3.2.1 1): "Reserved words shall not be
      *>   used as user-defined words or system-names." COMMUNICATION is
      *>   on the 2002 8.9 list (the communication facility is obsolete
      *>   there) and on neither the 2009 draft's 8.9 nor the 2023 8.9:
      *>   the facility is removed from the 2014 standard, and its other
      *>   words (CD, DISABLE, ENABLE, ...) already leave the list at
      *>   2014. So from 2014 on it is a user-defined word. Expected
      *>   output: "COMM". It fails if the word is still refused at 2014
      *>   (COBOL0901), as GnuCOBOL's 2014 list made it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4297C14.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMMUNICATION PIC X(4) VALUE "COMM".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY COMMUNICATION
           STOP RUN.
