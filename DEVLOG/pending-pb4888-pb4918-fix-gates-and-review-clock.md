## PB4918 and PB4888: a review stall counts from the implementer's last act, and a finisher's gate is a fix gate

Two process mechanisms of the loop and the fleet, one wave (w20261913a). Nothing here changes a compiled program.

**PB4918, the false STALL.** `progress.py` started a DONE-and-unreviewed group's 15-minute review clock at its report's
mtime, but an implementer writes the report, then gates the tip it reports and fills the gate line in at the verdict, so
the report can be a whole gate older than the implementer's return (the wave starts a review only at the return). The
wave records no return in the coordination directory, so the fix reads the report's own worktree
(`progress.implementer_activity`): a gate holding its worktree's gate lock (`gate.live_gate`) counts the group as running,
under `RUNNING_STALL_MIN`, from its dispatch; otherwise the clock starts at the newest of the report's mtime, the
worktree's HEAD log and its STATUS.md. The self-test gained a group with a held lock and a group whose worktree was active
5 minutes ago behind a 40-minute-old report; both read as stalls before the change.

**PB4888, the two approved levers.** Wave 20261887's fix cycles were 86 % gate time (22 gates, 11 RED, 13.7 of 40.4 median
minutes a slot wait). Lever 1: `MANDATORY-PRACTICES.md` I12, the finisher brief and the rolling wave's review finisher and
gate finisher prompts (`FIX_GATE_LINE`) make a finisher run `dotnet test --filter` over what it touched before it launches
the gate; `check_practices.py` refuses a brief or prompt without it. Lever 2: `gate_slot.py` has a third ticket class,
`fix`, served after `landing` and before `implementer` in `GATE_CLASSES` order (`_service_key` is the index in the tuple,
`_TICKET` and `_ticket_name` read it, so a class added later is served at its place); a finisher sets
`COBOL_GATE_CLASS=fix`. The self-test's class-order arm covers all three classes, and the waiting message names the class
that goes first. `verdict.json` timings gain `slot_taken_s`. The cap stays 3. PB4400 (the dead `landing` class) now names
the narrowed deletion. Lever 5 (queue-empty minutes in the PROGRESS line) and the one-wave measurement are not done; PB4888
stays open for them.

Gate: tooling lane (audits and the self-tests of the touched scripts); see the report for the verdict line.
