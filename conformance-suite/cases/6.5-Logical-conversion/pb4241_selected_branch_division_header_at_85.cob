      *>--- conformance-case
      *> id: pb4241_selected_branch_division_header_at_85
      *> rules: [GR-6.5-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4241 - the refusal below
      *> pb4241_selected_branch_division_header: the >>DEFINE and >>IF
      *> directives are COBOL-2002 introductions (7.3.11, 7.3.16), so at
      *> --std 85 the same program is refused with COBOL0900 (and nothing
      *> else is wrong with it: the REMARKS paragraph is read as an
      *> ordinary one).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241S85.
000300 >>DEFINE PB4241-SEL AS 1
000400 >>IF PB4241-SEL IS DEFINED
000500 PROCEDURE DIVISION.
000600 MAIN-PARA.
000700     PERFORM REMARKS.
000800     STOP RUN.
000900 >>END-IF
001000 REMARKS.
001100     DISPLAY "IN-REMARKS".
