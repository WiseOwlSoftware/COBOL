## PB3032: the orchestrate self-tests' meter leak hung two gates for hours

The gates of `worktree-wf_f0711751-f32-1` (PB4995) and `dispatch-guard-rule0` each reported 13 orchestrate self-tests
HUNG. In the first, every Unit, Conformance and Characterization test had passed, and gate slot 2 was held from about
05:00 (after a 3,252 s wait for it) to 07:58. The fake-claude supervisors took their lock and kept ticking their lanes, but never launched a unit. That is the
`budget.py` hold PB5113 found in the account test, and PB5113's sibling sweep had predicted it for the others. Neither
tree carried PB5098, so the runner waited 13,860 s after reporting each test stopped. The operator resume unit stopped the
26 orphaned supervisors and both gates returned RED. PB3032 now records this recurrence, is raised to MAJOR, and names the
fix: the same temp-HOME isolation PB5113 gave the account test, applied in the shared `Run-Orch` helper.
