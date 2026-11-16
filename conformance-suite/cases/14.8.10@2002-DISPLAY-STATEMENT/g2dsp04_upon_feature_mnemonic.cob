      *>--- conformance-case
      *> id: g2dsp04_upon_feature_mnemonic
      *> rules: [2002-SR-14.8.10.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.10.2 SR2 -- "Mnemonic-name-1 shall be associated
      *> with a hardware device in the SPECIAL-NAMES paragraph in the
      *> environment division."
      *> TOP-MN is the mnemonic-name of feature-name C01, not of a
      *> device-name; §2002:12.2.6.2 SR6 also limits mnemonic-name-2
      *> to the SEND and WRITE statements. The DISPLAY is refused at
      *> its line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DSP4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           SWITCH-1 IS SW-MN ON STATUS IS SW1-ON
           C01 IS TOP-MN.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "X" UPON TOP-MN
           STOP RUN.
