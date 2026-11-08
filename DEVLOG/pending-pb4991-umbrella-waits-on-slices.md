## PB4991: the umbrella decision waits on its open slices

The operator found `plan_wave.py --cluster PB4991` planning the umbrella note PB4991 itself as an unpartitioned group
(open, `blocked_by: []`). The umbrella is finished only when its slices are, a result dependency, so it now names
the open slices PB4993-PB4997 in `blocked_by`, and no campaign plan dispatches it.
