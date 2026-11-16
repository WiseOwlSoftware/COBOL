*>--- conformance-case
*> id: wave_h_facilities_inert
*> rules: [GR-14.9.7.4-1, GR-14.9.36.4-1, FMT-14.9.36.2]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> Wave H — facility statements that change nothing the program can see here. With no APPLY COMMIT clause
      *> active COMMIT and ROLLBACK each have "the same effect as a CONTINUE statement" (§2023:14.9.7.4 GR1,
      *> §2023:14.9.36.4 GR1); VALIDATE of an alphanumeric item has no stage to take part in (PICTURE X accepts any content,
      *> §2023:14.9.50.4 GR6 a) and no exception checking is on, so it changes nothing (GR5); the messaging statements
      *> are the 2023 facility (kb/Work PB4744). The surrounding statements execute normally.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WAVEHINERT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MSG-TAG   USAGE MESSAGE-TAG.
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
           SEND TO MSG-SERVER FROM MSG-BODY RETURNING MSG-TAG
           END-SEND.
      *> the facilities are inert, so ordinary control flow around them is unaffected
           PERFORM 3 TIMES
               ADD 1 TO COUNTER
           END-PERFORM.
           DISPLAY "COUNT=" COUNTER.
           DISPLAY "END".
           STOP RUN.
