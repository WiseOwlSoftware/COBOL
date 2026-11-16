      *>--- conformance-case
      *> id: g85_x14
      *> rules: [1985-GR-IV.6.4.2.1.1-3, 1985-GR-IV.6.4.2.1.1.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR3 (1985-GR-IV.6.4.2.1.1-3) and
      *> §1985:IV.6.4.2.1.1.1 GR3 (1985-GR-IV.6.4.2.1.1.1-3). A
      *> refusal. 'A conditional statement is one of the following'
      *> lists a WRITE statement with the END-OF-PAGE phrase (a
      *> conditional phrase is one of the listed phrases), and an
      *> imperative statement 'is a conditional statement that is
      *> delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 22 of the program below writes a
      *> WRITE statement with the END-OF-PAGE phrase without its scope
      *> terminator as imperative-statement-1 of an inline PERFORM
      *> (§1985:VI.6.21.2), where only imperative statements may stand;
      *> that is the program's only fault. WiseOwl refuses line 22 at
      *> 85 with COBOL2796; a compiler that accepts the program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X14.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "G85X14P".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF LABEL RECORDS ARE OMITTED
           LINAGE IS 5 LINES WITH FOOTING AT 3.
       01  PREC PIC X(8).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON PRTF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               WRITE PREC AT END-OF-PAGE DISPLAY "W"
           END-PERFORM.
           STOP RUN.
