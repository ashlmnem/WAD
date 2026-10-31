using UnityEngine;
using UnityEngine.SceneManagement;

namespace WAD.UI
{
    /// <summary>
    /// Steuerung fuer das Hauptmenue. Auf ein leeres GameObject in der
    /// MainMenu-Szene ziehen, Buttons per OnClick() im Inspector verknuepfen.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Szenen-Namen (exakt wie in Build Settings)")]
        public string firstLevelSceneName = "SampleScene";
        public string shootingRangeSceneName = "ShootingRange";

        [Header("Loadout-Menue-Panel")]
        public GameObject loadoutMenuPanel;

        private void Start()
        {
            // Cursor-Sperre aus Level/Shooting Range (FirstPersonCameraLook) rueckgaengig
            // machen - der Sperrzustand ueberlebt sonst den Szenenwechsel.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [Header("Continue-Verfuegbarkeit")]
        private const string SaveExistsKey = "WAD_SaveExists";

        public bool HasSaveGame => PlayerPrefs.GetInt(SaveExistsKey, 0) == 1;

        public void OnNewGameClicked()
        {
            // Neuer Run: den persistenten Run-State explizit zuruecksetzen.
            // RunStateManager ueberlebt Szenenwechsel (DontDestroyOnLoad), ein
            // neuer Run wuerde sonst Flags, Treibstoff und Kill-Zaehler des
            // vorherigen Runs erben.
            WAD.Core.RunStateManager.Instance?.BeginNewRun();

            PlayerPrefs.SetInt(SaveExistsKey, 1);
            PlayerPrefs.Save();
            LoadFirstLevel();
        }

        public void OnContinueClicked()
        {
            if (!HasSaveGame) return;

            // TODO: sobald ein echtes Save-System existiert (Stash-Inhalt,
            // letzter erreichter Level-Index), hier den gespeicherten Zustand
            // laden statt immer bei Level 1 zu starten.
            LoadFirstLevel();
        }

        public void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnShootingRangeClicked()
        {
            if (!TryLoadScene(shootingRangeSceneName)) return;
        }

        /// <summary>
        /// Laedt die erste Level-Szene. Vorher stand hier fest "Level1_TheStorm" -
        /// diese Szene existiert im Projekt nicht und ist nicht in den Build
        /// Settings, wodurch "Neues Spiel" und "Fortsetzen" eine Exception geworfen
        /// haben. Zusaetzlich wird jetzt geprueft, ob die Szene ueberhaupt ladbar ist.
        /// </summary>
        private void LoadFirstLevel()
        {
            TryLoadScene(firstLevelSceneName);
        }

        private bool TryLoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[MainMenuController] Kein Szenenname konfiguriert.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[MainMenuController] Szene '{sceneName}' ist nicht ladbar - " +
                               "bitte in File > Build Settings aufnehmen und den Namen hier exakt eintragen.");
                return false;
            }

            SceneManager.LoadScene(sceneName);
            return true;
        }

        public void ToggleLoadoutMenu()
        {
            if (loadoutMenuPanel == null) return;
            loadoutMenuPanel.SetActive(!loadoutMenuPanel.activeSelf);
        }
    }
}