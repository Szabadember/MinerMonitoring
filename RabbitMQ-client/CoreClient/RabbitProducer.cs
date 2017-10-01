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
            this.RetryCount = retryCount;
        }

        public IObservable<Unit> SendMetric(DateTime timestamp, string routingKey, long value)
        {
            var unixTimestamp = (Int64)(timestamp.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var obs = Observable.Create<Unit>((observer) => {
                try {
                    var valueStr = string.Format("{0}", value);
                    var messageBodyBytes = System.Text.Encoding.UTF8.GetBytes(valueStr);
                    var factory = new ConnectionFactory();
                    factory.Uri = new Uri(this.Url);
                
                    using (var conn = factory.CreateConnection())
                    {
                        using (var ch = conn.CreateModel()) {
                            var props = ch.CreateBasicProperties();
                            props.Timestamp = new AmqpTimestamp(unixTimestamp);
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
