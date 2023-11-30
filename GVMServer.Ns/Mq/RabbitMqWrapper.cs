namespace GVMServer.Ns.Mq.Publisher
{
    using System;
    using System.Text;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    public class RabbitMqWrapper
    {
        private EventingBasicConsumer m_Consumer = null;
        private string m_ChannelName = null;
        private IConnection m_Connection = null;
        private IModel m_ChannelModel = null;

        public RabbitMqWrapper(string exchange, string channel, string routingkey, bool supportConsumer = true, bool durableQueue = true, bool autoAck = true)
        {
            this.m_ChannelName = channel;
            this.m_Connection = RabbitConnectionManager.GetConnection();
            this.m_ChannelModel = this.m_Connection.CreateModel();

            this.m_ChannelModel.ExchangeDeclare(exchange: exchange, type: ExchangeType.Direct);
            this.m_ChannelModel.QueueDeclare(queue: channel,
                durable: durableQueue, // 持久化队列
                exclusive: false,
                autoDelete: false);
            this.m_ChannelModel.QueueBind(queue: channel, exchange: exchange, routingKey: routingkey);
            if (supportConsumer)
            {
                this.m_Consumer = new EventingBasicConsumer(this.m_ChannelModel);
                this.m_Consumer.Received += OnConsumerReceived;
                this.m_ChannelModel.BasicConsume(queue: channel,
                                    autoAck: autoAck,
                                    consumer: this.m_Consumer);
            }
        }

        private void OnConsumerReceived(object sender, BasicDeliverEventArgs e)
        {
            this.OnMessage(e);
        }

        protected virtual void OnMessage(BasicDeliverEventArgs e)
        {

        }

        public virtual bool Publish(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            return this.Publish(buffer);
        }

        public virtual bool Publish(byte[] message)
        {
            try
            {
                this.m_ChannelModel.BasicPublish(string.Empty, this.m_ChannelName, null, message);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual bool Ack(ulong deliveryTag)
        {
            try
            {
                this.m_ChannelModel.BasicAck(deliveryTag: deliveryTag,
                    multiple: false);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
