﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ErrorCode.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Defines the ErrorCode type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.Operations
{
    public enum ErrorCode : short
    {
        OperationDenied = -3, 
        OperationInvalid = -2, 
        InternalServerError = -1, 

        Ok = 0, 

        InvalidAuthentication = 0x7FFF, // codes start at short.MaxValue 
        GameIdAlreadyExists = 0x7FFF - 1,
        GameFull = 0x7FFF - 2,
        GameClosed = 0x7FFF - 3,
        AlreadyMatched = 0x7FFF - 4,
        ServerFull = 0x7FFF - 5,
        UserBlocked = 0x7FFF - 6,
        NoMatchFound = 0x7FFF - 7,
        RedirectRepeat = 0x7FFF - 8,
        GameIdNotExists = 0x7FFF - 9
    }
}