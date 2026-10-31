      *>--- conformance-case
      *> id: pb4176-copy-literal-not-closed
      *> rules: [SR-6.3.5-1, FMT-7.2.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4176 - ISO 6.3.5 1): a literal is continued when "A
      *> line terminates within an alphanumeric or boolean literal
      *> without a closing delimiter and the next line that is not a
      *> comment line or a blank line contains a fixed continuation
      *> indicator". The literal-1 of the COPY statement below ends its
      *> line open and the next line carries no indicator, so it is
      *> never continued and never closed. Expected: COBOL5563 naming
      *> the literal as not closed, at every edition (at COBOL-85, where
      *> literal-1 is not yet in the 7.2.3.2 format, COBOL0900 as well).
      *> It fails when the COPY operand scan calls the open literal "not
      *> an alphanumeric literal" (COBOL2450) and then refuses the next
      *> line's STOP as out of place in the statement (COBOL2449).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4176CPY.
       PROCEDURE DIVISION.
           COPY "ABC
           STOP RUN.
