*>--- conformance-case
*> id: wave_h_facilities_inert
*> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1, FMT-14.9.36.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> Wave H — the ISO §4.2.6 ¶3 recognize-and-name band. MCS (SEND/RECEIVE) and VALIDATE are
      *> facilities COBOL.NET does not implement: MCS is PROCESSOR-DEPENDENT (§4.2.6, Annex A.3 item 4) and
      *> VALIDATE is OPTIONAL (§4.2.7, A.4.14) and, at 2023, also OBSOLETE (§4.2.13, F.2 item 5). §4.2.6 ¶3
      *> nonetheless makes the compile-time WARNING MECHANISM mandatory, and §14.6.13.1.1 licenses raising
      *> no exception condition for them. COMMIT and ROLLBACK are PROVIDED since kb/Work PB3338; with no
      *> APPLY COMMIT clause active each has "the same effect as a CONTINUE statement" (§14.9.7.4 GR1,
      *> §14.9.36.4 GR1), so they draw no warning and the output is unchanged.
      *> This golden pins the resulting posture: the program COMPILES, RUNS, and the statements are INERT —
      *> the named warnings COBOLNET1578/1580 go to the non-failing compile channel (not stdout), so the
      *> observable behaviour is simply that the surrounding statements execute normally.
      *> Before Wave H each of these was a GENERIC parse error, which satisfied neither the warning
      *> obligation nor the project's never-a-silent-wrong-answer rule.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WAVEHINERT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG-TAG   PIC X(8)  VALUE "TAG1".
       01 MSG-BODY  PIC X(8)  VALUE "BODY".
       01 MSG-LEN   PIC 9(4)  VALUE 0.
       01 COUNTER   PIC 9     VALUE 0.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "START".
           COMMIT.
           ROLLBACK.
           VALIDATE MSG-BODY.
           RECEIVE FROM MSG-TAG GIVING MSG-BODY MSG-LEN
           END-RECEIVE.
           SEND TO MSG-TAG FROM MSG-BODY
           END-SEND.
      *> the facilities are inert, so ordinary control flow around them is unaffected
           PERFORM 3 TIMES
               ADD 1 TO COUNTER
           END-PERFORM.
           DISPLAY "COUNT=" COUNTER.
           DISPLAY "END".
           STOP RUN.
