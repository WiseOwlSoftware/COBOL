#!/usr/bin/env python3
"""Render implementer/finisher dispatch specs from ONE template (MANDATORY-PRACTICES.md O1).

    python .claude/skills/workstream/make_dispatch_specs.py <groups.json>

groups.json:
{
  "wave": "58", "scratch": "E:\\\\Temp\\\\...\\\\scratchpad", "base": "c54434a8d or later — ...",
  "groups": [
    {"letter": "KA", "slug": "w58a", "group": "INTRINSICS BEYOND BINARY64", "notes": "PB999, PB1000", "lead": "PB999",
     "codes": "COBOLNET2400–COBOLNET2402", "root": "...", "files": "`kb/Work/PB999.md`, ...", "body": "...",
     "pred": ""}          # optional: predecessor-branch instruction
  ]
}
Writes <scratch>\\msg-w<wave>-<slug[-1]>.txt per group and runs check_practices.py over them (exit 1 on a miss).
"""
import json, pathlib, subprocess, sys

HERE = pathlib.Path(__file__).resolve().parent
TEMPLATE = HERE / 'templates' / 'dispatch-spec-implementer.md'


def main():
    cfg = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8'))
    tpl = TEMPLATE.read_text(encoding='utf-8')
    scratch = pathlib.Path(cfg['scratch'])
    out = []
    for g in cfg['groups']:
        pred = g.get('pred', '')
        body = tpl.format(wave=cfg['wave'], base=cfg['base'], S=str(scratch), pred=(pred + '\n') if pred else '',
                          **{k: v for k, v in g.items() if k != 'pred'})
        p = scratch / f"msg-w{cfg['wave']}-{g['slug'][-1]}.txt"
        p.write_text(body, encoding='utf-8')
        out.append(str(p))
        print('wrote', p)
    return subprocess.call([sys.executable, str(HERE / 'check_practices.py'), *out])


if __name__ == '__main__':
    sys.exit(main())
