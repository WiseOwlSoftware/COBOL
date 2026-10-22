## Wave 1149's review lead filed: PB3530

Wave 1149 group A (PB3523) found that `sweep_figures.py --check` is red on main. The figure renderer draws the report-writer LINE clause of section 13.18.35.2 with `LINE NUMBER IS` outside the three-way brace, while PB1221's hand drawing in the spec text is the correct one, so `--apply` would undo that fix. PB3530 records the lead; the fix belongs in the renderer's brace-group layout, not in an exclusion list.
