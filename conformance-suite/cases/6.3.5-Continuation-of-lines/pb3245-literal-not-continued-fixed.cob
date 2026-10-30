      *>--- conformance-case
      *> id: pb3245-literal-not-continued-fixed
      *> rules: [SR-6.3.5-1, FMT-8.3.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3245 - ISO 6.3.5 1): continuation of a literal is
      *>   indicated when "A line terminates within an alphanumeric or
      *>   boolean literal without a closing delimiter and the next
      *>   line that is not a comment line or a blank line contains a
      *>   fixed continuation indicator". The literal below ends its
      *>   line open and the next line carries no indicator, so it is
      *>   never continued and never closed (8.3.3.2.2 prints the
      *>   closing quotation symbol). Refused at every edition, named
      *>   by COBOL5563 rather than by an unnamed parse error at the
      *>   opening delimiter (it was COBOL0001 "unexpected '"'").
      *>   cite.py --check 6.3.5 "A line terminates within an
      *>     alphanumeric or boolean literal without a closing
      *>     delimiter" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3245FX.
       PROCEDURE DIVISION.
           DISPLAY "NEVER CLOSED
           STOP RUN.
