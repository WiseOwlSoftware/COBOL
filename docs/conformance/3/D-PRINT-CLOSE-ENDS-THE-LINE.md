- **D-PRINT-CLOSE-ENDS-THE-LINE — the CLOSE of a record sequential print file ends the line a WRITE AFTER ADVANCING left
  open (kb/Work PB5683).** This entry speaks for COBOL-85 and later, beside `docs/conformance/7/DOC-A.1-146.md`'s account
  of how lines are advanced. When a record sequential print file is closed after a WRITE with the AFTER ADVANCING phrase,
  the CLOSE ends the line that WRITE left open (one line feed). A file then opened EXTEND begins on a new line, so its
  first WRITE AFTER ADVANCING 1 LINE leaves one empty line after the last line of the earlier OPEN. The 1985 text fixes
  the order of the records (`§1985:VII.4.7.4 GR13`) and leaves the form of the printed page to the implementor
  (`§1985:VII.4.7.4 GR16`); GnuCOBOL does the same (`libcob/fileio.c`: `sequential_write` and `cob_file_close`). Source:
  `kb/Work/PB5683/2026-11-16-ruling-extend-after-close.md` (row `1985-GR-VII.4.7.4-13`). Witness:
  `conformance:cases/VII.4.7@1985-WRITE-STATEMENT/pb5683_print_close_ends_the_line_85` (its declared output file, compared
  with LF line ends, holds an empty first line, AA, BB, an empty line, CC).
