      *>--- conformance-case
      *> id: g2002-special-names-dynamic-length-structure
      *> rules: [2002-FMT-12.2.6.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.6.1 - the 2002 general format of the SPECIAL-NAMES
      *> paragraph has no DYNAMIC LENGTH STRUCTURE clause: its clauses
      *> are the switch, feature and device entries, ALPHABET, SYMBOLIC
      *> CHARACTERS, LOCALE, ORDER TABLE, CLASS, CURRENCY SIGN,
      *> DECIMAL-POINT, CURSOR and CRT STATUS. A paragraph holding a
      *> DYNAMIC LENGTH STRUCTURE clause is therefore outside the 2002
      *> format and is refused at --std 2002. The 2002 clauses written in
      *> the 2002 order are accepted by g2002_special_names_full_format.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SPN09.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DYNAMIC LENGTH STRUCTURE DS-SHORT IS SHORT PREFIXED.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC X VALUE "A".
       PROCEDURE DIVISION.
           DISPLAY W1.
           STOP RUN.
