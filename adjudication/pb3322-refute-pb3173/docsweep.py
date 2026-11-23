# usage (from the repo root): python docsweep.py doc   (DOC-A.1 items)  |  python docsweep.py   (the other non-85 rules)
import json, re, sys
batch = json.load(open('adjudication/pb3173-editions/batch.json', encoding='utf-8'))['records']
cat = json.load(open('docs/rearchitecture/spec-rule-catalog.json', encoding='utf-8'))
rules = cat['rules'] if isinstance(cat, dict) and 'rules' in cat else cat
text = {r['id']: r for r in rules}
def norm(s): return re.sub(r'[^a-z0-9]+', ' ', s.lower()).strip()
t02 = norm(open('specs/ISO_COBOL_2002.md', encoding='utf-8').read())
t14 = norm(open('specs/ISO_COBOL_2014.md', encoding='utf-8').read())
t85 = norm(open('specs/ANSI_COBOL_1985.md', encoding='utf-8').read())
bad = []
for r in batch:
    rid = r['rule-id']
    if (rid.startswith('DOC-A.1-') != (sys.argv[1:] == ['doc'])) or r['editions'].startswith('85') and sys.argv[1:] != ['doc']:
        continue
    subj = text[rid]['text'].split('. This item')[0]
    key = norm(subj)[:70]
    in02, in14, in85 = key in t02, key in t14, key in t85
    band = r['editions']
    first = band.split(',')[0]
    exp_first = '85' if in85 else '2002' if in02 else '2014' if in14 else '2023'
    flag = '' if exp_first == first else '  <-- MISMATCH'
    print(f"{rid:12} band={band:15} in85={in85!s:5} in02={in02!s:5} in14={in14!s:5}{flag} | {subj[:70]}")
