﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeedbackControllerCollection.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Defines the FeedbackControllerCollection type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Photon.LoadBalancing.LoadShedding
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class FeedbackControllerCollection
    {
        private readonly Dictionary<FeedbackName, FeedbackController> values;

        public FeedbackControllerCollection(params FeedbackController[] controller)
        {
            this.values = new Dictionary<FeedbackName, FeedbackController>(controller.Length);
            this.Output = FeedbackLevel.Lowest;
            foreach (var c in controller)
            {
                this.values.Add(c.FeedbackName, c);
                if (c.Output > this.Output)
                {
                    this.Output = c.Output;
                }
            }
        }

        public FeedbackLevel Output { get; private set; }

        public FeedbackLevel CalculateOutput()
        {
            return this.Output = this.values.Values.Max(controller => controller.Output);
        }

        public FeedbackLevel SetInput(FeedbackName key, int input)
        {
            // Controllers are optional, we don't need to configure them all. 
            FeedbackController controller;
            if (this.values.TryGetValue(key, out controller))
            {
                if (controller.SetInput(input))
                {
                    if (controller.Output > this.Output)
                    {
                        return this.Output = controller.Output;
                    }

                    if (controller.Output < this.Output)
                    {
                        return this.CalculateOutput();
                    }
                }
            }

            return this.Output;
        }
    }
}