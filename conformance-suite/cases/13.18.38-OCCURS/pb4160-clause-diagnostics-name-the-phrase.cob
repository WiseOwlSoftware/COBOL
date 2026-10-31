      *>--- conformance-case
      *> id: pb4160-clause-diagnostics-name-the-phrase
      *> rules: [FMT-13.18.38.2, SR-13.18.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4160, the DATA DIVISION half. A diagnostic about
      *> a clause names the clause as written: OCCURS ASCENDING KEY
      *> IS 'X' (13.18.38.2 prints ASCENDING or DESCENDING before
      *> KEY), OCCURS DEPENDING ON 'D', CODE-SET AL1 (13.18.13) and
      *> COLLATING SEQUENCE OF IK IS 'X' (12.4.5.7), never the
      *> clause's first word and an ellipsis, which the console
      *> renders as "OCCURS . KEY IS". SR3 of 13.18.38.3 requires
      *> the first KEY data-name to name the entry or a subordinate
      *> of it and every later one a subordinate. The conformance
      *> suite's refusal test checks the RENDERED text of every
      *> diagnostic for the elided opening.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4160D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET AL1 FOR NATIONAL IS UTF-8.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CSF ASSIGN TO "csf" ORGANIZATION IS SEQUENTIAL.
           SELECT IXF ASSIGN TO "ixf" ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS IK
               COLLATING SEQUENCE OF IK IS NOSUCH.
       DATA DIVISION.
       FILE SECTION.
       FD CSF CODE-SET IS AL1.
       01 CSR PIC X(4).
       FD IXF.
       01 IXR.
          05 IK PIC X(4).
       WORKING-STORAGE SECTION.
       01 CNT PIC 9(2).
       01 T1.
          05 E1 PIC X OCCURS 3 TIMES ASCENDING KEY IS NOSUCH1.
       01 T2.
          05 E2 PIC X OCCURS 1 TO 5 TIMES DEPENDING ON NOSUCH2.
       01 T3.
          05 E3 PIC X OCCURS 3 TIMES DESCENDING KEY IS E3 E3.
       PROCEDURE DIVISION.
           STOP RUN.
