      *>--- conformance-case
      *> id: pb5577-validate-edited-operand-unbuilt
      *> rules: [GR-14.9.50.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:14.9.50.4 GR6 a: "Each elementary data item is checked for compatibility with the PICTURE, SIGN, and
      *> USAGE clauses". The check of an EDITED picture is not built yet (docs/rearchitecture/DESIGN-validate-facility.md
      *> section 6), so VALIDATE of an operand holding one is refused (COBOL7363) and never reported valid.
      *> The statement is the only reason for the refusal; the program is otherwise legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5577E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC-E.
          05 E-ED PIC ZZ9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE REC-E.
           STOP RUN.
