      *>--- conformance-case
      *> id: pb5924-udw-duplicate-file-name-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "all user-defined words within a given disjoint set must be unique"
      *> Two files of one name: file-names take no qualifier, so the
      *> duplicate is refused even though no statement names it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWDFN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DUP-FILE ASSIGN TO DISKA.
           SELECT DUP-FILE ASSIGN TO DISKB.
       DATA DIVISION.
       FILE SECTION.
       FD  DUP-FILE.
       01  REC-A             PIC X(10).
       FD  DUP-FILE.
       01  REC-B             PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NO".
           STOP RUN.
