      *>--- conformance-case
      *> id: g85_x08
      *> rules: [1985-GR-IV.6.4.2.1.1-2, 1985-GR-IV.6.4.2.1.1.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR2 (1985-GR-IV.6.4.2.1.1-2) and
      *> §1985:IV.6.4.2.1.1.1 GR1 (1985-GR-IV.6.4.2.1.1.1-1). A
      *> refusal. 'A conditional statement is one of the following'
      *> lists a READ statement with the AT END phrase (a conditional
      *> phrase is one of the listed phrases), and an imperative
      *> statement 'is a conditional statement that is delimited by its
      *> explicit scope terminator' (§1985:IV.6.4.2.3.1). Line 21 of
      *> the program below writes a READ statement with the AT END
      *> phrase without its scope terminator as imperative-statement-1
      *> of an inline PERFORM (§1985:VI.6.21.2), where only imperative
      *> statements may stand; that is the program's only fault.
      *> WiseOwl refuses line 21 at 85 with COBOL2796; a compiler that
      *> accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X08.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "G85X08S".
       DATA DIVISION.
       FILE SECTION.
       FD  SF LABEL RECORDS ARE STANDARD.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               READ SF AT END DISPLAY "R"
           END-PERFORM.
           STOP RUN.
