using VideoConverter.Web.Features.Metadata.Models;

namespace VideoConverter.Web.Features.Metadata.Services;

public interface IFilenameParser
{
    ParsedFilename ParseFilename(string filename);
}
