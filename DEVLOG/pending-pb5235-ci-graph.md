## PB5235, first half: the merge group reads the owner's passes from a ref and stops re-running them on Windows

PB5207 stage 1 keyed each test target by its declared inputs, and the gate records a pass in a store under the git common
directory. Only this host's worktrees can read that store. So every merge group re-ran the whole population on both
operating systems, including the Windows population the local gate had just passed. The owner approved, 2026-11-10, that
CI may accept a pass recorded on his machine. This change carries the passes to CI and lets the merge group act on them.

**The ref.** `refs/cobol-build-store/passes` names one parentless commit whose tree is the store's own layout,
`<target>/<key>.json`. A record holds only the small facts: target, key, facets, counts, and the head, tree and run it
came from. `python scripts/buildgraph.py publish` sends the union of the ref's records and this host's store, prunes
records older than 21 days, and pushes with a lease on the commit it read, so two publishers never drop each other's
records. CI reads it in the merge group alone, with `--no-filter`, because the `changes` checkout is a blob:none partial
clone. On a fixture, without the flag the record blobs were missing.

**The plan.** A new step of the `changes` job, `scripts/ci_plan.py`, computes every target's key at the merged tree on
each leg with the gate's own `buildgraph.keys`. On a leg that accepts records, it takes a pass at that exact key whose
facets are the leg's: its platform, its configuration, and the toolchain `toolchain.env` names for its OS. Its log names
each key and either RUN or the record accepted. The shard jobs build and test in the plan's configuration, gate each test
step on the plan's set, and skip entirely when the set is empty. ci-gate checks the population of exactly the assemblies
each leg ran.

**The finding that reshaped it.** As configured, no local pass could ever match a CI key. Linux ran Debug, Windows ran
Release, and the local gate runs Windows Debug. The legs now swap. Linux runs Release and accepts no record, so it remains
the merge group's own proof of Linux, Release, the inert watch and INV-1-strong. Windows runs Debug, the local gate's
configuration. Every configuration builds with TreatWarningsAsErrors, so no analyzer check is lost.
`ci_plan.ci_wiring` holds the workflow to the plan for every leg and every declared target. Against main's workflow it
reports 27 findings.

**Before.** The 17 green merge groups to 2026-11-11 05:22Z that ran the matrix averaged 13.0 minutes of wall time and 114.2
job-minutes. Of those, 55.3 job-minutes (48%) were the seven Windows shards re-running a population the local Windows gate
had already passed.

**After, replayed.** The plan was run against this host's 12 records at main's merges #1482 to #1492, the first merges
whose gates recorded passes. 15 of their 33 Windows target runs had a pass at the merged tree's key. For #1489 all three
did, so its whole Windows matrix, about 55 job-minutes, would have been skipped. Unit is the target usually left to run:
its key is every population input. PB5230's split of repository drift out of Unit raises that share. The measured after is
the first merge groups after this landing.

**The landing publishes.** `enqueue.py`'s `queue` pushes the branch, then publishes this host's passes, then opens and
queues the pull request. A publish that fails is exit 16, STORE NOT PUBLISHED, naming its cause, with nothing opened, and
the landing lane retries it once, as it does exit 9. A warning would have let a landing skip its publish silently.

**Review round 1, folded in.** The plan's reading of the ref is loud: an absent ref, an unreachable remote, a ref naming a
blob or a tree, a commit holding no pass, and each entry that is no pass (not JSON, filed under another key, not a record
path) are each a named warning in the job log, and every target then runs. A publisher that loses its lease mid-publish
re-reads and re-sends (now a self-test arm). The shard listings write the plan's list as one line with no line end, since
on Windows both jq and Python's print end lines CRLF.

**Caught before the merge group.** The first draft read the plan's JSON list in the Windows listing step with `jq -r`.
On this host, toolchain.env's jq 1.8.2 ends each line CRLF on Windows, and bash keeps the CR, so `Unit` would have become
`Unit\r` and the listing would have named no project. Both shard listings now print it from Python as one line with no line end.

**Also.** The BUILD TARGETS audit (`buildgraph.py check`) now runs in the merge group's audits job, as it already did in the
local gate. The session probe's first line is the latest scheduled nightly's verdict. Its first run showed the
2026-11-10 nightly red in `latest` and in the NIST `Guard`.

**Not in this change** (PB5235 stays open): the per-target dynamic input proof on the Linux shards, the nightly's jobs as
graph targets, and CI recording its own Linux passes.
