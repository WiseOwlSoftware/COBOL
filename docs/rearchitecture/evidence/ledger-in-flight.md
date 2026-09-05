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
    <h3>Train 1 landed, battery #44 is green on the compiler, and train 2 is forming <span class="pill good">LANDING</span></h3>
    <p><strong>The first five-cluster train carried four clusters and landed as one fast-forward:</strong> bit-usage groups under REDEFINES (PB203), a position operand's <em>carrier</em> rather than its class (PB201), the three LOCALE intrinsics' substituted result (PB470), and OPEN's fixed-file-attribute validation with its validated set varying by organization (PB193) — one commit each, one gate over the union of their filters, one push. The comprehensive battery ran on that head in a detached worktree and is green on every compiler leg with the NIST guard clean. Its two GnuCOBOL flips are not a regression in the compiler's answers: both programs use a <span class="mono">78</span>-level constant, a vendor extension the standard does not define, which the front end has never validated — PB201 removed the backend crash that had been hiding that, so the cases now compile and abort at run time. It is registered as <strong>PB485</strong>; the baseline rows were deliberately left at their ISO-correct verdicts, and the fix restores them for the right reason.</p>
    <p><strong>What the resume proved about its own instruments.</strong> Battery #43's one red, <span class="mono">IF141A</span>, was the NIST guard's compare arm scoring a report it could not read under load as a wrong answer; the fix (<strong>PB473</strong>) is finished on its worktree with the full equivalence proof green — both guard arms, verdicts identical over the whole corpus — and the verdict rules that had existed as two "character-for-character" copies now exist once. Along the way the fleet guard was found denying every worktree build over a path spelling (<strong>PB474</strong>, fixed), and a filed note was found truncated with the register check green (<strong>PB478</strong>).</p>
    <ol>
      <li><strong>Fix lane, at its cap</strong> — <strong>PB206</strong>, <strong>PB207</strong> and <strong>PB208</strong> (three faces of the VALUE clause) are running; <strong>PB473</strong>, <strong>PB204</strong> (a variable-length group crosses CALL, CALL RETURNING and INVOKE — §8.5.1.12 has an implementation at last) and <strong>PB205</strong> (a CONTROL-clause operand is a written reference, in all three clauses that write one) are finished and form train 2 with whichever reports next; <strong>PB485</strong> takes the first free slot because it owns the battery's two flips.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — batch 3, the data-description clauses of §13.18: part A's six subjects adjudicated and refuted, part B's eleven being refuted now on the pinned compiler; the registrar follows.</li>
      <li><strong>Golden lane, round 3</strong> — the two files recovered from the killed run are refuted and fixed (one derivation overreach, two WHEN legs that could not fail); the remaining four run next.</li>
      <li>Behind: the batch-3 registrar, the golden lander, the PB55 absorption campaign with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">7</span></h3>
      <p><strong>Answered 2026-09-04:</strong> witness-first adjudication — <em>no</em>, the lanes stay separate, because two lanes derive the evidence twice and that second derivation is the guarantee; the 160/220 turn caps — <em>yes</em>; five-cluster landing trains — <em>yes</em>.</p>
      <ul>
        <li><strong>Six from the process review</strong>, held in PB468 and asked one at a time as each becomes relevant: raising the implementer cap from three to six; table-driven tests that close many rows on one two-way drift test; effort tiers on checked steps; the already-OK "shall" rows whose evidence is positive-only; whether Report Writer stays claimed; extending PB386's derivation arm class by class — PB205 left one row (§13.18.16.4 GR6) at GAP for exactly that reason.</li>
        <li><strong>Still open, and not yet a bare question:</strong> the introducing edition of <span class="mono">USAGE POINTER TO type-name</span> — it needs the 2002 and 2014 texts before it can be asked.</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>Level-number validation</strong> — PB485: the check exists in the legacy engine and was never ported; two parser arms reach the level number and both must route to one screen</li>
        <li><strong>The denominator's unnumbered obligations</strong> — PB479: "shall" sentences the standard did not number have no inventory rows; measure the class first, then decide the row shape</li>
        <li><strong>Report Writer placement</strong> — PB484: the first body group prints one line too low, and a golden pins the wrong line</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
      </ul>
    </div>
  </div>
