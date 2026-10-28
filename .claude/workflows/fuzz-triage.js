// The morning triage of a differential-fuzz night (kb/Work PB3972; docs/rearchitecture/DESIGN-differential-fuzz.md section 4).
//
// THIN BY DESIGN: every decision this script makes is scripts/fuzz_triage.py's, tested in scripts/test_fuzz_triage.py (the
// slices, the briefs, the verdict validation, the refute plan, the filing). The script only runs the read-only agents the
// tool prepared, a few at a time, each from its brief; it reads no file and chooses no slice.
//
//   1. python scripts/fuzz_triage.py prepare --run <run dir> [--judge <cobol.exe>]      -> <triage>/slices.json, briefs/
//   2. Workflow fuzz-triage  args { triage, adjudicate: [<slice names of slices.json>], concurrency }
//   3. python scripts/fuzz_triage.py collect --triage <triage>                           (validate the verdicts)
//   4. python scripts/fuzz_triage.py refute-plan --triage <triage>                       -> {"slices": ["R1", ...]}
//   5. Workflow fuzz-triage  args { triage, refute: [<those slice names>], concurrency }
//   6. python scripts/fuzz_triage.py file --triage <triage> [--write]                    (on a branch; the gate follows)
//
// Either list may be empty; the two phases may run in one invocation only when the refuter slices are already known
// (a resumed triage). `triage` is the absolute triage directory the tool printed.
export const meta = {
  name: 'fuzz-triage',
  description: 'Differential-fuzz triage: read-only adjudicator slices, then the independent refuters of the GnuCOBOL-side records; fixes and files nothing',
  whenToUse: 'The morning after a differential-fuzz night, after scripts/fuzz_triage.py prepare (and, for the refute phase, refute-plan)',
  phases: [{ title: 'Adjudicate' }, { title: 'Refute' }],
}

const AUTH = 'Authorization: the owner (Brent Rector) wrote on 2026-10-24 ~17:00 PDT in the attended operator session: "Also, run the fuzz analysis and report its findings. Do not begin fixing any fuzz identified issues without my explicit approval" (standing for every fuzz night; the owner started the night\'s run for the morning triage). This reading is yours; you fix nothing, file no kb/Work note and post nothing. '

const triage = args.triage
const adjudicate = args.adjudicate || []
const refute = args.refute || []
const concurrency = args.concurrency || 4
const results = {}

// A rolling pool: `concurrency` workers each take the next slice until none is left.
const pool = async (names, run) => {
  let next = 0
  const worker = async () => {
    while (next < names.length) {
      const s = names[next++]
      try {
        results[s] = { slice: s, report: await run(s) }
      } catch (e) {
        results[s] = { slice: s, error: String(e) }
      }
      log(`${Object.keys(results).length}/${adjudicate.length + refute.length} slices returned (${s})`)
    }
  }
  await parallel(Array.from({ length: Math.min(concurrency, names.length) }, () => worker))
}

if (adjudicate.length) {
  phase('Adjudicate')
  await pool(adjudicate, (s) => agent(
    AUTH + `You are adjudicator ${s}. Read and follow the brief ${triage}/briefs/adj-${s}.md exactly; it points at the common block (authorization, materials, verdict vocabulary, the GnuCOBOL-side filing bar, checkpoint and stop rules, turn cap) and allocates your verdicts file, probes directory, records and report path. You are READ-ONLY inside every git tree. Your final message is the whole report as text (at most 70 lines).`,
    { label: `adj:${s}`, phase: 'Adjudicate', agentType: 'cobol-adjudicator' }))
}

if (refute.length) {
  phase('Refute')
  await pool(refute, (s) => agent(
    AUTH + `You are refuter ${s}, independent of the adjudicators. Read and follow the brief ${triage}/briefs/refute-${s}.md exactly: re-run each record, try to overturn it, write the corrected copy of every record you uphold, and append your outcome lines. You are READ-ONLY inside every git tree. Your final message is the table the brief asks for (at most 40 lines).`,
    { label: `refute:${s}`, phase: 'Refute', agentType: 'cobol-refuter' }))
}

return results
