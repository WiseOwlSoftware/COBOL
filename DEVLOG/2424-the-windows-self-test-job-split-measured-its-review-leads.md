## Entry 2424 — 2026-10-23 16:32 PDT — The Windows self-test job split, measured; its review leads and PB3584's over-broad sites fixed (PB3581, PB3584)

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

