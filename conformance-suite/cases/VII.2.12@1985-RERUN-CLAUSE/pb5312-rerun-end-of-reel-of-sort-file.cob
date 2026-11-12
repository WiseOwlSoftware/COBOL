      *>--- conformance-case
      *> id: pb5312-rerun-end-of-reel-of-sort-file
      *> rules: [1985-SR-VII.2.12.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5312 - §1985:VII.2.12.3 SR2: file-name-2 of the END OF REEL phrase is the sort file SRTF.
      *> A sort-merge file has no organization (its file control entry admits only ASSIGN, §1985:XI.2.3.3 SR2), so
      *> it is no "sequentially organized file". COBOL6529, as for a relative file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5312B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CKPT ASSIGN TO "PB5312BC.DAT".
           SELECT SRTF ASSIGN TO "PB5312BS.TMP".
       I-O-CONTROL.
           RERUN ON CKPT EVERY END OF REEL OF SRTF.
       DATA DIVISION.
       FILE SECTION.
       FD  CKPT.
       01  CKPT-REC PIC X(4).
       SD  SRTF.
       01  SRTF-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB5312B RAN".
           STOP RUN.
