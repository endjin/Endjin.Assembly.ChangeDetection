namespace Endjin.Assembly.ChangeDetection.SemVer
{
    public class AnalysisResult
    {
        public string VersionNumber { get; set; }

        public bool BreakingChangesDetected { get; set; }
    }
}