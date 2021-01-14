using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScansTracker.Data.Config;
using ScanTracker.Services.Core.Listeners;
using Serilog;
using Serilog.Events;

namespace ScansTracker.Bot
{
    public class Bot
    {
        private const bool AttemptReset = true;
        private static CancellationTokenSource? _mediatorToken;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

        private CancellationTokenSource _reconnectCts = null!;

        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection().AddHttpClient().AddMemoryCache()
                .AddMediatR(c => c.Using<LyricaMediator>(),
                    typeof(Bot), typeof(LyricaMediator))
                .AddLogging(l => l.AddSerilog())
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .BuildServiceProvider();
        }

        private static void ContextOptions(DbContextOptionsBuilder optionsBuilder)
        {
            var configuration = new ConfigurationBuilder()
                .AddUserSecrets<TrackerConfig>()
                .Build();

            optionsBuilder
                .UseNpgsql(configuration.GetConnectionString("PostgreSQL"))
                .UseLazyLoadingProxies();
        }

        private static Task LogAsync(LogMessage message)
        {
            var severity = message.Severity switch
            {
                LogSeverity.Critical => LogEventLevel.Fatal,
                LogSeverity.Error    => LogEventLevel.Error,
                LogSeverity.Warning  => LogEventLevel.Warning,
                LogSeverity.Info     => LogEventLevel.Information,
                LogSeverity.Verbose  => LogEventLevel.Verbose,
                LogSeverity.Debug    => LogEventLevel.Debug,
                _                    => LogEventLevel.Information
            };

            Log.Write(severity, message.Exception, message.Message);

            return Task.CompletedTask;
        }

        public static async Task Main()
        {
            await new Bot().StartAsync();
        }

        public async Task StartAsync()
        {
            var config = new ConfigurationBuilder()
                .AddUserSecrets<TrackerConfig>()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console()
                .CreateLogger();
            await using var services = ConfigureServices();

            await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
            var commands = services.GetRequiredService<CommandService>();

            var client = services.GetRequiredService<DiscordSocketClient>();
            var mediator = services.GetRequiredService<IMediator>();

            _reconnectCts = new CancellationTokenSource();
            _mediatorToken = new CancellationTokenSource();

            await new DiscordSocketListener(client, mediator)
                .StartAsync(_mediatorToken.Token);

            client.Disconnected += _ => ClientOnDisconnected(client);
            client.Connected += ClientOnConnected;

            client.Log += LogAsync;
            commands.Log += LogAsync;

            await client.LoginAsync(TokenType.Bot, config.GetValue<string>(nameof(TrackerConfig.Token)));
            await client.StartAsync();

            await Task.Delay(-1);
        }

        private Task ClientOnConnected()
        {
            Log.Debug("Client reconnected, resetting cancel tokens...");

            _reconnectCts.Cancel();
            _reconnectCts = new CancellationTokenSource();

            Log.Debug("Client reconnected, cancel tokens reset.");
            return Task.CompletedTask;
        }

        private Task ClientOnDisconnected(DiscordSocketClient client)
        {
            // Check the state after <timeout> to see if we reconnected
            Log.Information("Client disconnected, starting timeout task...");
            _ = Task.Delay(Timeout, _reconnectCts.Token).ContinueWith(async _ =>
            {
                Log.Debug("Timeout expired, continuing to check client state...");
                await CheckStateAsync(client);
                Log.Debug("State came back okay");
            });

            return Task.CompletedTask;
        }

        private async Task CheckStateAsync(DiscordSocketClient client)
        {
            // Client reconnected, no need to reset
            if (client.ConnectionState == ConnectionState.Connected) return;
            if (AttemptReset)
            {
                Log.Information("Attempting to reset the client");

                var timeout = Task.Delay(Timeout);
                var connect = client.StartAsync();
                var task = await Task.WhenAny(timeout, connect);

                if (task == timeout)
                {
                    Log.Fatal("Client reset timed out (task deadlocked?), killing process");
                    FailFast();
                }
                else if (connect.IsFaulted)
                {
                    Log.Fatal("Client reset faulted, killing process", connect.Exception);
                    FailFast();
                }
                else if (connect.IsCompletedSuccessfully) Log.Information("Client reset succesfully!");

                return;
            }

            Log.Fatal("Client did not reconnect in time, killing process");
            FailFast();
        }

        private void FailFast()
        {
            Environment.Exit(1);
        }
    }
}