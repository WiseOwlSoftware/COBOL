      *>--- conformance-case
      *> id: pb870_start_first_last_relative_ecio_checked
      *> rules: [GR-14.9.41.4-11, GR-14.9.41.4-12, GR-9.1.14-1,
      *>    GR-9.1.14-2, GR-9.1.14-3, GR-9.1.13.7-7]
      *> accept: [2002, 2023]
      *> constructs: [start-first-last-2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB870 - START FIRST and START LAST on a RELATIVE file
      *> with EC-I-O CHECKING ENABLED. The sibling
      *> pb870_start_first_last_relative_exceptions leaves checking off;
      *> this program is the checked arm of the same two statements
      *> (campaign PB3353: EC-I-O conditions with and without USE
      *> declaratives and FILE STATUS).
      *> The rules (each quotation checked with scripts/spec/cite.py):
      *>   14.9.41.4 GR11/GR12 - the empty file makes the invalid key
      *>     condition exist; 9.1.14 1) - "if enabled, the
      *>     EC-I-O-INVALID-KEY exception condition is set to exist",
      *>     so it is the last exception status (15.33.3).
      *>   9.1.14 2) - with an INVALID KEY phrase the phrase runs and
      *>     "any applicable exception processing statements are not
      *>     executed" (legs K1, K3A).
      *>   9.1.14 3) / 14.6.13.1.4 3) - without the phrase the
      *>     applicable USE procedure runs, and "If execution of the
      *>     declarative completes normally, execution continues as
      *>     specified in the rules for normal execution", i.e. the
      *>     statement ends (legs K2, K3B), with or without a FILE
      *>     STATUS clause (FE has none, FG has one).
      *>   9.1.13.7 7) - START on a connector not open input or I-O is
      *>     I-O status 47, a fatal EC-I-O-LOGIC-ERROR, which is NOT
      *>     the invalid key condition: the USE declarative below names
      *>     only EC-I-O-INVALID-KEY and does not apply, so 14.6.13.1.3
      *>     7) "If checking for the exception condition is enabled,
      *>     execution of the run unit is terminated abnormally" even
      *>     though FG has a FILE STATUS clause (DOC-A.1-103). The
      *>     golden's output therefore stops after "K4 BEFORE".
      *> WHY EACH LEG CAN FAIL. A START that stopped raising the
      *> condition under checking prints blanks or another name in K1,
      *> K2, K3; one that ran the USE procedure for K1 or K3A prints an
      *> extra DECL line; one that continued past the 47 prints K4
      *> AFTER.
       >>TURN EC-I-O CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB870ECK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FE ASSIGN TO "pb870ck-e.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS EK.
           SELECT FG ASSIGN TO "pb870ck-g.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS GK
               FILE STATUS IS ST-G.
       DATA DIVISION.
       FILE SECTION.
       FD FE.
       01 E-REC PIC X(4).
       FD FG.
       01 G-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 EK   PIC 9(4).
       01 GK   PIC 9(4).
       01 ST-G PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-I-O-INVALID-KEY.
       D11.
           DISPLAY "DECL [" FUNCTION EXCEPTION-STATUS "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT FE
           CLOSE FE
           OPEN OUTPUT FG
           CLOSE FG
           OPEN INPUT FE
           OPEN INPUT FG
      *> ---- K1: FE (no FILE STATUS), INVALID KEY phrase ---------
           START FE FIRST
               INVALID KEY DISPLAY "K1 INVALID ["
                   FUNCTION EXCEPTION-STATUS "]"
               NOT INVALID KEY DISPLAY "K1 NOT-INVALID"
           END-START
      *> ---- K2: FE, no phrase: the USE procedure runs -----------
           START FE LAST
           DISPLAY "K2 AFTER"
      *> ---- K3: FG (with FILE STATUS) both ways -----------------
           START FG FIRST
               INVALID KEY DISPLAY "K3A INVALID " ST-G " ["
                   FUNCTION EXCEPTION-STATUS "]"
           END-START
           START FG LAST
           DISPLAY "K3B AFTER " ST-G
           CLOSE FE
           CLOSE FG
      *> ---- K4: START on the closed FG: fatal 47, run unit ends -
           DISPLAY "K4 BEFORE"
           START FG LAST
           DISPLAY "K4 AFTER " ST-G
           STOP RUN.
