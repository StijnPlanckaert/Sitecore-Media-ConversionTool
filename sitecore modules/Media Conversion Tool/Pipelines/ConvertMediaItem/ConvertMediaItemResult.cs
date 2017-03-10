namespace Sitecore.Modules.MediaConversionTool.Pipelines.ConvertMediaItem
{
   public class ConvertMediaItemResult
   {
      public ConvertMediaItemResult(ConversionAction action, string message)
      {
         Action = action;
         Message = message;
      }

      public ConversionAction Action { get; set; }
      public string Message { get; set; }
   }
}