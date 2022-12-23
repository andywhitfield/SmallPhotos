using System;
using System.IO;
using SmallPhotos.Model;

namespace SmallPhotos;

public static class ModelExtensions
{
    public static string PhotoPath(this AlbumSource album, Photo photo) => PhotoPath(album, photo.RelativePath, photo.Filename ?? throw new ArgumentException($"Photo {photo.PhotoId} filename is null"));
    public static string PhotoPath(this AlbumSource album, string? relativePath, string filename) => Path.Combine(album.Folder ?? throw new ArgumentException($"Album {album.AlbumSourceId} has a null folder"), relativePath ?? "", filename);
    
    public static string GetRelativePath(this string? root, FileInfo file) =>
        (string.IsNullOrEmpty(root) ? (file.DirectoryName ?? "") : Path.GetRelativePath(root, file.DirectoryName ?? "")).GetRelativePath();
    private static string GetRelativePath(this string? relativePath) =>
        string.IsNullOrEmpty(relativePath) ? "" : relativePath == "." ? "" : relativePath;

}