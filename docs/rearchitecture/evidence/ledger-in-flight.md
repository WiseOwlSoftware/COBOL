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
    <h3>Resumed after the third pause — a three-landing chain, batch 3 on a fresh pin, and the process itself under review <span class="pill warn">SERIALIZED</span></h3>
    <p><strong>Owner decision PB278, taken 2026-09-01 — "Interleave."</strong> Adjudication runs as a <em>second lane, concurrently</em> with the fix lane; every agent works from an on-disk checkpoint so a usage-limit cutoff costs at most one step, and one lander pushes to main at a time.</p>
    <p><strong>Landing today, in this order.</strong> First the <strong>batch-2 registrar</strong> — the whole of EVALUATE · EXIT · GO TO · GOBACK · IF · INITIALIZE · MOVE · PERFORM · SEARCH · SET · SUBTRACT adjudicated and independently refuted, its findings clustered by mechanism into new register notes, and a merge-script defect caught on the way in: it would have closed two rows on evidence the refuter had just withdrawn. Behind it, <strong>PB383</strong> — BOOLEAN-OF-INTEGER's length guard answered <span class="mono">"0"</span> where the implementor's own documented answer is a zero-length value, two rules conflated in one condition; the fix makes raise-and-substitute one mechanism at ten sites. Then <strong>PB386</strong> — the owner's "yes, checkable only": a rule the standard leaves with no observable obligation now closes on an owner-signed <em>derivation</em> in CONFORMANCE.md, the Annex A.2 arm verified mechanically against a list generated from the standard's own citations, the schema and its C# twin changing together, a parity fixture holding them to the same refusal codes. The comprehensive battery is owed once this chain is down.</p>
    <ol>
      <li><strong>Fix lane</strong> — ranked by what a defect does to a user's program, not by its severity label; the head of the list is in the tile above. Two findings from PB383's sweep (a locale-formatted function returning a zero-length substitute that no documented class places, and a wording mismatch in the A.1 row-90 determination) become notes with the next registrar.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — <strong>batch 3 is being adjudicated now</strong> on a compiler pinned at this morning's main: the data description clauses (BLANK WHEN ZERO · the entry itself · LINAGE · PICTURE · SIGN · SYNCHRONIZED · USAGE · VALUE), seventeen subjects in two chunks, each rule checkpointed the moment it is decided, then refuted and registered. Batch 2's CONFORMS-but-untested rows are listed for the golden lane's third round.</li>
      <li><strong>The process under review</strong> — a three-perspective panel (throughput · tokens per closed row · correctness risk) measured the campaign against its own trend: the earned closure rate is lower than the headline, no further module declines remain, and the largest cost is a double derivation — the adjudicator writes and runs the probe that would be the witness, then throws it away for a later lane to rewrite. Its recommendation, <em>witness-first adjudication behind a blind expectation replicate</em>, goes to the owner as a decision with a pilot on the next batch.</li>
      <li>Last, as before: the PB55 absorption campaign, with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">2</span></h3>
      <p><strong>PB280 Q1–Q3, PB371 and PB386 are answered</strong> and no longer open.</p>
      <ul>
        <li><strong>The process change</strong> — adopt witness-first adjudication (one agent derives the verdict <em>and</em> its spec-derived witness in one pass, a blind replicate re-derives the expectation from the rule text alone before the program runs) for lane 3 from batch 4, on a measured pilot. The bare question and the pilot's success metric are being drafted from the panel's synthesis.</li>
        <li><strong>Still open, and not yet a bare question:</strong> the introducing edition of <span class="mono">USAGE POINTER TO type-name</span> — it needs the 2002 and 2014 texts before it can be asked.</li>
        <li><strong>Answered, for the record:</strong> Q1 — an A.1-<em>optional</em> element documented "Not provided." is DOCUMENTED-NON-SUPPORT (the <span class="mono">a1-optional-not-provided</span> selector). Q2 — a DOC row with nothing to observe stays GAP. Q3 — a determination may cite the host document but shall state the value. PB371 — a syntax rule whose antecedent only a declined module can create closes on the negative golden pinning that module's refusal. PB386 — a rule with no observable obligation closes on a checkable, owner-signed derivation; landing today.</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>The golden lane</strong> — spec-derived witnesses for the resolved-but-unwitnessed band; round 3 takes batch 2's CONFORMS-but-untested rows, and needs no compiler change</li>
        <li><strong>Phase-B dossier tooling</strong> — the adjudication lane's instrument: agents verify, they do not search. Batch 2's adjudicators reported their dossier gaps into the note that owns the generator</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
        <li><strong>PB380 — a one-shot initialiser whose loser does not wait</strong> — measure whether two run units can race it before calling it a defect</li>
      </ul>
    </div>
  </div>
