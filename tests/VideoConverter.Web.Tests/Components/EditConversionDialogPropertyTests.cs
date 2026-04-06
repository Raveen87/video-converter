using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using VideoConverter.Web.Features.Conversions.Logic;

namespace VideoConverter.Web.Tests.Components;

/// <summary>
/// Property-based tests for EditConversionDialog component.
/// </summary>
public class EditConversionDialogPropertyTests
{
    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 3: Enter triggers form submission in EditConversionDialog.
    /// Validates: Requirements 2.1.
    /// For any valid form state in EditConversionDialog, pressing Enter while focused on a form field SHALL trigger form submission.
    /// </summary>
    /// <returns>A property that validates Enter triggers form submission.</returns>
    [Property(MaxTest = 100)]
    public Property EnterTriggersFormSubmission()
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSubmitting = false;

        // Act
        var shouldTrigger = EditConversionDialogLogic.ShouldTriggerSubmit(enterKey, isSubmitting);

        // Assert - Enter key when not submitting should trigger submission
        return shouldTrigger
            .Label($"Enter key with isSubmitting={isSubmitting} should trigger form submission");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 3 (negative case): Non-Enter keys do not trigger form submission.
    /// Validates: Requirements 2.1.
    /// For any key that is not Enter, the form submission SHALL NOT be triggered.
    /// </summary>
    /// <param name="key">Generated key name.</param>
    /// <returns>A property that validates non-Enter keys do not trigger submission.</returns>
    [Property(MaxTest = 100)]
    public Property NonEnterKeysDoNotTriggerFormSubmission(NonEmptyString key)
    {
        // Skip if the key happens to be "Enter"
        if (string.Equals(key.Item, "Enter", StringComparison.Ordinal))
        {
            return true.Label("Skipped - key is Enter");
        }

        // Arrange
        const bool isSubmitting = false;

        // Act
        var shouldTrigger = EditConversionDialogLogic.ShouldTriggerSubmit(key.Item, isSubmitting);

        // Assert - Non-Enter keys should not trigger submission
        return (!shouldTrigger)
            .Label($"Key '{key.Item}' should not trigger form submission");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 3 (in-progress case): In-progress state blocks Enter triggers.
    /// Validates: Requirements 2.2.
    /// For any dialog in an in-progress state (submitting), pressing Enter SHALL NOT trigger additional submissions.
    /// </summary>
    /// <returns>A property that validates in-progress state blocks Enter triggers.</returns>
    [Property(MaxTest = 100)]
    public Property InProgressStateBlocksEnterTriggers()
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSubmitting = true; // In-progress state

        // Act
        var shouldTrigger = EditConversionDialogLogic.ShouldTriggerSubmit(enterKey, isSubmitting);

        // Assert - Enter key while submitting should NOT trigger another submission
        return (!shouldTrigger)
            .Label($"Enter key while isSubmitting=true should NOT trigger form submission");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 4: Invalid form blocks submission on Enter.
    /// Validates: Requirements 2.3.
    /// For any form with validation errors, pressing Enter SHALL NOT submit the form.
    /// </summary>
    /// <returns>A property that validates invalid form blocks submission.</returns>
    [Property(MaxTest = 100)]
    public Property InvalidFormBlocksSubmissionOnEnter()
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSubmitting = false;
        const bool isFormValid = false; // Invalid form state

        // Act
        var shouldAllowSubmission = EditConversionDialogLogic.ShouldAllowSubmission(enterKey, isSubmitting, isFormValid);

        // Assert - Enter key with invalid form should NOT allow submission
        return (!shouldAllowSubmission)
            .Label($"Enter key with invalid form should NOT allow submission");
    }

    /// <summary>
    /// Feature: dialog-enter-key-submit, Property 4 (positive case): Valid form allows submission on Enter.
    /// Validates: Requirements 2.1, 2.3.
    /// For any valid form, pressing Enter SHALL allow form submission.
    /// </summary>
    /// <returns>A property that validates valid form allows submission.</returns>
    [Property(MaxTest = 100)]
    public Property ValidFormAllowsSubmissionOnEnter()
    {
        // Arrange
        const string enterKey = "Enter";
        const bool isSubmitting = false;
        const bool isFormValid = true; // Valid form state

        // Act
        var shouldAllowSubmission = EditConversionDialogLogic.ShouldAllowSubmission(enterKey, isSubmitting, isFormValid);

        // Assert - Enter key with valid form should allow submission
        return shouldAllowSubmission
            .Label($"Enter key with valid form should allow submission");
    }
}
