namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMedia
{
   using System.Collections.Generic;
   using Jobs;
   using Sitecore.Pipelines;
   using Security.Accounts;

   public class ConvertMediaContext : PipelineArgs
   {
      public ConvertMediaContext(List<ConversionReference> references)
      {
         References = references;
         Queue = new List<IEnumerable<ConversionCandidate>>();
         Statistics = new ConversionStatistics();
      }

      #region Properties
      
      public Job Job { get; set; }
      public List<IEnumerable<ConversionCandidate>> Queue { get; private set; }
      public ConversionStatistics Statistics { get; private set; }
      public User User { get; set; }
      public List<ConversionReference> References { get; set; }
      public ConversionOptions Options { get; set; }

      #endregion Properties
   }
}