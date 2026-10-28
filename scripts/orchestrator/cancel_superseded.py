#!/usr/bin/env python3
"""cancel_superseded.py — CANCEL THE MERGE-GROUP RUNS GITHUB'S MERGE QUEUE HAS SUPERSEDED, SO THEY RELEASE THEIR RUNNERS
(kb/Work PB3769; DESIGN-test-build-ci.md §3.17.2 step 4).

    python scripts/orchestrator/cancel_superseded.py --run-id <this run's id> --sha <this run's head commit>
                                                     [--repo OWNER/NAME] [--dry-run]
    python scripts/orchestrator/cancel_superseded.py --self-test

WHY. When an entry of the merge queue fails or leaves, GitHub rebuilds every group behind it: each entry gets a NEW
merge-group commit and a new workflow run, and the runs of the old commits keep running, holding runners under the
repository's concurrent-job limit, until they finish on their own. Measured 2026-10-25: after #591 was ejected at
20:18Z every group restarted, and the superseded runs 40228927401 and 40228888256 (later 40230142896, 40230142122 and
40230639741) held runners beside the new groups' until they were cancelled by hand. A `concurrency` group cannot do it:
each merge group's ref is unique (`gh-readonly-queue/main/pr-<n>-<sha>`), so no run ever shares a group with the run
that replaced it.

HOW. CI's `supersede` job runs this on every merge-group run, at its start: the replacement groups are created at the
moment of supersession, so their runs are what sweeps the runs they replaced. It
  1. lists the repository's UNFINISHED merge-group runs (every status but completed), THEN
  2. reads the head commit of every current queue entry (queue_facts.live_heads, the one reader of the queue's facts),
     in that order: a run created after the read could otherwise look superseded, since its entry was not yet read;
     a run listed first belongs to an entry that existed before the read, and
  3. cancels each listed run that `decide` names: a run of the merge queue on main whose head commit heads no current
     entry. It never cancels its own run, a run whose entry is live, a completed run, a run of another event
     (`pull_request`, `push`, a dispatch) or of another branch's queue.
⛔ FAIL-SAFE: it cancels NOTHING when the queue cannot be read whole (gh failed, a malformed answer, no queue, more
entries than one page), when the queue reads empty, or when the read does not hold this run's own head commit (a read
taken before GitHub recorded this group, or this run is itself superseded): each says why, as a warning annotation on
the run. A cancel GitHub refuses (the run finished in between: HTTP 409) is reported and the sweep goes on. Every
outcome exits 0: housekeeping never decides a verdict, and every step of the job is `continue-on-error` as
well, the checkout included (LandingPipelineDriftTests, kb/Work PB3820); 2 is a usage error.
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import os
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import queue_facts  # noqa: E402  (live_heads: the one reader of the merge queue's facts)
import ruleset  # noqa: E402  (the gh runner: a hang guard, Unobserved on any failure; the repository's name)
sys.path.append(str(HERE.parent))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

Gh = ruleset.Gh
#: The branch every merge-group run of the queue on main is on: `gh-readonly-queue/main/pr-<n>-<sha>`.
QUEUE_BRANCH_PREFIX = f'gh-readonly-queue/{ruleset.BRANCH}/'
#: The REST workflow-run statuses of a run that has not finished (`completed` is the only finished one).
UNFINISHED = ('queued', 'in_progress', 'waiting', 'pending', 'requested')
CANCEL, KEEP, IGNORE = 'CANCEL', 'KEEP', 'IGNORE'


@dataclasses.dataclass(frozen=True)
class Verdict:
    run: int
    action: str                 # CANCEL, KEEP or IGNORE
    why: str


def decide(runs: list, live: dict[str, int] | None, own_run: int, own_sha: str) -> tuple[list[Verdict], str]:
    """(a verdict per listed run, '') — or ([], why) when the read of the queue cannot justify cancelling anything."""
    if live is None:
        return [], 'the merge queue could not be read whole (a failed or malformed read, no queue, more than one page)'
    if not live:
        return [], 'the merge queue reads empty, yet this merge-group run is testing one of its entries'
    if own_sha not in live:
        return [], (f'the queue read does not hold this run\'s own head commit {own_sha[:12]} (read before GitHub recorded '
                    'this group, or this run is itself superseded)')
    verdicts: list[Verdict] = []
    for r in runs:
        if not isinstance(r, dict) or not isinstance(r.get('id'), int):
            continue
        rid, sha, branch = r['id'], str(r.get('head_sha') or ''), str(r.get('head_branch') or '')
        if rid == own_run:
            verdicts.append(Verdict(rid, KEEP, 'this run'))
        elif r.get('event') != 'merge_group':
            verdicts.append(Verdict(rid, IGNORE, f"event {r.get('event')}, not merge_group"))
        elif r.get('status') not in UNFINISHED:
            verdicts.append(Verdict(rid, IGNORE, f"status {r.get('status')}"))
        elif not branch.startswith(QUEUE_BRANCH_PREFIX):
            verdicts.append(Verdict(rid, IGNORE, f'branch {branch}: not the merge queue on {ruleset.BRANCH}'))
        elif sha in live:
            verdicts.append(Verdict(rid, KEEP, f'heads the live entry of #{live[sha]}'))
        else:
            verdicts.append(Verdict(rid, CANCEL, f'{sha[:12]} heads no current queue entry ({branch})'))
    return verdicts, ''


def unfinished_runs(gh: Gh, repo: str) -> tuple[list[dict], list[str]]:
    """The repository's merge-group runs in every unfinished status, and the statuses GitHub would not list. A status
    left unlisted only means fewer runs are judged: nothing unseen is ever cancelled."""
    runs: dict[int, dict] = {}
    missed: list[str] = []
    for status in UNFINISHED:
        try:
            rc, out, _ = gh(['api', f'repos/{repo}/actions/runs?event=merge_group&status={status}&per_page=100'])
            listed = json.loads(out)['workflow_runs'] if rc == 0 else None
        except (ruleset.Unobserved, ValueError, KeyError, TypeError):
            listed = None
        if not isinstance(listed, list):
            missed.append(status)
            continue
        for r in listed:
            if isinstance(r, dict) and isinstance(r.get('id'), int):
                runs[r['id']] = r
    return [runs[k] for k in sorted(runs)], missed


def sweep(gh: Gh, repo: str, own_run: int, own_sha: str, dry_run: bool = False,
          say=print) -> tuple[list[int], list[int]]:
    """List, read, decide, cancel (module doc). Returns (the runs cancelled, the cancels GitHub refused)."""
    runs, missed = unfinished_runs(gh, repo)
    if missed:
        say(f"::warning::cancel_superseded: GitHub would not list the merge-group runs in status {', '.join(missed)}; "
            'those are not judged')
    live = queue_facts.live_heads(gh, repo)
    verdicts, hold = decide(runs, live, own_run, own_sha)
    if hold:
        say(f'::warning::cancel_superseded: cancelling nothing: {hold}')
        return [], []
    say(f'cancel_superseded: {len(runs)} unfinished merge-group run(s); {len(live or {})} live queue entries')
    cancelled: list[int] = []
    refused: list[int] = []
    for v in verdicts:
        if v.action != CANCEL:
            say(f'  {v.action:<6} {v.run}  {v.why}')
            continue
        if dry_run:
            say(f'  CANCEL {v.run}  {v.why}  (dry run: not cancelled)')
            continue
        try:
            rc, out, err = gh(['api', '-X', 'POST', f'repos/{repo}/actions/runs/{v.run}/cancel'])
        except ruleset.Unobserved as e:
            rc, out, err = 1, '', str(e)
        if rc == 0:
            cancelled.append(v.run)
            say(f'  CANCEL {v.run}  {v.why}')
        else:
            refused.append(v.run)
            say(f'::warning::cancel_superseded: GitHub refused to cancel run {v.run} ({v.why}): '
                f'{(out + err).strip()[:200]}')
    return cancelled, refused


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def self_test() -> int:
    failures: list[str] = []
    os.environ.pop('GITHUB_STEP_SUMMARY', None)     # its main() calls must not write a CI job's summary (the audits job)

    def expect(label: str, ok: bool, detail: str = '') -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f'  ({detail})' if detail and not ok else ''))
        if not ok:
            failures.append(label)

    def run(rid: int, sha: str, *, event: str = 'merge_group', status: str = 'in_progress', pr: int = 1) -> dict:
        return {'id': rid, 'head_sha': sha, 'event': event, 'status': status,
                'head_branch': f'{QUEUE_BRANCH_PREFIX}pr-{pr}-{sha}'}

    own, own_sha = 100, 'own0'
    live = {own_sha: 7, 'live1': 8}
    runs = [run(own, own_sha), run(101, 'live1'), run(102, 'old1'), run(103, 'old2', status='queued'),
            run(104, 'done1', status='completed'), run(105, 'pr1', event='pull_request'), run(106, 'push1', event='push'),
            {**run(107, 'other1'), 'head_branch': 'gh-readonly-queue/release/pr-9-other1'}, None, {'id': 'x'}]
    verdicts, hold = decide(runs, live, own, own_sha)
    act = {v.run: v.action for v in verdicts}
    expect("a run whose head commit heads a current queue entry is kept", act.get(101) == KEEP and not hold, str(act))
    expect("an unfinished merge-group run whose head commit heads no current queue entry is cancelled, queued or in "
           "progress", act.get(102) == CANCEL and act.get(103) == CANCEL, str(act))
    expect("this run itself is never cancelled, whatever its head commit",
           act.get(own) == KEEP and {v.run: v.action for v in decide([run(own, 'stale')], live, own, own_sha)[0]}
           == {own: KEEP}, str(act))
    expect("a completed run is ignored", act.get(104) == IGNORE, str(act))
    expect("a pull_request or push run is ignored, its head commit in no entry", act.get(105) == IGNORE
           and act.get(106) == IGNORE, str(act))
    expect("a merge-group run of another branch's queue is ignored", act.get(107) == IGNORE, str(act))
    expect("a malformed run item is skipped", set(act) == {100, 101, 102, 103, 104, 105, 106, 107}, str(act))
    for label, queue in (("an unreadable queue (None) cancels nothing", None),
                         ("an empty queue cancels nothing", {}),
                         ("a queue read that does not hold this run's own head commit cancels nothing (a stale read, "
                          "or this run is itself superseded)", {'live1': 8})):
        v, why = decide(runs, queue, own, own_sha)
        expect(label, v == [] and bool(why), f'{v} {why}')

    def fake(listed: dict[str, list[dict]], queue: object, *, cancel_rc: dict[int, int] | None = None,
             log: list[list[str]] | None = None, offline_queue: bool = False) -> Gh:
        def gh(args: list[str]) -> tuple[int, str, str]:
            if log is not None:
                log.append(list(args))
            if args[:2] == ['api', 'graphql']:
                if offline_queue:
                    raise ruleset.Unobserved('no network')
                return 0, json.dumps({'data': {'repository': {'mergeQueue': queue}}}), ''
            if args[:2] == ['api', '-X'] and args[3].endswith('/cancel'):
                rid = int(args[3].split('/')[-2])
                code = (cancel_rc or {}).get(rid, 0)
                return (0, '', '') if code == 0 else (1, '{"message":"Cannot cancel a workflow run that is completed."}',
                                                      'gh: HTTP 409')
            if args[0] == 'api' and '/actions/runs?' in args[1]:
                status = args[1].split('status=')[1].split('&')[0]
                if status not in listed:
                    return 1, '', 'HTTP 502'
                return 0, json.dumps({'workflow_runs': listed[status]}), ''
            return 1, '', 'unrouted'
        return gh

    queue = {'entries': {'pageInfo': {'hasNextPage': False}, 'nodes': [
        {'headCommit': {'oid': own_sha}, 'pullRequest': {'number': 7}},
        {'headCommit': {'oid': 'live1'}, 'pullRequest': {'number': 8}}]}}
    every = {s: [] for s in UNFINISHED}
    listed = {**every, 'in_progress': [run(own, own_sha), run(101, 'live1'), run(102, 'old1')],
              'queued': [run(103, 'old2', status='queued')]}
    log: list[list[str]] = []
    out: list[str] = []
    done, refused = sweep(fake(listed, queue, log=log), 'o/r', own, own_sha, say=out.append)
    cancels = [a[3] for a in log if a[:2] == ['api', '-X']]
    expect("the sweep cancels exactly the superseded runs, through the REST cancel endpoint of this repository",
           done == [102, 103] and refused == [] and cancels == ['repos/o/r/actions/runs/102/cancel',
                                                               'repos/o/r/actions/runs/103/cancel'], f'{done} {cancels}')
    first_query = next(i for i, a in enumerate(log) if a[:2] == ['api', 'graphql'])
    lists = [i for i, a in enumerate(log) if a[0] == 'api' and '/actions/runs?' in a[1]]
    expect("the runs are listed in every unfinished status BEFORE the queue is read (a run created after the read "
           "would otherwise look superseded)",
           len(lists) == len(UNFINISHED) and max(lists) < first_query, str(log))
    log.clear()
    done, refused = sweep(fake(listed, queue, log=log), 'o/r', own, own_sha, dry_run=True, say=out.append)
    expect("a dry run decides and cancels nothing", done == [] and not [a for a in log if a[:2] == ['api', '-X']],
           str(log))
    out.clear()
    done, refused = sweep(fake(listed, queue, cancel_rc={102: 1}), 'o/r', own, own_sha, say=out.append)
    expect("a cancel GitHub refuses (the run finished in between) is reported as a warning and the sweep goes on",
           done == [103] and refused == [102] and any(l.startswith('::warning::') and '102' in l for l in out), str(out))
    for label, gh in (("an offline queue read cancels nothing and says so as a warning",
                       fake(listed, queue, offline_queue=True)),
                      ("no merge queue on main (mergeQueue null) cancels nothing and says so as a warning",
                       fake(listed, None))):
        out.clear()
        log.clear()
        done, refused = sweep(gh, 'o/r', own, own_sha, say=out.append)
        expect(label, done == [] and refused == [] and any('cancelling nothing' in l and l.startswith('::warning::')
                                                           for l in out), str(out))
    out.clear()
    log = []
    partial = {'in_progress': listed['in_progress']}
    done, refused = sweep(fake(partial, queue, log=log), 'o/r', own, own_sha, say=out.append)
    expect("a run list GitHub refuses for some statuses judges only the runs it listed, says which statuses it could "
           "not list, and cancels nothing unseen",
           done == [102] and any('queued' in l and l.startswith('::warning::') for l in out), str(out))
    expect("every outcome exits 0 (housekeeping never decides a verdict); a usage error exits 2",
           main(['--run-id', '100', '--sha', own_sha, '--repo', 'o/r', '--dry-run'], gh=fake(listed, queue)) == 0
           and main(['--run-id', '100', '--sha', own_sha, '--repo', 'o/r'], gh=fake(listed, None)) == 0
           and main(['--sha', own_sha], gh=fake(listed, queue)) == 2)
    print(f"=== cancel_superseded.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None, gh: Gh | None = None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument('--self-test', action='store_true')
    ap.add_argument('--run-id', type=int, default=0, help="this workflow run's id (github.run_id): never cancelled")
    ap.add_argument('--sha', default='', help="this run's head commit (github.sha): must head a live entry")
    ap.add_argument('--repo', default='', help='OWNER/NAME (default: the repository gh resolves)')
    ap.add_argument('--dry-run', action='store_true', help='decide and print, cancel nothing')
    try:
        a = ap.parse_args(argv)
    except SystemExit as e:
        return 2 if e.code else 0
    if a.self_test:
        return self_test()
    if a.run_id <= 0 or not a.sha:
        print('cancel_superseded: --run-id and --sha are required (this run is never cancelled, and its head commit '
              'must head a live entry)')
        return 2
    run = gh or ruleset.real_gh
    try:
        repo = a.repo or ruleset.repo_name(run)
    except ruleset.Unobserved as e:
        print(f'::warning::cancel_superseded: cancelling nothing: the repository could not be resolved ({e})')
        return 0
    cancelled, refused = sweep(run, repo, a.run_id, a.sha, a.dry_run)
    print(f'cancel_superseded: {len(cancelled)} superseded run(s) cancelled'
          + (f', {len(refused)} refused' if refused else '') + (' (dry run)' if a.dry_run else ''))
    summary = os.environ.get('GITHUB_STEP_SUMMARY', '')
    if summary:
        with open(summary, 'a', encoding='utf-8', newline='\n') as f:
            f.write(f"### supersede{chr(10)}{chr(10)}cancelled: {', '.join(map(str, cancelled)) or 'none'}"
                    + (f"; refused: {', '.join(map(str, refused))}" if refused else '') + chr(10))
    return 0


if __name__ == '__main__':
    ensure_utf8()
    sys.exit(main())
