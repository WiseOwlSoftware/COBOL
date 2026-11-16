      *>--- conformance-case
      *> id: g2mrg-format-arms
      *> rules: [2002-FMT-14.8.23.1, 2002-SR-14.8.23.2-4,
      *>    2002-FMT-14.8.32.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.1 (general format) - the MERGE arms written
      *>   here: ON and KEY written and omitted, two KEY phrases and
      *>   one KEY phrase naming two keys, COLLATING SEQUENCE omitted,
      *>   "IS alphabet-name-1" alone (L5), "IS alphabet-name-1
      *>   alphabet-name-2" (COLLATING omitted, L2), "FOR ALPHANUMERIC
      *>   IS" alone (L3), "FOR NATIONAL IS" alone (L6) and both
      *>   choice-indicator alternatives with NATIONAL first (L4;
      *>   §2002:5.1.5.3 "The alternatives may be specified in any
      *>   order."), USING two files, GIVING one and two files, OUTPUT
      *>   PROCEDURE with THROUGH and with THRU (§2002:14.8.23.2 SR8).
      *>   (OUTPUT PROCEDURE without THRU:
      *>   g2mrg01-merge-anywhere-but-declaratives.)
      *> §2002:14.8.23.2 SR4b "Key data-names may be qualified." (SW-K1
      *>   OF SW-R) and SR4e: SW has a second record SW-R2 that does not
      *>   describe the keys ("need be described in only one of the
      *>   record descriptions").
      *> §2002:14.8.32.1 (RETURN format): RECORD INTO ... AT END ... NOT
      *>   AT END ... END-RETURN, and RECORD AT END ending at a period.
      *> DERIVATION (2002 text only). REV is ALPHABET "C" "B" "A", so
      *>   C < B < A (§2002:12.2.6.3 GR7k). Keys are listed in order of
      *>   decreasing significance (§2002:14.8.23.3 GR3), ASCENDING is
      *>   lowest first and DESCENDING highest first (GR3a, GR3b), the
      *>   COLLATING SEQUENCE alphabet applies to alphanumeric keys
      *>   (GR5a), and equal keys come back in USING order, file by
      *>   file (GR4a, GR4b). Every input file is in key order (GR6).
      *>   L1 SQ1 = A9x1 A1x1 C5x1, SQ2 = A5y2 B7y2 C8y2, K1 ascending
      *>     then K2 descending, native order: A9x1 A5y2 A1x1 B7y2
      *>     C8y2 C5x1, written to each GIVING file (GR12).
      *>   L3 same inputs, K1 descending under REV (A highest): the A
      *>     records SQ1 first (A9x1 A1x1 A5y2), B7y2, then the C
      *>     records SQ1 first (C5x1 C8y2); RETURN INTO delivers each
      *>     (§2002:14.8.32.3 GR4), the at end condition ends the loop
      *>     (GR3), and the THROUGH range runs on into OP-B.
      *>   L2 SQ1 = C1c5 A1a5, SQ2 = B1b6, K1 K2 ascending under REV:
      *>     C1c5 B1b6 A1a5 (native order would put A1a5 first).
      *>   L5 same inputs, K1 ascending under "IS REV" alone, which
      *>     names the alphanumeric sequence (GR5a): C1c5 B1b6 A1a5.
      *>   L4 same inputs and REV through the two-alternative form:
      *>     C1c5 B1b6 A1a5, gathered by the period-ended RETURN.
      *>   L6 SQ1 = A1a5 C1c5, SQ2 = B1b6, K1 ascending under "FOR
      *>     NATIONAL IS ALN" alone. That phrase sets only the national
      *>     sequence; the keys are alphanumeric, so the program
      *>     collating sequence (native: no PROGRAM COLLATING SEQUENCE
      *>     clause) applies (GR5, GR5b "Second, the collating
      *>     sequences established as the program collating
      *>     sequences."): A1a5 B1b6 C1c5.
      *> A compiler that drops the alphabet, the second key, a GIVING
      *>   file, the tie order or the THRU range, or refuses a form,
      *>   fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRGFM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "C" "B" "A"
           ALPHABET ALN FOR NATIONAL IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ1 ASSIGN TO "g2mrgfm1.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SQ2 ASSIGN TO "g2mrgfm2.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SQ3 ASSIGN TO "g2mrgfm3.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SQ4 ASSIGN TO "g2mrgfm4.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SW ASSIGN TO "g2mrgfm.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD SQ1.
       01 SQ1-R PIC X(4).
       FD SQ2.
       01 SQ2-R PIC X(4).
       FD SQ3.
       01 SQ3-R PIC X(4).
       FD SQ4.
       01 SQ4-R PIC X(4).
       SD SW.
       01 SW-R.
          05 SW-K1 PIC X.
          05 SW-K2 PIC X.
          05 SW-D  PIC XX.
       01 SW-R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 W   PIC X(4).
       01 EOF PIC 9 VALUE 0.
       01 ACC PIC X(40).
       01 P   PIC 99.
       PROCEDURE DIVISION.
       M-0.
           OPEN OUTPUT SQ1 SQ2
           WRITE SQ1-R FROM "A9x1"
           WRITE SQ1-R FROM "A1x1"
           WRITE SQ1-R FROM "C5x1"
           WRITE SQ2-R FROM "A5y2"
           WRITE SQ2-R FROM "B7y2"
           WRITE SQ2-R FROM "C8y2"
           CLOSE SQ1 SQ2
      *> L1: two KEY phrases (ON and the second KEY omitted), a
      *>     qualified key, no COLLATING SEQUENCE, GIVING two files.
           MERGE SW ON ASCENDING KEY SW-K1 OF SW-R DESCENDING SW-K2
               USING SQ1 SQ2
               GIVING SQ3 SQ4
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           OPEN INPUT SQ3
           PERFORM UNTIL EOF = 1
               READ SQ3 AT END MOVE 1 TO EOF
                   NOT AT END STRING " " SQ3-R DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE SQ3
           DISPLAY "L1 SQ3" ACC(1:P - 1)
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           OPEN INPUT SQ4
           PERFORM UNTIL EOF = 1
               READ SQ4 AT END MOVE 1 TO EOF
                   NOT AT END STRING " " SQ4-R DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE SQ4
           DISPLAY "L1 SQ4" ACC(1:P - 1)
      *> L3: DESCENDING under FOR ALPHANUMERIC IS REV, an output
      *>     procedure range written with THROUGH.
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           MERGE SW ON DESCENDING KEY SW-K1
               COLLATING SEQUENCE FOR ALPHANUMERIC IS REV
               USING SQ1 SQ2
               OUTPUT PROCEDURE IS OP-A THROUGH OP-B
           OPEN OUTPUT SQ1 SQ2
           WRITE SQ1-R FROM "C1c5"
           WRITE SQ1-R FROM "A1a5"
           WRITE SQ2-R FROM "B1b6"
           CLOSE SQ1 SQ2
      *> L2: one KEY phrase naming two keys, KEY and COLLATING
      *>     omitted, SEQUENCE IS alphabet-name-1 alphabet-name-2.
           MERGE SW ASCENDING SW-K1 SW-K2
               SEQUENCE IS REV ALN
               USING SQ1 SQ2
               GIVING SQ3
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           OPEN INPUT SQ3
           PERFORM UNTIL EOF = 1
               READ SQ3 AT END MOVE 1 TO EOF
                   NOT AT END STRING " " SQ3-R DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE SQ3
           DISPLAY "L2 SQ3" ACC(1:P - 1)
      *> L5: SEQUENCE IS alphabet-name-1 without alphabet-name-2.
           MERGE SW ON ASCENDING KEY SW-K1
               COLLATING SEQUENCE IS REV
               USING SQ1 SQ2
               GIVING SQ3
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           OPEN INPUT SQ3
           PERFORM UNTIL EOF = 1
               READ SQ3 AT END MOVE 1 TO EOF
                   NOT AT END STRING " " SQ3-R DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE SQ3
           DISPLAY "L5 SQ3" ACC(1:P - 1)
      *> L4: both choice-indicator alternatives, NATIONAL first; an
      *>     output procedure range written with THRU.
           MOVE SPACES TO ACC MOVE 1 TO P
           MERGE SW ON ASCENDING KEY SW-K1
               COLLATING SEQUENCE FOR NATIONAL IS ALN
                                  FOR ALPHANUMERIC IS REV
               USING SQ1 SQ2
               OUTPUT PROCEDURE IS OP-C THRU OP-D
      *> L6: FOR NATIONAL IS alone; the inputs are rewritten in native
      *>     order, the sequence that applies to these keys (GR6).
           OPEN OUTPUT SQ1 SQ2
           WRITE SQ1-R FROM "A1a5"
           WRITE SQ1-R FROM "C1c5"
           WRITE SQ2-R FROM "B1b6"
           CLOSE SQ1 SQ2
           MERGE SW ON ASCENDING KEY SW-K1
               COLLATING SEQUENCE FOR NATIONAL IS ALN
               USING SQ1 SQ2
               GIVING SQ3
           MOVE SPACES TO ACC MOVE 1 TO P MOVE 0 TO EOF
           OPEN INPUT SQ3
           PERFORM UNTIL EOF = 1
               READ SQ3 AT END MOVE 1 TO EOF
                   NOT AT END STRING " " SQ3-R DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-READ
           END-PERFORM
           CLOSE SQ3
           DISPLAY "L6 SQ3" ACC(1:P - 1)
           STOP RUN.
       OP-A.
           PERFORM UNTIL EOF = 1
               RETURN SW RECORD INTO W
                   AT END MOVE 1 TO EOF
                   NOT AT END STRING " " W DELIMITED BY SIZE
                                  INTO ACC WITH POINTER P
               END-RETURN
           END-PERFORM
           DISPLAY "L3" ACC(1:P - 1).
       OP-B.
           DISPLAY "L3 OP-B".
       OP-C.
           RETURN SW RECORD AT END GO TO OP-D.
           STRING " " SW-R DELIMITED BY SIZE INTO ACC WITH POINTER P
           GO TO OP-C.
       OP-D.
           DISPLAY "L4" ACC(1:P - 1).
