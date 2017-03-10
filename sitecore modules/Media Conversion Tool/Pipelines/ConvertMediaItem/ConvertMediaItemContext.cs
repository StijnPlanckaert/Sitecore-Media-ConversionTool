namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   using Data.Items;
   using Diagnostics;
   using Jobs;
   using Pipelines.ConvertMedia;
   using Utils;
   using Sitecore.Pipelines;
   using Security.Accounts;

   public class ConvertMediaItemContext : PipelineArgs
   {
      public ConvertMediaItemContext(ConversionCandidate candidate, ConvertMediaContext convertMediaContext)
      {
         Assert.ArgumentNotNull(candidate, "candidate");
         Assert.ArgumentNotNull(convertMediaContext, "mediaContext");

         Job = convertMediaContext.Job;
         User = convertMediaContext.User;
         MediaContext = convertMediaContext;
         ConversionOptions = convertMediaContext.Options;
         Item = Utils.GetItem(candidate.Uri);
      }

      public virtual void AbortPipeline(ConversionAction action, string message)
      {
         Result = new ConvertMediaItemResult(action, message);
         base.AbortPipeline();
      }

      public ConvertMediaContext MediaContext { get; set; }
      public Job Job { get; set; }
      public User User { get; set; }
      public Item Item { get; set; }
      public ConversionOptions ConversionOptions { get; set; }
      public ConvertMediaItemResult Result { get; set; }
      public string CleanupReference { get; set; }
   }
}