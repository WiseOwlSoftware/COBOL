## Review round 1 of `listing-step-pb5941`: how each finding was fixed

- R1-1 (BLOCKING, `--listing-output -` untested): fixed at the root. The untested path was also wrong. `Program.RunCompile` wrote the
  listing's UTF-8 bytes to `Console.OpenStandardOutput()`, past the process's standard output writer, on the claim that `Console.Out`
  re-encodes through the OEM code page. That claim was false: since PB899, `StandardStreams.EnsureUtf8` gives a redirected `Console.Out`
  a UTF-8 writer. Writing the probe test proved the claim false, because the reverted `Console.Out` path still passed a redirected
  cp437 byte test. The bypass was the actual defect. On a Windows console, `EnsureUtf8` sets the standard output encoding to UTF-16, so
  the raw stream read the listing's UTF-8 as UTF-16 and showed CJK characters for the whole listing. The new pseudoconsole witness
  measured this on a code page 437 console: 2 red on the branch head. The fix: `ListingOutput.Write(format, document, request,
  TextWriter)`, a character entry of the one format table. Text and HTML write to the writer directly. JSON's serializer bytes are
  decoded into the writer as they arrive (`CharacterStream`, private, a UTF-8 `Decoder`), so the output is still streamed. The `Stream`
  entry builds on it. The command line writes through `Console.Out`, so `EnsureUtf8` is again the one rule for standard output. The
  `Console.Out.Flush()` is gone, because the listing and the DISPLAY lines go through one writer, in order.
  Witnesses:
  - `ListingCliTests.ToStandardOutput_TheListingIsUtf8_AndTheDisplayLinesFollowIt`, for text, json and html. It runs the command as a
    process with output redirected under a cp437 console and checks strict UTF-8, no BOM, the non-ASCII comment, and the DISPLAY line
    last. It went 3 red when the listing was moved after the DISPLAY loop.
  - `WindowsConsoleDeviceTests.The_cobol_commands_listing_on_the_console_shows_its_characters_as_themselves`, for text and html. It was
    2 red on the bypass and is green after the fix.
  - `ListingOutputTests.EveryFormat_WrittenAsCharacters_IsWhatItsFileHolds`, for every format.

  JSON is not in the console witness: the document's serializer escapes non-ASCII, so its text is ASCII on any device.
  Timing: IX113A's `all` listing through redirected stdout took 4.09 to 5.13 s in 3 runs, against 3.65 to 7.06 s for the file. The
  bytes match except for the options row's 2-character destination name.
- Sibling sweep: the cobol command's path was spelled in three tests (`CliDiagnosticEncodingTests`,
  `ConformanceSuiteRunnerDriftTests`, `WindowsConsoleDeviceTests`). It is now `TestRepo.CobolCommand`, the one locator. PB899's
  redirected cp437 runner is now `OemConsoleCli`, shared by its test and the new listing test, and its private `IndexOf` became a span
  search. `git grep "OpenStandardOutput|OpenStandardError" -- src` finds no other raw-handle writer outside `StandardStreams`.
- Design corrections in DESIGN-listing.md, marked "PB5941 review R1-1": the banner and sections 4.2 (the character entry), 8 (the
  path, and the earlier claim corrected) and 10 item 6 (the witnesses). The user manual's listing page now covers both cases:
  redirected output and a console.
- Leads: round 1 recorded none (`review_findings.py show` lists the one finding).
