      *>--- conformance-case
      *> id: pb4856-debug-evaluate-consecutive-when
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4856. X3.23-1985 USE FOR DEBUGGING general rule 5:
      *> "If identifier-1 is specified in a phrase that is not executed or
      *> evaluated, the associated debugging section is not executed".
      *> ISO 2023 14.9.13.4 GR4 processes each WHEN phrase left to
      *> right, and several may precede one imperative statement, so a
      *> phrase after one that selected the arm is never evaluated.
      *> A: WHEN 1 WHEN D with K = 1 selects at the first phrase, D is not evaluated, no
      *> section. B: K = 2 evaluates the second phrase, so the section
      *> for D runs after the statement. C: ALSO subjects, the second
      *> phrase evaluated only when the first is false. D: D written in
      *> the first phrase is always evaluated.
      *> Why each leg can fail: one OR-ed match flagged as a whole marks
      *> the later phrase evaluated when the earlier one selected the arm.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4856EC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D PIC 9 VALUE 5.
       01 K PIC 9 VALUE 1.
       01 J PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB0 SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       DB0-B.
           DISPLAY "  DB0".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "A: first phrase selects".
           EVALUATE K
             WHEN 1
             WHEN D DISPLAY "ARM"
           END-EVALUATE.
           DISPLAY "B: second phrase evaluated".
           MOVE 2 TO K.
           EVALUATE K
             WHEN 1
             WHEN D DISPLAY "ARM"
             WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           DISPLAY "C: ALSO".
           MOVE 1 TO K.
           EVALUATE K ALSO J
             WHEN 1 ALSO 1
             WHEN D ALSO D DISPLAY "ARM"
           END-EVALUATE.
           DISPLAY "D: first phrase names D".
           EVALUATE K
             WHEN D
             WHEN 1 DISPLAY "ARM"
           END-EVALUATE.
           DISPLAY "END".
           STOP RUN.
