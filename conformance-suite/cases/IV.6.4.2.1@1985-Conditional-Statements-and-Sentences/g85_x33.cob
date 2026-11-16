      *>--- conformance-case
      *> id: g85_x33
      *> rules: [1985-GR-IV.6.4.2.1.1-7, 1985-GR-IV.6.4.2.1.1.1-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR7 (1985-GR-IV.6.4.2.1.1-7) and
      *> §1985:IV.6.4.2.1.1.1 GR6 (1985-GR-IV.6.4.2.1.1.1-6). A
      *> refusal. 'A conditional statement is one of the following'
      *> lists a STRING statement with the NOT ON OVERFLOW phrase (a
      *> conditional phrase is one of the listed phrases), and an
      *> imperative statement 'is a conditional statement that is
      *> delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 11 of the program below writes a
      *> STRING statement with the NOT ON OVERFLOW phrase without its
      *> scope terminator as imperative-statement-1 of an inline
      *> PERFORM (§1985:VI.6.21.2), where only imperative statements
      *> may stand; that is the program's only fault. WiseOwl refuses
      *> line 11 at 85 with COBOL2796; a compiler that accepts the
      *> program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X33.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S1 PIC X(4) VALUE "ABCD".
       01  S2 PIC X(2).
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               STRING S1 DELIMITED BY SIZE INTO S2
                   NOT ON OVERFLOW DISPLAY "O"
           END-PERFORM.
           STOP RUN.
