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
    <h3>Four landing trains in one night; battery #46 all green on the compiler and its one differential flip already fixed by train 4; battery #47 running at train 4's head <span class="pill good">LANDING</span></h3>
    <p><strong>What landed since the fourth pause.</strong> Four landing trains, each a fast-forward with one commit per cluster and one gate over the union of their filters. Train 1 (four clusters) and train 2 (six) carried the NIST guard's evidence rule, a variable-length group across CALL and INVOKE, the Report Writer CONTROL operand and the VALUE clause's one screen for every subject. Train 3 (five) carried the USAGE BIT leaf's carriage gate, level-number validation, EC-DATA-INCOMPATIBLE wired for every statement class with its exemption table as a structure, and the not-implemented carrier's three jobs split into three. Train 4 (four) carried the CALL statement whole: its exception partition (a CALL with only NOT ON EXCEPTION no longer swallows a failed activation), its program-prototype registry, a Format-2 argument classified once on its own carrier, and the VALUE size sentences that stopped binding a condition-name. Between the trains the lane-3 registrar filed batch 3 (the data-description clauses of §13.18) and golden-lane round 3 witnessed batch 2's resolved-but-untested rows with fifty-nine spec-derived programs.</p>
    <p><strong>The instruments.</strong> Batteries #44, #45 and #46 were each attributed to the row: #44's two differential flips were one pre-existing hole (no level-number validation), fixed as <strong>PB485</strong> in train 3; #45's five were that pair, two rejections the standard licenses, and one over-rejection the VALUE screen completed at level 88 (<strong>PB598</strong>, fixed in train 4); #46 is all green on every compiler leg and names exactly that PB598 flip, which train 4's lander then measured at zero on the merged tree after one spec-licensed rebaseline (a program-prototype-name the new registry makes legal source). Battery #47 is running in its own worktree at train 4's head.</p>
    <ol>
      <li><strong>Fix lane, at its cap</strong> — <strong>PB250</strong>, <strong>PB251</strong> and <strong>PB252</strong> are running; <strong>PB248</strong> (the integer-argument screen made one primitive with five readers; a float at an integer position now rejects, retiring a golden that pinned the defect) is finished and is train 5's first cluster. Train 5 leaves when it holds four to six clusters.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — batch 3 registered; batch 4 is next, with the registrar's reference-shape repairs folded into the merge script first.</li>
      <li><strong>Golden lane</strong> — round 3 landed; round 4's list is the rows batch 3 left resolved-but-untested, plus the rows that close only on an owner-signed derivation.</li>
      <li>Behind: the PB55 absorption campaign with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">12</span></h3>
      <p><strong>Answered 2026-09-04:</strong> witness-first adjudication — <em>no</em>, the lanes stay separate; the 160/220 turn caps — <em>yes</em>; five-cluster landing trains — <em>yes</em>.</p>
      <ul>
        <li><strong>Six from the process review</strong>, held in PB468 and asked one at a time as each becomes relevant: raising the implementer cap; table-driven tests closing many rows on one drift test; effort tiers; the already-OK "shall" rows with positive-only evidence; whether Report Writer stays claimed; extending PB386's derivation arm class by class — PB205's §13.18.16.4 GR6 row and round 3's two Annex A.2 rows wait on exactly that.</li>
        <li><strong>Two from the batch-3 registrar</strong> (PB579): whether the decimal-float rows convert to documented non-support on the witness already on disk, and whether a documented-non-support row stands while the decline behind it has a measured hole (VALIDATE's Format-5 <span class="mono">VALUE literal VALID</span> reaches the backend as a raw error).</li>
        <li><strong>One from the golden lane</strong> (PB592): A.1 item 206's "exactly the minimum range" determination contradicts §13.18.60.4 GR12's own asymmetric table and the compiler, which stores −128.</li>
        <li><strong>One from train 3</strong> (PB235): CLOSE's §14.9.6.4 GR1 row was closed on the same unpopulatable-antecedent derivation the owner signed for the READ rows — a class-by-class extension of PB386's arm, made before the standing answer on that class.</li>
        <li><strong>One from train 5</strong> (PB248): the strict verdict for a float operand at an integer argument position changed from ACCEPT to REJECT, on §15.3's own definition of an integer argument; a landed golden that pinned the old verdict is retired and a second is narrowed.</li>
        <li><strong>Still open, and not yet a bare question:</strong> the introducing edition of <span class="mono">USAGE POINTER TO type-name</span>.</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>The denominator's unnumbered and multi-sentence obligations</strong> — PB479 and PB581: "shall" sentences the standard did not number have no rows, and a numbered rule with several sentences can close on one; measure both classes before deciding the row shape</li>
        <li><strong>The citation audits' three blind spots</strong> — PB591, PB597 and PB616: a prose rule reference, a wrong rule number inside a correct clause, and a clause one level short of its sub-clause, all invisible to audits that validate quoted fragments only</li>
        <li><strong>Report Writer placement</strong> — PB484: the first body group prints one line too low, and a golden pins the wrong line</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
      </ul>
    </div>
  </div>
