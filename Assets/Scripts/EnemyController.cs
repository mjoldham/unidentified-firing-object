using System.Collections;
using System.Collections.Generic;
using UFO;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    protected GameManager _gm;
    protected Animator _animator;

    public Collider2D[] HitBoxes, HurtBoxes, ShieldBoxes;

    public enum MoveMode
    {
        Static,
        Relative
    }

    public MoveMode CurrentMode;

    public bool IsMirrored;

    // How the enemy should move along the x-axis in normalised time (0 to 1).
    public AnimationCurve XCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

    // How the enemy should move along the y-axis in normalised time (0 to 1).
    public AnimationCurve YCurve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

    public int Health = 1;

    private Queue<RoutePoint> _route;
    private int _beatsToGo;

    private ShotEmitter[] _emitters;

    Vector2 _start, _destination;
    double _destTime, _destDuration;

    private bool _isExiting;

    public void Init()
    {
        _gm = GameManager.Instance;
        _animator = GetComponent<Animator>();

        _emitters = GetComponentsInChildren<ShotEmitter>();
        foreach(ShotEmitter emitter in _emitters)
        {
            emitter.Init();
        }

        gameObject.SetActive(false);
    }

    private void StartMove(Vector2 position, int beats, bool isMirrored)
    {
        if (isMirrored)
        {
            position.x = -position.x;
        }

        _beatsToGo = beats;
        if (_beatsToGo == 0)
        {
            transform.position = _destination = position;
            return;
        }

        _start = transform.position;
        _destination = position;

        _destDuration = beats * _gm.BeatLength;
        _destTime = UnityEngine.AudioSettings.dspTime + _destDuration;
    }

    private void StartMove(RoutePoint rp)
    {
        StartMove(rp.Destination, rp.BeatsToComplete, IsMirrored);
    }

    public void Spawn(SpawnInfo spawnInfo)
    {
        _isExiting = false;
        _beatsToGo = 0;
        IsMirrored = spawnInfo.IsMirrored;

        transform.position = _destination = new Vector2(spawnInfo.Lane, GameManager.ScreenHalfHeight + 1.0f);
        gameObject.SetActive(true);

        if (spawnInfo.RoutePrefab == null || spawnInfo.RoutePrefab.childCount == 0)
        {
            ExitStage();
            return;
        }

        _route = new Queue<RoutePoint>(spawnInfo.RoutePrefab.childCount);
        foreach (Transform t  in spawnInfo.RoutePrefab)
        {
            _route.Enqueue(new RoutePoint(t)); // TODO: attach fire points!
        }

        StartMove(_route.Dequeue());
    }

    public bool TryDie(int count)
    {
        return (Health -= count) <= 0;
    }

    // Returns false when enemy should be despawned after exiting the stage.
    public bool Tick(float deltaTime)
    {
        // Updates position based on destination and curves.
        double time = UnityEngine.AudioSettings.dspTime;
        if (time < _destTime)
        {
            float t = (float)((_destTime - time) / _destDuration);
            float tx = XCurve.Evaluate(1.0f - t);
            float ty = YCurve.Evaluate(1.0f - t);

            float x = Mathf.Lerp(_start.x, _destination.x, tx);
            float y = Mathf.Lerp(_start.y, _destination.y, ty);
            transform.position = new Vector2(x, y);
        }
        else
        {
            transform.position = _destination;
            if (_isExiting)
            {
                gameObject.SetActive(false);
                return false;
            }
        }

        // While above the cutoff height, keeps ticking emitters.
        if (transform.position.y < GameManager.CutoffHeight)
        {
            return true;
        }

        if (ShotEmitter.Tick(_emitters))
        {
            return true;
        }

        foreach (ShotEmitter emitter in _emitters)
        {
            emitter.Restart();
        }

        return true;
    }

    private void ExitStage()
    {
        _isExiting = true;
        Vector2 dest = new Vector2(transform.position.x, -GameManager.ScreenHalfHeight - 1.0f);
        StartMove(dest, 4, false);
    }

    private void OnBeat()
    {
        if (_isExiting)
        {
            return;
        }

        if (--_beatsToGo > 0)
        {
            return;
        }

        // If finished moving, exit the stage.
        if (!_route.TryDequeue(out RoutePoint route))
        {
            ExitStage();
            return;
        }

        StartMove(route);
    }

    private void OnPause()
    {
        _animator.speed = 0.0f;
    }

    private void OnUnpause(double lostTime)
    {
        _animator.speed = 1.0f;
        _destTime += lostTime;
    }

    private void OnEnable()
    {
        GameManager.OnBeat += OnBeat;
        GameManager.OnPause += OnPause;
        GameManager.OnUnpause += OnUnpause;
    }

    private void OnDisable()
    {
        GameManager.OnBeat -= OnBeat;
        GameManager.OnPause -= OnPause;
        GameManager.OnUnpause -= OnUnpause;
    }
}
