/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

namespace WhatExec.Lib.Permissions;

/// <summary>
/// Owns WhatExec's definition of which file extensions count as executable on Windows: the union
/// of a fixed sixteen-entry list and the extensions named by the machine's <c>PATHEXT</c>
/// environment variable.
/// </summary>
/// <remarks>
/// <para>
/// The allow-list is this union, so a machine's own executable extensions are honoured alongside
/// WhatExec's fixed list. The list is therefore machine-specific, because <c>PATHEXT</c> is.
/// </para>
/// <para>
/// <c>PATHEXT</c> is read exactly once, in the constructor, and the resulting entries are held for
/// the life of the instance. No environment read occurs on the per-file detection path that forms
/// an Executable verdict, and a <c>PATHEXT</c> change made after construction is not seen by that
/// instance.
/// </para>
/// <para>
/// A <c>PATHEXT</c>-derived extension is judged on its extension and on the access check, with no
/// magic-number check applied, because the extensions <c>PATHEXT</c> adds beyond the sixteen
/// hardcoded entries, namely <c>.bat</c>, <c>.cmd</c>, <c>.vbs</c>, <c>.vbe</c>, <c>.js</c>,
/// <c>.jse</c>, <c>.wsf</c>, <c>.wsh</c>, <c>.msc</c>, <c>.py</c> and <c>.pyw</c>, have no PE
/// header. Where an extension is in both sources, namely <c>.com</c>, <c>.exe</c> and <c>.cpl</c>,
/// the hardcoded rule applies: <see cref="IsHardcodedExtension(string)"/> reports
/// <see langword="true"/> for it, so the consumer follows the hardcoded rule for those extensions.
/// </para>
/// </remarks>
internal sealed class ExecutableExtensionSet
{
    /// <summary>
    /// The sixteen fixed executable extensions WhatExec owns, unchanged and applied on every platform.
    /// </summary>
    private static readonly string[] HardcodedExtensionValues =
    [
        ".exe", ".msi", ".appx", ".com", ".sys", ".drv", ".mui", ".ocx",
        ".ax", ".msstyles", ".scr", ".cpl", ".acm", ".efi", ".dll", ".tsp"
    ];

    /// <summary>
    /// The hardcoded source, held per instance and compared case-insensitively.
    /// </summary>
    private readonly HashSet<string> _hardcodedExtensions = new(HardcodedExtensionValues, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The <c>PATHEXT</c> source, read once at construction and compared case-insensitively
    /// because <c>PATHEXT</c> is uppercase by convention on Windows.
    /// </summary>
    private readonly HashSet<string> _pathExtExtensions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableExtensionSet"/> class by reading the
    /// <c>PATHEXT</c> environment variable once, on Windows only.
    /// </summary>
    /// <remarks>
    /// The environment read happens only on Windows: on non-Windows platforms the read is skipped
    /// entirely and the sixteen hardcoded entries apply alone. An absent or empty <c>PATHEXT</c>
    /// value contributes no entries. The value is read once and held for the life of the instance,
    /// so a later <c>PATHEXT</c> change is not seen by this instance and no environment read
    /// remains on the per-file detection path.
    /// </remarks>
    internal ExecutableExtensionSet()
    {
        if (!OperatingSystem.IsWindows())
            return;

        AddPathExtEntries(Environment.GetEnvironmentVariable("PATHEXT"));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExecutableExtensionSet"/> class from an
    /// explicitly supplied <c>PATHEXT</c> value (used by tests).
    /// </summary>
    /// <remarks>
    /// This constructor performs no environment read; it only parses <paramref name="pathExt"/>.
    /// </remarks>
    /// <param name="pathExt">
    /// A <c>PATHEXT</c>-shaped value such as <c>.COM;.EXE;.BAT;.CMD</c>, split on <c>;</c>. An
    /// absent or empty value contributes no entries.
    /// </param>
    internal ExecutableExtensionSet(string? pathExt)
    {
        AddPathExtEntries(pathExt);
    }

    /// <summary>
    /// Determines whether the extension is one of WhatExec's sixteen fixed executable extensions.
    /// </summary>
    /// <remarks>
    /// The comparison is case-insensitive and the argument is normalised to a leading dot before
    /// comparison, so input such as <c>EXE</c>, <c>.exe</c> and <c> .exe </c> all match
    /// <c>.exe</c>. This method reports <see langword="true"/> for the extensions that
    /// appear in both sources, so the hardcoded rule wins where the two sources overlap.
    /// </remarks>
    /// <param name="extension">The extension to test; trimmed and given a leading dot if missing.</param>
    /// <returns><see langword="true"/> if the extension is one of the sixteen fixed entries; otherwise <see langword="false"/>.</returns>
    internal bool IsHardcodedExtension(string extension)
    {
        return _hardcodedExtensions.Contains(Normalise(extension));
    }

    /// <summary>
    /// Determines whether the extension comes from the <c>PATHEXT</c> source.
    /// </summary>
    /// <remarks>
    /// The comparison is case-insensitive and the argument is normalised to a leading dot before
    /// comparison, so <c>BAT</c>, <c>.bat</c> and <c> .bat </c> all match <c>.bat</c>. The entries
    /// come from a single constructor-time read, so no environment read happens here. A
    /// <c>PATHEXT</c>-derived extension is judged on its extension and on the access check, with no
    /// magic-number check applied, because those extensions have no PE header.
    /// </remarks>
    /// <param name="extension">The extension to test; trimmed and given a leading dot if missing.</param>
    /// <returns><see langword="true"/> if the extension is named by <c>PATHEXT</c>; otherwise <see langword="false"/>.</returns>
    internal bool IsPathExtExtension(string extension)
    {
        return _pathExtExtensions.Contains(Normalise(extension));
    }

    /// <summary>
    /// Determines whether the extension is in the allow-list, which is the union of the two sources.
    /// </summary>
    /// <remarks>
    /// The argument is normalised to a leading dot and compared case-insensitively. For an
    /// extension present in both sources (<c>.com</c>, <c>.exe</c> and <c>.cpl</c>) both
    /// <see cref="IsHardcodedExtension(string)"/> and <see cref="IsPathExtExtension(string)"/>
    /// report <see langword="true"/>; the consumer then applies the hardcoded rule, which governs
    /// the Executable verdict for those extensions.
    /// </remarks>
    /// <param name="extension">The extension to test; trimmed and given a leading dot if missing.</param>
    /// <returns><see langword="true"/> if either source contains the extension; otherwise <see langword="false"/>.</returns>
    internal bool Contains(string extension)
    {
        return IsHardcodedExtension(extension) || IsPathExtExtension(extension);
    }

    /// <summary>
    /// Parses a <c>PATHEXT</c>-shaped value into the per-instance <c>PATHEXT</c> source.
    /// </summary>
    /// <remarks>
    /// Entries are split on <c>;</c> (the Windows separator), trimmed, and skipped when empty. Each
    /// remaining entry is normalised to a leading dot and added case-insensitively. An absent or
    /// empty value contributes no entries.
    /// </remarks>
    /// <param name="pathExt">The value to parse; may be <see langword="null"/> or empty.</param>
    private void AddPathExtEntries(string? pathExt)
    {
        if (string.IsNullOrEmpty(pathExt))
            return;

        foreach (string entry in pathExt.Split(';'))
        {
            string normalised = Normalise(entry);

            if (normalised.Length == 0)
                continue;

            _pathExtExtensions.Add(normalised);
        }
    }

    /// <summary>
    /// Trims an entry or query argument and ensures it carries a leading dot, so both sources and
    /// the query methods normalise identically before comparison.
    /// </summary>
    /// <param name="value">The value to normalise; may be <see langword="null"/> or whitespace.</param>
    /// <returns>The trimmed, dot-prefixed value, or <see cref="string.Empty"/> when nothing remains after trimming.</returns>
    private static string Normalise(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();

        return trimmed.StartsWith('.') ? trimmed : "." + trimmed;
    }
}
