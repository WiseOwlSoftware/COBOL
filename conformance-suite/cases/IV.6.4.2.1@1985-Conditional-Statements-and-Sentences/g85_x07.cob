      *>--- conformance-case
      *> id: g85_x07
      *> rules: [1985-GR-IV.6.4.2.1.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR1 (1985-GR-IV.6.4.2.1.1-1). A refusal. 'A
      *> conditional statement is one of the following' lists a RETURN
      *> statement (a conditional phrase is one of the listed phrases),
      *> and an imperative statement 'is a conditional statement that
      *> is delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 25 of the program below writes a
      *> RETURN statement without its scope terminator as
      *> imperative-statement-1 of an inline PERFORM (§1985:VI.6.21.2),
      *> where only imperative statements may stand; that is the
      *> program's only fault. WiseOwl refuses line 25 at 85 with
      *> COBOL2796; a compiler that accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WK ASSIGN TO "G85X07W".
       DATA DIVISION.
       FILE SECTION.
       SD  WK.
       01  WREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           SORT WK ON ASCENDING KEY WREC
               INPUT PROCEDURE IS IN-SEC
               OUTPUT PROCEDURE IS OUT-SEC.
           STOP RUN.
       IN-SEC SECTION.
       I0.
           MOVE "AAAA" TO WREC.
           RELEASE WREC.
       OUT-SEC SECTION.
       O0.
           PERFORM
               RETURN WK AT END DISPLAY "A"
           END-PERFORM.
