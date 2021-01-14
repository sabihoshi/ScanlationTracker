﻿using System;

namespace ScanTracker.Services.Core.Attributes
{
    /// <summary>
    ///     Hides the module or command from display
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class HiddenAttribute : Attribute
    {
    }
}