      *>--- conformance-case
      *> id: pb4241_omitted_branch_division_header_at_85
      *> rules: [GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4241 - the refusal below pb4241_omitted_branch_division_header:
      *> the >>IF directive is a COBOL-2002 introduction (7.3.16), so at
      *> --std 85 the same program is refused with COBOL0900 (and nothing
      *> else is wrong with it: the REMARKS paragraph is read as an
      *> ordinary one).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241O85.
000300 PROCEDURE DIVISION.
000400 MAIN-PARA.
000500     PERFORM REMARKS.
000600     STOP RUN.
000700 >>IF PB4241-NEVER IS DEFINED
000800 IDENTIFICATION DIVISION.
000900 >>END-IF
001000 REMARKS.
001100     DISPLAY "IN-REMARKS".
