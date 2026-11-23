      *>--- conformance-case
      *> id: pb5871-keys-status-alphabetic-refused
      *> rules: [SR-12.4.5.12.3-2, SR-12.4.5.6.3-2, SR-12.4.5.8.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5871 - §2023:12.4.5.12.3 SR2 (RECORD KEY) and
      *>   §2023:12.4.5.6.3 SR2 (ALTERNATE RECORD KEY) require a data
      *>   item "of category alphanumeric or category national" and
      *>   §2023:12.4.5.8.3 SR2 (FILE STATUS) a two-character data item
      *>   "of the category alphanumeric". RK, AK and FS are PIC A,
      *>   category alphabetic, a category of its own (§2023:8.5.2.1
      *>   Table 2), and from COBOL-2014 on no sentence treats an
      *>   alphabetic item as alphanumeric (§2014:E.2). Expected:
      *>   COBOL0863 (the keys) and COBOL2455 (the status item) at 2014
      *>   and 2023; the 2002 twin pb5871-keys-status-alphabetic-2002
      *>   accepts it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5871L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5871l.dat"
              ORGANIZATION IS INDEXED
              ACCESS MODE IS DYNAMIC
              RECORD KEY IS RK
              ALTERNATE RECORD KEY IS AK
              FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 REC.
          05 RK PIC A(4).
          05 AK PIC A(3).
          05 DT PIC X(5).
       WORKING-STORAGE SECTION.
       01 FS PIC AA.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F
           CLOSE F
           STOP RUN.
