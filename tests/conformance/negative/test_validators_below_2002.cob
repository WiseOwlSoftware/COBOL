      *> reject-at: 85
      *> ISO §15.90 TEST-DATE-YYYYMMDD / §15.91 TEST-DAY-YYYYDDD / §15.93 TEST-NUMVAL / §15.94
      *> TEST-NUMVAL-C are COBOL-2002 introductions (§15.90/§15.91 carry direct in-spec 2002 attribution,
      *> D.31.3.1; PHASE-11-scout-notes.md spec:validators). Below 2002 the D8 catalog window rejects each
      *> reference BY NAME — COBOLNET1502 (IntrinsicBinder window gate).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11TESTV85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R2 PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE R2 = FUNCTION TEST-DATE-YYYYMMDD(20240229)
           COMPUTE R2 = FUNCTION TEST-DAY-YYYYDDD(2024366)
           COMPUTE R2 = FUNCTION TEST-NUMVAL("123.45")
           COMPUTE R2 = FUNCTION TEST-NUMVAL-C("$1,234.56")
           STOP RUN.
