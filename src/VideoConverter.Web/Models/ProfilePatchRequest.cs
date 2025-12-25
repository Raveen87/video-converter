using System.Collections.ObjectModel;

using VideoConverter.Common.Models;

namespace VideoConverter.Web.Models;

public sealed class ProfilePatchRequest(ProfilePatchDto patch, IEnumerable<string> changedFields)
{
    public ProfilePatchDto Patch { get; } = patch ?? throw new ArgumentNullException(nameof(patch));

    public IReadOnlyCollection<string> ChangedFields { get; } = ToReadOnlyCollection(changedFields);

    private static IReadOnlyCollection<string> ToReadOnlyCollection(IEnumerable<string> fields)
    {
        return new ReadOnlyCollection<string>(fields?.ToList() ?? []);
    }
}
