**2026-11-16, landed (wave 20262068, group A, with PB5767 in the same binder).** Re-probe on the branch build: `addc.cob`
at `--std 85` printed `AA=100 JJ=2 BB=400`. `CorrespondingBinder.Bind` now sets `perPair` for MOVE alone
(`verb is CorrVerb.Move && DialectLevel < 2002`), so ADD and SUBTRACT CORRESPONDING identify their group operands once at
every edition; after the fix `addc.cob` prints `AA=100 JJ=2 BB=200` at 85 and 2023. `docs/conformance/3/D-CORR85.md` is
corrected as the ruling asked (the amended §1985:IV.4.3.8 sentence governs ADD and SUBTRACT; MOVE keeps §1985:VI.6.19.4
GR1). The two cases that pinned the per-pair result are re-derived and renamed for what they now witness:
`conformance:cases/VI.6.6@1985-ADD-STATEMENT/pb5814_add_corresponding_subscript_once_85` (25, was 27) and
`conformance:cases/VI.6.26@1985-SUBTRACT-STATEMENT/pb5814_subtract_corresponding_subscript_once_85` (12, was 14); no
inventory row named the old ids. Rows 1985-GR-VI.6.6.4-3 and 1985-GR-VI.6.26.4-3 gained the witnesses.
