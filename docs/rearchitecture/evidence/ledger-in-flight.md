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
    <h3>Two lanes, and a landing chain <span class="pill warn">SERIALIZED</span></h3>
    <p><strong>Owner decision PB278, taken 2026-09-01 — "Interleave."</strong> Adjudication no longer waits for the fix queue to drain: it runs as a <em>second lane, concurrently</em> with the fix lane. The measurements that earned it are the ones on this page — most of the standard has never been read against the compiler, and a serial order would discover the worst of what remains last.</p>
    <p><strong>Eight landings are gated and queued behind one lander</strong> — one lander on main at a time, because DEVLOG numbering and fast-forward ordering admit no second: cluster E · witness A (<span class="num">+49</span> rows) · cluster F · the screen witnesses (<span class="num">+163</span> rows) · the citation sweep · cluster D · witness B2 · the golden lane (<span class="num">~140</span> rows). That is roughly <span class="num">350</span> rows of closure already earned and not yet reflected in the headline figure above — the queue is the bottleneck, not the work.</p>
    <ol>
      <li><strong>Fix lane</strong> — ranked by what a defect does to a user's program, not by its severity label; the head of the list is in the tile above.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — batch 1 complete: <strong>184 rules adjudicated</strong>, 110 defective across 85 mechanisms, 70 CONFORMS still under refutation. Every finding becomes a <span class="mono">kb/Work</span> note before it becomes a DEVLOG paragraph.</li>
      <li><strong>The comprehensive battery</strong> — owed once the chain drains and no fleet is live; it is the orchestrator's job, never a lander's, and the tile above says whether one is outstanding.</li>
      <li>Last, as before: the PB55 absorption campaign, with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">3</span></h3>
      <p><strong>PB280 Q1–Q3</strong>, raised by the Annex A.1 DOC-row landing — the only owner-parked note in the register.</p>
      <ul>
        <li><strong>Q1</strong> — does a "Not provided." determination on an A.1-<em>optional</em> item close as CONFORMS, or as DOCUMENTED-NON-SUPPORT? One answer settles about thirty items as a single selector rather than thirty adjudications; two rows are held out of the back-fill until it is answered.</li>
        <li><strong>Q2</strong> — may a DOC row with nothing in the compiler to observe close on the register alone? This widens the definition of DONE, which is exactly why that arm did not land with the mechanism.</li>
        <li><strong>Q3</strong> — is this repo's <span class="mono">CONFORMANCE.md</span> the §4.2.16 user documentation, able to discharge an obligation by reference?</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>The golden lane</strong> — drafting the spec-derived witnesses for the resolved-but-unwitnessed band; the single largest closable block on the board, and it needs no compiler change</li>
        <li><strong>Phase-B dossier tooling</strong> — the adjudication lane's instrument: agents verify, they do not search</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
        <li><strong>Worktree-aware fleet guard</strong> — a running fleet freezes the tree, so no sibling edit can race it</li>
      </ul>
    </div>
  </div>
