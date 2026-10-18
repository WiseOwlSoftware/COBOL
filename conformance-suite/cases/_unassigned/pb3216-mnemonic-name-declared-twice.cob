*>--- conformance-case
*> id: pb3216-mnemonic-name-declared-twice
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3216 - ISO 8.4.2.1 as in pb3216-alphabet-name-declared-twice: a mnemonic-name cannot be qualified,
*> so SYSOUT IS DEV and CONSOLE IS DEV leave `DISPLAY ... UPON DEV` without a reference that uniquely identifies
*> one device.  Expected: COBOLNET4250 naming the second mnemonic-name DEV.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3216-MNEMO.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SYSOUT IS DEV
    CONSOLE IS DEV.
PROCEDURE DIVISION.
    DISPLAY "X" UPON DEV
    STOP RUN.
