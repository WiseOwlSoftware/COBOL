      *>--- conformance-case
      *> id: pb6024_column_past_999_accepted_85
      *> rules: [1985-GR-XIII.3.11.4-2, 1985-GR-XIII.3.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb6024a.txt,
      *>    expected: pb6024_column_past_999_accepted_85.exp}
      *>---
      *> kb/Work PB6024. ANSI X3.23-1985 gives a report no page width:
      *> the PAGE clause (1985:XIII.3.8.2) prints no width phrase, and
      *> the COLUMN NUMBER clause (1985:XIII.3.11.2) is "COLUMN NUMBER
      *> IS integer-1" with no bound on integer-1 in its syntax rules.
      *> 1985:XIII.3.11.4 GR2: "Integer-1 specifies the column number
      *> of the leftmost character position of the printable item."
      *> 1985:XIII.3.2.4 GR2: "The report writer logical record
      *> structure of the file associated with file-name-1 is defined
      *> by the implementor." With no RECORD clause WiseOwl's record is
      *> the widest line (docs/CONFORMANCE.md, Annex A.1 item 159).
      *> The 999 columns COBOL-2002 assumes (2002 13.16.37.3 GR5) are
      *> not a 1985 bound, so the program is legal at 85.
      *> DERIVATION. No PAGE clause: one page of indefinite length
      *> (1985:XIII.3.8.4 GR3). D1's LINE PLUS 1 puts its line on
      *> 0 + 1 = 1 (1985:XIII.3.10.8 GR4d): AAA in columns 1-3 and the
      *> twenty characters B...BZ in columns 990-1009. D2 on line 2:
      *> CCC in columns 1000-1002. Why it can fail: a compiler that
      *> bounds a 1985 line by 999 refuses the program (COBOL2710,
      *> COBOL2745), and an engine that cuts the line at 999 loses
      *> the BZ tail and the CCC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6024A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb6024a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       REPORT SECTION.
       RD  R1.
       01  D1 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "AAA".
           05  COLUMN 990 PIC X(20) VALUE "BBBBBBBBBBBBBBBBBBBZ".
       01  D2 TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1000 PIC X(3) VALUE "CCC".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE D1.
           GENERATE D2.
           TERMINATE R1.
           CLOSE PRTF.
           DISPLAY "DONE".
           STOP RUN.
