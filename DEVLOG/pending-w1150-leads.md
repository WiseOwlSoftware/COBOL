## Wave 1150's two review leads filed: PB3534, PB3535

Wave 1150 left two leads, now filed as register notes. PB3534 records that three drift classes (BitRunImageDriftTests at 531 s, OperandClassScreenDriftTests at 224 s and CompiledProgramCacheDriftTests at 204 s) re-read the tracked tree once per test case and so set the floor of every gate's leg 1; the fix is one tree read shared per assembly, measured before and after. PB3535 records an unreachable branch and a matching docstring sentence in `place_rows` of the spec-figure renderer.
