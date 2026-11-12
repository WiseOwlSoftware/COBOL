**Decision: the transport of the store's records to CI, and the merge group's plan (2026-11-10).** The design is
`docs/rearchitecture/DESIGN-test-build-ci.md` §3.14.14. The decisions, each with its reason:

1. **One ref, `refs/cobol-build-store/passes`, naming one parentless commit whose tree is the store's own layout,
   `<target>/<key>.json`.** Records are content-addressed, so CI finds a record by the key it computes and by nothing
   else. One ref per branch would make CI guess which branch's records apply to a merge group of several pull requests.
   One ref per record would grow without bound in every fetch's ref advertisement. A ref outside `refs/heads/` and
   `refs/tags/` falls under no branch rule, ruleset or checkout.
2. **The ref carries the small record, never the evidence.** `buildgraph.PUBLISHED_FIELDS` keeps `target`, `key`,
   `result`, `assembly`, `facets`, `counts`, a generated target's `outputs` and `files` (PB5231's records), `head`,
   `tree`, `run` and `when`. The host's worktree and run-directory paths stay on the host.
3. **The writer is the landing, when it pushes the branch.** `buildgraph.publish` reads the ref, takes the union with this
   host's store, prunes records older than 21 days (`PRUNE_DAYS`), writes a new parentless commit, and pushes it with
   `--force-with-lease` on the commit it read. On a lost lease it re-reads and re-sends (three attempts). The union means
   two landings never drop each other's records. Parentless means the ref's history is one commit deep, so pruning frees
   the old blobs. `scripts/orchestrator/enqueue.py`'s `queue` step 4 calls it (`publish_passes`), after
   `git push --force origin <sha>:refs/heads/<branch>` and before it lists, opens or queues the pull request (released
   to this lane by Mythos in review round 1). A failed publish is exit 16, STORE NOT PUBLISHED, naming its cause, with
   nothing opened; the landing lane retries it once, as it does exit 9 (`land_lane.classify`). A failure that only
   warned would let a landing silently skip its publish, and a merge group with no record runs every target.
4. **The reader is the `changes` job's plan step, `scripts/ci_plan.py`, in a merge group only.** It is a step of the
   first job, not a job of its own: a separate job would add a runner start and a toolchain setup (about a minute) to the
   critical path, and the step needs the same full-history checkout `changes` already has. It fetches with
   `--no-filter` because the checkout is a `blob:none` partial clone. Measured on a fixture: without the flag, the
   record blobs are missing after the fetch. A dispatch run or an unverified push to main reads no record and runs
   everything, keeping the full matrix on demand that `workflow_dispatch` promises. It lives outside `buildgraph.py`
   because PB5231 owns that file's graph. ci_plan calls `buildgraph.keys` and `buildgraph.load_targets` and reads
   records only through `buildgraph.Published.lookup`, so after PB5231 the Conformance key's dependency on the
   generator's output resolves through `buildgraph.stored_outputs(<the Published records>)`. That needs the one-line
   call-site change named in the report.
5. **The trust model.** A record is accepted only when all four hold:
   - it sits at `<target>/<key>.json` for the key CI computed itself at the merged tree;
   - `is_pass` holds (its body names that target, that key and `result: pass`, the one rule `Store.lookup` applies too);
   - its facets equal the leg's, whose toolchain is `toolchain.summary(<toolchain.env at the merged tree>, "gate", <the
     leg's OS>)`, the string the gate on that OS records and refuses to run without;
   - the leg accepts records.

   Writers of the ref are writers of the repository, the same people who can push a branch. The owner approved accepting
   passes recorded on his machine (2026-11-10).
6. **The legs swap configurations: Linux runs Release and accepts no record; Windows runs Debug and accepts records.**
   Before, Linux ran Debug and Windows ran Release, and the local gate runs Windows Debug, so no local pass could ever
   match a CI key. After the swap, both configurations still run in every merge group whose matrix runs, Release on
   Linux, and the owner's pass stands for Windows Debug. `TreatWarningsAsErrors` holds in every configuration
   (`Directory.Build.props`), so no analyzer check is lost. Two tests depend on the configuration and move legs:
   `CompiledProgramCacheDriftTests` runs only in Debug (Linux CI before; now Windows CI and the local gate), and
   `ArchitectureEdgesDriftTests`'s growth check reads an optimized image (Windows CI before; now Linux CI).
7. **The workflow is held to the plan, mechanically.** `ci_plan.ci_wiring` (an arm of ci_plan's self-test) requires, for
   every leg of `CI_LEGS` and every target of `scripts/build_targets.json`:
   - a `dotnet test` step of the target's project, gated on its assembly being in the plan's set, in the plan's
     configuration;
   - the job skipped when the set is empty;
   - the populations listed from the set;
   - ci-gate's population check reading the set.

   PB5230's new repository-drift target is therefore red in that self-test until each leg has its step. That is the
   intent: a new target is never silently run on one leg only.

**Measured before (this lane, `gh run list --event merge_group`, the 31 merge-group runs to 2026-11-11 05:22Z).** The 17
green runs that ran the matrix: mean 13.0 minutes of wall time and 114.2 job-minutes. Of those job-minutes, 55.3 (48%)
were the seven Windows shards re-running a population the local Windows gate had passed; 37.2 were the five Linux shards.
The 14 runs that skipped the matrix (the inert lane) averaged 5.6 minutes of wall time.

**The plan replayed against this host's store** (12 records, written since stage 1 began recording, at main's
first-parent merges #1482 to #1492): 15 of the 33 Windows target runs had a pass at the merged tree's key, and one merge
(#1489, 493b6706d7) had all three, so its Windows matrix would have been skipped entirely. Unit is the target usually not
covered. Its key is every population input, so almost any merge of main changes it. PB5230's split of repository drift
out of Unit is what raises that share.
