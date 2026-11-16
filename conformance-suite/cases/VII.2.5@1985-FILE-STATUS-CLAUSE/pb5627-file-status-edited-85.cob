      *>--- conformance-case
      *> id: pb5627-file-status-edited-85
      *> rules: [1985-SR-VII.2.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:VII.2.5.3 SR2 (cite.py: OK): "Data-name-1
      *> must be defined in the Data Division as a two-character data
      *> item of the category alphanumeric". PIC X0 is two characters
      *> long but alphanumeric edited, a category of its own, so the
      *> FILE STATUS clause is refused under the 1985 rule, not
      *> §2023:12.4.5.8.3 SR2. It compiled and ran with FS=00 before
      *> kb/Work PB850; GnuCOBOL refuses it too. Fixed form: 85 defines
      *> no free form (kb/Work PB5754).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQF ASSIGN TO "pb850fsae.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD SQF.
       01 SQ-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 SQ-STATUS PIC X0.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT SQF
           CLOSE SQF
           STOP RUN.
