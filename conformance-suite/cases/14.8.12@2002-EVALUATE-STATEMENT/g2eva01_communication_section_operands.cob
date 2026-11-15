      *>--- conformance-case
      *> id: g2eva01_communication_section_operands
      *> rules: [2002-SR-14.8.12.2-8, 2002-GR-14.8.12.3-3,
      *>    2002-GR-14.8.12.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> EVALUATE whose selection subjects and objects are items of the
      *> COMMUNICATION SECTION: implicit CD data-names TL (TEXT LENGTH,
      *> "an integer of 4 digits, without an operational sign", and SD1
      *> (SYMBOLIC DESTINATION, "an elementary alphanumeric data item of
      *> 12 characters"), §2002:A.1.2.3 GR16, and the input-CD record
      *> item MC PIC 9(6). TL=3, SD1="AB", MC=7, W=3 (working storage).
      *> §2002:14.8.12.3 GR3 assigns each subject: a) an identifier "is
      *> assigned the value and class of the data item"; b) a literal
      *> its own; c) an arithmetic expression a numeric value; e) a
      *> condition a truth value; f) TRUE/FALSE a truth value.
      *> §2002:14.8.12.2 SR8: a partial-expression object "is treated as
      *> though it were specified as condition-2" built by preceding it
      *> with the subject; the subject is treated as TRUE (SR5 lists the
      *> partial forms). §2002:14.8.12.3 GR4 a) 1-6 analyse each pair;
      *> b) the first all-true WHEN is taken; c) a false pair ends that
      *> WHEN.
      *> T1 TL WHEN 3.0: a) numeric class, 3 = 3.0      T1 NUMERIC-EQ
      *> T2 TL WHEN > 5 (SR8: TL > 5 false) WHEN 1 THRU 2 (a5: false)
      *>    WHEN NOT 1 THRU 2 (TL < 1 OR TL > 2 true)   T2 NOT1-2
      *> T3 TL ALSO SD1: ANY ALSO "AC" (a1 true, SD1 = "AC" false);
      *>    IS POSITIVE ALSO "AB" (SR8 TL IS POSITIVE, §2002:8.8.4.1.6.2
      *>    GR1 "greater than zero"; SD1 = "AB" space-padded) T3 POS-AB
      *> T4 SD1 WHEN IS NUMERIC (§2002:8.8.4.1.3.3 GR2a: not all
      *>    digits, false) WHEN IS NOT NUMERIC (true)    T4 NOT-NUMERIC
      *> T5 TRUE ALSO MC: TL > W ALSO 7 (a3: 3 > 3 false, c: stop);
      *>    TL = W ALSO MC (true, MC = MC)              T5 TL=W MC
      *> T6 MC + TL WHEN 10: c) numeric 10              T6 SUM10
      *> T7 TL > 2 ALSO FALSE WHEN TRUE ALSO TL = 0: e) true matches
      *>    TRUE (a4); f) FALSE matches the false TL = 0 (a3) T7 COND
      *> T8 W WHEN TL THRU MC: a5 3 >= 3 AND 3 <= 7     T8 RANGE-CD
      *> T9 literal 3 WHEN TL: b) literal subject, a6   T9 LIT-SUBJ
      *> Each line fails if a CD operand loses its class (TL compared as
      *> "0003" with "3.0"), a partial expression is not spliced onto
      *> the subject, or pairs are mis-analysed. COBOL0903 (obsolete
      *> communication facility, §2002:G.2) is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2E1M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9(4) VALUE 3.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       CD CDIN FOR INPUT.
       01 CDREC.
          02 QN PIC X(12).
          02 FILLER PIC X(69).
          02 MC PIC 9(6).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 3 TO TL
           MOVE "AB" TO SD1
           MOVE 7 TO MC
           EVALUATE TL
             WHEN 3.0 DISPLAY "T1 NUMERIC-EQ"
             WHEN OTHER DISPLAY "T1 OTHER"
           END-EVALUATE
           EVALUATE TL
             WHEN > 5 DISPLAY "T2 GT5"
             WHEN 1 THRU 2 DISPLAY "T2 1-2"
             WHEN NOT 1 THRU 2 DISPLAY "T2 NOT1-2"
           END-EVALUATE
           EVALUATE TL ALSO SD1
             WHEN ANY ALSO "AC" DISPLAY "T3 AC"
             WHEN IS POSITIVE ALSO "AB" DISPLAY "T3 POS-AB"
             WHEN OTHER DISPLAY "T3 OTHER"
           END-EVALUATE
           EVALUATE SD1
             WHEN IS NUMERIC DISPLAY "T4 NUMERIC"
             WHEN IS NOT NUMERIC DISPLAY "T4 NOT-NUMERIC"
             WHEN OTHER DISPLAY "T4 OTHER"
           END-EVALUATE
           EVALUATE TRUE ALSO MC
             WHEN TL > W ALSO 7 DISPLAY "T5 WRONG"
             WHEN TL = W ALSO MC DISPLAY "T5 TL=W MC"
             WHEN OTHER DISPLAY "T5 OTHER"
           END-EVALUATE
           EVALUATE MC + TL
             WHEN 10 DISPLAY "T6 SUM10"
             WHEN OTHER DISPLAY "T6 OTHER"
           END-EVALUATE
           EVALUATE TL > 2 ALSO FALSE
             WHEN TRUE ALSO TL = 0 DISPLAY "T7 COND"
             WHEN OTHER DISPLAY "T7 OTHER"
           END-EVALUATE
           EVALUATE W
             WHEN TL THRU MC DISPLAY "T8 RANGE-CD"
             WHEN OTHER DISPLAY "T8 OTHER"
           END-EVALUATE
           EVALUATE 3
             WHEN TL DISPLAY "T9 LIT-SUBJ"
             WHEN OTHER DISPLAY "T9 OTHER"
           END-EVALUATE
           STOP RUN.
