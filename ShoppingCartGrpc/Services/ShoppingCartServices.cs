﻿using AutoMapper;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShoppingCartGrpc.Data;
using ShoppingCartGrpc.Models;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartGrpc.Services;

[Authorize]
public class ShoppingCartServices : ShoppingCartProtoService.ShoppingCartProtoServiceBase
{
    private readonly ShoppingCartContext _shoppingCartDbContext;
    private readonly DiscountService _discountService;
    private readonly IMapper _mapper;
    private readonly ILogger<ShoppingCartServices> _logger;

    public ShoppingCartServices(ShoppingCartContext shoppingCartDbContext, ILogger<ShoppingCartServices> logger, IMapper mapper, DiscountService discountService)
    {
        _shoppingCartDbContext = shoppingCartDbContext ?? throw new ArgumentNullException(nameof(shoppingCartDbContext));
        _discountService = discountService ?? throw new ArgumentNullException(nameof(discountService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mapper = mapper;        
    }

    public override async Task<ShoppingCartModel> GetShoppingCart(GetShoppingCartRequest request,
        ServerCallContext context) {

        var shoppingCart = await _shoppingCartDbContext.ShoppingCart
            .FirstOrDefaultAsync(s => s.UserName == request.Username);

        if (shoppingCart == null){
            throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with Username = { request.Username }"));
        }

        var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart); new ShoppingCartModel();

        return shoppingCartModel;
    }

    public override async Task<ShoppingCartModel> CreateShoppingCart(ShoppingCartModel request, ServerCallContext context)
    {
        var shoppingCart = _mapper.Map<ShoppingCart>(request);
        var isExist = await _shoppingCartDbContext.ShoppingCart
            .AnyAsync(s => s.UserName == shoppingCart.UserName);

        if (isExist) {
            _logger.LogError("Invalid Username for ShoppingCart creation. Username: {username}", shoppingCart.UserName);
            throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with UserName={request.Username} is already exist."));

        }

        _shoppingCartDbContext.ShoppingCart.Add(shoppingCart);
        await _shoppingCartDbContext.SaveChangesAsync();

        _logger.LogInformation("ShoppingCart is successfully created.UserName : {userName}", shoppingCart.UserName);

        var shoppingCartModel = _mapper.Map<ShoppingCartModel>(shoppingCart);
        return shoppingCartModel;
    }

    [AllowAnonymous]
    public override async Task<RemoveItemIntoShoppingCartResponse> RemoveItemIntoShoppingCart(RemoveItemIntoShoppingCartRequest request, ServerCallContext context)
    {
        // Get sc if exist or not
        // Check item if exist in sc or not
        // Remove item in SC db

        var shoppingCart = await _shoppingCartDbContext.ShoppingCart
            .FirstOrDefaultAsync(s => s.UserName == request.Username);

        if (shoppingCart == null) {
            throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with Username{request.Username}"));
        }

        var removeCartItem = shoppingCart.Items
            .FirstOrDefault(i => i.ProductId == request.RemoveCartItem.ProductId);

        if(null == removeCartItem)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"CartItem with ProductId={request.RemoveCartItem.ProductId} is not found in the ShoppingCart."));
        }

        shoppingCart.Items.Remove(removeCartItem);
        var removeCount = await _shoppingCartDbContext.SaveChangesAsync();

        var response = new RemoveItemIntoShoppingCartResponse { Success = removeCount > 0 };

        return response;
    }

    [AllowAnonymous]
    public override async Task<AddItemIntoShoppingCartResponse> AddItemIntoShoppingCart(IAsyncStreamReader<AddItemIntoShoppingCartRequest> requestStream, ServerCallContext context)
    {
        // Get sc if exist or not
        // Check the item if exist in sc or not
        // If item is exist +1 quantity
        // If item is not exist add new item into sc
        // Check discount and calculate the item price

        while(await requestStream.MoveNext())
        {
            var shoppingCart = await _shoppingCartDbContext.ShoppingCart
            .FirstOrDefaultAsync(s => s.UserName == requestStream.Current.Username);

            if (shoppingCart == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"ShoppingCart with username {requestStream.Current.Username} is not found."));
            }

            var newAddedCartItem = _mapper.Map<ShoppingCartItems>(requestStream.Current.NewCartItem);
            var cartItem = shoppingCart.Items.FirstOrDefault(i => i.ProductId == newAddedCartItem.ProductId);

            if(null != cartItem)
            {
                cartItem.Quantity++;
            }
            else
            {
                // grpc call discount service -- check discount and calculate the item last price
                var discount = await _discountService.GetDiscount(requestStream.Current.DiscountCode);
                newAddedCartItem.Price -= discount.Amount;
                shoppingCart.Items.Add(newAddedCartItem);
            }
        }

        var insertCount = await _shoppingCartDbContext.SaveChangesAsync();
        var response = new AddItemIntoShoppingCartResponse { 
            Success = insertCount > 0,
            InsertCount = insertCount
        };

        return response;
    }
}

