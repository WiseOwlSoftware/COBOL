## Entry 2755 — 2026-10-29 18:13 PDT — Filed the wide-argument consumer defect as PB4075 and retired the duplicate PB4009

The PB3953 report listed two new defects: MEAN of a wide argument is off in the 34th digit, and the binary64 family (SIN and its kin) reads a wide argument's 34-digit image. Both come from one mechanism, so they went into one note, PB4075: a function argument wider than 38 digits arrives at its consumers as the round-to-odd image, and each consumer needs the exact value instead. The note carries both runnable repros and the code sites, and PB4076 was not used.

PB4009 was retired as a duplicate of the landed PB4005. Both described the rendered implementer spec naming a one-file root twice, which PB3777's once-check refuses, and PB4005 landed the fix.

