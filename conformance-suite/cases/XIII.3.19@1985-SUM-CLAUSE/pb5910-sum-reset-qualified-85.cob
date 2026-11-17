      *>--- conformance-case
      *> id: pb5910-sum-reset-qualified-85
      *> rules: [1985-FMT-XIII.3.19.2, 1985-SR-XIII.3.19.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5910 (review R1-1). The RESET phrase's operand is data-name-2 of the
      *>   SUM clause's general format, and §1985:III.2 says a 'data-name' "must not
      *>   be reference-modified, subscripted, or qualified unless specifically
      *>   permitted by the rules of the format". §1985:XIII.3.19.3 SR4 permits
      *>   nothing: "Data-name-2 must be one of the data-names specified in the
      *>   CONTROL clause for this report". ISO/IEC 1989:2002 13.16.52.2 SR7 first
      *>   permits it: "Data-name-3 may be qualified and reference modified". The
      *>   CONTROL operands are plain; RESET ON names WS-KEY OF WS-GRP. Expected:
      *>   COBOL0900 at 85 at the RESET operand. Fails if the RESET operand's
      *>   qualifier is not asked at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5910RQ.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5910RQ.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          02 WS-KEY PIC 9 VALUE 1.
          02 WS-SUB PIC 9 VALUE 1.
          02 WS-AMT PIC 9 VALUE 3.
       REPORT SECTION.
       RD R1 CONTROLS ARE WS-KEY WS-SUB.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-KEY.
       01 CF1 TYPE CF WS-SUB LINE PLUS 1.
          02 COLUMN 1 PIC 99 SUM WS-AMT RESET ON WS-KEY OF WS-GRP.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
