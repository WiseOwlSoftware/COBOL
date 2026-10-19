## Entry 2244 — 2026-10-19 00:39 PDT — Skills pin moved to brent-tools 1.22.0 (the finished-work-lands-at-once release)

The public skills repo (BrentRector/claude-skills) is released as v1.22.0 (`0b6e663`), the release and its commit
approved by the owner today. It carries:
- finished work lands at once: no predecessor hold, successors merge the reviewed head, re-reviews read only the
  diff, and severity thresholds decide what blocks;
- one source of truth for unanswered findings, and a findings-file integrity rule proven by a byte-offset property
  test;
- breaking: the `rolling-wave.js` result field is confirmed renamed `blocking`.

`tools/claude-skills` moves to tag `v1.22.0`. The five overlays that name the pin (architecture-review, gate,
review, spec-lookup, workstream) say 1.22.0, and kb/Work PB1699 records the version and the pin. The overlay's own
PB3381 implementation of these rules landed today.

