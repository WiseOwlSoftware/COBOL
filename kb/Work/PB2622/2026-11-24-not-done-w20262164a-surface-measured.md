# PB2622: not done in wave 20262164 group A either; the surface, re-measured

Wave 20262164 group A (branch `worktree-wf_38508f28-551-1`) was dispatched with PB2622 bundled again with PB2627,
PB2724, PB3227 and PB3231, after wave 20262161 group A had recorded (`2026-11-23-split-from-group-a-w20262161.md`)
that PB2622 is a mechanism of its own. This wave carried the finished PB2627 and PB2724 work onto current main,
gated it and returned it for landing. It did not start PB2622 (one mechanism per implementer).

Measured on the tree of 2026-11-24 (merge of origin/main 32bcb7b79a), not on the note's pin cd85e4901:

- `grep -rhoE "DiagnosticCatalog\.[A-Z][A-Za-z0-9]*," src --include=*.cs`: 1467 call expressions that pass a
  descriptor and a message, over 678 distinct descriptors, in 149 files. The note measured 862 over 517 at its pin.
- 3283 non-comment source lines in `src/**/*.cs` hold the text `ISO §`.

So the migration is about 1.7 times the size the note measured. It needs the generator and the amended
DESIGN-architecture-review §8.3 (2) first, as one dispatch of its own, and then the emit-site migration in
file-set slices (for example by binder directory), each slice gated against the golden byte image. One fix-lane
implementer cannot do the design, the generator and 1467 sites within a 400-turn cap.
