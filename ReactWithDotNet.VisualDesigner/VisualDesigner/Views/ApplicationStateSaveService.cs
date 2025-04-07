using System.Threading;
using Microsoft.Extensions.Hosting;

namespace ReactWithDotNet.VisualDesigner.Views;

public class ApplicationStateSaveService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var (_, state) in ApplicationStateCache)
            {
                await UpdateLastUsageInfo(state);

                await TrySaveComponentForUser(state);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}