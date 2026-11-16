      *>--- conformance-case
      *> id: g85_x22
      *> rules: [1985-GR-IV.6.4.2.1.1-5, 1985-GR-IV.6.4.2.1.1.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR5 (1985-GR-IV.6.4.2.1.1-5) and
      *> §1985:IV.6.4.2.1.1.1 GR4 (1985-GR-IV.6.4.2.1.1.1-4). A
      *> refusal. 'A conditional statement is one of the following'
      *> lists an ADD statement with the ON SIZE ERROR phrase (a
      *> conditional phrase is one of the listed phrases), and an
      *> imperative statement 'is a conditional statement that is
      *> delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 10 of the program below writes an
      *> ADD statement with the ON SIZE ERROR phrase without its scope
      *> terminator as imperative-statement-1 of an inline PERFORM
      *> (§1985:VI.6.21.2), where only imperative statements may stand;
      *> that is the program's only fault. WiseOwl refuses line 10 at
      *> 85 with COBOL2796; a compiler that accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X22.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               ADD 1 TO N ON SIZE ERROR DISPLAY "S"
           END-PERFORM.
           STOP RUN.
