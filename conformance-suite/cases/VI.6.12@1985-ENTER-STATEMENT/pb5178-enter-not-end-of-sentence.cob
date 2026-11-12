      *>--- conformance-case
      *> id: pb5178-enter-not-end-of-sentence
      *> rules: [1985-FMT-VI.6.12.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * X3.23-1985 VI 6.12.2 prints the separator period after the
      * ENTER statement, and IV 2.1.6: "The separator period, when
      * used in the formats, has the status of a required word." The
      * DISPLAY below stands where the period is required; the
      * sentence compiled and ran (kb/Work PB5178). It is refused
      * with COBOL6973.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5178N.
       PROCEDURE DIVISION.
       P1.
           ENTER COBOL DISPLAY "SAME SENTENCE".
           STOP RUN.
