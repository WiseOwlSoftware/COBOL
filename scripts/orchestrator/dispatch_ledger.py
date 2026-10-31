"""THE DISPATCH LEDGER (`<coord>/inflight-groups.json`, kb/Work PB3728; docs/rearchitecture/DESIGN-orchestrator-loop.md
section 16.3; kb/Work PB3186): its one writer (record_dispatch, an upsert on wave and letter that keeps a re-rendered
group's first `at`, kb/Work PB3392), the hold it puts on the notes of a dispatch no report has answered
(held_by_dispatch, kb/Work PB2806, PB3311), the release of an aborted dispatch (release_dispatch), and the partition's
measurement of every dispatched wave (partition_record, record_partition, partition_summary; kb/Work PB3670 item 5).
The ledger's READER is coord.ledger_groups (the coordination directory's), which the report reader needs below this
module, to complete a report's notes (reports.answered_notes).

Its writers' callers: the wave planner (plan_wave.py) and the spec renderer (make_dispatch_specs.py records every
group it renders); nothing else writes the ledger. In the import DAG of section 16.10 (scripts/script_layering.py) it
imports reports, inflight and groups.
"""
from __future__ import annotations

import json
import pathlib
import re
import sys
import time
from typing import Any, Callable, Mapping, Sequence

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
sys.path.insert(0, str(HERE.parent))
import coord  # noqa: E402
from reports import Report, answers, ledger_slug  # noqa: E402
from inflight import Claim  # noqa: E402
from groups import Group  # noqa: E402

def record_dispatch(coord_dir: pathlib.Path, wave: str, groups: list[dict[str, Any]],
                    terminal: Callable[[str], bool], lane: str | None = None) -> list[dict[str, Any]]:
    """THE writer of the dispatch ledger (kb/Work PB3186): UPSERT one wave's groups, keyed on (wave, letter), each
    `{"letter", "notes": [ids], "files": [repo-relative paths], "members"?: {path: [member names]}, "slug"?}` (a file
    with no `members` entry is claimed whole, kb/Work PB3670), and return the entries written. A group
    recorded again (a re-rendered spec) replaces its notes, files and slug but KEEPS its `at`, the time of its FIRST
    recording (kb/Work PB3392): `at` is the dispatch order the landing check reads (the later of two overlapping
    branches re-plans) and the time `answers` bounds a report by, and a re-render is not a new dispatch (the Mythos
    lane's re-render of wave 1118's specs at 16:42, a wave dispatched about 05:00, had made the engine the LATER
    dispatch than wave 1126 C, 14:02). A group recorded under a key whose entry shares NO note with it is a DIFFERENT
    dispatch (two sessions planning one wave number) and takes the time of its own recording. Every other group of
    every wave stays, except the entries whose notes are all terminal and the entries coord.ledger_groups does not read (the retired hand entries), which are
    dropped, so the ledger holds exactly the dispatched work still in flight (inflight_file_sets, source 3).
    `lane` is the mailbox lane that dispatched the wave (mailbox.account_lane: `operator` or `mythos`; None records
    none): the stall reporter sends a stalled group's message to that lane's inbox (progress.post_stalls, kb/Work
    PB4190). Like `at`, a group recorded again keeps the lane of its FIRST recording, so one lane's re-render of the
    other's specs does not take the group over.
    make_dispatch_specs.py calls it for every wave it renders, the planner's included; nothing else writes the
    ledger but release_dispatch."""
    path = coord_dir / coord.DISPATCH_LEDGER
    mine = {(str(wave), str(g["letter"]).upper()) for g in groups}
    live = [g for g in coord.ledger_groups(coord_dir) if not all(terminal(i) for i in g.get("notes", []))]
    keep = [g for g in live if (str(g["wave"]), g["letter"].upper()) not in mine]
    earlier = {(str(g["wave"]), g["letter"].upper()): g for g in live if (str(g["wave"]), g["letter"].upper()) in mine}
    at = time.time()   # the dispatch order the landing check reads (the later of two overlapping branches re-plans):
    #   one millisecond apart in letter order, so two groups of one wave never tie (a tie left neither "later" and both
    #   proceeded with a warning: the ninth refuter's N4)

    def stamp(k: int, g: dict[str, Any]) -> float:
        """The group's dispatch time: its entry's own when this is the same group recorded again (it shares a note
        with the entry under its key, kb/Work PB3392), else this recording's."""
        was = earlier.get((str(wave), str(g["letter"]).upper()))
        if was is not None and isinstance(was.get("at"), (int, float)) and set(was.get("notes", [])) & set(g["notes"]):
            return was["at"]
        return at + k / 1000

    def lane_of(g: dict[str, Any]) -> str | None:
        """The group's dispatching lane: its entry's own when this is the same group recorded again, else `lane`."""
        was = earlier.get((str(wave), str(g["letter"]).upper()))
        if was is not None and was.get("lane") and set(was.get("notes", [])) & set(g["notes"]):
            return was["lane"]
        return lane

    new = [{"wave": str(wave), "letter": str(g["letter"]), "slug": g.get("slug") or f"w{wave}{str(g['letter']).lower()}",
            "notes": list(g["notes"]), "files": sorted(claim.files), "members": claim.member_lists(), "at": stamp(k, g),
            **({"lane": lg} if (lg := lane_of(g)) else {})}
           for k, g in enumerate(groups) for claim in [Claim.of(g["files"], g.get("members"))]]
    coord.write_json(path, {"groups": keep + new})
    return new


def held_by_dispatch(coord_dir: pathlib.Path, reports: list[Report], terminal: Callable[[str], bool]) -> dict[str, str]:
    """{open note: the dispatch holding it}: every note a dispatch-ledger group names (coord.ledger_groups, the entries
    inflight_file_sets reads as source 3) unless the note is terminal or the group is answered (`answers`: its own
    report, by slug, written at or after the group's `at`; the predicate inflight_file_sets reads too, kb/Work
    PB3311). Such a report answers the dispatch, and step 1 (awaiting landing) and step 2
    (finishers) govern its notes from then on; an unanswered group is still running, or was planned and not launched
    (another session's wave waiting for the operator's GO), so its notes are not planned again (kb/Work PB2806: the
    fix lane read no ledger, and wave 1044 re-planned seven of wave 1045's eight groups)."""
    held: dict[str, str] = {}
    for g in coord.ledger_groups(coord_dir):
        if any(answers(r, g) for r in reports):
            continue
        for nid in sorted(i for i in set(g.get("notes", [])) if not terminal(i)):
            held.setdefault(nid, f"dispatch {ledger_slug(g)}")
    return held


def release_dispatch(coord_dir: pathlib.Path, selector: str) -> list[dict[str, Any]]:
    """Drop the dispatch-ledger entries `selector` names (a wave `1040`, a group `1040a`, or a note id `PB2249`) and
    return them. A wave entry never expires on a clock (an expired entry would release the files of a wave still
    running), so an ABORTED dispatch is released here, by hand, explicitly (the ninth refuter's N5). The rewrite keeps
    only what coord.ledger_groups reads."""
    path = coord_dir / coord.DISPATCH_LEDGER
    groups = coord.ledger_groups(coord_dir)
    if re.fullmatch(r"PB\d+", selector):
        hit = lambda g: selector in g.get("notes", [])  # noqa: E731
    else:
        hit = lambda g: selector.lower() in (str(g["wave"]), f"{g['wave']}{g['letter']}".lower())  # noqa: E731
    gone = [g for g in groups if hit(g)]
    coord.write_json(path, {"groups": [g for g in groups if not hit(g)]})
    return gone


#: The partition's measurement (kb/Work PB3670 item 5): one JSON line per wave a plan dispatches, in the coordination
#: directory, read by `--partition-summary`.
PARTITION_MEASURE = "partition-measure.jsonl"
COLLISION_WAIT = "file-set collision with in-flight work "


def partition_record(wave: str, groups: Sequence[Group], waiting: Mapping[str, str],
                     collided: Mapping[str, Sequence[str]], at: float) -> dict[str, Any]:
    """One wave's partition measurement (kb/Work PB3670 item 5): how many groups it planned, the pairs of them that
    share a primary file and run side by side (different members: the case the member partition made possible), the
    notes a collision with in-flight work held and each one's reason (`held_by_collision`, prose for a person), WHAT
    each shared (`collided_on`: the files and `file#member`s of plan()'s `collided`, one per in-flight owner met, kept as
    data so no reader parses the prose; kb/Work PB3695), and the notes the in-wave rule held (blockers or claim
    meetings in two chains)."""
    by_file: dict[str, list[Group]] = {}
    for g in groups:
        if g.file:
            by_file.setdefault(g.file, []).append(g)
    parallel = [f"{a.letter}+{b.letter} on {f}" for f, on in sorted(by_file.items()) for k, a in enumerate(on)
                for b in on[k + 1:] if not (a.after or b.after)]
    return {"at": at, "wave": wave, "groups": len(groups), "same_file_parallel": parallel,
            "held_by_collision": {i: why[len(COLLISION_WAIT):] for i, why in waiting.items() if why.startswith(COLLISION_WAIT)},
            "collided_on": {i: list(hits) for i, hits in collided.items()},
            "held_in_wave": sorted(i for i, why in waiting.items() if "different successor chains" in why)}


def record_partition(coord_dir: pathlib.Path, record: Mapping[str, Any]) -> None:
    """Append one wave's partition record to <coord>/PARTITION_MEASURE."""
    with (coord_dir / PARTITION_MEASURE).open("a", encoding="utf-8", newline=chr(10)) as f:
        f.write(json.dumps(record, ensure_ascii=False, sort_keys=True) + chr(10))


def partition_summary(coord_dir: pathlib.Path) -> list[str]:
    """Per day (UTC) of recorded waves: waves, groups planned, same-file pairs run side by side, notes held by a
    collision with in-flight work, and the files those collisions named most (kb/Work PB3670 item 5: compare the day
    after the member partition with the day before; the landings' conflicts and their causes are landing_measure.py's
    bounce causes)."""
    path = coord_dir / PARTITION_MEASURE
    days: dict[str, dict[str, Any]] = {}
    for line in (path.read_text(encoding="utf-8").splitlines() if path.is_file() else []):
        try:
            r = json.loads(line)
        except ValueError:
            continue
        d = days.setdefault(time.strftime("%Y-%m-%d", time.gmtime(r.get("at", 0))),
                            {"waves": 0, "groups": 0, "parallel": 0, "held": 0, "files": {}})
        d["waves"] += 1
        d["groups"] += r.get("groups", 0)
        d["parallel"] += len(r.get("same_file_parallel", []))
        d["held"] += len(r.get("held_by_collision", {}))
        for hits in r.get("collided_on", {}).values():
            for f in hits:
                d["files"][f] = d["files"].get(f, 0) + 1
    return [f"{day}: {d['waves']} waves, {d['groups']} groups, {d['parallel']} same-file pairs side by side, "
            f"{d['held']} notes held by a collision"
            + (" (most named: " + ", ".join(f"{f} x{n}" for f, n in sorted(d["files"].items(), key=lambda kv: (-kv[1], kv[0]))[:5])
               + ")" if d["files"] else "") for day, d in sorted(days.items())]
