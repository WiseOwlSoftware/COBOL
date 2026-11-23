## Decision PB6135: the 1985 transcription closes on mechanical checks and a PDF text diff, not on more full passes

Passes 3 to 5 of the 1985 Markdown each cost about 10M tokens and confirmed 7, 5 and 4 new errors: repeated agent readings
do not converge cheaply. The owner approved closing PB6051 when every error class found has a whole-document check GREEN in
the gate and a PDF text-layer diff reports no unexplained difference; no further full pass runs.
