using Grpc.Core;
using Grpc.Net.Client;
using IdentityModel.Client;
using ProductGrpc.Protos;
using ShoppingCartGrpc.Protos;

namespace ShoppingCartWorkerService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        

        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Waiting for service is runnning...");
            Thread.Sleep(2000);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                //0 Get token from IS4
                //1 CreatevSC if not exist
                //2 Retrieve products from product grpc with server stream
                //3 Add sc item SC with client stream
                using var scChannel = GrpcChannel.ForAddress(_configuration.GetValue<string>("WorkerService:ShoppingCartServerUrl"));
                var scClient = new ShoppingCartProtoService.ShoppingCartProtoServiceClient(scChannel);

                //0 Get token from IS4
                var token = await GetTokenFromIS4();

                //1 Create SC if not exist
                var scModel = await GetOrCreateShoppingCartAsync(scClient, token);

                //open sc client stream
                using var scClientStream = scClient.AddItemIntoShoppingCart();

                //2 Retrieve products from product grpc with server stream
                using var productChannel = GrpcChannel.ForAddress(_configuration.GetValue<string>("WorkerService:ProductServerUrl"));
                var productClient = new ProductProtoService.ProductProtoServiceClient(productChannel);

                _logger.LogInformation("GetAllProducts started..");
                using var clientData = productClient.GetAllProduct(new GetAllProductsRequest());
                await foreach(var responseData in clientData.ResponseStream.ReadAllAsync())
                {
                    _logger.LogInformation("GetAllProducts Stream Response:{responseData}", responseData);

                    //3 Add sc items into SC with client stream
                    var addNewScItem = new AddItemIntoShoppingCartRequest
                    {
                        Username = _configuration.GetValue<string>("WorkerService:UserName"),
                        DiscountCode = "CODE_100",
                        NewCartItem = new ShoppingCartItemModel
                        {
                            ProductId = responseData.ProductId,
                            Productname = responseData.Name,
                            Price = responseData.Price,
                            Color = "Black",
                            Quantity = 1
                        }
                    };

                    await scClientStream.RequestStream.WriteAsync(addNewScItem);
                    _logger.LogInformation("ShoppingCart Client Stream Added New Item : {addNewScItem}", addNewScItem);

                }

                await scClientStream.RequestStream.CompleteAsync();

                var addItemIntoShoppingCartResponse = await scClientStream;
                _logger.LogInformation("AddItemIntoShoppingCart Client Stream Response: {addItemIntoShoppingCartResponse}", addItemIntoShoppingCartResponse);

                await Task.Delay(_configuration.GetValue<int>("WorkerService:TaskInterval"), stoppingToken);
            }
        }

        private async Task<string> GetTokenFromIS4()
        {
            //discover endpoints from metadata
            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(_configuration.GetValue<string>("WorkerService:IdentityServerUrl"));

            if (disco.IsError)
            {
                Console.WriteLine(disco.Error);
                return string.Empty;
            }

            //request token
            var tokenResponse = await client.RequestClientCredentialsTokenAsync
                (new ClientCredentialsTokenRequest {
                    Address = disco.TokenEndpoint,
                    ClientId = "ShoppingCartClient",
                    ClientSecret = "secret",
                    Scope = "ShoppingCartAPI"
                });

            if (tokenResponse.IsError)
            {
                Console.WriteLine(tokenResponse.Error);
                return String.Empty;
            }

            return tokenResponse.AccessToken;
        }

        private async Task<ShoppingCartModel> GetOrCreateShoppingCartAsync(ShoppingCartProtoService.ShoppingCartProtoServiceClient scClient, string token)
        {
            ShoppingCartModel shoppingCartModel;

            try
            {
                _logger.LogInformation("GetShoppingCartAsync started..");

                var headers = new Metadata();
                headers.Add("Authorization",$"Bearer {token}");

                shoppingCartModel = await scClient.GetShoppingCartAsync(new GetShoppingCartRequest { Username = _configuration.GetValue<string>("WorkerService:UserName") }, headers);

                _logger.LogInformation("GetShoppingCartAsync Response : {shoppingCartModel}", shoppingCartModel);

            }
            catch (RpcException ex)
            {
                if(ex.StatusCode == StatusCode.NotFound)
                {
                    _logger.LogInformation("GetShoppingCartAsync started..");
                    shoppingCartModel = await scClient.CreateShoppingCartAsync(new ShoppingCartModel { Username = _configuration.GetValue<string>("WorkerService:UserName") });
                    _logger.LogInformation("GetShoppingCartAsync Response : {shoppingCartModel}", shoppingCartModel);
                }else
                { throw ex; }
            }

            return shoppingCartModel;
        }
    }
}