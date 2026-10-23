#!/usr/bin/env python3
"""queue_facts.py — THE ONE READER OF GITHUB'S FACTS ABOUT THE OPEN PULL REQUESTS INTO MAIN AND THE MERGE QUEUE
(kb/Work PB3023, PB3527; DESIGN-orchestrator-loop.md section 4.9; DESIGN-test-build-ci.md section 3.17.2 step 3).

    python scripts/orchestrator/queue_facts.py            # each open pull request into main, one line each
    python scripts/orchestrator/queue_facts.py --self-test

Two readers use it and nothing else asks GitHub about a pull request's place in the queue: the branch survey
(prune_worktrees.live_survey: ENQUEUED, NOT QUEUED, BOUNCED) and the landing lane's queue watch (land_lane.py tick,
kb/Work PB3527), which takes the facts the survey read on the same tick.

WHAT ONE READ RETURNS ({head branch: PullRequest}, or None when GitHub could not be asked):
  - ONE GraphQL query: the merge queue's own entries on main (`mergeQueue(branch: "main")`, the queue's record of who
    is in it) and every OPEN pull request into main with its head commit, `isInMergeQueue`, `mergeStateStatus`, when
    auto-merge was enabled (`autoMergeRequest.enabledAt`), the newest RemovedFromMergeQueueEvent, and the head commit's
    check-runs with the workflow run of each;
  - for each REQUIRED context (ruleset.required_contexts: the committed ruleset's `required_status_checks`) the newest
    check-run on the head commit; and only while that check-run is UNRESOLVED (not COMPLETED), its Actions job's own
    status from the REST jobs API (`actions/jobs/<id>`: for a GitHub Actions check-run the check-run id IS the job id).
    A check-run still in progress whose job has completed is a PROPAGATION FAULT (PR #385, 2026-10-22: job `ci-gate`
    completed success at 18:46:46Z, its check-run on the head commit stayed in_progress for 50 minutes, the queue
    empty, the merge state BLOCKED): `CheckRun.stuck`.
A pull request is IN THE QUEUE when the queue's entries name it or GitHub says `isInMergeQueue`. Auto-merge on is NOT
in the queue: it is GitHub's promise to queue the pull request once its required checks pass, and that promise is what
PR #385 broke. Offline, unauthenticated, with no gh, or on any malformed answer the read is None: a non-observation,
never a claim that nothing is open or queued (a job that cannot be read leaves that check-run's `job_status` empty, so
it never reads stuck).
"""
from __future__ import annotations

import argparse
import dataclasses
import json
import pathlib
import sys
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import ruleset  # noqa: E402  (its gh runner: a hang guard, and Unobserved on any failure to read; the required contexts)

Gh = Callable[[list[str]], tuple[int, str, str]]
#: A check-run conclusion that keeps a required check from passing (GraphQL CheckConclusionState).
FAILED = ("FAILURE", "TIMED_OUT", "CANCELLED", "ACTION_REQUIRED", "STARTUP_FAILURE", "STALE")

QUERY = ('query($owner:String!,$name:String!){repository(owner:$owner,name:$name){'
         'mergeQueue(branch:"main"){entries(first:100){nodes{state enqueuedAt pullRequest{number}}}}'
         'pullRequests(states:OPEN,baseRefName:"main",first:100){nodes{number url headRefName headRefOid '
         'isInMergeQueue mergeStateStatus autoMergeRequest{enabledAt} '
         'timelineItems(itemTypes:[REMOVED_FROM_MERGE_QUEUE_EVENT],last:1){nodes{'
         '... on RemovedFromMergeQueueEvent{reason createdAt}}} '
         'commits(last:1){nodes{commit{oid checkSuites(first:20){nodes{workflowRun{databaseId} '
         'checkRuns(first:50){nodes{databaseId name status conclusion completedAt}}}}}}}}}}}')


@dataclasses.dataclass(frozen=True)
class CheckRun:
    """The newest check-run of one REQUIRED context on a pull request's head commit."""
    name: str
    status: str                 # GraphQL CheckStatusState: QUEUED, IN_PROGRESS, COMPLETED, WAITING, PENDING, REQUESTED
    conclusion: str             # GraphQL CheckConclusionState ('' while unresolved)
    job: int                    # its databaseId: for GitHub Actions, the job id
    run: int                    # its workflow run's databaseId (0: not an Actions check-run)
    completed_at: str = ''
    job_status: str = ''        # the Actions job's own status (REST), read only while the check-run is unresolved
    job_completed_at: str = ''

    @property
    def resolved(self) -> bool:
        return self.status == 'COMPLETED'

    @property
    def stuck(self) -> bool:
        """The propagation fault: the check-run is unresolved, but the job that reports it has completed."""
        return not self.resolved and self.job_status == 'completed'

    @property
    def failed(self) -> bool:
        return self.resolved and self.conclusion in FAILED

    @property
    def done_at(self) -> str:
        """When the work behind the check ended: the check-run's completion, else (stuck) its job's; '' while it runs."""
        return self.completed_at if self.resolved else self.job_completed_at if self.stuck else ''


@dataclasses.dataclass(frozen=True)
class PullRequest:
    number: int
    url: str
    head: str                       # the PR's head commit
    in_queue: bool                  # the merge queue holds it (its entries name it, or isInMergeQueue)
    auto_merge_at: str = ''         # when auto-merge was enabled ('' off): GitHub queues it once its required checks pass
    merge_state: str = ''           # mergeStateStatus (BLOCKED, CLEAN, BEHIND, UNKNOWN, ...)
    removed: str = ''               # the queue removed it: GitHub's reason and time (the newest RemovedFromMergeQueueEvent)
    checks: tuple[CheckRun, ...] = ()   # the required contexts' newest check-runs on the head commit
    repo: str = ''                  # owner/name: the repository the read asked about (a remedy names it)

    @property
    def bounced(self) -> str:
        """The queue removed it and nothing has queued it again (not in the queue, auto-merge off): GitHub's reason."""
        return '' if self.in_queue or self.auto_merge_at else self.removed

    @property
    def conflicts(self) -> bool:
        """GitHub cannot create its merge commit (mergeStateStatus DIRTY): the branch conflicts with main, so neither
        auto-merge nor a re-run nor a reopen can queue it; only the branch can, by bringing main in (MANDATORY-PRACTICES
        L12: a finisher). Read before the queue takes it (kb/Work PB3527 review R1-5)."""
        return not self.in_queue and self.merge_state == 'DIRTY'

    @property
    def red(self) -> CheckRun | None:
        """A required check that FAILED on the head before the queue took it: the pull request cannot enter the queue
        until the branch changes (kb/Work PB3096's pre-queue red, PB3527)."""
        return None if self.in_queue else next((c for c in self.checks if c.failed), None)


def _required_runs(commit: dict, required: tuple[str, ...]) -> list[CheckRun]:
    """The newest check-run of each required context on the commit (the highest databaseId: a re-run's is newer)."""
    newest: dict[str, CheckRun] = {}
    for suite in ((commit.get('checkSuites') or {}).get('nodes') or []):
        if not isinstance(suite, dict):
            continue
        run = int(((suite.get('workflowRun') or {}).get('databaseId')) or 0)
        for c in ((suite.get('checkRuns') or {}).get('nodes') or []):
            if not isinstance(c, dict) or c.get('name') not in required:
                continue
            cr = CheckRun(str(c['name']), str(c.get('status') or ''), str(c.get('conclusion') or ''),
                          int(c.get('databaseId') or 0), run, str(c.get('completedAt') or ''))
            if c['name'] not in newest or cr.job > newest[c['name']].job:
                newest[c['name']] = cr
    return [newest[n] for n in required if n in newest]


def _with_job(gh: Gh, repo: str, cr: CheckRun) -> CheckRun:
    """An unresolved Actions check-run with its job's own status; one that cannot be read stays as it was (unknown,
    never stuck)."""
    if cr.resolved or not cr.job or not cr.run:
        return cr
    rc, out, _ = gh(['api', f'repos/{repo}/actions/jobs/{cr.job}'])
    try:
        job = json.loads(out) if rc == 0 else {}
    except ValueError:
        job = {}
    if not isinstance(job, dict) or not job.get('status'):
        return cr
    return dataclasses.replace(cr, job_status=str(job['status']), job_completed_at=str(job.get('completed_at') or ''))


def read(gh: Gh | None = None, required: tuple[str, ...] | None = None) -> dict[str, PullRequest] | None:
    """{head branch: its OPEN pull request into main} (module doc), or None when GitHub could not be asked."""
    run = gh or ruleset.real_gh
    try:
        required = required if required is not None else ruleset.required_contexts(ruleset.load_committed())
        rc, out, _ = run(['repo', 'view', '--json', 'nameWithOwner', '--jq', '.nameWithOwner'])
        repo = out.strip()
        owner, _, name = repo.partition('/')
        if rc != 0 or not name:
            return None
        rc, out, _ = run(['api', 'graphql', '-f', f'query={QUERY}', '-F', f'owner={owner}', '-F', f'name={name}'])
        if rc != 0:
            return None
        data = json.loads(out)['data']['repository']
        nodes = data['pullRequests']['nodes']
        entries = ((data.get('mergeQueue') or {}).get('entries') or {}).get('nodes') or []
        queued = {int((e.get('pullRequest') or {}).get('number') or 0) for e in entries if isinstance(e, dict)}
        prs: dict[str, PullRequest] = {}
        for n in nodes if isinstance(nodes, list) else ():
            if not isinstance(n, dict) or not n.get('headRefName'):
                continue
            number = int(n.get('number') or 0)
            removed = [e for e in ((n.get('timelineItems') or {}).get('nodes') or []) if isinstance(e, dict) and e]
            commits = [c.get('commit') or {} for c in ((n.get('commits') or {}).get('nodes') or []) if isinstance(c, dict)]
            checks = _required_runs(commits[-1], required) if commits else []
            prs[str(n['headRefName'])] = PullRequest(
                number, str(n.get('url') or ''), str(n.get('headRefOid') or ''),
                number in queued or bool(n.get('isInMergeQueue')),
                str((n.get('autoMergeRequest') or {}).get('enabledAt') or ''), str(n.get('mergeStateStatus') or ''),
                f"{removed[-1].get('reason') or 'no reason given'} at {removed[-1].get('createdAt') or '?'}"
                if removed else '',
                tuple(_with_job(run, repo, c) for c in checks), repo)
        return prs
    except (ruleset.Unobserved, ValueError, KeyError, TypeError, AttributeError):
        return None


def describe(pr: PullRequest) -> str:
    """One line of what GitHub says about the pull request's way into the queue (the survey and the watch say it)."""
    checks = '; '.join(f'{c.name} {c.status.lower() or "?"}' + (f' {c.conclusion.lower()}' if c.conclusion else '')
                       + (f', its job {c.job_status} {c.job_completed_at}' if c.job_status and not c.resolved else '')
                       for c in pr.checks) or 'no required check-run on the head yet'
    return (f'merge state {pr.merge_state or "?"}; {checks}'
            + (f'; auto-merge on since {pr.auto_merge_at}' if pr.auto_merge_at else '; auto-merge off'))


# ── self-test ────────────────────────────────────────────────────────────────────────────────────────────────────
def fake(prs: list[dict], entries: list[int] = (), jobs: dict[int, dict] | None = None,
         log: list[list[str]] | None = None) -> Gh:
    """A gh that answers the repository's name, the one GraphQL query and the jobs API from planted data."""
    def gh(args: list[str]) -> tuple[int, str, str]:
        if log is not None:
            log.append(list(args))
        if args[:2] == ['repo', 'view']:
            return 0, 'o/r\n', ''
        if args[:2] == ['api', 'graphql']:
            return 0, json.dumps({'data': {'repository': {
                'mergeQueue': {'entries': {'nodes': [{'state': 'QUEUED', 'pullRequest': {'number': n}} for n in entries]}},
                'pullRequests': {'nodes': prs}}}}), ''
        if args[0] == 'api' and '/actions/jobs/' in args[1]:
            job = (jobs or {}).get(int(args[1].rsplit('/', 1)[1]))
            return (0, json.dumps(job), '') if job else (1, '', 'HTTP 404')
        return 1, '', 'unrouted'
    return gh


def node(number: int, branch: str, head: str, *, in_queue: bool = False, auto: str = '', state: str = 'BLOCKED',
         removed: str = '', runs: list[dict] = ()) -> dict:
    """A planted GraphQL pull-request node; `runs`: check-runs of one suite of workflow run 900 + number."""
    return {'number': number, 'url': f'u{number}', 'headRefName': branch, 'headRefOid': head, 'isInMergeQueue': in_queue,
            'mergeStateStatus': state, 'autoMergeRequest': {'enabledAt': auto} if auto else None,
            'timelineItems': {'nodes': [{'reason': removed, 'createdAt': '2026-10-22T18:00:00Z'}] if removed else []},
            'commits': {'nodes': [{'commit': {'oid': head, 'checkSuites': {'nodes': [
                {'workflowRun': {'databaseId': 900 + number}, 'checkRuns': {'nodes': list(runs)}}]}}}]}}


def self_test() -> int:
    failures: list[str] = []

    def expect(label: str, ok: bool, detail: str = '') -> None:
        print(f"  {'PASS' if ok else 'FAIL'}  {label}" + (f'  ({detail})' if detail and not ok else ''))
        if not ok:
            failures.append(label)

    req = ('ci-gate',)
    gate = lambda job, status, concl='', done='': {'databaseId': job, 'name': 'ci-gate', 'status': status,  # noqa: E731
                                                   'conclusion': concl or None, 'completedAt': done or None}
    log: list[list[str]] = []
    prs = read(fake([
        node(1, 'queued', 'a1', runs=[gate(11, 'COMPLETED', 'SUCCESS', '2026-10-22T18:00:00Z')]),
        node(2, 'flag', 'b1', in_queue=True),
        node(3, 'auto', 'c1', auto='2026-10-22T18:44:50Z', runs=[gate(31, 'IN_PROGRESS')]),
        node(4, 'stuck', 'd1', auto='2026-10-22T18:44:50Z',
             runs=[gate(41, 'IN_PROGRESS'), {**gate(40, 'COMPLETED', 'FAILURE', 'x'), 'name': 'audits'}]),
        node(5, 'removed', 'e1', removed='FAILED_CHECKS'),
        node(6, 'red', 'f1', auto='2026-10-22T18:44:50Z', runs=[gate(61, 'COMPLETED', 'FAILURE', 'y'),
                                                               gate(60, 'COMPLETED', 'SUCCESS', 'z')]),
        node(7, 'rerun', 'g1', auto='2026-10-22T18:44:50Z', runs=[gate(70, 'IN_PROGRESS'),
                                                                 gate(71, 'COMPLETED', 'SUCCESS', '2026-10-22T19:36:04Z')]),
    ], entries=[1], jobs={31: {'status': 'in_progress'}, 41: {'status': 'completed', 'completed_at': '2026-10-22T18:46:46Z'},
                          70: {'status': 'completed', 'completed_at': 'w'}}, log=log), required=req) or {}
    expect("a pull request the merge queue's own entries name, or that GitHub says isInMergeQueue, is in the queue; "
           "auto-merge on is NOT in the queue",
           prs['queued'].in_queue and prs['flag'].in_queue and not prs['auto'].in_queue
           and prs['auto'].auto_merge_at == '2026-10-22T18:44:50Z' and prs['auto'].repo == 'o/r', str(prs))
    stuck = prs['stuck'].checks
    expect("a required check-run in progress whose Actions job completed is STUCK (PR #385's propagation fault), with "
           "the job's completion as its time; one whose job still runs is not; an unrequired check is not read",
           [c.name for c in stuck] == ['ci-gate'] and stuck[0].stuck and stuck[0].done_at == '2026-10-22T18:46:46Z'
           and stuck[0].run == 904 and not prs['auto'].checks[0].stuck and prs['auto'].checks[0].done_at == '',
           f'{stuck} {prs["auto"].checks}')
    jobs_read = sorted(a[1].rsplit('/', 1)[1] for a in log if a[0] == 'api' and '/actions/jobs/' in a[1])
    expect("only an unresolved required check-run asks the jobs API; the newest check-run of a context decides (a "
           "re-run's), so a re-run that completed is not stuck",
           jobs_read == ['31', '41'] and prs['rerun'].checks[0].job == 71 and not prs['rerun'].checks[0].stuck,
           f'{jobs_read} {prs["rerun"].checks}')
    expect("a pull request the queue removed with auto-merge off is BOUNCED with GitHub's reason; a required check "
           "that FAILED before the queue is its red (the newest run decides)",
           prs['removed'].bounced.startswith('FAILED_CHECKS') and prs['red'].red is not None
           and prs['red'].red.job == 61 and prs['stuck'].red is None and not prs['queued'].bounced,
           f'{prs["removed"]} {prs["red"]}')
    dirty = read(fake([node(9, 'dirty', 'i1', auto='t', state='DIRTY', runs=[gate(91, 'COMPLETED', 'SUCCESS', 'v')]),
                       node(10, 'dirtyq', 'j1', in_queue=True, state='DIRTY')]), required=req) or {}
    expect("a pull request whose merge commit GitHub cannot create (mergeStateStatus DIRTY) CONFLICTS with main, its "
           "checks green or not; one the queue already holds does not (kb/Work PB3527 review R1-5)",
           dirty['dirty'].conflicts and dirty['dirty'].red is None and not dirty['dirtyq'].conflicts
           and not prs['auto'].conflicts, str(dirty))
    lost = read(fake([node(8, 'lost', 'h1', auto='t', runs=[gate(81, 'IN_PROGRESS')])], jobs={}), required=req) or {}
    expect("a job the jobs API cannot read leaves the check-run unknown, never stuck",
           not lost['lost'].checks[0].stuck and lost['lost'].checks[0].job_status == '', str(lost))

    def offline(args: list[str]) -> tuple[int, str, str]:
        raise ruleset.Unobserved('no network')
    expect("offline, a gh failure or a malformed answer is None (a non-observation), never {}",
           read(offline, required=req) is None and read(lambda a: (1, '', 'x'), required=req) is None
           and read(lambda a: (0, 'o/r' if a[0] == 'repo' else '{"data":{}}', ''), required=req) is None)
    expect("the required contexts come from the committed ruleset (ruleset.required_contexts)",
           ruleset.required_contexts(ruleset.load_committed()) == ('ci-gate',))
    print(f"=== queue_facts.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
    try:
        sys.stdout.reconfigure(encoding='utf-8')
    except AttributeError:
        pass
    ap = argparse.ArgumentParser(description=__doc__.split(chr(10))[0])
    ap.add_argument('--self-test', action='store_true')
    a = ap.parse_args(argv)
    if a.self_test:
        return self_test()
    prs = read()
    if prs is None:
        print('queue_facts: GitHub could not be asked (offline, no gh, or not authenticated): NOT OBSERVED')
        return 2
    for branch, pr in sorted(prs.items(), key=lambda kv: kv[1].number):
        where = ('IN THE QUEUE' if pr.in_queue else 'RED' if pr.red else 'CONFLICTS' if pr.conflicts
                 else 'BOUNCED' if pr.bounced else 'NOT QUEUED')
        print(f'#{pr.number} {branch} @{pr.head[:12]}: {where} — {describe(pr)}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
