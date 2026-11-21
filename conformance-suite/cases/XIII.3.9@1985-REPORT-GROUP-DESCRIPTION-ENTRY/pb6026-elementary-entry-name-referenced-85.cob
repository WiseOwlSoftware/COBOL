      *>--- conformance-case
      *> id: pb6026-elementary-entry-name-referenced-85
      *> rules: [1985-SR-XIII.3.9.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6026. A format 3 (elementary) entry: 1985:XIII.3.9.3
      *> SR10d, "Data-name-1 is optional but may be specified in any
      *> entry. Data-name-1 may be referenced only if the entry defines
      *> a sum counter." DI defines none, so MOVE DI is refused at 85
      *> (COBOL5092) citing SR10d; the 01 entry's data-name is held to
      *> SR5 and a group entry's to SR7 (1985-SR-XIII_3_9_3-5, -7), so
      *> a message that cites SR10d for those, or SR5 for this one,
      *> fails one of the three.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6026C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb6026c.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  W PIC X(3).
       REPORT SECTION.
       RD  R1.
       01  DA TYPE DETAIL LINE PLUS 1.
           05  DI COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           MOVE DI TO W.
           STOP RUN.
