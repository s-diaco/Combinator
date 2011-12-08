﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Orchard;
using Orchard.Environment;
using Orchard.Environment.Extensions;
using Orchard.UI.Resources;
using Piedone.Combinator.Extensions;
using Piedone.Combinator.Helpers;
using System.IO;
using System.Web;

namespace Piedone.Combinator.Models
{
    [OrchardFeature("Piedone.Combinator")]
    public class SmartResource : ISmartResource
    {
        private readonly IResourceManager _resourceManager;
        private readonly WorkContext _workContext;

        #region Path handling
        private string ApplicationPath
        {
            get
            {
                return _workContext.HttpContext.Request.ApplicationPath;
            }
        }

        private string FullPath
        {
            get
            {
                return RequiredContext.Resource.GetFullPath();
            }
        }

        public Uri PublicUrl
        {
            get
            {
                if (IsCDNResource) return new Uri(FullPath);
                else return new Uri(_workContext.HttpContext.Request.Url, PublicRelativeUrl);
            }
        }

        private string PublicRelativeUrl
        {
            get
            {
                return VirtualPathUtility.ToAbsolute(RelativeVirtualPath, ApplicationPath);
            }
        }

        public string RelativeVirtualPath
        {
            get
            {
                return VirtualPathUtility.ToAppRelative(FullPath, ApplicationPath);
            }
        }
        #endregion

        #region Public properties
        public ResourceRequiredContext RequiredContext { get; set; }

        public ResourceDefinition Resource
        {
            get { return RequiredContext.Resource; }
            set { RequiredContext.Resource = value; }
        }

        public RequireSettings Settings
        {
            get { return RequiredContext.Settings; }
            set { RequiredContext.Settings = value; }
        }

        public bool IsCDNResource
        {
            get
            {
                var fullPath = FullPath;

                return Uri.IsWellFormedUriString(fullPath, UriKind.Absolute)
                    && new Uri(fullPath).Host != _workContext.HttpContext.Request.Url.Host;
            }
        }

        public bool IsConditional
        {
            get { return !String.IsNullOrEmpty(RequiredContext.Settings.Condition); }
        }

        private Uri _urlOverride;
        public Uri UrlOverride
        {
            get { return _urlOverride; }
            set
            {
                _urlOverride = value;
                if (value != null && !String.IsNullOrEmpty(value.ToString()) && RequiredContext != null) Resource.SetUrl(value.ToString(), null);
            }
        }

        public ResourceType Type { get; set; }

        public string Content { get; set; }
        #endregion

        public SmartResource(
            IResourceManager resourceManager,
            IWorkContextAccessor workContextAccessor)
        {
            _resourceManager = resourceManager;
            _workContext = workContextAccessor.GetContext();
        }

        public ISmartResource FillRequiredContext(string publicUrl, ResourceType resourceType)
        {
            Type = resourceType;

            RequiredContext = new ResourceRequiredContext();

            // This is only necessary to build the ResourceRequiredContext object, therefore we also delete the resource
            // from the required ones.
            RequiredContext.Settings = _resourceManager.Include(ResourceTypeHelper.EnumToStringType(resourceType), publicUrl, publicUrl);
            RequiredContext.Resource = _resourceManager.FindResource(RequiredContext.Settings);
            _resourceManager.NotRequired(ResourceTypeHelper.EnumToStringType(resourceType), RequiredContext.Resource.Name);

            return this;
        }

        #region Serialization
        [DataContract]
        public class SerializableSettings
        {
            [DataMember]
            public Uri UrlOverride { get; set; }

            [DataMember]
            public string Culture { get; set; }

            [DataMember]
            public string Condition { get; set; }

            [DataMember]
            public Dictionary<string, string> Attributes { get; set; }
        }

        public bool SerializableSettingsEqual(ISmartResource other)
        {
            // If one's RequiredContext is null, their settings are not identical...
            if (RequiredContext == null ^ other.RequiredContext == null) return false;

            // However if both of them are null, we say the settings are identical
            if (RequiredContext == null && other.RequiredContext == null) return true;

            return
                UrlOverride == other.UrlOverride
                && Settings.Culture == other.Settings.Culture
                && Settings.Condition == other.Settings.Condition
                && Settings.AttributesEqual(other.Settings);
        }

        public string GetSerializedSettings()
        {
            return Piedone.HelpfulLibraries.Serialization.Helpers.Serializer.Serialize(
                new SerializableSettings()
                    {
                        UrlOverride = UrlOverride,
                        Culture = Settings.Culture,
                        Condition = Settings.Condition,
                        Attributes = Settings.Attributes
                    });
        }

        public void FillSettingsFromSerialization(string serialization)
        {
            if (String.IsNullOrEmpty(serialization)) return;

            var settings = Piedone.HelpfulLibraries.Serialization.Helpers.Serializer.Deserialize<SerializableSettings>(serialization);

            UrlOverride = settings.UrlOverride;
            Settings.Culture = settings.Culture;
            Settings.Condition = settings.Condition;
            Settings.Attributes = settings.Attributes;
        }
        #endregion
    }
}