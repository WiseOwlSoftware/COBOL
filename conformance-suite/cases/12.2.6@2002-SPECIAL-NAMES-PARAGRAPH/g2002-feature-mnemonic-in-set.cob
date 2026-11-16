      *>--- conformance-case
      *> id: g2002-feature-mnemonic-in-set
      *> rules: [2002-SR-12.2.6.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR6 - "Mnemonic-name-2 may be specified only in
      *> the SEND and WRITE statements." TOP-PAGE names the feature C01,
      *> so naming it in a SET statement violates SR6 and is refused.
      *> The permitted SEND and WRITE are accepted by
      *> g2002_feature_mnemonic_send_and_write.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN12.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           C01 IS TOP-PAGE.
       PROCEDURE DIVISION.
           SET TOP-PAGE TO ON.
           STOP RUN.
