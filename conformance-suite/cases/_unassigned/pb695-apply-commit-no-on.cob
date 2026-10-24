*>--- conformance-case
*> id: pb695-apply-commit-no-on
*> rules: []
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> !! THE OMITTED OPTIONAL WORD MUST REACH THE CLAUSE, NOT A PARSE ERROR (PB695).
      *> ISO 12.4.6.3.2 prints `APPLY COMMIT ON [ [ file-name-1 ] [ identifier-1 ] ] ...`. Measured on
      *> printed page 363 / folio 333: APPLY carries an underline rectangle (x 143.99-177.13 beneath a
      *> box of 145.21-175.84, 100% cover) and COMMIT one at 96.8%, while ON's box 228.49-243.51 has
      *> NO rule in its band at all. 8.3.2.4.3 therefore makes `APPLY COMMIT F` a conforming spelling
      *> of the clause, and the grammar's own comment claimed the opposite until family 3 measured it.
      *> F is SEQUENTIAL, which ISO 9.1.18.2 admits ("any type of file on a mass storage device may be
      *> specified"; kb/Work PB3340), so the ON-less clause puts F under commit control: the file the OPEN
      *> OUTPUT creates is absent again after the ROLLBACK (ISO 14.9.36.4 GR3, "restored to the state they
      *> were in at ... the start of the run unit"), and the OPEN INPUT of the OPTIONAL file answers '05'
      *> (ISO 9.1.13.2 item 4 a)). A clause the parser could not read would report COBOL0001; one parsed but
      *> ignored would leave the file there ('00').
      *> accept names 2023 ALONE, and that is not an omission: the whole commit-and-rollback facility is a
      *> COBOL-2023 addition (Annex E.3.2 item 2), so below 2023 APPLY and COMMIT are ordinary
      *> user-defined words and this clause does not exist (pb3338_apply_commit_below_2023).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB695APPNOON.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL F ASSIGN TO "pb695-apply-no-on.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
       I-O-CONTROL.
           APPLY COMMIT F.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           WRITE R FROM "W001"
           CLOSE F
           ROLLBACK
           OPEN INPUT F
           DISPLAY "OPEN INPUT F AFTER ROLLBACK " FS
           STOP RUN.
