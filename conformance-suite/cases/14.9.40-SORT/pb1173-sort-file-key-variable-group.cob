*>--- conformance-case
*> id: pb1173-sort-file-key-variable-group
*> rules: [SR-14.9.40.3-6]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 - A FILE SORT KEY THAT IS A VARIABLE-LENGTH GROUP IS REFUSED BY SR6 d).
      *>   cite.py --check 14.9.40.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item or an item subordinate to a dynamic-capacity table."
      *>     -> OK 14.9.40.3 6) d)
      *>   cite.py --check 8.5.1.12.1 "A variable-length group is a group item whose data description has at least one
      *>     dynamic-length elementary item or dynamic-capacity table as a subordinate item." -> OK 8.5.1.12.1
      *> The Format 2 twin is pb1173-sort-table-key-variable-group. KVG holds a dynamic-length elementary item.
      *> The USING and GIVING records are sized to the SD record (K1 + KVG is 1 to 4 bytes), so the record-size rules (SORT SR5, SR11) hold
      *> and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512SV.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512sv.tmp".
           SELECT IN1 ASSIGN TO "pb2512svi.dat".
           SELECT OU1 ASSIGN TO "pb2512svo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 K1 PIC X.
          05 KVG.
             10 KD PIC X DYNAMIC LENGTH LIMIT 3.
       FD IN1.
       01 IR PIC X(4).
       FD OU1.
       01 OR1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           SORT SW ASCENDING KEY KVG USING IN1 GIVING OU1
           STOP RUN.
