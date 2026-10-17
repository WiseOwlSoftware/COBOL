## PB3341 filed: S2's sweep missed the <edition>/<name> golden references in docs

PB3177 slice S2 (main fb006c1de) moved tests/conformance/ into conformance-suite/ and rewrote full paths and `conformance:` citations only. The short form `<edition>/<name>` survives in 25 files under docs/ (392 occurrences, 247 in markdown) and also in kb/, src/ and tests/. Filed as PB3341 with the rename-record rewrite and a resolves-to-a-file drift test as the fix; no reference was changed.
