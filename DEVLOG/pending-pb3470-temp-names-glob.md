## Filed PB3470, the temp_names.py glob blind spot

`scripts/temp_names.py` flags a fixed name joined to the shared temp directory but not a glob over it, which let the
conformance harness's `--self-test --keep` check delete other runs' `conformance-kept-*` directories and turned a Linux
gate red. Filed as kb/Work PB3470 (MINOR, process-only, silent); note and this entry only.
