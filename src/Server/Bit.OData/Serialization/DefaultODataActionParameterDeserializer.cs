﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Web.OData.Formatter.Deserialization;
using Bit.Core.Contracts;
using Bit.OData.ODataControllers;
using Bit.Owin.Contracts;
using Microsoft.OData;
using Microsoft.Owin;
using Newtonsoft.Json;
using System.Web.Http.Controllers;

namespace Bit.OData.Serialization
{
    public class DefaultODataActionParameterDeserializer : ODataDeserializer
    {
        private readonly ODataJsonDeSerializerStringCorrector _stringFormatterConvert;
        private readonly ODataJsonDeSerializerEnumConverter _odataJsonDeserializerEnumConverter;

        protected DefaultODataActionParameterDeserializer()
            : base(ODataPayloadKind.Parameter)
        {

        }

        public DefaultODataActionParameterDeserializer(ODataDeserializerProvider deserializerProvider, System.Web.Http.Dependencies.IDependencyResolver dependencyResolver)
            : base(ODataPayloadKind.Parameter)
        {
            if (dependencyResolver == null)
                throw new ArgumentNullException(nameof(dependencyResolver));

            _stringFormatterConvert = new ODataJsonDeSerializerStringCorrector(dependencyResolver.GetServices(typeof(IStringCorrector).GetTypeInfo()).Cast<IStringCorrector>().ToArray());
            _odataJsonDeserializerEnumConverter = new ODataJsonDeSerializerEnumConverter();
        }

        public override object Read(ODataMessageReader messageReader, Type type, ODataDeserializerContext readContext)
        {
            HttpActionDescriptor actionDescriptor = readContext.Request.GetActionDescriptor();

            if (actionDescriptor != null && !actionDescriptor.GetCustomAttributes<ActionAttribute>().Any())
                throw new InvalidOperationException($"{nameof(DefaultODataActionParameterDeserializer)} is designed for odata actions only");

            TypeInfo typeInfo = type.GetTypeInfo();

            IDependencyResolver dependencyResolver = readContext.Request.GetOwinContext()
                .GetDependencyResolver();

            ITimeZoneManager timeZoneManager = dependencyResolver.Resolve<ITimeZoneManager>();

            using (StreamReader requestStreamReader = new StreamReader(readContext.Request.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
            {
                using (JsonTextReader requestJsonReader = new JsonTextReader(requestStreamReader))
                {
                    JsonSerializer deserilizer = JsonSerializer.Create(new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        DateFormatHandling = DateFormatHandling.IsoDateFormat,
                        Converters = new JsonConverter[]
                        {
                            _odataJsonDeserializerEnumConverter,
                            _stringFormatterConvert,
                            new ODataJsonDeSerializerDateTimeOffsetTimeZone(timeZoneManager)
                        }
                    });

                    object result = deserilizer.Deserialize(requestJsonReader, typeInfo);

                    return result;
                }
            }
        }
    }

    internal class ODataJsonDeSerializerDateTimeOffsetTimeZone : JsonConverter
    {
        private readonly ITimeZoneManager _timeZoneManager;

        public ODataJsonDeSerializerDateTimeOffsetTimeZone(ITimeZoneManager timeZoneManager)
        {
            _timeZoneManager = timeZoneManager;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(DateTimeOffset) || Nullable.GetUnderlyingType(objectType) == typeof(DateTimeOffset);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            DateTimeOffset objAsDateTimeOffset = (DateTime)reader.Value;

            objAsDateTimeOffset = _timeZoneManager.MapFromClientToServer(objAsDateTimeOffset);

            return objAsDateTimeOffset;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal class ODataJsonDeSerializerStringCorrector : JsonConverter
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        public ODataJsonDeSerializerStringCorrector(IEnumerable<IStringCorrector> stringCorrectors)
        {
            _stringCorrectors = stringCorrectors;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            string rawString = (string)reader.Value;

            foreach (IStringCorrector stringCorrector in _stringCorrectors)
            {
                rawString = stringCorrector.CorrectString(rawString);
            }

            return rawString;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal class ODataJsonDeSerializerEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum || Nullable.GetUnderlyingType(objectType)?.IsEnum == true;
        }

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null || reader.Value == null)
                return null;

            Type unwrappedType = objectType.IsEnum ? objectType : Nullable.GetUnderlyingType(objectType);

            if (unwrappedType == null)
                throw new InvalidOperationException($"{nameof(unwrappedType)} is null");

            string fullName = unwrappedType.FullName;

            string enumRawValue = (string)reader.Value;

            enumRawValue = enumRawValue.Replace(fullName, string.Empty).Replace("'", string.Empty);

            return Enum.Parse(unwrappedType, enumRawValue);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
