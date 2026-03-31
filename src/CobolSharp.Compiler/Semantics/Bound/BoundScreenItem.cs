// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

namespace CobolSharp.Compiler.Semantics.Bound;

/// <summary>
/// Screen position: absolute or relative (LINE/COL with optional PLUS).
/// </summary>
public sealed record ScreenPosition(bool IsRelative, int Value);

/// <summary>
/// Represents a SCREEN SECTION data description entry.
/// Mirrors the level-number hierarchy of regular data items.
/// Runtime I/O is deferred — this is grammar + semantic model only.
/// </summary>
public sealed class BoundScreenItem
{
    public string? Name { get; set; }
    public int Level { get; set; }
    public bool IsGroup => Children.Count > 0;

    // Position
    public ScreenPosition? Line { get; set; }
    public ScreenPosition? Column { get; set; }

    // Blank/Erase
    public bool BlankScreen { get; set; }
    public bool BlankLine { get; set; }
    public bool EraseEol { get; set; }
    public bool EraseEos { get; set; }

    // Attributes
    public bool Bell { get; set; }
    public bool Blink { get; set; }
    public bool Highlight { get; set; }
    public bool Lowlight { get; set; }
    public bool ReverseVideo { get; set; }
    public bool Underline { get; set; }
    public int? ForegroundColor { get; set; }
    public int? BackgroundColor { get; set; }

    // Input behavior
    public bool Secure { get; set; }
    public bool Auto { get; set; }
    public bool Full { get; set; }
    public bool Required { get; set; }

    // Data binding
    public string? PicString { get; set; }
    public string? FromSource { get; set; }
    public string? ToTarget { get; set; }
    public string? UsingField { get; set; }
    public string? Value { get; set; }

    // Hierarchy
    public List<BoundScreenItem> Children { get; } = [];
    public BoundScreenItem? Parent { get; set; }

    internal void AddChild(BoundScreenItem child)
    {
        child.Parent = this;
        Children.Add(child);
    }
}
