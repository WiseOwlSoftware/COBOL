## Entry 3519 — 2026-11-11 18:32 PST — COBOL-85 text manipulation: REPLACE placement and scope, nested COPY, and the word COPY as a REPLACING operand

Three open notes shared one root, the text-manipulation stage reading the 2023 rules at every edition (PB5085, PB5126, PB5277).
Each re-probed on a fresh Debug build before any change: all four repros reproduced (COPY REPLACING COPY BY DISPLAY, a REPLACE after a
literal, nested library COPY, and the REPLACE of the first of two programs rewriting the second, which at 85 gave `REPLACED REPLACED`).

The fixes, each at its one site:

- **A REPLACE not after a separator period** (§1985:XII.3.3 SR1, relaxed by §2002:7.1.3.2 SR1): the new constructs row
  `replace-anywhere-2002` is asked by `CopyProcessor.ApplyReplaceStatements` when `FollowsSeparatorPeriod` says no, so 85 draws COBOL0900.
  Text that came from a library file counts as following the COPY statement's own period.
- **A COPY statement in library text** (§1985:XII.2.4 GR12, §2002:7.1.2.3 GR14): the row `copy-nested-library-text-2002` is asked in
  `ResolveOneCopy` of the library text it read, reported at the outer COPY line. Below 2002 that one report replaces the GR10 report for
  nesting under REPLACING.
- **The word COPY as a REPLACING operand** (§1985:XII.2.3 SR6, §2002:7.1.2.2 SR10, §2014:7.2.2.2 SR10): COBOL7006 from `OperandScreen.CopyWord`
  at 85, 2002 and 2014. 2023 already refuses every word operand (COBOL0902).
- **The scope of a REPLACE** (§1985:XII.3.4 GR3 against §2002:7.1.3.3 GR5): `EditionInfo.ReplaceScopeEndsWithSeparatelyCompiledProgram`
  is the edition fact; below 2002 the text between two REPLACE statements is applied in the pieces the `END PROGRAM` headers that close no
  open program leave, and `ReplaceStates.EndScope` drops every statement in effect. A contained program stays inside the scope.
  `ApplyReplaceStatements` and `ReplaceLineMap` now take the edition as a required argument, so the line map cannot disagree with the pass.

Goldens: `1985-SR-XII_3_3-1`, `pb5085-replace-not-after-period`, `1985-SR-XII_2_3-6`, `1985-GR-XII_2_4-12`, `pb5126_nested_copy_two_levels`,
`pb5277-replace-scope-separately-compiled`, `pb5277-replace-scope-ended`, `pb5277_replace_scope_compilation_group`; `pb1358_replace_anywhere` now
accepts from 2002 (it compiled at 85 only because of this defect). Design: DESIGN-cc-in-copy.md section 3.6. The three notes close no
inventory row (the 1985 inventory has no verdict rows, PB5094).

Review round 1: `OperandScreen.CopyWord` now also bars the compiler directive indicator (`>>`, 2002 and 2014) and the floating debug
indicator (`>>D`, 2002) as word-1/word-2 (§2002:7.1.2.2 SR10, §2014:7.2.2.2 SR10), and applies the 2014 set at 2023 under `--permissive`.
Golden `pb5085-replacing-directive-indicator` (refuse 2002, 2014), unit test `CopyReplacingBarredWordTests`. The landing paragraphs of the three
notes moved to `kb/Work/<id>/2026-11-12-landed.md` (rule 8).

