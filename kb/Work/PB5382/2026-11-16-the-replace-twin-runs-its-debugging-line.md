The re-read of read pass 17 (slice 1985-241) found one form still wrong after this fix: with no WITH DEBUGGING MODE clause, a `D` line brought in by `REPLACE ==DBG-ONE== BY == ... ==` is executed at `--std 85`; the COPY REPLACING twin hides its line as it should.
The residue is its own note, kb/Work PB5887; this note is not reopened.
