## PB5206: a killed tree's unreaped zombie no longer reads as alive

`ProcessJob.alive()` asked `killpg(pgid, 0)` on POSIX, which succeeds while a zombie is still in the group. A killed orphan
stays a zombie until whatever adopted it reaps it, and GitHub's runner reaps late, so process_job's self-test went RED in
#1475's merge group although the kill had worked. On Linux `alive()` now reads the group from /proc and asks
`detached.pid_alive`, the one liveness probe, about each member. The self-test makes itself a subreaper so the zombie case
runs on every host; the old body fails it every time in WSL.
