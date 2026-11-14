"""inert_watch.py — THE INOTIFY WATCH OF A WHOLE-POPULATION TEST RUN (kb/Work PB5398, PB2878, PB4395; docs/rearchitecture/DESIGN-orchestrator-loop.md section 16.12): `watch` runs a command and reports any open of a path the classification (`lane_classification.py`) says no test reads, so the day a test starts reading an inert or tooling-lane path the change that made it read it is red. Linux only (inotify through ctypes: no package to install on a CI runner or in WSL); `inert_lane.py watch` is its command."""
from __future__ import annotations

import os
import select
import shutil
import struct
import subprocess
import sys
import tempfile
from pathlib import Path
from lane_classification import CLASSIFICATION, Classification

IN_OPEN = 0x00000020


IN_Q_OVERFLOW = 0x00004000


IN_ISDIR = 0x40000000


EVENT = struct.Struct("iIII")


WATCH_VERDICT = "=== INERT WATCH: "


class Watch:
    """An inotify watch of `dirs` under `root`, recording every OPEN of a path `c` calls inert (`hits`) and any queue
    overflow (`overflow`), which leaves the watch unable to say nothing was read."""

    def __init__(self, root: Path, dirs: list[str], c: Classification):
        import ctypes
        import ctypes.util
        self.c, self.hits, self.overflow = c, set(), False
        self.libc = ctypes.CDLL(ctypes.util.find_library("c") or "libc.so.6", use_errno=True)
        self.fd = self.libc.inotify_init1(os.O_NONBLOCK | os.O_CLOEXEC)
        if self.fd < 0:
            raise OSError(ctypes.get_errno(), "inotify_init1 failed")
        self.wd: dict[int, str] = {}
        for d in dirs:
            w = self.libc.inotify_add_watch(self.fd, str(root / d).encode(), IN_OPEN)
            if w < 0:
                os.close(self.fd)
                raise OSError(ctypes.get_errno(), f"inotify_add_watch failed on {d} (raise fs.inotify.max_user_watches?)")
            self.wd[w] = d

    def drain(self, timeout: float) -> None:
        while select.select([self.fd], [], [], timeout)[0]:
            try:
                buf = os.read(self.fd, 1 << 16)
            except BlockingIOError:
                return
            off = 0
            while off + EVENT.size <= len(buf):
                wd, mask, _cookie, length = EVENT.unpack_from(buf, off)
                name = buf[off + EVENT.size: off + EVENT.size + length].split(b"\0", 1)[0].decode("utf-8", "replace")
                off += EVENT.size + length
                if mask & IN_Q_OVERFLOW:
                    self.overflow = True
                    continue
                d = self.wd.get(wd)
                if d is None:
                    continue
                path = "/".join(x for x in (d, name) if x) + ("/" if mask & IN_ISDIR else "")
                if self.c.is_watched(path):
                    self.hits.add(path)
            timeout = 0

    def close(self) -> None:
        os.close(self.fd)


def watch(root: Path, command: list[str], events: Path | None, say=print, c: Classification = CLASSIFICATION) -> int:
    """Run `command` under a watch of the inert and the tooling-lane paths (`Classification.is_watched`): its exit code
    when non-zero, else 1 when it opened a watched path or the watch overflowed, else 0. The verdict line names what was
    read; `events` (written empty once the watch is installed: the readiness mark a caller waiting on it reads) receives
    every path read, one per line. The command runs with a fresh Python bytecode cache (module docstring)."""
    if not sys.platform.startswith("linux"):
        say(f"{WATCH_VERDICT}NOT RUN — inotify is Linux's; this host is {sys.platform} ===")
        return 2
    dirs = c.watch_dirs(root)
    w = Watch(root, dirs, c)
    say(f"inert watch: {len(dirs)} director(ies) under {root} watched while `{' '.join(command)}` runs")
    if events is not None:
        events.write_text("", encoding="utf-8", newline="\n")
    pycache = tempfile.mkdtemp(prefix="inert-watch-pycache-")
    try:
        child = subprocess.Popen(command, env={**os.environ, "PYTHONPYCACHEPREFIX": pycache})
        try:
            while child.poll() is None:
                w.drain(0.5)
            w.drain(0.2)
        finally:
            if child.poll() is None:
                child.kill()
                child.wait()
    finally:
        w.close()
        shutil.rmtree(pycache, ignore_errors=True)
    rc = child.returncode
    hits = sorted(w.hits)
    if events is not None:
        events.write_text("".join(f"{h}\n" for h in hits) + ("OVERFLOW\n" if w.overflow else ""), encoding="utf-8",
                          newline="\n")
    if w.overflow:
        say(f"{WATCH_VERDICT}RED — the inotify queue overflowed, so the watch cannot say no inert path was read "
            "(raise fs.inotify.max_queued_events) ===")
        return rc or 1
    if hits:
        for h in hits[:40]:
            say(f"    opened: {h}")
        if len(hits) > 40:
            say(f"    … and {len(hits) - 40} more{f' (all in {events})' if events else ''}")
        inert = sum(1 for h in hits if c.is_inert(h))
        say(f"{WATCH_VERDICT}RED — {len(hits)} watched path(s) were opened while the tests ran ({inert} inert, "
            f"{len(hits) - inert} in the tooling lane), so a lane that runs no compiler test would skip a test that reads "
            "them: give an inert one a READ entry and a tooling one a CODE_READS entry in scripts/lane_classification.py naming "
            "its reader, or make the reader an audit or a script's self-test (kb/Work PB2878, PB4395) ===")
        return rc or 1
    say(f"{WATCH_VERDICT}GREEN — no inert or tooling-lane path was opened ({len(dirs)} director(ies) watched; command "
        f"exit {rc}) ===")
    return rc
