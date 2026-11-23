      *>--- conformance-case
      *> id: g2-exception-statement-31
      *> rules: [2002-RV-15.27.2-1, 2002-RV-15.27.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.27.2 RV1 - "the returned value is 31 spaces" (no
      *> exception raised, so no location was saved); RV2 - "the returned
      *> value is a 31-character alphanumeric character-string that is
      *> the name of the statement that caused the exception condition to
      *> be raised in uppercase letters, left-justified and
      *> space-filled on the right" (the TURN directive carries
      *> LOCATION). §2014:15.29.2 RV1/RV2 say the same 31. The harness
      *> trims trailing spaces, so the width is shown through FUNCTION
      *> LENGTH and a closing bracket after the function's value.
      *>   N-TL  RV1, before any exception  -> 31
      *>   A-TL  RV2, after the STRING overflow -> 31
      *>   A-T   the name STRING, then spaces to column 31, then "]"
       >>TURN EC-OVERFLOW-STRING CHECKING ON WITH LOCATION
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2-EXC-STMT-31.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-DST PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "N-TL=" FUNCTION LENGTH(FUNCTION EXCEPTION-STATEMENT).
           STRING "ABCDEFG" DELIMITED BY SIZE INTO WS-DST
               ON OVERFLOW CONTINUE
           END-STRING.
           DISPLAY "A-TL=" FUNCTION LENGTH(FUNCTION EXCEPTION-STATEMENT).
           DISPLAY "A-T=[" FUNCTION EXCEPTION-STATEMENT "]".
           STOP RUN.
