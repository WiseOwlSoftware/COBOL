**2026-11-17, landed (Delete program PB2119, wave B): PB2225 is deleted.**

Deleted: Pow10.AsLong(int), the field L and BuildLong() (src/WiseOwl.Cobol.Runtime/Values/Numeric/Pow10.cs) and their two lines in the design sketch of docs/rearchitecture/DESIGN-runtime-library.md section 2.3.

The reference sweep was re-run on the tree of the day (Grep and git grep over src, tests, tools, scripts, docs, .github and .claude): no
caller, no string or nameof use, no emitted text, no PublicAPI entry; the only mentions left are history (DEVLOG, kb/Work) and the arch-census
evidence records. Behaviour is neutral: the solution builds with 0 warnings and the 37 Characterization snapshots pass unchanged.
