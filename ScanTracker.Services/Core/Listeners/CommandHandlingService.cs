using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using ScanTracker.Services.Core.Messages;

namespace ScanTracker.Services.Core.Listeners
{
    public class CommandHandlingService : INotificationHandler<MessageReceivedNotification>
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<CommandHandlingService> _log;
        private readonly IServiceProvider _services;

        public CommandHandlingService(
            IServiceProvider services,
            ILogger<CommandHandlingService> log,
            CommandService commands,
            DiscordSocketClient discord)
        {
            _commands = commands;
            _services = services;
            _discord = discord;
            _log = log;

            _commands.CommandExecuted += CommandExecutedAsync;
        }

        public async Task Handle(MessageReceivedNotification notification, CancellationToken cancellationToken)
        {
            var rawMessage = notification.Message;

            if (!(rawMessage is SocketUserMessage message))
                return;

            if (message.Source != MessageSource.User)
                return;

            _log.LogTrace($"[{{0}}] {{1}}: {notification.Message.Content}",
                notification.Message.Channel,
                notification.Message.Author);

            var argPos = 0;
            var context = new SocketCommandContext(_discord, message);
            if (!(message.HasStringPrefix("p!", ref argPos, StringComparison.OrdinalIgnoreCase) ||
                  message.HasMentionPrefix(_discord.CurrentUser, ref argPos)))
                return;

            var result = await _commands.ExecuteAsync(context, argPos, _services, MultiMatchHandling.Best);

            if (result is null)
                _log.LogWarning("Command with context {0} ran by user {1} is null.", context, message.Author);
            else if (!result.IsSuccess) await CommandFailedAsync(context, result);
        }

        public Task CommandFailedAsync(ICommandContext context, IResult result)
        {
            return context.Channel.SendMessageAsync($"Error: {result.ErrorReason}");
        }

        public Task CommandExecutedAsync(
            Optional<CommandInfo> command,
            ICommandContext context,
            IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result.IsSuccess)
                return Task.CompletedTask;

            return Task.CompletedTask;
        }

        public Task InitializeAsync()
        {
            return _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }
    }
}