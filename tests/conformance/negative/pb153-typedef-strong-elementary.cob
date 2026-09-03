      *> reject-at: 2002 2014 2023
      *> ISO 8.5.3.1: "Elementary type definitions shall not be
      *> specified with the STRONG phrase." - with 8.5.3.3, "The only
      *> kind of items that may be strongly typed are group items."
      *> UNENFORCED until kb/Work PB153, and the illegal shape had
      *> already reached the corpus as the canonical witness of the
      *> version-matrix row usage-pointer-to-type-2014.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB153N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG PIC 9(3).
       01 V TYPE T.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
