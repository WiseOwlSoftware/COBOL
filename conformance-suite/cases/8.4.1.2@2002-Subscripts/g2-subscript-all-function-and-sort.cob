      *>--- conformance-case
      *> id: g2-subscript-all-function-and-sort
      *> rules: [2002-SR-8.4.1.2.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.2.2 SR6 - "The subscript ALL may be used only when
      *> the subscripted identifier is used as an intrinsic function
      *> argument or to identify the table in the table format of the
      *> SORT statement." Both granted contexts, accepted and run (the
      *> refusal elsewhere is pb1030-all-subscript-outside-function).
      *> Intrinsic argument: "the effect is as if each table element
      *> associated with that subscript position were specified", the
      *> rightmost ALL varying fastest (§2002:15.2).
      *>   K = 4 1 3 2: SUM 10, MAX 4, ORD-MIN 2            -> "A1 10 4 2"
      *>   E = (c a b)(f e d), so E (ALL, ALL) is c a b f e d:
      *>   MAX f; ORD-MAX 4 (row-major; column-major c f a e b d
      *>   would give 2); MIN (E (2, ALL)) d; ORD-MIN 2      -> "A2 f 4 d 2"
      *>   (§2002:15.58.3 RV1: "the position of the argument-1 having
      *>   the greatest value in the argument-1 series").
      *> SORT table format: "the rightmost subscript shall be the word
      *> ALL" for a table under two OCCURS levels (§2002:14.8.36.2 SR13;
      *> §2002:8.4.1.2.2 SR5). The key is E itself, named as
      *> data-name-1 ("shall be the same as, or subordinate to, the
      *> data item referenced by data-name-2", §2002:14.8.36.2 SR14a;
      *> "The data items referenced by the specifications of
      *> data-name-1 are the key data items", §2002:14.8.36.3 GR2).
      *> SORT E (2, ALL) ASCENDING KEY E orders only row 2, lower value
      *> first (GR18a) -> "A3 cabdef"; SORT E (1, ALL) DESCENDING KEY E
      *> orders row 1, higher value first (GR18b) -> "A4 cbadef".
      *> Each leg can fail: a refused ALL fails the compile; ALL
      *> expanded to one element or in the wrong order changes SUM,
      *> ORD-MAX or ORD-MIN; a SORT over the wrong row changes M.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G.
           05 K PIC 9 OCCURS 4 TIMES.
       01  M.
           05 R OCCURS 2 TIMES.
              10 E PIC X OCCURS 3 TIMES.
       01  N2 PIC 99.
       01  N1 PIC 9.
       01  N3 PIC 9.
       01  C1 PIC X.
       01  C2 PIC X.
       01  N4 PIC 9.
       01  N5 PIC 9.
       PROCEDURE DIVISION.
           MOVE "4132" TO G.
           MOVE "cabfed" TO M.
           COMPUTE N2 = FUNCTION SUM (K (ALL)).
           COMPUTE N1 = FUNCTION MAX (K (ALL)).
           COMPUTE N3 = FUNCTION ORD-MIN (K (ALL)).
           DISPLAY "A1 " N2 " " N1 " " N3.
           MOVE FUNCTION MAX (E (ALL, ALL)) TO C1.
           COMPUTE N4 = FUNCTION ORD-MAX (E (ALL, ALL)).
           MOVE FUNCTION MIN (E (2, ALL)) TO C2.
           COMPUTE N5 = FUNCTION ORD-MIN (E (ALL, ALL)).
           DISPLAY "A2 " C1 " " N4 " " C2 " " N5.
           SORT E (2, ALL) ASCENDING KEY E.
           DISPLAY "A3 " M.
           SORT E (1, ALL) DESCENDING KEY E.
           DISPLAY "A4 " M.
           STOP RUN.
