﻿using Digimezzo.Utilities.Helpers;
using Digimezzo.Utilities.Log;
using Dopamine.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dopamine.Core.IO
{
    public class DecodeSmartPlaylistResult
    {
        public OperationResult DecodeResult { get; set; }

        public string PlaylistName { get; set; }

        public string Match { get; set; }

        public string Order { get; set; }

        public SmartPlaylistLimit Limit { get; set; }

        public IList<SmartPlaylistRule> Rules { get; set; }
    }

    public class SmartPlaylistRule
    {
        public string Field { get; private set; }

        public string Operator { get; private set; }

        public string Value { get; private set; }

        public SmartPlaylistRule(string field, string @operator, string value)
        {
            this.Field = field;
            this.Operator = @operator;
            this.Value = value;
        }
    }

    public enum SmartPlaylistLimitType
    {
        Songs = 1,
        GigaBytes = 2,
        MegaBytes = 3,
        Minutes = 4
    }

    public class SmartPlaylistLimit
    {
        public SmartPlaylistLimitType Type { get; private set; }

        public string DisplayName { get; private set; }

        public int Value { get; private set; }

        public SmartPlaylistLimit(SmartPlaylistLimitType type, int value)
        {
            this.Type = type;
            this.DisplayName = TypeToString(type);
            this.Value = value;
        }

        public SmartPlaylistLimit(SmartPlaylistLimitType type, string displayName)
        {
            this.Type = type;
            this.DisplayName = displayName;
            this.Value = 0;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }

        public static string TypeToString(SmartPlaylistLimitType type)
        {
            switch (type)
            {
                case SmartPlaylistLimitType.Songs:
                    return "songs";
                case SmartPlaylistLimitType.GigaBytes:
                    return "GB";
                case SmartPlaylistLimitType.MegaBytes:
                    return "MB";
                case SmartPlaylistLimitType.Minutes:
                    return "minutes";
                default:
                    return "songs";
            }
        }

        public static SmartPlaylistLimitType StringToType(string typeString)
        {
            switch (typeString)
            {
                case "songs":
                    return SmartPlaylistLimitType.Songs;
                case "GB":
                    return SmartPlaylistLimitType.GigaBytes;
                case "MB":
                    return SmartPlaylistLimitType.MegaBytes;
                case "minutes":
                    return SmartPlaylistLimitType.Minutes;
                default:
                    return SmartPlaylistLimitType.Songs;
            }
        }
    }

    public class SmartPlaylistDecoder
    {
        public DecodeSmartPlaylistResult DecodePlaylist(string fileName)
        {
            if (!System.IO.Path.GetExtension(fileName.ToLower()).Equals(FileFormats.DSPL))
            {
                return new DecodeSmartPlaylistResult { DecodeResult = new OperationResult { Result = false } };
            }

            OperationResult decodeResult = new OperationResult { Result = true };

            string playlistName = string.Empty;
            string match = string.Empty;
            string order = string.Empty;
            SmartPlaylistLimit limit = new SmartPlaylistLimit(SmartPlaylistLimitType.Songs, 0);
            IList<SmartPlaylistRule> rules = new List<SmartPlaylistRule>();

            try
            {
                XDocument xdoc = XDocument.Load(fileName);

                // Name
                XElement nameElement = (from t in xdoc.Element("smartplaylist").Elements("name")
                                        select t).FirstOrDefault();

                playlistName = nameElement != null ? nameElement.Value : string.Empty;

                // Match
                XElement matchElement = (from t in xdoc.Element("smartplaylist").Elements("match")
                                         select t).FirstOrDefault();

                match = matchElement != null ? matchElement.Value : string.Empty;

                // Order
                XElement orderElement = (from t in xdoc.Element("smartplaylist").Elements("order")
                                         select t).FirstOrDefault();

                order = orderElement != null ? orderElement.Value : string.Empty;

                // Limit
                XElement limitElement = (from t in xdoc.Element("smartplaylist").Elements("limit")
                                         select t).FirstOrDefault();

                if(limitElement != null && !string.IsNullOrEmpty(limitElement.Attribute("type").Value))
                {
                    int limitValue = 0;

                    if(limitElement.Attribute("type") != null && int.TryParse(limitElement.Attribute("type").Value, out limitValue))
                    {
                        limit = new SmartPlaylistLimit(SmartPlaylistLimit.StringToType(limitElement.Attribute("type").Value), limitValue);
                    }
                }

                // Rules
                IList<XElement> ruleElements = (from t in xdoc.Element("smartplaylist").Elements("rule")
                                                select t).ToList();

                if (ruleElements == null || ruleElements.Count == 0)
                {
                    throw new Exception($"{nameof(ruleElements)} is null or contains no elements");
                }

                foreach (XElement ruleElement in ruleElements)
                {
                    rules.Add(new SmartPlaylistRule(ruleElement.Attribute("field").Value, ruleElement.Attribute("operator").Value, ruleElement.Value));
                }
            }
            catch (Exception ex)
            {
                LogClient.Error($"Could not decode smart playlist '{fileName}'. Exception: {ex.Message}");
                decodeResult.Result = false;
            }

            return new DecodeSmartPlaylistResult
            {
                DecodeResult = decodeResult,
                PlaylistName = playlistName,
                Match = match,
                Order = order,
                Limit = limit,
                Rules = rules
            };
        }
    }
}
