      *> reject-at: 85 2002 2014 2023
      *> ISO/IEC 1989:2023 §8.4.2.2.3 SR1: "For each non unique user-defined name that is explicitly referenced,
      *> uniqueness shall be established through a sequence of qualifiers that precludes any ambiguity of
      *> reference." TA and TB each declare INDEXED BY IX; the bare SET IX names neither uniquely, so it is
      *> refused (COBOLNET1639) - SR6's IX OF EA / IX OF EB is the conforming spelling (the positive companion
      *> 85/pb919_index_name_qualified). Before kb/Work PB919 this compiled clean and both tables silently
      *> shared one index.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W57PB919N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA.
          05 EA PIC 9(2) OCCURS 3 INDEXED BY IX.
       01 TB.
          05 EB PIC 9(2) OCCURS 4 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET IX TO 1
           DISPLAY EA (1) EB (1)
           STOP RUN.
