      *>--- conformance-case
      *> id: g85_x06
      *> rules: [1985-GR-IV.6.4.2.1.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR1 (1985-GR-IV.6.4.2.1.1-1). A refusal. 'A
      *> conditional statement is one of the following' lists a SEARCH
      *> statement (a conditional phrase is one of the listed phrases),
      *> and an imperative statement 'is a conditional statement that
      *> is delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 12 of the program below writes a
      *> SEARCH statement without its scope terminator as
      *> imperative-statement-1 of an inline PERFORM (§1985:VI.6.21.2),
      *> where only imperative statements may stand; that is the
      *> program's only fault. WiseOwl refuses line 12 at 85 with
      *> COBOL2796; a compiler that accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  TE PIC X OCCURS 3 TIMES INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           SET IX TO 1.
           PERFORM
               SEARCH TE AT END DISPLAY "A"
                   WHEN TE (IX) = "Y" DISPLAY "Y"
           END-PERFORM.
           STOP RUN.
