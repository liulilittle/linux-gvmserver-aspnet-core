namespace GVMServer.Ns.Mq
{
    using RabbitMQ.Client;

    public class RabbitConnectionManager
    {
        public static IConnection GetConnection()
        {
            ConnectionFactory factory = new ConnectionFactory() { AutomaticRecoveryEnabled = true };
            factory.UserName = "guest";
            factory.Password = "guest";
            factory.VirtualHost = "/";
            factory.HostName = "172.0.6.138";
            return factory.CreateConnection();
        }
    }
}
