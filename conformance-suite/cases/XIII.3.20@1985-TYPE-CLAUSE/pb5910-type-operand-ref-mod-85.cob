      *>--- conformance-case
      *> id: pb5910-type-operand-ref-mod-85
      *> rules: [1985-FMT-XIII.3.20.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5910. §1985:III.2: a 'data-name' "must not be reference-modified,
      *>   subscripted, or qualified unless specifically permitted by the rules of
      *>   the format"; ISO/IEC 1989:2002 13.16.55.2 SR10 first permits the TYPE
      *>   operand's reference modification. The operands are unqualified, so only
      *>   the modifier is asked: CONTROL IS WS-KEY (1:1) and TYPE CH WS-KEY (1:1),
      *>   the same operand as SR4 asks. Expected: COBOL0900 at 85 at the TYPE CH
      *>   operand (the CONTROL operand's refusal is pb5573-control-clause-ref-mod-85;
      *>   the qualified spelling is pb5573-type-control-ref-mod-85). Fails if an
      *>   unqualified reference-modified TYPE operand is not asked at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5910R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5910R.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          02 WS-KEY PIC X(2) VALUE "AB".
       REPORT SECTION.
       RD R1 CONTROL IS WS-KEY (1:1).
       01 CH1 TYPE CH WS-KEY (1:1) LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "CH1".
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(2) SOURCE WS-KEY.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
