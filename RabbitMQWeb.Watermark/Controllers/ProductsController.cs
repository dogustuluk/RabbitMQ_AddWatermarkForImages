using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RabbitMQWeb.Watermark.Models;
using RabbitMQWeb.Watermark.Services;

namespace RabbitMQWeb.Watermark.Controllers
{
    public class ProductsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly RabbitMQPublisher _rabbitMQPublisher; //event'i fırlatmak için yazmamız gerekmekte

        public ProductsController(AppDbContext context, RabbitMQPublisher rabbitMQPublisher)
        {
            _context = context;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            return View(await _context.Products.ToListAsync());
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Price,Stock,ImageName")] Product product, IFormFile ImageFile)
        {
            if (!ModelState.IsValid) return View(product); //kontrolü en başta yapıyoruz çünkü aşağıya geçildiğinde kod okunabilirliğinin artmasını istiyoruz.

            if (ImageFile is {Length:>0 }) 
            {
                //Guid ile rastgele string bir ifade oluşuyor.
                //rastgele oluşan string ifadenin bir de uzantısı olması gerekiyor. Gerekli olan uzantıyı Path ile dosyadan alıyoruz.
                //GetExtension >>> Dosyanın sadece uzantısını almaya yarayan metot.
                var randomImageName = Guid.NewGuid() + Path.GetExtension(ImageFile.FileName); // Path.GetExtension(ImageFile.FileName) >>> kodu ile ".jpg" ".png" kısmı gelir. "." nokta koymaya gerek olmaz

                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", randomImageName); //Dosyanın nereye kaydedileceğini belirttiğimiz kod

                await using FileStream stream = new(path, FileMode.Create); //C# 9.0'la gelen özellik sayesinde "new" dedikten sonra "FileStream" yazmamıza gerek yok.
                //c# 9.0 ile eğer sol tarafta tip belli ise sağ tarafta direkt olarak new anahtar sözcüğü ile devam edebiliriz.
                //FileMode.Create ile yeni bir dosya oluşturacağımızı belli ediyoruz.
                //henüz dosya oluşmuyor bu kod ile

                
                await ImageFile.CopyToAsync(stream); //resmi "stream"e kopyalamış olduk

                _rabbitMQPublisher.Publish(new ProductImageCreatedEvent() { ImageName = randomImageName  }); //event'i fırlatmış olduk

                product.ImageName = randomImageName;
            }

            
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Price,Stock,PictureUrl")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(product);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
    }
}
