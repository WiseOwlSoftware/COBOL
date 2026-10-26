      *>--- conformance-case
      *> id: pb3373-object-computer-literal-clause
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3373 - the CLOSED ISO 12.3.6.2 general format: after OBJECT-COMPUTER.'s optional
      *> computer-name-1 stand only the CHARACTER CLASSIFICATION and PROGRAM COLLATING SEQUENCE clauses.
      *> A literal written after the computer-name used to leave the parser ONE reading: no name, and
      *> XYZ "LIT" as an unrecognized clause, so XYZ was refused as the clause (and, at 85, as a
      *> clause without computer-name-1, COBOL0900).  XYZ is now the computer-name and the literal
      *> is refused BY NAME in the OBJECT-COMPUTER paragraph's format (COBOL1970), the one
      *> diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3373OL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ "LIT".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3373OL".
           STOP RUN.
