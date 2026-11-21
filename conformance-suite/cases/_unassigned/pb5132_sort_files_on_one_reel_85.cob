      *>--- conformance-case
      *> id: pb5132_sort_files_on_one_reel_85
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5132 item 2 - the SORT side of the multiple file reel
      *> rules (PB5086). §1985:XI.4.1.3 SR6 bars two files of one MERGE
      *> on one multiple file reel; the SORT statement has no such
      *> rule, and §1985:XI.4.4.3 SR7 is the one that matters here: the
      *> files named in SORT's USING phrase may reside on one multiple
      *> file reel. MULTIPLE FILE TAPE CONTAINS IN1 IN2
      *> (§1985:VII.2.11.3 GR1) places both USING files on one reel, so
      *> the SORT below must compile at --std 85. The companion refusal
      *> for MERGE is pb5086-merge-files-on-one-reel.
      *> Derivation: IN1 holds "BBBB" and IN2 holds "AAAA"; SORT ON
      *> ASCENDING KEY SR puts the smaller key first, so GIVING OUT1
      *> holds "AAAA" then "BBBB", and the DISPLAYs print them in that
      *> order.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5132S1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN1 ASSIGN TO "pb5132s1a.dat".
           SELECT IN2 ASSIGN TO "pb5132s1b.dat".
           SELECT OUT1 ASSIGN TO "pb5132s1c.dat".
           SELECT SD1 ASSIGN TO "pb5132s1d.dat".
       I-O-CONTROL.
           MULTIPLE FILE TAPE CONTAINS IN1 IN2.
       DATA DIVISION.
       FILE SECTION.
       FD  IN1.
       01  R1 PIC X(4).
       FD  IN2.
       01  R2 PIC X(4).
       FD  OUT1.
       01  R3 PIC X(4).
       SD  SD1.
       01  SR PIC X(4).
       WORKING-STORAGE SECTION.
       01  EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IN1.
           MOVE "BBBB" TO R1.
           WRITE R1.
           CLOSE IN1.
           OPEN OUTPUT IN2.
           MOVE "AAAA" TO R2.
           WRITE R2.
           CLOSE IN2.
           SORT SD1 ON ASCENDING KEY SR USING IN1 IN2 GIVING OUT1.
           OPEN INPUT OUT1.
           PERFORM UNTIL EOF-SW = "Y"
               READ OUT1
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END DISPLAY R3
               END-READ
           END-PERFORM.
           CLOSE OUT1.
           STOP RUN.
