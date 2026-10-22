## Entry 2348 — 2026-10-22 10:34 PDT — The second fuzz night's experiment record, and a fourth defect (PB3517: SIGNED/UNSIGNED accepted on every USAGE)

**What.** `docs/rearchitecture/evidence/fleet-optimization/2026-10-22-differential-fuzz-second-night.md` records the
second overnight differential fuzz run end to end: the run (4,931 programs, 314 survivor signatures), the re-judge of
every output row on a fresh build of main (144 of 392 now agree after PB3505), the delegated triage (three Opus
adjudicator slices and a Sonnet clerk, one checked verdict per signature), the crash-class rows read by the session, and
the readings worth keeping for the next triage (the DIVIDE zeros were an uninitialized receiver; an EXTERNAL item's VALUE
takes effect on INITIALIZE; the float-to-fixed last digit is documented truncation). The GnuCOBOL-side refusal rows
(THEY_REJECT 154, THEY_FAIL_RUN 19) are now classified too: 111 programs are 2014 features GnuCOBOL 3.2 lacks or its own
crashes, 25 are WiseOwl right where GnuCOBOL is stricter than the standard, and one is a WiseOwl defect.

**PB3517.** `SIGNED` and `UNSIGNED` are accepted after any USAGE: `COMP UNSIGNED`, `DISPLAY SIGNED` and
`PACKED-DECIMAL UNSIGNED` compile clean at `--std 2014` and the word is ignored (a signed PICTURE keeps its sign). The
general format of §13.18.60.2 places the choice only inside the BINARY-CHAR, BINARY-SHORT, BINARY-LONG and BINARY-DOUBLE
alternatives; GnuCOBOL refuses the fuzz program. The grammar's `binarySign` hangs off the whole usage clause and the data
binder reads it for every usage. An under-rejection, filed for the fix lane.

**Cost shape of the delegation, recorded for the next run.** Every slice's first agent reached the 160-turn cap; the
replacements finished from the checkpoint files in under half the tokens. Next time: about 25 signatures per agent, the
report returned as text (the subagent harness refuses a report file), and one program per signature unless it is a
candidate defect.

