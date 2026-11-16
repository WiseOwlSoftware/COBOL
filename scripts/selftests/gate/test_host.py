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
    found = Host(gate_mod.REPO).toolchain()
    expect("the real host's toolchain read answers (the NOT RUN cause or None, the versions it found) from "
           "toolchain.env without raising",
           found.cause in (None, "toolchain", "toolchain-unread") and found.matched == (found.cause is None)
           and isinstance(found.tools, str) and bool(found.tools), repr(found))


def case_01_real_host_names_its_lock_and_default_audits(expect):
    host = Host(gate_mod.REPO)
    expect("the real host names its gate lock inside the worktree's git directory and lists the audits a change that touches no "
           "tooling runs, less TOOLING_ONLY",
           host.lock_path().name == "cobol-gate.lock" and [n for n, _ in host.audit_commands(False)]
           and "SELF-TESTS" not in [n for n, _ in host.audit_commands(False)], "")


def case_02_real_host_captures_in_the_test_hosts_working_directory(expect):
    """kb/Work PB4333 R2-1: the capture compiles where the Conformance test host runs, the directory of its BUILT
    assembly (measured: `<project>/bin/Debug/net10.0`), never the project directory, which the fake host cannot see."""
    host = Host(gate_mod.REPO)
    try:
        where = host.test_output_dir("Conformance")
    except OSError as e:   # an unbuilt tree: the capture fails and nothing is carried, which the gate's cases hold
        expect("the real host names the Conformance assembly's output directory, or refuses an unbuilt tree", "expected one built" in str(e), str(e))
        return
    expect("the real host's capture directory is the one holding the built WiseOwl.Cobol.Tests.Conformance.dll, under bin/",
           (where / "WiseOwl.Cobol.Tests.Conformance.dll").is_file() and "bin" in where.parts
           and where != gate_mod.REPO / "tests" / "WiseOwl.Cobol.Tests.Conformance", str(where))


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(selftest_cases.run(sys.modules[__name__]))
