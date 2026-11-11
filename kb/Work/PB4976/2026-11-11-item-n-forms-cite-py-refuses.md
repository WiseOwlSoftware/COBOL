**Citation forms with a trailing `item N` that `cite.py --check` refuses (2026-11-11).**
Sources: E:/COBOL-coord/scratch/reports/w20262000d-PB5199-report.md R1-1, w20261983c-PB5117-report.md R1-2 and R2-1.
`python scripts/spec/cite.py --check` refuses each of these with "'item N' is not a rule designator: write SR n, GR n, AR n or RV n":
- `§2023:9.1.13.6 item 1` fails at origin/main e3c9e8551a. It occurs in 130 lines of 49 files (`grep -rn "9\.1\.13\.6 item 1" src docs tests`),
  among them `src/WiseOwl.Cobol.Runtime/IO/FileConnector.cs` (985, 1273), IndexedConnector, KeyedConnector, RelativeConnector,
  SequentialConnector, `docs/CONFORMANCE.md`, `docs/rearchitecture/DESIGN-runtime-library.md` (15) and the unit tests
  `FileBoundaryTests.cs` (7), `IndexedEngineTests.cs` (5). The clause text is a list item of 9.1.13.6, so the form to write is
  whatever `cite.py` resolves for that list item, not a loose "item 1".
- `§1985:p.XVII-94 item 14` fails (cite.py --check run on that string). On origin/main only the sibling `item 7` form exists
  (`docs/CONFORMANCE.md:3398`, `kb/Work/PB5086.md`); the `item 14` sites named by the review (`SequentialConnector.cs:182`,
  `ReversedUnpositionableMediumTests.cs:12`) are on the PB5199 branch and not yet on main, so the sweep must cover that branch's
  landing too.
- `§1985:IX.1.3.4 item (1) b)` fails. The review's sites (`DialectBehavior.cs:89/:138`, `IndexedConnector.cs:562/:573`, VCR row 7.54)
  are on the PB5117 branch, not on main at e3c9e8551a (grep finds no `IX.1.3.4 item`); sweep them when that lands.
The audit this note asks for should refuse the whole family `§<edition>:<locator> item <n>` and the page-form `p.<roman>-<n>`
locators where cite.py has no resolver for them, so a landing cannot add a new one.
