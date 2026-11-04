*>--- conformance-case
*> id: constant-record-below-2014
*> rules: [FMT-13.18.15.2]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.15 (cite.py --check 13.18.15 "CONSTANT RECORD clause" -> OK) and Annex E.2 item 10
*> (cite.py --check E.2 "where previously external items could not be strongly typed" -> OK: the 2014 standard already
*> carried the clause). The CONSTANT RECORD clause is a COBOL-2014 introduction (kb/Work PB3600, PB296; VCR row 7.48): the
*> post-ballot 2002 text (J4, 2001-12-04) has no such clause and the 2009 committee draft first has it at clause
*> 13.18.15. So the program below is refused at 85 and at 2002 with COBOL0900 and accepted from 2014 (the positive twin
*> is 13.18.15-CONSTANT-RECORD/constant_record, accepted at 2014 and 2023). Each leg can fail by being accepted at either
*> edition: the version-conformance pass is the only thing that stops it.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGKC02.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 CFG CONSTANT RECORD.
   05 CFG-TAG PIC X(4) VALUE "COBL".
PROCEDURE DIVISION.
MAIN.
    DISPLAY CFG-TAG.
    STOP RUN.
