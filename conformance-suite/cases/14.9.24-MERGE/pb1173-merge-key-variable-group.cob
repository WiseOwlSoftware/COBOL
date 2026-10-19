*>--- conformance-case
*> id: pb1173-merge-key-variable-group
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 - A MERGE KEY THAT IS A VARIABLE-LENGTH GROUP IS REFUSED BY SR4 d).
      *>   cite.py --check 14.9.24.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item, or an item subordinate to a dynamic-capacity table."
      *>     -> OK 14.9.24.3 4) d)   (MERGE prints a comma before "or an item subordinate"; SORT 14.9.40.3 6) d) does not)
      *>   cite.py --check 8.5.1.12.1 "A variable-length group is a group item whose data description has at least one
      *>     dynamic-length elementary item or dynamic-capacity table as a subordinate item." -> OK 8.5.1.12.1
      *> KVG holds a dynamic-length elementary item, so it is a variable-length group (a COBOL-2014 construct).
      *> The USING and GIVING records are sized to the SD record (K1 + KVG is 1 to 4 bytes), so the record-size rules (MERGE SR3, SR12) hold
      *> and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512MV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512mv.tmp".
           SELECT MI1 ASSIGN TO "pb2512mv1.dat".
           SELECT MI2 ASSIGN TO "pb2512mv2.dat".
           SELECT MO1 ASSIGN TO "pb2512mvo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KVG.
             10 KD PIC X DYNAMIC LENGTH LIMIT 3.
       FD MI1.
       01 M1R PIC X(4).
       FD MI2.
       01 M2R PIC X(4).
       FD MO1.
       01 MOR PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY KVG USING MI1 MI2 GIVING MO1
           STOP RUN.
