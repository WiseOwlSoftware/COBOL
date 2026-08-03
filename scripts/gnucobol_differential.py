#!/usr/bin/env python3
"""Run the extracted GnuCOBOL corpus through cobol.exe and bucket the accept/reject divergences.

Plan §11 A4 / PHASE-14 Step 13 — the EXTERNAL DIFFERENTIAL CORPUS.

⚖ LICENSING: the corpus is GPL-3.0 and lives ONLY in the git-ignored `tests/external/gnucobol/`; this repo is
BSL 1.1. This script is ours. The line (owner decision 2026-07-19): their COBOL test SOURCE and their EXPECTED
OUTPUT/diagnostic text are the substantial expressive content and are NEVER committed or reproduced. Short
factual group TITLES and KEYWORDS are citable identification — the nominative use any article describing the
suite would make — so this report carries them alongside our own verdicts, codes, and counts.

WHAT A DIVERGENCE MEANS (read before triaging — the naive reading is wrong):
GnuCOBOL's DEFAULT dialect is `default`, i.e. ISO **plus GnuCOBOL extensions**; the testsuite only pins an
ISO edition on a minority of groups (-std=cobol85/cobol2002/cobol2014) and pins VENDOR dialects (mf/ibm/acu)
on others. Therefore:
  * WE_REJECT_THEY_ACCEPT is NOT automatically our bug — it is our bug only where the construct is ISO.
    Where the construct is a GnuCOBOL/vendor extension, rejecting it is OUR CORRECT BEHAVIOUR.
  * WE_ACCEPT_THEY_REJECT is the higher-confidence signal: GnuCOBOL's permissive default rejected it, so
    an ISO-conforming compiler almost certainly should too. These are prime "our grammar is too lax" leads.
Confidence tiers are emitted so triage can start where the signal is cleanest.
"""
from __future__ import annotations
import argparse, json, os, re, subprocess, sys, tempfile
from concurrent.futures import ProcessPoolExecutor

STD_MAP = {'cobol85': '85', 'cobol2002': '2002', 'cobol2014': '2014', 'cobol2023': '2023'}
VENDOR_STDS = ('mf', 'ibm', 'acu', 'rm', 'xopen', 'mvs', 'bs2000', 'realia')


def std_of(cmd: str) -> tuple[str, str]:
    """Return (our_std, tier) for a GnuCOBOL compile command."""
    m = re.search(r'-std=([a-z0-9-]+)', cmd)
    if not m:
        return '2023', 'DEFAULT_DIALECT'      # their default = ISO + extensions
    s = m.group(1)
    if s in STD_MAP:
        return STD_MAP[s], 'ISO_PINNED'       # directly comparable
    base = s.split('-')[0]
    if base in VENDOR_STDS:
        return '2023', 'VENDOR_DIALECT'       # not ISO; rejecting is correct for us
    return '2023', 'DEFAULT_DIALECT'


def compile_once(exe: str, src: str, std: str, out_dll: str):
    """One compile attempt → (returncode, diagnostics, artifact_exists, runner_error).

    Everything the evidence rule needs, and nothing inferred. `runner_error` is non-empty only when the
    PROCESS could not be observed at all (timeout, launch failure) — distinct from a process that ran and
    said something.
    """
    try:
        r = subprocess.run([exe, src, '--std', std, '-o', out_dll],
                           capture_output=True, text=True, timeout=60)
        return r.returncode, r.stdout + r.stderr, os.path.exists(out_dll), ''
    except subprocess.TimeoutExpired:
        return -1, '<<TIMEOUT>>', False, 'TimeoutExpired'
    except Exception as e:                                    # noqa: BLE001
        return -1, '', False, f'{type(e).__name__}: {e}'


def has_evidence(rc: int, diag: str, artifact: bool) -> bool:
    """Does this compile support a VERDICT about the compiler?

    Accept needs the artifact it claims to have produced; reject needs the reason it claims to have found.
    An exit status on its own is not evidence: under load a process can be killed or fail to start and
    return non-zero having compiled nothing and said nothing. Verified against this build by
    evidence_control(): a rejection is exit 65 with ~300 bytes of diagnostic, an acceptance is exit 0 with
    the .dll — there is no third shape.
    """
    return bool(artifact) if rc == 0 else bool(diag.strip())


EVIDENCE_CONTROL_GOOD = ('IDENTIFICATION DIVISION.\nPROGRAM-ID. EVCTLOK.\n'
                         'PROCEDURE DIVISION.\nMAIN.\n    DISPLAY "OK".\n    STOP RUN.\n')
EVIDENCE_CONTROL_BAD = ('IDENTIFICATION DIVISION.\nPROGRAM-ID. EVCTLBAD.\n'
                        'PROCEDURE DIVISION.\nMAIN.\n    MOVE TO.\n    STOP RUN.\n')


def evidence_control(exe: str) -> list[str]:
    """Prove — on THIS build, before any case is scored — that the evidence rule can tell accept from reject.

    `feedback_green_gates_arent_evidence`: a rule that has never been shown to discriminate is not a rule. If
    the compiler ever rejects silently, `has_evidence` would classify every genuine rejection as a lost result
    and the differential would go quietly, uselessly clean. This is the check that makes that loud instead.
    Returns a list of failure descriptions (empty = the rule holds).
    """
    fails = []
    with tempfile.TemporaryDirectory(prefix='gcdiff_ctl_') as d:
        for tag, src_text, want_ok in (('accept', EVIDENCE_CONTROL_GOOD, True),
                                       ('reject', EVIDENCE_CONTROL_BAD, False)):
            src = os.path.join(d, f'{tag}.cob')
            with open(src, 'w', encoding='utf-8', newline='\n') as fh:
                fh.write(src_text)
            rc, diag, artifact, err = compile_once(exe, src, '2023', os.path.join(d, f'{tag}.dll'))
            if err:
                fails.append(f'{tag} control could not be run: {err}')
            elif (rc == 0) != want_ok:
                fails.append(f'{tag} control returned exit {rc} (expected {"0" if want_ok else "non-zero"})')
            elif not has_evidence(rc, diag, artifact):
                fails.append(f'{tag} control produced NO EVIDENCE (exit {rc}, {len(diag.strip())} diag bytes, '
                             f'artifact={artifact}) — the evidence rule cannot classify this build')
    return fails


def run_case(args):
    exe, group, workroot = args
    gid = group['id']
    srcs = [f for f in group['data'] if f.lower().endswith(('.cob', '.cbl'))]
    if not srcs:
        return None
    # the primary source is the one the compile check names, else the first
    compile_checks = [c for c in group['checks'] if c['kind'].startswith('compile')]
    if not compile_checks:
        return None
    chk = compile_checks[0]
    prim = next((s for s in srcs if s in chk['command']), srcs[0])

    d = os.path.join(workroot, re.sub(r'[^A-Za-z0-9_.-]', '_', gid))
    os.makedirs(d, exist_ok=True)
    for fn, content in group['data'].items():
        p = os.path.join(d, os.path.basename(fn))
        try:
            with open(p, 'w', encoding='utf-8', newline='\n') as fh:
                fh.write(content.lstrip('\n'))
        except OSError:
            return None

    std, tier = std_of(chk['command'])
    out_dll = os.path.join(d, '_out.dll')

    # ⛔ EVIDENCE-REQUIRED VERDICTS (plan §11 A12e). A compile is retried ONCE when it produced no evidence,
    # because the first attempt made no observation to retry the interpretation of — see compile_once().
    rc, diag, artifact, runner_error = compile_once(exe, os.path.join(d, os.path.basename(prim)), std, out_dll)
    if not runner_error and not has_evidence(rc, diag, artifact):
        try:
            os.remove(out_dll)
        except OSError:
            pass
        rc, diag, artifact, runner_error = compile_once(exe, os.path.join(d, os.path.basename(prim)), std, out_dll)

    def _rec(verdict, codes, first):
        return {'id': gid, 'file': group['file'], 'title': group['title'][:160],
                'keywords': group['keywords'], 'std': std, 'tier': tier,
                'verdict': verdict, 'ourCodes': codes, 'ourFirstError': first,
                'xfail': group.get('xfail', False)}

    # A harness failure is NOT a compiler verdict. Laundering one into WE_REJECT_THEY_ACCEPT is how a
    # totally broken run reports as 1046 "compiler bugs" — which is exactly what happened on the first
    # run of this script (a relative exe path that Windows CreateProcess could not resolve).
    if runner_error:
        return _rec('RUNNER_ERROR', [], runner_error)

    # ⛔ AND NEITHER IS AN EVIDENCE-FREE EXIT STATUS. Observed 2026-08-02: `run_fundamental.at :: "Numeric
    # operations (6)"` was scored WE_REJECT_THEY_ACCEPT — the 0-tolerance direction — with `ourCodes: []`, an
    # EMPTY `ourFirstError`, and a case that compiles clean when run directly. A "rejection" with no diagnostic
    # is not a rejection; it is a LOST RESULT, and scoring it as one manufactures a regression in the one
    # direction the gate treats as unforgivable. Same root cause as A12b/A12c: a missing observation was being
    # read as a negative observation. The premise is checked at startup by evidence_control(), so this rule
    # cannot quietly become wrong if the compiler ever stops printing a reason for a rejection.
    if not has_evidence(rc, diag, artifact):
        why = (f'exit {rc} with NO diagnostic output' if rc != 0
               else 'exit 0 but no output artifact was produced')
        return _rec('NO_COMPILER_EVIDENCE', [], f'{why} (retried once)')

    we_ok = (rc == 0)
    they_ok = not chk['expects_failure']
    if we_ok and they_ok:
        verdict = 'AGREE_ACCEPT'
    elif not we_ok and not they_ok:
        verdict = 'AGREE_REJECT'
    elif we_ok and not they_ok:
        verdict = 'WE_ACCEPT_THEY_REJECT'
    else:
        verdict = 'WE_REJECT_THEY_ACCEPT'

    codes = sorted(set(re.findall(r'\b(COBOLNET\d{4}|COBOL\d{4})\b', diag)))
    first = ''
    for line in diag.splitlines():
        if 'error' in line.lower():
            first = line.strip()[:300]
            break
    return {'id': gid, 'file': group['file'], 'title': group['title'][:160],
            'keywords': group['keywords'], 'std': std, 'tier': tier,
            'verdict': verdict, 'ourCodes': codes, 'ourFirstError': first,
            'xfail': group.get('xfail', False)}


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--exe', default='src/Cobol.Net.Cli/bin/Debug/net10.0/cobol.exe')
    ap.add_argument('--src', default='tests/external/gnucobol/tests/testsuite.src')
    ap.add_argument('--only', default='')
    ap.add_argument('--jobs', type=int, default=max(2, (os.cpu_count() or 4) - 2))
    ap.add_argument('--report', default='tests/external/gnucobol-differential-report.json')
    a = ap.parse_args()

    if not os.path.exists(a.exe):
        print(f'!! compiler not found: {a.exe}', file=sys.stderr); return 2
    # MUST be absolute: on Windows, CreateProcess does not resolve a relative forward-slash path, so
    # subprocess.run() raises FileNotFoundError (WinError 2) even though os.path.exists() is True. Left
    # relative, EVERY case fails identically and the run reports a corpus-wide false "we reject everything".
    a.exe = os.path.abspath(a.exe)

    # ⛔ THE EVIDENCE RULE IS PROVEN BEFORE IT IS APPLIED (plan §11 A12e). If this build cannot be told to
    # reject with a reason, every scored rejection below would be indistinguishable from a lost result.
    ctl = evidence_control(a.exe)
    if ctl:
        print('!! THE EVIDENCE CONTROL FAILED — verdicts from this run would not be trustworthy:', file=sys.stderr)
        for m in ctl:
            print(f'   {m}', file=sys.stderr)
        return 4
    print('evidence control: accept and reject are distinguishable on this build.')

    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    from gnucobol_extract import parse_file            # noqa: E402
    from dataclasses import asdict

    if not os.path.isdir(a.src):
        print(f'!! corpus absent: {a.src}\n   run scripts/fetch-gnucobol-tests.ps1 (GPL, never committed).',
              file=sys.stderr)
        return 2

    groups = []
    for f in sorted(x for x in os.listdir(a.src) if x.endswith('.at') and x.startswith(a.only)):
        groups.extend(asdict(g) for g in parse_file(os.path.join(a.src, f)))

    work = tempfile.mkdtemp(prefix='gcdiff_')
    payload = [(a.exe, g, work) for g in groups]
    results = []
    with ProcessPoolExecutor(max_workers=a.jobs) as ex:
        for i, r in enumerate(ex.map(run_case, payload, chunksize=4), 1):
            if r: results.append(r)
            if i % 200 == 0: print(f'  ... {i}/{len(payload)}', flush=True)

    from collections import Counter
    by_v = Counter(r['verdict'] for r in results)

    # LOUD failure on a broken harness — silence is not success. If a meaningful share of cases never
    # reached the compiler, every downstream number is noise and must not be read as a finding.
    # ⛔ NO_COMPILER_EVIDENCE COUNTS AS A HARNESS FAILURE, NOT A VERDICT. It is a case whose compile produced
    # neither an artifact nor a reason; folding it into WE_REJECT_THEY_ACCEPT is precisely the A12e defect.
    # Each one is a case to RE-RUN, never a divergence to triage — and each is named, because a lost result
    # that nobody looks at is how the previous version of this block stayed quiet.
    NON_VERDICTS = ('RUNNER_ERROR', 'NO_COMPILER_EVIDENCE')
    errs = sum(by_v.get(v, 0) for v in NON_VERDICTS)
    if errs:
        pct = 100.0 * errs / max(1, len(results))
        print(f'\n!! {errs} of {len(results)} cases ({pct:.1f}%) PRODUCED NO COMPILER VERDICT '
              f'(harness, not compiler).')
        for v in NON_VERDICTS:
            named = [r for r in results if r['verdict'] == v]
            if not named:
                continue
            print(f'   {v} ({len(named)}):')
            for m, n in Counter(r['ourFirstError'] for r in named).most_common(5):
                print(f'     {n:5}  {m[:110]}')
            for r in named[:10]:
                print(f'     re-run: {r["file"]} :: {r["title"][:80]}')
        if pct > 5.0:
            print('!! ABORTING: >5% harness failures — the verdict counts below would be meaningless.',
                  file=sys.stderr)
            return 3
    by_vt = Counter((r['verdict'], r['tier']) for r in results)
    print(f'\ncases run: {len(results)}')
    for v, n in by_v.most_common():
        print(f'  {v:24} {n}')
    print('\nby confidence tier:')
    for (v, t), n in sorted(by_vt.items()):
        if v.startswith('WE_'):
            print(f'  {v:24} {t:16} {n}')

    os.makedirs(os.path.dirname(a.report), exist_ok=True)
    with open(a.report, 'w', encoding='utf-8') as fh:
        json.dump({'summary': {f'{v}': n for v, n in by_v.items()},
                   'byTier': {f'{v}|{t}': n for (v, t), n in by_vt.items()},
                   'cases': results}, fh, indent=1)
    print(f'\nreport -> {a.report}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
