#!/usr/bin/env python3
"""Render implementer/finisher dispatch specs from ONE template (MANDATORY-PRACTICES.md O1).

    python .claude/skills/workstream/make_dispatch_specs.py <groups.json> [--coord DIR]

groups.json:
{
  "wave": "58", "scratch": "E:\\\\Temp\\\\...\\\\scratchpad", "base": "c54434a8d or later — ...",
  "stop_file": "E:\\\\Temp\\\\...\\\\scratchpad\\\\STOP-w58",   # REQUIRED: this fleet's own stop (kb/Work PB2483)
  "groups": [
    {"letter": "KA", "slug": "w58a", "group": "INTRINSICS BEYOND BINARY64", "notes": "PB999, PB1000", "lead": "PB999",
     "codes": "COBOLNET2400–COBOLNET2402", "root": "...", "files": "`kb/Work/PB999.md`, ...", "body": "...",
     "file_set": ["src/...", ...],   # the files the group changes, a LIST (plan_wave.py writes it); when absent or
                                      # empty, the set computed now from its notes (plan_wave.computed_file_set);
                                      # a file under src/Cobol.Net.Runtime/IO/ makes it a host-I-O group (below)
     "member_set": {"src/...": ["Type.Member", ...]},   # optional: the members it claims in those files (a file
                                      # absent is claimed whole; kb/Work PB3670), recorded in the ledger with them
     "pred": "",          # optional: predecessor-branch / resume instruction, rendered into the spec
     "after": "K"}        # optional: SAME-FILE SUCCESSOR of group K — ignored here; wf_rolling_wave.js runs this group
                          # only after K returns and hands it K's branch and report (workstream SKILL §2)
  ]
}
Refuses (exit 2, nothing written) a groups file whose `scratch` or `stop_file` is not a fleet path (coord.scratch_problem,
coord.fleet_stop_problem: absolute, the scratch outside the checkout, the stop file STOP-<scope>; kb/Work PB3114: Git Bash
made `E:COBOL-coordscratch` of an unquoted Windows path three times). Writes <scratch>\\msg-w<wave>-<letter
lower-cased>.txt per group (the file wf_rolling_wave.js opens), runs check_practices.py over the specs AND the groups
file (exit 1 on a miss), and only when that is GREEN RECORDS every rendered group in the dispatch ledger
(<coord>/inflight-groups.json) through plan_wave.record_dispatch, an upsert on (wave, letter) with the group's notes,
file set and member claims: a RED render records nothing, so it holds no note from a later plan (kb/Work PB3114). `--no-record` renders
and checks only (the planner's first pass on peeked codes, before it reserves any).

The ledger has exactly two structured writers, this renderer (every wave, the planner's and a hand-planned one alike)
and `plan_wave.py --release` (an aborted dispatch); it is never written from a scrape of a prompt (kb/Work PB3186: the
retired guard-hook recorder took every note id a brief mentioned and recorded six entries of 303-910 files, which bound
fix-lane worktrees to reference notes and stopped their landings). The planner's file-set partition and the landing
check read it (plan_wave.inflight_file_sets, source 3).

Every spec names TWO stop files (kb/Work PB2483): the owner's global stop (`coord.global_stop()`) and this fleet's own
`stop_file` (`coord.fleet_stop(scratch, scope)`; plan_wave.py writes it). A fleet stopped by its own file never stops
another session's agents.

Every spec's report path is in `coord.reports_dir()`, never under the wave's `scratch` (kb/Work PB2980): the enqueue unit
and the branch survey read that one directory, and a report anywhere else is never landed.

A HOST-I-O GROUP RUNS CI'S LINUX LEGS AT ITS OWN GATE (kb/Work PB3381, MANDATORY-PRACTICES I8). A group whose file set
(`group_claim`: its `file_set`, or, when that is empty, the set its notes declare) holds a file under HOST_IO
(`src/Cobol.Net.Runtime/IO/`: the connectors, the keyed-store engine, the host file medium) is `host_io`, and its spec's
LINUX GATE paragraph tells the implementer to run CI's Linux unit and conformance legs from WSL after the Windows gate
is GREEN; every other group keeps the paragraph that says the targeted Linux half is inside its own gate. Wave 1118 measured why:
PB3333 was DONE at 08:50, reviewed CLEAN at 11:07 and refused at its landing at 11:26 because 31 NIST RL programs
differ on Linux, which no earlier gate had run.
"""
import argparse, functools, json, pathlib, re, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
REPO = HERE.parents[2]
TEMPLATE = HERE / 'templates' / 'dispatch-spec-implementer.md'
sys.path.insert(0, str(REPO / 'scripts' / 'orchestrator'))
sys.path.insert(0, str(REPO / 'scripts' / 'spec'))
import coord  # noqa: E402  (the one definition of the stop files and the reports directory)
import plan_wave  # noqa: E402  (record_dispatch: THE ledger writer; computed_file_set: THE computed file set)
import work  # noqa: E402  (the register: which notes are terminal)

PB = re.compile(r'\bPB\d+\b')

#: The host file I/O layer (module docstring): a group that changes a file under it is a host-I-O group.
HOST_IO = 'src/Cobol.Net.Runtime/IO/'
#: The LINUX GATE paragraph of a spec ({linux_gate} in the template), by `host_io`. Every other group's: the targeted
#: Linux half runs inside the implementer's own gate (MANDATORY-PRACTICES I8, L10; kb/Work PB3612).
LINUX_GATE = {
    False: "LINUX GATE (I8, PB1732, PB3612): the Linux half of the targeted leg runs INSIDE your gate (owner 2026-10-23): "
           "after the Windows legs are green, `build-local` runs the same plan's legs in a WSL clone of your COMMITTED HEAD "
           "(`linux-gate.sh --legs targeted`, under the slot it holds), and your verdict line says `Linux half (WSL clone of "
           "HEAD) GREEN`. So COMMIT BEFORE YOU GATE (the clone tests HEAD, and the gate's record counts only when the tree it "
           "measured is the head's), and report `LINUX GATE: inside your gate (the targeted Linux half)`. A red Linux half is "
           "yours to root-cause and fix like a Windows red. `enqueue.py land` gates nothing again: it reads your gate's record.",
    True: "LINUX GATE (I8, PB1732; HOST-I-O GROUP, kb/Work PB3381): your group changes the host file I/O layer "
          f"(`{HOST_IO}`: the connectors, the keyed-store engine, the host file medium), whose behaviour differs by "
          "host, so CI's Linux legs that cover it run at YOUR gate, not first at the landing (wave 1118: PB3333 was "
          "refused at its landing because 31 NIST RL programs differ on Linux, after a GREEN Windows gate and a CLEAN "
          "review). After the Windows gate is GREEN and COMMITTED (the Linux gate builds a Linux clone of your committed "
          "HEAD), run `bash scripts/linux-gate.sh --legs unit,conformance --nice` at your worktree's root, from any "
          "shell (on a host that is not Linux it runs itself under WSL, kb/Work PB2999), in the BACKGROUND to a log, "
          "and block on its `=== LINUX GATE: ` line exactly as you block on the Windows gate's verdict (`timeout 580 "
          "bash -c 'until grep -q \"=== LINUX GATE: \" \"<log>\"; do sleep 5; done'`, re-issued until it prints); "
          "quote that line in your report. (With a plain `GREEN` verdict, the owner's `whole` scope, run all four legs "
          "instead: `bash scripts/linux-gate.sh --nice`.) A RED is yours to root-cause and fix like a Windows red: a "
          "platform difference is a defect, never a per-OS golden. CI's merge group still runs all of CI's Linux legs "
          "on the merge.",
}


def host_io(files):
    """True when any file of `files` (repo-relative, either separator) lies under HOST_IO."""
    return any(f.replace('\\', '/').startswith(HOST_IO) for f in files)


def group_claim(g, claim_of):
    """A group's declared set (plan_wave.Claim): its `file_set` LIST with its `member_set` (plan_wave.py writes both),
    or, when that is absent or empty, `claim_of(its note ids)` (the set its notes declare, plan_wave.computed_file_set).
    The one reading the ledger's record and the host-I-O paragraph share."""
    if isinstance(g.get('file_set'), list) and g['file_set']:
        return plan_wave.Claim.of(g['file_set'], g.get('member_set'))
    return claim_of(list(dict.fromkeys(PB.findall(g.get('notes', '')))))


def render(cfg, g, files, tpl=None):
    """One group's spec: the template filled from the groups file `cfg`, the group `g` and its file set `files`
    (group_claim's files: they decide the LINUX GATE paragraph) — the one render; the planner's self-test calls it too. Raises
    KeyError naming a placeholder the inputs do not fill."""
    tpl = tpl if tpl is not None else TEMPLATE.read_text(encoding='utf-8')
    if not cfg.get('stop_file'):
        raise KeyError("stop_file (this fleet's own STOP-<scope>, kb/Work PB2483; plan_wave.py writes it)")
    pred = g.get('pred', '')
    return tpl.format(wave=cfg['wave'], base=cfg['base'], S=str(cfg['scratch']), pred=(pred + '\n') if pred else '',
                      global_stop=str(coord.global_stop()), stop_file=cfg['stop_file'], reports=str(coord.reports_dir()),
                      linux_gate=LINUX_GATE[host_io(files)], **{k: v for k, v in g.items() if k != 'pred'})


def record(cfg, coord_dir, terminal=None, claim_of=None):
    """Record every group of the groups file `cfg` in the dispatch ledger (plan_wave.record_dispatch, an upsert on
    (wave, letter)) and return the entries written. A group's notes are its `notes` field's ids; its files and members
    are group_claim(g, claim_of): its `file_set` LIST and `member_set`, or, when a hand-built groups file carries none,
    `claim_of(note ids)` (default computed_claims(coord_dir)). `terminal(note id)` defaults to the register's landed or
    retired."""
    if terminal is None:
        status = {i.get('id'): i.get('status') for i in work.load()}
        terminal = lambda n: status.get(n) in work.TERMINAL_STATUSES  # noqa: E731
    claim_of = claim_of or computed_claims(coord_dir)
    groups = [{'letter': g['letter'], 'slug': g.get('slug'), 'notes': list(dict.fromkeys(PB.findall(g.get('notes', '')))),
               'files': sorted(c.files), 'members': c.member_lists()}
              for g in cfg['groups'] for c in [group_claim(g, claim_of)]]
    return plan_wave.record_dispatch(coord_dir, str(cfg['wave']), groups, terminal)


def computed_claims(coord_dir):
    """`claim_of`: plan_wave.computed_file_set over this tree, on the planner's index policy (THE index of the tree,
    cached in `coord_dir`, kb/Work PB3203), memoized per note list so the render and the record compute it once."""
    policy = plan_wave.IndexPolicy.of(coord_dir)
    once = functools.cache(lambda ids: plan_wave.computed_file_set(REPO, list(ids), policy))
    return lambda ids: once(tuple(ids))


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.split('\n')[0])
    ap.add_argument('groups', help='the groups file (groups.json)')
    ap.add_argument('--coord', help=f'coordination directory of the dispatch ledger (default ${coord.ENV} or {coord.DEFAULT})')
    ap.add_argument('--no-record', action='store_true', help='render and check only, recording nothing in the dispatch '
                    'ledger: the planner\'s first pass, on PEEKED codes, before it reserves any (plan_wave.py, kb/Work PB3114)')
    a = ap.parse_args(argv)
    cfg = json.loads(pathlib.Path(a.groups).read_text(encoding='utf-8'))
    # THE FLEET PATHS FIRST (kb/Work PB3114): a drive-relative scratch or stop file is refused before anything is written
    if refused := [w for w in (coord.scratch_problem(cfg.get('scratch') or ''),
                               coord.fleet_stop_problem(cfg.get('stop_file') or '')) if w]:
        print('\n'.join(f'{a.groups}: {w}' for w in refused))
        return 2
    tpl = TEMPLATE.read_text(encoding='utf-8')
    scratch = pathlib.Path(cfg['scratch'])
    ledger = coord.coord_dir(a.coord)
    claim_of = computed_claims(ledger)
    out = []
    for g in cfg['groups']:
        try:
            body = render(cfg, g, sorted(group_claim(g, claim_of).files), tpl)
        except KeyError as e:
            print(f"{a.groups}: group {g.get('letter')}: no value for {e}")
            return 1
        # Keyed on the FULL letter, lower-cased: the workflow opens msg-w<wave>-<letter.toLowerCase()>.txt, and a
        # multi-character letter (a same-file successor such as "V2") must not collide with another group's file.
        p = scratch / f"msg-w{cfg['wave']}-{g['letter'].lower()}.txt"
        p.write_text(body, encoding='utf-8', newline="\n")
        out.append(str(p))
        print('wrote', p)
    rc = subprocess.call([sys.executable, str(HERE / 'check_practices.py'), a.groups, *out])
    # ONLY A GREEN WAVE IS RECORDED (kb/Work PB3114): a ledger entry holds its notes from every later plan until its
    # report or a `--release`, so a RED render that recorded left its notes "held by dispatch" with nothing in flight
    if rc == 0 and not a.no_record:
        for e in record(cfg, ledger, claim_of=claim_of):
            print(f"recorded {e['slug']} ({', '.join(e['notes'])}): {len(e['files'])} files in "
                  f"{ledger / coord.DISPATCH_LEDGER}")
    elif rc:
        print(f"check_practices.py RED: nothing recorded in {ledger / coord.DISPATCH_LEDGER}")
    return rc


if __name__ == '__main__':
    sys.exit(main())
