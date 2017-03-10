using System;
using System.IO;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Modules.MediaConversionTool.Configuration;
using Sitecore.Resources.Media;
using Sitecore.StringExtensions;

namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
    public class ConvertMedia : ConvertMediaItemProcessor
    {
        /// <summary>
        ///     Main entry method for ConvertMedia pipeline processor.
        /// </summary>
        /// <param name="context"></param>
        public override void Process(ConvertMediaItemContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            ChangeMediaStorage(context);
        }

        protected virtual void ChangeMediaStorage(ConvertMediaItemContext context)
        {
            switch (context.MediaContext.Options.ConversionType)
            {
                case ConversionType.Blob:
                    ChangeToDatabase(context);
                    break;
                case ConversionType.File:
                    ChangeToFile(context);
                    break;
            }
        }

        protected void ChangeToFile(ConvertMediaItemContext context)
        {
            var item = context.Item;
            MediaItem mediaItem = item;
            var blobField = item.Fields[Settings.BlobFieldName];

            if (blobField == null)
            {
                // Might be that the item passed the IsMediaItem check (all kind of items can be part of the media lib)
                context.Result = new ConvertMediaItemResult(ConversionAction.Skipped,
                    $"Skipped item [{item.Name}:{item.ID}] because the item didn't contain a blobfield");
                return;
            }

            // blobField.GetBlobStream() bypasses any caches thus it could have negative performance impact. Though it guarantees latest data from the database.
            // mediaItem.GetMediaStream() does not return valid data when there is a version with a file based media.
            var memoryStream = blobField.GetBlobStream();
            if (memoryStream == null)
            {
                var abortMessage = "Item {0} - {1} does not have any media content.".FormatWith(item.Name,
                    item.Uri.ToString());
                context.AbortPipeline(ConversionAction.Failed, abortMessage);
            }

            var filePath = GetFilePath(mediaItem);
            try
            {
                var fileExists = FileExists(ref filePath, memoryStream);
                if (!fileExists)
                    SaveToFile(memoryStream, filePath);
                SetFilePath(mediaItem, filePath);
                context.CleanupReference = blobField.Value;
            }
            catch (Exception exception)
            {
                var message = "Failed to convert blob into file for item: {0} - {1}. Exception: {2}".FormatWith(
                    item.ID, item.Uri.ToString(), exception.ToString());
                context.AbortPipeline(ConversionAction.Failed, message);
            }
            context.Result = new ConvertMediaItemResult(ConversionAction.Processed,
                "Media storage for item: {0} - {1} has been changed to file system: {2}".FormatWith(item.Name,
                    item.Uri.ToString(), FileUtil.UnmapPath(filePath, false)));
        }

        protected void ChangeToDatabase(ConvertMediaItemContext context)
        {
            string abortMessage;
            var item = context.Item;
            var blobField = item.Fields[Settings.BlobFieldName];

            if (blobField == null)
            {
                // Might be that the item passed the IsMediaItem check (all kind of items can be part of the media lib)
                context.Result = new ConvertMediaItemResult(ConversionAction.Skipped,
                    $"Skipped item [{item.Name}:{item.ID}] because the item didn't contain a blobfield");
                return;
            }

            MediaItem mediaItem = item;
            var filePath = mediaItem.FilePath;
            // mediaItem.GetMediaStream() looks for media data in cache first and only when not found calls to lower level API.
            var memoryStream = mediaItem.GetMediaStream();
            if (memoryStream.Length > Sitecore.Configuration.Settings.Media.MaxSizeInDatabase)
            {
                abortMessage =
                    "Media content size exceeds allowed limit configured in MaxSizeInDatabase setting. Adjust the setting if you want to store large media content in the database. Item: {0} - {1}"
                        .FormatWith(item.Name, item.Uri.ToString());
                context.AbortPipeline(ConversionAction.Skipped, abortMessage);
            }

            try
            {
                using (new EditContext(item))
                {
                    blobField.SetBlobStream(memoryStream);
                }
                context.CleanupReference = filePath;
            }
            catch (Exception exception)
            {
                abortMessage = exception.ToString();
                context.AbortPipeline(ConversionAction.Failed, abortMessage);
            }
            finally
            {
                if (memoryStream != null)
                    memoryStream.Close();
            }
            context.Result = new ConvertMediaItemResult(ConversionAction.Processed,
                "Media storage for item: {0} - {1} has been changed to database. Blob id: {2}".FormatWith(item.Name,
                    item.Uri.ToString(), blobField.Value));
        }

        private static string GetFilePath(MediaItem item)
        {
            var path = MediaManager.Creator.GetMediaStorageFolder(item.ID, GetFileName(item));
            var relativePath = FileUtil.MakePath(Sitecore.Configuration.Settings.Media.FileFolder, path, '/');
            var absolutePath = FileUtil.MapPath(relativePath);
            FileUtil.EnsureFileFolder(absolutePath);
            return absolutePath;
        }

        private static string GetFileName(MediaItem item)
        {
            return FileUtil.MakePath(item.InnerItem.Parent.Paths.LongID,
                FileUtil.MakePath(item.Name, item.Extension, '.'), '/');
        }

        private static void SaveToFile(Stream stream, string fileName)
        {
            var buffer = new byte[8192];
            using (var fs = File.Create(fileName))
            {
                int length;
                do
                {
                    length = stream.Read(buffer, 0, buffer.Length);
                    fs.Write(buffer, 0, length);
                } while (length > 0);

                fs.Flush();
                fs.Close();
            }
        }

        private void SetFilePath(MediaItem mediaItem, string filePath)
        {
            var path = FileUtil.UnmapPath(filePath, false);
            using (new EditContext(mediaItem.InnerItem))
            {
                mediaItem.FilePath = path;
            }
        }

        private bool FileExists(ref string filePath, Stream memoryStream)
        {
            while (File.Exists(filePath))
            {
                if (FileEqualsStream(filePath, memoryStream))
                    return true;

                filePath = FileUtil.GetUniqueFilename(filePath);
            }

            return false;
        }

        // This check may consume more time then just deleting and recreating the file.
        // TODO: look into optimizing/removing this code.
        private static bool FileEqualsStream(string fileName, Stream stream)
        {
            var fi = new FileInfo(fileName);
            if (fi.Length != stream.Length)
                return false;

            try
            {
                using (var fs = File.OpenRead(fileName))
                {
                    return StreamsEqual(fs, stream);
                }
            }
            finally
            {
                if (stream.CanSeek)
                    stream.Seek(0, SeekOrigin.Begin);
            }
        }

        private static bool StreamsEqual(Stream fileStream, Stream memoryStream)
        {
            if (fileStream.Length != memoryStream.Length)
                return false;

            const int bufLenght = 8192;
            var buffer1 = new byte[bufLenght];
            var buffer2 = new byte[bufLenght];

            int length;
            do
            {
                length = fileStream.Read(buffer1, 0, bufLenght);
                memoryStream.Read(buffer2, 0, bufLenght);

                for (var i = 0; i < length; i++)
                    if (buffer1[i] != buffer2[i])
                        return false;
            } while (length > 0);


            return true;
        }
    }
}