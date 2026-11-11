#!/usr/bin/env python3
"""A dry run of the spec-reconcile workflow (.claude/workflows/spec-reconcile.js) with stubbed agents (kb/Work PB5255).

The workflow's script body runs under node with `agent`, `pipeline` and `parallel` stubbed: each scenario makes the
compare and verify agents return what a real one can (a clean batch, nothing at all, an empty pages_checked, an unread
page, a dead verifier, a verdict echoing the wrong claim, misnumbered findings) and checks what the run reports. The
review of round 1 asked for each: the result counts only what a VALID compare result proves and names every batch
without one (R1-4); a verdict attaches only to the claim whose id it echoes (R1-2); edition 2023, whose transcription
keeps no pages, is refused (R1-5).

    python scripts/spec/test_wf_spec_reconcile.py                        -> "=== SPEC-RECONCILE WORKFLOW DRY RUN: PASS ==="
    WF_SPEC_RECONCILE=<another spec-reconcile.js> python scripts/spec/test_wf_spec_reconcile.py   (another version)
"""
from __future__ import annotations

import json
import os
import pathlib
import shutil
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from merge_reconciliation import claim_key  # noqa: E402  (the merge's half of the one claim-key function)
WORKFLOW = pathlib.Path(os.environ.get("WF_SPEC_RECONCILE") or REPO / ".claude" / "workflows" / "spec-reconcile.js")

HARNESS = r"""
import fs from 'node:fs'
const src = fs.readFileSync(process.env.WF_SPEC_RECONCILE, 'utf8').replace(/^export const meta/m, 'const meta')
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const body = new AsyncFunction('args', 'log', 'agent', 'pipeline', 'parallel', src)
const pipeline = async (items, s1, s2) => Promise.all(items.map(async (it, i) => s2(await s1(it, it, i), it, i)))
const parallel = async (fns) => Promise.all(fns.map(f => f()))
const base = { pages: '1-3,10-11', edition: 2002, model: 'opus', effort: 'medium',
  outDir: 'E:/W/docs/rearchitecture/spec-reconciliation/2002', workDir: 'E:/Temp/x' }
let failures = 0
const check = (name, ok) => { console.log((ok ? '  ok    ' : '  FAIL  ') + name); if (!ok) failures++ }
async function run(argv, compare, verify) {
  const agent = async (prompt, opt) => opt.phase === 'Compare'
    ? compare(JSON.parse(prompt.match(/"batch": (\[[^\]]*\])/)[1]))
    : verify(prompt.match(/CLAIM (\S+)/)[1], (prompt.match(/claim_key exactly "([0-9a-f]+)"/) || [])[1])
  try { return await body(argv, () => {}, agent, pipeline, parallel) } catch (e) { return { thrown: e.message } }
}
const clean = (batch) => ({ batch, pages_checked: batch, pages_unread: [], findings: [] })
const verdict = (id, key) => ({ claim_id: id, claim_key: key, real: false, reasoning: 'present' })
const finding = (id, page, said) => ({ id, page, kind: 'incorrect-figure', severity: 'normative', pdf_says: said,
  markdown_says: 'm', why_it_matters: 'w' })
const twoClaims = (b) => b[0] === 1 ? { ...clean(b), findings: [finding('p2-1', 2, 'a'), finding('p2-2', 2, 'b')] } : clean(b)

let r = await run({ ...base, edition: 2023, outDir: 'E:/W/docs/rearchitecture/spec-reconciliation/agents' }, clean, verdict)
check('edition 2023 is refused, naming why: its transcription keeps no page headings',
  /2023.*no `## Page N` headings/.test(r.thrown || ''))
r = await run(base, clean, verdict)
check('a clean run is ok and reports the pages its agents checked',
  r.ok === true && r.pages_checked === 5 && r.batches_without_valid_compare.length === 0)
r = await run(base, (b) => (b[0] === 10 ? null : clean(b)), verdict)
check('a batch whose agent returned nothing is named, its pages are not counted, and the run is not ok',
  r.ok === false && r.pages_checked === 3 && r.pages_requested === 5
  && (r.batches_without_valid_compare[0] || {}).batch === 'p10-p11')
r = await run(base, (b) => (b[0] === 1 ? { ...clean(b), pages_checked: [] } : clean(b)), verdict)
check('a compare result with pages_checked [] is a batch with no valid result, never swept',
  r.ok === false && r.pages_checked === 2
  && /neither checked nor unread/.test(((r.batches_without_valid_compare || [])[0] || { problems: [] }).problems.join()))
r = await run(base, (b) => (b[0] === 1 ? { ...clean(b), pages_checked: [1, 2], pages_unread: [{ page: 3, reason: 'blank render' }] } : clean(b)), verdict)
check('an unread page is reported with its reason, not counted checked, and the run is not ok',
  r.ok === false && r.pages_checked === 4 && ((r.pages_unread || [])[0] || {}).page === 3)
r = await run(base, twoClaims, (id, key) => (id === 'p2-1' ? verdict(id, key) : null))
check('a claim whose verifier died is unverified by its id, never refuted by its neighbour',
  r.ok === false && r.refuted === 1 && String(r.unverified) === 'p2-2')
r = await run(base, twoClaims, (id, key) => verdict('p2-1', key))
check("a verdict echoing another claim's id attaches to nothing", r.refuted === 1 && String(r.unverified) === 'p2-2')
r = await run(base, twoClaims, (id, key) => verdict(id, id === 'p2-2' ? '0123456789abcdef' : key))
check("a verdict echoing its id with another claim's key attaches to nothing",
  r.refuted === 1 && String(r.unverified) === 'p2-2')
// The key the workflow hands a verifier is merge_reconciliation.claim_key of the claim: Python checks the printed pairs.
const keyed = [finding('p2-1', 2, 'ASCII'), { ...finding('p2-2', 2, 'é ü — “bars” ║ 𝔘 \u001f'), markdown_says: 'line\none' }]
const seen = []
await run(base, (b) => b[0] === 1 ? { ...clean(b), findings: keyed } : clean(b), (id, key) => { seen.push({ id, key }); return verdict(id, key) })
console.log('KEYS ' + JSON.stringify(keyed.map(f => ({ claim: f, key: (seen.find(s => s.id === f.id) || {}).key }))))
r = await run(base, (b) => b[0] === 1 ? { ...clean(b), findings: [finding('p2-1', 2, 'a'), finding('p2-1', 2, 'b')] } : clean(b), verdict)
check('findings misnumbered on their page make the batch invalid',
  r.ok === false && /should be p2-2/.test(((r.batches_without_valid_compare || [])[0] || { problems: [] }).problems.join()))
console.log('=== SPEC-RECONCILE WORKFLOW DRY RUN: ' + (failures ? 'FAIL (' + failures + ')' : 'PASS') + ' ===')
process.exit(failures ? 1 : 0)
"""


def main() -> int:
    node = shutil.which("node")
    if node is None:
        print("=== SPEC-RECONCILE WORKFLOW DRY RUN: FAIL (node is not on PATH; the dry run needs it) ===")
        return 1
    run = subprocess.run([node, "--input-type=module", "-"], input=HARNESS, text=True, encoding="utf-8",
                         capture_output=True, env={**os.environ, "WF_SPEC_RECONCILE": str(WORKFLOW)})
    lines = run.stdout.splitlines()
    pairs = [json.loads(ln[5:]) for ln in lines if ln.startswith("KEYS ")]
    verdict = lines.pop() if lines and lines[-1].startswith("===") else None
    for ln in lines:
        if not ln.startswith("KEYS "):
            print(ln)
    sys.stderr.write(run.stderr)
    # The workflow's claimKey and the merge's claim_key are one function written twice; this holds them equal.
    same = bool(pairs) and all(p.get("key") == claim_key(p["claim"]) for p in pairs[0])
    print(("  ok    " if same else "  FAIL  ") + "the key the workflow hands each verifier is merge_reconciliation."
          "claim_key of its claim (non-ASCII, astral and control characters among them)")
    ok = run.returncode == 0 and same and verdict is not None
    print(f"=== SPEC-RECONCILE WORKFLOW DRY RUN: {'PASS' if ok else 'FAIL'} ===")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
