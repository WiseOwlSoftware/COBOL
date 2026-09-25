import re,collections,sys
log=open(sys.argv[1]).read()
need=collections.OrderedDict()
for m in re.finditer(r'inventory (DOC-A\.1-\d+) cites the spec-derived test "([^"]+)"',log):
    need.setdefault(m.group(1),[])
    if m.group(2) not in need[m.group(1)]: need[m.group(1)].append(m.group(2))
p='docs/CONFORMANCE.md'; t=open(p,encoding='utf-8').read().split('\n')
for i,l in enumerate(t):
    for k,refs in need.items():
        if l.startswith(f'| {k} |'):
            a,cur=l.rstrip().rstrip('|').rsplit('|',1); cur=cur.strip()
            add='; '.join(f'`{r}`' for r in refs)
            t[i]=a+'| '+(add if cur in ('—','-','') else cur+'; '+add)+' |'; print(k)
open(p,'w',encoding='utf-8').write('\n'.join(t))
