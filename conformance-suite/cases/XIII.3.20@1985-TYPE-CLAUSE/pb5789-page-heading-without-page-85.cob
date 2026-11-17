      *>--- conformance-case
      *> id: pb5789-page-heading-without-page-85
      *> rules: [1985-SR-XIII.3.20.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5789. §1985:XIII.3.20.3 SR3: "Page heading and page
      *>   footing report groups may be specified only if a PAGE clause is
      *>   specified in the corresponding report description entry".
      *>   The RD has no PAGE clause: the page heading is refused with
      *>   COBOL0899.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5789NP3.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5789NP3.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-KEY PIC 9 VALUE 1.
       01 WS-OTH PIC 9 VALUE 1.
       REPORT SECTION.
       RD R1 CONTROLS ARE FINAL WS-KEY.
       01 PH1 TYPE PH LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "PH1".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "D1".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
