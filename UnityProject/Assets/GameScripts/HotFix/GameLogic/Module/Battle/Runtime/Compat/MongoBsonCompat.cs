using System;

namespace MongoDB.Bson.Serialization.Attributes
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public sealed class BsonIgnoreAttribute : Attribute
    {
    }
}
