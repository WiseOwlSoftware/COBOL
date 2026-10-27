## PB3537 and PB3561: the fuzz harness cuts a reply that loops, and a pause for a game loses no program

Two tooling defects of one root, the model's side of `scripts/differential_fuzz.py`, fixed together in a new module,
`scripts/fuzz_ollama.py` (the streaming chat to Ollama, the watch for a looping reply, the PAUSE file), and a tracked
wrapper, `scripts/fuzz_night.ps1`. Neither touches the compiler; both decide how many programs a night yields.

**PB3537, a reply that loops was paid for in full.** The third night's nemotron lost 19 of its first 90 replies to the
6,000-token cap, each a list of data items differing only by a counter (`fz00088`: WS-ARG1 … WS-ARG309, 10,540 characters).
Re-probed: `ollama_chat` still sent `stream: false`, so nothing could cut a reply before it ended. It streams now, and an
`EnumerationWatch` ends the reply when the last 150 non-blank lines hold at most 3 distinct forms (a form is a line with its
digit runs replaced by one mark); closing the connection makes Ollama stop. The threshold was measured, not chosen: over the
44,737 distinct replies five runs left on disk, 309 never completed, and a 150-line window flags 77 of them and none of the
44,428 complete programs; a 100-line window flagged 35 complete programs and a 120-line one, one. The class run over the same
files agrees (0 and 77), reads 53 % of those 77 replies before cutting, and cuts `fz00088` after 5,061 of its 10,540
characters. A cut reply never yields a program (the front of one the model did not finish may still hold a PROCEDURE
DIVISION), the model is asked for a SHORTER program, and a program that ends so is `NO_PROGRAM` with the reason "degenerate
enumeration". Ollama's own repeat-limit abort is the same shape and now records its message as the reply instead of an empty
file. `degenerate_replies` on the row, a line of the report and a new bake-off column count them per model (the bake-off
score became a NamedTuple, the nine-element positional tuple it had grown into). `--model-option MODEL:repeat_penalty=1.1`
and `repeat_last_n` set Ollama's repetition options for one model.

**PB3561, a pause lost the programs in flight.** The wrapper suspended the harness and ended Ollama's runner; the four
generations in flight failed with HTTP 500 and the harness recorded each as a terminal `RUNNER_ERROR` (three lost at 22:05
on 2026-10-22). The protocol is now a file, `<run dir>/PAUSE`: the wrapper writes it first, then ends the runner (after a
5-second grace), rewrites it on every poll and removes it after 120 quiet seconds; a worker waits for it before every
generation, and a generation that fails while it exists is asked again after it, as often as pauses end it, while a
runner failure outside a pause is asked again once. Only a failure outside a pause that is no runner failure, or a second
runner failure, is the row's `RUNNER_ERROR`, and it names the generation's own re-asks. The wrapper's rewriting is a lease
(its PAUSE starts `lease:`): a PAUSE not rewritten for 300 seconds belongs to a dead wrapper and is ignored with a message, so a
killed wrapper cannot hang the night; a PAUSE made by hand has no mark and holds until removed (checked end to end: the wrapper killed mid-pause). `pauses.jsonl` and the report count
the pauses, the minutes and the generations asked again, so the morning reads a pause as a pause. The wrapper left session
scratch for `scripts/`: its pure logic is `scripts/fuzz_night_lib.ps1`, with the self-test `scripts/test_fuzz_night.ps1`.

**What running it found that the self-tests did not.** The real wrapper, started against the real Ollama with a low game
threshold, detected Task Manager as a game, wrote PAUSE and ended the runner, and then died on its first poll: a lib
function's parameter `$Log` shadowed the wrapper's own `$Log` (PowerShell scopes dynamically), and the pure functions the
self-test drove never met the wrapper's variables. The parameter is `$Say`; the self-test now holds the lib to reading only
its parameters and locals, and holds every call the wrapper makes to a parameter its lib function declares. The wrapper also
no longer dies when it has no console, and it says when the GPU counters return nothing (a watcher that sees nothing never
sees a game) or the fast-forward to origin/main fails. An end-to-end run against the real model (4 programs, 2 jobs, the
runner ended mid-generation) lost nothing: the failed generation was asked again after the pause and every program got a
verdict. One thing was not explained: in the first such run an asked-again generation came back as Ollama's "prediction
aborted" with an empty reply; two further runs did not reproduce it and the message was not kept. A message that also names
the runner's death is now a runner failure (never a verdict on the model), and an aborted reply's message is recorded.

**Witnesses.** `fuzz_ollama.py --self-test` runs a stub Ollama over a local socket (chunked NDJSON, like the real server): an
endless 200,000-line enumeration is cut after about 150 lines and the stub sees the client leave; mid-reply errors, a stream cut
before its last chunk, HTTP 500 and 400, the `think` retry and the abort messages are each answered; the watch is held to a
brute-force reference whole and in pieces; `PauseGate` and `lossless` run on an injected clock. `differential_fuzz.py
--self-test` drives `Fuzz._one` through degenerate replies, a pause, a runner failure with no pause and a timeout. The
checks that carry the two fixes (the cut, the threshold, the retry rule, the lease, the pause order, the lib guard) were each seen to
fail under a planted mutation. Rows closed: none (`closes_rows: []`, both notes are harness defects with no
inventory row). The morning's launcher still calls the scratch wrapper; it needs its one line changed to
`scripts/fuzz_night.ps1`.

**Gate.** The first gate (head `e10332bbc`) stopped at the SELF-TESTS audit before any leg ran: `scripts/temp_names.py` found
the wrapper's default log, a fixed name in the machine-wide temp directory; the log now lives in the run directory
(`wrapper.log`). The second gate, run `000002-20261027T002209Z` at `f457da1a0`: `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched
gating, PB2515): GREEN — … Conformance 7,426/18,924 · Unit 37,268/38,399 · Characterization 36/36 cases ran (skipped 0) in 1
of 2 leg(s) · Linux half (WSL clone of HEAD) GREEN …`.

**Branch review round 1.** One blocking finding, two leads, all fixed in the files already changed. R1-1: `lossless` spent its
single retry on the first pause, so a generation a second pause killed (the owner alt-tabs, the retries start, the game is
heavy again) became a terminal `RUNNER_ERROR`, the loss PB3561 exists to prevent. A pause now costs no budget (every ask waits
for the PAUSE first, so it is no busy loop) and the runner-restart retry is its own single allowance; checks seen to fail on the
old rule cover two pauses, a pause then a runner failure, and a pause then two runner failures. R1-2: the `RUNNER_ERROR` reason
named the row's first re-ask even when the failing generation was a later repair round's; it names that generation's own. R1-3:
the printed instruction to create PAUSE by hand promised a pause that ended after five minutes; only the wrapper's PAUSE (first
line `lease:`, spelled once in each language and held equal by `test_fuzz_night.ps1`) expires.
