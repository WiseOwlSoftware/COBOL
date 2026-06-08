// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;

namespace CobolNet.CodeGen;

/// <summary>
/// A minimal indentation-aware writer for emitting readable C# source text. COBOL.NET emits C# as text and
/// hands it to Roslyn to parse + compile, so the generated <c>.cs</c> is directly inspectable — a deliberate
/// design choice (the output is meant to read like hand-written .NET).
/// </summary>
public sealed class CodeWriter
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private bool _atLineStart = true;

    /// <summary>Increase the indentation level by one (4 spaces).</summary>
    public void Indent() => _indent++;

    /// <summary>Decrease the indentation level by one.</summary>
    public void Outdent() => _indent = Math.Max(0, _indent - 1);

    /// <summary>Write a line at the current indentation. An empty argument writes a blank line.</summary>
    public void Line(string text = "")
    {
        if (text.Length == 0)
        {
            _sb.Append('\n');
            _atLineStart = true;
            return;
        }
        WriteIndentIfNeeded();
        _sb.Append(text).Append('\n');
        _atLineStart = true;
    }

    /// <summary>
    /// Write an open-brace line and increase indentation; pair with <see cref="CloseBrace"/>. Returns a
    /// disposable so callers can use a <c>using</c> block to scope a brace pair.
    /// </summary>
    public BlockScope Block(string header)
    {
        Line(header);
        Line("{");
        Indent();
        return new BlockScope(this);
    }

    /// <summary>Outdent and write a closing brace.</summary>
    public void CloseBrace()
    {
        Outdent();
        Line("}");
    }

    private void WriteIndentIfNeeded()
    {
        if (!_atLineStart) return;
        _sb.Append(' ', _indent * 4);
        _atLineStart = false;
    }

    /// <summary>The accumulated C# source.</summary>
    public override string ToString() => _sb.ToString();

    /// <summary>A <c>using</c>-scoped brace pair; closing it emits the matching <c>}</c>.</summary>
    public readonly struct BlockScope(CodeWriter writer) : IDisposable
    {
        /// <summary>Emit the closing brace for the block.</summary>
        public void Dispose() => writer.CloseBrace();
    }
}
