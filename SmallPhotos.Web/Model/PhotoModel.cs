using System.Drawing;

namespace SmallPhotos.Web.Model;

public class PhotoModel(long photoId, string source, bool isDropboxSource, string filename,
    string filepath, Size size, DateTime dateTaken, DateTime fileCreationDate,
    bool isStarred, IEnumerable<string> tags)
{
    private const string _dateFormat = "HH:mm' on 'dd MMMM yyyy";
    private const string _dateFormatShort = "dd MMM yyyy @ HH:mm";

    public long PhotoId { get; } = photoId;
    public string Source { get; } = source;
    public bool IsDropboxSource { get; } = isDropboxSource;
    public string Filename { get; } = filename;
    public string Filepath { get; } = filepath;
    public Size Size { get; } = size;
    public string SizeInfo { get; } = $"{size.Width}w x {size.Height}h";
    public DateTime DateTimeTaken { get; } = dateTaken;
    public string DateTaken { get; } = dateTaken.ToString(_dateFormat);
    public string DateTakenShort { get; } = dateTaken.ToString(_dateFormatShort);
    public string FileCreationDate { get; } = fileCreationDate.ToString(_dateFormat);
    public bool IsStarred { get; } = isStarred;
    public IEnumerable<string> Tags { get; } = tags;
}