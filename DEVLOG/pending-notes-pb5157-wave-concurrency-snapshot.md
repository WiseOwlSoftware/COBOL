## PB5157 filed: a wave's concurrency is read from the gate pool once and never again

Wave 20261983 was planned at 2 implementers because another lane's gate held a slot at 10:23, and it kept that width for 110
minutes while all three slots sat free and six groups queued. The merge queue stood empty because the fix lane produced nothing to
land. PB5157 records the defect in PB5100's derivation: the width is a snapshot of a value that moves.
