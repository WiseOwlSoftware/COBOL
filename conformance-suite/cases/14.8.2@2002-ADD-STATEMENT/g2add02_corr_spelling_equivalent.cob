      *>--- conformance-case
      *> id: g2add02_corr_spelling_equivalent
      *> rules: [2002-SR-14.8.2.2-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR5 - "The words CORR and CORRESPONDING are
      *> equivalent." Format 3 of the 2002 ADD statement (14.8.2.1)
      *> spells the phrase either way, with the plain 2002 ROUNDED
      *> phrase and the SIZE ERROR phrases. Each leg runs the SAME
      *> statement under both spellings on identical targets, so the
      *> two must agree; the values come from the 2002 rules:
      *>  §2002:14.6.5 rule 3: in ADD "both of the data items are
      *>    numeric data items" - pairs A, B, E; C (alphanumeric) and D
      *>    (no namesake) are not added.
      *>  L1 plain: A 2.0 + 1.25 = 3.25 truncated to 9(3)V9 -> 003.2
      *>    (§2002:14.6.3: truncation without ROUNDED); B 1 + 20 = 21;
      *>    E 200 + 50 = 250 -> 021 0032 QQQ 005 250.
      *>  L2 ROUNDED: §2002:14.6.3 "the absolute value of the resultant
      *>    identifier is increased by one in the low-order position
      *>    whenever the most significant digit of the excess is
      *>    greater than or equal to five": 3.25 -> 003.3.
      *>  L3 ON SIZE ERROR: E 200 + 900 = 1100 exceeds 9(3), so E keeps
      *>    200 while A and B are stored (§2002:14.6.4 size error rule
      *>    2) and the imperative runs once after all pairs
      *>    (§2002:14.6.5) -> SE then 021 0032 QQQ 005 200.
      *>  L4 NOT ON SIZE ERROR: the L1 targets plus S1 again: A 3.2 +
      *>    1.25 = 4.45 -> 004.4, B 41, E 300; no size error, so the
      *>    NOT SIZE ERROR imperative runs -> NSE then 041 0044 QQQ 005
      *>    300.
      *> Why each leg can fail: a spelling not accepted in one of the
      *> forms (with ROUNDED, SIZE ERROR, NOT SIZE ERROR, END-ADD), a
      *> spelling bound to a different statement, or the two spellings
      *> producing different pairs or results.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADD02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S1.
           05  A        PIC 9(3)V99 VALUE 1.25.
           05  B        PIC 9(3)    VALUE 20.
           05  C        PIC X(3)    VALUE "XYZ".
           05  E        PIC 9(3)    VALUE 50.
       01  S2.
           05  A        PIC 9(3)V99 VALUE 1.25.
           05  B        PIC 9(3)    VALUE 20.
           05  E        PIC 9(3)    VALUE 900.
       01  T1.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       01  T2.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       01  T3.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       01  T4.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       01  T5.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       01  T6.
           05  B        PIC 9(3)    VALUE 1.
           05  A        PIC 9(3)V9  VALUE 2.0.
           05  C        PIC X(3)    VALUE "QQQ".
           05  D        PIC 9(3)    VALUE 5.
           05  E        PIC 9(3)    VALUE 200.
       PROCEDURE DIVISION.
       P0.
           ADD CORR S1 TO T1.
           ADD CORRESPONDING S1 TO T2.
           DISPLAY "L1 CORR          " T1.
           DISPLAY "L1 CORRESPONDING " T2.
           ADD CORR S1 TO T3 ROUNDED.
           ADD CORRESPONDING S1 TO T4 ROUNDED.
           DISPLAY "L2 CORR          " T3.
           DISPLAY "L2 CORRESPONDING " T4.
           ADD CORR S2 TO T5
               ON SIZE ERROR DISPLAY "L3 CORR SE"
               NOT ON SIZE ERROR DISPLAY "L3 CORR NSE"
           END-ADD.
           ADD CORRESPONDING S2 TO T6
               ON SIZE ERROR DISPLAY "L3 CORRESPONDING SE"
               NOT ON SIZE ERROR DISPLAY "L3 CORRESPONDING NSE"
           END-ADD.
           DISPLAY "L3 CORR          " T5.
           DISPLAY "L3 CORRESPONDING " T6.
           ADD CORR S1 TO T1
               ON SIZE ERROR DISPLAY "L4 CORR SE"
               NOT ON SIZE ERROR DISPLAY "L4 CORR NSE"
           END-ADD.
           ADD CORRESPONDING S1 TO T2
               ON SIZE ERROR DISPLAY "L4 CORRESPONDING SE"
               NOT ON SIZE ERROR DISPLAY "L4 CORRESPONDING NSE"
           END-ADD.
           DISPLAY "L4 CORR          " T1.
           DISPLAY "L4 CORRESPONDING " T2.
           STOP RUN.
