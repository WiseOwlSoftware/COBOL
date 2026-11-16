      *>--- conformance-case
      *> id: g2spn02_send_advancing_device_mnemonic
      *> rules: [2002-SR-12.2.6.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR7 - "Mnemonic-name-3 may be specified only in
      *> the ACCEPT and DISPLAY statements." At 2002 the SEND statement
      *> (§2002:A.2.6.1 Format 2) takes ADVANCING mnemonic-name-1, and
      *> §2002:A.2.6.2 SR5 requires that name to be associated with a
      *> feature-name; OUT-DEV is a mnemonic-name-3 (SYSOUT IS OUT-DEV,
      *> a device-name), so SEND ... BEFORE ADVANCING OUT-DEV (line 39)
      *> breaks SR7 and is refused there.
      *>   cite.py --check "§2002:12.2.6.2 SR7" "Mnemonic-name-3 may be
      *>   specified only in the ACCEPT and DISPLAY statements" -> OK
      *>   cite.py --check "§2002:A.2.6.2 SR5" "When mnemonic-name-1 is
      *>   specified, the name is associated with a feature-name
      *>   specified by the implementor" -> OK
      *> The same program with BEFORE ADVANCING 2 LINES compiles at
      *> --std 2002 with only the COBOL0903 obsolete warnings, so the
      *> one error is the device mnemonic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG             PIC X(10) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SEND OUT-CD FROM MSG WITH EMI
               BEFORE ADVANCING OUT-DEV.
           STOP RUN.
