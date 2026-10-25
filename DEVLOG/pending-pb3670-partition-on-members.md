## PB3670: the wave planner partitions on members, not on whole files and subsystems

The owner, 2026-10-24 ~18:20 PDT: "I do not understand why we cannot update separate regions of code in a single source
file from different groups such that a merge conflict doesn't occur. Developers do this all the time but you insist on
single access to a source file", and on filing kb/Work PB3670: "this is top priority over all else".

**Re-probe.** `plan_wave.py --dry-run` on this branch's base held PB3665 (`StringUnstringBinder.cs`) and PB3598 /
PB3613 (`docs/CONFORMANCE.md`) behind dispatch w20261032a. The root cause was narrower than the note said. PB3280's site
block names the FOLDER `src/Cobol.Net.Compiler/Binding/`, and PB547's names `src/Cobol.Net.Runtime`.
`member_index.note_file_set` expanded every folder site into all the code files under it (232 and 222 files on the old
code), so a fix note that only said where to look reserved a whole subsystem. After the change PB3280 declares no file
and PB547 declares three, two of them by member.

**Mechanism 1: the units of a file** (`scripts/arch/source_units.py`, new). A C# member (`Type.Member`, nested types
`Outer.Inner.Member`, overloads one unit), a Python function or method, and a Markdown section are each a unit. A
type's own header, braces and tail form `Type.<own>`, so a change there meets a claim on the whole type and no claim on
one of its members. A member's leading comments and attributes belong to it. The C# reader is a tokenizer that handles
regular, verbatim, interpolated (holes, format specifiers, line comments inside a hole), raw and raw-interpolated
strings, char literals and preprocessor lines, plus a brace walk. A file it cannot balance is one unit. All 7,523
tracked `.cs`, `.py` and `.md` files read: two forms failed in the first sweep (`@"""…` is verbatim, never raw; a hole
holding a `//` comment), and both are fixed with self-test cases. `touched` maps a diff's hunks to units on both sides,
and a pure insertion also touches the lines on either side of it, so two members appended to one class meet, as git
would conflict on them. Names match by a contiguous run of dotted segments, and a section number (`§8.7`) matches its
subsections.

**Mechanism 2: the partition reads members** (`plan_wave.py`). A declared set is a `Claim`: its files, plus the members
claimed in each file (a file with none named is claimed whole). `work.note_sites` reads the members a site block names
(`path#Member`, or `` `path` (`member`, ...) `` when the parenthesis opens with a code span). A prose parenthesis or a
`:line` suffix means the whole file. `collision` meets only on a file either side claims whole or on a matching member,
and never on a file a merge driver merges by key, the rule `enqueue.py` already reads. An EMPTY set meets nothing: PB3263's
subsystem rule and `plan_wave.subsystems` are deleted, and the plan names such a group UNPARTITIONED and holds nothing
for it. A fix-lane note's folder site reserves nothing, while a restructuring note's still does (PB2251's
`OfdRegionLocks.cs`). Callers stay reserved only for what a note moves, renames or re-signs, and each caller file is
whole. `inflight_units` reads the units each worktree's and unlanded branch's actual diff touches in every file another
claim shares. It makes one `git diff-tree --stdin -p -U0`, one `cat-file --batch-check`, one `cat-file --batch`, and one
`git diff -U0 HEAD` per worktree with an uncommitted shared edit (GitBatchDriftTests). A git failure leaves every file
whole. Each unit read is cached by content, since branches cut from one base share its version. Measured on the live
coordination directory: 15 owners, 94 shared files, about 6 s, nearly all of it unit parsing. In a wave, two groups on
one primary file chain only when their claims there meet. The dispatch ledger, the groups file (`member_set`) and
`make_dispatch_specs.py` (`group_claim`) carry member claims, and `check_practices.py`'s O2 refusal is a shared-MEMBER
refusal. Every dispatched wave appends its measurement to `<coord>/partition-measure.jsonl`, and
`plan_wave.py --partition-summary` prints it per day. `landing_check.py` reads the claim's files.

**Item 4 (repair a real conflict at once)** is PB3640's own remedy, and PB3640 is in flight in dispatch w20261031 C
beside PB3608's review lane, so this change leaves it alone. The operator memory that forbids dispatching an
UNPARTITIONED plan is outside the repository and is reported to the operator.

**Tests.** `test_plan_wave.py` section 12 is rewritten for the new rule (405 checks): different members of one file run
side by side, the same member or the whole file chains, an empty set holds and is held by nothing, a running branch's
member holds only that member, a finisher carries its branch's units, the measurement and its summary, and an end-to-end
scratch repository whose committed and uncommitted edits become units. `member_index.py --self-test` adds site members,
a fix-lane folder that reserves nothing, and a re-signed member that holds its callers (31 checks).
`source_units.py --self-test` covers every unit kind, each string form, `touched` and `match`. The fix lane's golden
plan is unchanged apart from the new `member_set` key. Docs: DESIGN-architecture-review §8.7, DESIGN-orchestrator-loop
section 9 (items 1b and 4, the campaign paragraph), the workstream SKILL.md, MANDATORY-PRACTICES O2, and `work.py`'s
SITES rule text. No inventory row (process tooling).
