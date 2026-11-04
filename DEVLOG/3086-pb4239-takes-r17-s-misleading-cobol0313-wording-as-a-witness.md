## Entry 3086 — 2026-11-04 10:55 PST — PB4239 takes r17's misleading COBOL0313 wording as a witness

After PB4573 (wave 20261860) landed, probe r17 (`EQUATE "JSON" WITH "JS"`, then `01 JS`) names the word as written, so
PB4239's sibling S4 is fixed in its naming. The message still says "'JS' is not a clause of the data description entry",
which misleads when the word is a reserved synonym used as a data-name. The Mythos lane handed that wording to PB4239, whose
SR4 fix covers reserved words in the data-name position; the note now records r17 as its witness.

