﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeedbackControlSystem.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Defines the FeedbackControlSystem type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.LoadShedding
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using ExitGames.Logging;

    using Photon.LoadBalancing.GameServer;
    using Photon.LoadBalancing.LoadShedding.Configuration;

    internal sealed class FeedbackControlSystem : IFeedbackControlSystem, IDisposable
    {
        #region Constants and Fields

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        
        private readonly int maxCcu;

        private readonly string applicationRootPath; 

        private readonly FileSystemWatcher fileWatcher;

        private FeedbackControllerCollection controllerCollection;
        
        #endregion

        #region Constructors and Destructors

        public FeedbackControlSystem(int maxCcu, string applicationRootPath)
        {
            this.maxCcu = maxCcu;
            this.applicationRootPath = applicationRootPath; 

            this.Initialize();

            this.fileWatcher = new FileSystemWatcher(applicationRootPath, GameServerSettings.Default.WorkloadConfigFile);
            this.fileWatcher.Changed += this.ConfigFileChanged;
            this.fileWatcher.Created += this.ConfigFileChanged;
            this.fileWatcher.Deleted += this.ConfigFileChanged;
            this.fileWatcher.Renamed += this.ConfigFileChanged;
            this.fileWatcher.EnableRaisingEvents = true;
        }

        #endregion

        #region Properties

        public FeedbackLevel Output
        {
            get
            {
                return this.controllerCollection.Output;
            }
        }

        #endregion

        #region Implemented Interfaces

        #region IFeedbackControlSystem

        public void SetBandwidthUsage(int bytes)
        {
            this.controllerCollection.SetInput(FeedbackName.Bandwidth, bytes);
        }

        public void SetBusinessLogicQueueLength(int businessLogicQueue)
        {
            this.controllerCollection.SetInput(FeedbackName.BusinessLogicQueueLength, businessLogicQueue);
        }

        public void SetCpuUsage(int cpuUsage)
        {
            this.controllerCollection.SetInput(FeedbackName.CpuUsage, cpuUsage);
        }

        public void SetENetQueueLength(int enetQueue)
        {
            this.controllerCollection.SetInput(FeedbackName.ENetQueueLength, enetQueue);
        }

        public void SetLatencyTcp(int averageLatencyMs)
        {
            this.controllerCollection.SetInput(FeedbackName.LatencyTcp, averageLatencyMs);
        }

        public void SetLatencyUdp(int averageLatencyMs)
        {
            this.controllerCollection.SetInput(FeedbackName.LatencyUdp, averageLatencyMs);
        }

        public void SetOutOfRotation(bool isOutOfRotation)
        {
            this.controllerCollection.SetInput(FeedbackName.OutOfRotation, isOutOfRotation ? 1 : 0);
        }

        public void SetPeerCount(int peerCount)
        {
            this.controllerCollection.SetInput(FeedbackName.PeerCount, peerCount);
        }

        public void SetTimeSpentInServer(int timeSpentInServer)
        {
            this.controllerCollection.SetInput(FeedbackName.TimeSpentInServer, timeSpentInServer);
        }

        public void SetEnetThreadsProcessing(int enetThreadsProcessing)
        {
            this.controllerCollection.SetInput(FeedbackName.EnetThreadsProcessing, enetThreadsProcessing);
        }

        public void SetDisconnectRateUdp(int udpDisconnectRate)
        {
            this.controllerCollection.SetInput(FeedbackName.DisconnectRateUdp, udpDisconnectRate); 
        }

        #endregion

        #endregion

        #region Methods

        private static List<FeedbackController> GetNonConfigurableControllers(int maxCcu)
        {
            Dictionary<FeedbackLevel, int> peerCountThresholds = maxCcu == 0
                                                                     ? new Dictionary<FeedbackLevel, int>()
                                                                     : new Dictionary<FeedbackLevel, int> {
                                                                             { FeedbackLevel.Lowest, 1 }, 
                                                                             { FeedbackLevel.Low, 2 }, 
                                                                             { FeedbackLevel.Normal, maxCcu / 2 }, 
                                                                             { FeedbackLevel.High, maxCcu * 8 / 10 }, 
                                                                             { FeedbackLevel.Highest, maxCcu }
                                                                         };
            var peerCountController = new FeedbackController(FeedbackName.PeerCount, peerCountThresholds, 0, FeedbackLevel.Lowest);

            return new List<FeedbackController> { peerCountController };
        }

        private void ConfigFileChanged(object sender, FileSystemEventArgs e)
        {
            log.InfoFormat("Configuration file for Feedback Control System {0}\\{1} {2}. Reinitializing...", e.FullPath, e.Name, e.ChangeType);
            this.Initialize();
        }

        private void Initialize()
        {
            string message;
            FeedbackControlSystemSection section;
            string filename = Path.Combine(this.applicationRootPath, GameServerSettings.Default.WorkloadConfigFile);

            // CCU, Out-of-Rotation
            var allControllers = GetNonConfigurableControllers(this.maxCcu);

            if (!ConfigurationLoader.TryLoadFromFile(filename, out section, out message))
            {
                log.WarnFormat(
                    "Could not initialize Feedback Control System from configuration: Invalid configuration file {0}. Using default settings... ({1})", 
                    filename, 
                    message);
            }

            if (section != null)
            {
                // load controllers from config file.);
                foreach (FeedbackControllerElement controllerElement in section.FeedbackControllers)
                {
                    var dict = new Dictionary<FeedbackLevel, int>();
                    foreach (FeedbackLevelElement level in controllerElement.Levels)
                    {
                        dict.Add(level.Level, level.Value);
                    }

                    var controller = new FeedbackController(controllerElement.Name, dict, controllerElement.InitialInput, controllerElement.InitialLevel);

                    allControllers.Add(controller);
                }

                log.InfoFormat("Initialized FeedbackControlSystem with {0} controllers from config file.", section.FeedbackControllers.Count);
            }
            else
            {
                // default settings, in case no config file was found.
                allControllers.AddRange(DefaultConfiguration.GetDefaultControllers());
            }

            this.controllerCollection = new FeedbackControllerCollection(allControllers.ToArray());
        }

        #endregion

        public void Dispose()
        {
            if (this.fileWatcher != null)
            {
                this.fileWatcher.Dispose();
            }
        }
    }
}