      *>--- conformance-case
      *> id: pb5910_reset_plain_qualified_control_85
      *> rules: [1985-FMT-XIII.3.19.2, 1985-SR-XIII.3.19.3-2, 1985-SR-XIII.3.19.3-4, 1985-SR-XIII.4.3.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5910 (review R1-1). The 1985 report-writer references that MAY be
      *>   qualified, each beside a RESET operand that may not and is written plain:
      *>   - SUM's UPON operand: §1985:XIII.3.19.3 SR2, "Data-name-1 may be qualified
      *>     by a report-name" (UPON D1 OF R1);
      *>   - GENERATE's operand: §1985:XIII.4.3.3 SR1, "Data-name-1 must name a type
      *>     detail report group and may be qualified by a report-name" (GENERATE D1
      *>     OF R1);
      *>   - the SUM addend and the SOURCE operand are identifiers, and §1985:III.2
      *>     says "an identifier consists of a data-name, together with its
      *>     qualifiers, subscripts, and reference-modifier, as required for
      *>     uniqueness of reference" (WS-AMT OF WS-REC; WS-AMT is declared twice);
      *>   - the CONTROL operand: §1985:XIII.3.7.3 SR1, "Data-name-1 may be
      *>     qualified" (WS-KEY OF WS-REC).
      *>   RESET ON WS-KEY is plain (§1985:XIII.3.19.3 SR4 permits no qualifier).
      *> DERIVATION. S1 is the sum counter; DISPLAY shows it with its operational sign
      *>   (§1985:XIII.3.19.4 GR1: "The sum counter is a compiler-generated numeric
      *>   data item with an operational sign"). §1985:XIII.3.19.4 GR9c: "If the SUM
      *>   clause contains the UPON phrase, the addends are subtotalled when a
      *>   GENERATE statement for the designated detail report group is executed".
      *>   GR12: "If an explicit RESET phrase is specified, then the report writer
      *>   control system will set the sum counter to zero at the time that the
      *>   report writer control system is processing the designated level of the
      *>   control hierarchy". CF-USE displays the prior control values
      *>   (§1985:XIII.3.20.4 GR13a).
      *>   GENERATE 1 (KEY 1, SUB 1): no break; S1 = 3.
      *>   GENERATE 2 (SUB 2): a minor break, CF1 presented: CF 1 1 +03; the WS-SUB
      *>     level is not the RESET level, so S1 stays 3; then S1 = 6.
      *>   GENERATE 3 (KEY 2): a major break; CF1 (the minor level is processed with
      *>     it, §1985:XIII.3.20.4 GR6): CF 1 2 +06; the WS-KEY level resets S1 to 0;
      *>     then S1 = 3.
      *>   TERMINATE: every control footing: CF 2 2 +03.
      *> Fails if a permitted qualifier is refused at 85, or the RESET level is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5910RP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5910RP.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-REC.
          02 WS-KEY PIC 9 VALUE 1.
          02 WS-SUB PIC 9 VALUE 1.
          02 WS-AMT PIC 9 VALUE 3.
       01 WS-OTHER.
          02 WS-AMT PIC 9 VALUE 7.
       REPORT SECTION.
       RD R1 CONTROLS ARE WS-KEY OF WS-REC WS-SUB.
       01 D1 TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC 9 SOURCE WS-AMT OF WS-REC.
       01 CF1 TYPE CF WS-SUB LINE PLUS 1.
          02 S1 COLUMN 1 PIC 99 SUM WS-AMT OF WS-REC UPON D1 OF R1
                RESET ON WS-KEY.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CF-USE SECTION.
           USE BEFORE REPORTING CF1.
       CF-USE-P.
           DISPLAY "CF " WS-KEY " " WS-SUB " " S1.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           GENERATE D1 OF R1.
           MOVE 2 TO WS-SUB.
           GENERATE D1 OF R1.
           MOVE 2 TO WS-KEY.
           GENERATE D1 OF R1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
