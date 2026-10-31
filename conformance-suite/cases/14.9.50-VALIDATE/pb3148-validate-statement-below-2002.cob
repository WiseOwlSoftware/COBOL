      *>--- conformance-case
      *> id: pb3148-validate-statement-below-2002
      *> rules: [FMT-14.9.50.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3148 - the VALIDATE statement (ISO 14.9.50) is a
      *> COBOL-2002 introduction: 8.9 reserves the word VALIDATE from
      *> 2002, and COBOL-85 has no such statement. At --std 85 the
      *> statement is therefore refused BY NAME, the edition diagnostic
      *> COBOL0900 naming its introduction, as every other post-85
      *> statement is (UNLOCK, ALLOCATE, INVOKE), and with no COBOL1580
      *> facility warning beside it: the facility is not COBOL-85's to
      *> decline. It drew a bare parse error at its operand
      *> (COBOL0307 near 'WS-REC') when the statement arm asked whether
      *> the word was reserved. The accepted control at 2002 and later is
      *> wiseowl/cases/14.9.50-VALIDATE/pb3568-validate-legal-operands-inert.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3148VB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           VALIDATE WS-REC.
           STOP RUN.
