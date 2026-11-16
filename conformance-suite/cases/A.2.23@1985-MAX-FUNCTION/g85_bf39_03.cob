      *>--- conformance-case
      *> id: g85_bf39_03
      *> rules: [1989-FMT-A.2.23.2, 1989-RV-A.2.23.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.23.2 - FUNCTION MAX ( { argument-1 } ... ): one
      *> argument (M1), three integer arguments (M2) and three numeric
      *> arguments mixing integer and noninteger (M3) are all legal
      *> forms; a compiler that refuses any of them rejects legal source.
      *> §1989:A.2.23.4 RV2 - "the content of the argument-1 returned is
      *> the leftmost argument-1 having that value" (T1-T4). The ties
      *> are alphanumeric operands of unequal size, which compare EQUAL
      *> because the shorter is extended on the right by spaces
      *> (§1989:A.2.23.4 RV1 -> §1985:VI.6.3.1.1.2 GRL2.2), and RV3 makes
      *> the returned size that of the selected argument, so the
      *> bracketed DISPLAY shows WHICH one was returned:
      *> T1 MAX("AB " "AB") -> [AB ]; T2 MAX("AB" "AB ") -> [AB];
      *> T3 MAX("A" "B " "B") -> [B ]; T4 MAX("B" "A" "B ") -> [B].
      *> A rightmost or longest choice fails T1 or T2; a choice that
      *> ignores the tie away from position 1 fails T3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3903.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X(3) VALUE "AB ".
       01 W-B PIC X(2) VALUE "AB".
       01 W-I PIC 99.
       01 W-S PIC S9V9.
       01 W-N PIC -9.9.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "M1=[" FUNCTION MAX ("Q") "]".
           COMPUTE W-I = FUNCTION MAX (11 3 7).
           DISPLAY "M2=" W-I.
           COMPUTE W-S = FUNCTION MAX (2.5 3 -1.5).
           MOVE W-S TO W-N.
           DISPLAY "M3=" W-N.
           DISPLAY "T1=[" FUNCTION MAX (W-A W-B) "]".
           DISPLAY "T2=[" FUNCTION MAX (W-B W-A) "]".
           DISPLAY "T3=[" FUNCTION MAX ("A" "B " "B") "]".
           DISPLAY "T4=[" FUNCTION MAX ("B" "A" "B ") "]".
           STOP RUN.
