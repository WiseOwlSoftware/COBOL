**Addendum (2026-11-10): branch review round 1 of `selftest-cost`, findings R1-3 and R1-4.** The reviewer's records are
`E:/COBOL-coord/scratch/review-w20261995-a.jsonl`; its probes are under `E:/COBOL-coord/scratch/review-w20261995-a/`.

**R1-3 (medium): the change form never swept when a change edited what the check classifies BY.** `changed_paths` lists
only the paths a change touches, but `.gitattributes` decides which files are TEXT, `scripts/live_tools.py` which trees
WRITERS reads, and `scripts/line_endings.py` the arms themselves. *Scenario* (`r2/`): the base declares
`fixture.cbl -text` and fixture.cbl holds a lone CR; the change deletes that line. The change form read 1 path
(`.gitattributes`) and said CLEAN; the full sweep was RED on fixture.cbl in the index and the checkout: a gate GREEN and
a merge-group RED. *Decision:* fix. *Fix:* `classifiers()` derives the check's classification inputs from its own
source, this file plus every module of `scripts/` it imports at module level, transitively (today `line_endings.py`,
`inert_lane.py`, `live_tools.py`, `standard_streams.py`), so a module it starts to import is one with no list to keep;
`classifies(path)` adds a `.gitattributes` at any level. A change that touches one runs the sweep, and says so.
*Proof:* the self-test's new block pins the derived set and `classifies`, and replays the scenario: the change lists only
`.gitattributes`, its own paths hold no CR, and `check` of the change is RED as the sweep is. The reviewer's `r2/` now:
`LINE ENDINGS: the change edits .gitattributes, which classifies files it does not list: the whole tree is swept` then
`=== LINE ENDINGS: RED — 0 attributes · 1 index · 1 working tree · 0 writers (of 3 tracked text files ...)`, exit 1
(was CLEAN, exit 0).

**R1-4 (low, lead): ATTRIBUTES read only the catch-all.** It probed one path no narrower rule names, so `*.cob -eol`
passed while a `core.autocrlf=true` checkout of x.cob is CRLF, and no CI step reads a Windows checkout any more.
*Decision:* fix structurally, by git's own resolution, not a parse of the file. *Fix:* `attributes(root, paths)` runs ONE
`git check-attr --stdin -z text eol`; THE ATTRIBUTE RULE (`Attributes.lf`) accepts `text` unset (byte-exact) or `text`
auto/set WITH `eol=lf`, and every other combination is a finding naming the file and what git resolved. The catch-all
probe (`ATTRIBUTE_PROBE`, `CATCH_ALL`) is deleted. The same answer now decides byte-exactness for every arm and `--fix`
(`Entry.attrs`/`Entry.byte_exact` and the change form's `_byte_exact` are deleted: one reader of attributes, not three).
`git ls-files --eol`'s attr column was not used: it merges what it prints (`!text eol=lf` reads `text eol=lf`).
*Decided beyond the brief:* the brief said the change form needs no attribute check, since R1-3 sweeps any
`.gitattributes` edit. A sibling remains: a NEW file a change adds under an EXISTING narrow rule (`*.cob -eol` on main,
no .cob tracked yet) changes no classifier. The change form therefore checks the attributes of the paths it reads, from
the same single `check-attr` call it already made for byte-exactness, at no extra process. *Proof:* self-test arms:
`-text`, `binary`, `text eol=lf` and the catch-all pass; `*.cob -eol` and `!text` are refused as
`x.cob (text=auto eol=unset)` and `w.u (text=unspecified eol=lf)`; an untracked `y.cob` under `*.cob -eol` is refused by
the change form. The reviewer's `r3/` and `r3c/` are now RED with `x.cob (text=auto eol=unset)` (was CLEAN).
*Cost on the real tree:* `attributes()` over the 35,634 tracked entries took 2.13 s measured alone. The whole
`--check` took 25.9 s against 24.5 s for the branch head before this change, run back to back on a loaded host
(an earlier pair read 38.5 s against 26.8 s, so these wall times are noisy).

**Verdicts at this change:** `python scripts/line_endings.py --self-test` → `=== LINE ENDINGS SELF-TEST: PASS ===`;
`python scripts/line_endings.py --check` → `=== LINE ENDINGS: CLEAN — 35255 tracked text files LF in the index and the
working tree, each declared text eol=lf or byte-exact; ...`; `python scripts/gate.py --self-test` →
`=== gate SELF-TEST: PASS (111 arms) ===`.
