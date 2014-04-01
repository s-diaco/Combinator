﻿using System;
using System.Text.RegularExpressions;

namespace Piedone.Combinator.Models
{
    public interface ICombinatorSettings
    {
        Regex CombinationExcludeFilter { get; }
        bool CombineCDNResources { get; }
        Uri ResourceBaseUri { get; }
        bool MinifyResources { get; }
        Regex MinificationExcludeFilter { get; }
        bool EmbedCssImages { get; }
        int EmbeddedImagesMaxSizeKB { get; }
        Regex EmbedCssImagesStylesheetExcludeFilter { get; }
        bool GenerateImageSprites { get; }
        Regex[] ResourceSetFilters { get; }
    }
}
