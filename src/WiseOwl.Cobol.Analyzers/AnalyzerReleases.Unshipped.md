; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
WOCA0001 | WiseOwl.Editions | Error | EditionCiteAnalyzer: a string chosen by edition outside EditionInfo.Cite
WOCA0002 | WiseOwl.Editions | Error | EditionCiteAnalyzer: an edition-derived member another assembly can read is not marked [EditionDerived]
WOCA0004 | WiseOwl.Editions | Error | EditionCiteSlotAnalyzer: a citation written for an edition is not the one EditionInfo.Cite chooses at that edition
