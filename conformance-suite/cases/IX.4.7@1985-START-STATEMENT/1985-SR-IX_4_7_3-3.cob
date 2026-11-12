      *>--- conformance-case
      *> id: 1985-SR-IX_4_7_3-3
      *> rules: [1985-SR-IX.4.7.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.7.3 SR3 (THE START STATEMENT). A refusal: the rule
      *> forbids line 20 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. STRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO IXDISK
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXK.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IXR.
           05 IXK PIC X(4).
           05 IXD PIC X(20).
       PROCEDURE DIVISION.
       P1.
           OPEN INPUT IXF.
           MOVE "AAAA" TO IXK.
           START IXF KEY IS EQUAL TO IXK.
           CLOSE IXF.
           STOP RUN.
