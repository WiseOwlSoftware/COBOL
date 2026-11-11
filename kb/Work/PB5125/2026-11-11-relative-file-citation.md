# 2026-11-11 - the 1985 rule this note's Mechanism cites for status 24 is the relative-file one, §1985:VIII.1.3.4

The Mechanism paragraph above cites `§1985:VIII.4.9.4 GR14 b` for "a write beyond the externally defined boundaries is
status 24 and an invalid key condition". That locator is the sequential-file rule. The relative-file rule is
`§1985:VIII.1.3.4` item (3) c ("I-O Status = 24. An attempt is made to write beyond the externally defined boundaries
of a relative file"), which this note's own inventory row `1985-GR-VIII.1.3.4-L2.3` already quotes and `cite.py --check`
confirms. Found by the finisher of worktree-wf_7e916c7a-be9-1 (report w20261999a-PB5125, head caec16a9ac); the branch's
DEVLOG entry already cites §1985:VIII.1.3.4.
