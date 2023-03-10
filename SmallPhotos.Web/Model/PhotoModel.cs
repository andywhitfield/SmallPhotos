using System;
using System.Collections.Generic;
using System.Drawing;

namespace SmallPhotos.Web.Model;

public class PhotoModel
{
    private const string _dateFormat = "HH:mm' on 'dd MMMM yyyy";

    public PhotoModel(long photoId, string source, bool isDropboxSource, string filename, string filepath, Size size, DateTime dateTaken, DateTime fileCreationDate, bool isStarred, IEnumerable<string> tags)
    {
        PhotoId = photoId;
        Source = source;
        IsDropboxSource = isDropboxSource;
        Filename = filename;
        Filepath = filepath;
        Size = size;
        SizeInfo = $"{size.Width}w x {size.Height}h";
        DateTimeTaken = dateTaken;
        DateTaken = dateTaken.ToString(_dateFormat);
        FileCreationDate = fileCreationDate.ToString(_dateFormat);
        IsStarred = isStarred;
        Tags = tags;
    }

    public long PhotoId { get; }
    public string Source { get; }
    public bool IsDropboxSource { get; }
    public string Filename { get; }
    public string Filepath { get; }
    public Size Size { get; }
    public string SizeInfo { get; }
    public DateTime DateTimeTaken { get; }
    public string DateTaken { get; }
    public string FileCreationDate { get; }
    public bool IsStarred { get; }
    public IEnumerable<string> Tags { get; }
}