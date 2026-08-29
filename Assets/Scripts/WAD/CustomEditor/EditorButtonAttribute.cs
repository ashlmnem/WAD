#if UNITY_EDITOR
using System;

namespace Cheats
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EditorButtonAttribute : Attribute
    {
        public string Text;
        public EditorButtonAttribute(string text) {
            Text = text;
        }
    }
}

#endif