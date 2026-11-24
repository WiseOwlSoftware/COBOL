      *>--- conformance-case
      *> id: g2-ord-max-strings-arithmetic-standard
      *> rules: [2002-RV-15.58.3-1, 2002-RV-15.58.3-2,
      *>    2002-RV-15.58.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.58.3 RV1 - "the ordinal number that corresponds to
      *>   the position of the argument-1 having the greatest value"
      *> §2002:15.58.3 RV2 - "The comparisons used to determine the
      *>   greatest valued argument are made according to the rules
      *>   for simple conditions."
      *> §2002:15.58.3 RV3 - several equal to the greatest value: the
      *>   leftmost one's position.
      *> The arithmetic in force does not reach a comparison of
      *> alphanumeric arguments: ARITHMETIC IS STANDARD (the 2002 OPTIONS
      *> term) still compares the strings, never their digits, and
      *> still answers the ordinal (kb/Work PB2627: the standard-mode
      *> numeric bodies are keyed on the function, so the string form
      *> must be excluded from them by its own fact). Expected:
      *>   COMPUTE 002  RV1: "CCC" is greatest, position 2
      *>   MOVE    002  RV1: the same call in a MOVE
      *>   TIE     002  RV3: "CCC" twice, the leftmost is 2
      *>   PAD     001  RV2/RV3: "AB" = "AB " (space pad), leftmost 1
      *> A leg fails if a standard-mode numeric body takes the call.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OMXST.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC X(3) VALUE "BBB".
       01  W-B   PIC X(3) VALUE "CCC".
       01  W-C   PIC X(3) VALUE "AAA".
       01  W-P   PIC X(3) VALUE "AB".
       01  I     PIC 9(3).
       PROCEDURE DIVISION.
       P-MAIN.
           COMPUTE I = FUNCTION ORD-MAX(W-A W-B W-C)
           DISPLAY "COMPUTE=" I
           MOVE FUNCTION ORD-MAX(W-A W-B W-C) TO I
           DISPLAY "MOVE=" I
           MOVE FUNCTION ORD-MAX(W-A W-B W-B W-C) TO I
           DISPLAY "TIE=" I
           MOVE FUNCTION ORD-MAX("AB" W-P "A") TO I
           DISPLAY "PAD=" I
           STOP RUN.
