      *>--- conformance-case
      *> id: pb5910-type-operand-qualified-85
      *> rules: [1985-FMT-XIII.3.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5910. The TYPE clause's CONTROL HEADING / CONTROL FOOTING operands
      *>   are data-name-1 and data-name-2 of its general format, and §1985:III.2
      *>   says a 'data-name' "must not be reference-modified, subscripted, or
      *>   qualified unless specifically permitted by the rules of the format". The
      *>   printed TYPE clause (folio XIII-55) has no such permission; its SR4 asks
      *>   only that the operand "must be specified in the CONTROL clause of the
      *>   corresponding report description entry". ISO/IEC 1989:2002 13.16.55.2
      *>   SR10 first permits it: "Data-name-1 and data-name-2 may be qualified and
      *>   reference modified". The CONTROL operand is plain WS-KEY; the CH operand is
      *>   qualified with OF and the CF operand with IN. Expected: COBOL0900 at 85 at
      *>   each qualified TYPE operand. Fails if the TYPE operand's qualifier is not
      *>   asked at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5910Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5910Q.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          02 WS-KEY PIC 9 VALUE 1.
       REPORT SECTION.
       RD R1 CONTROL IS WS-KEY.
       01 CH1 TYPE CH WS-KEY OF WS-GRP LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH1".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-KEY.
       01 CF1 TYPE CF WS-KEY IN WS-GRP LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CF1".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
