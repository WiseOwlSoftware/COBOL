## PB4849: the refuter step goes first; the figure audits on a scan are decided

A note-only change. Three PB4849 waves in a row (20261893, 20261897, 20261900) spent their turns tuning the measurer's
pairing: each matched about 10 to 15 more figures. None reached the refuter step, which is what decides the 352 blocks still
awaiting a verdict. The note's "Still to do" now runs in a fixed order. First the refuter step, with the local witness as a
third reading, deciding from the rendered printed page and treating an unpaired figure as no blocker. Then pairing, only
where the verdicts show that it pays. The audits are decided: `audit_underlining`, `audit_figure_structure` and
`audit_figure_text` stay vector-only, because for a scan `sweep_figures --check` and `figure_reading.compare` already do
their job. `audit_grammar_optional_words` reads the decided figures. The superseded PB4912 branch is recorded ABANDONED.
