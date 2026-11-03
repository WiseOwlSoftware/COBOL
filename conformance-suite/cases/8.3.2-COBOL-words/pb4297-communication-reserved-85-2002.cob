      *>--- conformance-case
      *> id: pb4297-communication-reserved-85-2002
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4297 - the edition half of
      *>   pb4297_communication_user_defined_from_2014. COMMUNICATION is
      *>   reserved at COBOL-85 and on the 2002 8.9 list, so 8.3.2.1 1)
      *>   ("Reserved words shall not be used as user-defined words or
      *>   system-names") refuses it as a data-name at 85 and 2002.
      *>   Expected: COBOL0901 naming COMMUNICATION. It fails if the
      *>   gate admits a word the edition reserves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4297C02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 COMMUNICATION PIC X(4) VALUE "COMM".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY COMMUNICATION
           STOP RUN.
