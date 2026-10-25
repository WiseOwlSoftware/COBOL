## kb/Work: Sites blocks for 7 notes the next waves plan

PB935, PB2923, PB3099, PB3294, PB3600, PB3604 and PB3627 had no `**Sites:**` block, so the wave planner would have planned them as UNPARTITIONED and could not keep two parallel groups off one file. Each now names the files its fix changes and the members in them, checked against the declarations in the tree. PB3600 and PB3604 share `tests/version-matrix/constructs.json` and so the planner will serialize them. PB2985, PB3330 and PB3347 were left unchanged because the notes do not say which file the fix changes.
