      *>--- conformance-case
      *> id: pb5985-object-computer-reserved-word-name
      *> rules: [FMT-12.3.6.2, 1985-SR-VI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5985 - a reserved word written where OBJECT-COMPUTER's computer-name-1
      *> stands is no system-name (§1985:VI.4.4.3 SR1 "Computer-name is a system-name.";
      *> §1985:III.2: a reserved word "must not appear in the program as user-defined words or
      *> system-names"; §2023:8.3.2.1 SR1). It used to draw the parse layer's COBOL1970 "not a
      *> clause of the OBJECT-COMPUTER paragraph"; it is now COBOL7654, as a literal there is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5985OC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. GLOBAL.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
