namespace VideoConverter.Web.Models;

/// <summary>
/// Contains testable logic for the EditConversionDialog component.
/// </summary>
public static class EditConversionDialogLogic
{
    /// <summary>
    /// Determines whether a form submission should be triggered based on the key pressed and current state.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="isSubmitting">Whether a submission is currently in progress.</param>
    /// <returns>True if a form submission should be triggered; otherwise, false.</returns>
    public static bool ShouldTriggerSubmit(string? key, bool isSubmitting)
    {
        return string.Equals(key, "Enter", StringComparison.Ordinal) && !isSubmitting;
    }

    /// <summary>
    /// Determines whether a form submission should be allowed based on form validity and current state.
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    /// <param name="isSubmitting">Whether a submission is currently in progress.</param>
    /// <param name="isFormValid">Whether the form passes validation.</param>
    /// <returns>True if a form submission should proceed; otherwise, false.</returns>
    public static bool ShouldAllowSubmission(string? key, bool isSubmitting, bool isFormValid)
    {
        return ShouldTriggerSubmit(key, isSubmitting) && isFormValid;
    }
}
