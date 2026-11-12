      *>--- conformance-case
      *> id: pb5312-rerun-on-sort-file
      *> rules: [1985-SR-VII.2.12.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5312 - §1985:VII.2.12.3 SR1: file-name-1 of the RERUN clause is the sort file SRTF.
      *> A sort-merge file has no organization (its file control entry admits only ASSIGN, §1985:XI.2.3.3 SR2), so
      *> it is no "sequentially organized file". COBOL6529, as for a relative file.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5312A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CKPT ASSIGN TO "PB5312AC.DAT".
           SELECT SRTF ASSIGN TO "PB5312AS.TMP".
       I-O-CONTROL.
           RERUN ON SRTF EVERY END OF REEL OF CKPT.
       DATA DIVISION.
       FILE SECTION.
       FD  CKPT.
       01  CKPT-REC PIC X(4).
       SD  SRTF.
       01  SRTF-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "PB5312A RAN".
           STOP RUN.
