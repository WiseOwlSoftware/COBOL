## PB3314: a lock directory in the middle of its rmdir refuses creation, and Publish now paces its retries by time

After PB3313, one waiting lock-file region hold in 248,000 stressed Linux rounds still answered `Unavailable`. The note
guessed a tester reaping the pending file. The measurement said otherwise. Re-running the note's harness with
`Publish` instrumented and a first-chance handler that lists the lock directory recurred it twice in about 7,000,000
rounds (16 processes x 100,000 rounds beside 24 niced CPU burners, WSL, 32 cores): the eight `FileNotFoundException`s
are `HostFile.OpenLockHolder`'s `CreateNew`, not `ProbeLockFile`'s (that arm logged nothing in 3,200,000 rounds), and at
each the lock directory existed and listed empty. `SidecarRegionLocks.Look` lists only `r<offset>-*`, so it never
touches a pending file; the guess is refuted.

The mechanism is the kernel's. `vfs_rmdir` marks the inode dead and unhashes the name only afterwards, so while the
remover is preempted between the two steps the name still answers `stat` and `GetUnixFileMode`, .NET reads the dead
directory's `ENOENT` as the end of the directory (an empty listing), and every creation inside it answers `ENOENT`.
A micro-experiment (one thread making and removing a directory, one statting it and creating inside it; 8 processes
beside 24 burners) measured the window: thousands of episodes per process, up to 17 ms and 240 consecutive refusals.
`Publish`'s eight attempts were separated by `Thread.Yield()`, microseconds, so all eight fell inside one window, it
returned null, and `SidecarRegionLocks.Hold` answered `Unavailable` with the store mutex untaken.

The fix is the pacing, not a bigger count. `SidecarLockFiles.RetryPauseCeiling` is the schedule: the first retry is
immediate, every later one sleeps a jittered span up to twice the longest before it, to 128 ms, over 12 attempts (about
0.6 s at the ceiling, spent only when the race does not end). The sibling arm: `HostFile.CreateLockDirectory` read a name
that stats as a directory; with pacing a regular file at the lock directory name would have retried through the whole
schedule, so it now probes `FileAttributes.Directory` and a file is answered at once as the host refusing the lock
directory.

Tests (`RegionLocksDriftTests`): the schedule is asked directly (red with the old immediate tries; a window of one
publication in 500,000 is not one a test can wait for); two repetition witnesses hold the contract beside a sibling that
closes and reopens the directory on an oversubscribed host and beside a tester that reaps pending files without pause;
and a file at the lock directory name is `Unavailable` for the waiting and the non-waiting hold. The oversubscribed
witness does not discriminate the old code from the new (0 of 24 runs red before),; a version with a sibling that
never rests failed after the fix too, because there every attempt loses independently of any pause (no pause helps a
race that is not persistent). The design rule is in `DESIGN-runtime-library.md` beside PB3109's and PB3313's.

Review round 1 (R1-1): nothing called the schedule, so deleting Publish's pause left every test green. Publish now reports each pause through SidecarLockFiles.PauseObserver (an AsyncLocal, the FileRegistry.PauseObserver precedent) instead of sleeping when a test watches, and two tests drive Publish with a naming step that raises the lost-race exception: N lost races pause before retries 1..N and then publish; a race that never ends pauses before every retry (1..11) and answers null with no pending file left. With the pause deleted all four of these cases go red.
