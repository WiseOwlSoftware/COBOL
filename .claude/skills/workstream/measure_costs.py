"""Where do implementer/finisher tokens go? Attributes each assistant message's tokens to the tool call it made.

    python .claude/skills/workstream/measure_costs.py <session-dir>/subagents/workflows

First run 2026-09-23 over waves 45-57 (DEVLOG 1663): search 31.7 %, read 14.5 %, edit 13.4 %, probe 9.5 %, git 7.9 %.
"""
import json, pathlib, re, sys, collections
root = pathlib.Path(sys.argv[1])
B = chr(100) + 'otnet'  # avoid the literal in this file
pat = [('wait', r'tail -n \+1 -f'), ('gate', B + r' test|build-local'), ('build', B + r' build'),
       ('probe compile/run', r'\bcobol(\.exe)?\b|Cobol\.Net\.Cli|\.cob\b'), ('cite', r'cite\.py'),
       ('register/gen scripts', r'work\.py|gen_conformance|record_verdicts|gen_ledger|verify\.py'),
       ('git', r'(^|&& |; )git '), ('search grep/rg/find', r'\b(grep|rg|find)\b'),
       ('read sed/cat/head', r'\b(sed -n|cat |head |tail )'), ('python', r'python')]
cnt = collections.Counter(); tok = collections.Counter(); agents = 0; turns_hist = []
for wf in root.iterdir():
    j = wf / 'journal.jsonl'
    if not j.exists(): continue
    labs = {}
    for l in j.read_text(encoding='utf-8').splitlines():
        try: e = json.loads(l)
        except Exception: continue
        if e.get('type') == 'started': labs[e['agentId']] = e.get('label', '')
    for aid, lab in labs.items():
        if re.search('train|land|registrar|battery', lab, re.I): continue
        f = wf / f'agent-{aid}.jsonl'
        if not f.exists(): continue
        agents += 1; t_i = 0; msgs = {}
        for line in f.read_text(encoding='utf-8', errors='ignore').splitlines():
            try: m = json.loads(line)
            except Exception: continue
            msg = m.get('message') or {}
            if not msg.get('usage') or not msg.get('id'): continue
            d = msgs.setdefault(msg['id'], {'u': msg['usage'], 'c': []})
            d['c'] += [c for c in (msg.get('content') or []) if isinstance(c, dict)]
        for d in msgs.values():
            t_i += 1
            u = d['u']
            t = sum(u.get(k, 0) for k in ('input_tokens', 'cache_read_input_tokens', 'cache_creation_input_tokens', 'output_tokens'))
            ks = []
            for c in d['c']:
                if c.get('type') == 'tool_use':
                    n = c.get('name'); inp = c.get('input', {})
                    if n in ('Bash', 'PowerShell'):
                        cmd = inp.get('command', '')
                        ks.append(next((a for a, p in pat if re.search(p, cmd)), 'other shell'))
                    elif n in ('Read', 'Grep', 'Glob'): ks.append('Read/Grep tool')
                    elif n in ('Edit', 'Write'): ks.append('edit')
                    else: ks.append(n)
            if not ks: ks = ['(text only)']
            for k in ks:
                cnt[k] += 1; tok[k] += t / len(ks)
        turns_hist.append(t_i)
T = sum(tok.values())
print('implementer+finisher agents', agents, 'median turns', sorted(turns_hist)[len(turns_hist)//2], 'max', max(turns_hist))
for k, v in cnt.most_common():
    print(f"{k:24} calls={v:5}  token-share={tok[k]/T:6.1%}")
