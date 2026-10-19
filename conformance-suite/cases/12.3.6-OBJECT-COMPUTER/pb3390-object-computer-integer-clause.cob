      *>--- conformance-case
      *> id: pb3390-object-computer-integer-clause
      *> rules: [FMT-12.3.6.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3390 - the CLOSED ISO 12.3.6.2 general format: after OBJECT-COMPUTER.'s optional
      *> computer-name-1 stand only the CHARACTER CLASSIFICATION and PROGRAM COLLATING SEQUENCE clauses.
      *> An unsigned integer written after the computer-name used to leave the parser ONE reading: no
      *> name, and XYZ 12 as an unrecognized clause, so XYZ was refused as the clause. No level-number
      *> can follow this paragraph, so the error production now opens at the integer: XYZ is the
      *> computer-name and 12 is refused BY NAME in the OBJECT-COMPUTER paragraph's format
      *> (COBOLNET1970), the one diagnostic, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3390OI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. XYZ 12.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3390OI".
           STOP RUN.
