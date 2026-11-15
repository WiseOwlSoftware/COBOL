**Correction (2026-11-15, review R1-2 of PB5313's branch, wave 20262038 group A): the landed Sites paragraph names a file and a member that do not exist.**

`kb/Work/PB5313/2026-11-13-landed.md` line 1, the `Sites (as changed, wave 20262025)` paragraph, names
`src/WiseOwl.Cobol.Frontend/Preprocessor/FixedFormLine.cs` (`AreaBIndex`). The branch did not change `FixedFormLine.cs`, and no member
`AreaBIndex` exists anywhere in the tree (`grep -rn AreaBIndex src` finds none). Read the paragraph as naming the files it lists
before that one (`SourceLineMap.cs`, `AreaPlacement.cs`, `ReferenceFormatMap.cs`, and so on); the `FixedFormLine.cs` entry is a
slip and claims nothing. The landed file is not edited (rule 8); this file is its correction.

**Sites:** none (a correction of a note's prose; changes no code file).
