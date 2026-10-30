## `cobol --std COBOL-2002` is a usage error, not an unhandled exception (PB4126)

Re-probe on the branch build: `--std COBOL-2002`, `--std abc` and `--std ""` ended in an unhandled `InvalidOperationException`
("Cannot parse argument ... as expected type Nullable<Int32>", exit 127) out of the `--std` validator, which read the token with
`GetValueOrDefault<int?>()`; `--std 1999` already printed the usage error. The option was the only value option typed as a number, so
its parse could fail inside the validator that was meant to report the failure.

Fix at the root: `--std` is an `Option<string?>` like `--sign-encoding`, `--numeric-display`, `--source-format` and `--repository-update`.
`Program.StdSpellings` and `Program.TryParseStd` own the accepted spellings (`85`, `2002`, `2014`, `2023`, exact) in one place; the
validator, the `Resolve` defaulting and the sibling arm use them. Sibling arm: `cobol check-batch` read a manifest's std field with
`int.TryParse` and fell back to 2023 on unparseable text (or compiled at any integer such as 1999); a present field that is not an edition
is now a FAIL verdict naming the field as written, via the testable `Program.CheckBatchVerdict`.

Tests: `CliParserTests` gains a theory over `COBOL-2002`, `abc`, `1999`, empty, `+85`, ` 85`, `2002.0` that parses AND invokes (the throw was
in the parse itself) and expects the "--std must be one of 85, 2002, 2014, 2023" error, a `TryParseStd` theory over the four editions, and the
check-batch FAIL theory. No conformance golden: nothing about the COBOL source is wrong, it is a command-line argument error. No inventory row
closed (`closes_rows: []`). Self-review (architecture, full code, performance, duplication): one parse of the edition, no second copy of the
list; the option description is built from the same table.
