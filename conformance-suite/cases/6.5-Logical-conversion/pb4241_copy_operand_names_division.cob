      *>--- conformance-case
      *> id: pb4241_copy_operand_names_division
      *> rules: [GR-6.5-5]
      *> accept: [85]
      *> files: [PB4241RO.cpy, PB4241RP.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4241 - the operands of a COPY or REPLACE statement are
      *> words to match or to write, not text that stands in a division
      *> (7.2.3.3 1): the statement may stand wherever a
      *> character-string may). A pseudo-text operand that spells
      *> PROCEDURE DIVISION therefore leaves the IDENTIFICATION DIVISION
      *> no more than the COPY statement itself does, and the INSTALLATION
      *> and SECURITY paragraphs after it are still COBOL-85
      *> comment-entry paragraphs: their free text, here a quotation mark
      *> that is never closed, is commentary (6.5 5 copies the program
      *> text area to the resultant compilation group, and the comment
      *> entry is the implementor's reading of it, docs/CONFORMANCE.md
      *> D-CE1). The first COPY is written on one line, the second across
      *> two. Leg that can fail: the operand read as a division header
      *> leaves the division, INSTALLATION is read as program text, and
      *> the unclosed literal is refused.
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. PB4241RO.
000300 COPY PB4241RO REPLACING ==PROCEDURE DIVISION== BY ==X==.
000400 INSTALLATION. "AN UNCLOSED QUOTATION MARK, FREE TEXT.
000500 COPY PB4241RP
000600     REPLACING ==PROCEDURE DIVISION== BY ==X==.
000700 REPLACE ==ENVIRONMENT DIVISION== BY ==ENVIRONMENT DIVISION==.
000800 SECURITY. "NONE EITHER.
000900 PROCEDURE DIVISION.
001000 MAIN-PARA.
001100     DISPLAY "OK".
001200     STOP RUN.
