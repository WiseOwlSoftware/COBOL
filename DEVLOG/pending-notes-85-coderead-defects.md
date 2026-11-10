## kb/Work: file the 1985 code read's 113 non-conforming rows as 56 mechanism notes

The 1985 code read found 113 rows non-conforming at `--std 85`. They are filed one note per code-level mechanism, not one
per row. 46 new notes were filed, PB5158-PB5203: PB5158 holds the largest cluster, 16 rows where a NOT phrase is accepted
before its positive twin because the one `conditionalPhrases` loop ignores the printed order. Then PB5160 (apostrophe
literals), PB5161 (NUMVAL-C argument-2), PB5163 (empty argument lists), PB5167 (empty Environment Division entries),
PB5168 and PB5169 (comment-entries), PB5170 (configuration paragraphs' lone period), PB5175 (trailing currency symbol),
PB5176 (figurative MOVE to noninteger and edited items), PB5178 (ENTER routine-name), PB5198 (RERUN null facility) and
PB5200 (the open-mode USE tier's stale being-opened mode, 3 rows). The rest are single-row mechanisms. PB5164 is an
adjudication: it asks whether PB191 and R43 cover their 1985 rows. PB5203 (ADD Format 2 with no TO operand, wrong at every
edition) has no row of its own. 39 rows went to open notes as dated addenda: PB5070 (12), PB5069 (7; `crashes` now true),
PB5084 (4), PB5074, PB5076, PB5122 and PB5130 (3 each), and PB5071, PB5120, PB5121 and PB4995 (1 each). PB5070's
`wrong_answer` is now true. `work.py check` still refuses 1985 rule ids in `inventory_rows` (PB5094), so every note lists
its rows in its body, and PB5094 gained an addendum naming these notes. The row-to-note map is
`E:/COBOL-coord/scratch/coderead-85/filed-map.json`.
