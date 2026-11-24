- **D-USE-OPEN-MODE-BOTH — the USE procedure of an OPEN that fails on a file open in another mode (kb/Work PB5878).** When an
  OPEN fails on a file that is already open in another mode and a USE procedure is declared for each of the two modes, the
  procedure of the mode the file is open in is executed, as GnuCOBOL does; the 1985 text names no order
  (`§1985:XIII.4.8.4 GR5b`, GR5c; `§1985:VII.4.6.4 GR5` in the sequential module and its relative and indexed twins), nor
  does the 2023 text (`§2023:14.9.49.4 GR6`: "for any file open in the output mode or in the process of being opened in
  the output mode"). When only one of the two modes has a procedure, that one runs (kb/Work PB5825). GnuCOBOL's
  `libcob/fileio.c` `cob_open` answers status 41 before it records the attempted mode in `last_open_mode`, by which
  `cobc/codegen.c` `output_error_handler` selects the mode procedure. The same order holds for the WHEN EXCEPTION INPUT |
  OUTPUT | I-O | EXTEND phrases of an exception-checking PERFORM, whose match is the USE rule (`§2023:14.9.28.4 GR17`: "The
  rules for determining a match are specified in General rules 3a to 3g of the USE statement"): where a phrase names each
  of the two modes, the phrase of the mode the file is open in runs, whatever order they are written in (kb/Work PB2723).
  Implemented by `FileConnector.UseModeView`, which both selectors call through `ExceptionSelectionEmitter.OpenModeSelected`.
  Witnesses: `conformance:cases/14.9.49-USE/pb5878_use_both_modes_open_mode_runs` (85, 2014, 2023) and
  `conformance:cases/XIII.4.8@1985-USE-AFTER-STANDARD-EXCEPTION-PROCEDURE-STATEMENT/pb5878-report-file-both-modes-85`
  and, for the WHEN phrases, `conformance:cases/14.9.28-PERFORM/pb2723_when_exception_both_modes_open_mode_runs` (2023).
