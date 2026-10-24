## CI: an artifact upload or download no longer decides a verdict; both go through one retried action (kb/Work PB3140)

**Why.** Merge-group run 38502926616 (PR #39, 2026-10-14) passed 5338 of 5338 tests in a Windows conformance shard,
then its `actions/upload-artifact` step failed on the artifact service's `ETIMEDOUT`. The step ran `if: always()` with
nothing else, so its failure was the job's: `ci-gate` failed and the merge queue removed a pull request whose every
test had passed. `always()` makes an upload run after a red test; it never made the upload's own failure harmless.

**Re-probe.** On the base (24e337272) `build-and-test.yml` had five bare upload steps of that shape, no
`continue-on-error` and no retry anywhere, and one bare download (the population guard's) whose transient failure
failed `ci-gate` the same way.

**Fix.** One rule, in two composite actions. `.github/actions/upload-results` tries the upload three times (30 s, then
90 s apart, each retry `overwrite: true`), every attempt tolerated under `always()`, and warns when all three fail: a
job's verdict is its tests'. `.github/actions/download-results` tries three times into an emptied destination and only
the third failure fails the step, because the population guard needs the shards' trx and listings. A shard whose upload
was lost is absent from the union the guard checks, so the guard fails there, and its error line now says so. Every
caller keeps `if: ${{ always() }}`; the retention and `if-no-files-found` settings now live once, in the action.

**Sibling sweep.** `actions/cache` already only warns; checkout and setup-dotnet retry inside themselves. The one
network fetch a job makes itself, `scripts/fetch-gnucobol-tests.ps1` on a cache miss, had a single `Invoke-WebRequest`:
it now makes three attempts (`-MaximumRetryCount` retries only HTTP error statuses, not a connection timeout; probed
against an unresolvable host, three attempts and the failure line).

**Drift test.** `CiArtifactDriftTests` (Unit, tree sweep): no workflow names the raw upload or download action, every
upload step uses the action under `always()`, and both actions keep their retry shape; a self-test feeds each audit
every forbidden shape, and on the base's workflow the audit is red. `CiHarnessLogDriftTests` and
`FilterPopulationGuardDriftTests` read the action paths from its constants. DESIGN-test-build-ci.md §1.4 states the rule.

**Self-review.** GitHub Actions cannot run locally (no `act` or `actionlint` on this host), so the composite actions are
checked by PyYAML parsing, GitHub's documented composite keys (`continue-on-error` and `if` on `runs.steps`), the
upload action's documented `overwrite` input, and the drift test. Each attempt's condition names `always()`, so a red
job's status cannot skip the upload inside the composite action. The artifact name reaches the shell steps through
`env`, never through `${{ }}` in the script.
