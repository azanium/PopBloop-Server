﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UpdateGameEvent.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Defines the UpdateGameEvent type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.ServerToServer.Events
{
    #region using directives

    using System.Collections;

    using Lite.Operations;

    using Photon.LoadBalancing.Operations;
    using Photon.SocketServer;
    using Photon.SocketServer.Rpc;

    #endregion

    public class UpdateGameEvent : DataContract
    {
        #region Constructors and Destructors

        public UpdateGameEvent()
        {
        }

        public UpdateGameEvent(IRpcProtocol protocol, IEventData eventData)
            : base(protocol, eventData.Parameters)
        {
        }

        #endregion

        #region Properties

        [DataMember(Code = (byte)ParameterCode.PeerCount, IsOptional = true)]
        public byte ActorCount { get; set; }

        [DataMember(Code = (byte)ParameterCode.ApplicationId, IsOptional = true)]
        public string ApplicationId { get; set; }

        [DataMember(Code = (byte)ParameterCode.AppVersion, IsOptional = true)]
        public string ApplicationVersion { get; set; }

        [DataMember(Code = (byte)ParameterCode.GameId, IsOptional = false)]
        public string GameId { get; set; }

        [DataMember(Code = (byte)ParameterKey.GameProperties, IsOptional = true)]
        public Hashtable GameProperties { get; set; }

        [DataMember(Code = (byte)ServerParameterCode.NewUsers, IsOptional = true)]
        public string[] NewUsers { get; set; }

        [DataMember(Code = (byte)ServerParameterCode.RemovedUsers, IsOptional = true)]
        public string[] RemovedUsers { get; set; }

        [DataMember(Code = (byte)ServerParameterCode.Reinitialize, IsOptional = true)]
        public bool Reinitialize { get; set; }

        #endregion
    }
}