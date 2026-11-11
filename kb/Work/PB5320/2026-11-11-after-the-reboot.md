## 2026-11-11: process creation after the reboot is back at 7 ms

The owner rebooted the box (boot 12:27:04 PST). Measured at 12:30 PST, uptime 3 minutes, no gate running: 15 launches
of `cmd.exe /c exit` through PowerShell `Start-Process -Wait -PassThru`, one at a time.

| when | uptime | median | min | max |
|---|---|---|---|---|
| 2026-11-11 09:02 PST (before the reboot) | 8 days | 2.0 s | | |
| 2026-11-11 12:30 PST (after the reboot) | 3 min | 7.4 ms | 7.2 ms | 25.6 ms |

The 09:02 figure is the earlier session's, from `findings.md` in the coordination scratch (`gate-perf`); the 12:30
figures were measured by this session. What changed between the two rows is the uptime, and with it whatever the
machine had accumulated since the 2026-11-03 boot: the hardware, the toolchain and the repository are the same.

What this means for the decision this note carries:
- The degradation is a property of the box's uptime, and a reboot clears it. It will return: it grew for eight days
  before it was measured.
- Stage 0's creation-time probe is therefore the instrument that matters: a gate that reads the figure at its start can
  say "this box is sick" in its first lines, where this week it took a night of stack sampling to learn the same thing.
- Stages 1 to 3 (a runner without a process per program) are not needed to make today's gates fast. They remain the
  cure for the dependence itself: about 8,000 process creations a Conformance run keeps every gate hostage to this figure.
  The decision on them is taken on the gate times measured after the reboot, not on the sick figures.
