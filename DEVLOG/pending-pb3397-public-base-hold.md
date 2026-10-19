## File PB3397: the public base skill still holds a same-file predecessor

PB3381 deleted the rolling wave's predecessor hold from the project, but the public base skill pinned at `tools/claude-skills` still implements it (`rolling-wave.js` `held` and `hasSuccessorQueued`) and still teaches "review a held chain once at its tail" in `SKILL.md`, `grouping-fixes.md` and `README.md`. This entry files the lead from PB3381's report as kb/Work PB3397, related to PB3381, PB3380 and PB1699. The fix is a change in the public repository plus a pin move; nothing in the submodule is edited here.
