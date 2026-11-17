The re-read of read pass 17 (slice 1985-237) found one form still wrong after this fix: the NUMERIC class test of an alphabetic reference-modified item (`IF AB(1:2) NUMERIC`) compiles at `--std 85`; INITIALIZE, which this note fixed, is right.
The residue is its own note, kb/Work PB5881; this note is not reopened.
