#if UNITY_EDITOR

using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Cheats
{
    [CustomEditor(typeof(Object), true, isFallback = false)]
    [CanEditMultipleObjects]
    public class ButtonEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            foreach(var target in targets)
            {
                var mis = target.GetType().GetMethods().Where(m => m.GetCustomAttributes().Any(a => a.GetType() == typeof(EditorButtonAttribute)));
                if (mis != null)
                {
                    foreach(var mi in mis)
                    {
                        if (mi != null)
                        {
                            var attribute = (EditorButtonAttribute)mi.GetCustomAttribute(typeof(EditorButtonAttribute));
                            if (GUILayout.Button(attribute.Text)) {
                                mi.Invoke(target, null);
                            }
                        }
                    }
                }

            }
        }
    }
}

#endif