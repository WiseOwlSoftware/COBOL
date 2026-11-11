## Entry 3450 — 2026-11-10 16:44 PST — PB5207: the build process is redesigned top-down as one incremental build (decision, approved by the owner)

**Why.** The owner asked for a redesign of the build process as a whole and had received piecemeal fixes: "This is
precisely what the Unix make and Windows MSBuild utilities are for. They rerun only what is required due to changes."

**What was measured first.** The path from a changed file to verified-on-main is 142 files of 73,302 lines, tested by
172 self-tests of 98,181 lines, beside a compiler of 281,000 lines. Script self-tests take 64 % of local gate
wall-clock; four hung gates cost 31,115 s; 41 % of pull requests ran two or more gates. Agents were refused 1,095 times
in three days, 77 % of them by two hooks that parse shell text. The Unit assembly reads nearly the whole tree.

**The decision (kb/Work PB5207).** One graph of targets, each with declared inputs and a key that is a digest of them;
one result store shared by every worktree and read by CI; a target runs only when the store holds no pass at its key.
Tests split by what they read; self-tests run against fixtures with declared time bounds; one command (`check`, `why`,
`land`); a landing without hand-made artifacts; shell-parsing guards replaced by construction. Six stages, each landing
on its own and deleting what it replaces. The owner approved it and allowed CI to accept a pass recorded on his machine.

**Nothing is implemented by this entry.** Stage 1 follows kb/Work PB5205, which is its first half.

