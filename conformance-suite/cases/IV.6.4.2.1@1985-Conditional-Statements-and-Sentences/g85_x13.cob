      *>--- conformance-case
      *> id: g85_x13
      *> rules: [1985-GR-IV.6.4.2.1.1-3, 1985-GR-IV.6.4.2.1.1.1-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR3 (1985-GR-IV.6.4.2.1.1-3) and
      *> §1985:IV.6.4.2.1.1.1 GR2 (1985-GR-IV.6.4.2.1.1.1-2). A
      *> refusal. 'A conditional statement is one of the following'
      *> lists a WRITE statement with the NOT INVALID KEY phrase (a
      *> conditional phrase is one of the listed phrases), and an
      *> imperative statement 'is a conditional statement that is
      *> delimited by its explicit scope terminator'
      *> (§1985:IV.6.4.2.3.1). Line 26 of the program below writes a
      *> WRITE statement with the NOT INVALID KEY phrase without its
      *> scope terminator as imperative-statement-1 of an inline
      *> PERFORM (§1985:VI.6.21.2), where only imperative statements
      *> may stand; that is the program's only fault. WiseOwl refuses
      *> line 26 at 85 with COBOL2796; a compiler that accepts the
      *> program fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X13.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "G85X13X"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS XKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  XF LABEL RECORDS ARE STANDARD.
       01  XREC.
           05  XKEY PIC X(4).
           05  XDAT PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON XF.
       D-PARA.
           DISPLAY "DECLARATIVE".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       P0.
           PERFORM
               WRITE XREC NOT INVALID KEY DISPLAY "W"
           END-PERFORM.
           STOP RUN.
