      *>--- conformance-case
      *> id: pb870_start_first_last_relative_nofs_nouse
      *> rules: [GR-14.9.41.4-11, GR-14.9.41.4-12, GR-9.1.14-2,
      *>    GR-9.1.14-4, GR-9.1.13.7-7]
      *> accept: [2002, 2023]
      *> constructs: [start-first-last-2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB870 - START FIRST and START LAST on a RELATIVE file
      *> that has NO FILE STATUS clause and NO USE procedure, EC-I-O
      *> checking off (the default). The sibling
      *> pb870_start_first_last_relative_exceptions gives every file a
      *> FILE STATUS clause; this is the arm with nothing to see the
      *> failure (campaign PB3353).
      *> The rules (each quotation checked with scripts/spec/cite.py):
      *>   14.9.41.4 GR11/GR12 - the empty file makes the invalid key
      *>     condition exist (I-O status 23, nonfatal).
      *>   9.1.14 2) - with an INVALID KEY phrase it runs (leg U1).
      *>   9.1.14 4) - with neither phrase "control is transferred to
      *>     the end of the input-output statement": leg U2 continues,
      *>     and a NOT INVALID KEY phrase alone is not executed (U3).
      *>   9.1.13.7 7) - START on a closed file is I-O status 47, a
      *>     fatal status (9.1.13.1: first digit 4). With no FILE
      *>     STATUS clause and no USE procedure, nothing in the
      *>     program can see it; 9.1.13.1 leaves the action to the
      *>     implementor ("If the implementor chooses to continue
      *>     execution of the run unit ...") and this implementation
      *>     terminates the run unit abnormally (docs/CONFORMANCE.md
      *>     DOC-A.1-103; GnuCOBOL does the same). The output stops
      *>     after "U4 BEFORE".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB870NFS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN ASSIGN TO "pb870nf-n.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS NK.
       DATA DIVISION.
       FILE SECTION.
       FD FN.
       01 N-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 NK PIC 9(4).
       PROCEDURE DIVISION.
       M1.
           OPEN OUTPUT FN
           CLOSE FN
           OPEN INPUT FN
      *> ---- U1: INVALID KEY phrase runs -------------------------
           START FN FIRST
               INVALID KEY DISPLAY "U1 INVALID"
               NOT INVALID KEY DISPLAY "U1 NOT-INVALID"
           END-START
      *> ---- U2: no phrase, no FILE STATUS, no USE: continues ----
           START FN LAST
           DISPLAY "U2 AFTER"
      *> ---- U3: NOT INVALID KEY alone is not executed -----------
           START FN FIRST
               NOT INVALID KEY DISPLAY "U3 NOT-INVALID"
           END-START
           DISPLAY "U3 AFTER"
           CLOSE FN
      *> ---- U4: START on the closed file: fatal 47, nothing sees it
           DISPLAY "U4 BEFORE"
           START FN LAST
           DISPLAY "U4 AFTER"
           STOP RUN.
