﻿using System;
using Discord;
using Discord.WebSocket;
using MediatR;

namespace ScanTracker.Services.Core.Messages
{
    /// <summary>
    ///     Describes an application-wide notification that occurs when <see cref="IBaseSocketClient.ReactionRemoved" /> is
    ///     raised.
    /// </summary>
    public class ReactionRemovedNotification : INotification
    {
        /// <summary>
        ///     Constructs a new <see cref="ReactionRemovedNotification" /> object from the given data values.
        /// </summary>
        /// <param name="message">The value to use for <see cref="Message" />.</param>
        /// <param name="channel">The value to use for <see cref="Channel" />.</param>
        /// <param name="reaction">The value to use for <see cref="Reaction" />.</param>
        /// <exception cref="ArgumentNullException">Throws for <paramref name="channel" /> and <paramref name="reaction" />.</exception>
        public ReactionRemovedNotification(
            Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            Message = message;
            Channel = channel ?? throw new ArgumentNullException(nameof(channel));
            Reaction = reaction ?? throw new ArgumentNullException(nameof(reaction));
        }

        /// <summary>
        ///     The message (if cached) to which a reaction was added.
        /// </summary>
        public Cacheable<IUserMessage, ulong> Message { get; }

        /// <summary>
        ///     The channel in which a reaction was added to a message.
        /// </summary>
        public ISocketMessageChannel Channel { get; }

        /// <summary>
        ///     The reaction that was added to a message.
        /// </summary>
        public SocketReaction Reaction { get; }
    }
}