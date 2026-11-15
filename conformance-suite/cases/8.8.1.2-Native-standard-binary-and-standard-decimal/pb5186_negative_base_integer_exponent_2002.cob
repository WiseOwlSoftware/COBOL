      *>--- conformance-case
      *> id: pb5186_negative_base_integer_exponent_2002
      *> rules: [GR-8.8.1.2-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5186. ISO 2023 8.8.1.2 GR6c (the 2002 text, 8.8.1.1,
      *> the same): "If the value of an expression to be raised to a
      *> power is less than zero, the evaluation of the exponent shall
      *> result in an integer. Otherwise, the EC-SIZE-EXPONENTIATION
      *> exception condition is set to exist and the size error
      *> condition is raised" - whether or not a real result exists.
      *> The COBOL-85 rule, a size error only when no real number is
      *> the result, is pb5186_negative_base_real_root_85.
      *> Every non-integer exponent below takes the phrase and leaves
      *> R unchanged; an integer exponent written 1.0 is -32.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5186IE02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NB   PIC S99 VALUE -32.
       01  N40  PIC S99 VALUE -40.
       01  EX   PIC S9V9 VALUE 0.4.
       01  R    PIC S99V99 VALUE 77.
       01  RE   PIC -99.99.
       PROCEDURE DIVISION.
       P1.
           COMPUTE R ROUNDED = NB ** 0.2
               ON SIZE ERROR DISPLAY "SIZE ERROR 1"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = NB ** EX
               ON SIZE ERROR DISPLAY "SIZE ERROR 2"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = FUNCTION MAX (NB N40) ** 0.6
               ON SIZE ERROR DISPLAY "SIZE ERROR 3"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           COMPUTE R ROUNDED = NB ** 1.0
               ON SIZE ERROR DISPLAY "SIZE ERROR 4"
           END-COMPUTE.
           MOVE R TO RE. DISPLAY RE.
           STOP RUN.
