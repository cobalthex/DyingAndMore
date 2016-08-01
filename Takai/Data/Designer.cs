using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Takai.Data
{
    /// <summary>
    /// Marks that a member should not show up in a designer (typically used in Tool)
    /// Usually unnecessary with NonSerialized
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true)]
    [ComVisible(true)]
    public class NonDesignedAttribute : Attribute { }
    
    /// <summary>
    /// Marks a class/class that can be created in a designer
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    [ComVisible(true)]
    public class DesignerCreatableAttribute : Attribute { }
}
