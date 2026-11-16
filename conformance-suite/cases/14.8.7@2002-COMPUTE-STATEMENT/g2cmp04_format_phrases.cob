      *>--- conformance-case
      *> id: g2cmp04_format_phrases
      *> rules: [2002-FMT-14.8.7.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.7.1 -- Format 1 "COMPUTE { identifier-1
      *> [ ROUNDED ] } ... = arithmetic-expression-1", the choice-
      *> bracketed "ON SIZE ERROR imperative-statement-1" and NOT ON
      *> SIZE ERROR (zero, either or both, in either order) and the
      *> optional END-COMPUTE; Format 2 "COMPUTE { identifier-2 } ...
      *> = boolean-expression-1 [ END-COMPUTE ]". Every form is
      *> written and run; native arithmetic is in effect (no ARITHMETIC
      *> clause, §2002:11.8.4.2 GR3).
      *> Values: ROUNDED, §2002:14.6.3 ("increased by one in the
      *> low-order position whenever the most significant digit of the
      *> excess is greater than or equal to five"), else truncation;
      *> size error, §2002:14.6.4 (rule 1: "the values of all of the
      *> resultant data items remain unchanged from the values they
      *> had at the start of the execution"; rule 2: only the
      *> overflowing receiver is unchanged) and §2002:14.6.6 4b
      *> ("only that data item remains unchanged and processing
      *> proceeds to the next resulting data item to the right").
      *>  F1 X ROUNDED Y = 7 / 2 (3.5): X rounded 04, Y truncated 03,
      *>     ON then NOT ON, no size error   -> F1 NSE 04 03
      *>  F2 X ROUNDED Y ROUNDED = 9.5, no phrase -> F2 10 10
      *>  F3 Z (PIC 9, holds 7) = 50, NOT ON before ON: size error,
      *>     Z unchanged                     -> F3 SE 7
      *>  F4 Z = 5, NOT ON alone             -> F4 NSE 5
      *>  F5 Z = 12, ON alone, ended by the period -> F5 SE 5
      *>  F6 X ROUNDED Z ROUNDED = 9.6, ON alone: X = 10 stored,
      *>     Z (10 does not fit) alone unchanged -> F6 SE 10 5
      *>  F7 format 2, two receivers, END-COMPUTE: B-NOT 1100 = 0011,
      *>     B-OR 0001 = 0011 (§2002:8.8.2) -> F7 0011 0011
      *>  F8 format 2 ended by the period: 1100 B-XOR 1010 = 0110
      *>                                     -> F8 0110
      *> Fails if any of the forms is refused, a ROUNDED is applied to
      *> the wrong receiver, or a phrase runs on the wrong outcome.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CMP4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC 99.
       01 Y PIC 99.
       01 Z PIC 9 VALUE 7.
       01 B1 PIC 1(4) VALUE B"1100".
       01 B2 PIC 1(4).
       01 B3 PIC 1(4).
       PROCEDURE DIVISION.
       MAIN.
           COMPUTE X ROUNDED Y = 7 / 2
               ON SIZE ERROR DISPLAY "F1 SE"
               NOT ON SIZE ERROR DISPLAY "F1 NSE " X " " Y
           END-COMPUTE
           COMPUTE X ROUNDED Y ROUNDED = 9.5 END-COMPUTE
           DISPLAY "F2 " X " " Y
           COMPUTE Z = 50
               NOT ON SIZE ERROR DISPLAY "F3 NSE " Z
               ON SIZE ERROR DISPLAY "F3 SE " Z
           END-COMPUTE
           COMPUTE Z = 5
               NOT ON SIZE ERROR DISPLAY "F4 NSE " Z
           END-COMPUTE
           COMPUTE Z = 12
               ON SIZE ERROR DISPLAY "F5 SE " Z.
           COMPUTE X ROUNDED Z ROUNDED = 9.6
               ON SIZE ERROR DISPLAY "F6 SE " X " " Z
           END-COMPUTE
           COMPUTE B2 B3 = B-NOT B1 B-OR B"0001" END-COMPUTE
           DISPLAY "F7 " B2 " " B3
           COMPUTE B2 = B1 B-XOR B"1010".
           DISPLAY "F8 " B2
           STOP RUN.
