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
    <p><strong>The first day's whole landing chain is down — twenty landings — and battery #42 closed it on the morning of 2026-09-03</strong> with every test leg green. Its two red lines were a deliberate rebaseline (seven differential flips, every one the same named refusal of the declined screen surface) and a stale, untracked local rendering that the tracked tree never carried. What runs now is read-only: lane 3's second batch (seven subjects still to adjudicate, then all twenty refuted four at a time on a pinned compiler) and the golden lane's second round (one file left to draft, then validation and a lander). The collation key cache's Linux-only transient turned out to be a missing memory bound and landed the same morning; one implementer is in a worktree on six misfiled citations an un-gated audit found.</p>
    <ol>
      <li><strong>Fix lane</strong> — ranked by what a defect does to a user's program, not by its severity label; the head of the list is in the tile above.</li>
      <li><strong>Adjudication lane (lane 3)</strong> — <strong>batch 1 is registered</strong> (OPEN · READ · RELEASE · RETURN · START · UNLOCK · USE). Its findings are now notes: eighty-five mechanisms became fifty-seven, clustered across the parts and statements that share one root — one note for a phrase hoisted out of its repetition group, one for the carrier that turns a syntax-rule violation into a run-time <em>not implemented</em>, one for the audit that reports zero candidates across every parser rule. Batch 2 is being refuted from its on-disk checkpoints; its registrar follows, then batch 3 (inputs generated, compiler pinned). Every finding became a <span class="mono">kb/Work</span> note before it became a DEVLOG paragraph.</li>
      <li><strong>The comprehensive battery</strong> — #42 closed on 2026-09-03; the next is owed by the first landing that touches the compiler or its tests, and the tile above says whether one is outstanding.</li>
      <li>Last, as before: the PB55 absorption campaign, with its step-0 harvest precondition.</li>
    </ol>
  </div>
  <div class="cardgrid">
    <div class="card">
      <h3>Owner decisions open <span class="pill warn">1</span></h3>
      <p><strong>PB280 Q1–Q3, PB371 and PB386 are answered</strong> and no longer open; only the pointer-edition question remains.</p>
      <ul>
        <li><strong>Q1 — answered: DOCUMENTED-NON-SUPPORT.</strong> An A.1-<em>optional</em> element whose §7 determination reads "Not provided." is documented non-support, on A.1's own preamble ("if the element is provided by the implementor…") plus §4.2.7. Landed as the <span class="mono">a1-optional-not-provided</span> derived selector — a predicate over the requirement class the standard states and the determination this register filed — so it settles the two held rows today and the rest as they are determined, with no further adjudication. The rows stay open until a witness pins the documented posture.</li>
        <li><strong>Q2 — answered: no.</strong> A DOC row with nothing in the compiler to observe stays a GAP; the definition of DONE is not widened. Only 13 of 222 items are cited by any source, so a "nothing to observe" claim would be unfalsifiable for the other 209.</li>
        <li><strong>Q3 — answered: yes, and state the value.</strong> This repo's <span class="mono">CONFORMANCE.md</span> is the §4.2.16 user documentation; a determination may cite the governing host document, with its version, but shall also state the resulting value — because the value is what a witness can pin, and a by-reference row states none.</li>
        <li><strong>Q4 — answered (PB371): CONFORMS, with a witness pinning the refusal.</strong> A syntax rule that constrains a <em>claimed</em> statement, but whose antecedent only a <em>declined</em> module can create, closes on the negative golden that pins the antecedent's named diagnostic; landed 2026-09-03 — and the family turned out to be seven, not fourteen: the other seven state live content under the complement of the declined clause. The CLASS clause landed beside it, declined alongside VALIDATE.</li>
        <li><strong>PB386 — answered 2026-09-03: yes, checkable only.</strong> Golden-lane round 2 emptied the CONFORMS-but-untested band down to nine rows, and eight can never earn a witness: a rule on Annex A.2's undefined list, an antecedent no device or medium the compiler has can satisfy, a consequent indistinguishable from its neighbour. A checkable, owner-signed <em>derivation</em> recorded in CONFORMANCE.md may now stand in place of a test for exactly that class — the A.2 arm verified mechanically against the standard's own list, the other two as reviewed arguments — with the schema and its C# twin changing together and a drift test refusing a derivation on any row that does carry an obligation. It is now a fix-lane item.</li>
        <li><strong>Still open, and not yet a bare question:</strong> the introducing edition of <span class="mono">USAGE POINTER TO type-name</span> — it needs the 2002 and 2014 texts before it can be asked.</li>
      </ul>
    </div>
    <div class="card">
      <h3>Open builds</h3>
      <ul>
        <li><strong>The golden lane</strong> — drafting the spec-derived witnesses for the resolved-but-unwitnessed band; the single largest closable block on the board, and it needs no compiler change</li>
        <li><strong>Phase-B dossier tooling</strong> — the adjudication lane's instrument: agents verify, they do not search. Batch 1's fifteen subjects reported the same seven blind spots, chief among them that a citation-keyed selector cannot see code that <em>fails</em> to implement a rule — which is most of what a conformance review finds</li>
        <li><strong>Named diagnostics for three measured modules</strong> — PB281 (FORMAT / SELECT WHEN), PB282 (REWRITE / WRITE FILE), PB283 (VALIDATE), each a generic parse error today where the posture is <em>declined</em></li>
        <li><strong>Worktree-aware fleet guard</strong> — a running fleet freezes the tree, so no sibling edit can race it</li>
        <li><strong>PB380 — a one-shot initialiser whose loser does not wait</strong> — PB377's family (the cache's missing bound landed 2026-09-03); measure whether two run units can race it before calling it a defect</li>      </ul>
    </div>
  </div>
