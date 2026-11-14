      *>--- conformance-case
      *> id: 1985-FMT-VI_6_12_2
      *> rules: [1985-FMT-VI.6.12.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.12.2 (THE ENTER STATEMENT). A refusal: an implementation
      *> "may include the ENTER statement or not, at the option of the
      *> implementor" (§1985:I.1.5.2.5.5), and WiseOwl does not provide it, so
      *> even the return word of the format, ENTER COBOL, is refused at 85
      *> with COBOL7240 (kb/Work PB5308); a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ENTTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "BEFORE".
           ENTER COBOL.
           DISPLAY "AFTER".
           STOP RUN.
