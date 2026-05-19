using System;

[AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
public sealed class JsonIgnoreAttribute : Attribute
{
}
