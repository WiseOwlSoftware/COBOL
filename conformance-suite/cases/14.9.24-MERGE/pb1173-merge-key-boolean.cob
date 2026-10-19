*>--- conformance-case
*> id: pb1173-merge-key-boolean
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 (review R1-3) - A MERGE KEY OF CLASS BOOLEAN IS REFUSED BY SR4 c).
      *>   cite.py --check 14.9.24.3 "Key data items shall not be of the class boolean, message-tag, object, or
      *>     pointer." -> OK §14.9.24.3 4) c)
      *> The MERGE twin of pb1173-sort-file-key-boolean: KB is PIC 1 USAGE BIT, class boolean (USAGE BIT is a
      *> COBOL-2002 construct, so the rule can only fire from 2002). Before this golden no MERGE case witnessed the
      *> boolean arm of the class screen.
      *> The USING and GIVING records are sized to the SD record (K1 + KB is two bytes), so the record-size rules
      *> (MERGE SR3, SR12) hold and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512MB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512mb.tmp".
           SELECT MI1 ASSIGN TO "pb2512mb1.dat".
           SELECT MI2 ASSIGN TO "pb2512mb2.dat".
           SELECT MO1 ASSIGN TO "pb2512mbo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KB PIC 1 USAGE BIT.
       FD MI1.
       01 M1R PIC X(2).
       FD MI2.
       01 M2R PIC X(2).
       FD MO1.
       01 MOR PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY KB USING MI1 MI2 GIVING MO1
           STOP RUN.
