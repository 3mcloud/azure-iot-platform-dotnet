// <copyright file="JsonFieldsCollector.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Mmm.Iot.Common.Services.Helpers
{
    public class JsonFieldsCollector
    {
        private readonly Dictionary<string, JValue> fields;

        public JsonFieldsCollector(JToken token)
        {
            this.fields = new Dictionary<string, JValue>();
            this.CollectFields(token);
        }

        public Dictionary<string, JValue> GetAllFields()
        {
            return this.fields;
        }

        private void CollectFields(JToken jToken)
        {
            switch (jToken.Type)
            {
                case JTokenType.Object:
                    foreach (var child in jToken.Children<JProperty>())
                    {
                        this.CollectFields(child);
                    }

                    break;
                case JTokenType.Array:
                    foreach (var child in jToken.Children())
                    {
                        this.CollectFields(child);
                    }

                    break;
                case JTokenType.Property:
                    this.CollectFields(((JProperty)jToken).Value);
                    break;
                default:
                    this.fields.Add(jToken.Path, (JValue)jToken);
                    break;
            }
        }
    }
}