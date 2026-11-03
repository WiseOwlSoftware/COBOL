      *>--- conformance-case
      *> id: pb4279-copy-text-name-edge-hyphen
      *> rules: [GR-8.1.3.2-4]
      *> files: [PB4279H-.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4279 - the hyphen twin of pb4279-copy-text-name-
      *>   edge-underscore. Text-name-1 is a system-name and so a COBOL
      *>   word (8.3.2.3.1; 8.3.2.1), and 8.3.2.1 says "The hyphen or
      *>   underscore shall not appear as the first or last character in
      *>   such words." PB4279H- ends in a hyphen. Expected: COBOL2773
      *>   at every edition. It fails if the COPY statement takes the
      *>   ill-formed word as a text-name silently.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4279H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           COPY PB4279H-.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY W-H
           STOP RUN.
