#!/usr/bin/env bash
# push-main.sh — ⛔ THE ONLY WAY A COMMIT REACHES `main`. (Owner decision 2026-09-06, question 23; kb/Work/PB796.)
#
# `main` carries a REQUIRED status check — `ci-gate`, the terminal job of .github/workflows/build-and-test.yml —
# with `enforce_admins: true`. Every push to this repository is made with the owner's credentials, so an
# administrator exemption would be worth nothing; the rule binds the orchestrator's own pushes exactly as it binds
# a lander's. A bare `git push origin HEAD:main` of an unverified commit is therefore REFUSED BY THE SERVER, and
# this script is the flow that earns the verdict first:
#
#     1. push HEAD to the landing branch `ci/<short-sha>`   (a check run is attached to the COMMIT, not a branch,
#     2. find the run for that exact sha                     so the verdict earned there is the one the protection
#     3. `gh run watch <id> --exit-status`                   reads when the same sha reaches main)
#     4. GREEN  -> `git push origin HEAD:main` (fast-forward only), then delete the ci/ branch
#        RED    -> print the failing jobs and exit non-zero, WITHOUT touching main
#
# ⚙ WHY IT EXISTS. PB796: `git push origin HEAD:main` was the LAST step of both lander briefs, so nothing in the
# loop ever read the verdict the push triggered, and `main` stayed red for 29 hours across 16 consecutive
# completed runs while eleven landings each reported a green LOCAL gate. The local battery runs on ONE host
# (Windows, Debug); the workflow's `ubuntu-latest` jobs and its Release build are gated by CI and by nothing else.
# The brief-level fix ("wait for the run, report its conclusion") was prose in four places. This is the mechanism.
#
# ⭐ IT IS IDEMPOTENT AND SAFE TO RE-RUN. A full-matrix run is ~25–30 min, which is longer than an agent's command
# timeout — so a caller that is cut off simply RUNS IT AGAIN: a sha that already carries a green `ci-gate` skips
# straight to the main push, and a run already in flight for that sha is re-attached to, never duplicated. Run it
# in the background (the Bash tool's `run_in_background`) or re-run it; do not hand-roll the wait.
#
# Usage:  bash scripts/push-main.sh [--branch-prefix ci] [--no-delete]
#         PUSH_MAIN_TIMEOUT_MIN=45  — how long to wait for the landing run (default 45)
# Exit:   0 = the commit is on main and its CI was green · 1 = CI red or the push refused · 2 = usage/state error

set -uo pipefail

PREFIX=ci
DELETE_BRANCH=1
TIMEOUT_MIN="${PUSH_MAIN_TIMEOUT_MIN:-45}"
while [ $# -gt 0 ]; do
  case "$1" in
    --branch-prefix) PREFIX="${2:?--branch-prefix needs a value}"; shift 2 ;;
    --no-delete)     DELETE_BRANCH=0; shift ;;
    -h|--help)       sed -n '2,30p' "$0"; exit 0 ;;
    *) echo "push-main.sh: unknown argument '$1'" >&2; exit 2 ;;
  esac
done

cd "$(dirname "$0")/.." || exit 2
say() { printf '%s\n' "$*"; }
die() { printf '⛔ push-main: %s\n' "$*" >&2; exit 2; }

command -v gh  >/dev/null 2>&1 || die "the GitHub CLI (gh) is not on PATH — the landing verdict cannot be read"
gh auth status >/dev/null 2>&1 || die "gh is not authenticated (gh auth login)"

REPO="$(gh repo view --json nameWithOwner --jq .nameWithOwner 2>/dev/null)"
[ -n "$REPO" ] || die "could not resolve the repository from gh"
SHA="$(git rev-parse HEAD)"      || die "no HEAD"
SHORT="$(git rev-parse --short=12 HEAD)"
BRANCH="$PREFIX/$SHORT"

# ── The tree is not what lands; HEAD is. Say so loudly rather than silently leaving work behind. ──────────────
DIRTY="$(git status --porcelain -- . ':!.claude/settings.local.json' | grep -v ' STATUS.md$')"
if [ -n "$DIRTY" ]; then
  say "⚠ the working tree is DIRTY — these paths are NOT part of the commit being landed:"
  printf '%s\n' "$DIRTY" | sed 's/^/    /'
fi

git fetch --quiet origin '+refs/heads/main:refs/remotes/origin/main' \
  || die "git fetch origin main failed"
BASE="$(git rev-parse refs/remotes/origin/main)"
if [ "$BASE" = "$SHA" ]; then
  say "push-main: origin/main is already at $SHORT — nothing to land."
  exit 0
fi
# ⛔ FAST-FORWARD ONLY. Refused HERE, before a run is spent, and refused again by the server at the push.
if ! git merge-base --is-ancestor "$BASE" "$SHA"; then
  die "HEAD ($SHORT) is not a descendant of origin/main ($(git rev-parse --short=12 "$BASE")).
     Rebase first:  git fetch origin && git rebase origin/main
     A landing is a pure fast-forward; this script never force-pushes."
fi

green_ci_gate() {   # 1 = this sha already carries a successful `ci-gate` check run
  local n
  n="$(gh api "repos/$REPO/commits/$1/check-runs?check_name=ci-gate&per_page=100" \
         --jq '[.check_runs[] | select(.conclusion == "success")] | length' 2>/dev/null)"
  case "${n:-x}" in ''|*[!0-9]*) return 1 ;; esac
  [ "$n" -gt 0 ]
}

run_for_sha() {     # prints the newest run id for sha $1 on branch $2, or nothing
  gh run list --branch "$2" --limit 20 \
     --json databaseId,headSha,status,conclusion --jq \
     "[.[] | select(.headSha == \"$1\")] | first | .databaseId" 2>/dev/null | grep -E '^[0-9]+$'
}

report_red() {      # $1 = run id
  say ""
  say "⛔ CI IS RED ON $SHORT — main is UNTOUCHED. Failing jobs:"
  gh run view "$1" --json jobs --jq \
    '.jobs[] | select(.conclusion != null and .conclusion != "success" and .conclusion != "skipped")
     | "  JOB  \(.name) = \(.conclusion)", (.steps[]? | select(.conclusion == "failure") | "       step: \(.name)")' \
    2>/dev/null || say "  (could not enumerate jobs — gh run view $1)"
  say ""
  say "  attribute it:  gh run view $1 --log-failed"
}

# ── 1. Earn the verdict on the landing branch ────────────────────────────────────────────────────────────────
RUN_ID=""
if green_ci_gate "$SHA"; then
  say "push-main: $SHORT already carries a green ci-gate check — skipping straight to the main push."
else
  say "push-main: pushing $SHORT to $BRANCH for verification …"
  git push --quiet origin "HEAD:refs/heads/$BRANCH" || die "could not push $BRANCH"

  say "push-main: waiting for the run on $SHA (timeout ${TIMEOUT_MIN} min) …"
  for _ in $(seq 1 30); do                       # the run takes ~10–40 s to appear
    RUN_ID="$(run_for_sha "$SHA" "$BRANCH")"
    [ -n "$RUN_ID" ] && break
    sleep 5
  done
  # ⛔ NO RUN IS A FAILURE, NEVER A PASS. Since `paths-ignore` was removed every push to `ci/**` starts a run, so
  # "no run for my sha" no longer means "docs-only" — it means the workflow did not fire, and main would refuse
  # the push anyway for want of a check (feedback_verdict_evidence_invariant).
  [ -n "$RUN_ID" ] || die "no workflow run appeared for $SHORT on $BRANCH within 150 s — main NOT touched.
     Check: gh run list --branch $BRANCH  ·  gh workflow list"

  say "push-main: run $RUN_ID — https://github.com/$REPO/actions/runs/$RUN_ID"
  gh run watch "$RUN_ID" --exit-status --interval 20
  WATCH_RC=$?
  # ⛔ `gh run watch` exiting non-zero is not proof of a red — a dropped connection exits non-zero too. The
  # AUTHORITATIVE verdict is the run's own conclusion, so it is re-read here, and a run that is somehow still
  # in flight is polled to completion rather than guessed at.
  DEADLINE=$(( $(date +%s) + TIMEOUT_MIN * 60 ))
  while :; do
    STATUS="$(gh run view "$RUN_ID" --json status --jq .status 2>/dev/null)"
    [ "$STATUS" = "completed" ] && break
    if [ "$(date +%s)" -ge "$DEADLINE" ]; then
      die "run $RUN_ID is still '$STATUS' after ${TIMEOUT_MIN} min — main NOT touched. Re-run this script."
    fi
    sleep 20
  done
  CONCLUSION="$(gh run view "$RUN_ID" --json conclusion --jq .conclusion 2>/dev/null)"
  say "push-main: run $RUN_ID concluded '$CONCLUSION' (gh run watch rc=$WATCH_RC)"
  if [ "$CONCLUSION" != "success" ]; then
    report_red "$RUN_ID"
    say "  the landing branch $BRANCH is LEFT IN PLACE so the run can be re-run after the fix."
    exit 1
  fi
  # The run being green and the CHECK the protection reads being green are two statements; assert the second.
  green_ci_gate "$SHA" || { report_red "$RUN_ID"
    say "  (the run concluded success but no green 'ci-gate' check run is attached to $SHORT)"; exit 1; }
fi

# ── 2. Land ──────────────────────────────────────────────────────────────────────────────────────────────────
say "push-main: ci-gate is green on $SHORT — fast-forwarding main …"
if ! git push origin "HEAD:main"; then
  say "⛔ the push to main was REFUSED. Either origin/main moved (rebase and re-run) or the protection"
  say "   did not see the ci-gate check. main is unchanged."
  exit 1
fi
say "push-main: ✅ $SHORT is on main."

if [ "$DELETE_BRANCH" = 1 ]; then
  # Unconditional: a re-run that skipped verification still has last time's branch to clean up. The run and its
  # check runs are attached to the COMMIT, which is now reachable from main, so nothing is lost with the branch.
  git push --quiet origin --delete "$BRANCH" 2>/dev/null \
    && say "push-main: deleted the landing branch $BRANCH (the run and its check runs live on the commit)."
fi

# ── 3. Read the verdict of the run THIS push just started — the whole point of PB796. It is the
# already-verified short-circuit in the `changes` job, so it is `changes` + `ci-gate` and takes under a minute. ─
MAIN_RUN=""
for _ in $(seq 1 30); do
  MAIN_RUN="$(run_for_sha "$SHA" main)"
  [ -n "$MAIN_RUN" ] && break
  sleep 5
done
if [ -z "$MAIN_RUN" ]; then
  say "⚠ no run appeared on main for $SHORT within 150 s — check it by hand: gh run list --branch main"
  exit 0
fi
gh run watch "$MAIN_RUN" --exit-status --interval 15 >/dev/null 2>&1
MAIN_CONCLUSION="$(gh run view "$MAIN_RUN" --json conclusion --jq .conclusion 2>/dev/null)"
say "push-main: main run $MAIN_RUN = ${MAIN_CONCLUSION:-unknown}"
if [ "$MAIN_CONCLUSION" != "success" ]; then
  report_red "$MAIN_RUN"
  say "  ⛔ THE COMMIT IS ON MAIN AND ITS MAIN-BRANCH RUN IS NOT GREEN — report this as a BLOCKING finding."
  exit 1
fi
say "push-main: ✅ landed $SHORT — ci/$SHORT run ${RUN_ID:-<reused>}, main run $MAIN_RUN, both green."
