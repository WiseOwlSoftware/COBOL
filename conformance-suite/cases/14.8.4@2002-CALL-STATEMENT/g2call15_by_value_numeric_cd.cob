      *>--- conformance-case
      *> id: g2call15_by_value_numeric_cd
      *> rules: [2002-SR-14.8.4.2-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.4.2 SR19 -- "If identifier-4 or its corresponding
      *> formal parameter is specified with a BY VALUE phrase,
      *> identifier-4 shall be of class numeric, object, or pointer."
      *> The accepted side of the rule: two numeric items of an input
      *> CD record (§2002:A.1.2.3 GR2: TEXT LENGTH is an unsigned
      *> integer of 4 digits, MESSAGE COUNT one of 6 digits) passed by
      *> value in a format 3 CALL. IN-LEN carries the phrase itself;
      *> IN-COUNT has no phrase, and its formal parameter is BY VALUE,
      *> so §2002:14.8.4.3 GR9b: "BY VALUE is assumed". By value, the
      *> callee's MOVE to its formal leaves the caller's item alone.
      *> Fails if either CALL is refused, if a value arrives wrong, or
      *> if IN-LEN changes after the call. The facility is obsolete at
      *> 2002 and draws the warning COBOL0903, which does not fail it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CAL15.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       M-P.
           MOVE 42 TO IN-LEN
           MOVE 7 TO IN-COUNT
           CALL "G2C15S" AS NESTED USING BY VALUE IN-LEN IN-COUNT
           DISPLAY "AFTER " IN-LEN " " IN-COUNT
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2C15S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-A PIC 9(4).
       01 L-B PIC 9(6).
       PROCEDURE DIVISION USING BY VALUE L-A L-B.
       S-P.
           DISPLAY "S " L-A " " L-B
           MOVE 99 TO L-A
           GOBACK.
       END PROGRAM G2C15S.
       END PROGRAM G2CAL15.
