      *> reject-at: 2023
      *> ISO Annex A §A.4.9 (Locale support and related functions) is an OPTIONAL module; COBOL.NET's
      *> documented non-support of an item that has not yet landed is conformant per §4.2.7 / §A.4.1. The four
      *> locale functions — LOCALE-COMPARE §15.51, LOCALE-DATE §15.52, LOCALE-TIME §15.53,
      *> LOCALE-TIME-FROM-SECONDS §15.54 — are rejected BY NAME at bind time with COBOLNET1518
      *> (PHASE-11-scout-notes.md spec:locale).
      *>
      *> ⚠ STANDARD-COMPARE §15.85 WAS THE FIFTH LINE OF THIS PROGRAM AND IS DELIBERATELY GONE (kb/Work PB101
      *> T7). It is A.4.9 item 11 but travels on Annex A.3 item 25 — the implementor need not accept the syntax
      *> when support for ISO/IEC 14651:2020 is not provided — and COBOL.NET now provides it, so the reference
      *> COMPILES and belongs in the positive corpus (2002/pb101_standard_compare), not here. Leaving it would
      *> have left this fixture green for the wrong reason: the four remaining functions carry the 1518 on their
      *> own, so nothing would have failed while the assertion it makes about STANDARD-COMPARE went false.
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
           STOP RUN.
