#!/usr/bin/env python3
"""sequential_journal_model.py — THE EXECUTABLE MODEL of a SEQUENTIAL file under APPLY COMMIT
(docs/rearchitecture/DESIGN-runtime-library.md 2.2.2; kb/Work PB3340, PB3426).

    python scripts/models/sequential_journal_model.py --self-test     # the bounded sweep every gate and CI runs
    python scripts/models/sequential_journal_model.py --depth 6       # a deeper sweep by hand (minutes)
    python scripts/models/sequential_journal_model.py --rows          # run and print the 2.2.2.12 rows

WHY IT EXISTS. The owner's rule for a crash-consistent, multi-process design (2026-10-17, after the keyed store's five
prose review rounds): the design is checked by an executable model from round one, and reviewed only once the model
passes. This file IS that model for 2.2.2, and the reviews of 2026-10-19 (kb/Work PB3426 R1-1, R1-2, R2-1) are what it
catches: a REWRITE journaled after a capture, a created file left in place by a rollback, and a rollback that EXTENDED
a file another transaction's rollback had shortened. It runs the
protocol — the files, the writes, the syncs, the locks and the ORDER of the steps — over a medium that can REFUSE any
operation (the process continues), KILL the run unit at any operation (its locks go, its unsynced writes stay in the
operating environment), or LOSE POWER at any operation (every file keeps a PREFIX of its unsynced writes, a file
created without a directory sync vanishes, a file deleted without one returns); with a second run unit — not under
commit control, or under its own — run at every operation boundary of the first; and it asserts, after "the next
entry" (a fresh run unit's writable OPEN, which runs the entry step), that
  I1  the physical file holds EXACTLY the state at the last COMMIT (the journal read `O`, or was torn), or EXACTLY the
      state the COMMIT made durable (its decision was synced), never a third — and the program was told success only
      in the second case;
  I2  nothing another run unit changed is undone, and a record another run unit holds a lock on is never written;
  I3  no journal, holder, before file or empty lock directory is left behind, and
  I4  a capture (an OPEN OUTPUT of an existing file, a DELETE FILE) never runs while another connector holds the file.

WHAT IT IS NOT. It models the protocol, not the bytes: a record is a token, a file is a list of records, L is a
record count, a checksum is "the unit is whole or it is not". Framing, CRCs, the host APIs and the SidecarLockFiles
publication dance are the implementation's (PB3340), whose IHostMedium simulator (2.2.2.12) re-runs these rows over
real bytes. The second run unit's statements run atomically at an interleaving point of the first — before every medium
operation, and before every statement (the point where a ROLLBACK, a writable OPEN, a DELETE FILE or a COMMIT takes the
entry mutex as its first act; offered the points before medium operations only, the second run unit could never begin
between an append and the ROLLBACK that undid it: review round 3, R3-1): every mutual exclusion the protocol relies on
inside one statement is the entry mutex, modelled as a lock, so a finer interleaving reaches no state this one cannot (a
statement that finds the mutex held is BLOCKED and simply runs after the holder's statement, which is another point of the
same sweep).

THE PROTOCOL UNDER TEST (2.2.2.4–2.2.2.8, in the model's terms; every name below is a step the design states):
  files in the lock directory D: txn-<t>.live (the holder: locked while the owner lives, the liveness and the
  ownership), txn-<t>.journal (readable by everyone: header H(start); entries L(length), R(ordinal, image), K(kind);
  decision D(C|O|R) appended), txn-<t>.before (the copy of F a capture took), mutex (the entry mutex, held for the
  entry step and the rest of a writable OPEN, a DELETE FILE, a first change, a capture and a rollback).
  begin:    under the mutex: the lowest t with neither file; create+lock .live; create .journal; H synced; D synced.
  BASE:     start P and no K: the transaction's FIRST append writes L(n), n = F's length at that moment, and syncs it
            BEFORE the append — no other transaction lives at an append (each holds END), so n IS the committed
            length, and everything at or past it is this transaction's own from then on; later appends write
            nothing; a REWRITE of an ordinal k that is not its own append (every k before its first append, k < L
            after it) writes R(k, image) and syncs it BEFORE the overwrite, once per ordinal. The length at the BEGIN
            is never recorded: it may count another transaction's uncommitted appends, present or still in flight
            (review round 2, R2-1), and a transaction that never appended has nothing to truncate.
  capture:  under the mutex, with no other handle on F: copy F to .before, sync it, sync D, append K, sync; the
            phase is REPLACED: nothing more is journaled, whatever the verbs after it do to F.
  start A:  F absent at the first change (a creation): REPLACED from the start; no entry is ever written.
  COMMIT:   sync F; sync F's directory; append D(C); sync (THE DECISION); then retire (.before, .journal, .live,
            sync D). Refused before the decision's sync: region 1 (rollback); the sync of a handed-over C refused:
            region 3 (reverse: D(O) synced, then rollback; a refused reversal MARKS the file and touches nothing).
  ROLLBACK: under the mutex: K → F := .before; start P → every R(k) written back, F truncated to L when the journal
            has an L record (F is never shorter than it: only an appending transaction's rollback truncates, and two
            transactions never append at once — the model asserts it), F synced; start A → F deleted; then D(R)
            synced, then retire. Idempotent at every step.
  entry:    under the mutex, for every t whose .live is not held: torn or absent journal → litter deleted; D(C)/D(R)
            → retire; D(O) → rollback. A held .live is a live transaction: its holdings are read from its journal.
  holdings: a live journal holds the END of F (every WRITE through another connector: '51'), every ordinal in its R
            set and, once it has appended, every ordinal ≥ L (READ not IGNORING LOCK, REWRITE: '51'), and the WHOLE
            file once REPLACED (a
            writable OPEN by another run unit: '61'); an OPEN OUTPUT or DELETE FILE also needs no other handle ('61').

ITS OWN WATCHDOG (review round 2, R2-2; round 3, R3-1). A model that stays GREEN proves nothing once its oracle or its
fault injector is weakened, so --self-test also runs the protocol with each named MUTATION applied (each a defect a review
found or a write-ahead order broken); each mutation names the rows of 2.2.2.12 that must catch it, and the proof requires
each of those rows RED under it on its own — a RED from some other row proves nothing about the row the design names for
the defect (R3-1: the R2-1 row was GREEN under its own mutation while the arm passed on the twin found elsewhere).
`--mutations` runs that arm alone; `--mutation <name>` applies one to a hand run.
"""
from __future__ import annotations

import argparse
import itertools
import sys
import time
from dataclasses import dataclass, field

ABSENT = "ABSENT"
ZERO = "ZERO"      # the record a SetLength past the end of F appends: zero bytes, never a committed record

# name -> (what the mutation puts back or breaks, the rows of ROWS that must turn RED under it — its witnesses)
MUTATIONS = {
    "R1-1": ("an R record journaled after a capture, re-applied over the restored before file (review round 1)",
             ("R1-1 capture then REWRITE",)),
    "R1-2": ("a creation journaled as START P with L = 0: the rollback truncates the created file instead of removing it",
             ("R1-2 OPTIONAL created by OPEN EXTEND", "R1-2 created by OPEN OUTPUT")),
    "R1": ("the header not synced before the first change", ("first WRITE under EXTEND",)),
    "R3": ("the L record not synced before the first append", ("first WRITE under EXTEND",)),
    "R4": ("the K record synced before the before file", ("DELETE FILE then COMMIT",)),
    "R2-1": ("L read at the BEGIN, as the first text had it: it counts another transaction's appends, undone or in flight",
             ("R2-1 a transaction begun beside another's append, undone",
              "R2-1 a transaction begun beside another's append, committed")),
}
REFUSE, DIE, POWER = "refuse", "die", "power"


# ----------------------------------------------------------------------------------------------------------------------
# Faults
# ----------------------------------------------------------------------------------------------------------------------
class Refused(Exception):
    """The operating environment refused this one operation; the process continues."""


class Died(Exception):
    """The run unit died at this operation: its locks and handles are gone, its unsynced writes stay in the cache."""


class PowerLoss(Exception):
    """Every process died and the medium keeps only what was synced (a prefix of each file's unsynced writes)."""


class Blocked(Exception):
    """The statement found the entry mutex held by a live run unit: it runs after the holder's statement instead."""


class Fault:
    """The one fault, or the one interleaving point, of a scenario. The medium counts its OPERATIONS, and the fault modes
    index those: the n-th operation is refused, kills the run unit, or loses power. The driver counts the INTERLEAVING
    POINTS of the first run unit's program — every medium operation AND the boundary before every statement, its implicit
    COMMIT and item 8's ROLLBACK (the point where a ROLLBACK, a writable OPEN, a DELETE FILE or a COMMIT takes the entry
    mutex as its first act: offered the points before medium operations only, the second run unit could never begin
    between an append and the ROLLBACK that undid it, and the row named for R2-1 was a dead witness — review round 3,
    R3-1) — and the second run unit's program runs at the n-th point."""

    def __init__(self, target: int = 0, mode: str | None = None, hook=None, die_after_hook: bool = False):
        self.target, self.mode, self.hook, self.die_after_hook = target, mode, hook, die_after_hook
        self.ops, self.points, self.fired, self.hook_done = 0, 0, False, False

    def op(self):
        """Before a medium operation."""
        self.ops += 1
        self._point()
        if self.mode is not None and self.ops == self.target and not self.fired:
            self.fired = True
            if self.mode == REFUSE:
                raise Refused()
            if self.mode == DIE:
                raise Died()
            if self.mode == POWER:
                raise PowerLoss()

    def boundary(self):
        """Before a statement of the first run unit, before its implicit COMMIT, and before item 8's ROLLBACK."""
        self._point()

    def _point(self):
        self.points += 1
        if self.hook_done and self.die_after_hook:
            self.die_after_hook = False
            raise Died()
        if self.hook is not None and self.points == self.target and not self.fired:
            self.fired = True
            self.hook()
            self.hook_done = True


# ----------------------------------------------------------------------------------------------------------------------
# The medium: files as lists of units, durable and pending, with directory durability
# ----------------------------------------------------------------------------------------------------------------------
class FileImage:
    __slots__ = ("exists", "exists_durable", "durable", "pending")

    def __init__(self):
        self.exists, self.exists_durable, self.durable, self.pending = True, False, [], []


def dirname(path: str) -> str:
    return path.rsplit("/", 1)[0]


class Medium:
    def __init__(self, fault: Fault):
        self.fault = fault
        self.files: dict[str, FileImage] = {}
        self.ops = 0

    def _op(self):
        self.ops += 1
        self.fault.op()

    def exists(self, path: str) -> bool:
        f = self.files.get(path)
        return f is not None and f.exists

    def _open(self, path: str) -> FileImage:
        f = self.files.get(path)
        if f is None or not f.exists:
            raise FileNotFoundError(path)
        return f

    def create(self, path: str):
        self._op()
        f = self.files.get(path)
        if f is not None and f.exists:
            raise FileExistsError(path)
        if f is None:
            f = self.files[path] = FileImage()
        f.exists, f.durable, f.pending = True, [], []

    def write(self, path: str, unit):
        self._op()
        self._open(path).pending.append(unit)

    def sync(self, path: str):
        self._op()
        f = self._open(path)
        f.durable += f.pending
        f.pending = []

    def syncdir(self, d: str):
        self._op()
        for p, f in self.files.items():
            if dirname(p) == d:
                f.exists_durable = f.exists

    def delete(self, path: str):
        """Idempotent: a name another run unit removed between the caller's look and its delete is simply gone
        (SidecarLockFiles' rule: a tester deletes any holder it can open freely, so an owner's own delete of a
        holder it has just unlocked may find it gone)."""
        self._op()
        f = self.files.get(path)
        if f is not None:
            f.exists = False

    def view(self, path: str) -> list:
        f = self._open(path)
        return f.durable + f.pending

    def listdir(self, d: str) -> list[str]:
        return sorted(p for p, f in self.files.items() if f.exists and dirname(p) == d)

    def pending_shape(self) -> list[tuple[str, int]]:
        return [(p, len(f.pending)) for p, f in sorted(self.files.items()) if f.exists and f.pending]

    def power_loss(self, keep: dict[str, int]):
        for p, f in list(self.files.items()):
            if not f.exists_durable:
                del self.files[p]
                continue
            f.exists = True
            f.durable += f.pending[: keep.get(p, 0)]
            f.pending = []


def records(units: list) -> list:
    out: list = []
    for u in units:
        kind = u[0]
        if kind == "app":
            out.append(u[1])
        elif kind == "ovw":
            if u[1] < len(out):
                out[u[1]] = u[2]
        elif kind == "setlen":
            if u[1] < len(out):
                del out[u[1]:]
            else:
                out.extend([ZERO] * (u[1] - len(out)))    # SetLength past the end: the file grows with zero bytes
        elif kind == "set":
            out = list(u[1])
    return out


# ----------------------------------------------------------------------------------------------------------------------
# The host: locks and handles that die with their run unit
# ----------------------------------------------------------------------------------------------------------------------
class Host:
    def __init__(self):
        self.locks: dict[str, object] = {}
        self.handles: dict[str, set] = {}
        self.region: dict[tuple, object] = {}
        self.append: dict[str, object] = {}

    def lock(self, name: str, who) -> bool:
        holder = self.locks.get(name)
        if holder is None or holder is who:
            self.locks[name] = who
            return True
        return False

    def unlock(self, name: str, who):
        if self.locks.get(name) is who:
            del self.locks[name]

    def holder(self, name: str):
        return self.locks.get(name)

    def handles_on(self, path: str) -> set:
        return self.handles.setdefault(path, set())

    def kill(self, who):
        for name in [n for n, h in self.locks.items() if h is who]:
            del self.locks[name]
        for hs in self.handles.values():
            hs.discard(who)
        for key in [k for k, h in self.region.items() if h is who]:
            del self.region[key]
        for path in [p for p, h in self.append.items() if h is who]:
            del self.append[path]


# ----------------------------------------------------------------------------------------------------------------------
# The oracle: what the file must hold after the next entry
# ----------------------------------------------------------------------------------------------------------------------
class Oracle:
    """The state at the last COMMIT, kept by replaying each run unit's OWN changes when they commit (a change
    outside commit control commits the moment it is made), never by reading the file: the file holds other run
    units' uncommitted changes too. A COMMIT handed to the operating environment and never acknowledged (its
    decision written, its sync unanswered, the run unit dead) leaves TWO candidates — the committed state and the
    one that COMMIT made durable — carried through every later run unit's commit until the owner acknowledges or
    reverses it, or a later change that only one candidate admits decides it."""

    def __init__(self, initial):
        self.committed = initial
        self.post = None      # the state a handed-over but unacknowledged COMMIT may have made durable
        self.post_by = None   # the run unit whose COMMIT that is

    @staticmethod
    def replay(state, changes):
        """The state after the changes, or None: the changes cannot have been made on that state."""
        out = list(state) if state != ABSENT else ABSENT
        for c in changes:
            if c[0] in ("app", "ovw") and out == ABSENT:
                return None
            if c[0] == "app":
                out.append(c[1])
            elif c[0] == "ovw":
                if c[1] < len(out):
                    out[c[1]] = c[2]
            elif c[0] == "trunc":
                out = []
            elif c[0] == "create":
                out = []
            elif c[0] == "delete":
                out = ABSENT
        return out

    def _apply(self, changes):
        c = self.replay(self.committed, changes)
        p = self.replay(self.post, changes) if self.post is not None else None
        if c is None and p is None:
            raise Violation(f"I1: the changes {changes} committed on a file neither candidate state admits "
                            f"(committed {self.committed}, unacknowledged {self.post})")
        if c is None or p is None:                    # only one candidate admits them: it is the state
            self.committed, self.post, self.post_by = (p if c is None else c), None, None
        else:
            self.committed, self.post = c, p

    def commit(self, who, changes):
        if self.post is not None and self.post_by is who:
            self.committed, self.post, self.post_by = self.post, None, None   # the handed-over COMMIT acknowledged
        else:
            self._apply(changes)

    def handed(self, who, changes):
        if self.post is not None:
            # another run unit's COMMIT is unacknowledged; this one's sync is answered at once (the second run unit
            # runs fault-free), so its commit() applies its changes to both candidates
            return
        self.post, self.post_by = self.replay(self.committed, changes), who
        if self.post is None:
            raise Violation(f"I1: the changes {changes} handed over on a file the committed state {self.committed} refuses")

    def reversed(self, who):
        if self.post_by is who:
            self.post, self.post_by = None, None

    def plain(self, change):
        self._apply([change])


# ----------------------------------------------------------------------------------------------------------------------
# A run unit's connector on F
# ----------------------------------------------------------------------------------------------------------------------
@dataclass
class Txn:
    id: int
    start: str
    phase: str
    L: int | None = None        # the L record: F's length at this transaction's first append; None until it appends
    journaled: set = field(default_factory=set)


class RunUnit:
    def __init__(self, name: str, m: Medium, h: Host, oracle: Oracle, F: str, D: str, covered: bool, optional: bool,
                 mutation: str | None = None):
        self.name, self.m, self.h, self.oracle, self.F, self.D = name, m, h, oracle, F, D
        self.mutation = mutation                    # one of MUTATIONS, applied to this run unit's protocol, or None
        self.Fdir = dirname(F)
        self.covered, self.optional = covered, optional
        self.mode: str | None = None
        self.pos = 0
        self.last_read: int | None = None
        self.absent_input = False
        self.txn: Txn | None = None
        self.marked = False
        self.rollback_owed = False
        self.changes: list = []
        self.MUTEX = D + "/mutex"

    def __repr__(self):
        return self.name

    # --- names ---
    def J(self, t): return f"{self.D}/txn-{t}.journal"
    def LIVE(self, t): return f"{self.D}/txn-{t}.live"
    def BEFORE(self, t): return f"{self.D}/txn-{t}.before"

    # --- reads ---
    def _recs(self) -> list:
        return records(self.m.view(self.F))

    def _ids(self) -> list[int]:
        ids = set()
        for p in self.m.listdir(self.D):
            name = p[len(self.D) + 1:]
            if name.startswith("txn-"):
                ids.add(int(name[4:].split(".")[0]))
        return sorted(ids)

    def _alive(self, t) -> bool:
        return self.h.holder(self.LIVE(t)) is not None

    def _mine(self, t) -> bool:
        return self.txn is not None and self.txn.id == t

    def _read_journal(self, t):
        if not self.m.exists(self.J(t)):
            return None
        units = self.m.view(self.J(t))
        if not units or units[0][0] != "H":
            return None
        start, L = units[0][1], None
        decision, entries = "O", []
        for u in units[1:]:
            if u[0] == "D":
                decision = u[1]
            elif u[0] == "L":
                L = u[1]                              # the first append's record: the committed length at that moment
            else:
                entries.append(u)
        K = next((e for e in entries if e[0] == "K"), None)
        return {"start": start, "L": L, "decision": decision, "K": K,
                "R": {e[1]: e[2] for e in entries if e[0] == "R"},
                "replaced": start == "A" or K is not None}

    # --- the mutex ---
    def _take_mutex(self):
        if not self.h.lock(self.MUTEX, self):
            raise Blocked()

    def _drop_mutex(self):
        self.h.unlock(self.MUTEX, self)

    # --- the entry step (caller holds the mutex) ---
    def _entry_step(self):
        for t in self._ids():
            if self._mine(t):
                continue
            if self._alive(t) and self.h.holder(self.LIVE(t)) is not self:
                continue                              # another run unit's live transaction: its holdings apply
            self._settle(t)                           # dead, or my own whose retirement was refused

    def _settle(self, t: int):
        """Finish transaction t from what its journal says, whoever runs it: the owner or the entering run unit."""
        j = self._read_journal(t)
        if j is None:
            self._retire(t)                           # litter: a holder or a header that never synced
        elif j["decision"] in ("C", "R"):
            self._retire(t)                           # a retirement the owner did not finish
        else:
            self._rollback(t, j)                      # an open transaction: the owner's, or a dead owner's

    def _rollback(self, t: int, j: dict):
        F = self.F
        if j["K"] is not None:
            if not self.m.exists(self.BEFORE(t)) or not self.m.view(self.BEFORE(t)):
                raise Violation(f"I1: journal {t} names a capture but its before file is missing or empty")
            img = self.m.view(self.BEFORE(t))[0][1]
            if not self.m.exists(F):
                self.m.create(F)
            self.m.write(F, ("set", img))
        elif self.m.exists(self.BEFORE(t)):
            self.m.delete(self.BEFORE(t))             # a copy whose K never synced: F was never truncated
        if j["start"] == "P":
            if not self.m.exists(F):
                # the protocol never reaches this: START P is written for a file that exists, a capture's removal is
                # undone above, and a creation is START A (the mutation R1-2 reaches it: a creation as START P)
                raise Violation(f"I1: journal {t} says F existed at its begin (START P) but F is absent")
            cur = self._recs()
            for k, old in sorted(j["R"].items()):
                if k < len(cur):
                    CHECKS.no_foreign_lock(self, k)
                    self.m.write(F, ("ovw", k, old))
            if j["L"] is not None:                    # the transaction appended: everything at or past L is its own
                if len(cur) < j["L"]:
                    raise Violation(f"I1: F has {len(cur)} records, fewer than journal {t}'s L {j['L']}: SetLength "
                                    f"would extend it with zero bytes")
                self.m.write(F, ("setlen", j["L"]))
            self.m.sync(F)
            self.m.syncdir(self.Fdir)
        else:
            if self.m.exists(F):
                CHECKS.no_foreign_handle(self)
                self.m.delete(F)
                self.m.syncdir(self.Fdir)
        self.m.write(self.J(t), ("D", "R"))
        self.m.sync(self.J(t))
        self._retire(t)

    def _retire(self, t: int):
        for p in (self.BEFORE(t), self.J(t)):
            if self.m.exists(p):
                self.m.delete(p)
        live = self.LIVE(t)
        self.h.unlock(live, self)
        if self.m.exists(live):
            self.m.delete(live)
        self.m.syncdir(self.D)

    # --- holdings of live journals, and the sweep of dead ones ---
    def _holding_test(self, kind: str, k: int | None = None, exclusive: bool = False) -> str | None:
        """kind: WRITE | READ | REWRITE | OPEN | DELETE. Returns a status, or None when the statement may proceed."""
        if any(not self._alive(t) and not self._mine(t) for t in self._ids()):
            if self.h.holder(self.MUTEX) is self:
                self._entry_step()
            else:
                self._take_mutex()
                try:
                    self._entry_step()
                finally:
                    self._drop_mutex()
        others = self.h.handles_on(self.F) - {self}
        if exclusive and others:
            return "61"
        if kind == "WRITE" and self.h.append.get(self.F) not in (None, self):
            return "51"
        if kind in ("READ", "REWRITE") and self.h.region.get((self.F, k)) not in (None, self):
            return "51"
        for t in self._ids():
            if self._mine(t):
                continue
            j = self._read_journal(t)                 # a holder with no header yet is a transaction being born
            if kind in ("OPEN", "DELETE"):
                if exclusive or j is None or j["replaced"]:
                    return "61"
            elif kind == "WRITE":
                return "51"
            else:
                if j is None or j["replaced"] or k in j["R"] or (j["L"] is not None and k >= j["L"]):
                    return "51"
        return None

    # --- begin and capture (caller holds the mutex) ---
    def _begin(self, start: str):
        t = 0
        while self.m.exists(self.J(t)) or self.m.exists(self.LIVE(t)):
            t += 1
        live, j = self.LIVE(t), self.J(t)
        try:
            self.m.create(live)
            self.h.lock(live, self)
            self.m.create(j)
            self.m.write(j, ("H", start))
            if start == "P" and self.mutation == "R2-1":
                self.m.write(j, ("L", len(self._recs())))   # the mutation: the first text's L, read at the begin
            if self.mutation != "R1":                 # the mutation: the header not synced before the first change
                self.m.sync(j)
                self.m.syncdir(self.D)
        except Refused:
            self.h.unlock(live, self)
            for p in (j, live):
                if self.m.exists(p):
                    self.m.delete(p)
            raise
        self.txn = Txn(t, start, "BASE" if start == "P" else "REPLACED")
        if start == "P" and self.mutation == "R2-1":
            self.txn.L = len(self._recs())
        if self.m.exists(self.F):
            self.h.append[self.F] = self

    def _begin_creation(self):
        """The first change creates F: START A, REPLACED from the start (the mutation: journaled as START P on an empty
        file, so the rollback truncates the created file to nothing instead of removing it — the review's R1-2)."""
        self._begin("P" if self.mutation == "R1-2" else "A")
        if self.mutation == "R1-2":
            self.txn.L = 0
            self.m.write(self.J(self.txn.id), ("L", 0))
            self.m.sync(self.J(self.txn.id))

    def _note_append(self):
        """R3: the transaction's FIRST append writes the L record — F's length at this moment, which is the committed
        length, since no other transaction lives (each holds END, and the holding test just passed) — and syncs it
        before the append; later appends, and appends in the REPLACED phase, write nothing. (The mutation R3: not
        synced, so a power loss can drop the record and keep the append.)"""
        txn = self.txn
        if txn.phase != "BASE" or txn.L is not None:
            return
        n = len(self._recs())
        self.m.write(self.J(txn.id), ("L", n))
        if self.mutation != "R3":
            self.m.sync(self.J(txn.id))
        txn.L = n

    def _capture(self, kind: str):
        CHECKS.no_foreign_handle(self)
        t = self.txn.id
        b = self.BEFORE(t)
        img = tuple(self._recs())
        handed = False
        try:
            self.m.create(b)
            self.m.write(b, ("img", img))
            if self.mutation == "R4":                 # the mutation: K handed over and synced before the before file is
                self.m.write(self.J(t), ("K", kind))
                handed = True
                self.m.sync(self.J(t))
            self.m.sync(b)
            self.m.syncdir(self.D)
            if self.mutation != "R4":
                self.m.write(self.J(t), ("K", kind))
                handed = True
                self.m.sync(self.J(t))
        except Refused:
            if not handed:
                if self.m.exists(b):
                    self.m.delete(b)
                raise
            # The entry is in the cache and may or may not be on the medium: the journal is no longer known, so
            # nothing more changes through this transaction — the statement is fatal and item 8 rolls back from the
            # cache (K present: the before file holds what F still holds). A power loss that drops K drops nothing
            # else: F was not truncated, and the before file is litter the next entry removes.
            self.txn.phase = "REPLACED"
            raise
        self.txn.phase = "REPLACED"

    def _end_transaction(self):
        self.txn = None
        self.rollback_owed = False
        self.changes = []
        for key in [key for key, h in self.h.region.items() if h is self]:
            del self.h.region[key]
        if self.h.append.get(self.F) is self:
            del self.h.append[self.F]
        if self.mode is None:
            self.h.handles_on(self.F).discard(self)

    # --- statements ---
    def open(self, mode: str) -> str:
        if self.mode is not None:
            return "41"
        F = self.F
        if mode == "INPUT":
            if not self.m.exists(F):
                if not self.optional:
                    return "35"
                self.mode, self.absent_input = "INPUT", True
                return "05"
            self.mode, self.pos = "INPUT", 0
            self.h.handles_on(F).add(self)
            return "00"
        self._take_mutex()
        try:
            self._entry_step()
            st = self._holding_test("OPEN", exclusive=(mode == "OUTPUT"))
            if st is not None:
                return st
            exists = self.m.exists(F)
            if mode == "OUTPUT":
                if exists:
                    if self.covered:
                        if self.txn is None:
                            self._begin("P")
                        if self.txn.phase == "BASE":
                            self._capture("O")
                    self.m.write(F, ("setlen", 0))
                    self._change(("trunc", 0))
                else:
                    if self.covered and self.txn is None:
                        self._begin_creation()
                    self.m.create(F)
                    self._change(("create",))
                status = "00"
            else:
                if not exists:
                    if not self.optional:
                        return "35"
                    if self.covered and self.txn is None:
                        self._begin_creation()
                    self.m.create(F)
                    self._change(("create",))
                    status = "05"
                else:
                    status = "00"
            self.mode, self.pos = mode, 0
            self.h.handles_on(F).add(self)
            if self.covered and self.txn is not None:
                self.h.append[F] = self
            return status
        except Refused:
            return "30"
        finally:
            self._drop_mutex()

    def write(self, rec: str) -> str:
        if self.mode not in ("OUTPUT", "EXTEND"):
            return "48"
        if self.covered and self.txn is None:
            # The first change: the holding test and the begin under ONE hold of the mutex, so no other transaction
            # begins on F between the test and the header. A writer whose file a rollback removed (the state at the
            # last COMMIT was "no file") creates it again here, as its OPEN did.
            self._take_mutex()
            try:
                self._entry_step()
                st = self._holding_test("WRITE")
                if st is not None:
                    return st
                if self.m.exists(self.F):
                    self._begin("P")
                    self._note_append()               # under the same hold: header and L synced before the append
                else:
                    self._begin_creation()
                    self.m.create(self.F)
                    self._change(("create",))
                    self.h.handles_on(self.F).add(self)
                self.h.append[self.F] = self
            except Refused:
                return "30"
            finally:
                self._drop_mutex()
        else:
            st = self._holding_test("WRITE")
            if st is not None:
                return st
            if self.covered:
                try:
                    self._note_append()
                except Refused:
                    return "30"                       # fatal (R5): the append never ran, so F is right either way
        try:
            self.m.write(self.F, ("app", rec))
        except Refused:
            return "30"
        self._change(("app", rec))
        return "00"

    def read(self) -> str:
        if self.mode not in ("INPUT", "IO"):
            return "47"
        if self.absent_input or not self.m.exists(self.F):
            return "10"                               # an optional file not present, or one a rollback removed
        recs = self._recs()
        if self.pos >= len(recs):
            return "10"
        k = self.pos
        st = self._holding_test("READ", k)
        if st is not None:
            return st
        self.h.region[(self.F, k)] = self
        self.last_read, self.pos = k, k + 1
        return "00"

    def rewrite(self, rec: str) -> str:
        if self.mode != "IO" or self.last_read is None or not self.m.exists(self.F):
            return "43"
        k = self.last_read
        st = self._holding_test("REWRITE", k)
        if st is not None:
            return st
        recs = self._recs()
        if self.covered:
            if self.txn is None:
                self._take_mutex()
                try:
                    self._begin("P")
                except Refused:
                    return "30"
                finally:
                    self._drop_mutex()
            txn = self.txn
            # R2: an image of every record that is not this transaction's own append (every ordinal before its first
            # append; k < L after it), in the base phase only (the mutation R1-1: in the REPLACED phase too)
            if (txn.phase == "BASE" or self.mutation == "R1-1") and (txn.L is None or k < txn.L) \
                    and k not in txn.journaled:
                try:
                    self.m.write(self.J(txn.id), ("R", k, recs[k]))
                    txn.journaled.add(k)          # handed over: the entry may be on the medium, so it stays claimed
                    self.m.sync(self.J(txn.id))
                except Refused:
                    return "30"                   # fatal: item 8 rolls back; the overwrite never ran
        try:
            self.m.write(self.F, ("ovw", k, rec))
        except Refused:
            return "30"
        self._change(("ovw", k, rec))
        return "00"

    def close(self) -> str:
        if self.mode is None:
            return "42"
        self.mode, self.last_read, self.absent_input = None, None, False
        if not self.covered or self.txn is None:
            self.h.handles_on(self.F).discard(self)
            if not self.covered:
                for key in [key for key, h in self.h.region.items() if h is self]:
                    del self.h.region[key]
        return "00"

    def delete_file(self) -> str:
        if self.mode is not None:
            return "41"
        self._take_mutex()
        try:
            self._entry_step()
            st = self._holding_test("DELETE", exclusive=True)
            if st is not None:
                return st
            if not self.m.exists(self.F):
                return "35"
            if self.covered:
                if self.txn is None:
                    self._begin("P")
                if self.txn.phase == "BASE":
                    self._capture("D")
                self.h.handles_on(self.F).discard(self)
                if self.h.append.get(self.F) is self:
                    del self.h.append[self.F]
            self.m.delete(self.F)
            self.m.syncdir(self.Fdir)
            self._change(("delete",))
            return "00"
        except Refused:
            return "30"
        finally:
            self._drop_mutex()

    def _change(self, change):
        if self.covered:
            self.changes.append(change)
        else:
            self.oracle.plain(change)

    def commit(self) -> str:
        if self.marked:
            return self.rollback()
        if self.rollback_owed:
            return self.rollback()
        if self.txn is None:
            if self.mode is None:
                self.h.handles_on(self.F).discard(self)
            return "00"
        t = self.txn.id
        j = self.J(t)
        handed = False
        try:
            if self.m.exists(self.F):
                self.m.sync(self.F)
            self.m.syncdir(self.Fdir)
            self.m.write(j, ("D", "C"))
            handed = True
            self.oracle.handed(self, self.changes)
            self.m.sync(j)
        except Refused:
            if not handed:
                self._owner_rollback()                # region 1: the transaction is intact
                return "30"
            try:                                      # region 3: reverse, then roll back
                self.m.write(j, ("D", "O"))
                self.m.sync(j)
            except Refused:
                self.marked = True                    # the medium decides; nothing of F is touched
                return "30"
            self.oracle.reversed(self)
            self._owner_rollback()
            return "30"
        self.oracle.commit(self, self.changes)        # region 2
        try:
            self._retire(t)
        except Refused:
            pass                                      # completed by the next entry
        self._end_transaction()
        return "00"

    def _owner_rollback(self) -> bool:
        t = self.txn.id
        self._take_mutex()
        try:
            self._settle(t)
        except Refused:
            self.rollback_owed = True
            return False
        finally:
            self._drop_mutex()
        self._end_transaction()
        if not self.m.exists(self.F):
            # the state at the last COMMIT had no file: the file lock went with it (GR3), the open mode is kept
            self.h.handles_on(self.F).discard(self)
            self.last_read = None
        return True

    def rollback(self) -> str:
        if self.marked:
            j = self.J(self.txn.id)
            try:
                self.m.write(j, ("D", "O"))
                self.m.sync(j)
            except Refused:
                return "30"
            self.marked = False
            self.oracle.reversed(self)
        if self.txn is None:
            if self.mode is None:
                self.h.handles_on(self.F).discard(self)
            return "00"
        return "00" if self._owner_rollback() else "30"

    def sweep(self):
        """The next entry: a writable OPEN's entry step, and nothing else."""
        self._take_mutex()
        try:
            self._entry_step()
        finally:
            self._drop_mutex()


# ----------------------------------------------------------------------------------------------------------------------
# Invariant checks raised from inside the protocol (I2, I4)
# ----------------------------------------------------------------------------------------------------------------------
class Violation(AssertionError):
    pass


class Checks:
    def no_foreign_lock(self, who: RunUnit, k: int):
        holder = who.h.region.get((who.F, k))
        if holder is not None and holder is not who:
            raise Violation(f"I2: rollback by {who} writes ordinal {k} while {holder} holds its lock")

    def no_foreign_handle(self, who: RunUnit):
        others = who.h.handles_on(who.F) - {who}
        if others:
            raise Violation(f"I4: {who} captures or deletes F while {sorted(map(str, others))} hold it open")


CHECKS = Checks()


# ----------------------------------------------------------------------------------------------------------------------
# Programs
# ----------------------------------------------------------------------------------------------------------------------
STATEMENTS = ("OO", "OE", "OI", "ON", "W", "R", "RW", "CL", "DF", "CM", "RB")


def run_statement(u: RunUnit, stmt: tuple) -> str:
    op = stmt[0]
    if op == "OO":
        return u.open("OUTPUT")
    if op == "OE":
        return u.open("EXTEND")
    if op == "OI":
        return u.open("IO")
    if op == "ON":
        return u.open("INPUT")
    if op == "W":
        return u.write(stmt[1])
    if op == "R":
        return u.read()
    if op == "RW":
        return u.rewrite(stmt[1])
    if op == "CL":
        return u.close()
    if op == "DF":
        return u.delete_file()
    if op == "CM":
        return u.commit()
    if op == "RB":
        return u.rollback()
    raise ValueError(op)


def legal_programs(depth: int, prefix: str):
    """Every statement sequence of at most `depth` statements the connector's mode machine allows."""
    def step(seq, mode, can_rewrite, nw, nr):
        yield seq
        if len(seq) == depth:
            return
        if mode is None:
            nexts = [("OO",), ("OE",), ("OI",), ("ON",), ("DF",), ("CM",), ("RB",)]
        elif mode in ("OUTPUT", "EXTEND"):
            nexts = [("W", f"{prefix}W{nw}"), ("CL",), ("CM",), ("RB",)]
        elif mode == "INPUT":
            nexts = [("R",), ("CL",), ("CM",), ("RB",)]
        else:
            nexts = [("R",), ("CL",), ("CM",), ("RB",)] + ([("RW", f"{prefix}Y{nr}")] if can_rewrite else [])
        for s in nexts:
            if seq and seq[-1][0] in ("CM", "RB") and s[0] in ("CM", "RB"):
                continue
            m, cr, w, r = mode, can_rewrite, nw, nr
            if s[0] == "OO":
                m = "OUTPUT"
            elif s[0] == "OE":
                m = "EXTEND"
            elif s[0] == "OI":
                m, cr = "IO", False
            elif s[0] == "ON":
                m = "INPUT"
            elif s[0] == "CL":
                m, cr = None, False
            elif s[0] == "W":
                w += 1
            elif s[0] == "R":
                cr = True
            elif s[0] == "RW":
                r += 1
                cr = False
            yield from step(seq + [s], m, cr, w, r)
    yield from step([], None, False, 1, 1)


B_PROGRAMS = {
    "append":   [("OE",), ("W", "B1"), ("CL",)],
    "rewrite":  [("OI",), ("R",), ("RW", "BY1"), ("CL",)],
    "rewrite2": [("OI",), ("R",), ("R",), ("RW", "BY2"), ("CL",)],
    "truncate": [("OO",), ("W", "B1"), ("CL",)],
    "delete":   [("DF",)],
    "read":     [("ON",), ("R",), ("CL",)],
}
# LATER: B's statements after it run once A's program has ended (B keeps its transaction open across the rest of A's);
# DIE: B's run unit dies there (its locks and handles go; its journal is settled by the next entry).
LATER, DIE = ("...",), ("DIE",)
B_COVERED_PROGRAMS = {
    "append-commit":  [("OE",), ("W", "B1"), ("CL",), ("CM",)],
    "rewrite-commit": [("OI",), ("R",), ("RW", "BY1"), ("CL",), ("CM",)],
    "append-roll":    [("OE",), ("W", "B1"), ("CL",), ("RB",)],
    "rewrite-roll":   [("OI",), ("R",), ("RW", "BY1"), ("CL",), ("RB",)],
    "append-open":    [("OE",), ("W", "B1"), LATER],                                  # ends normally: implicit COMMIT
    "append-open-roll": [("OE",), ("W", "B1"), ("CL",), LATER, ("RB",)],
    "append-open-dies": [("OE",), ("W", "B1"), LATER, DIE],
    "rewrite-open":   [("OI",), ("R",), ("RW", "BY1"), ("CL",), LATER],
    "rewrite-open-roll": [("OI",), ("R",), ("RW", "BY1"), ("CL",), LATER, ("RB",)],   # R2-1: B's L counts A's appends
    "rewrite-open-dies": [("OI",), ("R",), ("RW", "BY1"), ("CL",), LATER, DIE],
    "rewrite-open-append-roll": [("OI",), ("R",), ("RW", "BY1"), ("CL",), LATER, ("OE",), ("W", "B2"), ("RB",)],  # L record
    "rewrite-open-append-commit": [("OI",), ("R",), ("RW", "BY1"), ("CL",), LATER, ("OE",), ("W", "B2"), ("CM",)],
}

F, D = "data/F", "data/.wolk-F"


# ----------------------------------------------------------------------------------------------------------------------
# One run of a scenario
# ----------------------------------------------------------------------------------------------------------------------
@dataclass
class Scenario:
    initial: object                   # ABSENT or a tuple of records
    optional: bool
    program: list
    b_program: list | None = None
    b_covered: bool = False
    b_at: int = 0                     # the interleaving point of A (a medium operation or a statement boundary) at which B's program runs
    fault_at: int = 0
    mode: str | None = None
    die_after_b: bool = False
    keep: dict | None = None          # the power-loss prefix choice
    mutation: str | None = None       # one of MUTATIONS applied to every run unit's protocol (the watchdog's proof)

    def title(self) -> str:
        prog = " ".join(s[0] + ("(" + s[1] + ")" if len(s) > 1 else "") for s in self.program)
        bits = [f"F={'absent' if self.initial == ABSENT else ','.join(self.initial)}",
                "OPTIONAL" if self.optional else "required", prog]
        if self.b_program:
            bits.append(f"B[{'covered' if self.b_covered else 'plain'}]@{self.b_at}=" +
                        " ".join(s[0] for s in self.b_program))
        if self.mode:
            bits.append(f"{self.mode}@{self.fault_at}" + (f" keep={self.keep}" if self.keep else ""))
        if self.die_after_b:
            bits.append("A dies after B")
        if self.mutation:
            bits.append(f"mutation={self.mutation}")
        return " · ".join(bits)


class Outcome:
    __slots__ = ("ops", "points", "statuses", "b_statuses", "pending")

    def __init__(self):
        self.ops, self.points, self.statuses, self.b_statuses, self.pending = 0, 0, [], [], []


def execute(sc: Scenario) -> Outcome:
    out = Outcome()
    h = Host()
    oracle = Oracle(list(sc.initial) if sc.initial != ABSENT else ABSENT)
    A_holder: list = []

    bs: list = []

    def run_b(b: RunUnit, stmts: list) -> bool:
        """Run B's statements; False when B's run unit died (DIE) or a fatal status ended its program."""
        for stmt in stmts:
            if stmt == DIE:
                h.kill(b)
                return False
            st = run_statement(b, stmt)
            out.b_statuses.append(st)
            if st == "30":
                b.rollback()
                return False
        return True

    def b_hook():
        if not sc.b_program:
            return
        b = RunUnit("B", m, h, oracle, F, D, sc.b_covered, sc.optional, sc.mutation)
        now = sc.b_program[: sc.b_program.index(LATER)] if LATER in sc.b_program else sc.b_program
        later = sc.b_program[len(now) + 1:] if LATER in sc.b_program else []
        try:
            if run_b(b, now) and (b.txn is not None or later):
                bs.append((b, later))                  # B lives on beside A: its transaction open, its program not done
            else:
                h.kill(b)                              # B's run unit ends
        except Blocked:
            out.b_statuses.append("BLOCKED")
            h.kill(b)

    fault = Fault(target=sc.b_at if sc.b_program else sc.fault_at,
                  mode=None if sc.b_program else sc.mode,
                  hook=b_hook if sc.b_program else None,
                  die_after_hook=sc.die_after_b)
    m = Medium(fault)
    if sc.initial != ABSENT:
        f = m.files[F] = FileImage()
        f.exists_durable = True
        f.durable = [("app", r) for r in sc.initial]
    a = RunUnit("A", m, h, oracle, F, D, True, sc.optional, sc.mutation)
    A_holder.append(a)
    died = power = False
    try:
        fatal = False
        for stmt in sc.program:
            fault.boundary()                           # the point before the statement: B may begin here (R3-1)
            st = run_statement(a, stmt)
            out.statuses.append(st)
            if st == "30":
                # a fatal I-O condition on a file under APPLY COMMIT: item 8 executes a ROLLBACK unconditionally
                # (§14.6.13.1.3), and the run unit terminates abnormally, whose path retries an owed rollback once
                # (§14.6.12) and then exits — its locks and handles go, its cached writes stay
                fault.boundary()
                a.rollback()
                if a.rollback_owed or a.marked:
                    a.rollback()
                fatal = True
                break
        if not fatal:
            fault.boundary()
            a.commit()                                 # normal termination: the implicit COMMIT (§14.6.11)
        h.kill(a)                                      # the run unit ends: its locks and handles go
    except Died:
        died = True
        h.kill(a)
    except PowerLoss:
        power = True
        out.pending = m.pending_shape()
        if sc.keep is None:
            return out                                 # the driver enumerates the prefixes and runs again
        m.power_loss(sc.keep)
        h = Host()
    except Blocked:
        raise AssertionError("A found the mutex held with no one else running")
    out.ops, out.points = m.ops, fault.points
    fault.die_after_hook = False                       # A is gone; a death armed for its next operation is moot
    for b, later in bs:                                # a B left alive runs the rest of its program, then ends normally
        if power:
            continue                                   # the power loss took B's run unit with A's
        if run_b(b, later):
            b.commit()                                 # normal termination: the implicit COMMIT
            h.kill(b)
    # the next entry
    fault.mode = None
    e = RunUnit("E", m, h, oracle, F, D, False, sc.optional, sc.mutation)
    e.sweep()
    actual = records(m.view(F)) if m.exists(F) else ABSENT
    litter = [p[len(D) + 1:] for p in m.listdir(D) if "mutex" not in p]
    if litter:
        raise Violation(f"I3: left behind {litter}")
    if oracle.post is not None:
        if actual != oracle.committed and actual != oracle.post:
            raise Violation(f"I1: F={actual}, neither the committed {oracle.committed} nor the unacknowledged {oracle.post}")
    elif actual != oracle.committed:
        raise Violation(f"I1: F={actual} but the state at the last COMMIT is {oracle.committed}")
    return out


def run(sc: Scenario) -> Outcome:
    """execute, naming the scenario in any failure (a Violation keeps its class; a crash of the model itself too)."""
    try:
        return execute(sc)
    except Violation as v:
        raise Violation(f"{v} [{sc.title()}]") from None
    except Exception as e:
        raise RuntimeError(f"the model itself failed: {type(e).__name__}: {e} [{sc.title()}]") from e


def sweep_scenario(base: Scenario) -> int:
    """Run the fault-free scenario, then every fault at every operation (and every power-loss prefix)."""
    count = 0
    plain = run(base)
    count += 1
    for i in range(1, plain.ops + 1):
        for mode in (REFUSE, DIE, POWER):
            sc = Scenario(**{**base.__dict__, "fault_at": i, "mode": mode})
            o = run(sc)
            count += 1
            if mode == POWER and o.pending:
                shapes = [range(n + 1) for _, n in o.pending]
                for choice in itertools.product(*shapes):
                    keep = {p: c for (p, _), c in zip(o.pending, choice)}
                    sc2 = Scenario(**{**sc.__dict__, "keep": keep})
                    run(sc2)
                    count += 1
    return count


def sweep_interleavings(base: Scenario, b_prog: list, b_covered: bool) -> int:
    """B's program at every interleaving point of A's: before every medium operation and before every statement (R3-1),
    with A continuing, and with A dying at its next point."""
    count = 0
    plain = run(base)
    for i in range(1, plain.points + 1):
        for die_after in (False, True):
            sc = Scenario(**{**base.__dict__, "b_program": b_prog, "b_covered": b_covered, "b_at": i,
                             "die_after_b": die_after})
            run(sc)
            count += 1
    return count


# ----------------------------------------------------------------------------------------------------------------------
# The named rows of 2.2.2.12 (each also lies inside the sweep; named so the table and the model agree by name)
# ----------------------------------------------------------------------------------------------------------------------
ROWS = [
    ("R1-1 capture then REWRITE", ("C1", "C2", "C3"), False,
     [("OO",), ("W", "X1"), ("W", "X2"), ("CL",), ("OI",), ("R",), ("RW", "Y1"), ("RB",)]),
    ("R1-1 append then capture", ("C1", "C2"), False,
     [("OE",), ("W", "X1"), ("CL",), ("OO",), ("W", "X2"), ("RB",)]),
    ("REWRITE then capture", ("C1", "C2"), False,
     [("OI",), ("R",), ("RW", "Y1"), ("CL",), ("OO",), ("W", "X1"), ("RB",)]),
    ("R1-2 OPTIONAL created by OPEN EXTEND", ABSENT, True,
     [("OE",), ("W", "X1"), ("RB",), ("CL",), ("ON",)]),
    ("R1-2 created by OPEN OUTPUT", ABSENT, False,
     [("OO",), ("W", "X1"), ("RB",), ("CL",), ("ON",)]),
    ("DELETE FILE then re-create then ROLLBACK", ("C1", "C2"), False,
     [("DF",), ("OO",), ("W", "X1"), ("CL",), ("RB",), ("ON",)]),
    ("DELETE FILE then COMMIT", ("C1", "C2"), False, [("DF",), ("CM",), ("ON",)]),
    ("create, DELETE FILE, re-create, COMMIT", ABSENT, False,
     [("OO",), ("W", "X1"), ("CL",), ("DF",), ("OO",), ("W", "X2"), ("CM",)]),
    ("first WRITE under EXTEND", ("C1", "C2"), False, [("OE",), ("W", "X1"), ("CM",)]),
    ("REWRITE twice, ROLLBACK", ("C1", "C2"), False,
     [("OI",), ("R",), ("RW", "Y1"), ("RW", "Y2"), ("RB",)]),
    ("COMMIT then more, ROLLBACK", ("C1",), False,
     [("OE",), ("W", "X1"), ("CM",), ("W", "X2"), ("RB",)]),
    ("CLOSE under commit, then COMMIT", ("C1",), False, [("OE",), ("W", "X1"), ("CL",), ("CM",)]),
    # R2-1 lives in these two rows' interleavings, one arm each: B "rewrite-open-roll" begins while A's append X1 is live
    # — at the boundary before A's CLOSE or its ROLLBACK, the points R3-1 added — and rolls back after A's ROLLBACK
    # shortened F (an L read at the begin would be 3: the extension with zero bytes); or begins in flight, between A's
    # begin and its write, and rolls back after A's COMMIT kept X1 (that L would be 2: the cut). "rewrite-open-append-
    # roll" appends into the end A's ROLLBACK vacated and rolls back.
    ("R2-1 a transaction begun beside another's append, undone", ("C1", "C2"), False,
     [("OE",), ("W", "X1"), ("CL",), ("RB",)]),
    ("R2-1 a transaction begun beside another's append, committed", ("C1", "C2"), False,
     [("OE",), ("W", "X1"), ("CL",), ("CM",)]),
]

_unknown = [w for _, ws in MUTATIONS.values() for w in ws if w not in {r[0] for r in ROWS}]
if _unknown:
    raise ValueError(f"MUTATIONS name witness rows that are not in ROWS: {_unknown}")


def sweep_with_b(base: Scenario) -> int:
    """The base scenario under every fault, then every second-run-unit program at every boundary (fault-free)."""
    n = sweep_scenario(base)
    for b_prog in B_PROGRAMS.values():
        n += sweep_interleavings(base, b_prog, False)
    for b_prog in B_COVERED_PROGRAMS.values():
        n += sweep_interleavings(base, b_prog, True)
    return n


def run_rows(verbose: bool, mutation: str | None = None, stop_at_first: bool = False) -> tuple[int, int, str | None]:
    """Returns (runs, failures, the first violation)."""
    runs = failures = 0
    first = None
    for name, initial, optional, prog in ROWS:
        base = Scenario(initial=initial, optional=optional, program=prog, mutation=mutation)
        try:
            n = sweep_with_b(base)
            runs += n
            if verbose:
                print(f"OK    {name}: {n} runs")
        except Violation as v:
            failures += 1
            first = first or f"{name}: {v}"
            if verbose:
                print(f"FAIL  {name}: {v}")
            if stop_at_first:
                break
    return runs, failures, first


def run_sweep(depth: int, b_depth: int, verbose: bool, mutation: str | None = None,
              stop_at_first: bool = False) -> tuple[int, int, str | None]:
    """Every legal program of up to `depth` statements; returns (runs, failures, the first violation)."""
    runs = failures = 0
    first = None
    programs = list(legal_programs(depth, "A"))
    for initial in (ABSENT, ("C1", "C2")):
        for optional in (False, True):
            if initial != ABSENT and optional:
                continue                               # OPTIONAL changes nothing for a present file
            for prog in programs:
                if not prog:
                    continue
                base = Scenario(initial=initial, optional=optional, program=prog, mutation=mutation)
                try:
                    runs += sweep_with_b(base) if len(prog) <= b_depth else sweep_scenario(base)
                except Violation as v:
                    failures += 1
                    first = first or str(v)
                    if verbose:
                        print(f"FAIL  {base.title()}: {v}")
                    if stop_at_first or failures > 20:
                        return runs, failures, first
    return runs, failures, first


def prove_mutations(verbose: bool) -> int:
    """The watchdog proves itself (review round 2, R2-2; round 3, R3-1): with each named mutation applied, every row it
    names as its witness must turn RED on its own. A RED from some other row proves nothing about the row the design names
    for the defect (R3-1: the R2-1 row was GREEN under its own mutation while the arm passed on the twin other rows
    reached). Returns the number of (mutation, witness) pairs the model failed to catch."""
    rows = {name: (initial, optional, prog) for name, initial, optional, prog in ROWS}
    dead = 0
    for name, (what, witnesses) in MUTATIONS.items():
        for row in witnesses:
            initial, optional, prog = rows[row]
            t0 = time.time()
            try:
                runs = sweep_with_b(Scenario(initial=initial, optional=optional, program=prog, mutation=name))
            except Violation as v:
                if verbose:
                    print(f"RED   mutation {name} ({what}) on row '{row}' in {time.time() - t0:.1f}s: {v}")
                continue
            dead += 1
            print(f"FAIL  mutation {name} ({what}) left its witness row '{row}' GREEN over {runs} runs: the watchdog is dead")
    return dead


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    ap.add_argument("--self-test", action="store_true", help="the bounded sweep every gate runs")
    ap.add_argument("--depth", type=int, default=0, help="sweep every program of up to this many statements")
    ap.add_argument("--b-depth", type=int, default=3, help="interleave the second run unit with programs up to this length")
    ap.add_argument("--rows", action="store_true", help="run the named 2.2.2.12 rows and print each")
    ap.add_argument("--mutations", action="store_true",
                    help="run only the watchdog's proof: every named mutation must be RED on each row it names")
    ap.add_argument("--mutation", choices=sorted(MUTATIONS), help="apply one named mutation to the rows or the sweep (by hand)")
    args = ap.parse_args(argv)
    t0 = time.time()
    if args.mutations:
        failures = prove_mutations(verbose=True)
    elif args.self_test or args.rows:
        runs, failures, _ = run_rows(verbose=True, mutation=args.mutation)
        if args.self_test:
            r2, f2, _ = run_sweep(4, 3, verbose=False, mutation=args.mutation)
            runs += r2
            failures += f2
            print(f"sweep: depth 4, {runs} runs with the rows")
            if args.mutation is None:
                failures += prove_mutations(verbose=True)
    elif args.depth:
        runs, failures, _ = run_sweep(args.depth, args.b_depth, verbose=True, mutation=args.mutation)
        print(f"sweep: depth {args.depth}, {runs} runs")
    else:
        ap.print_help()
        return 2
    verdict = "GREEN" if failures == 0 else f"RED ({failures} violations)"
    print(f"=== SEQUENTIAL JOURNAL MODEL: {verdict} in {time.time() - t0:.1f}s ===")
    return 0 if failures == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
