using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using ProductGrpc.Protos;

Console.WriteLine("Waiting for server is running...");
Thread.Sleep(2000);

using var channel = GrpcChannel.ForAddress("https://localhost:7138");
var client = new ProductProtoService.ProductProtoServiceClient(channel);

await GetProductAsync(client);
await GetAllProducts(client);
await AddProductAsync(client);
await UpdateProductAsync(client);
await DeleteProductAsync(client);

await GetAllProducts(client);
await InsertBulkProductAsync(client);
await GetAllProducts(client);

Console.ReadKey();

#region Methods

async Task GetProductAsync(ProductProtoService.ProductProtoServiceClient client)
{
    //GetProductAsync
    Console.WriteLine("GetProductAsync started ..");
    var response = await client.GetProductAsync(
            new GetProductRequest
            {
                ProductId = 1
            });

    Console.WriteLine($"GetProductAsync Response : {response.ToString()}");
}

async Task GetAllProducts(ProductProtoService.ProductProtoServiceClient client)
{

    //GetAllProducts
    //Console.WriteLine("GetAllProducts started...");
    //using (var clientData = client.GetAllProduct(new GetAllProductsRequest()))
    //{
    //    while(await clientData.ResponseStream.MoveNext(new System.Threading.CancellationToken()))
    //    {
    //        var currentProduct = clientData.ResponseStream.Current;
    //        Console.WriteLine(currentProduct);
    //    }
    //}

    //GetAllProduct with c# 9
    Console.WriteLine("GetAllProducts with c# 9 started...");
    using var clientData = client.GetAllProduct(new GetAllProductsRequest());
    await foreach (var responseData in clientData.ResponseStream.ReadAllAsync())
    {
        Console.WriteLine(responseData);
    }

    Console.WriteLine("GetAllProduct end");
}

async Task AddProductAsync(ProductProtoService.ProductProtoServiceClient client)
{
    Console.WriteLine("AddProductAsync Started...");
    var addProductResponse = await client.AddProductAsync(
        new AddProductRequest
        {
            Product = new ProductModel
            {
                Name = "Red",
                Description = "New Red Phone Mi10T",
                Price = 699,
                Status = ProductStatus.Instock,
                CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow),
            }
        });

    Console.WriteLine($"AddProduct Response: {addProductResponse.ToString()}");
}

async Task UpdateProductAsync(ProductProtoService.ProductProtoServiceClient client)
{
    //Update product async
    Console.WriteLine("UpdateProductAsync started...");
    var updateProductResponse = await client.UpdateProductAsync(
            new UpdateProductRequest
            {
                Product = new ProductModel
                {
                    ProductId = 1,
                    Name = "Red",
                    Description = "New Red Phone Mi10T",
                    Price = 699,
                    Status = ProductStatus.Instock,
                    CreatedTime= Timestamp.FromDateTime(DateTime.UtcNow)
                }
            }
        );

    Console.WriteLine($"UpdateProductAsync Response : {updateProductResponse.ToString()}");
}

async Task DeleteProductAsync(ProductProtoService.ProductProtoServiceClient client)
{
    //DeleteProductAsync
    Console.WriteLine("DeleteProductAsync Started...");
    var deleteProductResponse = await client.DeleteProductAsync(
            new DeleteProductRequest
            {
                ProductId = 3
            }
        );

    Console.WriteLine($"DeleteProductAsync Response : {deleteProductResponse.Success.ToString()}");
    Thread.Sleep(1000);
}

async Task InsertBulkProductAsync(ProductProtoService.ProductProtoServiceClient client)
{
    //InsertBulkProduct
    Console.WriteLine("InsertBulkProduct Started...");

    using var clientBulk = client.InsertBulkProduct();

    for(int i = 0; i < 3; i++)
    {
        var productModel = new ProductModel
        {
            Name = $"Prodiuct{i}",
            Description = "Bulk inserted product",
            Price = 399,
            Status = ProductStatus.Instock,
            CreatedTime = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        await clientBulk.RequestStream.WriteAsync(productModel);

    }

    await clientBulk.RequestStream.CompleteAsync();

    var responseBulk = await clientBulk;
    Console.WriteLine($"Status: {responseBulk.Success}. Insert Count: {responseBulk.InsertCount}");
}
#endregion