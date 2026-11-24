## Entry 4048 — 2026-11-24 11:03 PST — R2 restarts on a new pin: the census at 9e65164eb, and the inputs tool builds the projects the solution actually has

R2's remaining batches run on a new pin, main at 9e65164eb (kb/Work PB2911, the owner's release of 2026-11-24).

**The pin's evidence** (`docs/rearchitecture/evidence/arch-census/9e65164eb...`):
- `census.py` recorded 3,331 types (2,666 authored), 77 god-class candidates, 12 clone families, 98 unreachable and 144
  test-only members, 532 dead artifacts and 774 findings.
- `member_index.py --record` wrote the member index the inputs need.
- `r2_subsystems.py --out` split the tree into 146 shards over 2,849 files.

**The inputs tool failed on the new pin twice; both causes are fixed at the root (kb/Work PB6171):**
1. The analyzer step built projects from a hand-written `PROJECT_ORDER` that lacked `WiseOwl.Cobol.Analyzers` and
   `WiseOwl.Cobol.Listing`. `project_order` now computes the order from `WiseOwl.Cobol.sln` and the projects'
   `<ProjectReference>`s. It refuses a cycle, and its self-test plants a solution with an unlisted analyzer project.
2. The warm-up built the solution. There, MSBuild gives the analyzer reference the solution's configuration (Debug),
   while the step's project-alone builds honour `SetConfiguration=Release` and read `bin/Release`. The warm-up now
   builds each project alone. This was reproduced in a fresh worktree before the fix.

**After the fixes:** `r2_inputs.py` wrote 146 shard inputs with 0 pending (semgrep 8 files, analyzers 1,769, drift
1,280). `r2_inputs.py --self-test` passes.

