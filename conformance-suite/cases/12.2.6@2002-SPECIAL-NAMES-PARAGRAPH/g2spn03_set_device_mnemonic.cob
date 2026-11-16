      *>--- conformance-case
      *> id: g2spn03_set_device_mnemonic
      *> rules: [2002-SR-12.2.6.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR7 - "Mnemonic-name-3 may be specified only in
      *> the ACCEPT and DISPLAY statements." OUT-DEV is a
      *> mnemonic-name-3 (SYSOUT IS OUT-DEV), so SET OUT-DEV TO ON
      *> (line 25) breaks SR7 (the SET statement's mnemonic-name is a
      *> switch's mnemonic-name-1, §2002:12.2.6.2 SR5) and is refused
      *> there. The WRITE ADVANCING arm is
      *> cases/12.3.7-SPECIAL-NAMES/pb862-write-advancing-device-
      *> mnemonic, which refuses at 2002.
      *>   cite.py --check "§2002:12.2.6.2 SR7" "Mnemonic-name-3 may be
      *>   specified only in the ACCEPT and DISPLAY statements" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET OUT-DEV TO ON.
           STOP RUN.
