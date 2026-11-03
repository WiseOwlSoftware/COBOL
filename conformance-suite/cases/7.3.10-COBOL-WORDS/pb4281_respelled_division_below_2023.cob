      *>--- conformance-case
      *> id: pb4281_respelled_division_below_2023
      *> rules: [GR-7.3.10.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4281 - the refusal below
      *> pb4281_respelled_division_is_not_a_comment_entry: the
      *> COBOL-WORDS directive is a COBOL-2023 introduction (7.3.10), so
      *> at --std 85, 2002 and 2014 the same program is refused with
      *> COBOL0900.
000100 >>COBOL-WORDS EQUATE "DIVISION" WITH "DIV"
000200 IDENTIFICATION DIV.
000300 PROGRAM-ID. PB4281EB.
000400 PROCEDURE DIV.
000500 MAIN-PARA.
000600     PERFORM REMARKS.
000700     STOP RUN.
000800 REMARKS. DISPLAY "IN-REMARKS".
