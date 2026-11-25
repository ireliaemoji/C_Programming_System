public sealed class ExamDeadlineWorker(ExamStore store, ILogger<ExamDeadlineWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try { await store.FinalizeExpiredAttemptsAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
            catch (MySqlConnector.MySqlException exception)
            {
                logger.LogWarning("Exam settlement will retry (MySQL {ErrorCode}).", exception.ErrorCode);
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
