      *>--- conformance-case
      *> id: pb5910-type-operand-subscripted-85
      *> rules: [1985-FMT-XIII.3.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5910. §1985:III.2: a 'data-name' "must not be reference-modified,
      *>   subscripted, or qualified unless specifically permitted by the rules of
      *>   the format", and no 1985 rule permits a subscripted control name. The
      *>   CONTROL operand is plain WS-KEY; the CH operand is WS-KEY (1). Expected:
      *>   COBOL0899 at the TYPE CH operand, the refusal every edition gives a
      *>   subscripted TYPE operand (no edition permits it). Fails if the subscripted
      *>   TYPE operand is accepted at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5910S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5910S.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          02 WS-KEY PIC 9 VALUE 1.
       REPORT SECTION.
       RD R1 CONTROL IS WS-KEY.
       01 CH1 TYPE CH WS-KEY (1) LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH1".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-KEY.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
