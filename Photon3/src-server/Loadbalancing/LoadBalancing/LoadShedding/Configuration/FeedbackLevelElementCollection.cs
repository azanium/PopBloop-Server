﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeedbackLevelElementCollection.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.LoadShedding.Configuration
{
    using System;
    using System.Configuration;

    internal class FeedbackLevelElementCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new FeedbackLevelElement(); 
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((FeedbackLevelElement)element).Level;
        }
    }
}
