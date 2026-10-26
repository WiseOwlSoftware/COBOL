export const meta = {
  name: 'rolling-wave',
  description: 'Rolling fix lane: a queue of implementer groups run N at a time (a freed slot is refilled at once), each DONE branch reviewed on the branch; the wave starts no lander: the loop\'s landing lane lands every branch reviewed CLEAN (kb/Work PB3023, PB3103)',
  whenToUse: 'The standard fix-lane dispatch (workstream SKILL §2, references/landing.md): fill a freed slot the moment it frees; review each DONE branch (kb/Work PB3027); the loop\'s landing lane lands them (land_lane.py runs enqueue.py land, kb/Work PB3023, PB3103).',
  phases: [
    { title: 'Implement', detail: 'cobol-implementer per group, isolated worktrees, rolling pool; a review finisher per round of findings' },
    { title: 'Review', detail: 'one read-only cobol-reviewer per DONE branch (the review skill branch form)' },
  ],
}

// args: { scratch, wave, stop_file, global_stop, concurrency (6), groups: [{letter, lead, notes, codes}],
//         review_fix_rounds (2), reviewer_ceiling_min (90), gate_fix_rounds (1), gate_await_wait_s (540),
//         gate_await_windows (one per 9 minutes of the ceiling); each group may carry requirements: {campaign lead: [string]} }
// THE WAVE STARTS NO LANDER (kb/Work PB3023; DESIGN-test-build-ci.md section 3.17.5): it produces DONE branches reviewed
// CLEAN, and the loop's landing lane (land_lane.py, kb/Work PB3103) lands each with `scripts/orchestrator/enqueue.py
// land` as it comes back, beside this wave (a script: main merged in,
// the gate, the DEVLOG stamp, the landing checks, one pull request into GitHub's merge queue). The train, its size and
// its final-train threshold went with the lander (kb/Work PB3016's "may THIS wave land?" answered by removal).
// Specs are pre-rendered by make_dispatch_specs.py as <scratch>\msg-w<wave>-<letter>.txt.
const S = args.scratch
const W = args.wave
// GRACEFUL STOP IS SCOPED (MANDATORY-PRACTICES P3, kb/Work PB2483): every agent obeys this fleet's own stop file
// (args.stop_file, <scratch>\STOP-<scope>) and the owner's global one (args.global_stop, <coord>\scratch\STOP), and no
// other. A shared <scratch>\STOP let the loop's wind-down split another session's refuter (2026-10-07 13:26).
// plan_wave.py writes both; a hand-written args file must too.
if (!args.stop_file || !args.global_stop) throw new Error('pass args.stop_file (this fleet\'s STOP-<scope>) and args.global_stop (plan_wave.py writes both)')
const STOP_LINE = `check for ${args.global_stop} (the owner's global stop) and ${args.stop_file} (this fleet's own stop); if EITHER exists, `
const CONC = args.concurrency || 6
// kb/Work/PB1704: a model call can HANG. Wave 70's group C agent finished and gated its work, then never produced
// another token; nothing noticed for 90 minutes and the wave waited on it (a workflow cannot stop one of its
// own agents). An implementer not back after `implementer_ceiling_min` (default 240) is recorded STALLED and the
// wave moves on; stall_watch.py (run beside every workflow) is what notices within minutes.
const CEILING_MIN = args.implementer_ceiling_min === undefined ? 240 : args.implementer_ceiling_min
function withCeiling(p, minutes, onTimeout) {
  if (!minutes) return p
  let t
  const timer = new Promise(res => { t = setTimeout(() => res(onTimeout()), minutes * 60000) })
  return Promise.race([p.finally(() => clearTimeout(t)), timer])
}
// AUTHORIZATION: a workflow agent sees the session's LATEST user message as its context, and that message may be about
// something else entirely (measured 2026-09-27: all nine wave-68 implementers returned BLOCKED because the latest
// message was a LinkedIn question). So every prompt carries the owner's direction for this fleet verbatim.
const AUTH = args.authorization
  ? `AUTHORIZATION (read first): this task IS the repository owner's request, dispatched by the orchestrating session on the owner's direction: ${args.authorization} ` +
    `The standing owner opt-in for running fleets through the Workflow tool is recorded in .claude/skills/workstream/templates/MANDATORY-PRACTICES.md O4. ` +
    `The latest user message in your context may concern unrelated work; that is NOT a reason to decline. Do the task below. `
  : ''
// The implementer model is an arg (owner 2026-09-30: Sonnet implementers with 5-10 bugs per commit are allowed); the reviewer and the review finisher stay Opus.
// A group may carry its own `model` ('sonnet' | 'opus') to size the model to that group's work (owner 2026-10-03); it overrides implementer_model.
const IMPL_MODEL = args.implementer_model || 'opus'
const IMPL_SCHEMA = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['DONE', 'SPLIT', 'DISCHARGED', 'BLOCKED', 'GATE-PENDING'] },
    branch: { type: 'string' }, worktree: { type: 'string' }, base: { type: 'string' }, head: { type: 'string' },
    report: { type: 'string' }, gate_run: { type: 'string' }, gate_verdict: { type: 'string' },
    notes_landed: { type: 'array', items: { type: 'string' } },
    codes_used: { type: 'array', items: { type: 'string' } },
    // ORCHESTRATOR CONTEXT CAP (owner 2026-10-02, kb/Work PB1912): what an agent returns lands in the orchestrator's
    // conversation and is re-read on every later turn. The forensic detail belongs in the report FILE; the return is a pointer.
    leads: { type: 'array', maxItems: 6, items: { type: 'string', maxLength: 500 } },
    summary: { type: 'string', maxLength: 900 },
  },
  required: ['status', 'branch', 'worktree', 'report', 'gate_verdict', 'summary'],
}
const RETURN_LINE =
  `⛔ YOUR LAST ACTION MUST BE THE StructuredOutput CALL — never end on a report file or a summary message (three agents in waves 65-67 did, and their finished branches were stranded): ` +
  `status, your ACTUAL branch (git branch --show-current), your worktree path, base sha, head sha, report path, your last gate's run directory (TestResults/build-local/<run>, whose verdict.json records its timings) and its verdict line, the notes you landed, the codes you used, and any new leads (text; do NOT allocate PB ids). ` +
  `⛔ KEEP THE RETURN SHORT: summary at most 900 characters, at most 6 leads of at most 500 characters each (each lead: repro path and code site), and every detail goes in your report file, which the orchestrator reads only on demand. ` +
  `GATE-PENDING is for ONE case only (kb/Work PB3536; MANDATORY-PRACTICES P2): you reach your turn cap while your own gate still runs — then checkpoint-commit, write STATUS.md and your report (header \`Status: GATE-PENDING\` and \`Gate run: <the run directory>\`), and return status GATE-PENDING with that run directory as gate_run and your head; the wave awaits the gate's verdict for you.`

// A GATE HANDED TO THE WAVE (kb/Work PB3536; owner 2026-10-22 ~16:50 PDT: "Reruning these simply wastes tokens: but the
// workflow returned before its gate finished, so it needs the gate re-run at its head and a round-2 review. - we
// should have the failure in the first place"). An agent that reaches its turn cap while its gate runs returns
// GATE-PENDING with the gate's run directory: the gate runs DETACHED (scripts/run_gate_legs.py starts it through
// scripts/detached.py), so the agent's return does not kill it, and its verdict lands in the run directory's verdict
// file. The wave AWAITS that file, inside the same ceiling, one window at a time: ONE mechanical cobol-locator per
// window runs `run_gate_legs.py --await <run> --worktree <its worktree>` (a workflow script has no process API; a
// relative run name is the agent's WORKTREE's run, never the main checkout's: run_gate_legs.resolve_run) and returns
// its exit code and line. Exit 0 (GREEN) continues exactly as a DONE return carrying that verdict: the finished test
// (the report's `Status: GATE-PENDING` is finished only when its gate run is GREEN at its HEAD,
// reports.Report.finished), then the branch review. Any other verdict (RED; NOT RUN, a process that DIED among them;
// BUSY, a gate that never ran) goes to a GATE FINISHER, at most `gate_fix_rounds`. No verdict by
// the last window, or the ceiling, is STALLED. Before PB3536 the wave read any non-DONE as SPLIT and the next wave's
// finisher re-ran the same gate at the same head (wave 1150 group A's review finisher, whose gate the return killed).
const GATE_FIX_ROUNDS = args.gate_fix_rounds === undefined ? 1 : args.gate_fix_rounds
const AWAIT_WAIT_S = args.gate_await_wait_s === undefined ? 540 : args.gate_await_wait_s
const AWAIT_WINDOWS = args.gate_await_windows === undefined ? Math.max(1, Math.ceil((CEILING_MIN || 240) / 9)) : args.gate_await_windows
// `--await`'s exit while the gate still runs: run_gate_legs.AWAIT_PENDING_EXIT (3 is BUSY there, kb/Work PB3527);
// test_rolling_wave.py holds the two equal.
const AWAIT_PENDING_EXIT = 4
const AWAIT_SCHEMA = {
  type: 'object',
  properties: { exit: { type: 'integer' }, line: { type: 'string', maxLength: 1200 } },
  required: ['exit', 'line'],
}
async function awaitGate(g, r, label, ctl) {
  for (let w = 1; w <= AWAIT_WINDOWS && !ctl.cancelled; w++) {
    let v = null
    try {
      v = await agent(
        AUTH + `Run exactly ONE command, from the repository root E:\\COBOL, with the Bash tool's timeout set to 600000 ms, and nothing else: ` +
        `python scripts/run_gate_legs.py --await "${r.gate_run}" --worktree "${r.worktree}" --wait-s ${AWAIT_WAIT_S} ` +
        `— then call StructuredOutput with its exit code as \`exit\` and its last line verbatim as \`line\`. ` +
        `Read no file, judge nothing, retry nothing: an exit code other than 0 is an answer, not an error.`,
        { label: `gate-await-${label}-${w}`, phase: 'Implement', agentType: 'cobol-locator', schema: AWAIT_SCHEMA })
    } catch (e) { v = null }
    if (v && v.exit !== AWAIT_PENDING_EXIT) return v
  }
  return null
}

function fixGate(g, r, v, round) {
  return agent(
    AUTH + `You are the wave-${W} GATE FINISHER for group ${g.letter} (${g.notes}), gate round ${round} (kb/Work PB3536). ` +
    `Your dispatch spec is the file ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt — read it WHOLE and follow its brief, codes, gate, checkpoint and stop rules; its notes are implemented on the branch. ` +
    `FIRST \`python scripts/merge_drivers.py merge ${r.branch} --keep\` into your branch (head ${r.head}; worktree ${r.worktree}), then run \`python tools/claude-skills/skills/agent-fleet/references/status_delta.py ${r.worktree}\` and read ${r.report} as your orientation. ` +
    `Its gate (run ${r.gate_run}) ended: ${v.line} — read that run's verdict.json, its leg logs and not-run files, and fix every red at its root with the sibling sweep (a gate that DIED or was NOT RUN with no red of the change needs only the re-gate). ` +
    `Re-gate (the spec's gate, blocking on its verdict line). Then REWRITE the report at ${r.report} for YOUR branch: its header names your worktree, branch, base and HEAD and your status, and its gate section your verdict. ` +
    `Before EACH new step ${STOP_LINE}checkpoint-commit, write STATUS.md NEXT and the report, and return status SPLIT. ` +
    RETURN_LINE,
    { label: `gatefix-${g.letter}-r${round}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: g.model || IMPL_MODEL }
  ).then(f => f ? { ...f, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes } : { ...r, status: 'NO-RESULT', finisher: 'NO-RESULT' })
}

// A return as the rest of the wave reads it once any gate it handed over has settled: GATE-PENDING whose gate is
// GREEN reads DONE (the finished test still decides), RED goes to a gate finisher (whose own return settles the same
// way), no verdict is STALLED. Every other return passes through unchanged.
async function settleGate(g, r, label, ctl, round = 1) {
  if (!r || r.status !== 'GATE-PENDING') return r
  if (!r.gate_run) {
    log(`${g.letter}: ${label} returned GATE-PENDING with no gate run directory; read as SPLIT`)
    return { ...r, claimed: r.status, status: 'SPLIT' }
  }
  const v = await awaitGate(g, r, label, ctl)
  if (!v) {
    log(`${g.letter}: ${label}'s gate ${r.gate_run} wrote no verdict within the ceiling; recorded STALLED (kb/Work PB3536)`)
    return { ...r, claimed: r.status, status: 'STALLED', gate_verdict: `no verdict in ${r.gate_run}` }
  }
  log(`${g.letter}: ${label}'s handed gate ${r.gate_run}: exit ${v.exit}`)
  if (v.exit === 0) return { ...r, claimed: r.status, status: 'DONE', gate_verdict: v.line, gate_awaited: true }
  if (round > GATE_FIX_ROUNDS || ctl.cancelled) {
    log(`${g.letter}: ${label}'s gate is not GREEN and the gate finisher rounds (${GATE_FIX_ROUNDS}) are spent; read as SPLIT`)
    return { ...r, claimed: r.status, status: 'SPLIT', gate_verdict: v.line }
  }
  let f = null
  try { f = await fixGate(g, r, v, round) } catch (e) { f = null }
  if (!f) return { ...r, claimed: r.status, status: 'SPLIT', gate_verdict: v.line, finisher: 'NO-RESULT' }
  return settleGate(g, f, `gate finisher round ${round}`, ctl, round + 1)
}

// THE BRANCH REVIEW (kb/Work PB3027; DESIGN-test-build-ci.md §3.17.2 step 2; MANDATORY-PRACTICES I10). The review of
// finished work used to run inside the lander, between its gates (lander-train-brief step 5b, retired): on 2026-10-10
// the review shards were 23 % of the land units' subagent tokens and sat on the landing's critical path, and every
// finding fixed in the train re-gated every cluster (kb/Work PB3015, its evidence table 6). Now a DONE branch is
// reviewed where it was made: ONE read-only cobol-reviewer (the review skill's branch form) records its decisions in
// <scratch>\review-w<wave>-<letter>.jsonl through review_findings.py, which computes the verdict and stamps it into the
// report's header (`**Review:** CLEAN <reviewer> <head>`). FINDINGS → a REVIEW FINISHER (a fresh cobol-implementer:
// a workflow cannot message an agent that has returned) merges the branch, answers every blocking finding by its id
// (fixed, or no_change_needed with the reason), re-gates and returns; its branch is reviewed again. Only a branch
// reviewed CLEAN at its head is ready to land; at the round cap the FINDINGS stamp stays, and the next wave's planner
// dispatches a finisher for it (plan_wave.py). prune_worktrees.py lists a DONE branch WAITING TO LAND only under a
// CLEAN stamp at its tip, so the landing lane lands no unreviewed branch either.
// ROUND 2 AND LATER REVIEW THE DIFF, AND A SEVERITY THRESHOLD DECIDES (kb/Work PB3381, owner 2026-10-18). Round 1 is the
// whole branch (origin/main...HEAD); a re-review reads only `git diff <the head the last round reviewed>..HEAD` and the
// report's answers, because the earlier content is reviewed: on wave 1118 every round re-reviewed the whole branch,
// each found something new (PB3331: 5, 2, 1 findings over 2.3 h) and the cap ended in a DECISION, not a landing. The
// verdict blocks on critical, high and medium findings while a finisher can still answer them (BLOCKING), and on
// critical and high only at the round after the last finisher (BLOCKING_AT_CAP): a medium finding becomes a lead
// there, a low one never blocks, and every non-blocking finding is a LEAD the report lists and the result carries.
const REVIEW_FIX_ROUNDS = args.review_fix_rounds === undefined ? 2 : args.review_fix_rounds
const BLOCKING = 'critical,high,medium'
const BLOCKING_AT_CAP = 'critical,high'
const REVIEWER_CEILING_MIN = args.reviewer_ceiling_min === undefined ? 90 : args.reviewer_ceiling_min
// A CAMPAIGN'S STANDING REQUIREMENTS (kb/Work PB3359): the owner's instructions that every group of the campaign must
// meet, the bullets of the `## Standing requirements` section of the campaign's lead note. plan_wave.py binds each group
// to every campaign its notes belong to (a fix-lane group and a campaign finisher included) and writes them into the
// group's spec and its entry in these args: `g.requirements` is {campaign lead: [requirement]}. The implementer reads
// them in its spec; the branch reviewer is handed the same spec AND the requirement text, so a branch lacking one is a
// finding, never a CLEAN.
function reviewerRequirements(g) {
  return Object.entries(g.requirements || {}).map(([lead, reqs]) =>
    `STANDING REQUIREMENTS of campaign ${lead}, which the owner set for every group of it: ${reqs.map((r, i) => `(${i + 1}) ${r}`).join(' ')} ` +
    `Check the branch against EACH of them, and raise a finding (category requirement) when the branch lacks one, with the sibling arms the requirement names. `).join('')
}
const REVIEW_SCHEMA = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['CLEAN', 'FINDINGS', 'STOPPED'] },
    findings: { type: 'integer' },
    head: { type: 'string' },
    stamp: { type: 'string', maxLength: 400 },
    leads: { type: 'array', maxItems: 6, items: { type: 'string', maxLength: 500 } },
    summary: { type: 'string', maxLength: 600 },
  },
  required: ['verdict', 'findings', 'head', 'stamp'],
}
const reviews = []

// SAME-FILE SUCCESSORS (kb/Work PB3381; owner 2026-10-18: "We should not be holding finished work this long,
// continually postponing it. Something in our process is flawed", and "Make the changes now and land the clean links as
// they come."): a group with `after: '<letter>'` starts only once that predecessor is SETTLED: its review loop ended
// (CLEAN, FINDINGS n, STOPPED, NO-RESULT or STALE), or it returned unfinished. It is told the predecessor's FINAL branch
// (the review finisher's, when one ran), head, report and review verdict, merges THAT branch, and orients from that
// report's "For the next implementer" section instead of re-surveying the file (owner 2026-09-27: fix related work
// together and minimize repetitive context gathering). NOTHING IS HELD: every finished result is reviewed in its own slot
// the moment its implementer returns and goes to `ready` when CLEAN, predecessor or not, so the landing lane lands it at
// once and the successor's pull request merges on top. The hold this replaced (the predecessor "lands THROUGH the
// successor") was the train era's, when two same-file branches had to ride one train; since kb/Work PB3023/PB3024 every
// branch is its own pull request in the merge queue, so the hold saved nothing: on wave 1118 (the PB3330 engine chain)
// six groups were DONE over seven hours and none landed, PB3331 held from 08:43 behind its successor.
const queue = [...args.groups]
const results = []
const byLetter = {}        // letter -> the group's SETTLED result: the reviewed one (a review finisher's, when one ran)
const ready = []          // DONE and reviewed CLEAN: the landing lane lands them
let running = 0
let wake = () => {}

// A predecessor is settled when its result is in and, if it finished, its review loop has ended (`review` set).
function settled(letter) {
  const p = byLetter[letter]
  return !!p && (!p.finished || !!p.review)
}

// HOW A PREDECESSOR'S REVIEW LOOP ENDED, as its successor (and the successor's reviewer) reads it: ONE reading, so the
// implementer's note and the reviewer's scope never disagree (kb/Work PB3381 review R1-1).
//   'clean'       reviewed CLEAN at its head: it lands on its own.
//   'capped'      FINDINGS, and the FINDINGS FILE's last verdict (`p.last_verdict`, review_findings.py last-verdict)
//                 is the round after the last finisher at the result's head: `reviewed()` ran out of rounds, no review
//                 finisher followed that verdict, and the findings belong to its next-wave finisher; the successor
//                 does not answer them and its review does not record them again.
//   'unanswered'  FINDINGS whose last verdict a review finisher was dispatched to answer and did not (it came back
//                 SPLIT, BLOCKED, STALLED, killed = NO-RESULT, a DONE the finished test refused; with a commit of its
//                 own or none): that finisher rewrote the group's one report for its own branch with no Review line,
//                 so no report shows the findings any more, and whatever it left half done is on the successor's
//                 branch. The successor's review covers the predecessor's content as its own and records again each of
//                 those findings that still holds. A last verdict that cannot be read reads 'unanswered' too, and so
//                 does a TORN findings file (`v.torn`, verdict TORN, review R4-1, R5-1, R6-1: one the reader cannot
//                 open, or whose content is not records; the CLI exits 0 with that reading): no verdict is read from a
//                 torn file, whatever its records say, so it is never read as capped.
//   'unreviewed'  any other end (STOPPED, NO-RESULT, STALE) or an unfinished return: its content is reviewed with the
//                 successor's, as its own.
// ONE FINDINGS CASE, ONE OWNER (kb/Work PB3404): 'capped' findings are the predecessor's next-wave finisher's, and
// prune_worktrees.py carrying_findings holds the successor until that finisher has landed them; 'unanswered' findings
// are the SUCCESSOR's: its review answers them and its finisher fixes them, and carrying_findings does not hold a
// successor whose review is CLEAN over a head that contains the predecessor's (prune_worktrees.review_answers). Both
// read the SAME findings file through the same reader (review_findings.last_verdict, whose `capped` is the one
// definition of the capped round), never the report's stamp, which the finisher removes (review R3-1: deciding from
// the head or the stamp told the successor "prune_worktrees.py holds" while nothing did).
function predecessorEnd(p) {
  if (p.review === 'CLEAN') return 'clean'
  if (!/^FINDINGS/.test(p.review || '')) return 'unreviewed'
  const v = p.last_verdict
  return v && v.unanswered && !v.torn && v.capped && sameHead(v.head, p.head) ? 'capped' : 'unanswered'
}

// THE ONE READER OF A GROUP'S FINDINGS FILE (kb/Work PB3381 review R3-1): `review_findings.py last-verdict`, run by
// ONE mechanical cobol-locator (a workflow script has no process API; the finished test does the same), returning the
// file's newest verdict {file, round, branch, head, verdict, blocking, unanswered, torn} (a torn file's reading is
// verdict TORN, unanswered, with the branches and heads it names), or null when it gave no answer (read unanswered).
const LAST_VERDICT_SCHEMA = {
  type: 'object',
  properties: { exit: { type: 'integer' }, line: { type: 'string', maxLength: 800 } },
  required: ['exit', 'line'],
}
async function lastVerdict(g) {
  let v = null
  try {
    v = await agent(
      AUTH + `Run exactly ONE command, from the repository root E:\\COBOL, and nothing else: ` +
      `python scripts/orchestrator/review_findings.py last-verdict "${findingsFile(g)}" ` +
      `— then call StructuredOutput with its exit code as \`exit\` and its \`=== LAST VERDICT: \` line verbatim as \`line\`. ` +
      `Read no file, judge nothing, retry nothing: an exit code other than 0 is an answer, not an error.`,
      { label: `last-verdict-${g.letter}`, phase: 'Review', agentType: 'cobol-locator', schema: LAST_VERDICT_SCHEMA })
  } catch (e) { v = null }
  const m = v && v.exit === 0 && /^=== LAST VERDICT: (.*) ===$/.exec((v.line || '').trim())
  let parsed = null
  try { parsed = m ? JSON.parse(m[1]) : null } catch (e) { parsed = null }
  if (!parsed) log(`${g.letter}: review_findings.py last-verdict gave no verdict (${v ? `exit ${v.exit}: ${v.line}` : 'no answer'}); its successor reads it unanswered`)
  return parsed
}

function successorNote(g) {
  if (!g.after) return ''
  const p = byLetter[g.after]
  if (!p || !(p.head && p.head !== p.base)) {
    return `Your predecessor group ${g.after} produced NO branch (status ${p ? p.status : 'never ran'}); start from main as usual. `
  }
  const review = p.review || `none (it returned ${p.status}, unfinished)`
  return `⛔ SAME-FILE SUCCESSOR: your predecessor group ${g.after} (${p.notes}) ended ${p.status}, review ${review}, on its FINAL branch ${p.branch} ` +
    `(worktree ${p.worktree}, head ${p.head}, report ${p.report}${p.review ? `, review findings ${findingsFile({ letter: g.after })}` : ''}). ` +
    `FIRST \`python scripts/merge_drivers.py merge ${p.branch} --keep\` (head ${p.head}) into your branch, then read that ` +
    `report's "For the next implementer" section and its STATUS.md as your orientation for the shared file — run ` +
    `\`python tools/claude-skills/skills/agent-fleet/references/status_delta.py ${p.worktree}\` and read ONLY the commits it lists beyond the summary. ` +
    `Its notes are its own: never flip them or list them as yours. ` +
    {
      clean: `It was reviewed CLEAN and lands ON ITS OWN (the landing lane lands it now); your pull request merges on top of it. `,
      capped: `Its review ended ${p.review} at the round cap: those findings belong to group ${g.after}'s NEXT-WAVE FINISHER, not to you; do not fix or answer them. ` +
        `Your branch carries them, so it lands only after that finisher has landed ${g.after} (prune_worktrees.py holds a branch carrying another note's unanswered FINDINGS, read from ${findingsFile({ letter: g.after })}). `,
      unanswered: `Its review ended ${p.review} and its review finisher came back ${p.finisher || 'without a verdict'} without answering them: those findings (${findingsFile({ letter: g.after })}) are UNANSWERED, ` +
        `its report no longer shows them (the finisher rewrote it for its own branch), and your branch carries them with whatever that finisher left half done. ` +
        `they are YOURS, with no next-wave finisher of the predecessor to answer them: your own review covers the predecessor's commits as yours and records again each of those findings that still holds, your review finisher answers them, and prune_worktrees.py holds your branch for that findings file only until your own review is CLEAN over a head that contains the predecessor's (review_answers, kb/Work PB3404), ` +
        `so read \`python scripts/orchestrator/review_findings.py show ${findingsFile({ letter: g.after })}\` and fix the ones your change reaches (I9). A branch of it that has not landed by your landing lands with yours. `,
      unreviewed: `Its review did not finish (${review}), so its commits on your branch are reviewed with yours, and a branch of it that has not landed by your landing lands with yours. `,
    }[predecessorEnd(p)]
}

function runGroup(g) {
  const ctl = { cancelled: false }
  return withCeiling(agent(
    AUTH + `You are the wave-${W} fix-lane implementer for group ${g.letter} (${g.notes}). ` + successorNote(g) +
    `Your dispatch spec is the file ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt — read it WHOLE and follow it exactly; ` +
    `it names your brief, codes, report path, scratch dir, gate and checkpoint protocol. ` +
    `Before EACH new step ${STOP_LINE}checkpoint-commit, write STATUS.md NEXT and your report, and return status SPLIT. ` +
    RETURN_LINE,
    { label: `impl-${g.letter}-${g.lead}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: g.model || IMPL_MODEL }
  ).then(r => r ? { ...r, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes } : { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT' })
    .then(r => settleGate(g, r, 'implementer', ctl)),
  CEILING_MIN, () => {
    ctl.cancelled = true
    log(`${g.letter}: no return after ${CEILING_MIN} min; recorded STALLED, the wave moves on (PB1704)`)
    return { letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes, status: 'STALLED' }
  })
}

function findingsFile(g) { return `${S}\\review-w${W}-${g.letter.toLowerCase()}.jsonl` }

function sameHead(a, b) { return !!a && !!b && (a.startsWith(b) || b.startsWith(a)) }

// What round 1 of a SUCCESSOR's review leaves alone, by predecessorEnd: a predecessor reviewed CLEAN at its head was
// reviewed, so its content is not re-opened (wave 1118: PB3332's reviewer restated its predecessor's findings); a
// predecessor CAPPED at FINDINGS keeps its findings in its own file for its own finisher, and that file holds the
// successor for them (prune_worktrees.py carrying_findings). Nothing else is left alone: an UNANSWERED predecessor's
// findings are no longer in any report (its finisher rewrote it), so the successor's reviewer records again each one
// that still holds (review R1-1, R3-1), and an unreviewed predecessor is reviewed as this branch.
function predecessorScope(g) {
  const p = g.after ? byLetter[g.after] : null
  if (!p || !p.review || !(p.head && p.head !== p.base)) return ''
  const file = findingsFile({ letter: g.after })
  return {
    clean: `This branch merged its same-file predecessor ${p.branch} at ${p.head}, which was reviewed CLEAN (${file}): that content is reviewed and is not re-opened unless this branch's own commits touch it. `,
    capped: `This branch merged its same-file predecessor ${p.branch} at ${p.head}, whose review ended ${p.review} at the round cap: those findings are recorded in ${file} and belong to its next-wave finisher; do not record them again here. `,
    unanswered: `This branch merged its same-file predecessor ${p.branch} at ${p.head}, whose review ended ${p.review} and whose review finisher came back ${p.finisher || 'without a verdict'} without answering them: those findings (${file}) are UNANSWERED, and the predecessor's report no longer shows them. ` +
      `Review the predecessor's commits as part of this branch (the whole origin/main...HEAD), read \`review_findings.py show ${file}\`, and record again here each of those findings that still holds on this head. `,
    unreviewed: '',
  }[predecessorEnd(p)]
}

function reviewBranch(g, r, round) {
  const file = findingsFile(g)
  const label = `w${W}${g.letter.toLowerCase()}-review-r${round}`
  const blocking = round > REVIEW_FIX_ROUNDS ? BLOCKING_AT_CAP : BLOCKING
  return withCeiling(agent(
    AUTH + `You are the wave-${W} BRANCH REVIEWER for group ${g.letter} (${g.notes}), review round ${round} — READ-ONLY (kb/Work PB3027). ` +
    `Follow "The branch form" of E:\\COBOL\\.claude\\skills\\review\\SKILL.md exactly, with: branch ${r.branch}; worktree ${r.worktree}; head ${r.head}; ` +
    `the implementer's report ${r.report}; the findings file ${file}; your reviewer label ${label}; round ${round}; the blocking severities ${blocking}. ` +
    `The implementer's dispatch spec is ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt: read it, since the branch is reviewed against everything the implementer was told (its notes, its owner requirements, its gate). ` +
    reviewerRequirements(g) +
    (round > 1
      ? `This is a RE-REVIEW of the review finisher's branch (kb/Work PB3381): review ONLY \`git diff ${r.reviewed_head}..HEAD\` (the head round ${round - 1} reviewed, to this one; when the finisher's worktree brought in newer main, only its own commits, \`git log --no-merges ${r.reviewed_head}..HEAD --not origin/main\`) and the report's "## Review findings" section, which answers round ${round - 1}'s findings (review_findings.py show ${file} --round ${round - 1}) by id. The earlier content is reviewed and is not re-opened unless the diff touches it. Raise again only a finding that is neither fixed nor answered with a reason you accept, and any new defect the diff introduced. `
      : predecessorScope(g)) +
    `Record every finding with its severity (critical, high, medium, low or info), and run \`review_findings.py verdict ${file} --round ${round} --blocking ${blocking}\`: a finding of another severity is a LEAD, recorded but not blocking. ` +
    `Before EACH new step ${STOP_LINE}return verdict STOPPED and stamp nothing. ` +
    `Your LAST action is the StructuredOutput call: the verdict review_findings.py verdict printed (CLEAN or FINDINGS), the number of blocking findings, the head you reviewed, the stamp line review_findings.py stamp wrote, the LEAD lines verdict printed (at most 6) as \`leads\`, and a summary of at most 600 characters.`,
    { label, phase: 'Review', agentType: 'cobol-reviewer', schema: REVIEW_SCHEMA, model: 'opus' }
  ), REVIEWER_CEILING_MIN, () => {
    log(`${g.letter}: reviewer round ${round} not back after ${REVIEWER_CEILING_MIN} min; the branch stays unreviewed (PB1704)`)
    return null
  })
}

function fixFindings(g, r, v, round) {
  const ctl = { cancelled: false }
  return withCeiling(agent(
    AUTH + `You are the wave-${W} REVIEW FINISHER for group ${g.letter} (${g.notes}), after branch review round ${round} (kb/Work PB3027). ` +
    `Your dispatch spec is the file ${S}\\msg-w${W}-${g.letter.toLowerCase()}.txt — read it WHOLE and follow its brief, codes, gate, checkpoint and stop rules; its notes are already implemented on the reviewed branch. ` +
    `FIRST \`python scripts/merge_drivers.py merge ${r.branch} --keep\` into your branch (the reviewed head ${r.head}; worktree ${r.worktree}), then run \`python tools/claude-skills/skills/agent-fleet/references/status_delta.py ${r.worktree}\` and read ${r.report} as your orientation. ` +
    `The reviewer recorded ${v.findings} blocking finding(s): \`python scripts/orchestrator/review_findings.py show ${findingsFile(g)} --round ${round}\`. Answer EVERY BLOCKING one by its id: fix it at its root, with a regression test or golden and the sibling sweep, or answer no_change_needed with the reason. A LEAD (not blocking) is already listed in the report's "## Review findings": fix it when it sits in a file you change anyway (I9), else leave it for the clerk. ` +
    `Re-gate (the spec's gate, blocking on its verdict line). Then REWRITE the report at ${r.report} for YOUR branch: its header names your worktree, branch, base and HEAD, carries no Review line (your branch is reviewed again when you return DONE), and its "## Review findings" section keeps every LEAD line and gains one line per blocking finding id: fixed (the commit) or no_change_needed (the reason). ` +
    `Before EACH new step ${STOP_LINE}checkpoint-commit, write STATUS.md NEXT and the report, and return status SPLIT. ` +
    RETURN_LINE,
    { label: `fix-${g.letter}-r${round}`, phase: 'Implement', agentType: 'cobol-implementer', isolation: 'worktree', schema: IMPL_SCHEMA, model: g.model || IMPL_MODEL }
  ).then(f => settleGate(g, f, `review finisher round ${round}`, ctl)), CEILING_MIN, () => {
    ctl.cancelled = true
    log(`${g.letter}: review finisher round ${round} not back after ${CEILING_MIN} min; recorded STALLED (PB1704)`)
    return { status: 'STALLED' }
  })
}

// THE FINISHED TEST (kb/Work PB3181). A branch with finished work is the one its REPORT calls finished: DONE, or
// DISCHARGED with a HEAD past its base. That predicate lives ONCE, in Python (reports.Report.finished), because the
// branch survey (prune_worktrees.py) reads the same report to decide WAITING TO LAND; this wave kept its own copy
// (`hasWork`) of the return's claim, so a DONE return whose report had no status line was reviewed, stamped CLEAN and
// then never landed (w1080f, 2026-10-15). A workflow script has no process API, so ONE mechanical cobol-locator runs
// `plan_wave.py --finished <report>` and returns its exit code; a DONE or DISCHARGED return whose report it does not
// call finished is treated as SPLIT (logged, never reviewed): the next wave's planner gives it a finisher (plan_wave.py
// FINISH_STATUSES, kb/Work PB2575). Any other status is unfinished without asking.
const FINISHED_SCHEMA = {
  type: 'object',
  properties: { exit: { type: 'integer' }, line: { type: 'string', maxLength: 400 } },
  required: ['exit', 'line'],
}

async function finishedReport(g, r, label) {
  if (!r || !(r.status === 'DONE' || r.status === 'DISCHARGED')) return false
  let v = null
  if (r.report) {
    try {
      v = await agent(
        AUTH + `Run exactly ONE command, from the repository root E:\\COBOL, and nothing else: ` +
        `python scripts/orchestrator/plan_wave.py --finished "${r.report}" ` +
        `— then call StructuredOutput with its exit code (0, 1 or 2) as \`exit\` and its \`=== REPORT: \` line verbatim as \`line\`. ` +
        `Read no file, judge nothing, retry nothing: an exit code other than 0 is an answer, not an error.`,
        { label: `finished-${label}`, phase: 'Review', agentType: 'cobol-locator', schema: FINISHED_SCHEMA })
    } catch (e) { v = null }
  }
  if (v && v.exit === 0) return true
  log(`${g.letter}: ${label} returned ${r.status}, but plan_wave.py --finished does not call its report ${r.report || '(none named)'} finished ` +
      `(${v ? `exit ${v.exit}: ${v.line}` : 'no answer'}); treated as SPLIT, no reviewer dispatched (kb/Work PB3181)`)
  return false
}

// A return the finished test refused, as the rest of the wave reads it: SPLIT, with the status it claimed.
function asSplit(r) {
  return { ...r, claimed: r.status, status: 'SPLIT', finished: false }
}

// The review loop: review the head; on FINDINGS a review finisher answers them and its branch is reviewed again (its
// diff only, from `reviewed_head`), up to REVIEW_FIX_ROUNDS finishers. Returns the newest result (the finisher's branch
// contains the reviewed one) with `review`: CLEAN, FINDINGS <n> (the stamp stays; a later wave's finisher answers it),
// STOPPED, NO-RESULT or STALE, and `review_leads`: every round's non-blocking findings, for the clerk.
async function reviewed(g, r) {
  let cur = r
  const leads = []
  for (let round = 1; ; round++) {
    let v = null
    try { v = await reviewBranch(g, cur, round) } catch (e) { v = null }
    const verdict = !v ? 'NO-RESULT' : (v.verdict === 'CLEAN' && !sameHead(v.head, cur.head)) ? 'STALE' : v.verdict
    if (v && Array.isArray(v.leads)) leads.push(...v.leads)
    reviews.push({ letter: g.letter, round, branch: cur.branch, head: cur.head, verdict, findings: v ? v.findings : null, leads: v && v.leads ? v.leads.length : 0 })
    log(`${g.letter}: review round ${round} of ${cur.branch}: ${verdict}${v && v.verdict === 'FINDINGS' ? ' ' + v.findings : ''}`)
    const end = { review_rounds: round, review_leads: leads }
    if (verdict === 'CLEAN') return { ...cur, ...end, review: 'CLEAN' }
    if (verdict !== 'FINDINGS') return { ...cur, ...end, review: verdict }
    if (round > REVIEW_FIX_ROUNDS) {
      log(`${g.letter}: still FINDINGS after ${REVIEW_FIX_ROUNDS} review finisher(s); the stamp stays and the next wave plans a finisher`)
      return { ...cur, ...end, review: `FINDINGS ${v.findings}` }
    }
    let f = null
    try { f = await fixFindings(g, cur, v, round) } catch (e) { f = null }
    let next = f && f.head && f.head !== cur.head
      ? { ...f, letter: g.letter, lead: g.lead, notes: g.notes, codes: g.codes, reviewed_from: cur.branch, reviewed_head: cur.head }
      : null
    if (next && f.status === 'DONE') next = (await finishedReport(g, next, `review finisher round ${round}`)) ? { ...next, finished: true } : asSplit(next)
    if (!next || !next.finished) {
      log(`${g.letter}: review finisher round ${round} returned ${f ? f.status : 'NO-RESULT'}; not landed by this wave`)
      return { ...(next || cur), ...end, review: `FINDINGS ${v.findings}`, finisher: f ? f.status : 'NO-RESULT' }
    }
    cur = next
  }
}

function landable(r) {
  return r.finished === true && r.review === 'CLEAN'
}

function wakeAll() { const w = wake; wake = () => {}; w() }

// A settled result: recorded for its successor (byLetter holds the REVIEWED result, so a successor merges the reviewed
// head) and, when reviewed CLEAN, ready for the landing lane at once, predecessor or not (kb/Work PB3381).
function onResult(g, r) {
  results.push(r)
  byLetter[r.letter] = r
  if (landable(r)) ready.push(r)
  else if (r.finished) log(`${r.letter}: ${r.status} but review ${r.review}; not landed by this wave (prune_worktrees.py names who acts next)`)
  log(`${r.letter}: ${r.status}; ${ready.length} branch(es) reviewed CLEAN for the landing lane, ${queue.length} group(s) queued`)
}

function nextEligible() {
  const i = queue.findIndex(g => !g.after || settled(g.after))
  return i < 0 ? null : queue.splice(i, 1)[0]
}

async function worker() {
  while (queue.length > 0) {
    let g = nextEligible()
    if (!g) {
      if (running === 0) { g = queue.shift() }                    // a predecessor never ran: start the successor fresh
      else { await new Promise(res => { const prev = wake; wake = () => { prev(); res() } }); continue }
    }
    // ⛔ An agent that dies on an API error REJECTS rather than returning null. Before this try/finally, that rejection
    // escaped the worker with `running` still counted and no wake-up sent, so a same-file successor parked on `wake`
    // waited forever: wave 68 (2026-09-27) sat "running" for 16 h after its group U agent died. A rejection is now a
    // NO-RESULT like a null return, and the count and the wake-up happen whatever the outcome.
    running++
    let r
    try {
      r = await runGroup(g)
      // a DONE or DISCHARGED return is finished only when its report is (the finished test above, kb/Work PB3181)
      if (r.status === 'DONE' || r.status === 'DISCHARGED') r = (await finishedReport(g, r, 'implementer')) ? { ...r, finished: true } : asSplit(r)
      // the branch review holds this group's slot: reviewing and answering findings are part of finishing it, for a
      // same-file predecessor too (its successor waits for it to settle, then merges the reviewed head)
      if (r.finished) r = await reviewed(g, r)
      // a FINDINGS end is read back from its findings file, the source the survey's hold reads too, before a
      // successor is told whose findings they are (predecessorEnd; kb/Work PB3381 review R3-1)
      if (/^FINDINGS/.test(r.review || '') && queue.some(q => q.after === g.letter)) r = { ...r, last_verdict: await lastVerdict(g) }
    } catch (e) {
      r = { letter: g.letter, lead: g.lead, notes: g.notes, status: 'NO-RESULT', error: String(e && e.message || e) }
    } finally {
      running--
    }
    try {
      onResult(g, r)
    } finally {
      wakeAll()
    }
  }
}

phase('Implement')
await parallel(Array.from({ length: Math.min(CONC, queue.length) }, () => () => worker()))

log(`${ready.length} branch(es) ready to land (${ready.map(r => r.letter).join(' ') || 'none'}): the landing lane lands them`)
// the reviews' non-blocking findings, for the enqueue unit's clerk step (units/enqueue.md step 5; each report lists them too)
const leads = results.flatMap(r => (r.review_leads || []).map(l => `${r.letter} (${r.branch}): ${l}`))
return { results, ready: ready.map(r => ({ letter: r.letter, branch: r.branch, report: r.report })), reviews, leads }
