- **D-IO-STATUS-37-AUTHORITY — I-O status '37' insufficient authority on OPEN/DELETE FILE (§2023:9.1.13.6 item 6b; E.2 item 18)**: emitted at all
  editions. The spec permits it ("may") and marks detection processor-dependent; E.2 item 18 is a clarification
  that it is allowed, not a 2023 introduction — so it is NOT gated/suppressed below 2023. **Detection is a
  three-state presence probe, not an exception map alone.** `HostFile.Probe` (`Runtime/IO/FileSupport.cs`) asks
  the host with `File.GetAttributes`, whose failures distinguish a missing file
  (`FileNotFoundException`/`DirectoryNotFoundException` → §2023:9.1.13.6 item 5's '35') from a refused one
  (`UnauthorizedAccessException` → GR3's '37'); `File.Exists` collapses both to `false` and is banned throughout
  `Runtime/IO` for that reason (`HostFileProbeDriftTests`). `FileConnector.Open` answers GR3 from the probe
  for INPUT/I-O/EXTEND — OUTPUT is excluded because GR18 makes it a creation that never consults presence, so a
  writable-but-unlistable directory still accepts a new file — and `FileRegistry.DeleteFile` takes the same probe
  for §2023:14.9.10.4 GR14/GR16. Any refusal the probe cannot see still maps from `UnauthorizedAccessException` in
  `Open`'s `catch`. Witnesses: `unit:FileAuthorityPresenceTests` (a real DACL/mode denial — the corpus runner has
  no way to express the precondition) and `conformance:cases/9.1.13.6-Permanent-error-condition-with-unsuccessful-completion/pb323_open_presence_arms` for the probe's other two
  answers (kb/Work PB323).
