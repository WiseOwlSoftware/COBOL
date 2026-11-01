## Entry 2909 — 2026-10-31 20:51 PDT — kb/Work: file PB4348 and PB4349 (PB4324's review leads) and PB4269's review residue into PB4328

The branch review of PB4324 (pipeline step 1, review w20261792b-review-r1) came back clean with two non-blocking leads, and both are now notes. PB4348 records that the `package-cache` job in `build-and-test.yml` saves the NuGet cache under its exact key even when the `continue-on-error` restore before it failed, so a partial package set can become main's entry until a csproj changes. PB4349 records that the `CiBoundsDriftTests` summary cites DESIGN-test-build-ci.md section 3.17 while the paragraph it pins lives in section 1.4.

The branch review of PB4269 (lane-review-tooling-pb4269-whole-tier-r1-34) left three low leads in `scripts/gate_plan.py` and `scripts/run_gate_legs.py`. PB4328 deletes the first and replaces the second with `scripts/gate.py`, so the leads closed with that step and got no note of their own. They went into one paragraph at the end of PB4328's body, each with what gate.py must not carry over. PB4269's note is landed and was not touched.

