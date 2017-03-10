using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;

namespace Sitecore.Modules.MediaConversionTool.Utils
{
    internal static class Utils
    {
        private static readonly HashSet<ID> IgnoreFolders = new HashSet<ID>
        {
            TemplateIDs.Node,
            TemplateIDs.Folder,
            TemplateIDs.MediaFolder
        };
        
        /// <summary>
        /// Gets all versions of the item that has media content.
        /// </summary>
        /// <param name="item">The item</param>
        /// <returns>Item[]</returns>
        public static Item[] GetAllVersionsWithMedia(Item item)
        {
            var versionable = IsVersionable(item);

            if (!versionable && IsMediaItem(item))
                return new[] {item};

            return item.Versions.GetVersions(versionable).Where(IsMediaItem).ToArray();
        }

        public static bool IsMediaItem(Item item)
        {
            // HasMediaContent only checks if the item path is part of the media lib path
            // return MediaManager.HasMediaContent(item);
            return item.Paths.IsMediaItem && !IgnoreFolders.Contains(item.TemplateID);
        }

        public static bool IsVersionable(Item item)
        {
            var mediaField = item.Fields["blob"];
            if (mediaField == null)
                return false;

            return !mediaField.Shared;
        }

        /// <summary>
        ///     Checks whether an item with media is data stored as a file
        /// </summary>
        /// <param name="item">An item with some media data</param>
        /// <returns></returns>
        public static bool IsFileBased(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            return new MediaItem(item).FilePath.Length > 0;
        }

        public static string GetFriendlyFileSize(long sizeInBytes)
        {
            if (sizeInBytes < 1000)
                return sizeInBytes + " bytes";
            if (sizeInBytes < 1000000)
                return sizeInBytes/1000 + " kb";

            var temp = sizeInBytes/10000;
            var megs = temp/100.0;

            return megs.ToString("n") + " mb";
        }

        public static Item GetItem(ID itemId, string databaseName)
        {
            Item item = null;
            var database = Factory.GetDatabase(databaseName);
            if (database != null)
                item = database.GetItem(itemId);

            return item;
        }

        public static Item GetItem(ItemUri uri)
        {
            return Database.GetItem(uri);
        }

        public static bool CanConvert(Item item, User user, ref string message)
        {
            using (new SecurityEnabler())
            {
                var flag = AuthorizationManager.IsAllowed(item, AccessRight.ItemRead, user) &&
                           AuthorizationManager.IsAllowed(item, AccessRight.ItemWrite, user);
                if (!flag)
                    message =
                        "User does not have the required Read/Write access. To convert a media asset the user must have read and write access. User: {0}, Item: {1}"
                            .FormatWith(user.Name, item.Uri);
                return flag;
            }
        }
    }
}
