using System;
using UnityEngine;
using UnityEngine.LowLevel;

public sealed class CustomUpdateManager : IDisposable
{
    private sealed class FramePhase { }
    private sealed class PhysicsPhase { }
    private sealed class CameraPhase { }

    private readonly Action<float, float> frameTick;
    private readonly Action<float> physicsTick;
    private readonly Action renderTick;
    private bool disposed;

    public CustomUpdateManager(Action<float, float> onFrame, Action<float> onPhysics, Action onRender = null)
    {
        frameTick = onFrame;
        physicsTick = onPhysics;
        renderTick = onRender;
        PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
        bool frameAdded = onFrame == null || InsertBefore(ref loop,
            typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate),
            new PlayerLoopSystem { type = typeof(FramePhase), updateDelegate = TickFrame });
        bool physicsAdded = onPhysics == null || InsertBefore(ref loop,
            typeof(UnityEngine.PlayerLoop.FixedUpdate.ScriptRunBehaviourFixedUpdate),
            new PlayerLoopSystem { type = typeof(PhysicsPhase), updateDelegate = TickPhysics });
        bool renderAdded = onRender == null || InsertBefore(ref loop,
            typeof(UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate),
            new PlayerLoopSystem { type = typeof(CameraPhase), updateDelegate = TickRender });
        if (!frameAdded || !physicsAdded || !renderAdded)
            throw new InvalidOperationException("The Unity player loop is missing the required update phases.");
        PlayerLoop.SetPlayerLoop(loop);
    }

    private void TickFrame()
    {
        if (!disposed) frameTick?.Invoke(Time.deltaTime, Time.unscaledDeltaTime);
    }

    private void TickPhysics()
    {
        if (!disposed) physicsTick?.Invoke(Time.fixedDeltaTime);
    }

    private void TickRender()
    {
        if (!disposed) renderTick?.Invoke();
    }

    private static bool InsertBefore(ref PlayerLoopSystem parent, Type target, PlayerLoopSystem item)
    {
        PlayerLoopSystem[] systems = parent.subSystemList;
        if (systems == null) return false;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i].type == target)
            {
                var expanded = new PlayerLoopSystem[systems.Length + 1];
                Array.Copy(systems, 0, expanded, 0, i);
                expanded[i] = item;
                Array.Copy(systems, i, expanded, i + 1, systems.Length - i);
                parent.subSystemList = expanded;
                return true;
            }
            if (InsertBefore(ref systems[i], target, item)) return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
        Remove(ref loop, TickFrame);
        Remove(ref loop, TickPhysics);
        Remove(ref loop, TickRender);
        PlayerLoop.SetPlayerLoop(loop);
    }

    private static void Remove(ref PlayerLoopSystem parent, PlayerLoopSystem.UpdateFunction callback)
    {
        PlayerLoopSystem[] systems = parent.subSystemList;
        if (systems == null) return;
        int count = 0;
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i].updateDelegate == callback) continue;
            PlayerLoopSystem child = systems[i];
            Remove(ref child, callback);
            systems[count++] = child;
        }
        if (count != systems.Length) Array.Resize(ref systems, count);
        parent.subSystemList = systems;
    }
}
