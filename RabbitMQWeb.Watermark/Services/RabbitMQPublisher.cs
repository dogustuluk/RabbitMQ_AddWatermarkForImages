using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RabbitMQWeb.Watermark.Services
{
    public class RabbitMQPublisher
    {
        private readonly RabbitMQClientService _rabbitMQClientService;

        public RabbitMQPublisher(RabbitMQClientService rabbitMQClientService)
        {
            _rabbitMQClientService = rabbitMQClientService;
        }

        public void Publish(ProductImageCreatedEvent productImageCreatedEvent) //publish metodu event yollayacağı için "ProductImageCreatedEvent" parametresini alacak.
        {
            var channel = _rabbitMQClientService.Connect(); //kanalı çalıştırmak için

            var bodyString = JsonSerializer.Serialize(productImageCreatedEvent); //rabbitMQ'ya gönderilen mesajı serialize ediyoruz

            var bodyByte = Encoding.UTF8.GetBytes(bodyString); //mesajı byte'a çeviriyoruz.

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true; //mesajın memory'de durmaması, fiziksel olarak tutulmasını istediğimiz için true yapıyoruz

            channel.BasicPublish(exchange: RabbitMQClientService.ExchangeName, routingKey: RabbitMQClientService.RoutingWatermark, basicProperties: properties, body: bodyByte);

        }
    }
}
