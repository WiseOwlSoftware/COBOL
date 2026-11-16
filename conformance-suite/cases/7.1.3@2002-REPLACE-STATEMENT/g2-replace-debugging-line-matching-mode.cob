      *>--- conformance-case
      *> id: g2-replace-debugging-line-matching-mode
      *> rules: [2002-SR-7.1.3.2-10, 2002-GR-7.1.3.3-8,
      *>    2002-GR-7.1.3.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> WITH DEBUGGING MODE: every D line is compiled, so no leg
      *> depends on where replaced text is placed (§2002:H.2 "The
      *> standard is ambiguous as to how debugging lines are treated
      *> when used in conjunction with or imbedded within COPY and
      *> REPLACE statements"); each leg rests on an explicit rule.
      *> LEG 1 - §2002:7.1.3.2 SR10 "Pseudo-text-1 and pseudo-text-2
      *> may contain debugging lines": pseudo-text-2 "R1" "R2" ends on
      *> a D line; §2002:7.1.3.3 GR8f "the corresponding pseudo-text-2
      *> replaces the matched text in the source text": AR1R2Z. A
      *> pseudo-text-2 that dropped its D line prints AR1Z.
      *> LEG 2 - §2002:7.1.3.3 GR8c6 "Text words within a debugging
      *> line participate in the matching": source "L3" on a D line
      *> matches: BR3Z (no match: BL3Z).
      *> LEG 3 - GR8c6 "A debugging indicator shall be treated as if
      *> it did not appear in the line": pseudo-text-1 "L5" "L6"
      *> matches source "L5" (normal line) "L6" (D line): R5Z (no
      *> match: L5L6Z).
      *> LEG 4 - §2002:7.1.3.3 GR3 "Partial-word-1 specifies the text
      *> to be replaced by partial-word-2": LEADING YY on the D-line
      *> word YY-VAL makes XX-VAL: NEW (no replacement: OLD).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RPL07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. COMPUTER-1 WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XX-VAL PIC X(3) VALUE "NEW".
       01 YY-VAL PIC X(3) VALUE "OLD".
       PROCEDURE DIVISION.
       P1.
           REPLACE =="L1"== BY =="R1"
      D    "R2"==
                   =="L3"== BY =="R3"==
                   =="L5" "L6"== BY =="R5"==
                   LEADING ==YY== BY ==XX==.
           DISPLAY "A" "L1" "Z".
           DISPLAY "B"
      D    "L3"
           "Z".
           DISPLAY "L5"
      D    "L6"
           "Z".
      D    DISPLAY YY-VAL.
           STOP RUN.
