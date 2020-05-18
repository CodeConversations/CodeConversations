using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CodeConversations.Services;
using CodeConversations.Interactive;

namespace CodeConversations.Workers
{
    public class DotNetInteractiveProcessRunner : BackgroundService
    {
        internal static DotNetInteractiveProcessRunner Instance { get; private set; }
        private readonly ILogger<DotNetInteractiveProcessRunner> logger;

        private ConcurrentDictionary<string, Action<IKernelEvent>> pendingRequests =
            new ConcurrentDictionary<string, Action<IKernelEvent>>();

        public Process DotNetInteractiveProcess { get; private set; }

        public DotNetInteractiveProcessRunner(ILogger<DotNetInteractiveProcessRunner> logger,
            DotNetInteractiveCodeConversationsProxy proxy)
        {
            this.logger = logger;
            Instance = this;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting up.");

            var startInfo = new ProcessStartInfo("dotnet", "interactive stdio --default-kernel csharp")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            DotNetInteractiveProcess = new Process
            {
                StartInfo = startInfo
            };

            DotNetInteractiveProcess.Start();
            await base.StartAsync(cancellationToken);
        }

        public bool CanExecuteCode => DotNetInteractiveProcess != null && !DotNetInteractiveProcess.HasExited;
        public string SessionLanguage { get; set; }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping.");
            DotNetInteractiveProcess.Kill();
            await base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var output = await DotNetInteractiveProcess.StandardOutput.ReadLineAsync();

                    if (!string.IsNullOrEmpty(output))
                    {
                        logger.LogDebug(output);

                        var eventEnvelope = KernelEventEnvelope.Deserialize(output);

                        var token = eventEnvelope.Event.Command?.GetToken();

                        if (token != null && pendingRequests.TryGetValue(token, out var tracker))
                        {
                            tracker(eventEnvelope.Event);
                        }
                    }
                }
            }, stoppingToken);

            return Task.CompletedTask;
        }

        private void TrackEvents(string commandToken)
        {
            pendingRequests[commandToken] = kernelEvent =>
            {
                var channel = ContentSubjectHelper.GetOrCreateChannel(commandToken);
                switch (kernelEvent)
                {
                    case DisplayEventBase displayEvent:
                        var content = displayEvent.GetHtmlOrPlainText();
                        logger.LogInformation(content.Value);
                        channel.OnNext(content);
                        break;
                    case CommandHandled _:
                        pendingRequests.TryRemove(commandToken, out _);
                        logger.LogInformation("Completed");
                        channel.OnCompleted();
                        break;
                    case CommandFailed commandFailed:
                        var errMessage = commandFailed.Message;
                        pendingRequests.TryRemove(commandToken, out _);
                        logger.LogError(errMessage);
                        channel.OnError(new Exception(errMessage));
                        channel.OnCompleted();
                        break;
                }
            };
        }

        internal async Task ExecuteEnvelope(string token)
        {
            var envelope = EnvelopeHelper.GetAndDeleteEnvelope(token);
            if (envelope != null)
            {
                TrackEvents(token);
                var serialized = KernelCommandEnvelope.Serialize(envelope);
                await DotNetInteractiveProcess.StandardInput.WriteLineAsync(serialized);
            }
        }
    }

    internal static class ContentSubjectHelper
    {
        private static ConcurrentDictionary<string, ContentSubject> openChannels =
            new ConcurrentDictionary<string, ContentSubject>();

        public static ContentSubject GetOrCreateChannel(string token)
        {
            return openChannels.GetOrAdd(token, (key) => new ContentSubject(key));
        }
        public static void DeleteChannel(string token)
        {
            openChannels.TryRemove(token, out _);
        }

        public static void DeleteExpired()
        {
            var toDelete = openChannels.Values.Where(c => (DateTime.UtcNow - c.CreationTime).TotalHours > 4)
                .Select(c => c.Token).ToList();

            foreach (var token in toDelete)
            {
                openChannels.TryRemove(token, out _);
            }
        }
    }

    internal static class EnvelopeHelper
    {
        private static ConcurrentDictionary<string, IKernelCommandEnvelope> envelopes =
            new ConcurrentDictionary<string, IKernelCommandEnvelope>();

        public static void StoreEnvelope(string token, IKernelCommandEnvelope envelope)
        {
            envelopes.AddOrUpdate(token, envelope, (key, oldValue) => envelope);
        }
        public static IKernelCommandEnvelope GetAndDeleteEnvelope(string token)
        {
            envelopes.TryRemove(token, out var envelope);
            return envelope;
        }
    }
}