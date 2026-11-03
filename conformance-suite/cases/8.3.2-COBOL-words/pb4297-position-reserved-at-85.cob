      *>--- conformance-case
      *> id: pb4297-position-reserved-at-85
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4297 - the edition half of
      *>   pb4297_1985_words_user_defined_from_2002. POSITION is a
      *>   COBOL-85 reserved word (the MULTIPLE FILE TAPE clause's
      *>   POSITION phrase; reserved-words.json r85 true), so 8.3.2.1 1)
      *>   ("Reserved words shall not be used as user-defined words or
      *>   system-names") refuses it as a data-name at 85, and only
      *>   there. Expected: COBOL0901 naming POSITION. It fails if the
      *>   gate admits a word the edition reserves.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4297P85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 POSITION PIC X VALUE "L".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY POSITION
           STOP RUN.
