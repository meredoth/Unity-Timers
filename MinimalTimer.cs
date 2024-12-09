using UnityEngine;

namespace UnityTimer
{
public readonly struct MinimalTimer
{
   public static MinimalTimer Start(float duration) => new(duration);
   public bool IsCompleted => Time.time >= _triggerTime && _triggerTime != 0;
   
   private readonly float _triggerTime;
   private MinimalTimer(float duration) => _triggerTime = Time.time + duration;
}
}