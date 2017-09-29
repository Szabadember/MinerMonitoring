namespace CoreClient
{
    using System;
    using System.Reactive;
    using System.Reactive.Disposables;
    using System.Reactive.Linq;
    using RabbitMQ.Client;
    using CoreClientExtensions;

    public class RabbitProducer
    {
        public string Url { get; set; }
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string TopicPrefix { get; set; }
        public int RetryCount { get; set; }

        public RabbitProducer(
            string url,
            string exchangeName,
            string queueName,
            string topicPrefix,
            int retryCount)
        {
            this.Url = url;
            this.ExchangeName = exchangeName;
            this.QueueName = queueName;
            this.TopicPrefix = topicPrefix;
            this.RetryCount = retryCount;
        }

        public IObservable<Unit> SendMetric(string name, int value)
        {
            var obs = Observable.Create<Unit>((observer) => {
                try {
                    var routingKey = string.Format("{0}.{1}", this.TopicPrefix, name);
                    var valueStr = string.Format("{0}", value);
                    var messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(valueStr);
                    var factory = new ConnectionFactory();
                    factory.Uri = new Uri(this.Url);
                
                    using (var conn = factory.CreateConnection())
                    {
                        using (var ch = conn.CreateModel()) {
                            var props = ch.CreateBasicProperties();
                            ch.ExchangeDeclare(this.ExchangeName, ExchangeType.Topic);
                            ch.BasicPublish(this.ExchangeName,
                                            routingKey,
                                            props,
                                            messageBodyBytes);
                        }
                    }
                }
                catch (Exception e)
                {
                    observer.OnError(e);
                }

                observer.OnCompleted();
                return Disposable.Empty;
            });
            obs.RetryWithBackoffStrategy(this.RetryCount);

            return obs;
        }
    }

}
