#!/usr/bin/env python3
"""live_tools.py — THE ONE LIST OF THE TREES WHOSE TRACKED SCRIPTS ARE LIVE TOOLS (kb/Work PB3176, branch review R2-1).

A live tool is a script the gates, the loop or a session run against this tree. Five readers need the same answer:
  scripts/self_tests.py    discovers every self-test under ROOTS and runs it in every gate and in CI;
  scripts/line_endings.py  its WRITERS arm holds every text-mode write under ROOTS to an explicit `newline=`;
  scripts/temp_names.py    holds every script under ROOTS to no fixed name in the machine-wide temp directory;
  scripts/scratch_repo.py  its audit holds every self-test under ROOTS to the one scratch-repository fixture (PB3584);
  scripts/standard_streams.py  holds every Python file under ROOTS to the one UTF-8 switch for its streams (PB3637).
Each kept its own copy until PB3176: when the conformance suite's harness (conformance-suite/harness/, whose generator is
a gate audit; kb/Work PB3174) joined the self-test runner and the WRITERS arm, the temp-name audit's copy stayed at
scripts/ and .claude/, so a fixed temp name committed in the harness passed every gate. A tree added here reaches all
of them at once.

Not live: `docs/**/evidence/**` and `adjudication/**` hold frozen records that never run against the tree, and
`tools/claude-skills` is a submodule with its own repository and its own checks.
"""
from __future__ import annotations

#: Each root ends in `/`, so it is both a git pathspec and a `str.startswith` prefix of a repository path.
ROOTS = ("scripts/", ".claude/", "conformance-suite/harness/")
