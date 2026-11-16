      *>--- conformance-case
      *> id: g2dsp03_upon_switch_mnemonic
      *> rules: [2002-SR-14.8.10.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.10.2 SR2 -- "Mnemonic-name-1 shall be associated
      *> with a hardware device in the SPECIAL-NAMES paragraph in the
      *> environment division."
      *> SW-MN is the mnemonic-name of switch-name SWITCH-1, which is an
      *> external switch (§2002:12.2.6.3 GR2), not a hardware device;
      *> §2002:12.2.6.2 SR5 also limits mnemonic-name-1 to the SET
      *> statement. The DISPLAY is refused at its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSP3.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           SWITCH-1 IS SW-MN ON STATUS IS SW1-ON
           C01 IS TOP-MN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X" UPON SW-MN
           STOP RUN.
