## PB4181 filed: measure gate throughput at caps above 3

The owner asked why gates wait for a slot when the goal is throughput. The cap of 3 (PB2514) was decided on a per-gate slowdown measurement taken before server GC; a PB4150 Conformance run used about 3 of 32 cores on average. kb/Work PB4181 measures gates per hour at caps 3 to 8 once PB4150 returns (the owner keeps the cap at 3 until then so its timings stay clean) and brings the owner the numbers.
