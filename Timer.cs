using System;
using System.Threading;
using UnityEngine;

namespace UnityTimer
{
public class Timer
{
   public float Duration { get; private set; }
   public int Repeats { get; private set; }
   public bool IsActive { get; private set; }
   public float ElapsedTime => TimeRunning();
   public float TimeRemaining => _triggerTime + _pausedDuration - Time.time;
   public bool IsCompleted { get; private set; }  // True if the timer has completed at least once.
   public int CompletionsCount { get; private set; }
   public event Action CompletedEvent;

   private float _triggerTime;
   private float _pausedTime;
   private float _pausedDuration;
   private float _startTime;
   private bool _hasStarted;
   private bool _loop;
   private CancellationTokenSource cts;
   
   public void Start(float duration) => Start(duration, 1, false);
   public void Start(float duration, bool loop) => Start(duration, 0, true);
   public void Start(float duration, int repeats) => Start(duration, repeats, false);

   // Resets everything EXCEPT the listeners
   public void Stop()
   {
      Duration = default;
      Repeats = default;
      IsActive = default;
      IsCompleted = default;
      CompletionsCount = default;
      _triggerTime = default;
      _pausedTime = default;
      _pausedDuration = default;
      _startTime = default;
      _hasStarted = default;
      _loop = default;
      cts.Cancel();
   }

   public void Reset(float duration) => Reset(duration, 1, false);

   public void Reset(float duration, bool loop) => Reset(duration, 0, true);

   public void Reset(float duration, int repeats) => Reset(duration, repeats, false);
   
   public void Pause()
   {
      if (!IsActive) return;
      
      IsActive = false;
      _pausedTime = Time.time;
   }

   public void UnPause()
   {
      if (IsActive || !_hasStarted) return;
      
      IsActive = true;
      _pausedDuration += Time.time - _pausedTime;
   }
   
   public async void Pause(float duration)
   {
      Pause();
      try
      {
         await Awaitable.WaitForSecondsAsync(duration, cts.Token);
      }
      catch when (cts.IsCancellationRequested)
      { }
      catch (Exception ex)
      {
         Debug.LogError(ex.Message);
      }
      UnPause();
   }

   public void AddTime(float duration)
   {
      if (!_hasStarted) return;
      
      _triggerTime += duration;
   }
   
   private void Start(float duration, int repeats, bool loop)
   {
      if (_hasStarted)
      {
         Debug.LogWarning("This timer has already started! Stop it before trying to Start it again.");
         return;
      }
      
      _loop = loop;
      Duration = duration;
      Repeats = repeats;
      Init();
   }
   
   private void Reset(float duration, int repeats, bool loop)
   {
      Stop();
      Start(duration, repeats, loop);
   }
   
   private void Init()
   {
      IsActive = true;
      _startTime = Time.time;
      _hasStarted = true;
      _triggerTime = _startTime + Duration;
      cts = new CancellationTokenSource();
      Tick();
   }
   
   private async void Tick()
   {
      do
      {
         await WaitForCompletion();

         if (cts.IsCancellationRequested)
            break;
         
         IsCompleted = true;
         CompletionsCount++;
         OnCompleted();

         if (CompletionsCount < Repeats || _loop)
            AddTime(Duration);
         
      } while (!IsDurationExpired());
      
      IsActive = false;
   }
   
   private async Awaitable WaitForCompletion()
   {
      while (HasToWait())
      {
         try
         {
            await Awaitable.NextFrameAsync(cts.Token);
         }
         catch when (cts.IsCancellationRequested)
         { }
         catch (Exception ex)
         {
            Debug.LogError(ex.Message);
         }
      }
   }

   private bool HasToWait() => (!IsActive || !IsDurationExpired()) && !cts.IsCancellationRequested;
   private bool IsDurationExpired() => Time.time >= _triggerTime + _pausedDuration;
   private float TimeRunning() => _hasStarted ? Time.time - _startTime - _pausedDuration : 0;
   private void OnCompleted() => CompletedEvent?.Invoke();
}
}
