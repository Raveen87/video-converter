using static VideoConverter.Web.Components.TemplateHelpDialog;

namespace VideoConverter.Web.Features.Shared.Logic;

/// <summary>
/// Contains testable logic for the TemplateHelpDialog component.
/// </summary>
public static class TemplateHelpDialogLogic
{
    /// <summary>
    /// Determines the initial active panel index based on the template type.
    /// </summary>
    /// <param name="initialTab">The initial tab to display.</param>
    /// <returns>The zero-based index of the panel to display (0 for Filename, 1 for Path).</returns>
    public static int GetInitialActivePanelIndex(TemplateType initialTab)
    {
        return initialTab is TemplateType.Path ? 1 : 0;
    }
}
