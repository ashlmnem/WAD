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
        public string firstLevelSceneName = "Level1_TheStorm";
        public string shootingRangeSceneName = "ShootingRange";

        [Header("Loadout-Menü-Panel")]
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
            // Neuer Run: jeglicher alter RunState wird beim Szenenwechsel
            // ohnehin frisch instanziiert (RunStateManager.Awake), da die
            // vorherige Menue-Szene keinen DontDestroyOnLoad-RunStateManager hatte.
            PlayerPrefs.SetInt(SaveExistsKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene(firstLevelSceneName);
        }

        public void OnContinueClicked()
        {
            if (!HasSaveGame) return;

            // TODO: sobald ein echtes Save-System existiert (Stash-Inhalt,
            // letzter erreichter Level-Index), hier den gespeicherten Zustand
            // laden statt immer bei Level 1 zu starten.
            SceneManager.LoadScene(firstLevelSceneName);
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
            SceneManager.LoadScene(shootingRangeSceneName);
        }

        public void ToggleLoadoutMenu()
        {
            if (loadoutMenuPanel == null) return;
            loadoutMenuPanel.SetActive(!loadoutMenuPanel.activeSelf);
        }
    }
}