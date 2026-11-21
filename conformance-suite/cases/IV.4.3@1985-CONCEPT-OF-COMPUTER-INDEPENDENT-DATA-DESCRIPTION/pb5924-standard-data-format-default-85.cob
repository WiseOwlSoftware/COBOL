      *>--- conformance-case
      *> id: pb5924-standard-data-format-default-85
      *> rules: [1985-GR-IV.4.3.4-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.3.4 (cite.py: OK): "the standard data format must be used for data items other than integer and numeric functions"
      *> With no USAGE a numeric item is held in standard data format:
      *> its redefinition and its group read as its digit characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STDFMT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NUM               PIC 9(3) VALUE 123.
       01  NUM-X REDEFINES NUM PIC X(3).
       01  GRP.
           05  G-N           PIC 9(2) VALUE 45.
           05  G-A           PIC X(2) VALUE "AB".
       01  OUT-X             PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY NUM-X.
           MOVE GRP TO OUT-X.
           DISPLAY OUT-X.
           IF NUM-X = "123" DISPLAY "STANDARD".
           STOP RUN.
