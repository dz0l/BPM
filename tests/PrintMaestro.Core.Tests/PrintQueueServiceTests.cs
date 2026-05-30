using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Tests;

public class PrintQueueServiceTests
{
    [Fact]
    public void AddRange_SkipsUnsupportedFiles()
    {
        var service = new PrintQueueService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".txt");

        try
        {
            File.WriteAllText(tempFile, "test");
            var added = service.AddRange([tempFile, tempFile + ".unsupported"]);

            Assert.Single(added);
            Assert.Equal(PrintJobStatus.Pending, added[0].Status);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ClearCompleted_RemovesOnlyCompletedJobs()
    {
        var service = new PrintQueueService();
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".txt");

        try
        {
            File.WriteAllText(tempFile, "test");
            var job = service.Add(tempFile);
            service.UpdateStatus(job.Id, PrintJobStatus.Completed);

            service.ClearCompleted();

            Assert.Empty(service.Jobs);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
