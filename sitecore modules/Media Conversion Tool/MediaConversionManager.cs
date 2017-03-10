namespace Sitecore.Modules.MediaConversionTool
{
   using System.Collections.Generic;
   using Diagnostics;
   using Jobs;
   using Pipelines.ConvertMedia;
   using Security.Accounts;

   public class MediaConversionManager
   {
      private const string JobName = "MediaConversion";
      public static Job StartConversion(List<ConversionReference> references, ConversionOptions options, User user)
      {
         Assert.ArgumentNotNull(references, "references");
         Assert.ArgumentNotNull(options, "conversion options");
         Assert.ArgumentNotNull(user, "user");
         var jobOptions = new JobOptions(JobName, "MediaConversionTool", "system", new MediaConversionManager(), "DoConvert", new object[] {references, options, user});
         return new Job(jobOptions);
      }

      protected void DoConvert(List<ConversionReference> references, ConversionOptions options, User user)
      {
         ConvertMediaContext context = new ConvertMediaContext(references);
         context.Options = options;
         context.User = user;
         context.Job = GetJob(JobName);
         ConvertMediaPipeline.Run(context);
      }

      private Job GetJob(string jobName)
      {
         return JobManager.GetJob(jobName);
      }
   }
}