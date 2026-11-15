## Entry 3702 — 2026-11-15 14:32 PST — The 2002 read, fourth report filed in kb/Work (PB5700-PB5727)

The fourth report of the 2002 read (165 rows of the 2002 inventory in 58 entries, `E:/COBOL-coord/scratch/op-20261114/read-2002/defects-2002-d.json`) became notes. Nothing in the compiler, the goldens or the inventory records changed.

| Report section | Entries | Rows | Filed as |
|---|---|---|---|
| `new` | 26 | 44 | one defect note each, PB5700-PB5725 |
| `extend` | 14 | 43 | 12 entries (29 rows) as dated findings beside PB5599, PB5601, PB5641, PB5611, PB5612, PB5642, PB5645, PB5652, PB5658, PB5613, PB5648, PB5575, with the rows added to each `inventory_rows`; 2 entries not filed because their notes are landed: PB5600 (12 rows) and PB5609 (2 rows, landed on main while this branch waited) |
| `owner-decision` | 4 | 37 | 3 entries (36 rows) as dated findings beside PB5577, PB5578, PB5580; the CURRENT-DATE offset entry (1 row, 2002-RV-15.16.2-1, D-UTC-OFFSET) names no open note and is not filed |
| `conditional` | 2 | 2 | adjudication notes PB5726 (exception object during a declarative) and PB5727 (SORT USING under EC-I-O CHECKING); no verdict recorded |
| `dropped` | 12 | 39 | no note; the rows go to the witness lane in `defects-work/d/dropped-rows.json` |

Every row of the filed entries (111) sits in exactly one open note's `inventory_rows` (proof script `E:/COBOL-coord/scratch/op-20261115/file-defects-d/prove.py`).

Two of the new notes name the default edition: PB5713 (a level-88 with VALUE ... FROM crashes `SetEmitter.EmitSet` at --std 2023 as well; 2023 row SR-13.18.63.3-37, recorded DOCUMENTED-NON-SUPPORT) and PB5711 (a signed item under GROUP-USAGE NATIONAL without SIGN IS SEPARATE accepted at --std 2023; 2023 row SR-13.18.29.3-3, recorded CONFORMS). Each carries a `**2023 row:**` line; no inventory record was changed.

The dropped rows were already fixed on main (PB5640, PB5598, PB5575's run-time arm, PB5578, PB5602, PB5579, PB5620), settled by adjudication PB5625, showed no wrong value (2002-GR-14.8.25.3-3), had their VALIDATE refusals replaced (the pb3568 cases), or rest on an expectation the 2002 text contradicts. They need a witness at --std 2002, not a fix.

