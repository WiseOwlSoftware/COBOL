      *>--- conformance-case
      *> id: g2-type-same-as-subject
      *> rules: [2002-SR-13.16.55.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.55.2 SR1 - "The description of any data item
      *> subordinate to type-name-1 shall not contain a SAME AS clause
      *> that references the subject of this entry or any group item to
      *> which this entry is subordinate." WS-REC is the subject of
      *> TYPE T, and T-B, subordinate to T, is SAME AS WS-REC.
      *> WS-REC is a level 1 group in working storage, so the SAME AS
      *> target itself is legal (§2002:13.16.47.2 SR7). The same cycle
      *> is also forbidden from the SAME AS side (§2002:13.16.47.2 SR4:
      *> WS-REC's description holds TYPE T, the record T-B is under);
      *> the two rules are mirror images and cannot be separated.
      *> Expected: refused with the SAME AS cycle diagnostic COBOL1557
      *> at line 25 (T-B). Fails if the TYPE subject is not checked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2TYP01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T TYPEDEF.
           05  T-A PIC X(4).
           05  T-B SAME AS WS-REC.
       01  WS-REC TYPE T.
       PROCEDURE DIVISION.
           DISPLAY "BAD".
           STOP RUN.
