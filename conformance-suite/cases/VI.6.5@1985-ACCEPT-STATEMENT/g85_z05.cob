      *>--- conformance-case
      *> id: g85_z05
      *> rules: [1985-GR-VI.6.5.4-1, 1985-GR-VI.6.5.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> stdin: g85_z05.in
      *>---
      *> §1985:VI.6.5.4 GR1 - ACCEPT transfers data from the device
      *> and it REPLACES the receiver's content: every receiver starts
      *> full of another letter and none of it survives (lines A, B,
      *> C). The conversion is the implementor's (docs/conformance/7/
      *> DOC-A.1-1.md): a numeric DISPLAY receiver reads the decimal
      *> form over its own size, so the text '  7' stores 7 and shows
      *> 007 (line N; stored as characters it would show '  7'), and
      *> ADD 1 then gives 008 (line M).
      *> §1985:VI.6.5.4 GR5 - with no FROM the implementor's standard
      *> device is used: standard input (docs/conformance/7/
      *> DOC-A.1-2.md), which SYSIN also names, so the four input
      *> lines arrive in order across ACCEPTs with and without FROM
      *> (lines A-C; another default device leaves A, N, C unfilled).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z05.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYSIN IS IN-DEV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A10   PIC X(10) VALUE "ZZZZZZZZZZ".
       01  B10   PIC X(10) VALUE "YYYYYYYYYY".
       01  N3    PIC 9(3)  VALUE 999.
       01  C10   PIC X(10) VALUE "XXXXXXXXXX".
       PROCEDURE DIVISION.
       P1.
           ACCEPT A10.
           DISPLAY "A[" A10 "]".
           ACCEPT B10 FROM IN-DEV.
           DISPLAY "B[" B10 "]".
           ACCEPT N3.
           DISPLAY "N[" N3 "]".
           ADD 1 TO N3.
           DISPLAY "M[" N3 "]".
           ACCEPT C10.
           DISPLAY "C[" C10 "]".
           STOP RUN.
