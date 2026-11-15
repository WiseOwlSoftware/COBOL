#!/usr/bin/env python3
"""gate_reuse.py - WHAT A RECORDED RUN LETS THIS GATE REUSE (kb/Work PB5411, PB3536, PB5099, PB5205, PB5218; docs/rearchitecture/DESIGN-orchestrator-loop.md
section 16.5, step 2): the facets of a pass, the earlier runs whose passes this run carries, the passes recorded in the store, and the
re-derivation of a run's evidence from its own files. `PassCarry` returns what it found and never calls back into `Gate`."""
from __future__ import annotations

import datetime as dt
import json
import re
import shutil
import sys
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Callable

REPO = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(REPO / "scripts"))
import buildgraph  # noqa: E402  — THE targets, their keys and the one store of their passes (kb/Work PB5207, PB5218)
from test_population import GATED_ASSEMBLIES, PopulationError, UnreadableTrx, check as check_population, failed_results, passed_cases, parse_listing  # noqa: E402
from test_leg_report import verdict_of  # noqa: E402  — THE reader of a host's verdict line
from gate_verdict import HOST_PASSED, Outcome, audit_reasons, result_outcomes  # noqa: E402
from gate_host import GATE_CONFIGURATION, Host  # noqa: E402
from gate_runs import ORIGINAL_VERDICT_FILE, SCHEMA_WHOLE, VERDICT_FILE, read_record, record_schema, run_dirs, run_started  # noqa: E402


@dataclass
class Rederived:
    """WHAT A RUN'S OWN ON-DISK EVIDENCE PROVES (kb/Work PB5205; `PassCarry.rederive`): the record it re-derives (None when
    refused), what it found, and every reason it refused, each naming what is missing."""
    record: dict | None
    found: list[str]
    refusals: list[str]
    level: str = ""   # "stored" (every assembly proven EXACT), "passes" (the next gate lists), or "stored and passes"
    stored: dict[str, dict] = field(default_factory=dict)   # the assemblies proven EXACT, with their counts


@dataclass
class Carry:
    """WHAT EARLIER RUNS OF THIS WORKTREE LEFT AT EACH TARGET'S KEY (kb/Work PB5099, PB5218; `PassCarry.carry`): the runs they
    are, newest first, and each assembly's passes there."""
    runs: list[str]
    passed: dict[str, Counter]
    red_hosts: set[str] = field(default_factory=set)   # assemblies whose host was RED in a run whose passes are carried


class PassCarry:
    """What a recorded run lets this gate reuse: THE GATE'S REUSE RULE, ITS COMPOSITION RULE PER TARGET, the passes recorded
    in the store and a re-derivation (kb/Work PB3536, PB5099, PB5205, PB5218)."""
    def __init__(self, host: Host, run_root: Path, say: Callable[[str], None],
                 now: Callable[[], dt.datetime] = lambda: dt.datetime.now(dt.timezone.utc)):
        self.host, self.run_root, self.say, self.now = host, run_root, say, now

    def reusable(self, current: Path, out: Outcome) -> tuple[Path, dict] | None:
        """THE GATE'S REUSE RULE (kb/Work PB3536; the self-test reuse rule of kb/Work PB2914/PB3022 extended to the
        gate): the newest earlier run of this worktree whose verdict file is GREEN (exit 0) for THIS tree — measured at
        its start and unchanged at its end — against THIS base, under THIS host's toolchain (kb/Work PB5218), written by a gate that runs the whole population (record
        schema 5 on, kb/Work PB4328: an older record may be a `LEG 1 ONLY` one). None when the tree could not be measured or
        no run qualifies: a RED is never reused, and another tree (a landing's merged head) always runs."""
        if not out.tree:
            return None
        tools: str | None = None
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is not None and rec.get("exit_code") == 0 and rec.get("tree") == out.tree
                    and rec.get("tree_end") == out.tree and rec.get("base") == out.base
                    and record_schema(rec) >= SCHEMA_WHOLE):
                # ⛔ AND UNDER THIS HOST'S TOOLCHAIN (kb/Work PB5218): a GREEN measured under other tools is no answer here,
                # the rule a target's key has (its toolchain facet). Asked once, only when a record qualifies.
                if tools is None:
                    matched, tools = self.host.toolchain()
                    tools = tools if matched else ""
                if tools and rec.get("toolchain") == tools:
                    return p, rec
        return None

    def facets(self, toolchain: str) -> dict[str, str]:
        """The facets of every target's key for this gate: the host's platform, the configuration it builds and runs
        (GATE_CONFIGURATION) and the toolchain it runs under."""
        return {"platform": self.host.platform(), "configuration": GATE_CONFIGURATION, "toolchain": toolchain}

    def carry(self, current: Path, out: Outcome, todo: list[str]) -> Carry | None:
        """THE GATE'S COMPOSITION RULE, PER TARGET (kb/Work PB5099, owner 2026-11-09: "if only one test failed, any retry
        should test only that one case"; kb/Work PB5218): for each assembly in `todo` (a target the store does not show up
        to date), what the earlier runs of THIS WORKTREE passed at ITS key. An earlier run counts when it is a full-lane
        run that measured its own tree (a RED one included, its trx are its results; a REUSED record ran nothing) and its
        tree did not change while it ran; its trx of an assembly count when it recorded that assembly's key as this one.
        `_tested` then runs only the listed cases those passes do not cover. The per-case evidence stays in this worktree's
        run directories in stage 1 (kb/Work PB5218): the store holds whole-target passes only. None when no run
        qualifies."""
        if not out.tree or not out.keys:
            return None
        runs: list[str] = []
        red_hosts: set[str] = set()
        passed = {asm: Counter() for asm in GATED_ASSEMBLIES}
        for p in reversed(run_dirs(self.run_root)):
            rec = read_record(p) if p != current else None
            if (rec is None or rec.get("reused_from") or rec.get("lane") != "full" or record_schema(rec) < SCHEMA_WHOLE
                    or not rec.get("tree") or rec.get("tree_end") != rec["tree"]):
                continue
            keys = rec.get("keys") if isinstance(rec.get("keys"), dict) else {}
            found = {}
            try:
                for asm in todo:
                    if keys.get(asm) == out.keys.get(asm) and (p / f"{asm}.trx").is_file():
                        found[asm] = passed_cases([p / f"{asm}.trx"])
            except UnreadableTrx as e:
                self.say(f"build-local: ⚠ run {p.name}'s passes are not carried ({e})")
                continue
            if found:
                runs.append(p.name)
                for asm, names in found.items():
                    passed[asm] |= names
                red_hosts |= {r["asm"] for r in rec.get("runs") or []
                              if isinstance(r, dict) and r.get("red") and r.get("asm") in found
                              and not self._red_explained(p, r["asm"])}
        return Carry(runs, passed, red_hosts=red_hosts) if runs else None

    def record_passes(self, out: Outcome, run: Path, assemblies: list[str], generated: dict[str, str]) -> None:
        """RECORD WHAT WAS PROVEN (kb/Work PB5218): each assembly whose host was green (or that ran no host because its
        carried passes covered it) and whose population check was EXACT, at a tree that did not change while the gate
        ran, gets its pass in the store at its key. A red, a partial or an unmeasured population records nothing."""
        if not out.tree or out.tree_end != out.tree:
            return
        store = self.host.store()
        for asm in assemblies:
            pop, k = out.populations.get(asm), out.keys.get(asm)
            if pop is None or not pop.exact or k is None or any(r.asm == asm and r.red for r in out.runs):
                continue
            target = self.host.targets()[asm]
            store.record(target.name, k, {
                "assembly": asm, "facets": self.facets(str(out.toolchain)), "worktree": str(self.host.repo),
                "run": run.name, "evidence": str(run), "head": out.head, "tree": out.tree, "when": f"{self.now():%Y-%m-%dT%H:%M:%SZ}",
                "counts": {"discovered": pop.discovered, "ran": pop.ran, "skipped": pop.skipped, "carried": pop.carried},
                "composed_from": out.composed_from,
                # the output digest of each generated target it was keyed on and ran (kb/Work PB5231)
                "depends": {d: generated[d] for d in target.depends if d in generated}})
            out.recorded.append(asm)

    def rederive(self, run: Path) -> Rederived:
        """RE-DERIVE A RUN'S POPULATION EVIDENCE FROM ITS OWN FILES (kb/Work PB5205; owner 2026-11-10: "We better not be
        running all those tests again, when only the self tests failed to run"). A gate before PB5205, or one an audit
        stopped, may have left complete, green test results under a record that says `populations: {}` and
        `tree_end: null`, which `carry` cannot use. This proves from the run directory and the worktree what such a record
        could not say, and refuses, naming what is missing, unless ALL of it holds:
          * the record is a full-lane, schema-5-on record of a run that measured its own tree (never a REUSED one);
          * THE TOOLCHAIN it recorded is the one this host reports now (`Host.toolchain`);
          * EVERY HOST FINISHED GREEN: the record lists each gated assembly's host with exit 0, its log ends in the host's
            own `Passed!  - Failed: 0, Passed: P, Skipped: S, Total: T` line, and its trx is COMPLETE against that line (T
            results, P Passed, S NotExecuted and nothing else: `result_outcomes`, the results that line counts). A host
            that never finished has no such line; a short trx, too few results;
          * EVERY REASON IS AN AUDIT'S (`audit_reasons`): a host-level or population reason is never re-derived;
          * THE TREE DID NOT MOVE while it ran: it measured HEAD's own tree (`tree` equals `head_tree`, so nothing
            uncommitted), HEAD's reflog puts HEAD at the recorded head when the run started (`run_started`: its
            `gate.pid`, written before the tree was measured) and records no move from then to the last host's last
            write. THE RESIDUE (kb/Work PB5205 review R1-2), named in the record: an uncommitted edit made while the run
            ran and undone at ANY time before this re-derivation is in no reflog, where the live gate, which measures
            `tree_end` after its hosts, would have refused it;
          * each target's key is computed from its RECORDED tree and toolchain (`Host.target_keys`), a dependent one on the
            generated output the RUN recorded it was handed (`_rederived_keys`, kb/Work PB5231 review R2-2): refused when
            the store now records another digest there, and that assembly REFUSED (never stored or carried) when the run
            predates the hand-off.
        Then the record gains `tree_end` (its tree), `keys`, `audit_reds` and `rederived` (when, the evidence, the
        residue). Its verdict, line and exit code are unchanged: it stays RED. An assembly for which a run of this worktree
        at the SAME KEY kept the listing (`listing-<asm>.txt`, the expected population) is checked EXACT against it by the
        live check (`check_population`) and, exact, its pass is recorded in the store ("stored": the next gate finds that
        target up to date); a listing it is not exact against is a refusal. An assembly with no such listing is "passes":
        `carry` takes its trx passes, and the next gate builds, lists, runs only the cases they do not cover (none, for a
        complete run), checks EXACT and records the pass then. Neither can make a partial run a pass."""
        found: list[str] = []
        refusals: list[str] = []
        rec = read_record(run)
        if rec is None:
            return Rederived(None, found, [f"{run} holds no readable verdict record"])
        if rec.get("reused_from") or rec.get("lane") != "full" or record_schema(rec) < SCHEMA_WHOLE or not rec.get("tree"):
            return Rederived(None, found, ["it is not a full-lane record (schema 5 on) of a run that measured its own tree"])
        if rec.get("rederived"):
            return Rederived(None, found, [f"it was re-derived already ({rec['rederived'].get('at')})"])
        if rec.get("tree_end") == rec["tree"] and rec.get("keys"):
            return Rederived(None, found, ["nothing to re-derive: the record already says its tree_end and its keys"])
        matched, tools = self.host.toolchain()
        if not matched or tools != rec.get("toolchain"):
            refusals.append(f"the toolchain it recorded ({rec.get('toolchain')}) is not this host's ({tools})")
        hosts = {r.get("asm"): r for r in rec.get("runs") or [] if isinstance(r, dict)}
        ends: list[float] = []
        for asm in GATED_ASSEMBLIES:
            r, log, trx = hosts.get(asm), run / f"{asm}.log", run / f"{asm}.trx"
            if r is None or r.get("rc") != 0 or r.get("red"):
                refusals.append(f"{asm}: the record does not show its host exiting green ({r})")
                continue
            try:
                line = verdict_of(log.read_text(encoding="utf-8", errors="replace").splitlines()) or ""
                ends += [log.stat().st_mtime, trx.stat().st_mtime]
            except OSError as e:
                refusals.append(f"{asm}: its host log or trx cannot be read ({e})")
                continue
            m = HOST_PASSED.search(line)
            if m is None:
                refusals.append(f"{asm}: its host log ends in no `Passed!` verdict line (it never finished): {line!r}")
                continue
            passed_n, skipped_n, total = (int(g) for g in m.groups())
            try:
                outcomes = result_outcomes(trx)
            except UnreadableTrx as e:
                refusals.append(f"{asm}: its trx cannot be read ({e})")
                continue
            got = (sum(outcomes.values()), outcomes["Passed"], outcomes["NotExecuted"])
            if got != (total, passed_n, skipped_n):
                refusals.append(f"{asm}: its trx is not complete against its host's verdict line (results, Passed, "
                                f"NotExecuted {got}; the host said {(total, passed_n, skipped_n)})")
                continue
            found.append(f"{asm}: host exit 0, `Passed!` {passed_n:,} of {total:,}, trx complete")
        audit, other = audit_reasons([str(x) for x in rec.get("reasons") or []])
        if other:
            refusals.append(f"reasons no audit gave: {other}")
        started = run_started(run)
        if rec.get("head_tree") != rec["tree"] or not rec.get("head"):
            refusals.append("it did not measure its HEAD's own tree (uncommitted changes, or no head recorded): no reflog "
                            "can show that tree stood")
        elif started is None or not ends:
            refusals.append("its start (its gate.pid's time) or its end (the hosts' last writes) is unknown")
        else:
            moves = self.host.head_reflog()
            end = max(ends)
            if moves is None:
                refusals.append("HEAD's reflog cannot be read")
            else:
                before = [c for c, t in moves if t < started]
                during = [(c, t) for c, t in moves if started <= t <= end]
                if not before or before[0] != rec["head"]:
                    refusals.append(f"HEAD's reflog does not put HEAD at {rec['head'][:12]} when the run started "
                                    f"({before[0][:12] if before else 'no entry before it'})")
                if during:
                    refusals.append("HEAD moved while the run ran: " + ", ".join(
                        f"{c[:12]} at {dt.datetime.fromtimestamp(t, dt.timezone.utc):%H:%M:%SZ}" for c, t in during))
                if not refusals:
                    found.append(f"HEAD stood at {rec['head'][:12]} (tree {rec['tree'][:12]}) from the run's start "
                                 f"{dt.datetime.fromtimestamp(started, dt.timezone.utc):%Y-%m-%dT%H:%M:%SZ} to its hosts' "
                                 f"last write {dt.datetime.fromtimestamp(end, dt.timezone.utc):%Y-%m-%dT%H:%M:%SZ}")
        keys, refused = self._rederived_keys(rec, refusals)
        if refusals or keys is None:
            return Rederived(None, found, refusals)
        new = {**rec, "tree_end": rec["tree"], "keys": keys, "audit_reds": audit}
        levels: dict[str, str] = {asm: f"refused: {why}" for asm, why in refused.items()}
        stored: dict[str, dict] = {}
        for asm in GATED_ASSEMBLIES:
            if asm in refused:
                found.append(f"{asm}: REFUSED, never stored or carried: {refused[asm]}")
                continue
            listing = self._listing_at(asm, keys[asm], run)
            if listing is None:
                levels[asm] = "passes"
                continue
            where, listed = listing
            pop = check_population(asm, listed, [str(run / f"{asm}.trx")])
            if not pop.exact:
                return Rederived(None, found, [f"{asm}: not EXACT against the listing of run {where} at its key: "
                                               f"{pop.verdict_line()}"])
            stored[asm] = {"discovered": pop.discovered, "ran": pop.ran, "skipped": pop.skipped, "carried": 0}
            found.append(f"{asm}: EXACT against the listing run {where} kept at its key")
            levels[asm] = "stored"
        new["rederived"] = {
            "at": f"{self.now():%Y-%m-%dT%H:%M:%SZ}", "by": "gate.py --rederive (kb/Work PB5205, PB5218)", "levels": levels,
            "evidence": found,
            "residue": "an uncommitted edit made while the run ran and undone at any time before this re-derivation is in "
                       "no reflog (the live gate, measuring tree_end after its hosts, would have refused it)"
                       + ("; an assembly with no listing of its key on disk is left to the next gate, which lists its "
                          "population and checks the carried passes EXACT against it" if "passes" in levels.values() else "")}
        new["populations"] = {**(rec.get("populations") or {}), **{a: {**c, "exact": True} for a, c in stored.items()}}
        kept = [v for a, v in levels.items() if a not in refused]
        level = ("stored" if all(v == "stored" for v in kept) else
                 "passes" if all(v == "passes" for v in kept) else "stored and passes") \
            + (f" ({', '.join(sorted(refused))} refused)" if refused else "")
        return Rederived(new, found, [], level, stored)

    def _rederived_keys(self, rec: dict, refusals: list[str]) -> tuple[dict[str, str] | None, dict[str, str]]:
        """THE KEYS A RE-DERIVED RUN STANDS AT (kb/Work PB5231 review R2-2): each assembly's key at the run's RECORDED
        tree and toolchain, a dependent one keyed on the output digest the RUN ITSELF recorded (`generated`: the suite
        its host was handed and ran), never the digest the store holds now. Where the store's record at that generated
        key holds another digest, the run is refused (`refusals`), naming both. A run that recorded no generation of a
        dependency (it predates the hand-off, and its host generated its own suite) has that dependent assembly REFUSED:
        returned in the second map, never stored or carried. Returns (the keys, None when git cannot say; the refused
        assemblies with why)."""
        facets = self.facets(str(rec.get("toolchain") or ""))
        generated = {n: g for n, g in (rec.get("generated") or {}).items() if isinstance(g, dict) and g.get("outputs")}
        store = self.host.store()
        for name, g in sorted(generated.items()):
            now = (store.lookup(name, str(g.get("key") or "")) or {}).get("outputs")
            if now is not None and now != g["outputs"]:
                refusals.append(f"{name}: the run was handed output {g['outputs'][:12]} at key {str(g.get('key'))[:12]}, "
                                f"and the store now records {now[:12]} there: its dependents' passes are not that run's")
        refused: dict[str, str] = {}
        keys = self.host.target_keys(rec["tree"], facets, {n: g["outputs"] for n, g in generated.items()}, refused)
        refused = {asm: f"its run recorded no generation it was handed ({why}; it predates the hand-off of kb/Work "
                        "PB5231, so its host generated its own suite)" for asm, why in refused.items()}
        if keys is None:
            refusals.append(f"the targets' keys of its tree {rec['tree'][:12]} cannot be computed")
        return keys, refused

    def _listing_at(self, asm: str, key: str, run: Path) -> tuple[str, Counter] | None:
        """The expected population of `asm` at its `key`: the listing (`listing-<asm>.txt`) a run of this worktree kept
        whose key of `asm` (recorded, or computed from its recorded tree and toolchain) is `key`; `run` first."""
        for p in [run, *reversed(run_dirs(self.run_root))]:
            f = p / f"listing-{asm}.txt"
            rec = read_record(p) or {}
            if not f.is_file() or not rec.get("tree"):
                continue
            recorded = rec.get("keys") if isinstance(rec.get("keys"), dict) else {}
            try:
                k = recorded.get(asm) or (self.host.target_keys(rec["tree"], self.facets(str(rec.get("toolchain") or "")))
                                          or {}).get(asm)
            except buildgraph.Unresolved:
                continue   # its key cannot be resolved (kb/Work PB5231), so its listing is of no key we know
            if k == key:
                try:
                    return p.name, parse_listing(f.read_text(encoding="utf-8"), str(f))
                except (OSError, PopulationError):
                    continue
        return None

    def write_rederived(self, run: Path, rd: Rederived) -> None:
        """Keep the record its gate wrote as ORIGINAL_VERDICT_FILE (never overwritten), write the re-derived one, and record
        in the store the pass of every assembly it proved EXACT (kb/Work PB5218)."""
        assert rd.record is not None
        rec = rd.record
        store = self.host.store()
        for asm, counts in rd.stored.items():
            store.record(self.host.targets()[asm].name, rec["keys"][asm], {
                "assembly": asm, "facets": self.facets(str(rec.get("toolchain") or "")), "worktree": str(self.host.repo),
                "run": run.name, "evidence": str(run), "head": rec.get("head"), "tree": rec.get("tree"),
                "when": f"{self.now():%Y-%m-%dT%H:%M:%SZ}", "counts": counts, "rederived": True})
        original = run / ORIGINAL_VERDICT_FILE
        if not original.exists():
            shutil.copy2(run / VERDICT_FILE, original)
        (run / VERDICT_FILE).write_text(json.dumps(rd.record, indent=1, sort_keys=True) + "\n", encoding="utf-8",
                                        newline="\n")

    @staticmethod
    def _red_explained(run: Path, asm: str) -> bool:
        """WHETHER A RED HOST'S RED IS THE CASES ITS TRX NAMES (kb/Work PB5099, review R1-1 and R2-1): the host printed a
        `Failed!` verdict whose Failed count equals the trx's Failed results, and there is at least one. Then a retry of
        those cases tests everything the red was about. Any other red (no verdict line, a count the trx does not
        account for, a crash after the last result, a cleanup failure) has a host-level cause no case carries, and the
        assembly runs whole again."""
        try:
            verdict = verdict_of((run / f"{asm}.log").read_text(encoding="utf-8", errors="replace").splitlines())
            failed = failed_results(run / f"{asm}.trx")
        except (OSError, UnreadableTrx):
            return False
        m = re.match(r"Failed!\s+-\s+Failed:\s+(\d+)", verdict or "")
        return bool(m) and failed > 0 and int(m.group(1)) == failed
