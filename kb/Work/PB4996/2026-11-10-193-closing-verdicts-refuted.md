**2026-11-10, the 213 waiting closing verdicts refuted; 193 recorded (branch records-85).**

The operator ran one Opus `cobol-refuter` per verdict over the 213 closing verdicts of code-read slices 01-35 that no
refuter had attacked (20 batches, `E:/COBOL-coord/scratch/refute-85/out/r01.json` to `r20.json`; 2.9M tokens on account 2).
Each refuter had to probe every class that applied: variable length (OCCURS DEPENDING ON in the record, RECORD VARYING,
reference modification at its bounds); the refusal half (each lettered item violated in turn, degenerate forms); edition
arms (a later edition's phrase, operand class or phrase order the 85 compiler must refuse); groups against elementary
items, figurative constants, ALL literals, scaled, signed and edited items; every organization and access mode and the
statement after a failed OPEN or READ; zero, 18 digits, negative, size error.

Result: 193 UPHELD, 15 OVERTURNED, 5 DOUBTFUL. With the earlier sample (24 attacked, 2 overturned) the code read's
CONFORMS verdicts were wrong about 7 times in 100 before refutation.

Recorded here: the 193 upheld (`coderead-85/record-upheld.json`; `record_verdicts.py` printed GAP 882 to 689). Not
recorded: the 15 overturned and 5 doubtful (`coderead-85/held-2.json`), which with the 7 of `held.json` go to the register
as defects or adjudications (the operator files them with the 113 of PB5158-PB5203).

Known limit of this refutation: a canary on a sample with two known overturns found one of them with Opus; the attack
classes were added to the brief after that. A random 15 of the 193 are still owed a second attack on Mythos.
