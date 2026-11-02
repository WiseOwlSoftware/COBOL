      *>--- conformance-case
      *> id: pb4229-procedure-name-reserved-at-every-edition
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4229 - 8.3.2.1 1): "Reserved words shall not be used
      *> as user-defined words or system-names." A procedure-name is a
      *> user-defined word (8.3.2.2), and 8.9 reserves READ at every
      *> edition, so PERFORM READ names a procedure with a reserved
      *> word. Expected: COBOL0901 naming READ, and nothing else (the
      *> parse fails, so no bind-time rule is asked). It fails when the
      *> parse reads READ as the READ verb of an inline PERFORM and the
      *> error falls at the period ("unexpected '.'"), never naming the
      *> word. The reservation-gated twin is
      *> pb4206-perform-reserved-verb-as-procedure-name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4229PFM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM READ.
           STOP RUN.
