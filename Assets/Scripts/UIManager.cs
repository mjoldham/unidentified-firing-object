using UnityEngine;
using UnityEngine.UI;

namespace UFO
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Panels")]
        public GameObject MainMenu;
        public GameObject Credits;
        public GameObject Options;
        public GameObject HUD;

        [Header("Extends Slider")]
        public Slider ExtendsSlider;
        public Text ExtendsSliderText;

        [Header("Player")]
        public PlayerController Player;

        [Header("HUD")]
        public Text Extends;
        public Text Bombs;

        private void Awake()
        {
            // slider listeners
            ExtendsSlider.value = Player.ExtendCount;
            ExtendsSlider.onValueChanged.AddListener(value =>
            {
                Player.ExtendCount = (int) value;
                ExtendsSliderText.text = value.ToString();
            });

            // make sure theres only 1 instance
            MainMenu.SetActive(true);
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            Extends.text = "Extend Count: " + Player.ExtendCount.ToString();
            Bombs.text = "Bomb Count: " + Player.BombCount.ToString();
        }

        public void StartGame()
        {
            if (MainMenu != null)
                MainMenu.SetActive(false);

            ToggleHUD();
            GameManager.Instance.StartGame();
        }

        public void ToggleHUD()
        {
            HUD.SetActive(!HUD.activeInHierarchy);
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
