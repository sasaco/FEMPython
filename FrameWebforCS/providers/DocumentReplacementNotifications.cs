using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FrameWebforCS.providers;

// File replacement installs all parsed sections before bound controls and viewport
// subscribers can observe them. Ordinary edits still notify synchronously.
internal sealed class DocumentReplacementNotifications : IDisposable
{
    [ThreadStatic] private static DocumentReplacementNotifications? _current;

    private readonly List<Action> _pending = new();
    private bool _completed;

    private DocumentReplacementNotifications() { }

    internal static DocumentReplacementNotifications Begin()
    {
        if (_current != null)
            throw new InvalidOperationException("A document replacement is already in progress.");
        return _current = new DocumentReplacementNotifications();
    }

    internal static void Defer(Action notification)
    {
        if (_current is { } replacement)
            replacement._pending.Add(notification);
        else
            notification();
    }

    internal static void Publish(EventHandler? handlers, object sender, EventArgs args)
    {
        if (_current == null) { handlers?.Invoke(sender, args); return; }
        if (handlers == null) return;
        foreach (EventHandler handler in handlers.GetInvocationList())
            Defer(() => handler(sender, args));
    }

    internal static void Publish(PropertyChangedEventHandler? handlers, object sender, PropertyChangedEventArgs args)
    {
        if (_current == null) { handlers?.Invoke(sender, args); return; }
        if (handlers == null) return;
        foreach (PropertyChangedEventHandler handler in handlers.GetInvocationList())
            Defer(() => handler(sender, args));
    }

    internal static void Publish(Action? handlers)
    {
        if (_current == null) { handlers?.Invoke(); return; }
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
            Defer(handler);
    }

    internal static void Publish<T>(Action<T>? handlers, T value)
    {
        if (_current == null) { handlers?.Invoke(value); return; }
        if (handlers == null) return;
        foreach (Action<T> handler in handlers.GetInvocationList())
            Defer(() => handler(value));
    }

    internal void PublishFirst<T>(Action<T>? handlers, T value)
    {
        if (handlers == null) return;
        var callbacks = new List<Action>();
        foreach (Action<T> handler in handlers.GetInvocationList())
            callbacks.Add(() => handler(value));
        _pending.InsertRange(0, callbacks);
    }

    // A failed observer cannot prevent the other observers, especially the viewport,
    // from seeing the committed revision. Report all failures to the caller afterward.
    internal void Complete()
    {
        if (!ReferenceEquals(_current, this) || _completed)
            throw new InvalidOperationException("Invalid document replacement completion.");
        _completed = true;
        var failures = new List<Exception>();
        for (int index = 0; index < _pending.Count; index++)
        {
            try { _pending[index](); }
            catch (Exception exception) { failures.Add(exception); }
        }
        _pending.Clear();
        _current = null;
        if (failures.Count > 0)
            throw new AggregateException("The document was replaced, but one or more observers failed.", failures);
    }

    public void Dispose()
    {
        if (ReferenceEquals(_current, this))
            _current = null;
        _pending.Clear();
    }
}
