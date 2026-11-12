      *>--- conformance-case
      *> id: pb5165_implicit_qualifier_entry_clauses_2002
      *> rules: [SR-8.4.2.2.1-5]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5165 - from COBOL-2002 a data-name referenced in a data
      *> description entry clause takes the groups above the clause's subject as
      *> implicit qualifiers: §2002:F.2, item 67 (cite.py --check OK): "Data-names
      *> referenced in a data description entry need not be qualified when they
      *> are defined in the same group as the subject of the entry", §2023:8.4.2.2.1
      *> rule 5. The three clauses below each name a data-name that is declared
      *> twice in the program and once in the subject's own record. At COBOL-85
      *> §1985:IV.4.3.8.1 SR3 allows that only for redefinition, so each is
      *> COBOL1639 there (pb5165-odo-duplicate-name-1985 and its two siblings).
      *> DERIVATION: (1) CNT of OCCURS DEPENDING ON is R1's CNT (value 2), not R2's
      *> (4): R1 is CNT and 2 occurrences, so FUNCTION LENGTH (R1) is 3.
      *> (2) the key K of TK is TK's own K, not R4's: the table holds 1A 2B 3C
      *> in ascending key order, K OF TK (I3) = 2 holds for the second element,
      *> whose V is B. (3) XY RENAMES X THRU Y names R5's X and Y (R6, which follows,
      *> has its own), so XY after MOVE "PQR" TO R5 is PQ.
      *> Fails if: any clause is refused, or takes the other record's item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5165OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R1.
           05  CNT PIC 9 VALUE 2.
           05  T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON CNT.
       01  R2.
           05  CNT PIC 9 VALUE 4.
       01  R3.
           05  TK OCCURS 3 TIMES ASCENDING KEY IS K INDEXED BY I3.
               10  K PIC 9.
               10  V PIC X.
       01  R4.
           05  K PIC 9.
       01  R5.
           05  X PIC X.
           05  Y PIC X.
           05  Z PIC X.
       66  XY RENAMES X THRU Y.
       01  R6.
           05  X PIC X.
           05  Y PIC X.
       01  N PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE FUNCTION LENGTH (R1) TO N.
           DISPLAY N.
           MOVE "1A2B3C" TO R3.
           SEARCH ALL TK
               AT END DISPLAY "NOT FOUND"
               WHEN K OF TK (I3) = 2 DISPLAY V OF TK (I3).
           MOVE "PQR" TO R5.
           DISPLAY XY.
           STOP RUN.
