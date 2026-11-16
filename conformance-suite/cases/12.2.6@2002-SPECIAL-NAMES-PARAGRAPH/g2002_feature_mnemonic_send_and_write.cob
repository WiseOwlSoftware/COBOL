      *>--- conformance-case
      *> id: g2002_feature_mnemonic_send_and_write
      *> rules: [2002-SR-12.2.6.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR6 - "Mnemonic-name-2 may be specified only in
      *> the SEND and WRITE statements." Mnemonic-name-2 is the name a
      *> feature-name entry (here C01 IS TOP-PAGE) gives a feature
      *> (§2002:12.2.6.1). The rule grants both statements, so both are
      *> written and must compile: WRITE ... AFTER ADVANCING TOP-PAGE,
      *> which runs, and SEND ... BEFORE ADVANCING TOP-PAGE on an output
      *> CD (the communication facility, §2002:A.2.6), which follows the
      *> STOP RUN so that no message control system is needed: SR6 is a
      *> syntax rule, and the SEND leg fails if the compiler refuses the
      *> feature mnemonic there. The CD draws the obsolete warning.
      *> Other statements are refused at 2002 by the cases
      *> g2002-feature-mnemonic-in-set and g2002-feature-mnemonic-in-display.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN11.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           C01 IS TOP-PAGE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFX ASSIGN TO "G2SPN11.TXT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  PFX.
       01  PR PIC X(5).
       WORKING-STORAGE SECTION.
       01  MSG PIC X(10) VALUE "HELLO".
       01  FS  PIC XX.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT PFX.
           MOVE "LINE1" TO PR.
           WRITE PR AFTER ADVANCING TOP-PAGE.
           CLOSE PFX.
           DISPLAY "WRITE-OK".
           STOP RUN.
       P-SEND.
           SEND OUT-CD FROM MSG WITH EMI
               BEFORE ADVANCING TOP-PAGE.
