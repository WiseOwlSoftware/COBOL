## Filed: the spec-reconcile workflow launches its fleet before it knows a page renders (PB6093)

The third 1985 pass's pilot first launched from a worktree without the specs-private submodule. All 20 compare
agents failed at the same render step and read nothing (wf_c47b1440-dd1, 1.02M tokens). PB6093 files the defect:
a preflight render of the first page before the fleet is dispatched, and a render error that names the missing
submodule. Notes only.
