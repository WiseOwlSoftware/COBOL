## Entry 2425 — 2026-10-23 16:48 PDT — The Windows self-test job split, measured; its review leads and PB3584's over-broad sites fixed (PB3581, PB3584); Where pending branches wait: review latency, not landing (PB3608, PB3609 filed; PB3581, PB3584 review leads fixed)

### The Windows self-test job split, measured; its review leads and PB3584's over-broad sites fixed (PB3581, PB3584)

PB3581 landed with pull request #445. Its merge group ran `windows-self-tests` in 4.0 minutes beside `Windows build +
tests`, which fell from 16.0 minutes (pr-442) to 6.5; the next merge group (pr-439) read 6.7 and 8.8. The NIST guard
(12.4 to 13.6 minutes) is the merge queue's longest job now, so every landing's queue wall fell by the 3 to 4 minutes
the Windows job ran past it. PB3581 records the table.

The branch review's two LOW leads, fixed here: two comments of the `audits` job still named `windows-build-test` as the
home of the Windows-only self-tests and of the Windows-checkout line-ending check, and DESIGN-test-build-ci.md §1.4
still opened "Ten jobs." over eleven.

PB3584's `**Sites:**` named two DIRECTORIES (`scripts/` and `tests/Cobol.Net.Tests.Unit/`), so the wave planner read
its file set as colliding with every unit-test file: the Mythos lane's `plan_wave.py --cluster PB3582` held it on
`TransactionControlTests.cs`, a file of wave 1157 group A. The sites now name the files the design creates,
`scripts/scratch_repo.py` and `tests/Cobol.Net.Tests.Unit/SelfTestFixtureDriftTests.cs`.

### Where pending branches wait: review latency, not landing (PB3608, PB3609 filed; PB3581, PB3584 review leads fixed)

The owner asked how to get pending branches to main efficiently. Measured at ~16:00 PDT: all three gate slots free, the
merge queue empty, and of the 20 branches the survey printed IN FLIGHT, 13 were already merged (PB3609: the survey's
180-minute age guard labels a merged branch IN FLIGHT; it should guard only deletion). About six were truly unlanded, and
the ones that were finished waited for a reviewer: the loop reviews only inside an `enqueue` unit, and units run one at a
time for an hour or more, while the landing lane starts a landing within its five-minute tick (PB3608: a review lane beside the
landing lane). Two PB3340 branches with no shared history were found the same way; the Mythos lane kept the loop's
(`worktree-wf_f6f89a51-99e-1`) and PB3340's note records it.

The branch review of `fix-self-test-wall-leads` (CLEAN) left two LOW leads, fixed here: PB3581's saving is 2.4 minutes a
landing as measured (16.0 against the guard's 13.6 on pr-442; the run wall 16.4 to 14.1), not the 3 to 4 minutes
estimated before landing, and pr-445's guard cell reads 13.7; PB3584's design section now names the two files its Sites
paragraph names.

