## PB3603 filed: a kb/Work-only landing passes Windows leg 1 and fails the Linux gate (the register drift tests are not tree-wide sweeps)

A kb/Work-only branch passed the Windows leg-1 gate and failed the Linux gate, because the register drift tests (ClosesRowsBackLink, DefectiveRowCoverage) carry no tree-wide sweep trait and no impact-map row reaches them. Measured: 7 of the 8 unit-test classes that read the register or the inventory lack the trait. kb/Work PB3603 carries the structural fix, a drift test that requires the trait on every such class.
