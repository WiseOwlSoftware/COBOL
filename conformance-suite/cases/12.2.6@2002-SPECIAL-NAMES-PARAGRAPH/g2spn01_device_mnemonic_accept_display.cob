      *>--- conformance-case
      *> id: g2spn01_device_mnemonic_accept_display
      *> rules: [2002-SR-12.2.6.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.2 SR7 - "Mnemonic-name-3 may be specified only in
      *> the ACCEPT and DISPLAY statements." The rule GRANTS the two
      *> statements; this case writes both with a mnemonic-name-3
      *> (device-name-1 IS mnemonic-name-3, §2002:12.2.6.1) and shows
      *> them accepted and executed. The refusals beside it are
      *> g2spn02 (SEND ADVANCING) and g2spn03 (SET).
      *>   cite.py --check "§2002:12.2.6.2 SR7" "Mnemonic-name-3 may be
      *>   specified only in the ACCEPT and DISPLAY statements" -> OK
      *> OUT-DEV names the device SYSOUT and IN-DEV the device SYSIN
      *> (implementor device names, §2002:12.2.6.2 SR8). The harness
      *> runs with stdin closed, so what the ACCEPT stores is the
      *> implementor's end-of-input behavior and is not pinned; the
      *> case pins that the ACCEPT FROM IN-DEV compiles and executes
      *> and control passes on.
      *> Derived output:
      *>   DISPLAY-UPON-DEVICE   (DISPLAY ... UPON OUT-DEV)
      *>   ACCEPT-DONE           (after ACCEPT ... FROM IN-DEV)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSOUT IS OUT-DEV
           SYSIN IS IN-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W               PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DISPLAY-UPON-DEVICE" UPON OUT-DEV
           ACCEPT W FROM IN-DEV
           DISPLAY "ACCEPT-DONE"
           STOP RUN.
