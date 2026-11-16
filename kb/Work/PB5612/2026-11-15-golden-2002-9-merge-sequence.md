**2026-11-15, the golden-2002-9 integrator: the same disposition for EC-SORT-MERGE-SEQUENCE on MERGE ... GIVING. A row was added to this note's `inventory_rows`.**

Row added: `2002-GR-14.8.23.3-6`.

The writer of g2002u-14-8-23-merge found it, and the integrator re-ran it on a Debug build of main 5e49d46678. The repro is
`E:/COBOL-coord/scratch/gl2002/integrate-9/repro/merge/g2mrg6n-normal-completion.cob`, copied from
`gl2002/lane1/out/g2002u-14-8-23-merge/repro/`. It is `MERGE ... USING M-A M-B GIVING G-OUT` with M-A out of KEY order, under
`>>TURN EC-SORT-MERGE-SEQUENCE EC-I-O CHECKING ON`, with a USE procedure for EC-SORT-MERGE-SEQUENCE that ends normally (no RESUME).

- Command: `cobol.exe g2mrg6n-normal-completion.cob --std 2002 --run`.
- Observed: `SM=EC-SORT-MERGE-SEQUENCE`, `AFTER`, then three `IO=EC-I-O-LOGIC-ERROR` / `42` CLOSE results, rc 0. The run unit continues. `--std 2023` gives the same output, which is lawful at 2023.
- Expected at 2002: `SM=EC-SORT-MERGE-SEQUENCE`, then abnormal run-unit termination: no `AFTER` line and a nonzero exit.

The rules (each checked with `python scripts/spec/cite.py --check`, verdict OK):

- `§2002:14.8.23.3 GR6`: "all files associated with the MERGE statement are closed". The same GR6 sets EC-SORT-MERGE-SEQUENCE to exist, and that condition is `Fatal` in the 2002 exception-name table.
- `§2002:14.5.12.1.2`: "If execution of the declarative completes normally the execution of the run unit is terminated abnormally". The 2002 text has no MERGE or SORT carve-out.

The mechanism is this note's: the `catch (CobolFatalException e) when (RaisedByTheStatement(e))` in `CobolSort.Merge` disposes of the raise at
every edition. A consequence for the fixer: `pb1036_sort_merge_statement_ec` is accepted at 2002, and its legs after the first fatal
declarative that completes normally cannot run at 2002, so the fix splits or edition-gates that case. The output-procedure half of
GR6 (USING files left open) is a separate mechanism, filed as kb/Work PB5763.
