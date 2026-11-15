      *>--- conformance-case
      *> id: g2-type-level77-group
      *> rules: [2002-SR-13.16.55.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR7 - "If the subject of the entry is a level
      *> 77 item, type-name-1 shall be an elementary item." W is a group
      *> type; Z, a level-77 item, is described TYPE W. Expected:
      *> COBOL1536 at line 21. Fails if a 77 subject is expanded into a
      *> group. A 77 entry exists only in working-storage, local-storage
      *> and linkage (the 2002 data division general format), so the
      *> rule has no CD reach.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP07.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W TYPEDEF.
           05  W-A PIC X(2).
           05  W-B PIC 9(3).
       77  Z TYPE W.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
