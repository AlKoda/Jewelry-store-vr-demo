using System;
using System.Collections.Generic;
using UnityEngine;

public enum FeedbackTone { Info, Success, Warning }

// One short message feed for everything the visitor does ("Marker 3 placed",
// "Photo 4 saved", "Tape connected"). The desktop HUD and the VR toast panel both
// read it, so the monitor and the headset always say the same thing. Static and
// tiny: posting costs a list insert, readers draw the newest few.
public static class DemoFeedback
{
    public struct Message
    {
        public string Text;
        public FeedbackTone Tone;
        public float Time;
        public int Serial;
    }

    public const int Capacity = 4;
    public const float Lifetime = 3.5f;

    private static readonly List<Message> recent = new List<Message>(Capacity + 1);
    private static int serial;

    // Newest last; at most Capacity entries.
    public static IReadOnlyList<Message> Recent => recent;
    public static event Action<Message> Posted;

    public static void Post(string text, FeedbackTone tone = FeedbackTone.Info)
    {
        if (string.IsNullOrEmpty(text)) return;
        // The same text twice in a row refreshes the existing toast instead of stacking.
        if (recent.Count > 0 && recent[recent.Count - 1].Text == text) recent.RemoveAt(recent.Count - 1);
        Message message = new Message { Text = text, Tone = tone, Time = Now, Serial = ++serial };
        recent.Add(message);
        while (recent.Count > Capacity) recent.RemoveAt(0);
        Posted?.Invoke(message);
    }

    // Drops messages older than Lifetime; readers call it before drawing.
    public static void Prune(float now)
    {
        for (int i = recent.Count - 1; i >= 0; i--)
            if (now - recent[i].Time > Lifetime) recent.RemoveAt(i);
    }

    // 0..1 visibility of a message at `now`: quick fade in, hold, fade out at the end.
    public static float Opacity(Message message, float now)
    {
        float age = now - message.Time;
        if (age < 0 || age > Lifetime) return 0;
        return Mathf.Min(Mathf.Clamp01(age / 0.15f), Mathf.Clamp01((Lifetime - age) / 0.6f));
    }

    public static void Clear() { recent.Clear(); }

    public static float Now => Time.unscaledTime;

    public static Color ToneColor(FeedbackTone tone)
    {
        switch (tone)
        {
            case FeedbackTone.Success: return DemoUi.Success;
            case FeedbackTone.Warning: return DemoUi.Warning;
            default: return DemoUi.Accent;
        }
    }
}
