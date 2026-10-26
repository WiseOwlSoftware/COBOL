      *>--- conformance-case
      *> id: pb3751_page_limit_optional_words
      *> rules: [FMT-13.18.39.2, GR-13.18.39.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3751 - every spelling of the PAGE clause's
      *> [ LIMIT IS | LIMITS ARE ] bracket sets the same page limit.
      *>
      *> THE RULE. The printed 13.18.39.2 format (PDF p468) underlines
      *> PAGE alone: LIMIT, IS, LIMITS and ARE are optional words.
      *> 8.3.2.4.3: "Within each format, uppercase words that are not
      *>   underlined are called optional words and may be specified at
      *>   the user's option with no effect on the semantics of the
      *>   format."
      *>   OK  8.3.2.4.3  (Optional words)
      *> So once an alternative of the bracket is chosen, either of its
      *> words may be left out: PAGE IS 3 and PAGE ARE 3 are the same
      *> clause as PAGE LIMIT IS 3. The grammar demanded LIMIT before IS
      *> and LIMITS before ARE, and refused both (COBOL0001).
      *> The negative half: pb3751-page-limit-mixed-alternatives (a word
      *> of each alternative is no spelling of either).
      *>
      *> DERIVATION. Every report below has page limit 3 and one detail
      *> group of one LINE PLUS 1 line; only the bracket differs (R7
      *> also omits LINES, the integer-1-alone arm of the brace).
      *> 13.18.39.4 GR3 a) HEADING 1, b) FIRST DETAIL 1, d) LAST DETAIL
      *>   "equal to the page limit given by integer-1", 3.
      *>   OK  13.18.39.4 3) b)  /  3) d)  (General rules)
      *> 13.18.35.4 GR5 b) 3.: the first body group on a page goes on
      *>   "the line ... given by the FIRST DETAIL integer", line 1.
      *>   OK  13.18.35.4 5) b) 3.  (General rules)
      *> 13.18.35.4 GR4: "If the trial sum is less than or equal to the
      *>   lower limit for the report group, the page fit is declared
      *>   successful"; 13.18.57.4 GR8 e) the lower limit of a detail is
      *>   the LAST DETAIL line, 3. Trials 2 and 3 fit; the fourth
      *>   GENERATE's trial 4 does not, so a page advance precedes it.
      *>   OK  13.18.35.4 4)  /  13.18.57.4 8) e)  (General rules)
      *> 8.4.3.15.4 GR2: PAGE-COUNTER starts at 1 and "its value is
      *>   updated by 1 during each page advance".
      *>   OK  8.4.3.15.4 2)  (General rules)
      *> Each line is "<report> <PAGE-COUNTER>/<LINE-COUNTER>" after one
      *> GENERATE, so every report prints 1/01 1/02 1/03 2/01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3751PG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P1 ASSIGN TO "PB3751P1.RPT".
           SELECT P2 ASSIGN TO "PB3751P2.RPT".
           SELECT P3 ASSIGN TO "PB3751P3.RPT".
           SELECT P4 ASSIGN TO "PB3751P4.RPT".
           SELECT P5 ASSIGN TO "PB3751P5.RPT".
           SELECT P6 ASSIGN TO "PB3751P6.RPT".
           SELECT P7 ASSIGN TO "PB3751P7.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD P1 REPORT IS R1.
       FD P2 REPORT IS R2.
       FD P3 REPORT IS R3.
       FD P4 REPORT IS R4.
       FD P5 REPORT IS R5.
       FD P6 REPORT IS R6.
       FD P7 REPORT IS R7.
       WORKING-STORAGE SECTION.
       01 PC PIC 9.
       01 LC PIC 99.
       REPORT SECTION.
       RD R1 PAGE 3 LINES.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R2 PAGE LIMIT 3 LINES.
       01 D2 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R3 PAGE LIMIT IS 3 LINES.
       01 D3 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R4 PAGE IS 3 LINES.
       01 D4 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R5 PAGE LIMITS 3 LINES.
       01 D5 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R6 PAGE LIMITS ARE 3 LINES.
       01 D6 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       RD R7 PAGE ARE 3.
       01 D7 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "D".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT P1 P2 P3 P4 P5 P6 P7.
           INITIATE R1 R2 R3 R4 R5 R6 R7.
           PERFORM G1 4 TIMES.
           PERFORM G2 4 TIMES.
           PERFORM G3 4 TIMES.
           PERFORM G4 4 TIMES.
           PERFORM G5 4 TIMES.
           PERFORM G6 4 TIMES.
           PERFORM G7 4 TIMES.
           TERMINATE R1 R2 R3 R4 R5 R6 R7.
           CLOSE P1 P2 P3 P4 P5 P6 P7.
           STOP RUN.
       G1.
           GENERATE D1.
           MOVE PAGE-COUNTER OF R1 TO PC.
           MOVE LINE-COUNTER OF R1 TO LC.
           DISPLAY "R1 " PC "/" LC.
       G2.
           GENERATE D2.
           MOVE PAGE-COUNTER OF R2 TO PC.
           MOVE LINE-COUNTER OF R2 TO LC.
           DISPLAY "R2 " PC "/" LC.
       G3.
           GENERATE D3.
           MOVE PAGE-COUNTER OF R3 TO PC.
           MOVE LINE-COUNTER OF R3 TO LC.
           DISPLAY "R3 " PC "/" LC.
       G4.
           GENERATE D4.
           MOVE PAGE-COUNTER OF R4 TO PC.
           MOVE LINE-COUNTER OF R4 TO LC.
           DISPLAY "R4 " PC "/" LC.
       G5.
           GENERATE D5.
           MOVE PAGE-COUNTER OF R5 TO PC.
           MOVE LINE-COUNTER OF R5 TO LC.
           DISPLAY "R5 " PC "/" LC.
       G6.
           GENERATE D6.
           MOVE PAGE-COUNTER OF R6 TO PC.
           MOVE LINE-COUNTER OF R6 TO LC.
           DISPLAY "R6 " PC "/" LC.
       G7.
           GENERATE D7.
           MOVE PAGE-COUNTER OF R7 TO PC.
           MOVE LINE-COUNTER OF R7 TO LC.
           DISPLAY "R7 " PC "/" LC.
