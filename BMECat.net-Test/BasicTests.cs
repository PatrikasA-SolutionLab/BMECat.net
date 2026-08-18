using BMECat.net;
using System.Text;
using System.Text.Json;

namespace BMECat.net_Test
{
    [TestClass]
    public class BasicTests
    {
        [TestMethod]
        public void SimpleTest()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.Save("test.xml");
        }
        

        [TestMethod]
        public async Task SynchronousAsynchronousLoadingTest()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);


            ms.Position = 0;
            ProductCatalog synchronouslyLoadedCatalog = ProductCatalog.Load(ms);            
            string synchronouslyLoadedCatalogJson = JsonSerializer.Serialize(synchronouslyLoadedCatalog);

            ms.Position = 0;
            ProductCatalog asynchronouslyLoadedCatalog = await ProductCatalog.LoadAsync(ms);            
            string asynchronouslyLoadedCatalogJson = JsonSerializer.Serialize(synchronouslyLoadedCatalog);

            Assert.AreEqual(synchronouslyLoadedCatalogJson, asynchronouslyLoadedCatalogJson);

            Task.WaitAll();
        } // !SynchronousAsynchronousLoadingTest()


        [TestMethod]
        public async Task SynchronousAsynchronousSavingTest()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();


            MemoryStream msSynchronous = new MemoryStream();
            catalog.Save(msSynchronous);
            string  synchronousProductCatalog = System.Text.Encoding.UTF8.GetString(msSynchronous.ToArray());

            MemoryStream msAsynchronous = new MemoryStream();
            await catalog.SaveAsync(msAsynchronous);
            string asynchronousProductCatalog = System.Text.Encoding.UTF8.GetString(msAsynchronous.ToArray());
            
            Assert.AreEqual(synchronousProductCatalog, asynchronousProductCatalog);

            Task.WaitAll();
        } // !SynchronousAsynchronousLoadingTest()


        [TestMethod]
        public async Task RoundtripProductIdHandling_NoProductId()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();

            catalog.Products.First().PIds.Clear();

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);

            ms.Position = 0;
            ProductCatalog loadedCatalog = await ProductCatalog.LoadAsync(ms);
            Assert.AreEqual(loadedCatalog.Products.First().PIds.Count, 0);

            Task.WaitAll();
        }


        [TestMethod]
        public async Task RoundtripProductIdHandling_GTIN_only()
        {
            string testId = System.Guid.NewGuid().ToString();

            ProductCatalog catalog = _GenerateSimpleCatalog();

            catalog.Products.First().PIds.Clear();
            catalog.Products.First().PIds.Add(new ProductId() { Type = ProductIdTypes.GTIN, Id = testId });

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);

            ms.Position = 0;
            ProductCatalog loadedCatalog = await ProductCatalog.LoadAsync(ms);
            Assert.AreEqual(loadedCatalog.Products.First().PIds.Count, 1);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Type, ProductIdTypes.GTIN);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Id, testId);

            Task.WaitAll();
        }


        [TestMethod]
        public async Task RoundtripProductIdHandling_EAN_only()
        {
            string testId = System.Guid.NewGuid().ToString();

            ProductCatalog catalog = _GenerateSimpleCatalog();

            catalog.Products.First().PIds.Clear();
            catalog.Products.First().PIds.Add(new ProductId() { Type = ProductIdTypes.EAN, Id = testId });

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);

            ms.Position = 0;
            ProductCatalog loadedCatalog = await ProductCatalog.LoadAsync(ms);
            Assert.AreEqual(loadedCatalog.Products.First().PIds.Count, 1);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Type, ProductIdTypes.EAN);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Id, testId);

            Task.WaitAll();
        }


        [TestMethod]
        public async Task RoundtripProductIdHandling_supplier_specific_only()
        {
            string testId = System.Guid.NewGuid().ToString();

            ProductCatalog catalog = _GenerateSimpleCatalog();

            catalog.Products.First().PIds.Clear();
            catalog.Products.First().PIds.Add(new ProductId() { Type = ProductIdTypes.SupplierSpecific, Id = testId });

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);

            ms.Position = 0;
            ProductCatalog loadedCatalog = await ProductCatalog.LoadAsync(ms);
            Assert.AreEqual(loadedCatalog.Products.First().PIds.Count, 1);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Type, ProductIdTypes.SupplierSpecific);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Id, testId);

            Task.WaitAll();
        }


        [TestMethod]
        public async Task RoundtripProductIdHandling_EAN_GTIN()
        {
            string testId0 = System.Guid.NewGuid().ToString();
            string testId1 = System.Guid.NewGuid().ToString();

            ProductCatalog catalog = _GenerateSimpleCatalog();

            catalog.Products.First().PIds.Clear();
            catalog.Products.First().PIds.Add(new ProductId() { Type = ProductIdTypes.EAN, Id = testId0 });
            catalog.Products.First().PIds.Add(new ProductId() { Type = ProductIdTypes.GTIN, Id = testId1 });

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);

            ms.Position = 0;
            ProductCatalog loadedCatalog = await ProductCatalog.LoadAsync(ms);
            Assert.AreEqual(2, loadedCatalog.Products.First().PIds.Count);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Type, ProductIdTypes.EAN);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[0].Id, testId0);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[1].Type, ProductIdTypes.GTIN);
            Assert.AreEqual(loadedCatalog.Products.First().PIds[1].Id, testId1);

            Task.WaitAll();
        }


        private ProductCatalog _GenerateSimpleCatalog()
        {
            ProductCatalog catalog = new ProductCatalog()
            {
                Languages = { LanguageCodes.DEU },
                CatalogId = "QA_CAT_002",
                CatalogVersion = "001.002",
                CatalogName = "Office Material",
                GenerationDate = new System.DateTime(2004, 8, 20, 10, 59, 54),
                Currency = CurrencyCodes.EUR
            };

            catalog.Products.Add(new Product()
            {
                No = "Q20-P09",
                PIds = new List<ProductId>() { new ProductId() { Type = ProductIdTypes.EAN, Id = "0000000011" } },
                DescriptionShort = "Post-Safe Polythene Envelopes Deutsch",
                DescriptionLong = "Deutsch All-weather lightweight envelopes protect your contents and save you money. ALL - WEATHER.Once sealed, Post-Safe envelopes are completely waterproof.Your contents won't get damaged.",
                Stock = 100,
                Prices = new List<ProductPrice>()
                {
                    new ProductPrice()
                    {
                        Currency = CurrencyCodes.EUR,
                        Amount = 16.49m,
                        Tax = 0.19m
                    }
                }
            });

            return catalog;
        } // !_GenerateSimpleCatalog()


        [TestMethod]
        public async Task WriteParty_RoundTrip()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.Supplier = new Party
            {
                Id = "SUPP-001",
                Name = "Acme Corp",
                Street = "123 Main St",
                City = "Berlin",
                Zip = "10115",
                Country = "DE",
                Phone = "+49 30 12345"
            };

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);
            ms.Position = 0;
            ProductCatalog loaded = await ProductCatalog.LoadAsync(ms);

            Assert.IsNotNull(loaded.Supplier);
            Assert.AreEqual("Acme Corp", loaded.Supplier.Name);
            Assert.AreEqual("Berlin", loaded.Supplier.City);
        } // !WriteParty_RoundTrip()


        [TestMethod]
        public async Task WriteLogisticsDetails_RoundTrip()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.Products[0].LogisticsDetails = new LogisticsDetails
            {
                CountryOfOrigin = CountryCodes.DE,
                Weight = 1.5m,
                Length = 100m,
                Width = 50m,
                Depth = 20m,
                CustomsTariffNumber = new List<string> { "84713000" }
            };

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);
            ms.Position = 0;
            ProductCatalog loaded = await ProductCatalog.LoadAsync(ms);

            LogisticsDetails ld = loaded.Products[0].LogisticsDetails;
            Assert.IsNotNull(ld);
            Assert.AreEqual(CountryCodes.DE, ld.CountryOfOrigin);
            Assert.AreEqual(1.5m, ld.Weight);
            Assert.AreEqual(1, ld.CustomsTariffNumber?.Count);
            Assert.AreEqual("84713000", ld.CustomsTariffNumber[0]);
        } // !WriteLogisticsDetails_RoundTrip()


        [TestMethod]
        public async Task WriteMimeInfo_RoundTrip()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.Products[0].MimeInfos = new List<MimeInfo>
            {
                new MimeInfo
                {
                    MimeType = MimeTypes.ImageJpeg,
                    Source = "images/product.jpg",
                    Description = "Product photo",
                    Purpose = "normal",
                    Order = 1
                }
            };

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);
            ms.Position = 0;
            ProductCatalog loaded = await ProductCatalog.LoadAsync(ms);

            Assert.AreEqual(1, loaded.Products[0].MimeInfos?.Count);
            MimeInfo mime = loaded.Products[0].MimeInfos[0];
            Assert.AreEqual(MimeTypes.ImageJpeg, mime.MimeType);
            Assert.AreEqual("images/product.jpg", mime.Source);
            Assert.AreEqual("normal", mime.Purpose);
        } // !WriteMimeInfo_RoundTrip()


        [TestMethod]
        public async Task WriteFeatureSets_RoundTrip()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.Products[0].FeatureSets = new List<FeatureSet>
            {
                new FeatureSet
                {
                    FeatureClassificationSystem = new FeatureClassificationSystem
                    {
                        Classification = "ECLASS",
                        GroupName = "Office Supplies"
                    },
                    Features = new List<Feature>
                    {
                        new Feature { Name = "Color", Values = new List<string> { "Blue" } },
                        new Feature { Name = "Width", Values = new List<string> { "210" } }
                    }
                }
            };

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);
            ms.Position = 0;
            ProductCatalog loaded = await ProductCatalog.LoadAsync(ms);

            Assert.AreEqual(1, loaded.Products[0].FeatureSets?.Count);
            FeatureSet fs = loaded.Products[0].FeatureSets[0];
            Assert.AreEqual("ECLASS", fs.FeatureClassificationSystem?.Classification);
            Assert.AreEqual(2, fs.Features?.Count);
            Assert.AreEqual("Color", fs.Features[0].Name);
            Assert.AreEqual("Blue", fs.Features[0].Values?[0]);
        } // !WriteFeatureSets_RoundTrip()


        [TestMethod]
        public async Task WriteCatalogStructures_RoundTrip()
        {
            ProductCatalog catalog = _GenerateSimpleCatalog();
            catalog.CatalogStructures = new List<CatalogStructure>
            {
                new CatalogStructure
                {
                    Type = CatalogStructureTypes.Node,
                    GroupId = "CAT-1",
                    GroupName = "Electronics",
                    GroupOrder = "1"
                },
                new CatalogStructure
                {
                    Type = CatalogStructureTypes.Leaf,
                    GroupId = "CAT-1-1",
                    GroupName = "Laptops",
                    ParentId = "CAT-1",
                    GroupOrder = "1"
                }
            };

            MemoryStream ms = new MemoryStream();
            catalog.Save(ms);
            ms.Position = 0;
            ProductCatalog loaded = await ProductCatalog.LoadAsync(ms);

            Assert.AreEqual(2, loaded.CatalogStructures?.Count);
            CatalogStructure root = loaded.CatalogStructures.First(s => s.GroupId == "CAT-1");
            Assert.AreEqual("Electronics", root.GroupName);
            CatalogStructure leaf = loaded.CatalogStructures.First(s => s.GroupId == "CAT-1-1");
            Assert.AreEqual("CAT-1", leaf.ParentId);
        } // !WriteCatalogStructures_RoundTrip()
    }
}
