using System.Collections.Generic;
using UnityEngine;

// Scene dressing the presenter shows or hides from the desktop panel (Scene tab, SCENE
// DRESSING): the alarm bell, the ambient interior wear, the case evidence. Content is the
// object switched; it must be a child or another object, never this one, so the set stays
// enabled and listed while its content is hidden. ShownAtStart is the arrival state: Start
// applies it and a session reset restores it. All holds the enabled sets for the panel;
// Revision changes whenever that list does, so the panel re-sorts only then. SetShown is
// silent (resets, scripts, validation); Toggle is the presenter switch and confirms on the
// desktop only, like the other presenter switches.
public sealed class PresenterSet : MonoBehaviour
{
    public string Label;
    // One line under the switch (who decides, what it stands for); empty for none.
    public string Note;
    public GameObject Content;
    public bool ShownAtStart;
    // Found in Awake when unassigned.
    public DemoSession Session;

    private static readonly List<PresenterSet> all = new List<PresenterSet>();
    public static IReadOnlyList<PresenterSet> All => all;
    public static int Revision { get; private set; }

    public bool Shown => Content != null && Content.activeSelf;

    private void Awake()
    {
        if (Session == null) Session = FindFirstObjectByType<DemoSession>();
        // The panel draws Label on every event; name would allocate each time.
        if (string.IsNullOrEmpty(Label)) Label = name;
    }

    private void OnEnable()
    {
        if (!all.Contains(this)) { all.Add(this); Revision++; }
        if (Session != null) Session.SessionReset.AddListener(Restore);
    }

    private void OnDisable()
    {
        if (all.Remove(this)) Revision++;
        if (Session != null) Session.SessionReset.RemoveListener(Restore);
    }

    private void Start() { Restore(); }

    public void SetShown(bool shown)
    {
        if (Content != null && Content.activeSelf != shown) Content.SetActive(shown);
    }

    public void Toggle()
    {
        if (Content == null) return;
        SetShown(!Shown);
        DemoFeedback.Post(Label + (Shown ? " shown" : " hidden"), FeedbackTone.Info, true);
    }

    private void Restore() { SetShown(ShownAtStart); }
}
