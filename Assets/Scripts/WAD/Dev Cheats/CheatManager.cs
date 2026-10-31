using UnityEngine;

namespace Cheats
{
    public class CheatManager : MonoBehaviour
    {
        public bool are_cheats_enabled_by_default;
        public static bool cheats_enabled;

        void Awake()
        {
            // WICHTIG: EnableCheats()/DisableCheats() muessen auch im Player-Build
            // existieren. Vorher lagen sie komplett in '#if UNITY_EDITOR', wodurch
            // dieser Awake im Build nicht kompilierte (CS0103).
            if (are_cheats_enabled_by_default)
            {
                EnableCheats();
            }
            else
            {
                DisableCheats();
            }
        }

        public void EnableCheats()
        {
            cheats_enabled = true;
            PrintToLogs();
        }

        public void DisableCheats()
        {
            cheats_enabled = false;
            PrintToLogs();
        }

        public void PrintToLogs()
        {
            Debug.Log($"cheats are {(cheats_enabled ? "enabled" : "disabled")} now");
        }

#if UNITY_EDITOR
        // Inspector-Buttons (siehe ButtonEditor.cs) gibt es nur im Editor.
        [EditorButton("Enable")]
        private void EnableCheatsButton() => EnableCheats();

        [EditorButton("Disable")]
        private void DisableCheatsButton() => DisableCheats();
#endif
    }
}
