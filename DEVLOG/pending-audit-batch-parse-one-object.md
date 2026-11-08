## audit_batch: an answer is the one JSON object at the reply's first brace

PB4995's drafts pass rejected a complete draft as `unparsed` twice: the model followed the object with a stray `}`,
and `parse_answer` cut the reply at its LAST `}`. It now reads the one JSON value that starts at the first `{`
(`json.JSONDecoder.raw_decode`), so trailing prose or a stray brace is not part of the answer; the self-test plants the
stray brace, a fenced reply, trailing prose and a truncated object. No other batch tool cuts JSON at its last brace.
