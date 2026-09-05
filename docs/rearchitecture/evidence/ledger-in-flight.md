<!--
  THE ONE HAND-WRITTEN SECTION OF THE CONFORMANCE LEDGER.

  `scripts/spec/gen_ledger.py` inserts this file VERBATIM as the body of the artifact's
  "In flight right now" section (inside <section aria-label="In flight">, after its <h2>), so it is an HTML
  fragment in a .md file rather than markdown — verbatim means verbatim, and a renderer standing between the
  author and the page would be a second author of the only part a person writes.

  ⛔ NO COUNT THAT THE GENERATOR CAN MEASURE BELONGS HERE. Row totals, verdict histograms, register standing,
  A.1 coverage, gate results and the corpus are all computed from the tree; writing one of them by hand here is
  how this page would start lying again. What belongs here is NARRATIVE: which lanes are running, what is
  queued behind the lander, which owner questions are open. Numbers are allowed only when they describe work
  that has NOT landed yet — a queued landing's row count exists nowhere the generator can read.

  ⛔ AND IT IS NOT A WORK LIST (CLAUDE.md rule 8). The register is `kb/Work/`. Anything here that starts to
  look like a checklist of remaining items is a note that should have been filed instead.

  Classes available: .flight (the accent panel), .cardgrid + .card, .pill.good/.warn/.crit, .mono, .num, .dim.
-->
  <div class="flight">
    <h3>Battery #43 is green on the compiler, and the first five-cluster landing train is forming <span class="pill good">RESUMED</span></h3>
    <p><strong>The battery's one red line was the harness, not the compiler.</strong> #43 ran in a worktree cut at the batch head and reported a single NIST regression, <span class="mono">IF141A</span>. The attribution probe found no output difference at all: the battery's own compiled program produces a report byte-identical to its golden, sixty-five runs out of sixty-five, and the golden is what §15.98.4 requires. What failed was the guard's <em>compare</em> arm, which scores a lost observation — a report it could not read under load — as a wrong answer, the exact shape the compile and run arms were hardened against and the third arm never was. It is registered as <strong>PB473</strong> and its fix is in an implementer's hands now; the compiler's evidence stands.</p>
    <p><strong>Every stream resumed fresh from its disk checkpoint</strong> after the fourth session-limit pause, under the owner's 2026-09-04 process decisions: hard turn caps, one mechanism per implementer, and one lander carrying five clusters per landing. On the way back in, the workflow templates turned out to be un-launchable — checked out with Windows line endings the tool refuses — and the fleet guard was denying every worktree build because it read a Git-Bash drive path as a directory that does not exist (<strong>PB474</strong>, fixed with its self-test). Both were the kind of process defect that only shows up when the process is actually run.</p>
    <ol>
      <li><strong>Fix lane, at its cap</strong> — <strong>PB203</strong> (bit-usage groups under REDEFINES, one write-side bit-order law) and <strong>PB201</strong> (a position operand's <em>carrier</em>, not its class: the bind-time renderer bet on C# overload resolution and the bet had five losing sides) are finished on their worktrees; <strong>PB193</strong> (OPEN's fixed-file-attribute validation, I-O status 39) and <strong>PB470</strong> (three locale intrinsics returning a zero-length substitute where the documented answer is spaces) are finishing; <strong>PB473</strong> is fresh. Together they are the first five-cluster train — one build, one gate over the union of their filters, one commit per cluster so a red bisects, one push.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — batch 3, the data-description clauses of §13.18, is in its second half: part A's six subjects are adjudicated, refuted and waiting for the registrar; part B's eleven are being refuted now on the pinned compiler, the last two adjudicated first. Part A's refuters again overturned only downward — BLANK WHEN ZERO tests the unscaled value before the mask rescale — and its CONFORMS share is the softest number in the projection, measured here on purpose.</li>
      <li><strong>Golden lane, round 3</strong> — batch 2's resolved-but-unwitnessed rows in six files. Two writers had finished when the limit hit and their refuters died with them; the writers' reports were recovered from the dead run's journal to disk and the refuters are running against them rather than re-deriving forty goldens twice.</li>
      <li>Behind the train: the batch-3 registrar, then the golden lander, then the PB55 absorption campaign with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">7</span></h3>
      <p><strong>Answered 2026-09-04:</strong> witness-first adjudication — <em>no</em>, the lanes stay separate, because two lanes derive the evidence twice and that second derivation is the guarantee; the 160/220 turn caps — <em>yes</em>; five-cluster landing trains — <em>yes</em>.</p>
      <ul>
        <li><strong>Six from the process review</strong>, held in PB468 and asked one at a time as each becomes relevant: raising the implementer cap from three to six; table-driven tests that close many rows on one two-way drift test; effort tiers on checked steps; the already-OK "shall" rows whose evidence is positive-only; whether Report Writer stays claimed; extending PB386's derivation arm class by class.</li>
        <li><strong>Still open, and not yet a bare question:</strong> the introducing edition of <span class="mono">USAGE POINTER TO type-name</span> — it needs the 2002 and 2014 texts before it can be asked.</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>The NIST guard's evidence rule</strong> — PB473: a verdict only from an observation actually made, on all three arms, proven by witnesses that include a genuinely wrong report still scoring a regression</li>
        <li><strong>Phase-B dossier tooling</strong> — the adjudication lane's instrument: agents verify, they do not search. Batch 2's adjudicators reported their dossier gaps into the note that owns the generator</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
        <li><strong>The inventory's two row shapes</strong> — PB472: one writer builds every field on every row, the other assigns adjudicated fields onto loaded rows, and nothing asserts a row's key sequence</li>
      </ul>
    </div>
  </div>
