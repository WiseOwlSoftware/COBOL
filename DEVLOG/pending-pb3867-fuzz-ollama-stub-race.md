## PB3867: the fuzz harness's self-test counts its stub's finished requests instead of sharing one flag

PR #682 (PB3569) went RED in CI's `audits` job on `fuzz_ollama.py --self-test`, a test of PB3537's harness that the
branch does not touch. The stub Ollama server ended each request by setting one shared `threading.Event`, which each
`chat()` call cleared: the previous request's handler, still in its `finally`, could set it after the next request
cleared it, so the enumeration-cut check read the stub before the endless handler had seen the client close. In a WSL
clone of main it failed 1 run in 8, then 7 in 40. The stub now counts the handlers that returned under a condition
variable, and the check waits until every request so far has one; the fix failed 0 of 40 runs there and passes on
Windows.
