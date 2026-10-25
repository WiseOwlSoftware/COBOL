#!/usr/bin/env python3
"""Fail when a brief or a rendered dispatch spec drops a MANDATORY practice (templates/MANDATORY-PRACTICES.md).

    python .claude/skills/workstream/check_practices.py              # every brief in templates/
    python .claude/skills/workstream/check_practices.py <spec.txt…>  # rendered implementer specs
    python .claude/skills/workstream/check_practices.py <groups.json> <spec.txt…>  # + the same-file successor rule (O2)

A practice that lives only in a scratchpad or a transcript is forgotten by the next session (owner 2026-09-23);
this check is what keeps "automatic" true.
"""
import json, pathlib, re, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[2] / 'scripts' / 'orchestrator'))
import coord  # noqa: E402  (the one definition of the reports directory)
sys.path.insert(0, str(HERE.parents[2] / 'scripts' / 'arch'))
import source_units  # noqa: E402  (the name match two member claims meet on, kb/Work PB3670)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[3] / "scripts"))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402
T = HERE / 'templates'
POINTER = 'MANDATORY-PRACTICES.md'

# Every brief that lands a branch: PB3023 — the ONE landing step is `enqueue.py land` (no lander, no hand pull request), and
# a permission refusal of it is handed off as READY-TO-LAND, never retried or routed around (PB2601, L13).
LANDS = [r'enqueue\.py land', r'READY-TO-LAND <this worktree> <branch>|READY-TO-LAND <worktree> <branch>']

# role → (file, required patterns). Every brief must point at the practices file; the patterns are the practices
# that are cheapest to lose silently.
BRIEFS = {
    # P4: the stamped handoff (kb/Work/PB1698) — STATUS.md names the commit it describes; a resumer reads the delta.
    # PB1721: the gate is the ORDERED gate — `build-local.ps1 -Priority BelowNormal`; PB2515: leg 1 only; PB3022: one shape.
    'fix-lane-implementer-brief.md': [r'claude-skills', POINTER, r'BelowNormal', r'build-local\.ps1 -Priority BelowNormal', r'STATUS-AT:',
                                      r'status_delta\.py', r'BUILD-LOCAL GATE: GREEN',
                                      # PB1732 (I8): CI's Linux legs run under WSL before the report
                                      r'linux-gate\.sh', r'FIX WHAT YOU FIND',  # I9
                                      r'LEG 1 ONLY \(batched gating, PB2515\)'],  # PB2515: the implementer's done-state
    'implementer-brief.md': [r'claude-skills', POINTER, r'BelowNormal', r'build-local\.ps1 -Priority BelowNormal', r'BUILD-LOCAL GATE: GREEN',
                             r'FIX WHAT YOU FIND', r'LEG 1 ONLY \(batched gating, PB2515\)'],  # I9; PB2515
    # PB3023: a finisher takes ONE branch enqueue.py stopped on (or the queue bounced), fixes and gates it, and returns
    # DONE: its commits are reviewed on the branch before the new tip lands (I10, L9: enqueue.py's exit 11)
    'finisher-brief.md': [r'claude-skills', POINTER, r'STOP', r'until grep -q', r'STATUS-AT:', r'build-local\.ps1 -Priority BelowNormal',
                          r'BUILD-LOCAL GATE: ', r'FIX WHAT YOU FIND', r'=== ENQUEUE: ', r'DO NOT LAND IT YOURSELF',
                          r'exit 11',
                          # the owner's exception-path test rule: a POINTER to its one home, never a second case list
                          r'SORT AND FILE I/O TESTS \(MANDATORY-PRACTICES I11', r'the I11 row of `templates/MANDATORY-PRACTICES\.md`'],
    # I11: the report records the exception-path tests, so the reviewer has one place to read them
    'implementer-report-template.md': [r'## Owner test requirement \(MANDATORY-PRACTICES I11\)'],
    # the lanes MANDATORY-PRACTICES I10 does not review name themselves at the landing (enqueue.py UNREVIEWED_LANES)
    'golden-lane-brief.md': [r'claude-skills', POINTER, r'build-local\.ps1 -Priority BelowNormal', *LANDS,
                             r'enqueue\.py land <your branch> --lane golden',
                             r'compare_oracle'],  # L11 (PB3107): a golden lane's ADDED cases, compared against its base
    'registrar-brief.md': [r'claude-skills', POINTER, r'code site', *LANDS, r'enqueue\.py land <your branch> --lane registrar'],
    # PB2483: a stop is scoped; every workflow names the owner's global stop AND the fleet's own.
    'wf_lane3_adjudicate.js': [r'claude-skills', r'args\.stopFile', r'args\.globalStopFile', r'GRACEFUL STOP', r'CHECKPOINT PER RULE', r"model: 'opus'", r"agentType: 'cobol-adjudicator'", r"agentType: 'cobol-refuter'"],
    'wf_lane3_refute.js': [r'claude-skills', r'args\.stopFile', r'args\.globalStopFile', r'GRACEFUL STOP', r"model: 'opus'", r"agentType: 'cobol-refuter'"],
    # PB2558-PB2561 (the R2 adversarial review): per-decision JSON lines, the scoped stops, the read-only reviewer role,
    # the completeness pass, the examined null and the three-lens skeptics, decided on disk by r2_collect.py.
    'wf_r2_review.js': [r'claude-skills', r'args\.stopFile', r'args\.globalStopFile', r'GRACEFUL STOP', r'CHECKPOINT PER DECISION',
                        r"agentType: 'cobol-reviewer'", r'r2_collect\.py', r'null-check', r'LENSES',
                        # the w1034 refuter's C2: the launch state is the PAIR's, from disk, through one tool
                        r'planOf', r'--launch', r'--status', r'--append',
                        # PB2707: the launch is launch.js, the template with its plan from disk; nothing is transcribed
                        r'launch\.js', r'const A = args'],
    'dispatch-spec-implementer.md': [r'claude-skills', r'BelowNormal', r'build-local\.ps1 -Priority BelowNormal', r'\{global_stop\}', r'\{stop_file\}',
                                     r'\{reports\}',  # PB2980: the report goes in coord.reports_dir(), never the wave's scratch
                                     r'until grep -q', r'where\.py', r'orient\.py', r'semgrep/verify\.py', r'cite\.py --check',
                                     r'Turn cap \{turn_cap\}',  # PB3701: read from the agent's maxTurns, never typed
                                     r'code site', r'leg-1-Conformance\.trx', r'drift_rules\.py', r'STATUS-AT:',
                                     r'status_delta\.py',
                                     # PB1721: the ordered gate, blocked on its own verdict line
                                     r'BUILD-LOCAL GATE: ',
                                     # PB1732 (I8): the LINUX GATE paragraph, rendered by host_io (PB3381:
                                     # make_dispatch_specs.LINUX_GATE; a rendered spec carries linux-gate.sh)
                                     r'\{linux_gate\}', r'FIX WHAT YOU FIND',  # I9
                                     r'LSP DIAGNOSTICS',  # P13: act on the language server's diagnostics
                                     r'BRANCH REVIEW \(MANDATORY-PRACTICES I10',  # PB3027: reviewed on the branch
                                     # I11: the owner's SORT and file I/O exception-path test rule (2026-10-17), as a
                                     # POINTER to its one home (the I11 row), never a second case list
                                     r'SORT AND FILE I/O TESTS \(MANDATORY-PRACTICES I11', r'the I11 row of',
                                     r'LEG 1 ONLY \(batched gating, PB2515\)'],  # PB2515: the implementer's done-state
    # O2: the standard fix-lane dispatch — rolling pool, same-file successors (no hold), the graceful STOP, and the explicit
    # final StructuredOutput reminder (three agents in waves 65-67 ended without it and stranded finished branches).
    # PB1703 / PB1704: a dead agent (rejection) and a hung agent (ceiling) must not hold the wave.
    'wf_rolling_wave.js': [r'args\.stop_file', r'args\.global_stop', r"agentType: 'cobol-implementer'", r'StructuredOutput',
                           # PB3381: no hold; a successor starts once its predecessor's review loop settled, every
                           # finished result is reviewed in its slot, round 2+ reviews the diff, a severity threshold
                           r'g\.after', r'settled\(g\.after\)', r'if \(r\.finished\) r = await reviewed\(g, r\)',
                           r'reviewed_head', r'BLOCKING_AT_CAP', r'status_delta\.py', r'withCeiling\(',
                           r"status: 'NO-RESULT', error", r'starts no lander',  # PB3023, PB3103: the landing lane lands
                           # PB3027 (I10): a DONE branch is reviewed on the branch by one read-only reviewer, findings go
                           # to a review finisher, and only a branch reviewed CLEAN reaches a train
                           r"agentType: 'cobol-reviewer'", r'review_findings\.py', r"r\.review === 'CLEAN'",
                           r'REVIEW FINISHER', r'REVIEW_FIX_ROUNDS',
                           # PB3359: a campaign's standing requirements and the group's dispatch spec reach the reviewer
                           r'g\.requirements', r'reviewerRequirements\(g\)', r'msg-w\$\{W\}-\$\{g\.letter\.toLowerCase\(\)\}\.txt: read it'],
    # O8: every fleet workflow runs with the stall watchdog beside it (PB1704).
    'MANDATORY-PRACTICES.md': [r'stall_watch\.py', r'linux-gate\.sh', r'I9', r'dispatch_guard\.py', r'prune_worktrees\.py',  # O8; PB1732; I9, O9, O10
                               r'STOP-<scope>',  # P3, PB2483: the scoped stop
                               r'set-cap', r'set-implementer-scope',  # PB2514 (one shared cap)
                               # PB2600 (O10: the owner's branch rule; nothing archived, abandon by a recorded
                               # decision); PB2602 (O11: one landing queue); PB2601 (L13: the landing hand-off)
                               r'NO BRANCH IS LEFT UNDECIDED', r'--abandon', r'ONE LANDING PATH', r'coord\.py loop-state',
                               r'READY-TO-LAND <worktree> <branch>', r'enqueue\.py land',  # PB3023
                               r'I10', r'review_findings\.py', r'WAITING FOR REVIEW',  # PB3027: review on the branch
                               r'\| I11 \|'],  # the owner's SORT and file I/O exception-path test rule (2026-10-17)
}
# The group slug is w<wave><letter>, optionally followed by a successor ordinal (w68v2 = the second same-file
# cluster after group V), so the report path stays wave-and-group prefixed.
# A RENDERED spec names the two stop files themselves (PB2483): the owner's global `...\scratch\STOP` and the fleet's own
# `...\STOP-<scope>`, never a bare shared `{S}\STOP` that another session's wind-down would trip.
RENDERED_STOPS = [r'scratch[\\/]STOP(?![-\w])', r'[\\/]STOP-' + coord.STOP_SCOPE.pattern]   # the scope: coord's one rule
# A RENDERED spec's report path is in THE reports directory (coord.reports_dir(), kb/Work PB2980), the only one the land
# unit and prune_worktrees.py read: wave 1055's scratch was its own folder, and its six reports were never seen.
RENDERED_REPORT = re.escape(str(coord.reports_dir())) + r'\\w\d+[a-z]\d*-PB\d+-report\.md'
# A RENDERED spec's `Turn cap` is the implementer's maxTurns (kb/Work PB3701), the number the harness enforces.
RENDERED_TURN_CAP = rf'Turn cap {coord.agent_turn_cap("cobol-implementer")} —'
SPEC = [p for p in BRIEFS['dispatch-spec-implementer.md']
        if p not in (r'\{global_stop\}', r'\{stop_file\}', r'\{reports\}', r'\{linux_gate\}', r'Turn cap \{turn_cap\}')] + \
    RENDERED_STOPS + [RENDERED_REPORT, RENDERED_TURN_CAP, r'LINUX GATE \(I8, PB1732', r'linux-gate\.sh']

# kb/Work PB3701: a role's turn cap has ONE home, the `maxTurns` of `.claude/agents/<role>.md` (coord.agent_turn_cap), and
# every other statement of a cap is read from it or checked here. Each entry: (file, a regex whose one group is the
# number the file states, the roles whose cap that number is). A statement that vanished from its file is a failure too
# (the regex stopped matching, so the copy is no longer checked), and so is one that differs from the agent file.
CAP_COPIES = [
    ('.claude/skills/workstream/templates/MANDATORY-PRACTICES.md', r'implementer (\d+) \(raised from', ('cobol-implementer',)),
    ('.claude/skills/workstream/templates/MANDATORY-PRACTICES.md', r'read-only (\d+), implementer', ('cobol-adjudicator', 'cobol-refuter')),
    ('.claude/skills/workstream/templates/MANDATORY-PRACTICES.md', r'finisher transcript \(cap (\d+)\)', ('cobol-finisher',)),
    ('.claude/skills/workstream/templates/MANDATORY-PRACTICES.md', r'`cobol-implementer` \(Opus, high, (\d+) turns\)', ('cobol-implementer',)),
    ('.claude/skills/workstream/templates/MANDATORY-PRACTICES.md', r'`cobol-finisher` \(Opus, high, (\d+) turns', ('cobol-finisher',)),
    ('.claude/skills/workstream/references/cost-law-and-budget.md', r'\| implementer \| \*\*(\d+) turns\*\*', ('cobol-implementer',)),
    ('.claude/skills/workstream/references/cost-law-and-budget.md', r'read-only agent[^|]*\| \*\*(\d+) turns\*\*', ('cobol-adjudicator', 'cobol-refuter')),
    ('.claude/skills/workstream/SKILL.md', r'Turn caps: (\d+) for any read-only agent', ('cobol-adjudicator', 'cobol-refuter')),
    ('.claude/skills/workstream/SKILL.md', r'any read-only agent, (\d+) for an implementer', ('cobol-implementer',)),
    ('.claude/skills/workstream/templates/finisher-brief.md', r'Turn cap (\d+)\.', ('cobol-finisher',)),
]
# The implementer briefs state NO number (they point at the spec's `Turn cap` line), so none can differ from the cap.
NUMBERLESS_CAP_BRIEFS = ('fix-lane-implementer-brief.md', 'implementer-brief.md')
LITERAL_CAP = re.compile(r'\b\d{2,3}[- ]turns?\b|turn cap \d|cap of \d|hard cap:? \d|~\d{3}\b', re.IGNORECASE)


# P2: the blocking wait must RETURN when the verdict appears. `tail -f <log> | grep -m1 <verdict>` does not: grep exits
# on the match, but tail only notices at its next write, and the verdict is the log's LAST line, so the wait idles until
# its timeout (kb/Work PB1743, measured 2026-09-29: w72h's gate went green at 11:56 and its wait returned at 12:03:48,
# exit 124). The form is `until grep -q <verdict> <log>; do sleep 5; done`.
HANGING_WAIT = re.compile(r"tail -n \+1 -f[^\n]*grep -m1|tail -f[^\n]*grep -m1")


def check(path, pats):
    text = path.read_text(encoding='utf-8')
    missing = [p for p in pats if not re.search(p if p != POINTER else re.escape(POINTER), text)]
    if HANGING_WAIT.search(text):
        missing.append('P2: a `tail -f … | grep -m1` wait (it idles to its timeout); use `until grep -q …; do sleep 5; done`')
    return missing


def turn_cap_copies(repo=None, agents_dir=None):
    """The statements of a role's turn cap that differ from the role's `maxTurns` (kb/Work PB3701), as messages: a copy
    in CAP_COPIES whose number differs or whose text is gone, an implementer brief that types a number, and a role
    whose agent file has no `maxTurns`."""
    repo = repo or REPO
    found = []
    for rel, rx, roles in CAP_COPIES:
        text = (repo / rel).read_text(encoding='utf-8')
        stated = re.findall(rx, text)
        if not stated:
            found.append(f'{rel}: no statement matching /{rx}/ (the cap it states is no longer checked)')
        for role in roles:
            cap = coord.agent_turn_cap(role, agents_dir)
            found += [f'{rel}: states {n} turns for {role}, whose maxTurns is {cap}' for n in stated if int(n) != cap]
    for name in NUMBERLESS_CAP_BRIEFS:
        text = (repo / '.claude' / 'skills' / 'workstream' / 'templates' / name).read_text(encoding='utf-8')
        found += [f'{name}: types a turn cap ({m.group(0)!r}); point at the spec\'s `Turn cap` line' for m in LITERAL_CAP.finditer(text)]
    return found


# P1: a mechanical role takes its model from its own frontmatter (Sonnet). A per-call `model` on its agent() call
# OVERRIDES that, which is how "model: 'opus' on every agent" silently put clerks on Opus (owner 2026-09-27).
MECHANICAL = re.compile(r"agentType:\s*'cobol-(?:clerk|locator)'")
CALL_MODEL = re.compile(r"\bmodel:\s*'")


def mechanical_model_overrides():
    """Every workflow template line (or agent() option object) that names a mechanical role AND passes a model."""
    found = []
    for js in [*T.glob('*.js'), *(HERE.parents[1] / 'workflows').glob('*.js')]:
        for i, line in enumerate(js.read_text(encoding='utf-8').splitlines(), 1):
            if MECHANICAL.search(line) and CALL_MODEL.search(line):
                found.append(f'{js.name}:{i}')
    return found


# O2: two groups of one wave whose `root` names the same primary src/ file and whose MEMBER claims on it meet (either
# claims the file whole, or a member of one matches a member of the other: the groups file's `member_set`,
# scripts/arch/source_units.py `match`) are a same-file SUCCESSOR chain (`after:`), so the second merges the first's
# branch instead of conflicting with it. Two groups on DIFFERENT MEMBERS of one file run side by side (kb/Work PB3670,
# the owner 2026-10-24: "Developers do this all the time"); git's three-way merge combines their branches.
PRIMARY = re.compile(r'src/[\w./-]+\.(?:cs|g4)')


def members_meet(a, b, f):
    """Whether two groups' claims on their shared primary file `f` meet: either claims it whole (no `member_set`
    entry), or a member of one matches a member of the other (source_units.meet)."""
    ma, mb = (a.get('member_set') or {}).get(f), (b.get('member_set') or {}).get(f)
    return ma is None or mb is None or source_units.meet(ma, mb) is not None


def same_file_without_successor(groups_path):
    groups = json.loads(groups_path.read_text(encoding='utf-8'))['groups']
    after = {g['letter']: g.get('after') for g in groups}

    def chain(letter):
        seen = []
        while letter and letter not in seen:
            seen.append(letter)
            letter = after.get(letter)
        return seen

    by_file = {}
    for g in groups:
        m = PRIMARY.search(g.get('root', ''))
        if m:
            by_file.setdefault(m.group(0), []).append(g)
    found = []
    for f, on in by_file.items():
        for i, a in enumerate(on):
            for b in on[i + 1:]:
                if a['letter'] not in chain(b['letter']) and b['letter'] not in chain(a['letter']) and members_meet(a, b, f):
                    found.append(f"{a['letter']}+{b['letter']} on {f}")
    return found


def specs_missing_requirements(groups_path, specs):
    """The (spec name, requirement) pairs where a rendered spec lacks a requirement the groups file declares (kb/Work
    PB3359). A group's `requirements` is {campaign lead: [requirement]}, the lead notes' `## Standing requirements`
    (plan_wave.group_requirements) of every campaign the group's notes belong to: the owner's instructions that the
    group must meet, which reach an implementer only through its spec. A spec is matched to its group by the name
    `msg-w<wave>-<letter>.txt` (make_dispatch_specs.py's), so a group's requirement is checked in its OWN spec only.
    Whitespace is compared collapsed, as the planner collapses it. A group with none declares none."""
    doc = json.loads(groups_path.read_text(encoding='utf-8'))
    found = []
    for s in specs:
        path = pathlib.Path(s)
        text = ' '.join(path.read_text(encoding='utf-8').split()) if path.exists() else ''
        m = re.fullmatch(r'msg-w[^-]+-([a-z0-9]+)\.txt', path.name)
        for g in doc.get('groups', []):
            if m and g.get('letter', '').lower() != m.group(1):
                continue
            for reqs in (g.get('requirements') or {}).values():
                found += [(path.name, r) for r in reqs if ' '.join(r.split()) not in text]
    return found


def specs_with_wrong_linux_gate(groups_path, specs):
    """The rendered spec names whose LINUX GATE paragraph is not the one their group's file set calls for (kb/Work
    PB3381, MANDATORY-PRACTICES I8): a host-I-O group (make_dispatch_specs.host_io over its `file_set`) runs CI's Linux
    legs at its own gate, every other group leaves them to the landing. Matched by name like
    specs_missing_requirements; a group whose groups file carries no `file_set` was decided by the renderer from its
    notes' declared set, which this check does not recompute (the planner's self-test pins that arm)."""
    import make_dispatch_specs as mds  # noqa: PLC0415  (the one HOST_IO and the one paragraph text)
    doc = json.loads(groups_path.read_text(encoding='utf-8'))
    found = []
    for s in specs:
        path = pathlib.Path(s)
        m = re.fullmatch(r'msg-w[^-]+-([a-z0-9]+)\.txt', path.name)
        g = next((g for g in doc.get('groups', []) if m and g.get('letter', '').lower() == m.group(1)), None)
        if g is None or not (isinstance(g.get('file_set'), list) and g['file_set']) or not path.exists():
            continue
        if mds.LINUX_GATE[mds.host_io(g['file_set'])] not in path.read_text(encoding='utf-8'):
            found.append(f"{path.name} ({'a host-I-O group' if mds.host_io(g['file_set']) else 'no host-I-O file'})")
    return found


# PB1699: the generic fleet scripts and base skills are CONSUMED from the pinned public submodule, never copied or
# wrapped. Four ways that silently rots: the pin moves and the overlays still name the old version; `.agent-fleet.json`
# carries a key no script accepts (it would be ignored); a brief names a submodule script that is not there; or a
# caller still points at the retired project copies.
REPO = HERE.parents[2]
SKILLS = REPO / 'tools' / 'claude-skills'
OVERLAYS = ['workstream', 'gate', 'review', 'spec-lookup']
PIN = re.compile(r'brent-tools (\d+\.\d+\.\d+)')
RETIRED = re.compile(r'scripts/spec/(?:orient|fix_clusters|status_delta)\.py')
SUBMODULE_SCRIPT = re.compile(r'tools/claude-skills/([\w./-]+\.(?:py|js))')
FROZEN = ('DEVLOG/', 'docs/rearchitecture/evidence/')


def fleet_tooling():
    plugin = SKILLS / '.claude-plugin' / 'plugin.json'
    if not plugin.exists():
        return ['tools/claude-skills is not checked out — run: git submodule update --init tools/claude-skills']
    found = []
    version = json.loads(plugin.read_text(encoding='utf-8'))['version']
    for name, path in [(o, REPO / '.claude' / 'skills' / o / 'SKILL.md') for o in OVERLAYS] + \
                      [('PB1699', REPO / 'kb' / 'Work' / 'PB1699.md')]:
        pins = set(PIN.findall(path.read_text(encoding='utf-8')))
        if pins != {version}:
            found.append(f'{name} names brent-tools {sorted(pins) or "no version"}, the submodule is {version}')
        if name in OVERLAYS and f'brent-tools:' not in path.read_text(encoding='utf-8'):
            found.append(f'{name} overlay does not invoke its brent-tools base skill first')
    sys.path.insert(0, str(SKILLS / 'skills' / 'agent-fleet' / 'references'))
    try:
        import fleet_config
        _, cfg = fleet_config.load(REPO / fleet_config.NAME)
        sources = ''.join((SKILLS / 'skills' / 'agent-fleet' / 'references' / s).read_text(encoding='utf-8')
                          for s in ('orient.py', 'fix_clusters.py', 'status_delta.py'))
        found += [f'.agent-fleet.json key {k!r} is no flag of the fleet scripts' for k in cfg
                  if f'"--{k.replace("_", "-")}"' not in sources]
    except (OSError, ValueError, ImportError) as e:
        found.append(f'.agent-fleet.json: {e}')
    tracked = subprocess.run(['git', 'ls-files', '-z'], cwd=REPO, capture_output=True, text=True,
                             encoding='utf-8').stdout.split('\0')
    for rel in tracked:
        if not rel or rel.startswith(FROZEN) or rel.startswith('tools/claude-skills') or \
                not rel.endswith(('.md', '.py', '.js', '.ps1', '.sh', '.yml', '.json', '.txt')):
            continue
        try:
            text = (REPO / rel).read_text(encoding='utf-8')
        except (OSError, UnicodeDecodeError):
            continue
        if RETIRED.search(text):
            found.append(f'{rel} still calls a retired project copy (use the tools/claude-skills path)')
        found += [f'{rel} names tools/claude-skills/{m}, which the pinned submodule does not have'
                  for m in sorted(set(SUBMODULE_SCRIPT.findall(text))) if not (SKILLS / m).exists()]
    return found


def main():
    bad = 0
    tooling = fleet_tooling()
    if tooling:
        bad += 1
        print('FAIL     fleet tooling (kb/Work/PB1699):\n' + '\n'.join(f'           {t}' for t in tooling))
    else:
        print('ok       fleet tooling: pin, .agent-fleet.json, submodule script paths, no retired copies')
    over = mechanical_model_overrides()
    if over:
        bad += 1
        print(f'FAIL     mechanical role given a per-call model (P1 — it overrides its Sonnet frontmatter): {over}')
    caps = turn_cap_copies()
    if caps:
        bad += 1
        print('FAIL     a turn cap differs from its role\'s maxTurns (kb/Work PB3701, P5):\n' + '\n'.join(f'           {c}' for c in caps))
    else:
        print('ok       turn caps: every statement matches its role\'s maxTurns; the implementer briefs type no number')
    groups_files = [pathlib.Path(a) for a in sys.argv[1:] if a.endswith('.json')]
    for gp in groups_files:
        cfg = json.loads(gp.read_text(encoding='utf-8'))
        # the fleet paths' one rule (coord.fleet_stop_problem, scratch_problem: kb/Work PB2483, PB3114), the same the
        # planner and the spec renderer ask before they write
        for why in (coord.fleet_stop_problem(cfg.get('stop_file') or ''), coord.scratch_problem(cfg.get('scratch') or '')):
            if why:
                bad += 1
                print(f'FAIL     {gp.name}: {why} (P3)')
        clash = same_file_without_successor(gp)
        if clash:
            bad += 1
            print(f'FAIL     {gp.name}: groups share members of a primary file without a same-file successor (`after:`, '
                  f'O2): {clash}')
        else:
            print(f'ok       {gp.name}')
    specs = [a for a in sys.argv[1:] if not a.endswith('.json')]
    for gp in groups_files:
        lacking = specs_missing_requirements(gp, specs)
        if lacking:
            bad += 1
            print(f'FAIL     {gp.name}: standing campaign requirements missing from rendered specs (kb/Work PB3359): '
                  + '; '.join(f'{n}: {r[:80]}' for n, r in lacking))
        wrong = specs_with_wrong_linux_gate(gp, specs)
        if wrong:
            bad += 1
            print(f'FAIL     {gp.name}: the LINUX GATE paragraph is not the group\'s (host-I-O groups run the Linux legs '
                  f'at their gate, kb/Work PB3381): {"; ".join(wrong)}')
    if len(sys.argv) > 1:
        targets = [(pathlib.Path(a), SPEC) for a in specs]
    else:
        targets = [(T / f, pats) for f, pats in BRIEFS.items()]
    for path, pats in targets:
        if not path.exists():
            print(f'MISSING  {path}'); bad += 1; continue
        miss = check(path, pats)
        if miss:
            bad += 1
            print(f'FAIL     {path.name}: missing {miss}')
        else:
            print(f'ok       {path.name}')
    print('=== PRACTICES CHECK: ' + ('GREEN' if not bad else f'RED ({bad})') + ' ===')
    return 1 if bad else 0


if __name__ == '__main__':
    ensure_utf8()
    sys.exit(main())
