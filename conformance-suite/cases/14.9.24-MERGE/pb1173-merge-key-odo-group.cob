*>--- conformance-case
*> id: pb1173-merge-key-odo-group
*> rules: [SR-14.9.24.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2512 - A MERGE KEY THAT IS A GROUP HOLDING AN OCCURS DEPENDING TABLE IS REFUSED BY SR4 d), IN
      *> MERGE'S OWN WORDS.
      *>   cite.py --check 14.9.24.3 "A key data item shall not be a variable-length group, an occurs-depending-on
      *>     data item, a dynamic-length elementary item, or an item subordinate to a dynamic-capacity table."
      *>     -> OK 14.9.24.3 4) d)   (MERGE prints a comma before "or an item subordinate"; SORT 14.9.40.3 6) d) does not)
      *>   cite.py --check 13.18.38.4 "is an occurs-depending group item" -> OK 13.18.38.4 8)
      *> KODO has an entry subordinate to it that specifies DEPENDING, so it is an occurs-depending group item. The
      *> refusal quotes the clause verbatim, comma included: the SORT twin (pb1173-sort-file-key-odo-group) quotes
      *> SORT 14.9.40.3 6) d), whose copy has no comma, and a quote that mixed them up would pass cite.py, which
      *> ignores punctuation (the golden's expect pins the comma).
      *> The USING and GIVING records are sized to the SD record (CNT + KODO is 2 to 4 bytes), so the record-size rules (MERGE SR3, SR12) hold
      *> and the key rule is the ONE reason the program is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2512MO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "pb2512mo.tmp".
           SELECT MI1 ASSIGN TO "pb2512mo1.dat".
           SELECT MI2 ASSIGN TO "pb2512mo2.dat".
           SELECT MO1 ASSIGN TO "pb2512moo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 CNT PIC 9.
          05 KODO.
             10 KODE PIC X OCCURS 1 TO 3 DEPENDING ON CNT.
       FD MI1.
       01 M1R PIC X(4).
       FD MI2.
       01 M2R PIC X(4).
       FD MO1.
       01 MOR PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           MERGE SW ASCENDING KEY KODO USING MI1 MI2 GIVING MO1
           STOP RUN.
