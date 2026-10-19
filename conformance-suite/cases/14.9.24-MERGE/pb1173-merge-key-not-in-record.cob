*>--- conformance-case
*> id: pb1173-merge-key-not-in-record
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 (review R1-1) - A MERGE KEY MUST BE DESCRIBED IN A RECORD OF THE MERGE FILE, AND THE REFUSAL
      *> CITES MERGE'S RULE, NOT SORT'S.
      *>   cite.py --check 14.9.24.3 "The data items identified by key data-names shall be described in records
      *>     associated with file-name-1." -> OK §14.9.24.3 4) a)
      *> WK is a WORKING-STORAGE item, not part of any record of SW, so it cannot be a key. The MERGE twin of
      *> pb1173-sort-file-key-not-in-record: the refusal used to read "(ISO §14.9.40.3 SR6a)", SORT's clause, for a
      *> MERGE statement; the golden's expect pins "§14.9.24.3 SR4 a)".
      *> The USING and GIVING records are sized to the SD record (one byte), so the record-size rules (MERGE SR3,
      *> SR12) hold and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512MN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512mn.tmp".
           SELECT MI1 ASSIGN TO "pb2512mn1.dat".
           SELECT MI2 ASSIGN TO "pb2512mn2.dat".
           SELECT MO1 ASSIGN TO "pb2512mno.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
       FD MI1.
       01 M1R PIC X(1).
       FD MI2.
       01 M2R PIC X(1).
       FD MO1.
       01 MOR PIC X(1).
       WORKING-STORAGE SECTION.
       01 WK PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY WK USING MI1 MI2 GIVING MO1
           STOP RUN.
