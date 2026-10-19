## Entry 2268 — 2026-10-19 14:40 PDT — PB3424 review leads filed: a dynamic national member gives a variable-length group two widths

The w1130a finisher report for PB3424 (PR #300) left two things only in prose: the A.1 item 57 width divergence (a group holding a dynamic-length national item displays `xy06`, 4 positions, while BYTE-LENGTH is 6) and, as review lead R3-1, the CONVERT ANY abort in the Tier-C island for the same group. I re-ran `NV.cob` on the branch's built compiler under `--permissive` and both reproduce. They share one root (the group's current-extent image composes a dynamic national leaf one character a position while the storage is UTF-16BE), so one note, PB3441, holds both; PB3442 went unused. R1-2 was not filed, since the report answers it as fixed. No code changed.

The enqueue unit's ledger render left 56 new points in the main checkout's docs/rearchitecture/evidence/ledger-trend.json; this branch carries them (kb/Work PB2426, recurrence line added).

