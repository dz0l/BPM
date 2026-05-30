using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Tests;

public class PrintQueueJobControlTests
{
    [Fact]
    public void PauseJob_SetsPausedStatus()
    {
        var service = new PrintQueueService();
        var job = CreateTempJob(service);

        service.PauseJob(job.Id);

        Assert.Equal(PrintJobStatus.Paused, service.Jobs[0].Status);
    }

    [Fact]
    public void ResumeJob_RestoresPendingStatus()
    {
        var service = new PrintQueueService();
        var job = CreateTempJob(service);
        service.PauseJob(job.Id);

        service.ResumeJob(job.Id);

        Assert.Equal(PrintJobStatus.Pending, service.Jobs[0].Status);
    }

    [Fact]
    public void RetryJob_RestoresFailedJob()
    {
        var service = new PrintQueueService();
        var job = CreateTempJob(service);
        service.UpdateStatus(job.Id, PrintJobStatus.Failed, errorMessage: "error");

        service.RetryJob(job.Id);

        Assert.Equal(PrintJobStatus.Pending, service.Jobs[0].Status);
        Assert.Null(service.Jobs[0].ErrorMessage);
    }

    [Fact]
    public void HasActiveJobs_ReturnsTrueWhenPrinting()
    {
        var service = new PrintQueueService();
        var job = CreateTempJob(service);
        service.UpdateStatus(job.Id, PrintJobStatus.Printing);

        Assert.True(service.HasActiveJobs);
    }

    private static PrintJob CreateTempJob(PrintQueueService service)
    {
        var tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".txt");
        File.WriteAllText(tempFile, "test");
        return service.Add(tempFile);
    }
}
