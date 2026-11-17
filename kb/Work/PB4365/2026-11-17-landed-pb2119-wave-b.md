**2026-11-17, landed (Delete program PB2119, wave B): PB4365 is deleted.**

Deleted: SourceLocation.None (src/WiseOwl.Cobol.Frontend/Common/SourceLocation.cs).

The reference sweep was re-run on the tree of the day (Grep and git grep over src, tests, tools, scripts, docs, .github and .claude): no
caller, no string or nameof use, no emitted text, no PublicAPI entry; the only mentions left are history (DEVLOG, kb/Work) and the arch-census
evidence records. Behaviour is neutral: the solution builds with 0 warnings and the 37 Characterization snapshots pass unchanged.
