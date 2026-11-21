      *>--- conformance-case
      *> id: pb6008-return-in-unreached-section-after-85
      *> rules: [1985-SR-XI.4.3.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6008 - "A RETURN statement may only be used within
      *> the range of an output procedure associated with a SORT or
      *> MERGE statement for file-name-1" (§1985:XI.4.3.3 SR3). The
      *> output procedure O20 loops by GO TO and leaves by GO TO O20-X,
      *> its last paragraph, where "the return mechanism provides for
      *> termination of the merge" (§1985:XI.4.1.4 GR9). No path of its
      *> range reaches section O99, written after it, so the RETURN
      *> there is outside every range. Refused COBOL7081, as the same
      *> section written before O20 is; it was accepted while a GO TO's
      *> destination was taken to flow on to the end of the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6008RA.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "P6008IN1".
           SELECT IN2 ASSIGN TO "P6008IN2".
           SELECT SD-FILE ASSIGN TO "P6008WRK".
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  IN1-REC PIC X(4).
       FD  IN2.
       01  IN2-REC PIC X(4).
       SD  SD-FILE.
       01  SD-REC.
           05 SD-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       M-1.
           MERGE SD-FILE ON ASCENDING KEY SD-KEY
               USING IN1 IN2
               OUTPUT PROCEDURE IS O20.
           DISPLAY "DONE".
           STOP RUN.
       O20 SECTION.
       O20-1.
           RETURN SD-FILE AT END GO TO O20-X.
           DISPLAY "O20 " SD-KEY.
           GO TO O20-1.
       O20-X.
           EXIT.
       O99 SECTION.
       O99-1.
           RETURN SD-FILE AT END GO TO O99-X.
           DISPLAY "O99 " SD-KEY.
       O99-X.
           EXIT.
