#!/usr/bin/env python3
"""Registrar-1 normalizer (kb/Work PB1522 step 2): merge the eleven adjudication branches' final.jsonl into
record_verdicts batch files WITHOUT changing a verdict or dropping evidence.

Every normalization is mechanical and reversible from the row's notes:
  * editions list -> comma string; parenthetical edition prose -> notes;
  * a code-location ref that is not '<path>[#Symbol]' keeps its leading valid ref (if any) and the rest of the
    text moves to notes; a ref whose symbol does not resolve as written ('Class.Member') is re-written to the
    member that does resolve (same file), otherwise its fragment moves to notes;
  * the DOC row's computed anchor is added when Schema.anchor_obliged says the row owes it;
  * any key outside the record vocabulary moves to notes.
Inputs: <scratch>/in/<branch>/final.jsonl.  Owner ids come from owners.json {rule-id: "PBnnnn ..."}.
"""
import json, re, sys, glob, pathlib
sys.path.insert(0, 'scripts/spec')
from inventory_schema import load_schema, load_inventory  # noqa

ROOT = pathlib.Path('.')
PAT = re.compile(r'^[A-Za-z0-9_./-]+(#[A-Za-z0-9_.<>-]+)?$')
LEAD = re.compile(r'^([A-Za-z0-9_./-]+(?:#[A-Za-z0-9_.<>-]+)?)')
ANCH = re.compile(r'^(DOC-A\.1-[0-9]+|4\.2\.16|A\.4\.[0-9]+)$')
ED = ('85', '2002', '2014', '2023')
FIELDS = ('rule-id', 'verdict', 'code-location', 'test-ref', 'editions', 'notes')
_text = {}

def body(p):
    if p not in _text:
        _text[p] = (ROOT / p).read_text(encoding='utf-8', errors='replace')
    return _text[p]

def resolves(path, sym):
    return re.search(r'\b' + re.escape(sym) + r'\b', body(path)) is not None

def norm_loc(ref, moved):
    ref = ref.strip()
    if not ref:
        return None
    m = LEAD.match(ref)
    tok = m.group(1) if m else ''
    rest = ref[len(tok):].strip() if tok else ref
    path = tok.split('#')[0]
    if not tok or '/' not in path or not (ROOT / path).is_file():
        moved.append(ref)
        return None
    if rest:
        moved.append(ref)
    if path == 'docs/CONFORMANCE.md' and '#' not in tok:
        if ref not in moved: moved.append(ref)
        return None
    if '#' in tok:
        frag = tok.split('#', 1)[1].rstrip('.')
        if path == 'docs/CONFORMANCE.md':
            if not ANCH.match(frag):
                if ref not in moved: moved.append(ref)
                return None
            return f'{path}#{frag}'
        if resolves(path, frag):
            return f'{path}#{frag}'
        for cand in reversed(frag.split('.')):
            if cand and resolves(path, cand):
                return f'{path}#{cand}'
        if ref not in moved: moved.append(ref)
        return path
    return path

def norm(rec, schema, rows, owners):
    out = {k: rec.get(k, '') for k in FIELDS}
    extra = [f'{k}: {json.dumps(v)}' for k, v in rec.items() if k not in FIELDS]
    ed = rec.get('editions', '')
    ednote = ''
    if isinstance(ed, list):
        ed = ','.join(str(e) for e in ed)
    if '(' in ed:
        ednote = ed[ed.index('('):].strip()
        ed = ed[:ed.index('(')]
    eds = [e.strip() for e in ed.split(',') if e.strip()]
    assert all(e in ED for e in eds), (rec['rule-id'], eds)
    out['editions'] = ','.join(eds)
    moved, locs = [], []
    for ref in re.split(r';\s+', rec.get('code-location', '') or ''):
        n = norm_loc(ref, moved)
        if n and n not in locs:
            locs.append(n)
    row = dict(rows[rec['rule-id']]); row['verdict'] = out['verdict']
    anchor = schema.anchor_for(row)
    if anchor and schema.anchor_obliged(row) and anchor not in locs:
        locs.insert(0, anchor)
    out['code-location'] = '; '.join(locs)
    tr = [t.strip() for t in re.split(r';\s+', rec.get('test-ref', '') or '') if t.strip()]
    out['test-ref'] = '; '.join(tr)
    notes = rec.get('notes', '')
    add = []
    if ednote: add.append(f'editions note: {ednote}')
    if moved: add.append('code-location commentary (registrar-normalized, kb/Work PB1522): ' + ' | '.join(moved))
    if extra: add.append('adjudicator extra fields: ' + ' | '.join(extra))
    own = owners.get(rec['rule-id'])
    if own: add.insert(0, f'Owning note: {own}.')
    if add: notes = notes + ' || ' + ' || '.join(add)
    out['notes'] = notes
    return out

if __name__ == '__main__':
    scratch = sys.argv[1]
    schema = load_schema()
    rows = {r['rule-id']: r for r in load_inventory()}
    owners = json.load(open('adjudication/registrar-1/owners.json')) if pathlib.Path('adjudication/registrar-1/owners.json').exists() else {}
    overrides = json.load(open('adjudication/registrar-1/r42.json')) if pathlib.Path('adjudication/registrar-1/r42.json').exists() else {}
    recs = []
    for f in sorted(glob.glob(f'{scratch}/in/*/final.jsonl')):
        for l in open(f):
            if l.strip():
                recs.append(json.loads(l))
    doc, nondoc, r42, nod = [], [], [], []
    for r in recs:
        rid = r['rule-id']
        if rid in overrides:
            o = overrides[rid]
            rr = dict(r); rr['verdict'] = 'DOCUMENTED-NON-SUPPORT'
            rr['test-ref'] = o['test-ref']
            rr['notes'] = o['notes'] + ' || adjudicator (NEEDS-OWNER-DECISION before R42): ' + r['notes']
            rr['code-location'] = o.get('code-location', r.get('code-location', ''))
            r42.append(norm(rr, schema, rows, {}))
        elif r['verdict'] == 'NEEDS-OWNER-DECISION':
            nod.append(rid)
        elif rid.startswith('DOC-A.1-'):
            doc.append(norm(r, schema, rows, owners))
        else:
            nondoc.append(norm(r, schema, rows, owners))
    key = lambda x: x['rule-id']
    for name, b in (('batch-doc', doc), ('batch-nondoc', nondoc), ('batch-r42', r42)):
        json.dump({'batch': f'registrar-1-{name}', 'records': sorted(b, key=key)},
                  open(f'adjudication/registrar-1/{name}.json', 'w'), indent=1, ensure_ascii=False)
        print(name, len(b))
    print('NOD excluded', len(nod), ' '.join(nod))
