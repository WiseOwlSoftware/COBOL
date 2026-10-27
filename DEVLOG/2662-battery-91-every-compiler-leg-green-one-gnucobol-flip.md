## Entry 2662 — 2026-10-27 12:06 PDT — Battery #91: every compiler leg green; one GnuCOBOL flip, PERFORM VARYING without BY at 85, ruled expected

The owner asked for the comprehensive battery (11:15, relayed by the Mythos lane): the last one, #90, was 4,319 commits
back. The operator ran battery #91 on main `0fc3b47c8`, covering every landing since #90 (DEVLOG 2051-2658), in a fresh
detached worktree with a cold compile cache. It took 2,184 s of wall time beside the loop's running wave. Every compiler
leg is green. Conformance ran 19,067 of 19,067, Unit 38,113 with no failures, and characterization 36 of 36. The NIST
guard reads 362 MATCH and 0 REGRESSION, and every static audit is clean.

The GnuCOBOL differential found one per-case flip. `run_misc:2376` is a PERFORM VARYING with no BY phrase, pinned to
`-std=cobol85`, where GnuCOBOL refuses it. At #90 our compiler accepted it at `--std 85`; now it refuses it with
COBOL0900. That is landed PB2282's edition gate: the BY phrase is omissible only from 2002 (§14.9.28.4 GR12, checked;
VCR row 7.42). The battery's build refuses the program at 85, compiles it at 2002 and 2023, and the run prints `XXX`,
the case's own expected output. Only the battery runs the external differential, so the committed baseline kept #90's
row until now.

The flip is ruled expected (kb/Work PB3871; the Mythos lane checked row 7.42 on its own and agreed). The per-case
baseline is re-recorded from this run's report with exactly that row and its `# Last recorded:` line moved, and plan
§0's CURRENT battery reference is #91. The script's own verdict was NOT GREEN on the flip alone.

