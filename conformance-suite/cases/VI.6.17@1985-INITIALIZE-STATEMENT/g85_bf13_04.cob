      *>--- conformance-case
      *> id: g85_bf13_04
      *> rules: [1985-GR-VI.6.17.4-5, 1985-GR-VI.6.17.4-4, 1985-GR-VI.6.17.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.17.4 GR5 - without REPLACING, alphabetic,
      *> alphanumeric and alphanumeric edited items become spaces and
      *> numeric and numeric edited items become zeros, each as an
      *> elementary MOVE of SPACES or ZEROS (line 1; the edited item
      *> ZZ9.99 shows the MOVE editing: "  0.00").
      *> §1985:VI.6.17.4 GR4 - B-RED, a REDEFINES subordinate to the
      *> receiving group, is excluded, so B-ORIG keeps the spaces it was
      *> given and is not overwritten by zeros (line 2); a receiving
      *> identifier that itself has REDEFINES is initialized (line 3).
      *> §1985:VI.6.17.4 GR6 - the identifiers are processed left to
      *> right: N4 then X4 (same storage) leaves spaces (line 4), X4
      *> then N4 leaves zeros (line 5).
      *> A leg fails if a category gets the wrong figurative, if the
      *> redefining item is initialized, or if the order is reversed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1304.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  A-ALPHA         PIC A(3)   VALUE "ABC".
           05  A-ALNUM         PIC X(3)   VALUE "DEF".
           05  A-AEDIT         PIC XBX.
           05  A-NUM           PIC 9(3)   VALUE 123.
           05  A-NEDIT         PIC ZZ9.99.
           05  B-ORIG          PIC X(4)   VALUE "WXYZ".
           05  B-RED REDEFINES B-ORIG.
               10  B-RED-N     PIC 9(4).
       01  N4                  PIC 9(4)   VALUE 1234.
       01  X4 REDEFINES N4     PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           MOVE "GH" TO A-AEDIT.
           MOVE 4.56 TO A-NEDIT.
           INITIALIZE G1.
           DISPLAY "[" A-ALPHA "][" A-ALNUM "][" A-AEDIT "]["
               A-NUM "][" A-NEDIT "]".
           DISPLAY "[" B-ORIG "]".
           INITIALIZE B-RED.
           DISPLAY "[" B-ORIG "]".
           INITIALIZE N4 X4.
           DISPLAY "[" X4 "]".
           INITIALIZE X4 N4.
           DISPLAY "[" X4 "]".
           STOP RUN.
