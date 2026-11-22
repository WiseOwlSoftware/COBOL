## The construct rows' citations are canonicalized at their source (train-1985-c, group m2)

`audit_code_citations.py` reads the GENERATED `src/WiseOwl.Cobol.Editions/ConstructRegistry.g.cs` but not its input,
`tests/version-matrix/constructs/<id>.json`. So `--canonicalize src/ tests/` rewrote the generated file and left the
rows unchanged, and the next `gen-constructs.ps1` (which the train ran after group t) wrote the old designators back.
The gate's CITATIONS audit then refused 13 lines of the .g.cs. On this train the 13 rows were rewritten at their
source to the designators the audit names (for example `§2023:13.18.60.4 GR22 d)2.` became `GR22d.2`, and
`§2023:15.28.3 rule 1` became `AR1`). The registry was then regenerated, and the audit reads `0 finding(s)`.

**Owed (open, this note):** the canonicalizer and the audit should read a generator's input rather than its output,
or the audit should refuse a generated file whose input it does not read. A citation in a generated file should be
fixed where it is written. Until then, `--canonicalize` over `src/` silently edits generated files.
