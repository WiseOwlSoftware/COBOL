## Entry 2804 — 2026-10-30 13:27 PDT — PB1680 — library text is named by its directory entry's spelling, not by the suffix that was probed

**Re-probe.** `COPY gr9dbook.` over `gr9dbook.cpy` holding `01 QX PIC X(0).` on the Windows build printed
`gr9dbook.CPY(1,8): error COBOL2147 …`: `CopyProcessor.LocateInLibrary` returned `spelled + suffix`, and the
case-insensitive `File.Exists` said yes to the first suffix probed (`.CPY`). The note reproduced as filed.

**Fix.** The name a probe returns now comes from the file system. `CompilationInputs.LocateFile` (the ambient-input
gateway, so the probe is still recorded as `FileExists` records it) enumerates the entry by its name with
`MatchCasing.CaseInsensitive` and returns the path with its final component spelled as the directory spells it; where a
case-sensitive file system holds several entries differing only in case, the exactly spelled one is the file. If the
directory cannot be listed the probe spelling stands. `LocateInLibrary` asks it for every probe, so the
already-included set (circular-include detection), the source origin, every library-text diagnostic (COBOL2574 among them) and the line map all
carry `gr9dbook.cpy`. The probe ORDER is untouched: §7.2.3.3 SR3 and the DOC-A.1-40 determination (`.CPY` before `.cpy`,
GnuCOBOL's order) still decide which file is the text; only the spelling of the name returned changed. A directory
component of the path keeps the spelling the user wrote.

**Oracle.** The R0 oracle recorded a manifest `platform` and printed a comparison NOTE only because this defect made the
Windows and Linux diagnostics of 19 NIST continuity cells differ. Both are removed (`capture_oracle.py`,
`compare_oracle.py`, DESIGN-architecture-review.md §4.1); no consumer of the field existed.

**Tests.** `CopybookRealNameTests` (new): `LocateFile` on a mixed-case file and its upper-case probe (asserting by the host's
own case behaviour, so it holds on Windows and Linux), the exact-spelling preference on a case-sensitive file system, and a
COPY by text-name whose origin file must equal the on-disk name ordinally. `SourceLineMapTests.Copy_MapsMainAndCopybookLines`
no longer compares the copybook name ignoring case (the loosening was the symptom). Before the fix the COPY test names
`gr9dbook.CPY` on Windows.

**Rows.** None claimed or closed (`closes_rows: []` with its reason); DOC-A.1-40's text gained the sentence that the found text
is named by its directory entry.

