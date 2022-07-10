using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using ProductGrpc.Dara;
using ProductGrpc.Protos;
using Google.Protobuf.WellKnownTypes;
using ProductGrpc.Models;
using AutoMapper;

namespace ProductGrpc.Services;
public class ProductService : ProductProtoService.ProductProtoServiceBase
{
    private readonly Dara.ProductContext _productContext;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(ProductContext productContext, ILogger<ProductService> logger, IMapper mapper)
    {
        _productContext = productContext;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper;
    }

    public override Task<Empty> Test(Empty request, ServerCallContext context)
    {
        return base.Test(request, context);
    }

    public override async Task<ProductModel> GetProduct(GetProductRequest request, ServerCallContext context)
    {
        var product = await _productContext.Product.FindAsync(request.ProductId);

        if(product == null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={request.ProductId} is not found"));
        }

        //var productModel = new ProductModel
        //{
        //    ProductId = product.ProductId,
        //    Name = product.Name,
        //    Description = product.Description,
        //    Price = product.Price,
        //    Status = ProductStatus.Instock,
        //    CreatedTime = Timestamp.FromDateTime(product.CreatedTime)
        //};

        var productModel = _mapper.Map<ProductModel>(product);

        return productModel;
    }

    public override async Task GetAllProduct(GetAllProductsRequest request, IServerStreamWriter<ProductModel> responseStream, ServerCallContext context)
    {
        var productList = await _productContext.Product.ToListAsync();

        foreach (var product in productList)
        {
            //var productModel = new ProductModel
            //{
            //    ProductId = product.ProductId,
            //    Name = product.Name,
            //    Description = product.Description,
            //    Price = product.Price,
            //    Status = Protos.ProductStatus.Instock,
            //    CreatedTime = Timestamp.FromDateTime(product.CreatedTime)
            //};

            var productModel = _mapper.Map<ProductModel>(product);
            await responseStream.WriteAsync(productModel);
        }
    }

    public override async Task<ProductModel> AddProduct(AddProductRequest request, ServerCallContext context)
    {
        //var product = new Product
        //{
        //    ProductId = request.Product.ProductId,
        //    Name = request.Product.Name,
        //    Description = request.Product.Description,
        //    Price = request.Product.Price,
        //    Status = Models.ProductStatus.INSTOCK,
        //    CreatedTime = request.Product.CreatedTime.ToDateTime()
        //};

        var product = _mapper.Map<Product>(request.Product);
        _productContext.Product.Add(product);
        await _productContext.SaveChangesAsync();
        _logger.LogInformation($"Product Successfully added: productId: {product.ProductId}, productName: {product.Name}");

        //var productModel = new ProductModel
        //{
        //    ProductId = product.ProductId,
        //    Name = product.Name,
        //    Description = product.Description,
        //    Price = product.Price,
        //    Status = Protos.ProductStatus.Instock,
        //    CreatedTime = Timestamp.FromDateTime(product.CreatedTime)
        //};

        var productModel = _mapper.Map<ProductModel>(product);
        return productModel;
    }

    public override async Task<ProductModel> UpdateProduct(UpdateProductRequest request, ServerCallContext context)
    {
        var product = _mapper.Map<Product>(request.Product);

        bool isExist = await _productContext.Product.AnyAsync(p => p.ProductId == product.ProductId);

        if (!isExist) {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={request.Product.ProductId} is not found"));
        }

        _productContext.Entry(product).State = EntityState.Modified;

        try
        {
            await _productContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {

            throw;
        }

        var productModel = _mapper.Map<ProductModel>(product);
        return productModel;
    }

    public override async Task<DeleteProductResponse> DeleteProduct(DeleteProductRequest request, ServerCallContext context)
    {
       var product = await _productContext.Product.FindAsync(request.ProductId);
        if (product == null) {
            throw new RpcException(new Status(StatusCode.NotFound, $"Product with ID={request.ProductId} is not found"));
        }

        _productContext.Product.Remove(product);
        var deleteCount = await _productContext.SaveChangesAsync();

        var response = new DeleteProductResponse
        {
            Success = deleteCount > 0,
        };

        return response;
    }

    public override async Task<InsertBulkProductResponse> InsertBulkProduct(IAsyncStreamReader<ProductModel> requestStream, ServerCallContext context)
    {
        while(await requestStream.MoveNext())
        {
            var product = _mapper.Map<Product>(requestStream.Current);
            _productContext.Product.Add(product);
        }

        var insertCount = await _productContext.SaveChangesAsync();

        var response = new InsertBulkProductResponse
        {
            Success = insertCount > 0,
            InsertCount = insertCount,
        };

        return response;
    }
}

