## Entry 2753 — 2026-10-29 17:19 PDT — Three docs/CONFORMANCE.md notes wait for the Mythos lane's A.1 chain (PB4047, PB4012, PB2103)

The Mythos lane's A.1 chain (cluster PB2911) edits `docs/CONFORMANCE.md` one link per wave: PB1369, then PB2556, then
PB1253. On 2026-10-29 the loop's groups collided with it on that file three times, PB3840 and PB3953 among them. The
planner holds a group only while other work on its files is in flight, so in the gap between two links nothing kept a
loop group off the file. The wave planner's dry run showed the next three it would plan there once PB3953 ends: PB4047,
PB4012 and PB2103.

Each of the three is now `blocked_by` the chain's three links, and its body says why. The register derives the hold from
the links' status, so it ends by itself when the third one lands; nobody has to clear it. With the hold, a dry run of the
planner no longer names them. The lasting fix is PB3738 (splitting CONFORMANCE.md) with PB4053 (the row-key partition),
which the Mythos lane runs after the chain.

