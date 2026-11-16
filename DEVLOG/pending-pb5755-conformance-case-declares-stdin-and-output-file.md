## A conformance case declares its standard input and an output file the harness compares after the run

kb/Work PB5755 and PB5765. The suite's harness ran every case with standard input closed and judged it on standard output
alone, so no case could witness an ACCEPT rule that needs data from the input device (§1985:VI.6.5.4 GR1-GR5), and none
could witness a rule whose effect shows only after the run unit ends (STOP RUN's implicit CLOSE, §1985:VI.6.24.4 GR2). Six
rows of the 1985 inventory waited on those two facilities.

**What was built.** Two header fields, each travelling the road `files` travels: the generator's header reader and manifest,
the Python runner, and the C# Conformance runner from the same manifest.

- `stdin: <file>` names a file beside the case, LF line ends only. `run.py` feeds it to the program's run (`execute` takes the
  bytes, an adapter hook's `StepContext.stdin` carries them) and keeps a closed input for a case that declares none; the
  in-process runner passes it to `CutRunner.RunExit`. A compile never sees it.
- `output-file: {name: <file the program leaves in its run directory>, expected: <file beside the case>}`. Both runners
  compare the two once the process has ended, on the normalization of the standard output and in addition to it. A missing
  file, a differing file and an absent expected file are three distinct FAILs, each naming the file.
- The manifest entry carries `stdin` (a suite-relative path) and `output-file` (`{name, expected}`), each only for a case
  that declares it; `SuiteManifest.cs` reads both.

**The generator refuses** a declared name that is a path, a missing file, a CR in the input, a field twice, an `output-file`
that is not exactly `{name, expected}`, an `expected` ending in `.out`, one file in two parts (`files`, `stdin`, `expected`,
the case's own source), and either field on a case no edition accepts. Its new `--check` rule (PB5755 item 3) refuses a case
that names a rule of the transfer from the input device, executes an ACCEPT reading standard input and declares no `stdin`.
The rules come from data, not a list: an inventory row of kind GR whose subject is the ACCEPT statement and whose catalog
text (`cite.rule_book`) speaks of its device and not of a screen. That selects exactly GR1-GR5 of 1985, 2002 and 2023 and
leaves GR14 (a screen rule) out. The program side reads the ACCEPT statements (`cobol_source.device_accepts`): no FROM, or
FROM CONSOLE or SYSIN or a mnemonic-name SPECIAL-NAMES binds to one, and not a screen item. The two input device names are
the one place the checker states a list; a self-test holds them to `ImplementorNames.cs`.

**Tests, each of which can fail.** `generate.py --self-test` plants a refused case for every refusal above and three accepted
ones that must stay accepted (ACCEPT FROM DATE, ACCEPT of a screen item, a device ACCEPT naming another rule).
`run.py --self-test` plants a case fed a two-line input, the same stub with none, and output-file cases that pass, differ,
miss, lack the expected file, and fail on standard output while the file matches. `ConformanceSuiteDeclaredFilesTests`
compiles and runs real programs through the in-process runner: an echo of two ACCEPTs with and without a declared input, and
a writer that ends with STOP RUN without a CLOSE against a matching, a differing, a missing and an absent expected file.

**What would turn red if a runner forgot a field.** The planted tests of its own side, and the verdict comparison of the two
runners (`BothRunnersVerdictDriftTests`) on every suite case that declares either field.

**Not here.** The 1985 ACCEPT and STOP RUN cases that close the six rows are written and attacked separately and folded into
this branch; this change closes no row itself.

**The cases folded in, and the rows they close.** `g85_z05` (declared input `g85_z05.in`) closes 1985-GR-VI.6.5.4-1 and -5; `g85_z06` closes -3; `g85_z08` closes -2 and -4; `g85_z07` (declared output file `G85Z07.DAT`, expected `g85_z07.exp`, with a debugging section that proves the STOP RUN sentence on its second leg) closes 1985-GR-VI.6.24.4-2. Each row was recorded as a witness only, after an independent refuter upheld it in its second pass; the Python harness and the C# Conformance tests both pass all four cases. Two register entries came with them. `DOC-A.1-5` said a smaller ACCEPT receiver ignores the line's remaining records under GR4b, which the standard does not say: its sentence now cites GR3, GR4a and GR4b each for its own case and states, as the implementor's choice and as GnuCOBOL does, that every ACCEPT begins at a new line (kb/Work PB5755/2026-11-16-every-accept-begins-at-a-new-line.md). `D-DEBUG-SWITCH-85` is new because `g85_z07` rests on the object-time switch of the 1985 debug facility, which §1985:XV.1.3.4 leaves outside the program and WiseOwl COBOL keeps as `RunUnit.DebugMode`, on by default. The 1985 inventory line went from 55 GAP to 49 GAP.

On the final resume, origin/main had not moved past the branch base (merge: already up to date), and the merged tree read the same 1985 line, 49 GAP, as the fold left it.

The first gate run (000001-20261116T101141Z) was RED in the BUILD TARGETS audit: the generator imported cite (16 undecided path probes of scripts/spec) and two new is_file probes were not judged. The rule text now comes straight from the rule catalogs, which are inputs of the target, and the two probes are judged in build_targets.json.
The second gate run was RED in SELF-TESTS only (all four test targets green): the generate.py self-test reads ImplementorNames.cs, now a TOOLING_READS entry in scripts/lane_classification.py.
