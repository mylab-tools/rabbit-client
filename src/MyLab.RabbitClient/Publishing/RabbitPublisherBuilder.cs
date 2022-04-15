using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyLab.Log.Dsl;
using MyLab.RabbitClient.Connection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using IBasicProperties = RabbitMQ.Client.IBasicProperties;

namespace MyLab.RabbitClient.Publishing
{
    /// <summary>
    /// Build parameters for publishing and perform publish
    /// </summary>
    public class RabbitPublisherBuilder
    {
        private readonly IPublisherBuilderStrategy _builderStrategy;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly IEnumerable<IPublishingContext> _pubContexts;

        private readonly byte[] _content;

        readonly List<Action<IBasicProperties>> _configActions;
        readonly IDictionary<string, object> _headers;

        /// <summary>
        /// Logger
        /// </summary>
        public IDslLogger Log { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitPublisherBuilder"/>
        /// </summary>
        public RabbitPublisherBuilder(
            IPublisherBuilderStrategy builderStrategy,
            string exchange, 
            string routingKey,
            IEnumerable<IPublishingContext> pubContexts)
        {
            _builderStrategy = builderStrategy;
            _exchange = exchange;
            _routingKey = routingKey;
            _pubContexts = pubContexts;

            _configActions = new List<Action<IBasicProperties>>();
            _headers = new Dictionary<string, object>();
            _content = default;
        }

        RabbitPublisherBuilder(RabbitPublisherBuilder initial,
            byte[] newContent,
            List<Action<IBasicProperties>> newConfigActions,
            IDictionary<string, object> newHeaders)
        {
            _builderStrategy = initial._builderStrategy;
            _exchange = initial._exchange;
            _routingKey = initial._routingKey;
            _pubContexts = initial._pubContexts;

            if (_exchange == null && _routingKey == null)
                throw new InvalidOperationException("No one target parameter specified");

            _content = newContent;
            _headers = new Dictionary<string, object>(newHeaders);
            _configActions = new List<Action<IBasicProperties>>(newConfigActions);
        }

        /// <summary>
        /// Adds property assignation
        /// </summary>
        public RabbitPublisherBuilder AndProperty(Action<IBasicProperties> propAct)
        {
            if (propAct == null) throw new ArgumentNullException(nameof(propAct));
            
            var propActs = new List<Action<IBasicProperties>>(_configActions){ propAct };

            return new RabbitPublisherBuilder(this, _content, propActs, _headers)
            {
                Log = Log
            };
        }

        /// <summary>
        /// Add header
        /// </summary>
        public RabbitPublisherBuilder AndHeader(string name, object value)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (value == null) throw new ArgumentNullException(nameof(value));

            var newHeaders = new Dictionary<string,object>(_headers)
            {
                { name, value }
            };

            return new RabbitPublisherBuilder(this, _content, _configActions, newHeaders)
            {
                Log = Log
            };
        }

        /// <summary>
        /// Use object as json as message content
        /// </summary>
        public RabbitPublisherBuilder SendJson(object obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var json = JsonConvert.SerializeObject(obj, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            var newContent = Encoding.UTF8.GetBytes(json);

            return new RabbitPublisherBuilder(this, newContent, _configActions, _headers)
            {
                Log = Log
            };
        }

        /// <summary>
        /// Use binary as message content
        /// </summary>
        public RabbitPublisherBuilder SendBinary(byte[] binData)
        {
            if (binData == null) throw new ArgumentNullException(nameof(binData));

            return new RabbitPublisherBuilder(this, binData, _configActions, _headers)
            {
                Log = Log
            };
        }

        /// <summary>
        /// Use string as message content
        /// </summary>
        public RabbitPublisherBuilder SendString(string strData)
        {
            if (strData == null) throw new ArgumentNullException(nameof(strData));

            var newContent = Encoding.UTF8.GetBytes(strData);
            return new RabbitPublisherBuilder(this, newContent, _configActions, _headers)
            {
                Log = Log
            };
        }

        /// <summary>
        /// Publish message
        /// </summary>
        public void Publish()
        {
            if(_content == null)
                throw new InvalidOperationException("Content not specified");

            _builderStrategy.Use(pBuilderUsing =>
            {
                var basicProperties = pBuilderUsing.CreateBasicProperties();

                basicProperties.ContentType = "application/json";
                basicProperties.Headers = _headers;

                foreach (var configAction in _configActions)
                    configAction(basicProperties);

                
                var publishingMessage = new RabbitPublishingMessage
                {
                    Content = _content,
                    RoutingKey = _routingKey,
                    Exchange = _exchange,
                    BasicProperties = basicProperties
                };

                var publishingContexts = new List<IDisposable>();

                if (_pubContexts != null)
                {
                    var pubContexts = _pubContexts
                        .Select(f => f.Set(publishingMessage))
                        .Where(c => c != null);

                    publishingContexts.AddRange(pubContexts);
                }

                try
                {
                    pBuilderUsing.Publish(
                        publishingMessage.Exchange ?? "",
                        publishingMessage.RoutingKey ?? "",
                        publishingMessage.BasicProperties,
                        publishingMessage.Content
                    );
                }
                finally
                {
                    foreach (var ctx in publishingContexts)
                    {
                        try
                        {
                            ctx.Dispose();
                        }
                        catch (Exception e)
                        {
                            Log?.Error("Closing rabbit publishing context error", e).Write();
                        }
                    }
                }
            });
        }
    }
}
