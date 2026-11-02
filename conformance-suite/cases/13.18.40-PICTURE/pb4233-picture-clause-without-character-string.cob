      *>--- conformance-case
      *> id: pb4233-picture-clause-without-character-string
      *> rules: [FMT-13.18.40.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4233 - ISO 13.18.40.2 Format 1 prints PICTURE IS
      *> character-string-1, and character-string-1 is not in brackets,
      *> so the clause cannot be written without it (5.2.6.2 licenses
      *> omission only inside brackets). A period followed by a space is
      *> the separator period (8.3.5 3)), never a picture
      *> character-string, so the entry for WS-X below ends at its PIC
      *> with no character-string, and the next line (which begins with
      *> no level-number) is no part of it. Expected: the PICTURE clause
      *> is refused, naming the missing picture character-string, at
      *> every edition. It fails when the lexer reads past the period
      *> into the next line and takes X(3) as the picture: the program
      *> then compiled and ran as if WS-X were PIC X(3) VALUE 'ABC'.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4233NOPIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-X PIC.
           X(3) VALUE 'ABC'.
       PROCEDURE DIVISION.
           DISPLAY WS-X.
           STOP RUN.
