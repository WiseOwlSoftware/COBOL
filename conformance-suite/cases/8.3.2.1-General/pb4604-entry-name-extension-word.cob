      *>--- conformance-case
      *> id: pb4604-entry-name-extension-word
      *> rules: [SR-8.3.2.1-1, SR-13.16.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4604.  ENTRY is a word this implementation reserves for a nonstandard extension (ISO 4.2.10 3);
      *> docs/CONFORMANCE.md D-RW1), and 8.3.2.1 1) makes a reserved word of it (cite.py --check 8.3.2.1 "Reserved words
      *> shall not be used as user-defined words" -> OK).  Written where 13.16.3 SR4 puts the entry-name it is that
      *> reservation's fault, which the diagnostic names, not "not a clause of the data description entry".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4604C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ENTRY PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
