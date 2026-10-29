## The PB3777 once-check refused a one-file group's spec, so wave 20261751 launched nothing: fixed as PB4054

`plan_wave.py` planned wave 20261751 with three groups, and `check_practices.py` refused group B's rendered spec (PB4031, one
file: `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/ReportWriterEmitter.cs`). The template ends both `THE GROUP IS ONE ROOT:` and
`CODE SITES the notes name:` with the group's files. For a group of one file with no claimed members both are the bare path,
and `repeated_sentences` splits after a colon, so it counted the 63-character `<path>.` as a sentence stated twice. The plan
went RED, reserved nothing and wrote no wf-args file, and the wave unit launched no fleet.

PB3777 requires each rule to be stated once, and a lone path states no rule. `repeated_sentences` now counts only a piece of
two words or more. A new check in `test_plan_wave.py` renders a one-file group with the incident's path through the real
template. Without the fix it fails with the incident's message; with it the self-test passes 488/488. The wave's three real
specs pass the fixed check, and the existing checks still show it refusing a repeated real sentence. kb/Work PB4054.
