## Entry 2746 — 2026-10-29 15:57 PDT — Register: file PB4066, the Linux gate's tripwire reads the worktree's own commit as an escape

The enqueue unit filed PB4066 after a clerk's notes-only gate (`notes-leads-1029f`, run 000001-20261029T221136Z) came back RED on
its Linux half. Every leg had passed. The `repository-written` tripwire in `scripts/linux-gate.sh` (PB1719's guard against a test
writing to the real repository) saw the worktree's `HEAD` move from `c21a207cd` to its child `12214f14f`, a commit the clerk made on
Windows while the Linux self-tests ran, because the enqueue unit sent it one more lead mid-gate. The tripwire then reported an escape
from the clone that never happened. The note records the sites, the witness and a direction: name the case where a descendant
commit moved the gated tree as a superseded head, not as RED. No code changes here.

