## A third unreadable file, and the same failure on four more branches (2026-11-22)

Filed by the enqueue unit's lead clerk. The reports of the PB5801, PB5575, PB5775, PB5768, PB5733, PB5615 and PB5795 branches each
record `python scripts/semgrep/verify.py` FAILing on main for files that are not named in `WHOLE_FILE_UNREADABLE`. All name
`IntrinsicArgumentRules.cs` and `VersionConformancePass.cs`; the PB5795 report (w20262091a, base e04520c9e6) adds:

> `DiagnosticCatalog.cs` fails only inside the full scan (alone it parses, base and branch): `FAIL: 3 unreadable file(s) unnamed`.
> The base copy of `VersionConformancePass.cs` scanned alone: Syntax error.

So the cause is not only a construct the parser lacks (two files fail alone) but also, for a third, a load or whole-scan
dependence (the PB5502 pattern: it passes on one run and fails on another). A fix that only names the files in
`WHOLE_FILE_UNREADABLE` leaves `DiagnosticCatalog.cs` flapping; find why it fails in the full scan (timeout, memory, the file's
size under parallel scans) first. kb/Work PB5502 carries the sizes.
