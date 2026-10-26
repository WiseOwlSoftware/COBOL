      *>--- conformance-case
      *> id: pb3390-data-entry-level-number-after-run
      *> rules: [FMT-13.16.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3390 - ISO 8.3.2.2.13: "A level-number, expressed as a one-digit or two-digit
      *> number". W-X's entry ends at a foreign word FOO with its separator period forgotten, and the
      *> next line opens W-Y's entry at level-number 05. The data division's error production used to
      *> take the integer into its run, so FOO 05 W-Y was ONE foreign clause and W-Y's PICTURE was
      *> reported as W-X's second (COBOL2423). Its run now holds no unsigned integer, since the
      *> next entry's level-number may follow these lists: the missing period is the diagnosis, at 05.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3390LN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X FOO
       05 W-Y PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3390LN".
           STOP RUN.
