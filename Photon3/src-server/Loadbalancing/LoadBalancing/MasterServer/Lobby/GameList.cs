﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GameList.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Defines the GameList type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.MasterServer.Lobby
{
    #region using directives

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using ExitGames.Logging;

    using Photon.LoadBalancing.MasterServer.GameServer;
    using Photon.LoadBalancing.Operations;
    using Photon.LoadBalancing.ServerToServer.Events;

    #endregion

    public class GameList
    {
        #region Constants and Fields

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();

        private readonly Dictionary<string, GameState> changedGames;

        private readonly LinkedListDictionary<string, GameState> gameDict;

        private readonly HashSet<string> removedGames;

        #endregion

        #region Constructors and Destructors

        private Guid id; 

        public GameList()
        {
            this.id = Guid.NewGuid();
            this.gameDict = new LinkedListDictionary<string, GameState>();
            this.changedGames = new Dictionary<string, GameState>();
            this.removedGames = new HashSet<string>();
        }

        #endregion

        #region Properties

        public int ChangedGamesCount
        {
            get
            {
                return this.changedGames.Count + this.removedGames.Count;
            }
        }

        public int Count
        {
            get
            {
                return this.gameDict.Count;
            }
        }

        #endregion

        #region Public Methods

        public void AddGameState(GameState gameState)
        {
            this.gameDict.Add(gameState.Id, gameState);
        }

        public void CheckJoinTimeOuts(int timeOutSeconds)
        {
            this.CheckJoinTimeOuts(TimeSpan.FromSeconds(timeOutSeconds));
        }

        public void CheckJoinTimeOuts(TimeSpan timeOut)
        {
            DateTime minDate = DateTime.UtcNow.Subtract(timeOut);
            this.CheckJoinTimeOuts(minDate);
        }

        public void CheckJoinTimeOuts(DateTime minDateTime)
        {
            var toRemove = new List<GameState>();

            foreach (GameState gameState in this.gameDict)
            {
                if (gameState.JoiningPlayerCount > 0)
                {
                    gameState.CheckJoinTimeOuts(minDateTime);

                    // check if there are still players left for the game
                    if (gameState.PlayerCount == 0)
                    {
                        toRemove.Add(gameState);
                    }
                }
            }

            // remove all games where no players left
            foreach (GameState gameState in toRemove)
            {
                this.RemoveGameState(gameState.Id);
            }
        }

        public bool ContainsGameId(string gameId)
        {
            return this.gameDict.ContainsKey(gameId);
        }

        public Hashtable GetAllGames(int maxCount)
        {
            if (maxCount <= 0)
            {
                maxCount = this.gameDict.Count;
            }

            var hashTable = new Hashtable(maxCount);

            int i = 0;
            foreach (GameState game in this.gameDict)
            {
                if (game.IsVisbleInLobby)
                {
                    Hashtable gameProperties = game.ToHashTable();
                    hashTable.Add(game.Id, gameProperties);
                    i++;
                }

                if (i == maxCount)
                {
                    break;
                }
            }

            return hashTable;
        }

        public Hashtable GetChangedGames()
        {
            if (this.changedGames.Count == 0 && this.removedGames.Count == 0)
            {
                return null;
            }

            var hashTable = new Hashtable(this.changedGames.Count + this.removedGames.Count);

            foreach (GameState gameInfo in this.changedGames.Values)
            {
                if (gameInfo.IsVisbleInLobby)
                {
                    Hashtable gameProperties = gameInfo.ToHashTable();
                    hashTable.Add(gameInfo.Id, gameProperties);
                }
            }

            foreach (string gameId in this.removedGames)
            {
                hashTable.Add(gameId, new Hashtable { { (byte)GameParameter.Removed, true } });
            }

            this.changedGames.Clear();
            this.removedGames.Clear();

            return hashTable;
        }

        public void RemoveGameServer(IncomingGameServerPeer gameServer)
        {
            // find games belonging to the game server instance
            List<GameState> instanceStates = this.gameDict.Where(gameState => gameState.GameServer == gameServer).ToList();

            // remove game server instance games
            foreach (GameState gameState in instanceStates)
            {
                this.RemoveGameState(gameState.Id);
            }
        }

        public bool RemoveGameState(string gameId)
        {
            GameState gameState;
            if (!this.gameDict.TryGet(gameId, out gameState))
            {
                return false;
            }

            if (log.IsDebugEnabled)
            {
                LogGameState("RemoveGameState:", gameState);
            }

            this.gameDict.Remove(gameId);
            this.changedGames.Remove(gameId);
            this.removedGames.Add(gameId);
            return true;
        }

        public bool TryGetGame(string gameId, out GameState gameState)
        {
            return this.gameDict.TryGet(gameId, out gameState);
        }

        public bool TryGetRandomGame(ILobbyPeer peer, Hashtable gameProperties, out GameState gameState)
        {
            foreach (GameState game in this.gameDict)
            {
                if (game.IsOpen && game.IsVisible && game.IsCreatedOnGameServer && (game.MaxPlayer <= 0 || game.PlayerCount < game.MaxPlayer))
                {
                    if (game.IsBlocked(peer))
                    {
                        continue;
                    }

                    if (gameProperties != null && game.MatchGameProperties(gameProperties) == false)
                    {
                        continue;
                    }

                    gameState = game;
                    return true;
                }
            }

            gameState = null;
            return false;
        }

        public bool UpdateGameState(UpdateGameEvent updateOperation, out GameState gameState)
        {
            // try to get the game state 
            if (this.gameDict.TryGet(updateOperation.GameId, out gameState) == false)
            {
                if (updateOperation.Reinitialize)
                {
                    if (log.IsDebugEnabled)
                    {
                        log.DebugFormat("Reinitialize: Add Game State {0}", updateOperation.GameId);
                    }
                    
                    this.AddGameState(gameState);
                    
                }
                else
                {
                    if (log.IsDebugEnabled)
                    {
                        log.DebugFormat("Game not found: {0}", updateOperation.GameId);
                    }
                    return false;
                }
            }

            bool oldVisible = gameState.IsVisbleInLobby;
            bool changed = gameState.Update(updateOperation);

            if (changed)
            {
                if (gameState.IsVisbleInLobby)
                {
                    this.changedGames[gameState.Id] = gameState;

                    if (oldVisible == false)
                    {
                        this.removedGames.Remove(gameState.Id);
                    }
                }
                else
                {
                    if (oldVisible)
                    {
                        this.changedGames.Remove(gameState.Id);
                        this.removedGames.Add(gameState.Id);
                    }
                }

                if (log.IsDebugEnabled)
                {
                    LogGameState("UpdateGameState: ", gameState);
                }
            }

            return true;
        }

        #endregion

        #region Methods

        private static void LogGameState(string prefix, GameState gameState)
        {
            if (log.IsDebugEnabled)
            {
                log.DebugFormat(
                    "{0}id={1}, peers={2}, max={3}, open={4}, visible={5}, peersJoining={6}", 
                    prefix, 
                    gameState.Id, 
                    gameState.GameServerPlayerCount, 
                    gameState.MaxPlayer, 
                    gameState.IsOpen, 
                    gameState.IsVisible, 
                    gameState.JoiningPlayerCount);
            }
        }

        #endregion
    }
}