*>--- conformance-case
*> id: pb2533-data-entry-reserved-word-clause
*> rules: [FMT-13.16.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2533 - a RESERVED word written among the clauses of the CLOSED ISO 13.16.2 Format 1
      *> general format ("level-number [ entry-name-clause ] ..."; cite.py --check 13.16.2 "level-number"
      *> -> OK, General formats) is refused BY NAME, as an ordinary word run is (pb487-unrecognized-data-clause).
      *> BLOCK opens the BLOCK CONTAINS clause of a FILE description entry (13.4.5.2), not of a data
      *> description entry.  It was refused with a bare parse error ("COBOL0307: unexpected 'BLOCK'. A period
      *> may be missing ..."), because the error production that names a closed format's residue begins at an
      *> IDENTIFIER; it is now COBOLNET1941, the code this format's refusal has always carried.  The entry's
      *> clause list had already ENDED when the parser met BLOCK (PIC X(3) is a complete entry), which is
      *> why the refusal is reported where the clause list stands rather than where the entry's period is
      *> missing.  Refused at every edition: BLOCK is reserved in every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2533DE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(3) BLOCK CONTAINS 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
