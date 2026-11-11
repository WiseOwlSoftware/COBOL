#!/usr/bin/env python3
"""A dry run of the spec-reconcile workflow (.claude/workflows/spec-reconcile.js) with stubbed agents (kb/Work PB5255).

The workflow's script body runs under node with `agent`, `pipeline` and `parallel` stubbed. The stub agents do what the
prompts tell a real one: a compare agent WRITES its compare file into a temporary edition directory and returns the
output of the real `merge_reconciliation.py --compare-summary <file>`; a verify agent runs the real
`merge_reconciliation.py --claim-key <file> <id>`, writes its verify file with the key it printed, and returns its
verdict. Scenarios make them misbehave as a real one can (return nothing, write an invalid file, die, retype its
findings with one more space, list a claim id its file does not hold). Each run's directory is then merged by the real
merge, so what the workflow reports is held to what the merge proves. The review rounds asked for each arm: the
result counts only what a valid compare FILE records and names every batch without one (R1-4, R3-1); a verdict
attaches only to its own claim (R1-2, R2-1) and its key comes from the FILE, never a return value (R3-1); edition
2023, whose transcription keeps no pages, is refused (R1-5).

    python scripts/spec/test_wf_spec_reconcile.py                        -> "=== SPEC-RECONCILE WORKFLOW DRY RUN: PASS ==="
    WF_SPEC_RECONCILE=<another spec-reconcile.js> python scripts/spec/test_wf_spec_reconcile.py   (another version)
"""
from __future__ import annotations

import os
import pathlib
import shutil
import subprocess
import sys
import tempfile

REPO = pathlib.Path(__file__).resolve().parents[2]
WORKFLOW = pathlib.Path(os.environ.get("WF_SPEC_RECONCILE") or REPO / ".claude" / "workflows" / "spec-reconcile.js")
MERGE = pathlib.Path(__file__).resolve().parent / "merge_reconciliation.py"

HARNESS = r"""
import fs from 'node:fs'
import path from 'node:path'
import { spawnSync } from 'node:child_process'
const { WF_SPEC_RECONCILE, PYTHON, MERGE_PY, ROOT } = process.env
const src = fs.readFileSync(WF_SPEC_RECONCILE, 'utf8').replace(/^export const meta/m, 'const meta')
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor
const body = new AsyncFunction('args', 'log', 'agent', 'pipeline', 'parallel', src)
const pipeline = async (items, s1, s2) => Promise.all(items.map(async (it, i) => s2(await s1(it, it, i), it, i)))
const parallel = async (fns) => Promise.all(fns.map(f => f()))
let failures = 0, n = 0
const check = (name, ok) => { console.log((ok ? '  ok    ' : '  FAIL  ') + name); if (!ok) failures++ }
const py = (...a) => spawnSync(PYTHON, [MERGE_PY, ...a], { encoding: 'utf8' })
const write = (f, o) => { fs.mkdirSync(path.dirname(f), { recursive: true }); fs.writeFileSync(f, JSON.stringify(o)) }

// One run in its own checkout-shaped directory. `fileOf(batch)` is what the compare agent writes (null: nothing);
// `ret(summary, batch)` is what it returns (default: the summary command's output, as the prompt says); `verdictOf(claim)`
// is the verifier's verdict (null: it dies).
async function run(scenario) {
  const root = path.join(ROOT, 'r' + (++n), 'docs', 'rearchitecture', 'spec-reconciliation')
  const out = path.join(root, scenario.dir || '2002').replace(/\\/g, '/')
  fs.mkdirSync(out, { recursive: true })
  const args = { pages: '1-3,10-11', edition: 2002, model: 'opus', effort: 'medium', outDir: out, workDir: path.join(ROOT, 'scratch' + n), ...(scenario.args || {}) }
  const agent = async (prompt, opt) => {
    if (opt.phase === 'Compare') {
      const batch = JSON.parse(prompt.match(/"batch": (\[[^\]]*\])/)[1])
      const file = prompt.match(/--compare-summary (\S+)/)[1]
      const doc = (scenario.fileOf || clean)(batch)
      if (doc) write(file, doc)
      const r = py('--compare-summary', file)
      const summary = r.stdout.trim() ? JSON.parse(r.stdout) : null
      return (scenario.ret || ((s) => s))(summary, batch)
    }
    const [, file, id] = prompt.match(/--claim-key (\S+) (\S+)/)
    const r = py('--claim-key', file, id)
    if (r.status !== 0) return { claim_id: id, failure: r.stderr.trim() }
    const got = JSON.parse(r.stdout)
    const v = (scenario.verdictOf || (() => ({ real: false, reasoning: 'present' })))(got.claim)
    if (!v) return null
    write(path.join(path.dirname(file), 'verify-' + id + '.json'), { claim_id: id, claim_key: got.claim_key, page: got.claim.page, kind: got.claim.kind, claim: got.claim, verdict: v })
    return { claim_id: id, claim_key: got.claim_key, kind: got.claim.kind, severity: got.claim.severity, ...v }
  }
  let result
  try { result = await body(args, () => {}, agent, pipeline, parallel) } catch (e) { result = { thrown: e.message } }
  const merged = py('--edition', '2002', '--root', root)
  return { r: result, merge: { rc: merged.status, out: merged.stdout } }
}
const clean = (batch) => ({ batch, pages_checked: batch, pages_unread: [], findings: [] })
const finding = (id, page, said) => ({ id, page, kind: 'incorrect-figure', severity: 'normative', pdf_says: said, markdown_says: 'm', why_it_matters: 'w' })
const twoClaims = (b) => b[0] === 1 ? { ...clean(b), findings: [finding('p2-1', 2, 'a'), finding('p2-2', 2, 'b')] } : clean(b)
const problemsOf = (r) => ((r.batches_without_valid_compare || [])[0] || { problems: [] }).problems.join(' ')

let { r, merge } = await run({ dir: 'agents', args: { edition: 2023 } })
check('edition 2023 is refused, naming why: its transcription keeps no page headings', /2023.*no `## Page N` headings/.test(r.thrown || ''))
;({ r, merge } = await run({}))
check('a clean run is ok and reports the pages its files record; the merge agrees', r.ok === true && r.pages_checked === 5 && merge.rc === 0)
;({ r, merge } = await run({ fileOf: (b) => b[0] === 10 ? null : clean(b), ret: (s, b) => b[0] === 10 ? null : s }))
check('a batch whose agent wrote and returned nothing is named, its pages not counted, and the run is not ok',
  r.ok === false && r.pages_checked === 3 && (r.batches_without_valid_compare[0] || {}).batch === 'p10-p11')
;({ r, merge } = await run({ fileOf: (b) => b[0] === 1 ? { ...clean(b), pages_checked: [] } : clean(b) }))
check('a compare FILE with pages_checked [] is a batch with no valid record, named with the merge\'s problem',
  r.ok === false && r.pages_checked === 2 && /neither checked nor unread/.test(problemsOf(r)))
;({ r, merge } = await run({ fileOf: (b) => b[0] === 1 ? { ...clean(b), pages_checked: [1, 2], pages_unread: [{ page: 3, reason: 'blank render' }] } : clean(b) }))
check('an unread page is reported with its reason, not counted checked, and the run is not ok',
  r.ok === false && r.pages_checked === 4 && ((r.pages_unread || [])[0] || {}).page === 3)
;({ r, merge } = await run({ fileOf: twoClaims, verdictOf: (c) => c.id === 'p2-1' ? { real: false, reasoning: 'present' } : null }))
check('a claim whose verifier died is unverified by its id, never refuted by its neighbour',
  r.ok === false && r.refuted === 1 && String(r.unverified) === 'p2-2')
// R3-1: the agent's return retypes its findings with one more space; nothing the run uses comes from it.
;({ r, merge } = await run({ fileOf: twoClaims, ret: (s) => s && { ...s, findings: [finding('p2-1', 2, 'a '), finding('p2-2', 2, 'b ')] } }))
check('a return that differs from its file by one space changes nothing: each verifier gets the FILE\'s key, and the merge attaches every verdict',
  r.refuted === 2 && r.unverified.length === 0 && merge.rc === 0 && /refuted\s+: 2/.test(merge.out))
;({ r, merge } = await run({ fileOf: twoClaims, ret: (s, b) => s && b[0] === 1 ? { ...s, claims: ['p2-1', 'p2-9'] } : s }))
check('a claim id the file does not hold fails the verifier\'s command by name, and the claim is unverified with that reason',
  r.ok === false && r.refuted === 1 && r.unverified.length === 1 && r.unverified[0].id === 'p2-9'
  && /holds no claim p2-9/.test(r.unverified[0].failure))
;({ r, merge } = await run({ ret: (s, b) => s && b[0] === 1 ? { ...s, file: 'compare-p10-p11.json' } : s }))
check('a summary of another file is no record of this batch', r.ok === false && /not compare-p1-p3.json/.test(problemsOf(r)))
;({ r, merge } = await run({ fileOf: (b) => b[0] === 1 ? { ...clean(b), findings: [finding('p2-1', 2, 'a'), finding('p2-1', 2, 'b')] } : clean(b) }))
check('findings misnumbered on their page make the batch invalid, by the merge\'s rule',
  r.ok === false && /numbered \[1, 1\]/.test(problemsOf(r)))
console.log('=== SPEC-RECONCILE WORKFLOW DRY RUN: ' + (failures ? 'FAIL (' + failures + ')' : 'PASS') + ' ===')
process.exit(failures ? 1 : 0)
"""


def main() -> int:
    node = shutil.which("node")
    if node is None:
        print("=== SPEC-RECONCILE WORKFLOW DRY RUN: FAIL (node is not on PATH; the dry run needs it) ===")
        return 1
    with tempfile.TemporaryDirectory() as tmp:
        run = subprocess.run([node, "--input-type=module", "-"], input=HARNESS, text=True, encoding="utf-8",
                             env={**os.environ, "WF_SPEC_RECONCILE": str(WORKFLOW), "PYTHON": sys.executable,
                                  "MERGE_PY": str(MERGE), "ROOT": tmp})
    return run.returncode


if __name__ == "__main__":
    sys.exit(main())
