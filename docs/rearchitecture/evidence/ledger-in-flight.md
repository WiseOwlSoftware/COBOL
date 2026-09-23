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
    <h3>In flight — 2026-09-22 late evening</h3>
    <p><strong>Landing.</strong> <span class="pill warn">train 52</span> is landing wave 52 (one operand-class screen for GO TO DEPENDING / SEARCH VARYING / SET, ADDRESS OF as an argument, OO class data, NUMVAL-C locale grouping) together with the Conformance runner's compiled-program cache (PB985), which cuts a lander's re-gate from ~23 min to ~1.5 min. Trains 53 and 54 (wave 54: directive state, the MOVE chain for ACCEPT and INVOKE, the report LINE clause, operand surfaces, refusals that must carry a diagnostic) are merged and gating behind it.</p>
    <p><strong>Implementing.</strong> Finishers for the reserved-word gate's legacy parse path (PB655 + PB764), POINTER storage images at the CALL boundary (PB970), group operands the image composer cannot build (PB244), method DECLARATIVES (PB1010), keys after dynamic-length members (PB1025 + PB1026), SORT/MERGE termination (PB993) and the reference resolver's silent nulls (PB1030).</p>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions</h3>
      <p>Open questions live in <span class="mono">kb/Work/</span> as notes of kind <span class="mono">decision</span> and are asked one at a time as each becomes relevant. Standing precedence for implementation latitude: the ISO text where it controls, then GnuCOBOL, then IBM or Micro Focus.</p>
    </div>
    <div class="card">
      <h3>History</h3>
      <p>What landed and why is recorded in <span class="mono">DEVLOG.md</span>; this page shows only the current state.</p>
    </div>
  </div>
