## PB4813: the 2002 canary was refused, and the owner moved 2002 to its text layer; PB4917 files the refusal written as a page

A note-only change. The 2002 canary batch returned one transcription out of five pages ($0.10). For the other four, the
model declined, naming the per-copy license line on each page, and wrote a summary instead. `collect` accepted the
summaries as pages. They were deleted, and no full run was submitted. The license line was not hidden to get past the
refusal. The owner chose the text-layer route: the 2002 PDF has a true text layer, so its text is extracted directly with no
model, its general formats are measured by the 2023 vector arm, and the local model helps only where the structure is
ambiguous. That is one more backend of `transcribe_pdf.py`, not a second converter. PB4917 files the silent failure: a
refusal or a summary must be recorded as a failed page, never written.
