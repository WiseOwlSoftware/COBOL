#!/usr/bin/env python3
"""kept_liveness_model.py — THE EXECUTABLE MODEL of a transaction's LIVENESS BYTE across the give-back of the handle that
holds it (docs/rearchitecture/DESIGN-runtime-library.md 2.2.1.8, 2.2.1.10; kb/Work PB3549).

    python scripts/models/kept_liveness_model.py --self-test     # every interleaving of every row, then the watchdog
    python scripts/models/kept_liveness_model.py --rows          # run and print the rows
    python scripts/models/kept_liveness_model.py --rows --mutation dispose-leaves-bytes-to-close   # the race, shown

WHY IT EXISTS. The owner's rule for a crash-consistent, multi-process design (2026-10-17): the design is checked by an
executable model, and a timing failure is a failure. PB3549 was one: a run unit under commit control ended with its
ROLLBACK refused, so its transaction stood in the file "as a death leaves it", and the next writable OPEN's dead-owner
step (KeyedStore.RollBackDeadOwners) must roll it back. It did, except once in a parallel Linux unit run: the endpoint's
disposal unlocked nothing (HandleRegionLocks.Dispose was a no-op) and the liveness byte was left to the CLOSE of the
descriptor, and a fork in another thread held a copy of that descriptor until its exec, so the open file description, and
with it the lock, outlived the CLOSE. The next OPEN read the dead owner as ALIVE. This model shows that race in every
interleaving where it exists, and shows it gone under the fixed protocol.

WHAT IT MODELS (the operating environment's facts the protocol relies on, and nothing else):
  * An OPEN FILE DESCRIPTION carries the byte-range locks (a Linux open-file-description lock; a Windows file object, whose
    LockFileEx locks Windows unlocks only once its last handle is closed). It lives while any descriptor refers to it, and
    its locks go when it dies. A lock request conflicts with every OTHER description's lock on the byte; F_GETLK never
    reports the asker's own.
  * A FORK in another thread copies every descriptor of the process into the child (each description gains a reference),
    and the child's EXEC closes them all (every descriptor of the runtime is close-on-exec). The fork and the exec fall at
    any point of the run units' steps, the exec possibly never before the probe (a child still starting).
  * A call of fcntl can answer EINTR (a signal; the .NET runtime itself signals threads) or ENOLCK (the kernel could not
    allocate the lock record now) instead of its answer, any number of times; neither is the answer to the question.

THE PROTOCOL UNDER TEST (the runtime's steps, in the model's terms):
  A (the owner): open (a descriptor, its description, its endpoint); draw (the liveness byte held EXCLUSIVELY through the
  endpoint, the transaction's entry in the file); end-refused (the abnormal ending's ROLLBACK refused: the transaction
  stands); then the GIVE-BACK of the kept handle (KeptFile.GiveBack): detach (off the store, under the mutex), dispose (the
  endpoint unlocks every byte it holds, through the descriptor, which is still open), close (the descriptor).
  B (the next writable OPEN, another run unit or a later one of the same process): its own descriptor, and the dead-owner
  probe of the liveness byte (F_OFD_GETLK through B's description), read by OfdRegionLocks.Classify and KeyedStore.OwnerOf:
  free is DEAD (the transaction is rolled back), held is ALIVE, a host that cannot carry the lock is UNKNOWABLE (read as
  alive: nothing is rolled back on a guess). EINTR is asked again for as long as it recurs; ENOLCK is asked again up to
  LOCK_RECORD_ATTEMPTS calls in all.

THE INVARIANTS:
  I1  B's OPEN after A's give-back rolls A's abandoned transaction back, whatever forks and execs fell where and however
      many transient EINTR/ENOLCK answers the probe met (§9.1.18.5, "When a run unit terminates abnormally, all file
      changes made since the previous commit or, if none, the beginning of the run unit are rolled back").
  I2  B's OPEN before A's give-back never rolls A's live transaction back, whatever the host answered.
A host that LASTINGLY cannot carry the lock is the documented Unknowable answer: nothing is rolled back on a guess, so I1 is
asked only of transient answers (fewer ENOLCKs than LOCK_RECORD_ATTEMPTS), and I2 of every answer.

ITS OWN WATCHDOG. --self-test also runs each named MUTATION (each a defect the protocol must not have: the one PB3549 found
and its siblings) and requires every row the mutation names RED under it; a model whose oracle or fault injector is
weakened would stay GREEN, which proves nothing. `--mutations` runs that arm alone.
"""
from __future__ import annotations

import argparse
import sys
import time
from pathlib import Path
sys.path.append(str(Path(__file__).resolve().parents[1]))  # scripts/, for standard_streams
from standard_streams import ensure_utf8  # noqa: E402

LIVENESS = "L"                # the transaction's liveness byte (HostRegionLocks.TransactionLivenessByte)
LOCK_RECORD_ATTEMPTS = 4      # OfdRegionLocks.LockRecordAttempts
EINTR, ENOLCK = "EINTR", "ENOLCK"

#: name -> (what it breaks, the rows that must catch it)
MUTATIONS = {
    "dispose-leaves-bytes-to-close": ("the endpoint's disposal unlocks nothing; the lock is left to the descriptor's close "
                                      "(HandleRegionLocks.Dispose before PB3549)", ["give-back-under-fork"]),
    "close-before-dispose": ("the descriptor is closed before the endpoint is disposed, so the unlock meets a closed "
                             "descriptor (SequentialConnector's CLOSE before PB3549)", ["give-back-under-fork"]),
    "eintr-not-retried": ("an interrupted probe answers Unavailable (OfdRegionLocks.HeldByAnother before PB3549)",
                          ["interrupted-probe"]),
    "enolck-not-retried": ("a transient ENOLCK answers Unavailable at once", ["transient-enolck"]),
    "unknowable-read-as-dead": ("a probe the host cannot answer rolls the owner back", ["live-owner-kept"]),
}


class Violation(AssertionError):
    pass


class Host:
    """Descriptors, open file descriptions and their byte-range locks."""

    def __init__(self, faults: list[str]):
        self.refs: dict[int, int] = {}                 # description -> live descriptors
        self.locks: dict[int, dict[str, bool]] = {}    # description -> byte -> exclusive
        self.fds: dict[tuple[str, int], int] = {}      # (process, fd) -> description
        self.next = 0
        self.faults = list(faults)                     # the answers the next probe calls give instead of their own

    def open(self, process: str) -> int:
        self.next += 1
        d = self.next
        self.refs[d], self.locks[d] = 1, {}
        self.fds[(process, d)] = d
        return d

    def close(self, process: str, fd: int) -> None:
        d = self.fds.pop((process, fd), None)
        if d is None:
            return
        self.refs[d] -= 1
        if self.refs[d] == 0:
            self.locks[d] = {}                         # the description dies, and its locks with it

    def fork(self) -> None:
        for (process, fd), d in list(self.fds.items()):
            if process == "parent":
                self.fds[("child", fd)] = d
                self.refs[d] += 1

    def exec(self) -> None:
        for (process, fd) in [k for k in self.fds if k[0] == "child"]:
            self.close(process, fd)                    # every runtime descriptor is close-on-exec

    def _description(self, process: str, fd: int) -> int | None:
        return self.fds.get((process, fd))

    def setlk(self, process: str, fd: int, byte: str, exclusive: bool) -> str:
        d = self._description(process, fd)
        if d is None:
            return "EBADF"
        for other, held in self.locks.items():
            if other != d and self.refs[other] > 0 and byte in held and (exclusive or held[byte]):
                return "EAGAIN"
        self.locks[d][byte] = exclusive
        return "OK"

    def unlock(self, process: str, fd: int, byte: str) -> str:
        d = self._description(process, fd)
        if d is None:
            return "EBADF"                             # an unlock through a closed descriptor does nothing
        self.locks[d].pop(byte, None)
        return "OK"

    def getlk(self, process: str, fd: int, byte: str) -> str:
        if self.faults:
            return self.faults.pop(0)
        d = self._description(process, fd)
        if d is None:
            return "EBADF"
        held = any(other != d and self.refs[other] > 0 and byte in locks for other, locks in self.locks.items())
        return "HELD" if held else "FREE"


def probe(host: Host, process: str, fd: int, mutation: str | None) -> str:
    """OfdRegionLocks.Classify + KeyedStore.OwnerOf: Dead, Alive or Unknowable."""
    attempts = 1
    while True:
        answer = host.getlk(process, fd, LIVENESS)
        if answer == "FREE":
            return "Dead"
        if answer == "HELD":
            return "Alive"
        if answer == EINTR and mutation != "eintr-not-retried":
            continue
        if answer == ENOLCK and mutation != "enolck-not-retried" and attempts < LOCK_RECORD_ATTEMPTS:
            attempts += 1
            continue
        return "Unknowable"


class World:
    def __init__(self, faults: list[str], mutation: str | None):
        self.host = Host(faults)
        self.mutation = mutation
        self.fd_a: int | None = None
        self.held_a: set[str] = set()                  # the endpoint's hold map
        self.transaction = False                       # the transaction's entry stands in the file
        self.given_back = False                        # A's give-back has begun (its owner is gone from then on)
        self.trace: list[str] = []

    # ── A's steps ──
    def a_open(self):
        self.fd_a = self.host.open("parent")

    def a_draw(self):
        if self.host.setlk("parent", self.fd_a, LIVENESS, exclusive=True) != "OK":
            raise Violation("the owner could not draw its liveness byte")
        self.held_a.add(LIVENESS)
        self.transaction = True

    def a_end_refused(self):
        pass                                           # the ROLLBACK refused: the transaction stands

    def a_detach(self):
        self.given_back = True

    def a_dispose(self):
        if self.mutation != "dispose-leaves-bytes-to-close":
            for byte in sorted(self.held_a):
                self.host.unlock("parent", self.fd_a, byte)
        self.held_a.clear()

    def a_close(self):
        self.host.close("parent", self.fd_a)

    def a_steps(self):
        tail = [("close", self.a_close), ("dispose", self.a_dispose)] if self.mutation == "close-before-dispose" \
            else [("dispose", self.a_dispose), ("close", self.a_close)]
        return [("open", self.a_open), ("draw", self.a_draw), ("end-refused", self.a_end_refused),
                ("detach", self.a_detach)] + tail

    # ── B's writable OPEN and its dead-owner step ──
    def b_open(self):
        fd_b = self.host.open("parent")
        answer = probe(self.host, "parent", fd_b, self.mutation)
        live_owner = self.transaction and not self.given_back
        roll_back = answer == "Dead" or (answer == "Unknowable" and self.mutation == "unknowable-read-as-dead")
        self.trace.append(f"B probed {answer}")
        if self.transaction and roll_back:
            if live_owner:
                raise Violation("I2: B rolled back the transaction of an owner that still holds it: " + " -> ".join(self.trace))
            self.transaction = False
        self.host.close("parent", fd_b)


def run(b_at: int, fork_at: int | None, exec_at: int | None, faults: list[str], mutation: str | None) -> World:
    """One interleaving: A's steps with B's OPEN before step `b_at` (len = after them all), a fork before step `fork_at`
    and its exec before step `exec_at` (None: never, before B's probe), the probe's first answers `faults`."""
    w = World(faults, mutation)
    steps = w.a_steps()
    b = ("B-open", w.b_open)
    order = steps[:b_at] + [b] + steps[b_at:]
    for i, (name, step) in enumerate(order):
        if i == fork_at:
            w.host.fork()
            w.trace.append("fork")
        if exec_at is not None and i == exec_at:
            w.host.exec()
            w.trace.append("exec")
        step()
        w.trace.append(name)
    return w


def interleavings(b_positions, fault_sets):
    for b_at in b_positions:
        n = 6 + 1                                      # A's six steps and B's
        for fork_at in [None, *range(n)]:
            for exec_at in [None] if fork_at is None else [None, *range(fork_at + 1, n)]:
                for faults in fault_sets:
                    yield b_at, fork_at, exec_at, faults


def row_after_give_back(fault_sets):
    """I1: B after A's whole give-back."""
    def check(mutation):
        runs = 0
        for b_at, fork_at, exec_at, faults in interleavings([6], fault_sets):
            w = run(b_at, fork_at, exec_at, faults, mutation)
            runs += 1
            if w.transaction:
                raise Violation(f"I1: B's OPEN after the give-back left the abandoned transaction standing "
                                f"(fork before step {fork_at}, exec before step {exec_at}, probe faults {faults}): "
                                + " -> ".join(w.trace))
        return runs
    return check


def row_live_owner():
    """I2: B after A's draw and before its give-back begins (before end-refused, before detach), every host answer."""
    lasting = [ENOLCK] * LOCK_RECORD_ATTEMPTS
    fault_sets = [[], [EINTR], [ENOLCK], lasting, [EINTR] + lasting]

    def check(mutation):
        runs = 0
        for b_at, fork_at, exec_at, faults in interleavings([2, 3], fault_sets):
            w = run(b_at, fork_at, exec_at, faults, mutation)
            runs += 1
            if not w.transaction:
                raise Violation("I2: the live owner's transaction is gone: " + " -> ".join(w.trace))
        return runs
    return check


ROWS = {
    "give-back-under-fork": row_after_give_back([[]]),
    "interrupted-probe": row_after_give_back([[EINTR], [EINTR, EINTR], [EINTR, EINTR, EINTR]]),
    "transient-enolck": row_after_give_back([[ENOLCK] * k for k in range(1, LOCK_RECORD_ATTEMPTS)] + [[ENOLCK, EINTR, ENOLCK]]),
    "live-owner-kept": row_live_owner(),
}


def run_rows(verbose: bool, mutation: str | None = None) -> tuple[int, int]:
    runs = failures = 0
    for name, check in ROWS.items():
        try:
            n = check(mutation)
            runs += n
            if verbose:
                print(f"OK    {name}: {n} interleavings")
        except Violation as v:
            failures += 1
            if verbose:
                print(f"FAIL  {name}: {v}")
    return runs, failures


def prove_mutations(verbose: bool) -> int:
    """Every mutation must turn each row it names RED on its own; a GREEN witness row means the watchdog is dead."""
    dead = 0
    for name, (what, witnesses) in MUTATIONS.items():
        for row in witnesses:
            try:
                ROWS[row](name)
            except Violation as v:
                if verbose:
                    print(f"RED   mutation {name} ({what}) on row '{row}': {v}")
                continue
            dead += 1
            print(f"FAIL  mutation {name} ({what}) left its witness row '{row}' GREEN: the watchdog is dead")
    return dead


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    ap.add_argument("--self-test", action="store_true", help="every row, then the watchdog")
    ap.add_argument("--rows", action="store_true", help="run the rows and print each")
    ap.add_argument("--mutations", action="store_true", help="run only the watchdog's proof")
    ap.add_argument("--mutation", choices=sorted(MUTATIONS), help="apply one named mutation to the rows (by hand)")
    args = ap.parse_args(argv)
    t0 = time.time()
    if args.mutations:
        failures = prove_mutations(verbose=True)
    elif args.self_test or args.rows:
        runs, failures = run_rows(verbose=True, mutation=args.mutation)
        print(f"rows: {runs} interleavings")
        if args.self_test and args.mutation is None:
            failures += prove_mutations(verbose=True)
    else:
        ap.print_help()
        return 2
    verdict = "GREEN" if failures == 0 else f"RED ({failures} violations)"
    print(f"=== KEPT LIVENESS MODEL: {verdict} in {time.time() - t0:.1f}s ===")
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    ensure_utf8()
    sys.exit(main())
