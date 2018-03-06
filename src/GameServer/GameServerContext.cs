﻿// <copyright file="GameServerContext.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer
{
    using System;
    using System.Linq;
    using MUnique.OpenMU.DataModel.Configuration;
    using MUnique.OpenMU.GameLogic;
    using MUnique.OpenMU.GameServer.MessageHandler;
    using MUnique.OpenMU.GameServer.RemoteView;
    using MUnique.OpenMU.Interfaces;
    using MUnique.OpenMU.Persistence;

    /// <summary>
    /// The context of a game server which contains all important configurations and services used by one game server instance.
    /// </summary>
    public class GameServerContext : GameContext, IGameServerContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GameServerContext"/> class.
        /// </summary>
        /// <param name="gameServerDefinition">The game server definition.</param>
        /// <param name="guildServer">The guild server.</param>
        /// <param name="loginServer">The login server.</param>
        /// <param name="friendServer">The friend server.</param>
        /// <param name="repositoryManager">The repository manager.</param>
        public GameServerContext(
            GameServerDefinition gameServerDefinition,
            IGuildServer guildServer,
            ILoginServer loginServer,
            IFriendServer friendServer,
            IRepositoryManager repositoryManager)
            : base(gameServerDefinition.GameConfiguration, repositoryManager)
        {
            this.Id = gameServerDefinition.ServerID;
            this.GuildServer = guildServer;
            this.LoginServer = loginServer;
            this.FriendServer = friendServer;
            this.ServerConfiguration = gameServerDefinition.ServerConfiguration;

            this.PacketHandlers = gameServerDefinition.ServerConfiguration.SupportedPacketHandlers.Select(m => new ConfigurableMainPacketHandler(m, this)).ToArray<IMainPacketHandler>();
            this.GuildCache = new GuildCache(this, new GuildInfoSerializer());
        }

        /// <inheritdoc/>
        public byte Id { get; }

        /// <inheritdoc/>
        public GuildCache GuildCache { get; }

        /// <inheritdoc/>
        public IGuildServer GuildServer { get; }

        /// <inheritdoc/>
        public ILoginServer LoginServer { get; }

        /// <inheritdoc/>
        public IFriendServer FriendServer { get; }

        /// <summary>
        /// Gets the main packet handlers.
        /// </summary>
        public IMainPacketHandler[] PacketHandlers { get; }

        /// <inheritdoc/>
        public GameServerConfiguration ServerConfiguration { get; }

        /// <inheritdoc/>
        public override void AddPlayer(Player player)
        {
            base.AddPlayer(player);
            player.PlayerLeftWorld += this.PlayerLeftWorld;
            player.PlayerEnteredWorld += this.PlayerEnteredWorld;
        }

        /// <inheritdoc/>
        public override void RemovePlayer(Player player)
        {
            if (player == null)
            {
                return;
            }

            player.PlayerEnteredWorld -= this.PlayerEnteredWorld;
            player.PlayerLeftWorld -= this.PlayerLeftWorld;
            base.RemovePlayer(player);
        }

        private void PlayerEnteredWorld(object sender, EventArgs e)
        {
            if (sender is Player player)
            {
                this.FriendServer.SetOnlineState(player.SelectedCharacter.Id, player.SelectedCharacter.Name, this.Id);
                if (player.SelectedCharacter.GuildMemberInfo != null)
                {
                    player.ShortGuildID = this.GuildServer.GuildMemberEnterGame(player.SelectedCharacter.GuildMemberInfo.GuildId, player.SelectedCharacter.Name, this.Id);
                    this.GuildCache.RegisterShortId(player.SelectedCharacter.GuildMemberInfo.GuildId, player.ShortGuildID);
                }
            }
        }

        private void PlayerLeftWorld(object sender, EventArgs e)
        {
            if (sender is Player player)
            {
                this.FriendServer.SetOnlineState(player.SelectedCharacter.Id, player.SelectedCharacter.Name, 0xFF);
                if (player.SelectedCharacter.GuildMemberInfo != null)
                {
                    this.GuildServer.GuildMemberLeaveGame(player.SelectedCharacter.GuildMemberInfo.GuildId, player.SelectedCharacter.Name, this.Id);
                }
            }
        }
    }
}
