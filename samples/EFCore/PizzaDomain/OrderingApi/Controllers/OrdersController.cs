using Microsoft.AspNetCore.Mvc;
using OrderingApi.ApplicationServices;

namespace OrderingApi.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<Guid> Post(OrderService.SubmitOrderCommand command)
    {
        var order = await _orderService.SubmitOrder(command);
        return order.Id;
    }
}