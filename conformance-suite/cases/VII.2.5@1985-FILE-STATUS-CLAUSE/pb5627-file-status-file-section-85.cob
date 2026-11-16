      *>--- conformance-case
      *> id: pb5627-file-status-file-section-85
      *> rules: [1985-SR-VII.2.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5627. §1985:VII.2.5.3 SR2 (cite.py: OK): data-name-1
      *> "must be defined in the Data Division as a two-character data item
      *> of the category alphanumeric and must not be defined in the File
      *> Section, Report Section, or the Communication Section". FS-ITEM is
      *> a record item of the file's own FD, so it is refused under that
      *> rule. COBOL-85 has no local-storage section, which the 2023
      *> message (§2023:12.4.5.8.3 SR2) names.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5627C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5627c.txt"
               FILE STATUS IS FS-ITEM.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC.
           05  FS-ITEM PIC XX.
           05  F-DATA PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
