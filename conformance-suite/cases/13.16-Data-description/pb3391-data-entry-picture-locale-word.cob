      *>--- conformance-case
      *> id: pb3391-data-entry-picture-locale-word
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3391 - the CLOSED ISO 13.16.2 Format 1 general format of the data description
      *> entry prints no clause FOO. After PIC X, FOO SIZE 3 has the shape of the PICTURE clause's
      *> LOCALE phrase, which is recognized by the TEXT of its word: the word run FOO cannot continue
      *> at the reserved word SIZE, the LOCALE phrase was the one reading left, and its word test
      *> refused FOO, so the parse used to stop at a bare COBOL0001 "cannot parse construct near
      *> 'FOO'" and then refused SIZE as well. FOO is now refused BY NAME in the data description
      *> entry's format (COBOLNET1941), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3391DE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X FOO SIZE 3.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3391DE".
           STOP RUN.
