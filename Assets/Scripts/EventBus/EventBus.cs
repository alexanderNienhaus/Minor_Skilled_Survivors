using System;

/// <summary>
/// This class is used for receiving and sending events from other parts of the code base
/// </summary>
/// <typeparam name="T">The event targeted either in subscribing or publishing</typeparam>
public class EventBus<T> where T : CustomEvent
{
    public static event Action<T> OnEvent;

    public static void Publish(T pEvent)
    {
        OnEvent?.Invoke(pEvent);
    }
}