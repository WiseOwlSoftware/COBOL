      *>--- conformance-case
      *> id: g2dsp02_upon_undeclared_mnemonic
      *> rules: [2002-SR-14.8.10.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.10.2 SR2 -- "Mnemonic-name-1 shall be associated
      *> with a hardware device in the SPECIAL-NAMES paragraph in the
      *> environment division."
      *> NO-SUCH-DEV is named in no SPECIAL-NAMES entry, so it is
      *> associated with no hardware device: the DISPLAY is refused
      *> at its line. The accepted half is g2dsp01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSP2.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           SWITCH-1 IS SW-MN ON STATUS IS SW1-ON
           C01 IS TOP-MN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X" UPON NO-SUCH-DEV
           STOP RUN.
