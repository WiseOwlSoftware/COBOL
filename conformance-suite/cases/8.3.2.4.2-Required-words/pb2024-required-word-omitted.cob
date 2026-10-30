      *>--- conformance-case
      *> id: pb2024-required-word-omitted
      *> rules: [SR-8.3.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> MOVE WS-A WS-B - the underlined TO of MOVE Format 1 is omitted. Expected: refused at every
      *> edition.
      *> ISO 8.3.2.4.2 1) - required words: "Keywords. Within each format, such words are uppercase
      *> and underlined." ISO 5.2.2: keywords "are required in order to select the functionality
      *> associated with that keyword, subject to the conventions specified in 5.2.6"; no bracket or
      *> choice indicator holds TO apart from MOVE in ISO 14.9.25.2 Format 1
      *> "MOVE { identifier-1 / literal-1 } TO { identifier-2 } ...", so no convention lets it go.
      *>   cite.py: OK 8.3.2.4.2 1) (Required words)  OK 5.2.2 (Keywords)
      *> The rule holds across the grammar by measurement: scripts/spec/audit_grammar_optional_words.py
      *> joins every underlined word to the grammar and reports one a rule lets be omitted (kb/Work
      *> PB2024); its known catches are the pb755 (LOCK ON) and pb756 (ALTERNATE RECORD) negatives.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2024R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X(4) VALUE "ABCD".
       01 WS-B PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE WS-A WS-B.
           DISPLAY WS-B.
           STOP RUN.
