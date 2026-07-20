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
    runner_error = ''
    try:
        r = subprocess.run([exe, os.path.join(d, os.path.basename(prim)), '--std', std, '-o', out_dll],
                           capture_output=True, text=True, timeout=60)
        we_ok = (r.returncode == 0)
        diag = r.stdout + r.stderr
    except subprocess.TimeoutExpired:
        we_ok, diag, runner_error = False, '<<TIMEOUT>>', 'TimeoutExpired'
    except Exception as e:                                    # noqa: BLE001
        we_ok, diag, runner_error = False, '', f'{type(e).__name__}: {e}'

    # A harness failure is NOT a compiler verdict. Laundering one into WE_REJECT_THEY_ACCEPT is how a
    # totally broken run reports as 1046 "compiler bugs" — which is exactly what happened on the first
    # run of this script (a relative exe path that Windows CreateProcess could not resolve).
    if runner_error:
        return {'id': gid, 'file': group['file'], 'title': group['title'][:160],
                'keywords': group['keywords'], 'std': std, 'tier': tier,
                'verdict': 'RUNNER_ERROR', 'ourCodes': [], 'ourFirstError': runner_error,
                'xfail': group.get('xfail', False)}

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
    errs = by_v.get('RUNNER_ERROR', 0)
    if errs:
        pct = 100.0 * errs / max(1, len(results))
        print(f'\n!! {errs} of {len(results)} cases ({pct:.1f}%) FAILED IN THE HARNESS, not the compiler.')
        for m, n in Counter(r['ourFirstError'] for r in results
                            if r['verdict'] == 'RUNNER_ERROR').most_common(5):
            print(f'   {n:5}  {m[:110]}')
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
