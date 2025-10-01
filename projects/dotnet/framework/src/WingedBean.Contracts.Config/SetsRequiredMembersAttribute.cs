using System;

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

// Required for C# 11 required members in netstandard2.1
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
internal sealed class SetsRequiredMembersAttribute : Attribute
{
}
