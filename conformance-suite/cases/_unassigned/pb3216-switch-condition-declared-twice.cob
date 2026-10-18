*>--- conformance-case
*> id: pb3216-switch-condition-declared-twice
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB3216 - ISO 8.4.2.1 as in pb3216-alphabet-name-declared-twice: the condition-name of a switch entry's
*> ON STATUS phrase is referenced unqualified only, so S-ON declared by two switches leaves `IF S-ON` without a
*> reference that uniquely identifies one condition.  It compiled clean and tested SWITCH-1 alone.
*> Expected: COBOLNET4250 naming the second S-ON.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3216-SWITCH.
ENVIRONMENT DIVISION.
CONFIGURATION SECTION.
SPECIAL-NAMES.
    SWITCH-1 ON STATUS IS S-ON
    SWITCH-2 ON STATUS IS S-ON.
PROCEDURE DIVISION.
    IF S-ON DISPLAY "ON" END-IF
    STOP RUN.
