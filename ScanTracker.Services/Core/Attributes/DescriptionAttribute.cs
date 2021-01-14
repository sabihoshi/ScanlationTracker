﻿using System;
using Discord.Commands;

namespace ScanTracker.Services.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Enum | AttributeTargets.Field)]
    public class DescriptionAttribute : SummaryAttribute
    {
        public DescriptionAttribute(string text) : base(text)
        {
        }
    }
}