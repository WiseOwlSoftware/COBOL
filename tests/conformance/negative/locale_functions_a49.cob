      *> reject-at: 2023
      *> ISO Annex A §A.4.9 (Locale support and related functions) is an OPTIONAL module; COBOL.NET's
      *> documented non-support (ratified decision 3) is conformant per §4.2.7 / §A.4.1. The five locale
      *> functions — LOCALE-COMPARE §15.51, LOCALE-DATE §15.52, LOCALE-TIME §15.53, LOCALE-TIME-FROM-SECONDS
      *> §15.54, STANDARD-COMPARE §15.85 — are rejected BY NAME at bind time with COBOLNET1518
      *> (PHASE-11-scout-notes.md spec:locale). STANDARD-COMPARE additionally cites §A.3 item 25 (dependent
      *> on an ISO/IEC 14651:2020 implementation).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P11LOCFN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REL  PIC X.
       01 WS-DATE PIC X(10).
       01 WS-SECS PIC 9(6).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LOCALE-COMPARE("A" "B") TO WS-REL
           MOVE FUNCTION LOCALE-DATE(20240229) TO WS-DATE
           MOVE FUNCTION LOCALE-TIME(120000) TO WS-DATE
           COMPUTE WS-SECS = FUNCTION LOCALE-TIME-FROM-SECONDS(3600)
           MOVE FUNCTION STANDARD-COMPARE("A" "B") TO WS-REL
           STOP RUN.
