// Fieldwork messages: what happened and what the tool does. Koda asked (October 24) for hints on
// what each tool works on and how, so these reach the shared feed again; the record stays the
// instructor's material, and no message tells the trainee which clue to go to next.
public static class ForensicFeedback {
 public static void Post(string message, FeedbackTone tone=FeedbackTone.Info) { DemoFeedback.Post(message, tone); }
}
