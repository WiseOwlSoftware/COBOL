## Entry 2825 — 2026-10-30 18:50 PDT — PB4182 filed: the runner drift test re-judges the whole conformance suite twice more

ConformanceSuiteRunnerDriftTests proves that run.py and the in-process runner agree by running every suite test through run.py and then through ConformanceSuiteRunner.Judge again, in one serial Fact, although the 32 partition classes already judged every test in the same run. PB4150 measured it at 50-205 s, often the Conformance leg's last case. kb/Work PB4182 replaces the brute force with a verdict fixture (one case per verdict path), deletes the repeated in-process pass, and moves any whole-suite run.py check to CI.

