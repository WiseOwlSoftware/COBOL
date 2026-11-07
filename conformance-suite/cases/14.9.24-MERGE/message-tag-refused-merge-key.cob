*>--- conformance-case
*> id: message-tag-refused-merge-key
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-3 - A MERGE KEY OF CLASS MESSAGE-TAG IS REFUSED BY §2023:14.9.24.3 SR4 c).
      *>   cite.py --check 14.9.24.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §2023:14.9.24.3 4) c)
      *> The MERGE twin of message-tag-refused-sort-file-key: SMT is a level-1 USAGE MESSAGE-TAG record of the SD, 8
      *> bytes wide as a slot (DataItem.PointerByteWidth), so the USING and GIVING records are X(8).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MTMERGEK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "mtmergek.tmp".
           SELECT MI1 ASSIGN TO "mtmergek1.dat".
           SELECT MI2 ASSIGN TO "mtmergek2.dat".
           SELECT MO1 ASSIGN TO "mtmergeko.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SMT USAGE MESSAGE-TAG.
       FD MI1.
       01 M1R PIC X(8).
       FD MI2.
       01 M2R PIC X(8).
       FD MO1.
       01 MOR PIC X(8).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY SMT USING MI1 MI2 GIVING MO1
           STOP RUN.
