using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEngine.ParticleSystem;

namespace UFO
{
    public class PlayerAnimator : AnimatorBase
    {
        private PlayerController _player;

        public Animator BodyAnimator;
        public Transform ThrusterLeft, ThrusterRight;
        public MeshRenderer[] MuzzleFlashes;

        public MeshRenderer DeathFlash;
        public int DeathFrames = 25;
        private int _deathFrames;

        public MeshRenderer Shield;

        public int FlashingFrames = 10;
        private int _flashingFrames;

        public MeshRenderer PowerupFlashPrefab;
        public Color ShieldColour, PowerColour, BombColour, ExtendColour, BonusColour;
        public int PowerupFrames = 25;

        private MeshRenderer _shieldFlash, _powerFlash, _bombFlash, _extendFlash;
        private int _shieldFrames, _powerFrames, _bombFrames, _extendFrames;
        private MeshRenderer[] _bonusFlashes = new MeshRenderer[3];
        private int _bonusCount;
        private int[] _bonusFrames = new int[3];

        private SpriteRenderer[] _renderers;
        private TrailRenderer[] _trails;

        public enum AnimState
        {
            PlayerNeutral = 0,
            PlayerBankLeft,
            PlayerBankRight
        }

        private static int[] _stateIDs = new int[] { Animator.StringToHash(nameof(AnimState.PlayerNeutral)),
                                                     Animator.StringToHash(nameof(AnimState.PlayerBankLeft)),
                                                     Animator.StringToHash(nameof(AnimState.PlayerBankRight)) };

        private Vector3 _thrusterLeftPos, _thrusterRightPos;
        private const float _pixelSize = 1.0f / 64.0f;

        private float _firingScale;

        public IEnumerator Init(PlayerController player)
        {
            _player = player;
            _renderers = _player.GetComponentsInChildren<SpriteRenderer>();
            _trails = _player.GetComponentsInChildren<TrailRenderer>();

            _thrusterLeftPos = (Vector2)ThrusterLeft.localPosition;
            _thrusterRightPos = (Vector2)ThrusterRight.localPosition;

            for (int i = 0; i < MuzzleFlashes.Length; i++)
            {
                MuzzleFlashes[i].gameObject.SetActive(false);
            }

            _shieldFlash = Instantiate(PowerupFlashPrefab);
            _shieldFlash.material.SetColor(GameManager.ColourID, ShieldColour);
            _shieldFlash.gameObject.SetActive(false);

            _powerFlash = Instantiate(PowerupFlashPrefab);
            _powerFlash.material.SetColor(GameManager.ColourID, PowerColour);
            _powerFlash.gameObject.SetActive(false);

            _bombFlash = Instantiate(PowerupFlashPrefab);
            _bombFlash.material.SetColor(GameManager.ColourID, BombColour);
            _bombFlash.gameObject.SetActive(false);

            _extendFlash = Instantiate(PowerupFlashPrefab);
            _extendFlash.material.SetColor(GameManager.ColourID, ExtendColour);
            _extendFlash.gameObject.SetActive(false);

            for (int i = 0; i <_bonusFlashes.Length; i++)
            {
                _bonusFlashes[i] = Instantiate(PowerupFlashPrefab);
                _bonusFlashes[i].material.SetColor(GameManager.ColourID, BonusColour);
                _bonusFlashes[i].gameObject.SetActive(false);
            }

            DeathFlash.transform.SetParent(null);

            Shield.gameObject.SetActive(false);
            yield return null;
        }

        public static void SwitchToState(Animator animator, AnimState state)
        {
            animator.CrossFade(_stateIDs[(int)state], 0.0f);
        }

        private void OnMove(Vector2 move, bool isFiring)
        {
            if (move.x != 0.0f)
            {
                Quaternion rot = Quaternion.Euler(0.0f, 0.0f, 0.2f * Vector2.SignedAngle(Vector2.up, move));
                ThrusterLeft.rotation = ThrusterRight.rotation = rot;

                if (move.x < 0.0f)
                {
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerBankLeft]);
                    ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);
                    ThrusterRight.localPosition = _thrusterRightPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);

                    ThrusterLeft.rotation *= Quaternion.Euler(0.0f, 0.0f, -30.0f);
                }
                else
                {
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerBankRight]);
                    ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(2 * _pixelSize, 0.0f, 0.0f);
                    ThrusterRight.localPosition = _thrusterRightPos + new Vector3(2 * _pixelSize, 0.0f, 0.0f);

                    ThrusterRight.rotation *= Quaternion.Euler(0.0f, 0.0f, 30.0f);
                }

                return;
            }

            if (move.y < 0.0f)
            {
                ThrusterLeft.rotation = Quaternion.Euler(0.0f, 0.0f, 10.0f);
                ThrusterRight.rotation = Quaternion.Euler(0.0f, 0.0f, -10.0f);
            }
            else if (move.y > 0.0f)
            {
                ThrusterLeft.rotation = Quaternion.Euler(0.0f, 0.0f, -10.0f);
                ThrusterRight.rotation = Quaternion.Euler(0.0f, 0.0f, 10.0f);
            }
            else
            {
                ThrusterLeft.rotation = ThrusterRight.rotation = Quaternion.identity;
            }

            GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerNeutral]);
            ThrusterLeft.localPosition = _thrusterLeftPos;
            ThrusterRight.localPosition = _thrusterRightPos;
        }

        private void OnFireStart()
        {
            _firingScale = 3.0f;
            foreach (MeshRenderer flash in MuzzleFlashes)
            {
                flash.material.SetFloat(GameManager.NormTimeID, 0.0f);
                flash.gameObject.SetActive(true);
            }
        }

        private void OnFireEnd()
        {
            _firingScale = 0.0f;
            foreach (MeshRenderer flash in MuzzleFlashes)
            {
                flash.gameObject.SetActive(false);
            }
        }

        private void StartFlash(Vector2 position, MeshRenderer flash, int totalFrames, ref int frames)
        {
            frames = totalFrames;
            flash.material.SetFloat(GameManager.NormTimeID, totalFrames < 0 ? 1.0f : 0.0f);
            flash.transform.position = position;
            flash.gameObject.SetActive(true);
        }

        private void OnGetShield(Vector2 position)
        {
            StartFlash(position, _shieldFlash, PowerupFrames, ref _shieldFrames);
            Shield.gameObject.SetActive(true);
        }

        private void OnShieldDown()
        {
            StartFlash(_player.transform.position, _shieldFlash, -PowerupFrames, ref _shieldFrames);
            Shield.gameObject.SetActive(false);
        }

        private void OnGetPower(Vector2 position)
        {
            StartFlash(position, _powerFlash, PowerupFrames, ref _powerFrames);
        }

        private void OnGetBomb(Vector2 position)
        {
            StartFlash(position, _bombFlash, PowerupFrames, ref _bombFrames);
        }

        private void OnGetExtend(Vector2 position)
        {
            StartFlash(position, _extendFlash, PowerupFrames, ref _extendFrames);
        }

        private IEnumerator KeepBonusCount()
        {
            _bonusCount++;
            yield return null;
            _bonusCount--;
        }

        private void OnItemScore(Vector2 position)
        {
            StartFlash(position, _bonusFlashes[_bonusCount], PowerupFrames, ref _bonusFrames[_bonusCount]);
            StartCoroutine(KeepBonusCount());
        }

        private void OnDeath()
        {
            StartFlash(_player.transform.position, DeathFlash, DeathFrames, ref _deathFrames);
        }

        private void OnInvincibilityStart()
        {
            _flashingFrames = FlashingFrames;
            if (_renderers == null)
            {
                return;
            }

            foreach (SpriteRenderer renderer in _renderers)
            {
                renderer.enabled = false;
            }
        }

        private void OnInvincibilityEnd()
        {
            _flashingFrames = 0;
            foreach (SpriteRenderer renderer in _renderers)
            {
                renderer.enabled = true;
            }
        }

        private void FlashTick(MeshRenderer flash, int totalFrames, ref int frames)
        {
            if (frames == 0)
            {
                return;
            }

            if (frames > 0)
            {
                if (--frames > 0)
                {
                    flash.material.SetFloat(GameManager.NormTimeID, (float)(totalFrames - frames) / totalFrames);
                }
                else
                {
                    flash.gameObject.SetActive(false);
                }
            }
            else
            {
                if (++frames < 0)
                {
                    flash.material.SetFloat(GameManager.NormTimeID, (float)(-frames) / totalFrames);
                }
                else
                {
                    flash.gameObject.SetActive(false);
                }
            }
        }

        private void Tick()
        {
            float fireFreq = 10.0f;
            Vector3 offset = _firingScale * _pixelSize * Mathf.Sin(2.0f * Mathf.PI * fireFreq * Time.time) * Vector3.up;
            ThrusterLeft.localPosition = _thrusterLeftPos + offset;
            ThrusterRight.localPosition = _thrusterRightPos + offset;

            if (_firingScale > 0.0f)
            {
                for (int i = 0; i < MuzzleFlashes.Length; i++)
                {
                    MuzzleFlashes[i].material.SetFloat(GameManager.NormTimeID, 3.0f * fireFreq * Time.time);
                }
            }

            if (_flashingFrames != 0)
            {
                bool flip = _flashingFrames > 0;
                int frames = Mathf.Abs(_flashingFrames);
                if (--frames == 0)
                {
                    _flashingFrames = flip ? -FlashingFrames : FlashingFrames;
                    foreach (SpriteRenderer renderer in _renderers)
                    {
                        renderer.enabled = flip;
                    }
                }
                else
                {
                    _flashingFrames = flip ? frames : -frames;
                }
            }

            FlashTick(_shieldFlash, PowerupFrames, ref _shieldFrames);
            FlashTick(_powerFlash, PowerupFrames, ref _powerFrames);
            FlashTick(_bombFlash, PowerupFrames, ref _bombFrames);
            FlashTick(_extendFlash, PowerupFrames, ref _extendFrames);

            for (int i = 0; i < _bonusFlashes.Length; i++)
            {
                FlashTick(_bonusFlashes[i], PowerupFrames, ref _bonusFrames[i]);
            }

            FlashTick(DeathFlash, DeathFrames, ref _deathFrames);
        }

        private void ResetTrails()
        {
            foreach (TrailRenderer trail in _trails)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }
        
        private void ClearTrails()
        {
            foreach (TrailRenderer trail in _trails)
            {
                trail.Clear();
                trail.emitting = false;
            }
        }

        private void OnEnable()
        {
            PlayerController.OnSpawn += ResetTrails;
            PlayerController.OnTick += Tick;
            PlayerController.OnMove += OnMove;
            PlayerController.OnFireStart += OnFireStart;
            PlayerController.OnFireEnd += OnFireEnd;
            PlayerController.OnDeath += OnDeath;

            PlayerController.OnGetShield += OnGetShield;
            PlayerController.OnShieldDown += OnShieldDown;
            PlayerController.OnGetPower += OnGetPower;
            PlayerController.OnGetBomb += OnGetBomb;
            PlayerController.OnGetExtend += OnGetExtend;
            PlayerController.OnItemScore += OnItemScore;

            PlayerController.OnInvincibilityStart += OnInvincibilityStart;
            PlayerController.OnInvincibilityEnd += OnInvincibilityEnd;

            GameManager.OnGameOver += ClearTrails;
        }

        private void OnDisable()
        {
            PlayerController.OnSpawn -= ResetTrails;
            PlayerController.OnTick -= Tick;
            PlayerController.OnMove -= OnMove;
            PlayerController.OnFireStart -= OnFireStart;
            PlayerController.OnFireEnd -= OnFireEnd;
            PlayerController.OnDeath -= OnDeath;

            PlayerController.OnGetShield -= OnGetShield;
            PlayerController.OnShieldDown -= OnShieldDown;
            PlayerController.OnGetPower -= OnGetPower;
            PlayerController.OnGetBomb -= OnGetBomb;
            PlayerController.OnGetExtend -= OnGetExtend;
            PlayerController.OnItemScore -= OnItemScore;

            PlayerController.OnInvincibilityStart -= OnInvincibilityStart;
            PlayerController.OnInvincibilityEnd -= OnInvincibilityEnd;

            GameManager.OnGameOver -= ClearTrails;
        }
    }
}
