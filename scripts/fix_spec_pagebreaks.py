#!/usr/bin/env python3
"""Repair sentences in specs/ISO_COBOL.md that a page-break block cut in half.

The markdown is an OCR transcription of the printed standard. At every printed page boundary the
transcription injects a block:

    ---                      (sometimes)
    <a id="page-N"></a>
    ## Page N
    # ISO/IEC 1989:2023 (E)  (the running header, sometimes)

When the boundary falls MID-SENTENCE, that block lands inside a rule's prose, so the sentence is split
into two paragraphs with roughly six lines of furniture between them. Anyone reading — human or tool —
who stops at the break gets half a rule. That is not hypothetical: deriving semantics from a truncated
rule is exactly how a spec misreading shipped earlier in this project.

THE REPAIR: join the two halves into one paragraph and move the page-break block to immediately AFTER
the completed sentence. The anchor is PRESERVED (scripts/render-spec-page.py and the figure audit map
markdown lines to PDF pages through it); it simply moves by at most one paragraph, which cannot
misattribute a figure because figures are their own blocks, never mid-sentence.

Conservative by construction — a join happens ONLY when:
  * the text before the break does not end with sentence-final punctuation, and
  * the text after the break begins lowercase (or with a continuation word), and
  * neither side is a heading, table row, code fence, anchor, or list item.
Everything else is left exactly as it is.
"""
from __future__ import annotations
import argparse, re, sys

ANCHOR = re.compile(r'^<a id="page-(\d+)"></a>$')
PAGE_H = re.compile(r'^#+\s*Page\s+\d+$')
# The running header appears in THREE transcribed shapes: '# ISO/IEC 1989:2023 (E)', '**ISO/IEC …**', and
# BARE 'ISO/IEC 1989:2023 (E)' with no markup at all. Missing the bare form is not a cosmetic slip — the
# joiner would splice the header string into the middle of a normative rule, corrupting the spec. Caught in
# the dry run on page 53; the assertion in repair() now makes that failure impossible to apply silently.
RUN_H = re.compile(r'^(?:#+\s*|\*\*)?ISO/IEC\s+1989:2023\s*\(E\)(?:\*\*)?$')
FOOTER = re.compile(r'^\s*(?:\d+\s+)?(?:©|\(c\)|Ⓒ)?\s*ISO/IEC\s+2023\s*$|^Licensed to .*prohibited\.?\s*$', re.I)
CONT = re.compile(r'^[a-z(]|^(and|or|the|of|to|in|a|an|is|are|that|which|for|with|by|shall|may)\b', re.I)
BLOCK_START = ('#', '<a', '|', '```', '>')


def is_furniture(s: str) -> bool:
    return (s == '' or s == '---' or bool(PAGE_H.match(s))
            or bool(RUN_H.match(s)) or bool(FOOTER.match(s)))


def find_splits(lines: list[str]):
    out = []
    for i, l in enumerate(lines):
        m = ANCHOR.match(l.strip())
        if not m:
            continue
        j = i - 1
        while j >= 0 and lines[j].strip() in ('', '---'):
            j -= 1
        if j < 0:
            continue
        before = lines[j].strip()
        k = i + 1
        while k < len(lines) and is_furniture(lines[k].strip()):
            k += 1
        if k >= len(lines):
            continue
        after = lines[k].strip()
        if not before or not after:
            continue
        if before.endswith(('.', ')', ':', ';', '?', '!', '|', '`', '**')) or before.startswith(BLOCK_START):
            continue
        if after.startswith(BLOCK_START) or re.match(r'^\d+[.)]\s|^[-—*]\s', after):
            continue
        if not CONT.match(after):
            continue
        out.append((j, i, k))          # (last line of first half, anchor line, first line of second half)
    return out


def repair(lines: list[str], splits) -> list[str]:
    # Work back-to-front so earlier indices stay valid.
    for j, i, k in sorted(splits, key=lambda t: -t[0]):
        block = [ln for ln in lines[j + 1:k] if ln.strip() != '' and ln.strip() != '---']
        joined = lines[j].rstrip() + ' ' + lines[k].lstrip()
        # HARD GUARD: never splice page furniture into a normative sentence.
        assert 'ISO/IEC 1989:2023' not in joined and 'Licensed to' not in joined, (
            f'refusing to join furniture into prose at line {j+1}: {joined[:160]!r}')
        lines[j:k + 1] = [joined, ''] + block + ['']
    return lines


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--path', default='specs/ISO_COBOL.md')
    ap.add_argument('--apply', action='store_true')
    ap.add_argument('--show', type=int, default=6)
    a = ap.parse_args()

    text = open(a.path, encoding='utf-8').read()
    lines = text.split('\n')
    splits = find_splits(lines)
    print(f'mid-sentence page-break splits found: {len(splits)}')

    for j, i, k in splits[:a.show]:
        print('\n--- BEFORE ' + '-' * 60)
        for n in range(j, min(k + 1, len(lines))):
            print(f'{n+1:6}| {lines[n]}')
        print('    AFTER  ' + '-' * 60)
        block = [ln for ln in lines[j + 1:k] if ln.strip() not in ('', '---')]
        print(f'      | {lines[j].rstrip() + " " + lines[k].lstrip()}')
        print('      |')
        for b in block:
            print(f'      | {b}')

    if a.apply:
        out = repair(lines, splits)
        open(a.path, 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
        # verify: no split should remain, and every anchor must survive
        after_lines = open(a.path, encoding='utf-8').read().split('\n')
        left = find_splits(after_lines)
        before_anchors = len([1 for l in text.split('\n') if ANCHOR.match(l.strip())])
        after_anchors = len([1 for l in after_lines if ANCHOR.match(l.strip())])
        print(f'\nAPPLIED. remaining splits: {len(left)}   anchors {before_anchors} -> {after_anchors}')
        if after_anchors != before_anchors:
            print('!! ANCHOR COUNT CHANGED — page mapping would break', file=sys.stderr)
            return 2
        if left:
            print(f'!! {len(left)} splits remain', file=sys.stderr)
            return 3
    return 0


if __name__ == '__main__':
    sys.exit(main())
