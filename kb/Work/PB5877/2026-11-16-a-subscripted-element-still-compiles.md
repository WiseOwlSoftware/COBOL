The re-read of read pass 17 (slice 1985-240) found one form still wrong after this fix: `READ IXF KEY IS IX-T (1)`, where `IX-T PIC X(4) OCCURS 2` lies at the prime record key's positions in a second record description, compiles and runs at `--std 85` and at `--std 2023`; the REDEFINES form this note refused is right.
The residue is its own note, kb/Work PB5883; this note is not reopened.
