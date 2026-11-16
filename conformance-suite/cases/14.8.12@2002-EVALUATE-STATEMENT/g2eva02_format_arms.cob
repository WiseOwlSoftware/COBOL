      *>--- conformance-case
      *> id: g2eva02_format_arms
      *> rules: [2002-FMT-14.8.12.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.1 - the selection-subject arms (identifier,
      *> literal, arithmetic-expression, boolean-expression, condition,
      *> TRUE, FALSE; ALSO), the selection-object arms ([NOT]
      *> identifier, literal, arithmetic-expression, boolean-expression
      *> and range-expression, condition, partial-expression, TRUE,
      *> FALSE, ANY), range-expression with THROUGH/THRU, arithmetic
      *> bounds and IN alphabet-name-1, several WHEN phrases sharing one
      *> imperative, "WHEN OTHER imperative-statement-2 END-EVALUATE",
      *> and an EVALUATE with no WHEN OTHER. N = 7, C = "G", B1 = 1010,
      *> B2 = 0101. §2002:14.8.12.3 GR3 assigns each subject; GR4 a)
      *> 1-6 analyse each pair ("If the selection object is the word
      *> ANY, the result is true"; for a condition or TRUE/FALSE "If the
      *> truth value of the selection subject and selection object
      *> match, the result of the analysis is true"; a partial
      *> expression has "the selection subject is placed to the left of
      *> the leading relational operator"; a range is subject >= left
      *> AND subject <= right, or with NOT subject < left OR subject >
      *> right; otherwise subject [NOT] = object); b) the first
      *> all-true WHEN is selected, c) a false pair ends that WHEN; GR5
      *> a)-c) run its imperative, else WHEN OTHER, else nothing.
      *>  S1 N ALSO C ALSO N + 1: WHEN 7 ALSO "B" ALSO 8 fails on "B";
      *>     NOT 3 ALSO NOT "B" ALSO NOT N: 7 <> 3, G <> B, 8 <> 7
      *>                                        -> S1 NOT-OBJECTS
      *>  S2 C WHEN "A" THRU "M" IN MY-ALPHA (NATIVE)  -> S2 RANGE-IN
      *>  S3 C WHEN NOT "A" THROUGH "C": G > C         -> S3 NOT-RANGE
      *>  S4 N WHEN > 9 (false) WHEN > 5 (true)        -> S4 PARTIAL
      *>  S5 TRUE ALSO FALSE WHEN N > 5 ALSO N < 5     -> S5 TRUE-FALSE
      *>  S6 N > 5 WHEN FALSE (no match) WHEN TRUE     -> S6 COND-TRUE
      *>  S7 B1 B-AND B2 = 0000 WHEN B"0000"           -> S7 BOOL-SUBJ
      *>  S8 B1 WHEN NOT B2 B-OR B"0000": 1010 <> 0101 -> S8 NOT-BOOL
      *>  S9 N ALSO C WHEN ANY ALSO "G"                -> S9 ANY
      *>  S10 literal subject 7 WHEN N                 -> S10 LIT-SUBJ
      *>  S11 N WHEN 1 WHEN 7 (one imperative)         -> S11 TWO-WHEN
      *>  S12 N WHEN NOT N * 1 (7 = 7, false) WHEN N * 1
      *>                                               -> S12 ARITH-OBJ
      *>  S13 N WHEN N - 2 THRU N + 2 (5 .. 9)         -> S13 ARITH-RANGE
      *>  S14 N WHEN 1 only: no WHEN selected, no WHEN OTHER, so nothing
      *>      runs and the next statement prints       -> S14 NONE
      *> Each line fails if an arm is parsed into another shape (a NOT
      *> applied to the wrong operand, a partial expression not spliced
      *> onto the subject) or a pair is mis-analysed; every EVALUATE but
      *> S14 has a WHEN OTHER that prints "OTHER".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVA02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET MY-ALPHA IS NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N   PIC 99 VALUE 7.
       01 C   PIC X  VALUE "G".
       01 B1  PIC 1(4) USAGE BIT VALUE B"1010".
       01 B2  PIC 1(4) USAGE BIT VALUE B"0101".
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE N ALSO C ALSO N + 1
             WHEN 7 ALSO "B" ALSO 8 DISPLAY "S1 WRONG"
             WHEN NOT 3 ALSO NOT "B" ALSO NOT N
               DISPLAY "S1 NOT-OBJECTS"
             WHEN OTHER DISPLAY "S1 OTHER"
           END-EVALUATE
           EVALUATE C
             WHEN "A" THRU "M" IN MY-ALPHA DISPLAY "S2 RANGE-IN"
             WHEN OTHER DISPLAY "S2 OTHER"
           END-EVALUATE
           EVALUATE C
             WHEN NOT "A" THROUGH "C" DISPLAY "S3 NOT-RANGE"
             WHEN OTHER DISPLAY "S3 OTHER"
           END-EVALUATE
           EVALUATE N
             WHEN > 9 DISPLAY "S4 WRONG"
             WHEN > 5 DISPLAY "S4 PARTIAL"
             WHEN OTHER DISPLAY "S4 OTHER"
           END-EVALUATE
           EVALUATE TRUE ALSO FALSE
             WHEN N > 5 ALSO N < 5 DISPLAY "S5 TRUE-FALSE"
             WHEN OTHER DISPLAY "S5 OTHER"
           END-EVALUATE
           EVALUATE N > 5
             WHEN FALSE DISPLAY "S6 WRONG"
             WHEN TRUE DISPLAY "S6 COND-TRUE"
             WHEN OTHER DISPLAY "S6 OTHER"
           END-EVALUATE
           EVALUATE B1 B-AND B2
             WHEN B"0000" DISPLAY "S7 BOOL-SUBJ"
             WHEN OTHER DISPLAY "S7 OTHER"
           END-EVALUATE
           EVALUATE B1
             WHEN NOT B2 B-OR B"0000" DISPLAY "S8 NOT-BOOL"
             WHEN OTHER DISPLAY "S8 OTHER"
           END-EVALUATE
           EVALUATE N ALSO C
             WHEN ANY ALSO "G" DISPLAY "S9 ANY"
             WHEN OTHER DISPLAY "S9 OTHER"
           END-EVALUATE
           EVALUATE 7
             WHEN N DISPLAY "S10 LIT-SUBJ"
             WHEN OTHER DISPLAY "S10 OTHER"
           END-EVALUATE
           EVALUATE N
             WHEN 1 WHEN 7 DISPLAY "S11 TWO-WHEN"
             WHEN OTHER DISPLAY "S11 OTHER"
           END-EVALUATE
           EVALUATE N
             WHEN NOT N * 1 DISPLAY "S12 WRONG"
             WHEN N * 1 DISPLAY "S12 ARITH-OBJ"
             WHEN OTHER DISPLAY "S12 OTHER"
           END-EVALUATE
           EVALUATE N
             WHEN N - 2 THRU N + 2 DISPLAY "S13 ARITH-RANGE"
             WHEN OTHER DISPLAY "S13 OTHER"
           END-EVALUATE
           EVALUATE N
             WHEN 1 DISPLAY "S14 WRONG"
           END-EVALUATE
           DISPLAY "S14 NONE"
           STOP RUN.
       END PROGRAM G2EVA02.
