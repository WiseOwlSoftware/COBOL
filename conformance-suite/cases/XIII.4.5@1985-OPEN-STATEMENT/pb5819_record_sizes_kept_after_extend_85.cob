      *>--- conformance-case
      *> id: pb5819_record_sizes_kept_after_extend_85
      *> rules: [1985-GR-XIII.4.5.4-18]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5819 (the ruling: kb/Work/PB5819/
      *> 2026-11-16-the-row-takes-a-test.md). §1985:XIII.4.5.4 GR18:
      *> "The minimum and maximum record sizes for a file are
      *> established at the time the file is created and must not
      *> subsequently be changed", so the sizes in force at the READ
      *> are those of the creation under RECORD CONTAINS 12, unchanged
      *> by the OPEN EXTEND, and §1985:VII.1.3.5 gives status 04 where
      *> "the length of the record being processed does not conform to
      *> the fixed file attributes for that file", 00 otherwise.
      *> The expected lines pass through two elements the text leaves
      *> to the implementor, each a documented determination: the
      *> report writer logical record structure (DOC-A.1-159: each
      *> line is its characters and a line end, which another
      *> description reads as fixed 12-byte blocks, DOC-A.1-146 (a)
      *> and (c')) and which fixed attributes an OPEN validates
      *> (DOC-A.1-129: no OPEN compares a record size on a sequential
      *> file); so OPEN OUTPUT, OPEN EXTEND and OPEN INPUT are 00, the
      *> three whole blocks READ 00, the residue of the three line
      *> ends READ 04 (6 bytes for CR LF, 3 for LF) and then the at
      *> end condition READ 10. A compiler that records the sizes at
      *> creation and wrongly changes them at the OPEN EXTEND breaks
      *> the rule and prints 04 on the three whole blocks; only the
      *> statuses are displayed, never the record area, so the
      *> expected output holds whatever the host's line end.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5819RSZ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT  ASSIGN TO "pb5819.dat" FILE STATUS IS FSR.
           SELECT SEQF ASSIGN TO "pb5819.dat" FILE STATUS IS FSS.
       DATA DIVISION.
       FILE SECTION.
       FD  RPT RECORD CONTAINS 12 CHARACTERS
           REPORT IS RA.
       FD  SEQF RECORD CONTAINS 12 CHARACTERS.
       01  SREC PIC X(12).
       WORKING-STORAGE SECTION.
       77  FSR PIC XX.
       77  FSS PIC XX.
       77  N   PIC 99 VALUE 0.
       77  W   PIC X(12).
       REPORT SECTION.
       RD  RA.
       01  DA TYPE DETAIL LINE PLUS 1.
           05 COLUMN 1 PIC X(12) SOURCE W.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT RPT.
           DISPLAY "OPEN OUTPUT " FSR.
           INITIATE RA.
           MOVE "111111111111" TO W. GENERATE DA.
           MOVE "222222222222" TO W. GENERATE DA.
           TERMINATE RA.
           CLOSE RPT.
           OPEN EXTEND RPT.
           DISPLAY "OPEN EXTEND " FSR.
           INITIATE RA.
           MOVE "333333333333" TO W. GENERATE DA.
           TERMINATE RA.
           CLOSE RPT.
           OPEN INPUT SEQF.
           DISPLAY "OPEN INPUT " FSS.
           PERFORM RD1 UNTIL FSS NOT = "00" AND FSS NOT = "04"
               OR N > 9.
           CLOSE SEQF.
           STOP RUN.
       RD1.
           READ SEQF AT END MOVE "10" TO FSS.
           ADD 1 TO N.
           DISPLAY "READ " FSS.
