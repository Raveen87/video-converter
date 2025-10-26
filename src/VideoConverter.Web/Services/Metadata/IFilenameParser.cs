using VideoConverter.Web.Domain.Models;

namespace VideoConverter.Web.Services.Metadata;

public interface IFilenameParser
{
    ParsedFilename ParseFilename(string filename);
}
