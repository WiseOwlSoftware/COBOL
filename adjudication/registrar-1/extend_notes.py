#!/usr/bin/env python3
"""Registrar-1: extend the OPEN/OWNER kb/Work notes that already own an adjudicated row's mechanism (kb/Work PB1522).
Adds the rows to `inventory_rows` (idempotent) and appends one dated section carrying each row's adjudicated verdict,
its branch and the adjudicators' finding text that named the note. Usage: extend_notes.py <scratch-in-dir>"""
import json, re, sys, glob, pathlib
sys.path.insert(0, 'adjudication/registrar-1')
from owners_map import EXTEND
SCR = sys.argv[1]
recs, branch, findings = {}, {}, []
for f in glob.glob(f'{SCR}/*/final.jsonl'):
    b = f.split('/')[-2]
    for l in open(f):
        if l.strip():
            r = json.loads(l); recs[r['rule-id']] = r; branch[r['rule-id']] = b
for f in glob.glob(f'{SCR}/*/findings.json'):
    d = json.load(open(f)); d = d.get('findings', d) if isinstance(d, dict) else d
    for x in d:
        x['_b'] = f.split('/')[-2]; findings.append(x)
R42 = {f'DOC-A.1-{n}' for n in (3, 4, 11, 27, 28, 41, 45, 83, 91, 172, 199)}
EXTRA = json.load(open('adjudication/registrar-1/extend_extra.json'))
MARK = '## Registrar-1 (kb/Work PB1522) — rows adjudicated on the `claude/adj-*` branches'
for note, rows in EXTEND.items():
    p = pathlib.Path(f'kb/Work/{note}.md'); t = p.read_text(encoding='utf-8')
    if MARK in t:
        continue
    m = re.search(r'^inventory_rows:\s*\[(.*?)\]', t, re.M | re.S)
    have = re.findall(r'"([^"]+)"', m.group(1))
    new = have + [r for r in rows if r not in have]
    t = t[:m.start()] + 'inventory_rows: ' + json.dumps(new, ensure_ascii=False) + t[m.end():]
    lines = [MARK, '', f'Filed 2026-09-24 by REGISTRAR-1 (cloud, branch `claude/adj-registrar-1`). The adjudicators (tree `0caa7d5`) '
             f'attributed these rows to this note; this note now claims them in `inventory_rows`.', '']
    for r in rows:
        v = recs[r]['verdict']; b = branch[r]
        state = ('recorded' if not r.startswith('DOC-A.1-') and v != 'NEEDS-OWNER-DECISION' else
                 'excluded from the batches (owner question below)' if v == 'NEEDS-OWNER-DECISION' else
                 'NOT recorded — DOC rows wait on owner decision PB1535')
        lines.append(f'- `{r}` — adjudicated **{v}** on `claude/{b}`; {state}.')
    fs = [x for x in findings if re.search(r'\b' + note + r'\b', str(x.get('owning_note', '')))
          and set(x.get('rule_ids', [])) & set(rows)]
    for x in fs:
        lines += ['', f'Adjudicator finding (`{x["_b"]}`, harm {x.get("harm")}): {x.get("mechanism", "")}', '',
                  f'- Repro: {x.get("repro", "")}', f'- Expected: {x.get("expected_from_spec", "")}',
                  f'- Observed: {x.get("observed_or_reasoning", "")}', f'- Code site: {x.get("code_site", "")}']
    if note in EXTRA:
        lines += ['', EXTRA[note]]
    lines += ['', 'Each row\'s full evidence (rule text with its `cite.py --check` line, probes, determination) is the record\'s '
              '`notes` in `adjudication/registrar-1/batch-{doc,nondoc}.json` on the registrar branch.']
    t = t.rstrip('\n') + '\n\n' + '\n'.join(lines) + '\n'
    p.write_text(t, encoding='utf-8')
    print(note, len(rows), 'rows,', len(fs), 'findings')
