using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQWeb.Watermark.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQWeb.Watermark.BackgroundServices
{
    public class ImageWatermarkProcessBackgroundService : BackgroundService
    {
        private readonly RabbitMQClientService _rabbitMQClientService; //alıyoruz çünkü bu class'tan bir kanal gelmesi lazım
        private readonly ILogger _logger;
        private IModel _channel; //readonly olarak yazmamamızın sebebi bunu constructor'da set etmeyecek oluşumuz.
        //readonly olanlar constructor'da set edilebilir.

        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitMQClientService, ILogger<ImageWatermarkProcessBackgroundService> logger)
        {
            _rabbitMQClientService = rabbitMQClientService;
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        { //RabbitMQ'ya bağlanıldı
            _channel = _rabbitMQClientService.Connect();
            _channel.BasicQos(0, 1, false);

            return base.StartAsync(cancellationToken);
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        { //RabbitMQ dinleniyor
            var consumer = new AsyncEventingBasicConsumer(_channel);

            _channel.BasicConsume(RabbitMQClientService.QueueName, false, consumer);

            consumer.Received += Consumer_Received; //burada lambda ile kodlama yapmadık çünkü çok fazla kod olacak, dolayısıyla ayrı bir method olarak yazmamız 
            //okunabilirlik açısından daha iyi olacaktır.

            return Task.CompletedTask;
        }

        private Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        { //Resme image ekleme işlemi bu method'ta yapılmaktadır.
            //Task.Delay(5000).Wait(); //5sn gecikmeli çalışması için

            try
            {
                //@event ile byte dizini gelecek, onu string'e çeviriyoruz
                var ProductImageCreatedEvent = JsonSerializer.Deserialize<ProductImageCreatedEvent> //deserialize ediyoruz çünkü gelen data Json formatında, onun bir object
                    //veri tipinde olması lazım. Deserialize ile object veri tipine dönüştürmüş oluyoruz.
                (Encoding.UTF8.GetString(@event.Body.ToArray())); //buradan "wwwroot/Images" içerisindeki resmin ismi geliyor

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", ProductImageCreatedEvent.ImageName); //Varolan resmi çekmek için gerekli kod.

                var siteName = "www.RabbitDogus.com";

                using var img = Image.FromFile(path); //path'teki resmi alma işlemi için gerekli kod

                using var graphic = Graphics.FromImage(img); //resme yazı yazabilmek için bir grafik oluşturmak gereklidir. Bunun için gerekli olan kod.

                var font = new Font(FontFamily.GenericMonospace, 32, FontStyle.Bold, GraphicsUnit.Pixel); //yazılan yazının font'unu ayarlamak için gerekli olan kod.

                var textSize = graphic.MeasureString(siteName, font); //font'un boyutunu almak için gerekli kod. yazılacak olan yazının kaç piksel uzunlukta olduğunu bulur.

                var color = Color.FromArgb(128, 255, 255, 255);
                var brush = new SolidBrush(color); //yazı yazma işlemi gerçekleştirmek için gerekli olan kod

                var position = new Point(img.Width - ((int)textSize.Width + 30), img.Height - ((int)textSize.Height + 30)); //yazılacak olan yazının nereye yazılacağını belirten kod

                graphic.DrawString(siteName, font, brush, position); // çizim yapıldı

                img.Save("wwwroot/images/watermarks/"+ ProductImageCreatedEvent.ImageName);

                img.Dispose(); //image'ın bellekte yer kaplamaması için dispose ediyoruz
                graphic.Dispose(); //graphic'in bellekte yer kaplamaması için dispose ediyoruz

                _channel.BasicAck(@event.DeliveryTag, false);

               
            }
            catch (Exception ex)
            {

                _logger.LogError(ex.Message);
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //RabbitMQ ile bağlantı kesilir.
            //RabbitMQClientService'te rabbitMQ dispose edilecek. Dolayısıyla burada tekrardan dispose etmeye gerek yok, zaten uygulama kapandığında dispose olacak.
            return base.StopAsync(cancellationToken);
        }
    }
}
