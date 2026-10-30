      *>--- conformance-case
      *> id: l1c20_occurs_dynamic_usage_per_occurrence
      *> rules: [GR-13.18.38.4-1]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR1 — the USAGE clause of an OCCURS DYNAMIC
      *> (format 4) entry applies to each occurrence: every element of
      *> the dynamic-capacity table has the clause's storage form.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR1 (FORMATS 1, 2 AND 4): "Except for the OCCURS
      *>   clause itself, all data description clauses associated
      *>   with an item whose description includes an OCCURS clause
      *>   apply to each occurrence of the item described."
      *>   OK  §13.18.38.4 1)  (General rules)
      *> §8.5.1.9.2: a dynamic-capacity table element referenced as a
      *>   sending operand gives "the same [result] as for a
      *>   fixed-capacity table whose number of occurrences is the
      *>   current capacity of the table".
      *>   OK  §8.5.1.9.2   (Operations on a single element)
      *> §15.14.1: "The BYTE-LENGTH function returns an integer equal
      *>   to the length of the argument in bytes."
      *>   OK  §15.14.1   (General)
      *> §13.18.40.5 Table 8: '-' gives space for a positive or zero
      *>   value, '-' for a negative value.
      *> The storage one occurrence takes is the implementor's
      *>   determination, docs/CONFORMANCE.md DOC-A.1-205 (BINARY:
      *>   3-4 digits is 2 bytes), DOC-A.1-215 (PACKED-DECIMAL:
      *>   digits/2 + 1 bytes, so 3 digits is 2) and DOC-A.1-209
      *>   (DISPLAY: one byte per character position).
      *> The companion golden l1c20_occurs_usage_per_occurrence pins
      *> formats 1 and 2.
      *>
      *> DERIVATION (FROM 3: current capacity 3, every occurrence set
      *> before it is read; the length is asked of occurrences 1-3).
      *> PL=2 2 2  PACKED-DECIMAL S9(3): 2 bytes each.
      *> BL=2 2 2  BINARY S9(4): 2 bytes each.
      *> DL=3 3 3  DISPLAY 9(3): 3 bytes each.
      *> P3 = -9 + (-5) = -14; P2 = 7; B3 = -1 - (-300) = 299;
      *> D = [001][002][003]. A wrong implementation that gave only
      *> the first element the clause would change a later length or
      *> value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20V.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 P  PIC S9(3) PACKED-DECIMAL OCCURS DYNAMIC FROM 3 TO 9.
       01 G2.
          05 B  PIC S9(4) BINARY OCCURS DYNAMIC FROM 3 TO 9.
       01 G3.
          05 D  PIC 9(3) OCCURS DYNAMIC FROM 3 TO 9.
       01 E     PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -5 TO P (1).
           MOVE 7 TO P (2).
           MOVE -9 TO P (3).
           ADD P (1) TO P (3).
           MOVE P (3) TO E.
           DISPLAY "P3=[" E "]".
           MOVE P (2) TO E.
           DISPLAY "P2=[" E "]".
           DISPLAY "PL=" FUNCTION BYTE-LENGTH(P (1))
              " " FUNCTION BYTE-LENGTH(P (2))
              " " FUNCTION BYTE-LENGTH(P (3)).
           MOVE -300 TO B (1).
           MOVE 1234 TO B (2).
           MOVE -1 TO B (3).
           SUBTRACT B (1) FROM B (3).
           MOVE B (3) TO E.
           DISPLAY "B3=[" E "]".
           DISPLAY "BL=" FUNCTION BYTE-LENGTH(B (1))
              " " FUNCTION BYTE-LENGTH(B (2))
              " " FUNCTION BYTE-LENGTH(B (3)).
           MOVE 1 TO D (1).
           MOVE 2 TO D (2).
           MOVE 3 TO D (3).
           DISPLAY "D=[" D (1) "][" D (2) "][" D (3) "]".
           DISPLAY "DL=" FUNCTION BYTE-LENGTH(D (1))
              " " FUNCTION BYTE-LENGTH(D (2))
              " " FUNCTION BYTE-LENGTH(D (3)).
           STOP RUN.
