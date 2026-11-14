#!/usr/bin/env python3
"""The REAL host's reads, which the planted `FakeHost` replaces everywhere else (DESIGN-orchestrator-loop.md section 16.2, kb/Work
PB5433): a module split that drops an import its real host needs is invisible to every case that plants the host, and the first
gate run after it ends NOT RUN [driver] on a NameError. Each case asks one real read that touches the names a split moves."""
import sys

import selftest_cases
from standard_streams import ensure_utf8

gate_mod = selftest_cases.subject(__file__)   # scripts/gate.py
from gate_host import Host


def case_00_real_host_reads_toolchain(expect):
    matched, tools = Host(gate_mod.REPO).toolchain()
    expect("the real host's toolchain read answers (matched, the versions it found) from toolchain.env without raising",
           isinstance(matched, bool) and isinstance(tools, str) and bool(tools), repr((matched, tools)))


def case_01_real_host_names_its_lock_and_default_audits(expect):
    host = Host(gate_mod.REPO)
    expect("the real host names its gate lock inside the worktree's git directory and lists the audits a change that touches no "
           "tooling runs, less TOOLING_ONLY",
           host.lock_path().name == "cobol-gate.lock" and [n for n, _ in host.audit_commands(False)]
           and "SELF-TESTS" not in [n for n, _ in host.audit_commands(False)], "")


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
