using System.Collections;
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

        [Header("Options")]
        public Slider ExtendsSlider;
        public Slider ScrollSlider, MusicSlider, EffectsSlider;
        public Toggle AutobombToggle;

        [Header("HUD")]
        public Text Score;
        public Text Hiscore;
        public Text Extends;

        public Text Bombs;
        public GameObject Paused, GameOver, StageComplete;

        private PlayerController _player;

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


        private void Start()
        {
            _player = PlayerController.Instance;

            ExtendsSlider.value = _player.ExtendCount;
            ExtendsSlider.onValueChanged.AddListener(value =>
            {
                Extends.text = value.ToString();
                _player.ExtendCount = (int)value;
            });

            ScrollSlider.value = 1.0f;
            ScrollSlider.onValueChanged.AddListener(value =>
            {
                GameManager.OnChangeScroll?.Invoke(value);
            });

            MusicSlider.value = 1.0f;
            MusicSlider.onValueChanged.AddListener(value =>
            {
                AudioManager.OnChangeMusic?.Invoke(value);
            });

            EffectsSlider.value = 1.0f;
            EffectsSlider.onValueChanged.AddListener(value =>
            {
                AudioManager.OnChangeFX?.Invoke(value);
            });

            AutobombToggle.isOn = false;
            AutobombToggle.onValueChanged.AddListener(value =>
            {
                PlayerController.Autobomb = value;
            });
        }

        private void Update()
        {
            Score.text = GameManager.CurrentScore.ToString();
            Hiscore.text = GameManager.Hiscore.ToString();
            Extends.text = _player.ExtendCount.ToString();
            Bombs.text = _player.BombCount.ToString();
        }

        public void StartGame()
        {
            if (MainMenu != null)
                MainMenu.SetActive(false);

            //HUD.SetActive(true);
            GameManager.OnGameStart?.Invoke((int)ExtendsSlider.value);
        }

        public void ShowPanel(GameObject panel)
        {
            //HUD.SetActive(false);
            MainMenu.SetActive(false);
            Credits.SetActive(false);
            Options.SetActive(false);

            panel.SetActive(true);
        }

        public void BackToMainMenu()
        {
            //HUD.SetActive(false);
            Credits.SetActive(false);
            Options.SetActive(false);
            MainMenu.SetActive(true);
        }

        public void Quit()
        {
            Application.Quit();
        }

        private void OnPause()
        {
            Paused.SetActive(true);
        }

        private void OnUnpause(double time)
        {
            Paused.SetActive(false);
        }

        private void OnGameOver()
        {
            GameOver.SetActive(true);
        }

        private void OnGameEnd()
        {
            GameOver.SetActive(false);
            BackToMainMenu();
        }

        public void DisplayStage(int index)
        {
            // TODO: enable stage title from list.
        }

        public void OnStageComplete(int index)
        {
            // TODO: display bonuses.
        }

        private void OnEnable()
        {
            GameManager.OnGameOver += OnGameOver;
            GameManager.OnGameEnd += OnGameEnd;
            GameManager.OnPause += OnPause;
            GameManager.OnUnpause += OnUnpause;
        }

        private void OnDisable()
        {
            GameManager.OnGameOver -= OnGameOver;
            GameManager.OnGameEnd -= OnGameEnd;
            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
        }
    }
}
