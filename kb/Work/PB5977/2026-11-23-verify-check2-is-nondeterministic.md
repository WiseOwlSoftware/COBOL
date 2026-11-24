**2026-11-23, lead filed by the enqueue clerk (batch 20261123c).** Source: `E:/COBOL-coord/scratch/reports/w20262147b-PB2652-report.md`, New defects 1.

`scripts/semgrep/verify.py` CHECK 2 (WHOLE_FILE_UNREADABLE) is nondeterministic on an unchanged tree: two runs reported 6 and 5 different
"unreadable at all" files (`ObjectPropertyResolver.cs`, `PtrEmitter.cs`, `SequentialConnector.cs`, `DataBinder.ClausePlacement.cs`, ...) and
FAILed, a third run PASSed. The set differs from the files this note names, and varies run to run, which points to semgrep timeouts under host
load rather than a parse limit. A fix that lists files in WHOLE_FILE_UNREADABLE (this note's approach) cannot hold: the verifier must tell a
timeout from an unparseable file (re-run the file alone, or read semgrep's own error) before it counts a file as unreadable.
