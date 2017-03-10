namespace Sitecore.Modules.MediaConversionTool
{
    using Data;

    public class ConversionReference
    {
        public ItemUri ItemUri { get; set; }
        public bool Recursive { get; set; }

        public ConversionReference(ItemUri uri, bool recursive)
        {
            ItemUri = uri;
            Recursive = recursive;
        }
    }
}