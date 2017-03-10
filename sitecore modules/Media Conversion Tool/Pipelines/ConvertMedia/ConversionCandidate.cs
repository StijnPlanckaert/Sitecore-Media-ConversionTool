namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Data;

   public class ConversionCandidate
   {
      public ConversionCandidate(ItemUri uri, bool deep)
      {
         Uri = uri;
         ItemId = uri.ItemID;
         DatabaseName = uri.DatabaseName;
         Deep = deep;
      }

      public ConversionCandidate(ItemUri uri) : this(uri, false){}

      public bool Deep { get; set; }

      public IEnumerable<ConversionCandidate> Children
      {
         get
         {
            return ConversionQueue.GetChildIterator(Uri, Deep);
         }
      }

      public ID ItemId { get; set; }

      public ItemUri Uri { get; set; }

      public string DatabaseName { get; set; }
   }
}