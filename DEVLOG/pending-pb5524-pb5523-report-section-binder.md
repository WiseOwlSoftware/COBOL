## PB5524 and PB5523: an ambiguous UPON detail stops the bind, and CODE ALL refuses a national symbolic character

**PB5524.** Re-probe: two reports sharing detail `DET`, RPT declared first, `UPON DET` in RPT2's control footing, gave COBOL1920 and then a cascading COBOL2046 at 85, and the hint suggested `DET OF RPT`, the other report whose detail SR2 refuses. `ResolveUponDetail` now returns at `Match.Ambiguous` (the diagnostic is already reported) and `ReportGroupResolution.Resolve` takes a `preferred` report so the example qualifier names the control footing's own report. The note said SR2 forbids a qualifier at 85; the text allows "a report-name" qualifier, so the hint was wrong only in which report it named. Golden `pb5524_upon_ambiguous_detail_other_report_first` (refused at all four editions, hint `'DET OF RPT2'`). GENERATE and USE BEFORE REPORTING also continue with the first candidate of an ambiguous match; reported as a lead.

**PB5523.** Re-probe: `CODE ALL SYM` with `SYMBOLIC CHARACTERS FOR NATIONAL SYM IS 66` compiled. `CodeFigurativeCharacters` reads `SymbolicOf` and refuses a national symbol with COBOL2713 (§2023:13.18.12.3 SR1, §2023:12.3.7.4 GR11c). Golden `pb5523_code_all_national_symbolic_character` (refused 2002 to 2023).

Rows re-verdicted: none (both notes claimed none). Notes flipped to landed.
