using System;
using System.Runtime;
using System.Runtime.InteropServices;

namespace Takai.Data
{
    /// <summary>
    /// Marks a class/property that can be edited in a designer
    /// </summary>
    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Property |
                    AttributeTargets.Field, Inherited = true)]
    [ComVisible(true)]
    public class DesignerModdableAttribute : Attribute { }
}
