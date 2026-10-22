/*
    WhatExec.Lib
    Copyright (c) 2025-2026 Alastair Lundy

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.IO.Abstractions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace WhatExec.Lib.Permissions;

/// <summary>
/// Evaluates whether the current user holds effective execute access to a file by reading the
/// file's security descriptor from an open handle and walking its DACL in stored order, applying
/// deny entries before allow entries and letting the first matching entry decide the verdict.
/// </summary>
/// <remarks>
/// The current user's SID and group SIDs are resolved once in the constructor and held for the
/// life of the instance, so no identity read happens on the per-file path. On non-Windows
/// platforms the constructor returns without resolving an identity (it never throws), and
/// <see cref="HasEffectiveExecuteAccess"/> returns <see langword="false"/> for every file.
/// <para>
/// Documented limitations, where the verdict can differ from the operating system:
/// <list type="bullet">
///     <item>The owner implicit grant is not modelled: the owner receives READ_CONTROL and
///     WRITE_DAC and never execute, so owner status cannot change an execute verdict.</item>
///     <item>Deny-only and disabled group SIDs, privilege-based bypasses, and the null-DACL case
///     are not modelled.</item>
///     <item>The verdict is a snapshot of the descriptor at read time and does not guarantee the
///     file stays executable afterwards.</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class WindowsEffectiveAccessEvaluator
{
    private const int GenericReadBit = unchecked((int)0x80000000);
    private const int GenericWriteBit = 0x40000000;
    private const int GenericExecuteBit = 0x20000000;
    private const int GenericAllBit = 0x10000000;

    private const int FileGenericRead = 0x00120089;
    private const int FileGenericWrite = 0x00120116;
    private const int FileGenericExecute = 0x001200A0;
    private const int FileAllAccess = 0x001F01FF;

    /// <summary>
    /// The specific execute right (FILE_EXECUTE) a mapped access mask must carry to grant
    /// execute access.
    /// </summary>
    private const int FileExecute = 0x00000020;

    private readonly SecurityIdentifier? _userSid;

    private readonly SecurityIdentifier[] _groupSids = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsEffectiveAccessEvaluator"/> class and,
    /// on Windows, resolves the current user's SID and the full set of its group SIDs once.
    /// </summary>
    /// <remarks>
    /// The group set is taken from <see cref="WindowsIdentity.Groups"/> unfiltered: deny-only and
    /// disabled SIDs are not removed because the public surface exposes no such attributes. On
    /// non-Windows platforms the constructor returns without resolving an identity and does not
    /// throw.
    /// </remarks>
    internal WindowsEffectiveAccessEvaluator()
    {
        if (!OperatingSystem.IsWindows())
            return;

        using WindowsIdentity identity = WindowsIdentity.GetCurrent();

        _userSid = identity.User;
        _groupSids = identity.Groups?.OfType<SecurityIdentifier>().ToArray() ?? [];
    }

    /// <summary>
    /// Determines whether the current user holds effective execute access to the file described
    /// by an open <paramref name="stream"/>.
    /// </summary>
    /// <remarks>
    /// The security descriptor is read from the open handle rather than by path, so the
    /// descriptor and the stream's file always describe the same file. Entries are examined in
    /// stored order: deny entries are applied before allow entries, and the first matching entry
    /// that carries the execute right wins. The mere presence of an execute rule for a trustee
    /// the current user does not belong to does not satisfy the probe.
    /// </remarks>
    /// <param name="stream">The open stream whose file security descriptor is evaluated.</param>
    /// <returns><see langword="true"/> if a matching allow entry grants execute access and no
    /// matching deny entry precedes it; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is
    /// <see langword="null"/>.</exception>
    internal bool HasEffectiveExecuteAccess(FileSystemStream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!OperatingSystem.IsWindows())
            return false;

        FileSecurity security = stream.GetAccessControl();
        byte[] binary = security.GetSecurityDescriptorBinaryForm();
        RawSecurityDescriptor descriptor = new RawSecurityDescriptor(binary, 0);
        RawAcl? dacl = descriptor.DiscretionaryAcl;

        return dacl is not null && EvaluateDacl(dacl);
    }

    /// <summary>
    /// Replaces each generic access right bit present in <paramref name="mask"/> with the
    /// corresponding file-specific rights from the standard file generic mapping, and returns
    /// the result.
    /// </summary>
    /// <param name="mask">The raw stored access mask, possibly carrying generic bits.</param>
    /// <returns>The mask with every present generic bit mapped to its file-specific
    /// rights.</returns>
    internal static int MapGenericRights(int mask)
    {
        if ((mask & GenericAllBit) != 0)
            mask = (mask & ~GenericAllBit) | FileAllAccess;

        if ((mask & GenericReadBit) != 0)
            mask = (mask & ~GenericReadBit) | FileGenericRead;

        if ((mask & GenericWriteBit) != 0)
            mask = (mask & ~GenericWriteBit) | FileGenericWrite;

        if ((mask & GenericExecuteBit) != 0)
            mask = (mask & ~GenericExecuteBit) | FileGenericExecute;

        return mask;
    }

    /// <summary>
    /// Walks the DACL in stored order, applying deny entries before allow entries and letting
    /// the first matching entry win.
    /// </summary>
    /// <param name="dacl">The discretionary ACL to evaluate.</param>
    /// <returns><see langword="true"/> if a matching allow entry grants execute access and no
    /// matching deny entry precedes it; otherwise <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private bool EvaluateDacl(RawAcl dacl)
    {
        for (int i = 0; i < dacl.Count; i++)
        {
            if (IsDenyExecuteMatch(dacl[i]))
                return false;
        }

        for (int i = 0; i < dacl.Count; i++)
        {
            if (IsAllowExecuteMatch(dacl[i]))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the ACE is a deny entry, trusted by the current user, and carries the
    /// execute right.
    /// </summary>
    /// <param name="ace">The DACL entry to test.</param>
    /// <returns><see langword="true"/> if the ACE denies the current user execute access;
    /// otherwise <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private bool IsDenyExecuteMatch(GenericAce ace)
    {
        return ace is QualifiedAce { AceType: AceType.AccessDenied or AceType.AccessDeniedObject } denyAce
               && GrantsExecute(denyAce)
               && IsTrustedByCurrentIdentity(denyAce);
    }

    /// <summary>
    /// Determines whether the ACE is an allow entry, trusted by the current user, and carries
    /// the execute right.
    /// </summary>
    /// <param name="ace">The DACL entry to test.</param>
    /// <returns><see langword="true"/> if the ACE grants the current user execute access;
    /// otherwise <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private bool IsAllowExecuteMatch(GenericAce ace)
    {
        return ace is QualifiedAce { AceType: AceType.AccessAllowed or AceType.AccessAllowedObject } allowAce
               && GrantsExecute(allowAce)
               && IsTrustedByCurrentIdentity(allowAce);
    }

    /// <summary>
    /// Determines whether the ACE's raw stored access mask carries the execute right after
    /// generic rights are mapped, so a stored generic-execute bit (0x20000000) also satisfies
    /// the test.
    /// </summary>
    /// <param name="ace">The qualified DACL entry whose access mask is tested.</param>
    /// <returns><see langword="true"/> if the mapped mask includes the execute right; otherwise
    /// <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private static bool GrantsExecute(QualifiedAce ace)
    {
        return (MapGenericRights(ace.AccessMask) & FileExecute) != 0;
    }

    /// <summary>
    /// Determines whether the ACE's trustee is the current user's SID or one of the group SIDs
    /// resolved in the constructor.
    /// </summary>
    /// <param name="ace">The qualified DACL entry whose trustee is tested.</param>
    /// <returns><see langword="true"/> if the trustee matches the current user's SID or one of
    /// its group SIDs by value; otherwise <see langword="false"/>.</returns>
    [SupportedOSPlatform("windows")]
    private bool IsTrustedByCurrentIdentity(QualifiedAce ace)
    {
        if (ace.SecurityIdentifier is not SecurityIdentifier trustee)
            return false;

        if (_userSid is not null && _userSid.Equals(trustee))
            return true;

        return _groupSids.Any(groupSid => groupSid.Equals(trustee));
    }
}
