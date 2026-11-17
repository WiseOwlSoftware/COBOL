**2026-11-17, landed (Delete program PB2119, wave B): PB2185 is deleted.**

Deleted: ContextSensitiveWords.Count (src/WiseOwl.Cobol.Editions/ContextSensitiveWords.cs).

The reference sweep was re-run on the tree of the day (Grep and git grep over src, tests, tools, scripts, docs, .github and .claude): no
caller, no string or nameof use, no emitted text, no PublicAPI entry; the only mentions left are history (DEVLOG, kb/Work) and the arch-census
evidence records. Behaviour is neutral: the solution builds with 0 warnings and the 37 Characterization snapshots pass unchanged.
