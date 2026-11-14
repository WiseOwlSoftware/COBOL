- **D-RD-CODE-REFMOD — RD CODE reference modification (§2023:13.18.12.3 SR2, §2023:13.18.12.4 GR2; kb/Work PB1292)**: identifier-1 is an identifier,
  so it may be subscripted or reference-modified (a reference-modified identifier references the slice, an alphanumeric
  item, §2023:8.4.3.3.4 GR6 c)). GR2 charges the code's characters to the logical record size, which `RECORD CONTAINS` and the
  report's line width need as a NUMBER at compile time, and SR2 excludes the dynamic-length shapes for exactly that
  reason. Determination: a reference-modified CODE identifier whose leftmost-position and length are not both integer
  literals (the length may be omitted) is refused under SR2's "dynamic-length elementary item" (COBOL2713), the same
  literal-position rule the CONTROL operand's reference modification takes (§2023:13.18.16.3 SR4); the standard states no
  outcome for a code whose length varies. The choice is the implementor's (CLAUDE.md rule 1); no GnuCOBOL behavior was
  measured for it.
