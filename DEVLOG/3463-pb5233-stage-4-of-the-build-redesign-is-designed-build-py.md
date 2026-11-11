## Entry 3463 — 2026-11-10 22:04 PST — PB5233: stage 4 of the build redesign is designed: build.py check / why / land

Stage 4 of the build redesign (kb/Work PB5207) is designed and filed as PB5233. One command, `build.py` with the verbs `check`, `why` and `land`, replaces `build-local`, the orchestration inside `gate.py` and `enqueue.py land`. The `land` verb checks the merged tree and owns the pull request until it merges. The work lands as two landings: 4a replaces the gate and 4b replaces the landing, and each deletes what it replaces. It is built after stages 2 and 3 land, which is why PB5233 is blocked by PB5230, PB5231 and PB5232. The design and the controller's decisions on it are sidecar files beside the note.

