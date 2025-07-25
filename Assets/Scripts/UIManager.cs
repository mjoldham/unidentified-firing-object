using UnityEngine;

namespace UFO
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        public GameObject MainMenu;
        public GameObject Credits;
        public GameObject Options;

        private void Awake()
        {
            MainMenu.SetActive(true);
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void StartGame()
        {
            if (MainMenu != null)
                MainMenu.SetActive(false);

            GameManager.Instance.StartGame();
        }

        public void ShowPanel(GameObject panel)
        {
            MainMenu.SetActive(false);
            Credits.SetActive(false);
            Options.SetActive(false);

            panel.SetActive(true);
        }

        public void BackToMainMenu()
        {
            Credits.SetActive(false);
            Options.SetActive(false);
            MainMenu.SetActive(true);
        }
    }
}
