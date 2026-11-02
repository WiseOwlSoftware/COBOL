## Entry 2951 — 2026-11-01 09:49 PST — PB4395 filed: the no-build tooling lane of the pipeline review, and why 29 C# test classes stood in its way

Mythos asked the operator to check step 11's CLAUDE.md "Testing" text (wave 20261808 group A) against PIPELINE-REVIEW.md
§13, which the owner approved as written. One clause differed: §13 says a tooling-only change runs the audits and the
self-tests whose recorded inputs it touches; the branch says it runs what a code change runs, plus those self-tests. The
branch describes the code truly. Step 3 (PB4326) left the no-build lane to step 5, and step 5's `gate.py` built only the
inert and full lanes, so a tooling-only change pays the build and the whole Windows population, about 5 minutes. From
2026-10-25 to 2026-11-01 that was 45 of the 244 merges that changed more than docs and notes.

The owner chose to build the approved lane, then asked why 43 C# test files read `scripts/`. Measured at 55d93dbb4: 92 C#
files under `tests/` name `scripts/`, 41 of them only in a comment. About 17 classes (182 cases, 112 s summed per Unit
run) check what the compiler's generators and spec extractors produce, and three scripts run inside the build itself.
Those scripts are compiler inputs. About 29 classes (156 cases, 226 s summed) test the pipeline's own Python, shell and
workflow tooling from the compiler's Unit assembly. They date from before `self_tests.py` (PB2563), when the Unit
population was the one run every gate and CI was sure to make. They cost every compiler gate 156 unneeded cases, and they
are why a tooling change could not be checked without a build.

PB4395 (cluster PB4323, after steps 8 and 11) specifies the change. Move the pipeline classes into the scripts'
self-tests and delete them from C#. Keep the generator classes in C#, with their scripts listed once as code paths and
held to that list by a drift arm. Then build the tooling lane (audits plus self-tests, no slot, no build) and restore §13's
clause verbatim.

