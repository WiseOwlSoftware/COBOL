      *> reject-at: 2002 2014 2023
      *> ISO 8.4.3.3.3 SR1: identifier-1 may be "a group item that is neither a strongly-typed group nor a
      *> variable-length group" - a STRONG typed group is excluded. kb/Work PB70: every SR1 exclusion used to
      *> fall to a run-time NotImplemented (a sending ref-mod) or a silent drop (a receiving one); it is a
      *> bind-time rejection now, COBOLNET1647.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB70NSTRONG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ST-T TYPEDEF STRONG.
          05 SA PIC X(2).
          05 SB PIC 9(3).
       01 ST TYPE ST-T.
       01 R PIC X(4).
       PROCEDURE DIVISION.
           MOVE ST(1:2) TO R.
           STOP RUN.
