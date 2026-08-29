using UnityEngine;

namespace Cheats
{
    public class CheatManager : MonoBehaviour
    {
        public bool are_cheats_enabled_by_default;
        public static bool cheats_enabled;

        void Awake()
        {
            if (are_cheats_enabled_by_default)
            {
                EnableCheats();
            }
            else
            {
                DisableCheats();
            }
        }

        #if UNITY_EDITOR
        [EditorButton("Enable")]
        public void EnableCheats()
        {
            cheats_enabled = true;
            PrintToLogs();
        }

        [EditorButton("Disable")]
        public void DisableCheats()
        {
            cheats_enabled = false;
            PrintToLogs();
        }

        public void PrintToLogs()
        {
            Debug.Log($"cheats are {(cheats_enabled ? "enabled" : "disabled")} now");
        }
        #endif
    }  
}