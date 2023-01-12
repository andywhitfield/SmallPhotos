using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SmallPhotos.Model;

namespace SmallPhotos;

public static class ModelExtensions
{
    public static string CombinePath(string basePath, string path1, string path2) => Path.Combine(basePath, path1.RemoveRoot(), path2.RemoveRoot());
    public static string PhotoPath(this AlbumSource album, Photo photo) => PhotoPath(album, photo.RelativePath, photo.Filename ?? throw new ArgumentException($"Photo {photo.PhotoId} filename is null"));
    public static string PhotoPath(this AlbumSource album, string? relativePath, string filename) => CombinePath(album.Folder ?? throw new ArgumentException($"Album {album.AlbumSourceId} has a null folder"), relativePath ?? "", filename);

    public static string GetRelativePath(this string? root, FileInfo file) =>
        (string.IsNullOrEmpty(root) ? (file.DirectoryName ?? "") : Path.GetRelativePath(root, file.DirectoryName ?? "")).GetRelativePath();
    public static string GetRelativePath(this string? fullPath, string? basePath, string filename)
    {
        var relative = (fullPath != null && basePath != null && fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase)) ? fullPath.Substring(basePath.Length) : (fullPath ?? "");
        if (relative.EndsWith(filename, StringComparison.OrdinalIgnoreCase))
            relative = relative.Substring(0, relative.Length - filename.Length - 1);
        return relative;
    }

    public static string GetDropboxPhotoPath(string? basePath, string? relativePath, string? filename)
    {
        StringBuilder dropboxFilename = new(basePath ?? "");
        AppendDirectorySeparator(dropboxFilename);

        if (!string.IsNullOrEmpty(relativePath))
        {
            if (relativePath.Length > 0 && relativePath[0] == '/')
                dropboxFilename.Append(relativePath.Substring(1));
            else
                dropboxFilename.Append(relativePath);
        }

        AppendDirectorySeparator(dropboxFilename)
            .Append(filename ?? "");
        
        return dropboxFilename.ToString();

        static StringBuilder AppendDirectorySeparator(StringBuilder path)
        {
            if (path.Length == 0 || path[path.Length - 1] != '/')
                path.Append('/');
            return path;
        }
    }

    private static readonly HashSet<char> _pathSeparators = new() { '\\', '/' };
    private static string RemoveRoot(this string path) => string.IsNullOrEmpty(path) || path.Length == 0 || !_pathSeparators.Contains(path[0])
        ? path : path.Substring(1);

    private static string GetRelativePath(this string? relativePath) =>
        string.IsNullOrEmpty(relativePath) ? "" : relativePath == "." ? "" : relativePath;
}
