## Entry 2188 — 2026-10-18 02:04 PDT — PB3359: a campaign's standing requirements reach every spec and the branch reviewer; PB3360 retired as a duplicate of PB3114

The owner's 2026-10-17 ~22:25 requirement (SORT and file I/O tests cover the abnormal and exception paths as fully as the
success paths) reached an implementer only because the wave-1115 unit edited groups.json by hand, and it never reached the
branch reviewer, whose prompt (`wf_rolling_wave.js` `reviewBranch`) named no dispatch spec. Re-probe on this tree: still
true (`plan_wave.as_group_json` had no input for a campaign-wide requirement; `reviewBranch` read neither the spec nor any
requirement).

The fix gives a requirement one place and one path. A campaign lead note carries a `## Standing requirements` section, one
bullet per requirement (PB3353's holds the owner's sentence and the paths it names). `plan_wave.campaign_requirements`
reads it and `plan_wave.group_requirements` binds each group to the campaigns its notes name in `cluster:`;
`as_group_json`/`body_of` write each requirement into that group's spec body as an `OWNER REQUIREMENT` sentence; the group's
groups.json and args entries carry `{lead: [requirement]}`; `check_practices.py`
(`specs_missing_requirements`) turns a rendered spec that lacks one RED; `wf_rolling_wave.js` hands the branch reviewer the
group's dispatch spec path and the requirement text (`reviewerRequirements(g)`), and the review skill's branch form says a
branch lacking one is a finding of category `requirement`. A prose section, not a frontmatter list, because a requirement is
a sentence with commas and the list grammar (`work._split_list`, with its C# parity twin) splits on them.
`test_plan_wave.py` section 9a pins the parse (wrapped bullets, CRLF, no section), the spec body, the practices check both
ways, the args-file keys, the reviewer prompt, and that PB3353 still carries the owner's requirement. The design is
DESIGN-orchestrator-loop.md section 9.1 "Standing requirements"; the workstream skill names the section.

Branch review R1-1 (of the notes branch that filed PB3359-PB3361): PB3360 duplicated the open note PB3114 (a RED plan holds
its notes in the dispatch ledger and burns its codes; a drive-relative `--scratch` from Git Bash). PB3360 is retired and its
wave-1115 evidence and the two additions to the expected fix (the ledger writer moved to `make_dispatch_specs.py`; the
stop-file predicate is `check_practices.py`'s) are PB3114's "Second instance" section. PB3361 (the finished test trusts
the free-text `report` field) stays open and unstarted. No source file other than the planner, the practices check and the
rolling wave changed; no inventory row is claimed or closed.

Branch review R1 of this branch (wave 1116 B finisher): the first cut keyed the requirements to the planning MODE (only a `--cluster`
wave read the lead note), so a fix-lane wave that planned a campaign member (wave 1116's group C, PB2849, cluster PB1951 and PB3353) or
the finisher of a campaign branch wrote `requirements: []` and the unit again hand-edited groups.json, the PB3359 failure itself. The
binding is now by membership (`group_requirements`), per group, in every mode. groups.json and the args file lost the wave-level
`campaign` and `requirements` keys, check_practices matches a spec to its own group by the `msg-w<wave>-<letter>.txt` name, and
section 9a pins a real PB3353 member planned without `--cluster`. The golden `testdata/plan_wave_default.json` gained the empty
`requirements` per group.

Branch review R2 (wave 1116 B finisher, round 2): the campaign lead note kb/Work/PB3353.md still said its `## Standing requirements` section was read by `plan_wave.py --cluster PB3353`, the superseded mode rule, which would send a reader to hand-edit groups.json for a fix-lane member. The sentence now names `plan_wave.group_requirements` and the membership rule, and section 9a of `test_plan_wave.py` pins that the note says so.

