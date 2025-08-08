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
        public GameObject HowToPlay, Options, Credits;
        public GameObject HUD;

        [Header("Options")]
        public Slider StartSlider;
        public Text StartPercent;
        public Toggle SecondLoopToggle;
        public Slider ExtendsSlider;
        public Toggle AutobombToggle;
        public Slider ScrollSlider, MusicSlider, EffectsSlider;

        [Header("HUD")]
        public Text Score;
        public Text Hiscore, Loscore;
        public Text Extends, Bombs;
        public GameObject StageStart1, StageStart2, Paused, GameOver, StageComplete1, StageComplete2;
        public GameObject BonusParent;
        public Text ExtendBonus, ShieldBonus, BombBonus, TotalBonus;

        private Coroutine _currCoroutine;

        public IEnumerator Init()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                yield break;
            }
            Instance = this;

            ShowPanel(MainMenu);

            StartSlider.value = 0.0f;
            StartSlider.onValueChanged.AddListener(value =>
            {
                StartPercent.text = Mathf.RoundToInt(100 * value).ToString();
                GameManager.StartAtBeat = Mathf.RoundToInt(value * GameManager.TrackBeats);
            });

            SecondLoopToggle.isOn = false;
            SecondLoopToggle.onValueChanged.AddListener(value =>
            {
                GameManager.StartAtSecondLoop = value;
            });

            ExtendsSlider.value = GameManager.StartingExtends = PlayerController.ExtendCount;
            ExtendsSlider.onValueChanged.AddListener(value =>
            {
                Extends.text = value.ToString();
                PlayerController.ExtendCount = (int)value;
                GameManager.StartingExtends = (int)value;
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

            yield return null;
        }

        public void Tick()
        {
            Score.text = GameManager.CurrentScore.ToString();
            Hiscore.text = GameManager.Hiscore.ToString();
            Loscore.text = GameManager.Loscore.ToString();
            Extends.text = PlayerController.ExtendCount.ToString();
            Bombs.text = PlayerController.BombCount.ToString();

            ExtendBonus.text = GameManager.ExtendBonus.ToString();
            ShieldBonus.text = GameManager.ShieldBonus.ToString();
            BombBonus.text = GameManager.BombBonus.ToString();
            TotalBonus.text = GameManager.TotalBonus.ToString();
        }

        private IEnumerator DisplayingTitle(GameObject title, int beatsBefore, int beatsOn)
        {
            yield return new WaitForSeconds(beatsBefore * (float)GameManager.BeatLength);
            title.SetActive(true);
            yield return new WaitForSeconds(beatsOn * (float)GameManager.BeatLength);
            title.SetActive(false);
        }

        public void StartGame()
        {
            MainMenu.SetActive(false);
            GameManager.OnGameStart?.Invoke();
        }

        private void OnStageStart(bool secondLoop)
        {
            if (_currCoroutine != null)
            {
                StopCoroutine(_currCoroutine);
            }

            _currCoroutine = StartCoroutine(DisplayingTitle(secondLoop ? StageStart2 : StageStart1, PlayerController.SpawnBeats, 8));
        }

        public void ShowPanel(GameObject panel)
        {
            MainMenu.SetActive(false);
            HowToPlay.SetActive(false);
            Options.SetActive(false);
            Credits.SetActive(false);

            panel.SetActive(true);
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
            ShowPanel(MainMenu);
        }

        private IEnumerator EndingStage()
        {
            StartCoroutine(DisplayingTitle(BonusParent, 0, GameManager.BeatsBeforeEnd));
            yield return StartCoroutine(DisplayingTitle(StageComplete2, 0, GameManager.BeatsBeforeEnd));
            GameManager.OnGameEnd?.Invoke();
        }

        private IEnumerator GoingToNextLoop()
        {
            int duration = GameManager.BeatsBeforeEnd + PlayerController.SpawnBeats;
            StartCoroutine(DisplayingTitle(BonusParent, 0, duration));
            yield return StartCoroutine(DisplayingTitle(StageComplete1, 0, duration));
            OnStageStart(true);
        }

        public void OnStageComplete(bool secondLoop)
        {
            if (_currCoroutine != null)
            {
                StopCoroutine(_currCoroutine);
            }

            if (secondLoop)
            {
                _currCoroutine = StartCoroutine(EndingStage());
            }
            else
            {
                _currCoroutine = StartCoroutine(GoingToNextLoop());
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameOver += OnGameOver;
            GameManager.OnGameEnd += OnGameEnd;
            GameManager.OnPause += OnPause;
            GameManager.OnUnpause += OnUnpause;
            GameManager.OnStageStart += OnStageStart;
            GameManager.OnStageComplete += OnStageComplete;
        }

        private void OnDisable()
        {
            GameManager.OnGameOver -= OnGameOver;
            GameManager.OnGameEnd -= OnGameEnd;
            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
            GameManager.OnStageStart -= OnStageStart;
            GameManager.OnStageComplete -= OnStageComplete;
        }
    }
}
