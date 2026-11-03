## kb/Work PB4462: every semgrep invariant rule is text, and verify.py reads the scan's errors

`scripts/semgrep/verify.py` compared the invariant rules' finding counts with `baseline.json` and read only the scan's `results`,
never its `errors[]`. semgrep's C# grammar predates C# 12 and skips a span it cannot read, so an AST rule is blind there and a
rule that matches nothing in an unread span looks like one that matches nothing in a clean one. PB2352 re-baselined the raw
diagnostic-code literal rule 225 to 229 when 19 literals moved from an unread file into readable ones, and PB2355's DEVLOG blamed
225 to 226 on an "interpolated string with an ellipsis" when the line was byte-identical elsewhere.

**What the re-probe showed.** The old scan had 160 errors: 213 product files are read only in part and five not at all
(`ReportSectionBinder.cs`, `VersionConformancePass.cs`, `ConditionalPhrases.cs`, `Frontend.cs`, `BTree.cs`), in every project, not
three binder files. The constructs are valid C# 11-14 the product writes on purpose: a primary-constructor class header, a list
pattern with a slice designation, `extension` members, `allows ref struct`, a raw interpolated string. After a failure early in
a class semgrep drops the span to the next member it can read, which was `DataBinder.cs` lines 14-4492.

**The mechanism, found by experiment, decided the fix.** semgrep parses a file only when a rule needs the parse or has a hit. A
single `pattern-regex` rule is matched over the file's characters, and a hit inside an unread span is still reported (the new
regexes and an independent Python pass agree on all 248 raw-literal hits file by file, 18 of them inside `DataBinder.cs`'s unread
span). A `patterns:` block, even of regexes, parses first. A file that fails to parse wholly loses every hit to every rule. So
the blind rules were the three AST rules (`no-decimal`, `no-persistent-byte-storage`, `raw-diagnostic-code-literal`) and the one
`patterns:` regex rule (`integer-n-parsed-outside-host-value`); the other five regex rules were never blind.

**Fix.** `invariants.yml`: all nine rules are one `pattern-regex`. The AST rules match the same declarations and calls as text, a
`//` comment ruled out by a tempered prefix, and the `patterns:` rule's `pattern-not-regex` is a lookahead. `verify.py` has four
checks now. Check 0 refuses any rule that is not one `pattern-regex`, naming its keys, so the next AST rule cannot reintroduce the
blindness. Check 1 fire-tests each rule in `testdata/Violations.cs` and in the new `testdata/Unparsed.cs`, a file semgrep reads
only in part. Check 2 reads `errors[]`: a `PartialParsing` span is counted and listed by `--gaps`, and anything else (a whole-file
"Syntax error", a timeout, a rule error) fails naming the file; it also compares the files semgrep cannot read at all with
`WHOLE_FILE_UNREADABLE`, the five above with their first failing member, and a missing or stale entry fails. Check 3 is the old
ratchet. `--self-test` covers the pure parts (the real config is text, each rule shape is refused, every error kind is classified,
the exception drift, the ratchet) and needs no semgrep, so CI's audits run it. The five files are named and not reshaped: they
are valid C#, and a lint tool's parser does not decide the code. A violation added to one fails `verify.py`, because the dropped
hit surfaces as that file's "Syntax error".

**Re-measured.** The raw-code-literal count is 248, not 230: the 18 hidden sites are all in `DataBinder.cs` (COBOL0898 x6, COBOL1625
x4, COBOL1530 x2, COBOL1537 x2, COBOL1532 x2, COBOL1536, COBOL1538). The other three counts are unchanged (43, 3, 2). Of the 248, 11
sites already had a catalog descriptor that nothing used (COBOL1625 `ValueNumericOutOfRange` had no reference at all; COBOL1571 x3,
COBOL1627, COBOL1664 x3) and now name it, which changes no diagnostic (`Edition.Error(descriptor, ...)` forwards the code). The
baseline is 237, a measurement. The 237 sites carry 86 codes with no descriptor, where PB4463 named 19: its note carries the count,
and the drift test that should have caught them, `DiagnosticRegistryDriftTests.EveryEmittedCode_IsACatalogDescriptor`, skips every
dotted call in its regex.

**Merged first.** The PB2352 branch (PR #1045, worktree-wf_7d06ed65-e14-4) was merged in with main's PB2104 work resolved by hand in
five files (`OoDataBinder.cs` took `m.Definition` and `Binding.FormalName`, the pointer claim scan in `PtrBindBasedAndAddressablesPass.cs`
took `m.Definition`, `RepositoryCodec.cs` kept the expansion records over `State.Configuration`, two inventory rows kept main's
verdicts); the build is 0 warnings, 0 errors.

**Self-review.** Architecture: one mechanism per job, the rule set is data and the verifier derives what it may contain. Full code:
the regexes were compared with semgrep's own AST results on every parseable region before the AST rules were removed (no
difference in 230 sites) and with a Python pass afterwards (no difference in 248). Performance: `verify.py` adds one parse-all
semgrep run (about 15 s). Duplication: the raw-literal debt is PB4463's, not a second list.

**Branch review, round 1.** The reviewer found `scripts/arch/r2_inputs.py` (`run_semgrep`), the R2 review fleet's input step, reading
only `results` from the same scan: a file semgrep rejects wholly was reported to every finder as having no invariant hit, and a
timeout or rule error was dropped. It now reads `errors[]` through `verify.py`'s helpers, names the wholly unreadable files in each
shard input (`semgrep_unread`, from a forced parse, so a file with no hit is named too) and stops on any other error; its
self-test covers the three cases. The determination that a `PartialParsing` span is counted, not fatal (the owner asked for fatal)
is now recorded in the PB4462 note for the owner to confirm or overturn. The branch was merged again with main, which had moved
the OO method symbol's origin to a type (PB4459): its uses in `OoDataBinder.cs`, `PtrBindBasedAndAddressablesPass.cs` and
`FlagConformancePass.cs` were ported to the extracted binders.
