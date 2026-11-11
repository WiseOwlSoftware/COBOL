export const meta = {
  name: 'spec-reconcile',
  description: 'Compare the canonical standard PDF against its markdown transcription (an edition: 1985, 2002 or 2023) page by page and ledger every discrepancy',
  whenToUse: 'Reconciling a transcription against its canonical PDF. args {pages, edition, model, effort, outDir, workDir}, every one required: pages as an array or ranges ("1-12,37-48", for 2002 a run of docs/rearchitecture/spec-reconciliation/2002/ranges.json); outDir the edition directory in the worktree of the branch that lands the run; workDir a scratch directory of this run alone. See docs/rearchitecture/spec-reconciliation/README.md.',
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
//            docs/rearchitecture/spec-reconciliation/2002/ranges.json (scripts/spec/reconcile_ranges.py). The guard
//            after the run is `merge_reconciliation.py --edition <e> --expect N`, which counts the pages actually swept.
//   edition  1985, 2002 or 2023: a row of EDITIONS below.
//   model    the model of every agent (the owner's decision for 2002, 2026-11-11: Opus).
//   effort   the reasoning effort of every agent: low, medium, high, xhigh or max (2002: medium).
//   outDir   where every agent writes its file: the edition's directory docs/rearchitecture/spec-reconciliation/<dir>
//            INSIDE the worktree of the branch that will land the run. The checkout the work units are built from is
//            the one outDir lies in, so the agents compare the PDF with that branch's transcription.
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
  2023: {
    standard: 'ISO/IEC 1989:2023', dir: 'agents', pages: 1261,
    keyword: '§2023:5.2.2 (Keywords): keywords are shown in uppercase and underlined, and are required',
    choice: '§2023:5.2.6.4 (Choice indicators): a pair of bars just inside a bracket or brace. Inside BRACES, one or more of the enclosed alternatives shall be specified, each only once; inside BRACKETS, zero or more, each at most once; in ANY ORDER. Without the bars, a bracket around stacked alternatives means AT MOST ONE of them',
    ellipsis: '§2023:5.2.7 (Ellipses)',
  },
}
const EDITION = Number(args.edition)
const ROW = EDITIONS[EDITION]
if (!ROW) throw new Error('spec-reconcile: edition ' + JSON.stringify(args.edition) + ' has no row (rows: ' + Object.keys(EDITIONS).join(', ') + ')')
const STANDARD = ROW.standard

if (typeof args.model !== 'string') throw new Error('spec-reconcile: argument model must be a string naming the model')
const MODEL = args.model
if (!EFFORTS.includes(args.effort)) throw new Error('spec-reconcile: argument effort must be one of ' + EFFORTS.join(', ') + '; got ' + JSON.stringify(args.effort))
const EFFORT = args.effort

// outDir is the edition's directory inside a checkout; the checkout is what precedes it.
const SUFFIX = '/docs/rearchitecture/spec-reconciliation/' + ROW.dir
const OUT = String(args.outDir).replace(/\\/g, '/').replace(/\/+$/, '')
if (!OUT.endsWith(SUFFIX) || OUT.length === SUFFIX.length) {
  throw new Error('spec-reconcile: argument outDir must be <the landing worktree>' + SUFFIX + ' for edition ' + EDITION + '; got ' + JSON.stringify(args.outDir))
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

log(`Reconciling ${STANDARD}: ${PAGES.length} pages in ${batches.length} batches of up to ${BATCH}; model ${MODEL}, effort ${EFFORT}`)
// Echo the actual boundaries so a mis-transcribed list is visible AT LAUNCH, not after the tokens are spent.
log(`first=${PAGES[0]} last=${PAGES[PAGES.length - 1]} distinct=${new Set(PAGES).size} batches=${batches.map(b => b[0] + '-' + b[b.length - 1]).join(',')}`)
log(`writes ${LEDGER} · renders in ${SCRATCH}`)

const FINDINGS = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          page: { type: 'integer' },
          kind: { type: 'string', enum: ['missing-text', 'incorrect-text', 'misplaced-content', 'missing-figure', 'incorrect-figure', 'missing-heading', 'ocr-error', 'summarised-not-transcribed', 'other'] },
          severity: { type: 'string', enum: ['normative', 'structural', 'cosmetic'] },
          pdf_says: { type: 'string' },
          markdown_says: { type: 'string' },
          why_it_matters: { type: 'string' },
          suggested_repair: { type: 'string' },
        },
        required: ['page', 'kind', 'severity', 'pdf_says', 'markdown_says', 'why_it_matters'],
      },
    },
    pages_checked: { type: 'array', items: { type: 'integer' } },
  },
  required: ['findings', 'pages_checked'],
}

const VERDICT = {
  type: 'object',
  properties: {
    real: { type: 'boolean' },
    reasoning: { type: 'string' },
    corrected_description: { type: 'string' },
  },
  required: ['real', 'reasoning'],
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
      'Its last two lines name your FIGURE pages (a general format is drawn on them) and your PROSE pages. If it exits',
      'with an error (a page the transcription does not hold), report that page as a missing-text finding and go on.',
      '',
      'WHERE THE PRIORITY LIES. On a FIGURE page, diagram fidelity comes first: check every general format as below,',
      'then the prose. On a PROSE page, the prose checks below are the whole job. Every page gets a complete comparison.',
      '',
      'STEP 2 - for EACH page P in your batch:',
      '  a) Read the image ' + SCRATCH + '/f' + batch[0] + '/spec_pP.png (the Read tool renders it visually)',
      '  b) Read the markdown ' + SCRATCH + '/f' + batch[0] + '/page-P.md',
      '  c) Compare them carefully and completely.',
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
      'page. If a page is clean, report nothing for it but still list it in pages_checked. Reporting a non-issue',
      'is worse than missing one - a false finding sends the next session to "fix" correct text.',
      '',
      'STEP 3 - PERSIST BEFORE YOU RETURN. This is MANDATORY, not a nicety. Use the Write tool to save your',
      'complete findings as JSON to:',
      '  ' + LEDGER + 'compare-p' + batch[0] + '-p' + batch[batch.length - 1] + '.json',
      'Shape: {"batch": ' + JSON.stringify(batch) + ', "pages_checked": [...], "findings": [ ...same objects you return... ]}',
      'Write it EVEN IF you found nothing (findings: []) - a file proving a batch was swept clean is evidence;',
      'its absence is indistinguishable from work that never ran. The run may be interrupted by a rate or session',
      'limit at any moment, and your file is then the ONLY durable record of this work. Write it, then return.',
    ].join('\n'),
    { label: 'compare:p' + batch[0] + '-' + batch[batch.length - 1], phase: 'Compare', schema: FINDINGS, model: MODEL, effort: EFFORT }
  ),
  (res, batch, i) => {
    if (!res || !res.findings || res.findings.length === 0) return []
    return parallel(res.findings.map((f, vi) => () =>
      agent(
        [
          'Adversarially VERIFY a claimed discrepancy between the ' + STANDARD + ' PDF and its markdown transcription.',
          '',
          'CLAIM - page ' + f.page + ', kind=' + f.kind + ', severity=' + f.severity,
          '  PDF allegedly says      : ' + f.pdf_says,
          '  Markdown allegedly says : ' + f.markdown_says,
          '  Why it allegedly matters: ' + f.why_it_matters,
          '',
          'Check it yourself:',
          '  ' + WORKUNIT + ' ' + f.page + ' --edition ' + EDITION + ' --out ' + SCRATCH + '/v' + f.page + '-' + vi + ' --dpi 300',
          'Then Read ' + SCRATCH + '/v' + f.page + '-' + vi + '/spec_p' + f.page + '.png and ' + SCRATCH + '/v' + f.page + '-' + vi + '/page-' + f.page + '.md.',
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
          '  ' + LEDGER + 'verify-p' + f.page + '-' + f.kind + '-' + vi + '.json',
          'Shape: {"page": ' + f.page + ', "kind": "' + f.kind + '", "severity": "' + f.severity + '",',
          '        "claim": {"pdf_says": ..., "markdown_says": ..., "why_it_matters": ...},',
          '        "verdict": {"real": true|false, "reasoning": ..., "corrected_description": ...}}',
          'Include the claim so the file stands alone. A verdict that exists only in a return value is lost the',
          'moment the run is interrupted, and re-verifying costs another full page render and read.',
        ].join('\n'),
        { label: 'verify:p' + f.page + '-' + vi, phase: 'Verify', schema: VERDICT, model: MODEL, effort: EFFORT }
      ).then(v => Object.assign({}, f, { verdict: v }))
    ))
  }
)

const flat = results.filter(Boolean).flat().filter(Boolean)
const confirmed = flat.filter(f => f.verdict && f.verdict.real)
const refuted = flat.filter(f => f.verdict && !f.verdict.real)

log('claimed ' + flat.length + ' - confirmed ' + confirmed.length + ' - refuted ' + refuted.length)

return {
  edition: EDITION,
  pages_swept: PAGES.length,
  claimed: flat.length,
  confirmed: confirmed.length,
  refuted: refuted.length,
  by_severity: {
    normative: confirmed.filter(f => f.severity === 'normative').length,
    structural: confirmed.filter(f => f.severity === 'structural').length,
    cosmetic: confirmed.filter(f => f.severity === 'cosmetic').length,
  },
  confirmed_findings: confirmed.map(f => ({
    page: f.page, kind: f.kind, severity: f.severity,
    pdf_says: f.pdf_says, markdown_says: f.markdown_says,
    why_it_matters: f.why_it_matters, suggested_repair: f.suggested_repair,
    verifier: f.verdict.corrected_description || f.verdict.reasoning,
  })),
  refuted_summary: refuted.map(f => ({ page: f.page, kind: f.kind, why_refuted: f.verdict.reasoning })),
}
