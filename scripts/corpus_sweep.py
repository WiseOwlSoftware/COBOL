#!/usr/bin/env python3
"""THE reachability instrument: ask a shape-question of EVERY COBOL program the project tests against.

⛔ WHY THIS SCRIPT EXISTS — PB209. "A corpus-wide mechanical scan found ZERO programs with <shape>" was, until
2026-08-31, a `find … -name '*.cob'` typed at a shell. Run over `tests/external/gnucobol/` that glob finds
**two files**, because the 1,323 GnuCOBOL programs live as `AT_DATA` heredocs inside 36 `.at` autotest wrappers.
Two waves therefore shipped new bind-time REJECTIONS on a blast radius stated as empty, and the very next
differential found both shapes in the corpus the sweep was named after — one of them in a case whose title is
literally "REDEFINES: with OCCURS" (§13.18.44.3 SR5 s.1 → COBOL1701; §13.18.63.3 SR13 s.2 → COBOL1702).

Three rules follow from that, and they are the design:

  1. **The external half is EXTRACTED, never globbed.** It comes from `gnucobol_extract.differential_cases`,
     the same call `gnucobol_differential.py` makes, so a sweep over "the corpus" mechanically includes the
     programs the gate compiles. There is no second parser and no place to add one.
  2. **Every population prints its program count, always.** A population that contributed two files was
     reported as a corpus because nothing made the contribution visible. Now it cannot be silent.
  3. **The instrument proves itself before it is believed.** `--verify-population` asserts the external
     population equals the differential's committed per-case baseline; a sweep whose population check fails
     REFUSES to report hit counts, because "zero hits" from a broken reader is the exact defect above.
     `--self-test` runs that check on the real corpus, and drives its red on an emptied one, whenever this script,
     the extractor, the fetch or the committed baseline changes (kb/Work PB4395).

⚖ LICENSING: the GnuCOBOL corpus is GPL-3.0 and lives only in the git-ignored `tests/external/gnucobol/`.
Program TEXT is held in memory and matched; it is never written out. Hits are reported by our own coordinate
(case id + member name) plus the group's short factual title — citable identification, the nominative use any
write-up of the suite would make (owner decision 2026-07-19).

Usage:
    python scripts/corpus_sweep.py                                  # the census — what a sweep would read
    python scripts/corpus_sweep.py --verify-population              # the drift check, alone
    python scripts/corpus_sweep.py --pattern 'REDEFINES' --name redefines
    python scripts/corpus_sweep.py --codes COBOL1698,COBOL1701   # which diagnostics FIRED (compiled)
    python scripts/corpus_sweep.py --self-test                      # the population gate (PB209), below
"""
from __future__ import annotations
import argparse, json, os, re, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import gnucobol_extract as gx                                                              # noqa: E402
from standard_streams import ensure_utf8  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# ── the populations ───────────────────────────────────────────────────────────────────────────────────────
#
# A file-tree population is (name, relative root, suffixes). The external corpus is NOT one of these: it has
# no files, which is the whole point, so it is loaded through the shared extractor below.
FILE_POPULATIONS: list[tuple[str, str, tuple[str, ...]]] = [
    ('conformance',      'conformance-suite',     ('.cob', '.cbl', '.cpy')),
    ('nist-programs',    'tests/nist/programs',   ('.cob', '.cbl')),
    ('nist-copylib',     'tests/nist/copylib',    ('.cpy',)),
    ('characterization', 'tests/characterization', ('.cob', '.cbl', '.cpy')),
    ('version-matrix',   'tests/version-matrix',  ('.cob', '.cbl', '.cpy')),
    ('differential',     'tests/differential',    ('.cob', '.cbl', '.cpy')),
]

EXTERNAL = 'gnucobol-external'
BASELINE = 'tests/external/gnucobol-verdict-baseline.tsv'
REPORT = 'tests/external/gnucobol-differential-report.json'


class Program:
    """One program the sweep can read, from whichever population it came from."""
    __slots__ = ('population', 'coordinate', 'text')

    def __init__(self, population: str, coordinate: str, text: str):
        self.population, self.coordinate, self.text = population, coordinate, text


def load_file_population(name: str, rel: str, suffixes: tuple[str, ...], tree: str = ROOT) -> list[Program]:
    out: list[Program] = []
    for dirpath, _dirs, files in os.walk(os.path.join(tree, rel)):
        for f in sorted(files):
            if f.lower().endswith(suffixes):
                p = os.path.join(dirpath, f)
                try:
                    text = open(p, encoding='utf-8', errors='replace').read()
                except OSError:
                    continue
                out.append(Program(name, os.path.relpath(p, tree).replace('\\', '/'), text))
    return out


def load_external_population(src: str) -> tuple[list[Program], str]:
    """The GnuCOBOL half — through the ONE extractor. Returns (programs, state)."""
    try:
        progs = gx.iter_programs(src)
    except FileNotFoundError:
        return [], 'absent'
    return ([Program(EXTERNAL, f'{p.case_id}::{p.filename}', p.text) for p in progs], 'present')


def baseline_case_count(path: str) -> int | None:
    """The differential's `cases run`, as committed. `None` when the baseline itself is missing."""
    full = os.path.join(ROOT, path)
    if not os.path.exists(full):
        return None
    with open(full, encoding='utf-8') as fh:
        return sum(1 for line in fh if line.strip() and not line.startswith('#'))


def verify_population(src: str, baseline: str) -> tuple[bool, str, dict]:
    """⛔ THE DRIFT CHECK. The sweep's external population must equal the differential's `cases run`.

    Non-tautological on purpose: the left side is recomputed live from the `.at` wrappers, the right side is
    the COMMITTED per-case baseline the differential writes. They can only agree while the sweep is really
    reading the corpus the gate compiles — which is exactly the claim PB209 found to be false.
    """
    try:
        cases = gx.differential_cases(src)
    except FileNotFoundError:
        return False, (f'EXTERNAL POPULATION ABSENT - no corpus at {src}. This sweep CANNOT answer a '
                       f'reachability question about the external corpus; run python scripts/external_corpus.py ensure '
                       f'(GPL, never committed). A missing population is not an empty one.'), \
               {'state': 'absent', 'external': 0, 'baseline': baseline_case_count(baseline) or 0}
    n, b = len(cases), baseline_case_count(baseline)
    if b is None:
        return False, f'NO BASELINE at {baseline} — nothing to check the population against.', \
               {'state': 'no-baseline', 'external': n, 'baseline': 0}
    if n != b:
        return False, (f'POPULATION DRIFT: the sweep reads {n} external case(s) but the differential baseline '
                       f'records {b}. One of the two readers stopped seeing the corpus - that is PB209 '
                       f'recurring, and NO hit count from this run may be believed.'), \
               {'state': 'drift', 'external': n, 'baseline': b}
    return True, f'external population OK: {n} case(s) == {b} baseline row(s)', \
           {'state': 'ok', 'external': n, 'baseline': b}


def code_census(report: str, codes: list[str]) -> dict[str, list[dict]]:
    """Which diagnostics actually FIRED on the external corpus, from the differential's own per-case report.

    The pattern sweep answers "does the shape appear"; only a compile answers "does the screen fire". Both are
    reachability questions and both were got wrong at once, so both live in this one instrument.
    """
    full = os.path.join(ROOT, report)
    if not os.path.exists(full):
        raise FileNotFoundError(full)
    with open(full, encoding='utf-8') as fh:
        data = json.load(fh)
    hits: dict[str, list[dict]] = {c: [] for c in codes}
    for r in data.get('cases', []):
        for c in r.get('ourCodes', []):
            if c in hits:
                hits[c].append({'id': r['id'], 'verdict': r['verdict'], 'title': r.get('title', '')})
    return hits


def main(argv: list[str] | None = None, tree: str = ROOT) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--src', default=gx.DEFAULT_SRC, help='the .at wrapper directory of the external corpus')
    ap.add_argument('--baseline', default=BASELINE)
    ap.add_argument('--report', default=REPORT)
    ap.add_argument('--pattern', default='', help='regex asked of every program text (re.IGNORECASE|re.M)')
    ap.add_argument('--name', default='', help='what the pattern is looking for, for the report header')
    ap.add_argument('--codes', default='', help='comma-separated COBOLnnnn — which FIRED on the corpus')
    ap.add_argument('--verify-population', action='store_true', help='run the drift check alone')
    ap.add_argument('--show', type=int, default=25, help='max hits printed per population')
    ap.add_argument('--json', action='store_true', help='emit one machine-readable JSON line as well')
    a = ap.parse_args(argv)

    ok, message, detail = verify_population(a.src, a.baseline)
    print(f'{"POPULATION OK  " if ok else "POPULATION BAD "} {message}')
    if a.verify_population:
        if a.json:
            print('JSON ' + json.dumps(detail, sort_keys=True))
        return 0 if ok else 1

    # ── the census, always ────────────────────────────────────────────────────────────────────────────────
    pops: list[Program] = []
    counts: dict[str, int] = {}
    for name, rel, sfx in FILE_POPULATIONS:
        progs = load_file_population(name, rel, sfx, tree)
        counts[name] = len(progs)
        pops.extend(progs)
    ext, state = load_external_population(a.src)
    counts[EXTERNAL] = len(ext)
    pops.extend(ext)

    print('\n=== SWEEP POPULATION CENSUS (programs actually READ) ===')
    for name, _rel, _sfx in FILE_POPULATIONS:
        print(f'  {name:20} {counts[name]:6}')
    print(f'  {EXTERNAL:20} {counts[EXTERNAL]:6}   ({state}; extracted from .at - NOT a file glob)')
    print(f'  {"TOTAL":20} {len(pops):6}')

    if not ok:
        print('\n!! REFUSING TO REPORT HITS: the population check failed above, so a zero here would mean '
              'nothing. Fix the population first - a sweep is evidence about the programs it OPENED.',
              file=sys.stderr)
        return 1

    rc = 0
    if a.pattern:
        rx = re.compile(a.pattern, re.IGNORECASE | re.MULTILINE)
        label = a.name or a.pattern
        by_pop: dict[str, list[Program]] = {}
        for p in pops:
            if rx.search(p.text):
                by_pop.setdefault(p.population, []).append(p)
        total = sum(len(v) for v in by_pop.values())
        print(f'\n=== PATTERN SWEEP: {label} ===')
        for name in list(counts):
            hit = by_pop.get(name, [])
            print(f'  {name:20} {len(hit):6} / {counts[name]:6}')
            for p in hit[:a.show]:
                print(f'      {p.coordinate}')
            if len(hit) > a.show:
                print(f'      … and {len(hit) - a.show} more')
        print(f'=== SWEEP: {total} PROGRAM(S) MATCH {label} over {len(pops)} program(s) ===')
        if a.json:
            print('JSON ' + json.dumps({'pattern': a.pattern, 'name': label, 'total': total,
                                        'byPopulation': {k: len(v) for k, v in by_pop.items()},
                                        'counts': counts}, sort_keys=True))

    if a.codes:
        codes = [c.strip() for c in a.codes.split(',') if c.strip()]
        try:
            hits = code_census(a.report, codes)
        except FileNotFoundError as e:
            print(f'\n!! no differential report at {e} — run scripts/gnucobol_differential.py first; '
                  f'"which diagnostics fired" is a COMPILE question and cannot be answered by grep.',
                  file=sys.stderr)
            return 2
        # The unit here is CASES, not programs: the differential compiles one primary source per case, so
        # `detail['external']` (1323) is the right denominator and `counts[EXTERNAL]` (every extracted member,
        # 1611) is not. Naming the wrong denominator is how this file's ancestor got its claim wrong.
        print(f'\n=== DIAGNOSTIC CENSUS over the external corpus '
              f'({detail["external"]} case(s) compiled, from {counts[EXTERNAL]} extracted program(s)) ===')
        for c in codes:
            h = hits[c]
            print(f'  {c:14} {len(h):4} case(s)' + ('' if h else '   - fires on NO corpus case'))
            for r in h[:a.show]:
                print(f'      {r["id"]:<26} {r["verdict"]:<24} {r["title"][:60]}')
        if a.json:
            print('JSON ' + json.dumps({'codes': {c: [r['id'] for r in hits[c]] for c in codes}},
                                       sort_keys=True))
    return rc


# ── the self-test: the gate that keeps "the corpus" one population (PB209) ─────────────────────────────────────────
#
# Until kb/Work PB4395 a C# class of the compiler's test assembly (ExternalCorpusPopulationDriftTests) ran this
# instrument, so a change to it, to the extractor or to the fetch was a compiler change only a build could check. The
# INVARIANT arm compares two sides produced independently, one read live from the corpus and one written by the
# differential and committed, so their agreement is evidence rather than a tautology; the EMPTIED arm is not a formality
# (a check observed only passing is indistinguishable from one that inspects nothing, and "inspects nothing, reports a
# clean zero" is exactly PB209). A corpus that cannot be obtained is a LOUD failure, never a skip: a missing population is
# not an empty one. The census arms run on a planted tree, so this self-test reads the compiler's test corpora only
# through the external corpus and the committed baseline.

#: ⛔ THE ARMS scripts/external_corpus.py's self-test must declare (DESIGN-test-build-ci.md §3.14.4 arm (3); kb/Work
#: PB2611, PB4395). Held here, in the corpus family's other script, because a list in the file it checks would satisfy
#: a source search by itself; scripts/self_test_arms.py `DECLARED` is the register it belongs in.
EXTERNAL_CORPUS_ARMS = (
    'a present corpus runs NO fetch and is not red',
    'an absent corpus runs the fetch script once, in that tree, and is not red when it lands',
    'a fetch that exits non-zero is the named FETCH FAILED red',
    'a fetch that exits 0 but leaves no marker is the named FETCH FAILED red, never a green',
    'a fetch that cannot start (no pwsh) is the named FETCH FAILED red, never a crash',
)


def _run(argv: list[str], tree: str = ROOT) -> tuple[int, str, str]:
    """`main(argv)` in this process: (its exit code, its stdout, its stderr)."""
    import contextlib  # noqa: PLC0415
    import io  # noqa: PLC0415
    out, err = io.StringIO(), io.StringIO()
    with contextlib.redirect_stdout(out), contextlib.redirect_stderr(err):
        rc = main(argv, tree)
    return rc, out.getvalue(), err.getvalue()


def _json_line(stdout: str) -> dict:
    line = next((x.strip() for x in stdout.splitlines() if x.strip().startswith('JSON ')), '')
    return json.loads(line[len('JSON '):]) if line else {}


def self_test() -> int:
    import tempfile  # noqa: PLC0415
    import external_corpus  # noqa: PLC0415  (scripts/: THE one rule for obtaining the corpus)
    import self_test_arms  # noqa: PLC0415  (scripts/: the one reader of a self-test's declared arms)
    from pathlib import Path  # noqa: PLC0415
    results: list[tuple[str, bool, str]] = []

    def arm(name: str, ok: bool, detail: object = '') -> None:
        results.append((name, bool(ok), '' if ok else str(detail)))

    committed = baseline_case_count(BASELINE)
    src = os.path.join(ROOT, gx.DEFAULT_SRC)
    reds = external_corpus.ensure(Path(ROOT))
    ok, message, detail = verify_population(src, BASELINE)
    arm('THE INVARIANT: the sweep reads, live from the .at wrappers, exactly the external population the '
        "differential's committed baseline records (a corpus that cannot be obtained is RED, never a pass)",
        not reds and committed and ok and detail == {'state': 'ok', 'external': committed, 'baseline': committed},
        f'{reds} {message} {detail}; the committed baseline holds {committed} case(s)')
    programs, state = load_external_population(src)
    arm('the external population is PROGRAMS extracted from AT_DATA, at least one per case, never the two files a '
        '*.cob glob over the same tree returns', state == 'present' and committed and len(programs) >= committed,
        f'{state}: {len(programs)} program(s) for {committed} case(s)')

    with tempfile.TemporaryDirectory(prefix='corpus-sweep-selftest-') as tmp:
        tree = os.path.join(tmp, 'tree')
        empty = os.path.join(tmp, 'empty')
        os.makedirs(empty)
        planted = {'conformance-suite/cases/a/a.cob': 1, 'tests/nist/programs/b.cbl': 2, 'tests/nist/copylib/c.cpy': 3,
                   'tests/characterization/d.cob': 4, 'tests/version-matrix/e.cbl': 5, 'tests/differential/f.cpy': 6}
        for rel, n in planted.items():
            for i in range(n):
                path = os.path.join(tree, os.path.dirname(rel), f'{i}{os.path.basename(rel)}')
                os.makedirs(os.path.dirname(path), exist_ok=True)
                with open(path, 'w', encoding='utf-8', newline='\n') as fh:
                    fh.write('       IDENTIFICATION DIVISION.\n       DATA DIVISION.\n       01 R REDEFINES S.\n')
        baseline = os.path.join(tree, 'baseline.tsv')
        with open(baseline, 'w', encoding='utf-8', newline='\n') as fh:
            fh.write('# id\tverdict\nc1\tok\nc2\tok\nc3\tok\n')

        rc, out, _ = _run(['--verify-population', '--json', '--src', empty, '--baseline', baseline], tree)
        arm('an EMPTIED extraction makes the population check RED and NAMES the drift, never a clean zero',
            rc != 0 and _json_line(out) == {'state': 'drift', 'external': 0, 'baseline': 3}
            and 'POPULATION DRIFT' in out, f'exit {rc}\n{out}')
        rc, out, err = _run(['--pattern', 'REDEFINES', '--src', empty, '--baseline', baseline], tree)
        arm('a PATTERN sweep over that broken population REFUSES TO REPORT HITS, and still prints the census',
            rc != 0 and 'REFUSING TO REPORT HITS' in err and '=== SWEEP:' not in out
            and 'SWEEP POPULATION CENSUS' in out, f'exit {rc}\n{out}{err}')
        rc, out, _ = _run(['--src', empty, '--baseline', baseline], tree)
        census = {x.split()[0]: x.split()[1] for x in out.splitlines() if x.startswith('  ') and len(x.split()) > 1}
        want = {name: str(sum(n for k, n in planted.items() if k.startswith(rel + '/')))
                for name, rel, _ in FILE_POPULATIONS}
        arm('the census names EVERY population and the programs it read, the external one included',
            all(census.get(n) == c for n, c in want.items()) and census.get(EXTERNAL) == '0',
            f'want {want}; got {census}')

    gaps = self_test_arms.findings(declared={'scripts/external_corpus.py': EXTERNAL_CORPUS_ARMS}, exact=frozenset())
    arm("scripts/external_corpus.py's self-test still declares every arm of the one corpus rule (kb/Work PB2611)",
        not gaps, '; '.join(gaps))

    for name, passed, why in results:
        print(f"  {'PASS' if passed else 'FAIL'}  {name}" + ('' if passed or not why else f'\n        {why}'))
    failed = sum(1 for _, passed, _ in results if not passed)
    print(f"=== CORPUS SWEEP SELF-TEST: {'FAIL — ' + str(failed) + ' of ' if failed else 'ALL GREEN — '}"
          f'{len(results)} arms ===')
    return 1 if failed else 0


if __name__ == '__main__':
    ensure_utf8()
    sys.exit(self_test() if sys.argv[1:] == ['--self-test'] else main())
