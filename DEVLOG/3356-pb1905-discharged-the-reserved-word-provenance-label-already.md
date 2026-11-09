## Entry 3356 — 2026-11-08 20:10 PST — PB1905 discharged: the reserved-word provenance label already names the last reserving edition

Wave 20261937 group C re-probed PB1905 (the generator labelling every word that left the list after 1985, 2002 or 2014
"1985-reserved, removed post-85"). It no longer reproduces: the generated table and `reserved-words.json` hold no such label, CD reads
"kept by ISO 2002 §2002:8.9, removed by 2014" and CLOCK-UNITS "removed by 2002". PB656 (a77598e96a) split the label by flag and
PB4297 (f79e307929) moved the 2002 flag to the ISO 2002 list; `ReservedWordsDriftTests.Provenance_AgreesWithTheFlagsItDescribes` holds
the labels to the flags. No code change; the note flips to landed with `closes_rows: []` and a reason. Gate: inert lane.

