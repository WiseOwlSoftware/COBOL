*>--- conformance-case
*> id: pb1173-merge-key-dynamic-length
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 - A MERGE KEY THAT IS A DYNAMIC-LENGTH ITEM IS REFUSED BY SR4 d).
      *>   cite.py --check 14.9.24.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item, or an item subordinate to a dynamic-capacity table."
      *>     -> OK 14.9.24.3 4) d)   (MERGE prints a comma before "or an item subordinate"; SORT 14.9.40.3 6) d) does not)
      *> SK is PIC X DYNAMIC LENGTH (a COBOL-2014 construct), so the rule can only fire from 2014.
      *> The USING and GIVING records are sized to the SD record (K1 + SK is 1 to 4 bytes), so the record-size rules (MERGE SR3, SR12) hold
      *> and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512MD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512md.tmp".
           SELECT MI1 ASSIGN TO "pb2512md1.dat".
           SELECT MI2 ASSIGN TO "pb2512md2.dat".
           SELECT MO1 ASSIGN TO "pb2512mdo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 SK PIC X DYNAMIC LENGTH LIMIT 3.
       FD MI1.
       01 M1R PIC X(4).
       FD MI2.
       01 M2R PIC X(4).
       FD MO1.
       01 MOR PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY SK USING MI1 MI2 GIVING MO1
           STOP RUN.
