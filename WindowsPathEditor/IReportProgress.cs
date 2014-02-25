namespace WindowsPathEditor
{
    internal interface IReportProgress
    {
        void Begin();

        void ReportProgress(string progress);

        void Done();

        bool Cancelled { get; }
    }
}