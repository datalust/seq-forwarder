// Copyright Andreas Håkansson, Steven Robbins and contributors
// MIT License https://github.com/NancyFx/Nancy/blob/master/license.txt

using System.Collections.Generic;
using System.IO;
using Nancy;
using Nancy.IO;
using Newtonsoft.Json;

namespace Seq.Forwarder.Web.Formats
{
    public class JsonNetSerializer : ISerializer
    {
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetSerializer"/> class.
        /// </summary>
        public JsonNetSerializer()
        {
            _serializer = new JsonSerializer();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetSerializer"/> class,
        /// with the provided <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">Json converters used when serializing.</param>
        public JsonNetSerializer(JsonSerializer serializer)
        {
            this._serializer = serializer;
        }

        /// <summary>
        /// Whether the serializer can serialize the content type
        /// </summary>
        /// <param name="contentType">Content type to serialise</param>
        /// <returns>True if supported, false otherwise</returns>
        public bool CanSerialize(string contentType)
        {
            return Helpers.IsJsonType(contentType);
        }

        /// <summary>
        /// Gets the list of extensions that the serializer can handle.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> of extensions if any are available, otherwise an empty enumerable.</value>
        public IEnumerable<string> Extensions
        {
            get { yield return "json"; }
        }

        /// <summary>
        /// Serialize the given model with the given contentType
        /// </summary>
        /// <param name="contentType">Content type to serialize into</param>
        /// <param name="model">Model to serialize</param>
        /// <param name="outputStream">Output stream to serialize to</param>
        /// <returns>Serialised object</returns>
        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(new UnclosableStreamWrapper(outputStream))))
            {
                _serializer.Serialize(writer, model);               
            }
        }
    }
}