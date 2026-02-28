using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Models;

namespace SalesAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DealStrategyController : ControllerBase
{
    private readonly SalesDbContext _context;

    public DealStrategyController(SalesDbContext context)
    {
        _context = context;
    }

    [HttpGet("getCustomerSummary/{customerId:int}")]
    [EndpointSummary("Get customer summary")]
    [EndpointDescription("Returns account profile, order volume, revenue, average order value, and top product categories for a customer.")]
    [ProducesResponseType(typeof(CustomerSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerSummaryResponse>> GetCustomerSummary(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var orders = await _context.SalesOrders
            .Where(o => o.CustomerId == customerId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();

        var totalRevenue = orders.Sum(o => o.TotalAmountUsd ?? 0m);
        var orderCount = orders.Count;
        var avgOrderValue = orderCount == 0 ? 0m : totalRevenue / orderCount;

        var topCategories = orders
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.Product.ProductCategory ?? "Uncategorized")
            .Select(g => new CategorySpend(g.Key, g.Sum(x => x.LineTotalUsd ?? (x.Quantity * x.UnitPriceUsd))))
            .OrderByDescending(g => g.Amount)
            .Take(3)
            .ToList();

        var response = new CustomerSummaryResponse(
            customer.CustomerId,
            customer.CustomerName,
            customer.CustomerType,
            customer.Industry,
            customer.Country,
            customer.State,
            customer.City,
            orderCount,
            totalRevenue,
            avgOrderValue,
            orders.Select(o => o.OrderDate).DefaultIfEmpty().Max(),
            topCategories);

        return Ok(response);
    }

    [HttpGet("getCustomerOrderTrends/{customerId:int}")]
    [EndpointSummary("Get customer order trends")]
    [EndpointDescription("Returns monthly order count and revenue trend points for the requested customer and time range (for example 90d, 6m, 1y).")]
    [ProducesResponseType(typeof(CustomerOrderTrendResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerOrderTrendResponse>> GetCustomerOrderTrends(int customerId, [FromQuery] string? timeRange = "180d")
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var lookbackDays = ParseTimeRangeToDays(timeRange);
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-lookbackDays));

        var orders = await _context.SalesOrders
            .Where(o => o.CustomerId == customerId && o.OrderDate >= startDate)
            .AsNoTracking()
            .ToListAsync();

        var points = orders
            .GroupBy(o => new DateOnly(o.OrderDate.Year, o.OrderDate.Month, 1))
            .OrderBy(g => g.Key)
            .Select(g => new OrderTrendPoint(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalAmountUsd ?? 0m),
                g.Average(x => x.TotalAmountUsd ?? 0m)))
            .ToList();

        var trendDirection = "stable";
        if (points.Count >= 2)
        {
            var first = points.First().Revenue;
            var last = points.Last().Revenue;
            if (last > first * 1.1m)
            {
                trendDirection = "up";
            }
            else if (last < first * 0.9m)
            {
                trendDirection = "down";
            }
        }

        return Ok(new CustomerOrderTrendResponse(customerId, customer.CustomerName, timeRange ?? "180d", points, trendDirection));
    }

    [HttpGet("getCustomerProductMix/{customerId:int}")]
    [EndpointSummary("Get customer product mix")]
    [EndpointDescription("Returns spend distribution by product category and top purchased products for the customer.")]
    [ProducesResponseType(typeof(CustomerProductMixResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerProductMixResponse>> GetCustomerProductMix(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var orderItems = await _context.OrderItems
            .Where(oi => oi.Order.CustomerId == customerId)
            .Include(oi => oi.Product)
            .AsNoTracking()
            .ToListAsync();

        var totalAmount = orderItems.Sum(oi => oi.LineTotalUsd ?? (oi.Quantity * oi.UnitPriceUsd));

        var categories = orderItems
            .GroupBy(oi => oi.Product.ProductCategory ?? "Uncategorized")
            .Select(g =>
            {
                var amount = g.Sum(x => x.LineTotalUsd ?? (x.Quantity * x.UnitPriceUsd));
                var percent = totalAmount == 0 ? 0 : Math.Round((amount / totalAmount) * 100m, 2);
                return new ProductMixCategory(g.Key, amount, percent, g.Count());
            })
            .OrderByDescending(g => g.Amount)
            .ToList();

        var products = orderItems
            .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName })
            .Select(g =>
            {
                var amount = g.Sum(x => x.LineTotalUsd ?? (x.Quantity * x.UnitPriceUsd));
                var percent = totalAmount == 0 ? 0 : Math.Round((amount / totalAmount) * 100m, 2);
                return new ProductMixItem(g.Key.ProductId, g.Key.ProductName, amount, percent, g.Sum(x => x.Quantity));
            })
            .OrderByDescending(g => g.Amount)
            .Take(10)
            .ToList();

        return Ok(new CustomerProductMixResponse(customerId, customer.CustomerName, totalAmount, categories, products));
    }

    [HttpGet("getRegionalPerformance/{regionId}")]
    [EndpointSummary("Get regional performance")]
    [EndpointDescription("Returns aggregate order and revenue performance for a region, including top reps.")]
    [ProducesResponseType(typeof(RegionalPerformanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RegionalPerformanceResponse>> GetRegionalPerformance(string regionId)
    {
        var orders = await _context.SalesOrders
            .Where(o => o.SalesRep != null && o.SalesRep.Region != null && o.SalesRep.Region == regionId)
            .Include(o => o.SalesRep)
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .ToListAsync();

        if (orders.Count == 0)
        {
            return NotFound(new ErrorResponse($"No orders found for region '{regionId}'."));
        }

        var revenue = orders.Sum(o => o.TotalAmountUsd ?? 0m);
        var customers = orders.Select(o => o.CustomerId).Distinct().Count();
        var reps = orders.Where(o => o.SalesRepId.HasValue).Select(o => o.SalesRepId!.Value).Distinct().Count();

        var topReps = orders
            .Where(o => o.SalesRep is not null)
            .GroupBy(o => new { o.SalesRepId, o.SalesRep!.RepName })
            .Select(g => new RepPerformanceSnapshot(
                g.Key.SalesRepId ?? 0,
                g.Key.RepName,
                g.Count(),
                g.Sum(x => x.TotalAmountUsd ?? 0m),
                g.Average(x => x.TotalAmountUsd ?? 0m)))
            .OrderByDescending(g => g.TotalRevenue)
            .Take(5)
            .ToList();

        return Ok(new RegionalPerformanceResponse(regionId, orders.Count, revenue, customers, reps, topReps));
    }

    [HttpGet("getRepPerformance/{repId:int}")]
    [EndpointSummary("Get sales rep performance")]
    [EndpointDescription("Returns pipeline and win metrics for a sales rep, including top revenue customers.")]
    [ProducesResponseType(typeof(RepPerformanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RepPerformanceResponse>> GetRepPerformance(int repId)
    {
        var rep = await _context.SalesReps.FindAsync(repId);
        if (rep is null)
        {
            return NotFound(new ErrorResponse($"Sales rep {repId} was not found."));
        }

        var orders = await _context.SalesOrders
            .Where(o => o.SalesRepId == repId)
            .Include(o => o.OrderItems)
            .AsNoTracking()
            .ToListAsync();

        var revenue = orders.Sum(o => o.TotalAmountUsd ?? 0m);
        var winRate = orders.Count == 0
            ? 0m
            : Math.Round((decimal)orders.Count(o => string.Equals(o.OrderStatus, "Closed-Won", StringComparison.OrdinalIgnoreCase)) / orders.Count * 100m, 2);

        var avgDealSize = orders.Count == 0 ? 0m : revenue / orders.Count;

        var topCustomers = orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new CustomerRevenue(g.Key, g.Sum(x => x.TotalAmountUsd ?? 0m), g.Count()))
            .OrderByDescending(g => g.Revenue)
            .Take(5)
            .ToList();

        return Ok(new RepPerformanceResponse(rep.SalesRepId, rep.RepName, rep.Region, orders.Count, revenue, avgDealSize, winRate, topCustomers));
    }

    [HttpGet("calculateDealRisk/{customerId:int}")]
    [EndpointSummary("Calculate deal risk")]
    [EndpointDescription("Calculates a heuristic risk score and signal list for an account based on recency, trend, and product concentration.")]
    [ProducesResponseType(typeof(DealRiskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DealRiskResponse>> CalculateDealRisk(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var orders = await _context.SalesOrders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();

        if (orders.Count == 0)
        {
            return Ok(new DealRiskResponse(customerId, customer.CustomerName, 85, "high", new[]
            {
                "No historical orders found.",
                "No evidence of recent engagement.",
                "Recommend discovery call before proposing expansion."
            }));
        }

        var riskScore = 0;
        var signals = new List<string>();

        var mostRecent = orders.First().OrderDate;
        var daysSinceLastOrder = DateOnly.FromDateTime(DateTime.UtcNow.Date).DayNumber - mostRecent.DayNumber;
        if (daysSinceLastOrder > 120)
        {
            riskScore += 35;
            signals.Add($"Last order was {daysSinceLastOrder} days ago.");
        }
        else if (daysSinceLastOrder > 60)
        {
            riskScore += 20;
            signals.Add($"Last order was {daysSinceLastOrder} days ago.");
        }

        var recentWindowStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-90));
        var previousWindowStart = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-180));

        var recentRevenue = orders.Where(o => o.OrderDate >= recentWindowStart).Sum(o => o.TotalAmountUsd ?? 0m);
        var previousRevenue = orders.Where(o => o.OrderDate >= previousWindowStart && o.OrderDate < recentWindowStart).Sum(o => o.TotalAmountUsd ?? 0m);

        if (previousRevenue > 0)
        {
            var change = (recentRevenue - previousRevenue) / previousRevenue;
            if (change < -0.3m)
            {
                riskScore += 30;
                signals.Add("Revenue declined by more than 30% in the most recent 90 days.");
            }
            else if (change < -0.15m)
            {
                riskScore += 15;
                signals.Add("Revenue declined in the most recent 90 days.");
            }
        }

        var uniqueProducts = await _context.OrderItems
            .Where(oi => oi.Order.CustomerId == customerId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .CountAsync();

        if (uniqueProducts <= 2)
        {
            riskScore += 15;
            signals.Add("Portfolio concentration is high (2 or fewer products purchased).");
        }

        riskScore = Math.Clamp(riskScore, 0, 100);
        var riskLevel = riskScore >= 70 ? "high" : riskScore >= 40 ? "medium" : "low";

        if (signals.Count == 0)
        {
            signals.Add("Stable recent activity and diversified product adoption.");
        }

        return Ok(new DealRiskResponse(customerId, customer.CustomerName, riskScore, riskLevel, signals));
    }

    [HttpGet("identifyCrossSellOpportunities/{customerId:int}")]
    [EndpointSummary("Identify cross-sell opportunities")]
    [EndpointDescription("Finds products not yet purchased by the customer but commonly purchased by peer customers.")]
    [ProducesResponseType(typeof(CrossSellResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrossSellResponse>> IdentifyCrossSellOpportunities(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var purchasedProductIds = await _context.OrderItems
            .Where(oi => oi.Order.CustomerId == customerId)
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        var peerCustomerIds = await _context.Customers
            .Where(c => c.CustomerId != customerId &&
                        c.CustomerType == customer.CustomerType &&
                        c.Industry == customer.Industry)
            .Select(c => c.CustomerId)
            .Take(50)
            .ToListAsync();

        var opportunities = await _context.OrderItems
            .Where(oi => peerCustomerIds.Contains(oi.Order.CustomerId) && !purchasedProductIds.Contains(oi.ProductId))
            .Include(oi => oi.Product)
            .GroupBy(oi => new { oi.ProductId, oi.Product.ProductName, oi.Product.ProductCategory })
            .Select(g => new CrossSellOpportunity(
                g.Key.ProductId,
                g.Key.ProductName,
                g.Key.ProductCategory,
                g.Count(),
                Math.Round(g.Average(x => x.UnitPriceUsd), 2),
                BuildRationale(g.Key.ProductName, g.Key.ProductCategory)))
            .OrderByDescending(x => x.PeerPurchaseCount)
            .Take(8)
            .ToListAsync();

        return Ok(new CrossSellResponse(customerId, customer.CustomerName, opportunities));
    }

    [HttpGet("generateExecutiveSummary/{customerId:int}")]
    [EndpointSummary("Generate executive summary")]
    [EndpointDescription("Generates an account-level strategic narrative using summary metrics, deal risk, and top cross-sell recommendations.")]
    [ProducesResponseType(typeof(ExecutiveSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExecutiveSummaryResponse>> GenerateExecutiveSummary(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var summaryResult = await GetCustomerSummary(customerId);
        var riskResult = await CalculateDealRisk(customerId);
        var opportunitiesResult = await IdentifyCrossSellOpportunities(customerId);

        if (summaryResult.Result is not OkObjectResult summaryOk || summaryOk.Value is not CustomerSummaryResponse summary)
        {
            return StatusCode(500, new ErrorResponse("Failed to compute customer summary."));
        }

        if (riskResult.Result is not OkObjectResult riskOk || riskOk.Value is not DealRiskResponse risk)
        {
            return StatusCode(500, new ErrorResponse("Failed to compute risk profile."));
        }

        if (opportunitiesResult.Result is not OkObjectResult oppOk || oppOk.Value is not CrossSellResponse opportunities)
        {
            return StatusCode(500, new ErrorResponse("Failed to compute cross-sell opportunities."));
        }

        var topOpportunity = opportunities.Opportunities.FirstOrDefault();
        var topCategory = summary.TopCategories.FirstOrDefault()?.Category ?? "N/A";

        var narrative = $"{summary.CustomerName} has {summary.TotalOrders} orders totaling {summary.TotalRevenueUsd:C}. " +
                        $"Average order value is {summary.AverageOrderValueUsd:C}, with strongest activity in '{topCategory}'. " +
                        $"Current deal risk is {risk.RiskLevel} ({risk.RiskScore}/100). " +
                        (topOpportunity is null
                            ? "No clear cross-sell candidate was identified from peer behavior."
                            : $"Top cross-sell candidate is {topOpportunity.ProductName} ({topOpportunity.ProductCategory ?? "Uncategorized"}).");

        return Ok(new ExecutiveSummaryResponse(
            customerId,
            summary.CustomerName,
            narrative,
            risk,
            opportunities.Opportunities.Take(3).ToList()));
    }

    [HttpPost("createFollowUpTask/{customerId:int}")]
    [EndpointSummary("Create follow-up task")]
    [EndpointDescription("Creates a suggested follow-up task payload with generated priority and due date.")]
    [ProducesResponseType(typeof(FollowUpTaskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FollowUpTaskResponse>> CreateFollowUpTask(int customerId, [FromBody] FollowUpTaskRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return BadRequest(new ErrorResponse("Action is required."));
        }

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var risk = await CalculateDealRisk(customerId);
        var priority = "medium";

        if (risk.Result is OkObjectResult riskOk && riskOk.Value is DealRiskResponse riskPayload)
        {
            priority = riskPayload.RiskLevel == "high" ? "high" : riskPayload.RiskLevel == "low" ? "low" : "medium";
        }

        var task = new FollowUpTaskResponse(
            Guid.NewGuid().ToString("N"),
            customerId,
            customer.CustomerName,
            request.Action.Trim(),
            priority,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(priority == "high" ? 2 : 5),
            "draft");

        return Ok(task);
    }

    [HttpPost("draftCustomerEmail/{customerId:int}")]
    [EndpointSummary("Draft customer email")]
    [EndpointDescription("Generates a draft outbound email aligned to the provided strategy and account opportunities.")]
    [ProducesResponseType(typeof(DraftEmailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DraftEmailResponse>> DraftCustomerEmail(int customerId, [FromBody] DraftEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Strategy))
        {
            return BadRequest(new ErrorResponse("Strategy is required."));
        }

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer {customerId} was not found."));
        }

        var opportunities = await IdentifyCrossSellOpportunities(customerId);
        var opportunityName = "an additional product line";

        if (opportunities.Result is OkObjectResult oppOk && oppOk.Value is CrossSellResponse oppPayload)
        {
            opportunityName = oppPayload.Opportunities.FirstOrDefault()?.ProductName ?? opportunityName;
        }

        var subject = $"Next-step strategy for {customer.CustomerName}";
        var body =
            $"Hi {customer.CustomerName} team,\n\n" +
            $"Based on our recent account review, we recommend the following strategy: {request.Strategy.Trim()}.\n\n" +
            $"We also see strong potential to explore {opportunityName}, which has performed well for similar customers in your segment.\n\n" +
            "If useful, we can schedule a short working session this week to align on timeline, scope, and expected outcomes.\n\n" +
            "Best regards,\nYour Account Team";

        return Ok(new DraftEmailResponse(customerId, customer.CustomerName, subject, body));
    }

    private static int ParseTimeRangeToDays(string? timeRange)
    {
        if (string.IsNullOrWhiteSpace(timeRange))
        {
            return 180;
        }

        var value = timeRange.Trim().ToLowerInvariant();

        if (value.EndsWith("d") && int.TryParse(value[..^1], out var days))
        {
            return Math.Max(days, 1);
        }

        if (value.EndsWith("m") && int.TryParse(value[..^1], out var months))
        {
            return Math.Max(months * 30, 30);
        }

        if (value.EndsWith("y") && int.TryParse(value[..^1], out var years))
        {
            return Math.Max(years * 365, 365);
        }

        if (int.TryParse(value, out var numericDays))
        {
            return Math.Max(numericDays, 1);
        }

        return 180;
    }

    private static string BuildRationale(string productName, string? productCategory)
    {
        var categoryText = string.IsNullOrWhiteSpace(productCategory) ? "adjacent category" : productCategory;
        return $"Peers in the same segment frequently buy {productName} in {categoryText}.";
    }
}

public record CustomerSummaryResponse(
    int CustomerId,
    string CustomerName,
    string? CustomerType,
    string? Industry,
    string? Country,
    string? State,
    string? City,
    int TotalOrders,
    decimal TotalRevenueUsd,
    decimal AverageOrderValueUsd,
    DateOnly LastOrderDate,
    IReadOnlyList<CategorySpend> TopCategories);

public record CategorySpend(string Category, decimal Amount);

public record CustomerOrderTrendResponse(
    int CustomerId,
    string CustomerName,
    string TimeRange,
    IReadOnlyList<OrderTrendPoint> Points,
    string TrendDirection);

public record OrderTrendPoint(
    DateOnly Month,
    int OrderCount,
    decimal Revenue,
    decimal AverageOrderValue);

public record CustomerProductMixResponse(
    int CustomerId,
    string CustomerName,
    decimal TotalAmount,
    IReadOnlyList<ProductMixCategory> Categories,
    IReadOnlyList<ProductMixItem> TopProducts);

public record ProductMixCategory(
    string Category,
    decimal Amount,
    decimal SharePercent,
    int LineCount);

public record ProductMixItem(
    int ProductId,
    string ProductName,
    decimal Amount,
    decimal SharePercent,
    int TotalQuantity);

public record RegionalPerformanceResponse(
    string RegionId,
    int TotalOrders,
    decimal TotalRevenue,
    int DistinctCustomers,
    int ActiveSalesReps,
    IReadOnlyList<RepPerformanceSnapshot> TopReps);

public record RepPerformanceSnapshot(
    int RepId,
    string RepName,
    int OrderCount,
    decimal TotalRevenue,
    decimal AverageDealSize);

public record RepPerformanceResponse(
    int RepId,
    string RepName,
    string? Region,
    int TotalOrders,
    decimal TotalRevenue,
    decimal AverageDealSize,
    decimal WinRatePercent,
    IReadOnlyList<CustomerRevenue> TopCustomers);

public record CustomerRevenue(int CustomerId, decimal Revenue, int OrderCount);

public record DealRiskResponse(
    int CustomerId,
    string CustomerName,
    int RiskScore,
    string RiskLevel,
    IReadOnlyList<string> Signals);

public record CrossSellResponse(
    int CustomerId,
    string CustomerName,
    IReadOnlyList<CrossSellOpportunity> Opportunities);

public record CrossSellOpportunity(
    int ProductId,
    string ProductName,
    string? ProductCategory,
    int PeerPurchaseCount,
    decimal AvgPeerUnitPriceUsd,
    string Rationale);

public record ExecutiveSummaryResponse(
    int CustomerId,
    string CustomerName,
    string Narrative,
    DealRiskResponse Risk,
    IReadOnlyList<CrossSellOpportunity> TopCrossSellOpportunities);

public class FollowUpTaskRequest
{
    public string Action { get; set; } = string.Empty;
}

public record FollowUpTaskResponse(
    string TaskId,
    int CustomerId,
    string CustomerName,
    string Action,
    string Priority,
    DateTime CreatedUtc,
    DateTime DueUtc,
    string Status);

public class DraftEmailRequest
{
    public string Strategy { get; set; } = string.Empty;
}

public record DraftEmailResponse(
    int CustomerId,
    string CustomerName,
    string Subject,
    string Body);

public record ErrorResponse(string Message);