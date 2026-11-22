## The spaced sub-item: refused by the tree audit, rewritten outside C# (wave 20262131 group s)

The audit and the rewrite are kb/Work PB4976's addendum of the same date: RESOLVE refuses a spaced sub-item path after a
numbered rule (`designator-not-canonical`), and `audit_code_citations.py --canonicalize` glues it (`SR1 b)` -> `SR1b`,
`GR6 d) 2. b.)` -> `GR6d.2.b`), after which RESOLVE checks the sub-item against the printed labels. The levels alternate
(letter, number, letter), so `a) b)` is a list of two items and is never glued.

**The 1985 sites this note named** (`1985-GR-XIII.3.20.4-10.json`, `1985-SR-XIII.3.15.3-3/-4.json`, the
`XIII.3.19@1985-SUM-CLAUSE` case) are rewritten and resolve; `code-reads-1985/1985-GR-XIII.3.10.7-2.json` is a digest-pinned
read record (its refuter's text is checked against the digest it was imported with), so it is left as written.

**Left:** the spaced form in C# (1,930 sites, among them the DiagnosticCatalog and ObjectReferenceSet messages this note
names, whose goldens change with them), the grammar (22) and 81 lists elsewhere, all on RESOLVE's shrink-only baseline.
The C# files belong to the train's citation groups (h, q); `--canonicalize src/ tests/` is the burn-down once they merge,
each changed message swept through its goldens.
