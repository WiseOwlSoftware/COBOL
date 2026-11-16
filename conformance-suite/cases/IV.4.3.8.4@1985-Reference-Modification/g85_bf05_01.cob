      *>--- conformance-case
      *> id: g85_bf05_01
      *> rules: [1985-GR-IV.4.3.8.4.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:IV.4.3.8.4.4 GR3 - the order in which a reference modifier
      *    is evaluated, one leg per lettered branch:
      * a. S (K) (K:1) selects the K-th character of the K-th element
      *    (A1). "the reference-modifier is applied to each of the
      *    implicitly specified elements of the table" for an ALL
      *    subscript: MAX and ORD-MIN over S (ALL) (n:1) compare one
      *    character of each element, so a modifier applied once to the
      *    whole table, or not at all, gives another answer (A2 Z not
      *    CAX; A3 2 not 1). Z (K) (K:1) as the second receiver of MOVE:
      *    K is moved first, then the subscript and the modifier are
      *    both evaluated (A4).
      * b. X (K:1) has no subscript: its modifier is "evaluated at the
      *    time subscripting would be evaluated" - for a MOVE receiver
      *    immediately before the move to it (§1985:VI.6.19.4 GR2), so
      *    after K = 4 (B1).
      * c. "reference modification is evaluated immediately after
      *    evaluation of the function": REVERSE ("abcde") (1:2) is "ed";
      *    a modifier applied to the argument first would give "ba"
      *    (C1).
      * §1989:A.2.2 AR4 - the ALL subscript in a repeated argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0501.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S-TBL.
           02  S           PIC X(3) OCCURS 3 TIMES.
       01  Z-TBL.
           02  Z           PIC X(4) OCCURS 4 TIMES.
       01  K               PIC 9 VALUE 1.
       01  N               PIC 9.
       01  X               PIC X(5) VALUE "abcde".
       01  R1              PIC X.
       01  R3              PIC X(3).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "AZB" TO S (1).
           MOVE "BCA" TO S (2).
           MOVE "CAX" TO S (3).
           MOVE 2 TO K.
           MOVE S (K) (K:1) TO R1.
           DISPLAY "A1=" R1.
           MOVE FUNCTION MAX (S (ALL) (2:1)) TO R1.
           DISPLAY "A2=" R1.
           COMPUTE N = FUNCTION ORD-MIN (S (ALL) (3:1)).
           DISPLAY "A3=" N.
           MOVE ALL "." TO Z-TBL.
           MOVE 1 TO K.
           MOVE 3 TO K Z (K) (K:1).
           DISPLAY "A4=" Z-TBL.
           MOVE 1 TO K.
           MOVE 4 TO K X (K:1).
           DISPLAY "B1=" X.
           MOVE FUNCTION REVERSE ("abcde") (1:2) TO R3.
           DISPLAY "C1=[" R3 "]".
           STOP RUN.
