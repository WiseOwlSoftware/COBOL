- **D-USE-OPEN-MODE-BOTH — the USE procedure of an OPEN that fails on a file open in another mode (kb/Work PB5878).** When an
  OPEN fails on a file that is already open in another mode and a USE procedure is declared for each of the two modes, the
  procedure of the mode the file is open in is executed, as GnuCOBOL does; the 1985 text names no order
  (`§1985:XIII.4.8.4 GR5b`, GR5c; `§1985:VII.4.6.4 GR5` in the sequential module and its relative and indexed twins), nor
  does the 2023 text (`§2023:14.9.49.4 GR6`: "for any file open in the output mode or in the process of being opened in
  the output mode"). When only one of the two modes has a procedure, that one runs (kb/Work PB5825). GnuCOBOL's
  `libcob/fileio.c` `cob_open` answers status 41 before it records the attempted mode in `last_open_mode`, by which
  `cobc/codegen.c` `output_error_handler` selects the mode procedure. Implemented by `FileConnector.UseModeView`.
  Witnesses: `conformance:cases/14.9.49-USE/pb5878_use_both_modes_open_mode_runs` (85, 2014, 2023) and
  `conformance:cases/XIII.4.8@1985-USE-AFTER-STANDARD-EXCEPTION-PROCEDURE-STATEMENT/pb5878-report-file-both-modes-85`.
