using System;
using System.Collections.Generic;
using UnityEngine;
using DesingerTables;

// ============ 播放驱动接口（每个逻辑动画一个 Driver，内部可用 Animator 或 DOTween） ============

/// <summary>
/// 单个逻辑动画的播放驱动。
/// value 既可以代表 Animator 动画，也可以代表 DOTween 动画。
/// </summary>
public interface IAnimDriver2
{
    /// <summary> 逻辑名（如 "Stand" / "Fire"）。 </summary>
    string Key { get; }

    /// <summary> 优先级。 </summary>
    int Priority { get; }

    /// <summary> 持续时间（秒），用于时间轴计算。 </summary>
    float Duration { get; }

    /// <summary>
    /// 在给定对象上播放该动画。
    /// 对 AnimatorDriver2 来说，使用 animator；对 DotweenDriver2 来说，使用 target（Transform）。
    /// </summary>
    void Play(Animator animator, Transform target, float fromNormalizedTime = 0f);
}

/// <summary>
/// 使用 Animator 的动画驱动。
/// </summary>
public class AnimatorDriver2 : IAnimDriver2
{
    public string Key { get; private set; }
    public int Priority { get; private set; }
    public float Duration { get; private set; }

    private readonly string _stateName;

    public AnimatorDriver2(string key, string stateName, float duration, int priority)
    {
        Key = key;
        _stateName = stateName;
        Duration = duration;
        Priority = priority;
    }

    public void Play(Animator animator, Transform target, float fromNormalizedTime = 0f)
    {
        if (!animator) return;
        animator.Play(_stateName, 0, Mathf.Clamp01(fromNormalizedTime));
    }
}

/// <summary>
/// 使用 DOTween（或其它基于 Transform 的系统）的动画驱动。
/// 这里通过 DesingerScripts.Animation.data 查表执行实际 Tween。
/// </summary>
public class DotweenDriver2 : IAnimDriver2
{
    public string Key { get; private set; }
    public int Priority { get; private set; }
    public float Duration { get; private set; }

    private readonly string _animName;

    public DotweenDriver2(string key, string animName, float duration, int priority)
    {
        Key = key;
        _animName = animName;
        Duration = duration;
        Priority = priority;
    }

    public void Play(Animator animator, Transform target, float fromNormalizedTime = 0f)
    {
        if (!target) return;
        // 具体 Tween 播放逻辑交给 DesingerScripts.Animation.data[_animName]
        DesingerScripts.Animation.data[_animName]?.Invoke(target, Duration, fromNormalizedTime);
    }
}

// ============ 当前/待播 条目 ============

/// <summary>
/// 队列中的一个动画条目（被打断或待播），记录驱动和开始时间。
/// </summary>
internal struct AnimEntry
{
    public IAnimDriver2 Driver;
    public float StartTime;   // 开始时间（逻辑时间）
}

// ============ UnitAnim2 控制器 ============

/// <summary>
/// 增强版动画管理控制器：
/// 1. 使用 DesingerTables.UnitAnimInfo2.data 作为配置表（key -> IAnimDriver2）。
/// 2. 负责优先级、打断、待播、进度同步等逻辑。
/// </summary>
public class UnitAnim2 : MonoBehaviour
{
    /// <summary> 播放倍速，作用于时间轴。 </summary>
    public float timeScale = 1f;

    private Animator _animator;
    private Transform _target;

    // 当前正在播的动画
    private IAnimDriver2 _currentDriver;
    private float _currentStartTime;
    private float _currentDuration;

    // 等待队列：被打断的与因优先级不足待播的动画
    private readonly List<AnimEntry> _queue = new List<AnimEntry>();

    // 逻辑时间（受 timeScale 影响）
    private float _logicTime;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _target = transform;
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime * timeScale;
        if (dt <= 0f) return;

        _logicTime += dt;

        // 1) 当前动画是否结束
        if (_currentDriver != null && _currentDuration > 0f)
        {
            float elapsed = _logicTime - _currentStartTime;
            if (elapsed >= _currentDuration)
            {
                _currentDriver = null;
                _currentStartTime = 0f;
                _currentDuration = 0f;
                TryStartNext();
                return;
            }
        }

        // 2) 清理队列中过期的条目
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            var e = _queue[i];
            float duration = e.Driver != null ? e.Driver.Duration : 0f;
            float elapsed = _logicTime - e.StartTime;
            if (duration > 0f && elapsed >= duration)
                _queue.RemoveAt(i);
        }

        // 3) 若当前为空，尝试启动下一个
        if (_currentDriver == null)
            TryStartNext();
    }

    /// <summary>
    /// 从队列中选优先级最高且未过期的一项播放。
    /// </summary>
    private void TryStartNext()
    {
        // _animator / _target 在 Start 中获取，这里只判空
        if (!_animator || !_target) return;

        AnimEntry? best = null;
        int bestPriority = int.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < _queue.Count; i++)
        {
            var e = _queue[i];
            if (e.Driver == null) continue;
            float duration = e.Driver.Duration;
            float elapsed = _logicTime - e.StartTime;
            if (duration > 0f && elapsed >= duration) continue;

            if (e.Driver.Priority > bestPriority)
            {
                bestPriority = e.Driver.Priority;
                best = e;
                bestIndex = i;
            }
        }

        if (!best.HasValue) return;

        var be = best.Value;
        _queue.RemoveAt(bestIndex);

        var driver = be.Driver;
        float durationD = Mathf.Max(driver.Duration, 0f);
        float elapsedD = durationD > 0f ? Mathf.Clamp(_logicTime - be.StartTime, 0f, durationD) : 0f;
        float fromNorm = durationD > 0f ? Mathf.Clamp01(elapsedD / durationD) : 0f;

        driver.Play(_animator, _target, fromNorm);
        _currentDriver = driver;
        _currentDuration = durationD;
        _currentStartTime = _logicTime - elapsedD;
    }

    /// <summary>
    /// 请求播放一个逻辑动画。
    /// </summary>
    public void Play(string key)
    {
        if (UnitAnimInfo2.data == null) return;
        if (!UnitAnimInfo2.data.ContainsKey(key)) return;

        // _animator / _target 在 Start 中获取，这里只判空
        if (!_animator || !_target) return;

        var driver = UnitAnimInfo2.data[key];
        if (driver == null) return;

        int currentPriority = _currentDriver != null ? _currentDriver.Priority : 0;

        // 同一逻辑动画再次请求：进入队列，当前继续播放
        if (_currentDriver != null && _currentDriver.Key == key)
        {
            _queue.Add(new AnimEntry
            {
                Driver = driver,
                StartTime = _logicTime
            });
            return;
        }

        // 高优先级：打断当前，立即播放
        if (driver.Priority >= currentPriority)
        {
            PushCurrentToQueue();
            driver.Play(_animator, _target, 0f);
            _currentDriver = driver;
            _currentDuration = Mathf.Max(driver.Duration, 0f);
            _currentStartTime = _logicTime;
            return;
        }

        // 低优先级：加入队列
        _queue.Add(new AnimEntry
        {
            Driver = driver,
            StartTime = _logicTime
        });
    }

    private void PushCurrentToQueue()
    {
        if (_currentDriver == null || _currentDuration <= 0f) return;
        _queue.Add(new AnimEntry
        {
            Driver = _currentDriver,
            StartTime = _currentStartTime
        });
    }
}
