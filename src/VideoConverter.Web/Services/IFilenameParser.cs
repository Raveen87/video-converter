using VideoConverter.Web.Services.Models;

namespace VideoConverter.Web.Services;

public interface IFilenameParser
{
    ParsedFilename ParseFilename(string filename);
}
