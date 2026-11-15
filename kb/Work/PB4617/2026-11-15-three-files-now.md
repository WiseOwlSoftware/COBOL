**2026-11-15: reported again, and the unreadable set is now three files.** Four more reports carry the same lead:
`w20262040d-PB5572-report.md` (New defects 3), `w20262039g-PB5147-report.md` (New defects 1), `w20262039b-PB5529-report.md`
and `w20262039h-PB5361-report.md` (its verify.py line). `python scripts/semgrep/verify.py` FAILs CHECK 2 on main with
"3 unreadable file(s) unnamed": `src/WiseOwl.Cobol.Compiler/Binding/IntrinsicArgumentRules.cs`,
`src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs` and
`src/WiseOwl.Cobol.Editions/Diagnostics/DiagnosticCatalog.cs` (this note's file). CHECK 3 (the counts) does not run behind
it. PB5572's report shows the base tree's `VersionConformancePass.cs` failing identically, so the branches under which the
FAIL was seen did not cause it:

```
semgrep scan --pattern 'class $C { ... }' --lang csharp <file>   ->   Syntax error at line 1
```

(`git show ed86914443:src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs`). It is the whole-file mechanism PB4970 and this note describe, now seen in three files. Fix per this note's direction: find the construct that breaks the parse in each file (a
semgrep C# grammar gap or a timeout under the whole-tree run, PB4970's reading), or name each in `WHOLE_FILE_UNREADABLE`
(`scripts/semgrep/verify.py`) with its first failing member and reason. Sites stay as this note lists them; add
`src/WiseOwl.Cobol.Compiler/Binding/IntrinsicArgumentRules.cs` and
`src/WiseOwl.Cobol.Compiler/Validation/VersionConformancePass.cs` to the files whose parse is repaired if the grammar gap
is fixed in the sources.
