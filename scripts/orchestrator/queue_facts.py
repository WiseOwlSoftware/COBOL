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
    auto-merge was enabled (`autoMergeRequest.enabledAt`), the newest RemovedFromMergeQueueEvent, whether a commit was
    pushed after it (the last 100 removal and commit timeline items: `headRefOid` is the CURRENT head, which every push
    moves, so a removal is read against the head it was observed at, kb/Work PB3275), and the head commit's check-runs
    with the workflow run of each;
  - for each REQUIRED context (ruleset.required_contexts: the committed ruleset's `required_status_checks`) the newest
    check-run on the head commit; and only while that check-run is UNRESOLVED (not COMPLETED), its Actions job's own
    status from the REST jobs API (`actions/jobs/<id>`: for a GitHub Actions check-run the check-run id IS the job id).
    A check-run still in progress whose job has completed is a PROPAGATION FAULT (PR #385, 2026-10-22: job `ci-gate`
    completed success at 18:46:46Z, its check-run on the head commit stayed in_progress for 50 minutes, the queue
    empty, the merge state BLOCKED): `CheckRun.stuck`.
A third caller asks for the REDS of a bounce (kb/Work PB3644): `fetch_bounce_results` downloads the test results of the
newest failed merge-group run of a branch's open pull request into the worktree (`gate_plan.CI_BOUNCE_RESULTS`), where the
gate planner reads them as tier 0a, called by `run_gate_legs.Host.fetch_bounce_results` before every gate's plan.
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
import re
import shutil
import sys
from typing import Callable

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import ruleset  # noqa: E402  (its gh runner: a hang guard, and Unobserved on any failure to read; the required contexts)
sys.path.append(str(pathlib.Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

Gh = Callable[[list[str]], tuple[int, str, str]]
#: A check-run conclusion that keeps a required check from passing (GraphQL CheckConclusionState).
FAILED = ("FAILURE", "TIMED_OUT", "CANCELLED", "ACTION_REQUIRED", "STARTUP_FAILURE", "STALE")

QUERY = ('query($owner:String!,$name:String!){repository(owner:$owner,name:$name){'
         'mergeQueue(branch:"main"){entries(first:100){nodes{state enqueuedAt pullRequest{number}}}}'
         'pullRequests(states:OPEN,baseRefName:"main",first:100){nodes{number url headRefName headRefOid '
         'isInMergeQueue mergeStateStatus autoMergeRequest{enabledAt} '
         'timelineItems(itemTypes:[REMOVED_FROM_MERGE_QUEUE_EVENT],last:1){nodes{'
         '... on RemovedFromMergeQueueEvent{reason createdAt}}} '
         'pushes:timelineItems(itemTypes:[REMOVED_FROM_MERGE_QUEUE_EVENT,PULL_REQUEST_COMMIT],last:100){nodes{__typename}} '
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
    pushed_since_removal: bool = False  # a commit reached the branch AFTER the newest queue removal: a head the queue never saw

    @property
    def bounced(self) -> str:
        """The queue removed it AT THIS HEAD and nothing has queued it again (not in the queue, auto-merge off, nothing
        pushed since): GitHub's reason. `head` is the pull request's CURRENT headRefOid, which every push moves, so the
        removal is read against the head it was observed at: a push after it is the fix, a head the queue has not seen,
        and the branch reads by its report like an unpushed finisher tip (kb/Work PB3275 variant 3)."""
        return '' if self.in_queue or self.auto_merge_at or self.pushed_since_removal else self.removed

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


def pushed_since_removal(timeline: list) -> bool:
    """Whether a PullRequestCommit follows the newest RemovedFromMergeQueueEvent in the timeline window (oldest first,
    the last 100 of the two kinds). A window with no removal holds only commits, all newer than a removal older than
    the window, so a commit in it is a push since; the caller asks only for a pull request the queue removed."""
    kinds = [str(i.get('__typename') or '') for i in timeline if isinstance(i, dict)]
    last_removal = max((k for k, kind in enumerate(kinds) if kind == 'RemovedFromMergeQueueEvent'), default=-1)
    return 'PullRequestCommit' in kinds[last_removal + 1:]


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
                tuple(_with_job(run, repo, c) for c in checks), repo,
                bool(removed) and pushed_since_removal((n.get('pushes') or {}).get('nodes') or []))
        return prs
    except (ruleset.Unobserved, ValueError, KeyError, TypeError, AttributeError):
        return None


# ── THE REDS OF A MERGE-QUEUE BOUNCE (kb/Work PB3644) ──────────────────────────────────────────────────────────────
#: A merge group's workflow run is on the branch `gh-readonly-queue/<base>/pr-<number>-<sha>`.
MERGE_GROUP_BRANCH = re.compile(r'^gh-readonly-queue/[^/]+/pr-(\d+)-[0-9a-f]+$')
#: A workflow run conclusion (REST) that is a test failure, so its artifacts hold the reds. `cancelled` is not one: the
#: queue cancels a merge group when an earlier entry fails, and a case that never ran is no red.
FAILED_RUNS = ('failure', 'timed_out')
#: The artifacts of build-and-test.yml's test jobs (./.github/actions/upload-results): the TRX files of every shard.
RESULTS_PATTERN = 'test-results-*'
BOUNCE_SOURCE = 'SOURCE.txt'


def bounce_run(runs: list, number: int) -> dict | None:
    """The newest merge-group run of pull request `number` among `runs` (the REST list, any order), or None."""
    mine = [r for r in runs if isinstance(r, dict) and r.get('event', 'merge_group') == 'merge_group'
            and (m := MERGE_GROUP_BRANCH.match(str(r.get('head_branch') or ''))) and int(m.group(1)) == number]
    return max(mine, key=lambda r: int(r.get('id') or 0), default=None)


def fetch_bounce_results(branch: str, dest: pathlib.Path, gh: Gh | None = None) -> str:
    """THE REDS OF A BOUNCE, where the gate planner reads them (kb/Work PB3644; gate_plan.CI_BOUNCE_RESULTS): the test
    results (TRX) of the newest FAILED merge-group run of `branch`'s open pull request, downloaded into `dest`. The
    queue's bounce lives in GitHub's CI run, never in a worktree, so a finisher's re-gate planned from local files alone
    ran the failed cases by impact like any other (tier 0a is for exactly them). Returns one line saying what `dest`
    now holds. `dest` is REPLACED by the newest bounce's results, EMPTIED when the pull request has no failed
    merge-group run (a stale bounce's reds would mis-order the next plan), and LEFT AS IT WAS when GitHub cannot be
    asked (a non-observation changes nothing). It orders the gate and never selects it, so a miss costs only the order."""
    run = gh or ruleset.real_gh
    try:
        rc, out, _ = run(['repo', 'view', '--json', 'nameWithOwner', '--jq', '.nameWithOwner'])
        repo = out.strip()
        if rc != 0 or '/' not in repo:
            return f'ci bounce: not observed (gh repo view exited {rc}); {dest} left as it was'
        rc, out, _ = run(['pr', 'list', '--head', branch, '--state', 'open', '--json', 'number', '--jq', '.[0].number'])
        if rc != 0:
            return f'ci bounce: not observed (gh pr list exited {rc}); {dest} left as it was'
        if not out.strip().isdigit():
            return _no_bounce(dest, f'{branch} has no open pull request')
        number = int(out.strip())
        rc, out, _ = run(['api', f'repos/{repo}/actions/runs?event=merge_group&per_page=100'])
        listed = json.loads(out).get('workflow_runs') if rc == 0 else None
        if not isinstance(listed, list):
            return f'ci bounce: not observed (the merge-group run list of pull request #{number} was unreadable); {dest} left as it was'
        newest = bounce_run(listed, number)
        if newest is None or newest.get('conclusion') not in FAILED_RUNS:
            return _no_bounce(dest, f'pull request #{number} has no failed merge-group run among the newest {len(listed)}'
                                    if newest is None else
                                    f'its newest merge-group run {newest.get("id")} concluded {newest.get("conclusion")}')
        marker = f'GitHub Actions run {newest["id"]} ('
        try:
            fetched = (dest / BOUNCE_SOURCE).read_text(encoding='utf-8').startswith(marker)
        except OSError:
            fetched = False
        if fetched:   # a bounce's results are some 200 MB: a re-gate of the same bounce downloads them once
            return f'ci bounce: the failed merge-group run {newest["id"]} of pull request #{number}: its test results are already in {dest}'
        shutil.rmtree(dest, ignore_errors=True)
        dest.mkdir(parents=True)
        rc, _, err = run(['run', 'download', str(newest['id']), '--repo', repo, '--pattern', RESULTS_PATTERN,
                          '--dir', str(dest)])
        if rc != 0:
            return (f'ci bounce: run {newest["id"]} of pull request #{number} failed, but its test results could not be '
                    f'downloaded ({(err.strip().splitlines() or ["no reason given"])[0]}; expired or never uploaded): '
                    f'{dest} is empty')
        (dest / BOUNCE_SOURCE).write_text(
            f'{marker}merge group of pull request #{number}, {newest.get("conclusion")}, {newest.get("created_at") or "?"}); '
            'fetched by queue_facts.fetch_bounce_results (kb/Work PB3644)\n', encoding='utf-8', newline='\n')
        return f'ci bounce: the failed merge-group run {newest["id"]} of pull request #{number}: its test results are in {dest}'
    except (ruleset.Unobserved, ValueError, KeyError, TypeError, AttributeError, OSError) as e:
        return f'ci bounce: not observed ({type(e).__name__}: {str(e).splitlines()[0] if str(e) else ""}); {dest} left as it was'


def _no_bounce(dest: pathlib.Path, why: str) -> str:
    shutil.rmtree(dest, ignore_errors=True)
    return f'ci bounce: none ({why})'


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
         removed: str = '', runs: list[dict] = (), pushed: int = 0) -> dict:
    """A planted GraphQL pull-request node; `runs`: check-runs of one suite of workflow run 900 + number; `pushed`:
    commits pushed to the branch after the queue removed it (the `pushes` window: two commits, the removal, then those)."""
    commit, removal = {'__typename': 'PullRequestCommit'}, {'__typename': 'RemovedFromMergeQueueEvent'}
    return {'number': number, 'url': f'u{number}', 'headRefName': branch, 'headRefOid': head, 'isInMergeQueue': in_queue,
            'mergeStateStatus': state, 'autoMergeRequest': {'enabledAt': auto} if auto else None,
            'timelineItems': {'nodes': [{'reason': removed, 'createdAt': '2026-10-22T18:00:00Z'}] if removed else []},
            'pushes': {'nodes': [commit, commit] + ([removal] if removed else []) + [commit] * pushed},
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
    fixed = read(fake([node(11, 'fixed', 'k1', removed='FAILED_CHECKS', pushed=3),
                       node(12, 'again', 'l1', removed='FAILED_CHECKS', auto='t', pushed=1),
                       node(13, 'old', 'm1', removed='FAILED_CHECKS')]), required=req) or {}
    expect("a pull request the queue removed is BOUNCED only at the head it was removed at: a commit pushed after the "
           "removal makes its current head a new one (PB3275 variant 3: the head is headRefOid, which every push moves), "
           "so it is not bounced; one with no push since still is",
           not fixed['fixed'].bounced and fixed['fixed'].removed.startswith('FAILED_CHECKS')
           and fixed['fixed'].pushed_since_removal and not fixed['again'].bounced and fixed['old'].bounced
           and not fixed['old'].pushed_since_removal and not prs['removed'].pushed_since_removal, str(fixed))
    expect("the timeline window is read oldest first: commits before the newest removal are no push since; with no "
           "removal in the window every commit in it is newer than the removal; a malformed item counts for nothing",
           not pushed_since_removal([{'__typename': 'PullRequestCommit'}, {'__typename': 'RemovedFromMergeQueueEvent'}])
           and pushed_since_removal([{'__typename': 'RemovedFromMergeQueueEvent'}, {'__typename': 'PullRequestCommit'}])
           and pushed_since_removal([{'__typename': 'PullRequestCommit'}, {'__typename': 'RemovedFromMergeQueueEvent'},
                                     {'__typename': 'RemovedFromMergeQueueEvent'}, {'__typename': 'PullRequestCommit'}])
           and not pushed_since_removal([{'__typename': 'RemovedFromMergeQueueEvent'}, {'__typename': 'PullRequestCommit'},
                                         {'__typename': 'RemovedFromMergeQueueEvent'}])
           and pushed_since_removal([{'__typename': 'PullRequestCommit'}]) and not pushed_since_removal([None, {}, 'x'])
           and not pushed_since_removal([]))
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
    # THE REDS OF A BOUNCE (kb/Work PB3644): the failed merge-group run's artifacts, into the worktree.
    import tempfile  # noqa: PLC0415
    group = lambda n, rid, concl, sha='abc123': {'id': rid, 'head_branch': f'gh-readonly-queue/main/pr-{n}-{sha}',  # noqa: E731
                                                  'conclusion': concl, 'created_at': f'2026-10-24T10:{rid % 60:02d}:00Z'}

    def bounce_gh(runs: list[dict], *, pr: str = '547', download_rc: int = 0, runs_rc: int = 0,
                  asked: list[list[str]] | None = None) -> Gh:
        def gh(args: list[str]) -> tuple[int, str, str]:
            if asked is not None:
                asked.append(list(args))
            if args[:2] == ['repo', 'view']:
                return 0, 'o/r\n', ''
            if args[:2] == ['pr', 'list']:
                return 0, pr + '\n', ''
            if args[0] == 'api':
                return (0, json.dumps({'workflow_runs': runs}), '') if runs_rc == 0 else (1, '', 'HTTP 502')
            if args[:2] == ['run', 'download']:
                if download_rc:
                    return download_rc, '', 'no valid artifacts found to download\nmore'
                art = pathlib.Path(args[args.index('--dir') + 1]) / 'test-results-Linux-unit'
                art.mkdir(parents=True)
                (art / 'unit.trx').write_text('<TestRun/>', encoding='utf-8', newline='\n')
                return 0, '', ''
            return 1, '', 'unrouted'
        return gh

    with tempfile.TemporaryDirectory() as tmp:
        dest = pathlib.Path(tmp) / 'ci-bounce'
        asked: list[list[str]] = []
        runs = [group(5, 900, 'failure'), group(54, 901, 'failure'), group(547, 700, 'failure', 'old'),
                group(547, 800, 'failure'), group(547, 750, 'success')]
        note = fetch_bounce_results('fix', dest, bounce_gh(runs, asked=asked))
        downloaded = [a for a in asked if a[:2] == ['run', 'download']]
        expect("a bounce fetches the newest FAILED merge-group run of the branch's open pull request (pr-547, never pr-5 "
               "or pr-54), its test-results artifacts only, into the directory the planner reads, with its provenance",
               'the failed merge-group run 800 of pull request #547' in note
               and len(downloaded) == 1 and downloaded[0][2] == '800' and '--pattern' in downloaded[0]
               and downloaded[0][downloaded[0].index('--pattern') + 1] == RESULTS_PATTERN
               and (dest / 'test-results-Linux-unit' / 'unit.trx').is_file()
               and 'run 800' in (dest / BOUNCE_SOURCE).read_text(encoding='utf-8'), f'{note} {asked}')
        asked.clear()
        again = fetch_bounce_results('fix', dest, bounce_gh(runs, asked=asked))
        expect("the same bounce is downloaded once: a re-gate finds its provenance marker and asks GitHub no download",
               'already in' in again and not [a for a in asked if a[:2] == ['run', 'download']], f'{again} {asked}')
        expect("the newest merge-group run decides: a pull request whose newest run PASSED, or was CANCELLED by the queue "
               "(a case that never ran is no red), is no bounce, and the previous bounce's results are EMPTIED",
               'none' in fetch_bounce_results('fix', dest, bounce_gh([group(547, 800, 'failure'), group(547, 850, 'success')]))
               and not dest.exists(), str(dest))
        fetch_bounce_results('fix', dest, bounce_gh(runs))
        cancelled = fetch_bounce_results('fix', dest, bounce_gh([group(547, 800, 'failure'), group(547, 900, 'cancelled')]))
        expect("a cancelled merge group is no bounce", 'none' in cancelled and 'cancelled' in cancelled and not dest.exists(),
               cancelled)
        fetch_bounce_results('fix', dest, bounce_gh(runs))
        nopr = fetch_bounce_results('fix', dest, bounce_gh(runs, pr='null'))
        expect("a branch with no open pull request has no bounce: nothing is downloaded and old results are emptied",
               'none' in nopr and 'no open pull request' in nopr and not dest.exists(), nopr)
        fetch_bounce_results('fix', dest, bounce_gh(runs))
        gone = fetch_bounce_results('fix', dest, bounce_gh([*runs, group(547, 810, 'failure')], download_rc=1))
        expect("a failed run whose artifacts cannot be downloaded (expired, never uploaded) says so, naming gh's reason, "
               "and leaves the directory empty, never the previous bounce's reds",
               'could not be downloaded (no valid artifacts found to download' in gone and dest.is_dir()
               and not any(dest.iterdir()), gone)
        fetch_bounce_results('fix', dest, bounce_gh(runs))
        unread = [fetch_bounce_results('fix', dest, bounce_gh(runs, runs_rc=1)),
                  fetch_bounce_results('fix', dest, lambda a: (1, '', 'x')),
                  fetch_bounce_results('fix', dest, offline)]
        expect("a GitHub that cannot be asked (a failed call, an offline gh) is a non-observation that leaves the results "
               "as they were, and says so",
               all('not observed' in u and 'left as it was' in u for u in unread)
               and (dest / 'test-results-Linux-unit' / 'unit.trx').is_file(), str(unread))
        expect("bounce_run takes the newest run by id among exactly this pull request's, and none from a list with other "
               "branches or malformed items",
               bounce_run(runs, 547)['id'] == 800 and bounce_run(runs, 5)['id'] == 900 and bounce_run(runs, 9) is None
               and bounce_run([None, {}, 'x', {'head_branch': 'main'}], 547) is None, str(bounce_run(runs, 547)))
    print(f"=== queue_facts.py self-test: {'FAIL' if failures else 'PASS'} ({len(failures)} failure(s)) ===")
    return 1 if failures else 0


def main(argv: list[str] | None = None) -> int:
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
                 else 'BOUNCED' if pr.bounced else 'PUSHED SINCE ITS REMOVAL' if pr.removed and pr.pushed_since_removal
                 else 'NOT QUEUED')
        print(f'#{pr.number} {branch} @{pr.head[:12]}: {where} — {describe(pr)}')
    return 0


if __name__ == '__main__':
    ensure_utf8()
    sys.exit(main())
