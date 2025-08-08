using System.Collections;
using UnityEngine;

namespace UFO
{
    public class GameInitialiser : MonoBehaviour
    {
        public Transform SceneRootPrefab;
        private Transform _sceneRoot;

        private GameManager _gm;
        private AudioManager _audio;
        private PlayerAnimator _playerAnim;
        private PlayerController _player;
        private UIManager _ui;

        private Animator[] _animators;

        private bool _initialised;

        private IEnumerator Start()
        {
            _initialised = false;
            _sceneRoot = Instantiate(SceneRootPrefab);

            _gm = _sceneRoot.GetComponentInChildren<GameManager>();
            _audio = _sceneRoot.GetComponentInChildren<AudioManager>();
            _playerAnim = _sceneRoot.GetComponentInChildren<PlayerAnimator>();
            _player = _sceneRoot.GetComponentInChildren<PlayerController>();
            _ui = _sceneRoot.GetComponentInChildren<UIManager>();

            yield return StartCoroutine(_player.Init(_gm));
            yield return StartCoroutine(_audio.Init());
            yield return StartCoroutine(_gm.Init(_audio, _player));
            yield return StartCoroutine(_playerAnim.Init(_player));
            yield return StartCoroutine(_ui.Init());

            _initialised = true;
        }

        private void FixedUpdate()
        {
            if (_initialised)
            {
                _gm.Tick();
            }
        }

        private void Update()
        {
            if (_initialised)
            {
                _ui.Tick();
            }
        }

        private void LateUpdate()
        {
            if (_initialised)
            {
                _audio.Tick();
            }
        }

        private void OnPause()
        {
            foreach (Animator anim in _animators)
            {
                anim.enabled = false;
            }
        }

        private void OnUnpause(double lostTime)
        {
            foreach (Animator anim in _animators)
            {
                anim.enabled = true;
            }
        }

        private void OnEnable()
        {
            GameManager.OnPause += OnPause;
            GameManager.OnUnpause += OnUnpause;
        }

        private void OnDisable()
        {
            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
        }
    }
}
