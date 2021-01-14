using System;
using MediatR;

namespace ScanTracker.Services.Core.Messages
{
    /// <summary>
    ///     Describes an application-wide notification that occurs when <see cref="IDiscordSocketClient.Disconnected" /> is
    ///     raised.
    /// </summary>
    public class DisconnectedNotification : INotification
    {
        public DisconnectedNotification(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}