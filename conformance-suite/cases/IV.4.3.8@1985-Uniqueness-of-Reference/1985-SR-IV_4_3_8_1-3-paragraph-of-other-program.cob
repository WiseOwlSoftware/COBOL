      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_1-3-paragraph-of-other-program
      *> rules: [1985-SR-IV.4.3.8.1-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85. §1985:IV.4.3.8.1 SR3: "A paragraph-name or
      *> section-name appearing in a program may not be referenced
      *> from any other program" (cite.py --check OK). RR85XPI is
      *> contained in RR85XPG and PERFORMs PX, a paragraph of the
      *> containing program, so WiseOwl refuses it at 85 with
      *> COBOL1639 at that PERFORM; a compiler that accepts it
      *> fails (registrar re-read, 2026-11-15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85XPG.
       PROCEDURE DIVISION.
       PO.
           CALL "RR85XPI".
           STOP RUN.
       PX.
           DISPLAY "PX".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RR85XPI.
       PROCEDURE DIVISION.
       PI.
           PERFORM PX.
           EXIT PROGRAM.
       END PROGRAM RR85XPI.
       END PROGRAM RR85XPG.
