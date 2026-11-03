      *>--- conformance-case
      *> id: pb4279-copy-text-name-edge-underscore
      *> rules: [GR-8.1.3.2-4]
      *> files: [PB4279U_.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4279 - text-name-1 is a system-name (8.3.2.3.1
      *>   lists text-name) and so a COBOL word (8.3.2.1: "A COBOL word
      *>   is a character-string of not more than 63 characters that
      *>   forms ... a system-name"), and 8.3.2.1 bars its edges: "The
      *>   hyphen or underscore shall not appear as the first or last
      *>   character in such words." PB4279U_ ends in an underscore, so
      *>   it is refused here as it is in a data-name, although the
      *>   library text it names is present beside the test. Expected:
      *>   COBOL2773 at every edition (at 85 the underscore is also a
      *>   2002 introduction). It fails if the COPY statement takes the
      *>   ill-formed word as a text-name silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4279U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY PB4279U_.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY W-U
           STOP RUN.
