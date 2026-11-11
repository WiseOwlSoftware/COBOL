export const meta = {
  name: 'spec-reconcile',
  description: 'Compare the canonical standard PDF against its markdown transcription (an edition that keeps its pages: 1985 or 2002) page by page and ledger every discrepancy',
  whenToUse: 'Reconciling a transcription against its canonical PDF. args {pages, edition, model, effort, outDir, workDir}, every one required: pages as an array or ranges ("1-12,37-48", for 2002 a run of docs/rearchitecture/spec-reconciliation/2002/ranges.json); outDir the edition directory in the worktree of the run\'s branch; workDir a scratch directory of this run alone. See docs/rearchitecture/spec-reconciliation/README.md.',
  phases: [
    { title: 'Compare', detail: 'one agent per batch of up to 12 consecutive pages: read the rendered PDF page, diff against the markdown slice' },
    { title: 'Verify', detail: 'adversarially re-check each claimed discrepancy against the same page, one agent per claim' },
  ],
}

// THE ARGUMENTS (kb/Work PB5255). Every one is REQUIRED and checked before any agent starts; a missing, malformed or
// unknown one throws naming it. No default stands in for one: a default page list once swept pages nobody asked for, a
// default output directory wrote into the MAIN checkout whatever worktree ran the workflow, a shared scratch directory
// let two fleets on one machine overwrite each other's renders, and an unpinned model ran whatever the host defaulted to.
//   pages    an array of PDF page numbers, or a string of pages and ranges "3-65,67,70-72". ALWAYS a COMPUTED list,
//            never a typed one: a hand-transcribed list cost a whole 4.3M-token sweep (64 of 173 target pages covered,
//            105 others swept, and the run still reported "169 pages swept"). For 2002 pass a run's `pages` from
//            docs/rearchitecture/spec-reconciliation/2002/ranges.json (scripts/spec/reconcile_ranges.py). The proof of
//            what was swept is the files on disk: `merge_reconciliation.py --edition <e>` validates them and names every
//            page no valid compare file covers. RE-RUNNING a batch replaces its earlier files as a unit, and the run on
//            disk wins: delete the batch's compare and verify files first. This script cannot see the disk, so it
//            never deletes; the merge refuses an earlier run's verdict whose claim_key the new claim does not share
//            (README, "Re-running a batch").
//   edition  1985 or 2002: a row of EDITIONS below.
//   model    the model of every agent (the owner's decision for 2002, 2026-11-11: Opus).
//   effort   the reasoning effort of every agent: low, medium, high, xhigh or max (2002: medium).
//   outDir   where every agent writes its file: the edition's directory docs/rearchitecture/spec-reconciliation/<dir>
//            INSIDE the worktree of the run's branch. The checkout the work units are built from is the one outDir
//            lies in, so the agents compare the PDF with that branch's transcription.
//   workDir  this run's own scratch directory for rendered pages (e.g. E:/Temp/specrecon-2002-account1-r01).
// Scripts have no filesystem access, so the arguments are checked for shape here; the files they name are the agents'.
const KNOWN = ['pages', 'edition', 'model', 'effort', 'outDir', 'workDir']
const EFFORTS = ['low', 'medium', 'high', 'xhigh', 'max']
if (!args || typeof args !== 'object' || Array.isArray(args)) {
  throw new Error('spec-reconcile: args must be an object {' + KNOWN.join(', ') + '}; got ' + JSON.stringify(args))
}
const unknown = Object.keys(args).filter(k => !KNOWN.includes(k))
if (unknown.length) throw new Error('spec-reconcile: unknown argument(s) ' + unknown.join(', ') + ' (known: ' + KNOWN.join(', ') + ')')
for (const k of KNOWN) {
  if (args[k] === undefined || args[k] === null || args[k] === '') throw new Error('spec-reconcile: argument ' + k + ' is required')
}

// What the prompts say about each edition, keyed by edition (scripts/spec/spec_pdf.py EDITIONS holds the files; the
// transcription's path is never spelled here: page_workunit.py --edition slices the edition's own Markdown). Each row
// cites the general-format notation of ITS OWN conventions clauses, each citation checked with scripts/spec/cite.py
// --check, so a figure is judged by the notation of the standard it is in. An edition with no row is an error.
// 2023 HAS NO ROW (kb/Work PB5255 review R1-5): its transcription dropped its `## Page N` headings, so
// page_workunit.py has no page slice to give an agent and the comparison this workflow asks for cannot be made. Its
// sweep is complete (spec-reconciliation/LEDGER.json); a re-run would first need a page-keyed 2023 transcription.
const EDITIONS = {
  1985: {
    standard: 'ANSI X3.23-1985', dir: '1985', pages: 976,
    keyword: '§1985:IV.2.1.2 (Words): underlined uppercase words are key words, required; uppercase words not underlined are optional',
    choice: '§1985:IV.2.1.4 (Brackets, Braces, and Choice Indicators): choice indicators {| |} are bars just inside BRACES; one or more of the unique options they enclose must be specified, each at most once. Without the bars, braces mean exactly one of the stacked options, explicitly specified or implicitly selected, and brackets at most one',
    ellipsis: '§1985:IV.2.1.5 (Ellipsis)',
  },
  2002: {
    standard: 'ISO/IEC 1989:2002', dir: '2002', pages: 882,
    keyword: '§2002:5.1.1 (Keywords): keywords are shown in uppercase and underlined, and are required; optional words are uppercase and not underlined (§2002:5.1.2)',
    choice: '§2002:5.1.5.3 (Choice indicators): a pair of bars just inside a bracket or brace. Inside BRACES, one or more of the enclosed alternatives shall be specified, each only once; inside BRACKETS, zero or more, each at most once; in ANY ORDER. Without the bars, a bracket around stacked alternatives means AT MOST ONE of them (§2002:5.1.5.1) and a brace EXACTLY ONE, explicitly specified or implicitly selected (§2002:5.1.5.2)',
    ellipsis: '§2002:5.1.6 (Ellipses)',
  },
}
const EDITION = Number(args.edition)
const ROW = EDITIONS[EDITION]
if (!ROW) {
  throw new Error('spec-reconcile: edition ' + JSON.stringify(args.edition) + ' has no row (rows: ' + Object.keys(EDITIONS).join(', ') + ')' +
    (EDITION === 2023 ? '; the 2023 transcription keeps no `## Page N` headings, so there is no page slice to compare' : ''))
}
const STANDARD = ROW.standard

if (typeof args.model !== 'string') throw new Error('spec-reconcile: argument model must be a string naming the model')
const MODEL = args.model
if (!EFFORTS.includes(args.effort)) throw new Error('spec-reconcile: argument effort must be one of ' + EFFORTS.join(', ') + '; got ' + JSON.stringify(args.effort))
const EFFORT = args.effort

// outDir is the edition's directory inside a checkout; the checkout is what precedes it.
const SUFFIX = '/docs/rearchitecture/spec-reconciliation/' + ROW.dir
const OUT = String(args.outDir).replace(/\\/g, '/').replace(/\/+$/, '')
if (!OUT.endsWith(SUFFIX) || OUT.length === SUFFIX.length) {
  throw new Error('spec-reconcile: argument outDir must be <the run\'s worktree>' + SUFFIX + ' for edition ' + EDITION + '; got ' + JSON.stringify(args.outDir))
}
const CHECKOUT = OUT.slice(0, OUT.length - SUFFIX.length)
const LEDGER = OUT + '/'
const SCRATCH = String(args.workDir).replace(/\\/g, '/').replace(/\/+$/, '')
if (SCRATCH.startsWith(CHECKOUT + '/')) throw new Error('spec-reconcile: argument workDir must lie outside the checkout (renders are not tracked); got ' + JSON.stringify(args.workDir))
const WORKUNIT = 'cd ' + CHECKOUT + ' && python scripts/spec/page_workunit.py'

function toPages(a) {
  const bad = (what) => { throw new Error('spec-reconcile: argument pages: ' + what) }
  let out = []
  if (Array.isArray(a)) out = a.map(n => (Number.isInteger(n) ? n : bad('not a page number: ' + JSON.stringify(n))))
  else if (typeof a === 'string') {
    for (const tok of a.split(/[,\s]+/)) {
      if (!tok) continue
      const r = tok.match(/^(\d+)-(\d+)$/)
      if (r) {
        const lo = Number(r[1]), hi = Number(r[2])
        if (hi < lo) bad('a range runs backwards: ' + tok)
        for (let n = lo; n <= hi; n++) out.push(n)
      } else if (/^\d+$/.test(tok)) out.push(Number(tok))
      else bad('not a page or a range: ' + JSON.stringify(tok))
    }
  } else bad('an array or a string of pages and ranges; got ' + JSON.stringify(a))
  if (!out.length) bad('no pages')
  const outside = out.filter(n => n < 1 || n > ROW.pages)
  if (outside.length) bad('outside 1..' + ROW.pages + ' of the ' + EDITION + ' PDF: ' + outside.slice(0, 10).join(', '))
  if (new Set(out).size !== out.length) bad('a page is given twice')
  return out
}
const PAGES = toPages(args.pages)

// A batch is up to BATCH CONSECUTIVE pages: a gap in the list starts a new batch, so a run of the 2002 plan
// ("1-12,37-48,...") is cut exactly at its batches and every file is named after the batch the plan names.
const BATCH = 12
const batches = []
for (const p of PAGES) {
  const last = batches[batches.length - 1]
  if (last && last.length < BATCH && p === last[last.length - 1] + 1) last.push(p)
  else batches.push([p])
}
const span = (b) => 'p' + b[0] + '-p' + b[b.length - 1]

log(`Reconciling ${STANDARD}: ${PAGES.length} pages in ${batches.length} batches of up to ${BATCH}; model ${MODEL}, effort ${EFFORT}`)
// Echo the actual boundaries so a mis-transcribed list is visible AT LAUNCH, not after the tokens are spent.
log(`first=${PAGES[0]} last=${PAGES[PAGES.length - 1]} distinct=${new Set(PAGES).size} batches=${batches.map(b => b[0] + '-' + b[b.length - 1]).join(',')}`)
log(`writes ${LEDGER} · renders in ${SCRATCH}`)

// The compare agent's file and its return value are ONE object of this shape (the prompt says so), so the script can
// hold the return to the rules merge_reconciliation.py holds the file to (kb/Work PB5255 review round 1).
const FINDINGS = {
  type: 'object',
  properties: {
    batch: { type: 'array', items: { type: 'integer' } },
    pages_checked: { type: 'array', items: { type: 'integer' } },
    pages_unread: {
      type: 'array',
      items: { type: 'object', properties: { page: { type: 'integer' }, reason: { type: 'string' } }, required: ['page', 'reason'] },
    },
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          id: { type: 'string', pattern: '^p[0-9]+-[1-9][0-9]*$' },
          page: { type: 'integer' },
          kind: { type: 'string', enum: ['missing-text', 'incorrect-text', 'misplaced-content', 'missing-figure', 'incorrect-figure', 'missing-heading', 'ocr-error', 'summarised-not-transcribed', 'other'] },
          severity: { type: 'string', enum: ['normative', 'structural', 'cosmetic'] },
          pdf_says: { type: 'string' },
          markdown_says: { type: 'string' },
          why_it_matters: { type: 'string' },
          suggested_repair: { type: 'string' },
        },
        required: ['id', 'page', 'kind', 'severity', 'pdf_says', 'markdown_says', 'why_it_matters'],
      },
    },
  },
  required: ['batch', 'pages_checked', 'pages_unread', 'findings'],
}

// A claim's CONTENT identity, the same function as merge_reconciliation.claim_key (kb/Work PB5255 review R2-1;
// test_wf_spec_reconcile.py holds the two equal): two 32-bit FNV-1a hashes over the UTF-16 code units of the page and
// every text field joined by U+001F. An id is reused when a batch is re-run, so the verifier copies this key into its
// file and the merge attaches the verdict only to a claim with the same key. Agents retype long quotes inexactly, so the
// key is copied, never the text compared.
const CLAIM_TEXT = ['kind', 'pdf_says', 'markdown_says', 'why_it_matters']
function claimKey(f) {
  const text = [String(f.page)].concat(CLAIM_TEXT.map(k => String(f[k]))).join('\u001f')
  let out = ''
  for (let h of [0x811c9dc5, 0x050c5d1f]) {
    for (let i = 0; i < text.length; i++) h = Math.imul((h ^ text.charCodeAt(i)) >>> 0, 0x01000193) >>> 0
    out += h.toString(16).padStart(8, '0')
  }
  return out
}

const VERDICT = {
  type: 'object',
  properties: {
    claim_id: { type: 'string' },
    claim_key: { type: 'string' },
    real: { type: 'boolean' },
    reasoning: { type: 'string' },
    corrected_description: { type: 'string' },
  },
  required: ['claim_id', 'claim_key', 'real', 'reasoning'],
}

// What makes a compare result untrustworthy: the merge's rules (merge_reconciliation.compare_problems), so a batch the
// merge would refuse is named at the end of THIS run, not discovered at the merge. Nothing missing is defaulted.
function compareProblems(res, batch) {
  if (!res) return ['the agent returned nothing']
  const out = []
  const checked = Array.isArray(res.pages_checked) ? res.pages_checked : (out.push('no pages_checked'), [])
  const unread = Array.isArray(res.pages_unread) ? res.pages_unread : (out.push('no pages_unread'), [])
  if (JSON.stringify(res.batch) !== JSON.stringify(batch)) out.push('batch ' + JSON.stringify(res.batch) + ' is not ' + JSON.stringify(batch))
  if (unread.some(u => !u || !Number.isInteger(u.page) || typeof u.reason !== 'string' || !u.reason.trim())) out.push('an unread page has no reason')
  const unreadPages = unread.map(u => u && u.page)
  const all = checked.concat(unreadPages)
  const outside = all.filter(p => !batch.includes(p))
  if (outside.length) out.push('page(s) ' + outside.join(', ') + ' outside the batch')
  if (new Set(all).size !== all.length) out.push('a page is listed twice, or both checked and unread')
  const neither = batch.filter(p => !all.includes(p))
  if (neither.length) out.push('page(s) ' + neither.join(', ') + ' neither checked nor unread')
  const n = {}
  for (const f of res.findings || []) {
    if (!checked.includes(f.page)) out.push('finding ' + f.id + ' is on page ' + f.page + ', which was not checked')
    n[f.page] = (n[f.page] || 0) + 1
    if (f.id !== 'p' + f.page + '-' + n[f.page]) out.push('finding ' + f.id + ' should be p' + f.page + '-' + n[f.page])
  }
  if (!Array.isArray(res.findings)) out.push('no findings list')
  return out
}

const results = await pipeline(
  batches,
  (batch, _orig, i) => agent(
    [
      'You are reconciling the canonical ' + STANDARD + ' PDF against its markdown transcription (the ' + EDITION,
      'edition; the work unit below slices it for you). Pages in YOUR batch: ' + batch.join(', ') + '. Work ONLY these pages.',
      '',
      'STEP 1 - build your work units (renders the PDF pages and extracts the matching markdown slices):',
      '  ' + WORKUNIT + ' ' + batch.join(' ') + ' --edition ' + EDITION + ' --out ' + SCRATCH + '/f' + batch[0] + ' --dpi 300',
      'Its last three lines name your HEADING-ONLY pages (the transcription holds nothing but `## Page N` for them:',
      'if the printed page is not blank, that is a missing-text finding), your FIGURE pages (a general format is drawn',
      'on them) and your PROSE pages. If it exits with an error, compare nothing: list every page of the batch in',
      'pages_unread with the error as the reason, write the file (STEP 3) and return.',
      '',
      'WHERE THE PRIORITY LIES. On a FIGURE page, diagram fidelity comes first: check every general format as below,',
      'then the prose. On a PROSE page, the prose checks below are the whole job. Every page gets a complete comparison.',
      '',
      'STEP 2 - for EACH page P in your batch:',
      '  a) Read the image ' + SCRATCH + '/f' + batch[0] + '/spec_pP.png (the Read tool renders it visually)',
      '  b) Read the markdown ' + SCRATCH + '/f' + batch[0] + '/page-P.md',
      '  c) Compare them carefully and completely.',
      'A page you could not read or compare completely (the image did not render or is unreadable, the markdown is',
      'missing, you ran out of room) goes in pages_unread with the reason, NEVER in pages_checked: pages_checked is your',
      'statement that you compared the page, and a page listed there with no findings is recorded as clean.',
      '',
      'FIGURES - judge each general format by THIS edition\'s notation, which its own conventions clauses state:',
      '  - ' + ROW.keyword + '.',
      '  - ' + ROW.choice + '.',
      '  - ' + ROW.ellipsis + ': the ellipsis binds to the right bracket or brace immediately to its LEFT and repeats the',
      '    portion back to the matching left delimiter, so misplacing it changes what repeats.',
      'THE PRIORITY DEFECT CLASS on a figure page is a lost CHOICE INDICATOR. A transcription loses those bars toward',
      'falsely restrictive syntax - legal source made to look illegal (the 2023 transcription\'s GOBACK format lost its',
      'bars, so it said "raising-phrase OR status-phrase" where the standard permits BOTH). For every figure, look at the',
      'printed glyphs closely (crop/zoom if needed): is there a bar just inside the bracket or brace, and does the',
      'markdown keep it? ALSO CHECK: a brace or bracket that OPENS but never CLOSES; an ellipsis on the wrong side of a',
      'delimiter; underlining lost or added (a keyword made optional, or the reverse); a figure notes paragraph that',
      'contradicts what the diagram shows.',
      '',
      'PROSE - on every page:',
      '  - missing-text: normative text printed in the PDF but absent from the markdown',
      '  - incorrect-text: present but WRONG - altered wording, numbers, operators, cross-references',
      '  - misplaced-content: present but in the wrong place (wrong section, wrong order, a label attached to the',
      '    previous item, a sub-item detached from its parent)',
      '  - missing-heading: a heading absent, or demoted to bold text, or at the wrong level',
      '  - ocr-error: a character-level error that changes meaning',
      '  - summarised-not-transcribed: the markdown DESCRIBES the page instead of reproducing it - third-person',
      '    prose ("the introduction explains that..."), a bulleted digest, or any transcription-tool meta-text left in',
      '    the file. Report it as NORMATIVE whenever the page carries normative content: a summary READS like content,',
      '    so nothing else surfaces the hole. If a page talks ABOUT the standard rather than BEING it, say so plainly.',
      '  - a table, list or image the page prints that the markdown lost or restructured',
      '',
      'WHAT IS NOT A DISCREPANCY - do not report these:',
      '  - The page scaffolding the markdown adds: the "## Page N" heading and the editorial notes the transcription',
      '    marks as its own. The running header (' + STANDARD + '), the folio, the copyright footer and the reseller\'s',
      '    per-copy stamp line are removed on purpose; their absence is not a finding, and NEVER quote the stamp line.',
      '  - Reflowed line breaks or hyphenation differences from PDF justification.',
      '  - Markdown formatting choices that preserve meaning (a drawn figure instead of the printed glyphs is fine IF no',
      '    information is lost).',
      '',
      'Quote both sources VERBATIM in pdf_says / markdown_says so a verifier can check you without re-reading the',
      'page. Reporting a non-issue is worse than missing one - a false finding sends the next session to "fix" correct',
      'text.',
      '',
      'EACH FINDING HAS AN id: "p<page>-<n>", n counting THAT PAGE\'s findings from 1 in the order you list them (the',
      'first finding on page 40 is "p40-1", the second "p40-2", the first on page 41 "p41-1"). Its verifier names it by',
      'this id, and a finding without a correct id is refused by the merge.',
      '',
      'STEP 3 - PERSIST BEFORE YOU RETURN. This is MANDATORY, not a nicety. Use the Write tool to save your',
      'complete result as JSON to:',
      '  ' + LEDGER + 'compare-' + span(batch) + '.json',
      'Shape: {"batch": ' + JSON.stringify(batch) + ', "pages_checked": [...], "pages_unread": [{"page": P, "reason": "..."}],',
      '        "findings": [ {"id": "p<page>-<n>", "page": ..., ...} ]}',
      'Every page of the batch is in exactly one of pages_checked and pages_unread. Return EXACTLY the object you wrote.',
      'Write it EVEN IF you found nothing (findings: []) - a file proving a batch was swept clean is evidence;',
      'its absence is indistinguishable from work that never ran. The run may be interrupted by a rate or session',
      'limit at any moment, and your file is then the ONLY durable record of this work. Write it, then return.',
    ].join('\n'),
    { label: 'compare:' + span(batch), phase: 'Compare', schema: FINDINGS, model: MODEL, effort: EFFORT }
  ),
  (res, batch, i) => {
    const problems = compareProblems(res, batch)
    if (problems.length) return { batch, problems, claims: [] }
    if (res.findings.length === 0) return { batch, res, claims: [] }
    return parallel(res.findings.map((f) => () =>
      agent(
        [
          'Adversarially VERIFY a claimed discrepancy between the ' + STANDARD + ' PDF and its markdown transcription.',
          '',
          'CLAIM ' + f.id + ' - page ' + f.page + ', kind=' + f.kind + ', severity=' + f.severity,
          '  PDF allegedly says      : ' + f.pdf_says,
          '  Markdown allegedly says : ' + f.markdown_says,
          '  Why it allegedly matters: ' + f.why_it_matters,
          '',
          'Check it yourself:',
          '  ' + WORKUNIT + ' ' + f.page + ' --edition ' + EDITION + ' --out ' + SCRATCH + '/v-' + f.id + ' --dpi 300',
          'Then Read ' + SCRATCH + '/v-' + f.id + '/spec_p' + f.page + '.png and ' + SCRATCH + '/v-' + f.id + '/page-' + f.page + '.md.',
          '',
          'Your job is to REFUTE. Default to real=false when uncertain. Reject the claim if:',
          '  - the "missing" text is actually present (possibly reflowed or on an adjacent line)',
          '  - the difference is only page scaffolding, removed furniture, line wrapping, hyphenation, or formatting',
          '  - the claim misquotes either source',
          '  - the difference does not change meaning',
          'For a claimed notation defect in a general format, look at the printed glyphs yourself, say what you see, and',
          'judge it by this edition\'s notation: ' + ROW.choice + '.',
          'Set real=true ONLY if you independently confirm a genuine content discrepancy. If the finding is real',
          'but was described inaccurately, set real=true and give the corrected description.',
          '',
          'PERSIST BEFORE YOU RETURN - MANDATORY. Use the Write tool to save your verdict as JSON to:',
          '  ' + LEDGER + 'verify-' + f.id + '.json',
          'Shape: {"claim_id": "' + f.id + '", "claim_key": "' + claimKey(f) + '", "page": ' + f.page + ', "kind": "' + f.kind + '",',
          '        "severity": "' + f.severity + '", "claim": {"pdf_says": ..., "markdown_says": ..., "why_it_matters": ...},',
          '        "verdict": {"real": true|false, "reasoning": ..., "corrected_description": ...}}',
          'claim_id is exactly "' + f.id + '" and claim_key exactly "' + claimKey(f) + '" (copy both character for',
          'character): the key is this claim\'s content, and the merge attaches your verdict to the claim with that id AND',
          'that key, no other. Include the claim so the file stands alone. Return {"claim_id": "' + f.id + '",',
          '"claim_key": "' + claimKey(f) + '", "real": ..., "reasoning": ...}. A verdict that exists only in a return',
          'value is lost the moment the run is interrupted.',
        ].join('\n'),
        { label: 'verify:' + f.id, phase: 'Verify', schema: VERDICT, model: MODEL, effort: EFFORT }
      ).then(v => Object.assign({}, f, { verdict: v && v.claim_id === f.id && v.claim_key === claimKey(f) ? v : null }))
    )).then(claims => ({ batch, res, claims }))
  }
)

// The result reports only what a VALID compare result proves (kb/Work PB5255 review R1-4): pages its agent checked,
// pages it could not read, and every batch with no valid result, by name. A batch whose agent died swept nothing.
const done = results.filter(r => r && r.res)
const failed = batches.map((b, i) => results[i] && results[i].res ? null : { batch: span(b), problems: results[i] ? results[i].problems : ['the pipeline returned nothing'] }).filter(Boolean)
const claims = done.flatMap(r => r.claims || [])
const confirmed = claims.filter(f => f.verdict && f.verdict.real === true)
const refuted = claims.filter(f => f.verdict && f.verdict.real === false)
const unverified = claims.filter(f => !f.verdict)
const checked = done.flatMap(r => r.res.pages_checked)
const unread = done.flatMap(r => r.res.pages_unread)

log('pages checked ' + checked.length + ' of ' + PAGES.length + ' requested · unread ' + unread.length + ' · batches with no valid compare result ' + failed.length)
log('claimed ' + claims.length + ' - confirmed ' + confirmed.length + ' - refuted ' + refuted.length + ' - unverified ' + unverified.length)
log('the files on disk are the record: python scripts/spec/merge_reconciliation.py --edition ' + EDITION + ' (in ' + CHECKOUT + ')')

return {
  ok: failed.length === 0 && unread.length === 0 && unverified.length === 0,
  edition: EDITION,
  pages_requested: PAGES.length,
  pages_checked: checked.length,
  pages_unread: unread,
  batches_without_valid_compare: failed,
  claimed: claims.length,
  confirmed: confirmed.length,
  refuted: refuted.length,
  unverified: unverified.map(f => f.id),
  by_severity: {
    normative: confirmed.filter(f => f.severity === 'normative').length,
    structural: confirmed.filter(f => f.severity === 'structural').length,
    cosmetic: confirmed.filter(f => f.severity === 'cosmetic').length,
  },
  confirmed_findings: confirmed.map(f => ({
    id: f.id, page: f.page, kind: f.kind, severity: f.severity,
    pdf_says: f.pdf_says, markdown_says: f.markdown_says,
    why_it_matters: f.why_it_matters, suggested_repair: f.suggested_repair,
    verifier: f.verdict.corrected_description || f.verdict.reasoning,
  })),
  refuted_summary: refuted.map(f => ({ id: f.id, page: f.page, kind: f.kind, why_refuted: f.verdict.reasoning })),
}
