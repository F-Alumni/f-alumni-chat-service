using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ChatService.Services.DTOs.Common;
using ChatService.Services.DTOs.Requests;
using ChatService.Services.DTOs.Responses;
using ChatService.Services.Interfaces;

namespace ChatService.Api.Controllers;

/// <summary>
/// Sample REST API endpoints demonstrating standard routing, status codes, OpenAPI metadata, pagination, and JWT authorization.
/// </summary>
[ApiController]
[Route("api/v1/sample-items")]
[Produces("application/json")]
public class SampleItemsController : ControllerBase
{
    private readonly ISampleItemService _sampleItemService;
    private readonly ILogger<SampleItemsController> _logger;

    public SampleItemsController(
        ISampleItemService sampleItemService,
        ILogger<SampleItemsController> logger)
    {
        _sampleItemService = sampleItemService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a list of all sample items (Public endpoint).
    /// </summary>
    /// <response code="200">Returns the list of sample items.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<SampleItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SampleItemResponse>>> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET api/v1/sample-items requested.");
        var items = await _sampleItemService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>
    /// Retrieves a paginated and filtered list of sample items (Public endpoint).
    /// </summary>
    /// <param name="search">Optional search keyword matching item name or description.</param>
    /// <param name="pageNumber">1-based page number (default 1).</param>
    /// <param name="pageSize">Number of items per page (default 10, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the paginated list envelope with metadata.</response>
    [HttpGet("paged")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<SampleItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SampleItemResponse>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("HTTP GET api/v1/sample-items/paged requested. Search: {Search}, Page: {Page}, Size: {Size}", search, pageNumber, pageSize);
        var pagedResult = await _sampleItemService.GetPagedAsync(search, pageNumber, pageSize, cancellationToken);
        return Ok(pagedResult);
    }

    /// <summary>
    /// Retrieves a specific sample item by unique identifier (Public endpoint).
    /// </summary>
    /// <param name="id">Unique GUID identifier of the sample item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Returns the requested sample item.</response>
    /// <response code="404">Item with specified ID was not found.</response>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SampleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SampleItemResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("HTTP GET api/v1/sample-items/{Id} requested.", id);
        var item = await _sampleItemService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    /// <summary>
    /// Creates a new sample item resource. Requires authenticated USER or ADMIN role.
    /// </summary>
    /// <param name="request">Creation payload with name and optional description.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Item created successfully, returns the created item and Location header.</response>
    /// <response code="400">Request validation failed or payload invalid.</response>
    /// <response code="401">Missing or invalid JWT Bearer token.</response>
    /// <response code="403">Authenticated user does not have USER or ADMIN role.</response>
    [HttpPost]
    [Authorize(Roles = "USER,ADMIN")]
    [ProducesResponseType(typeof(SampleItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SampleItemResponse>> Create(
        [FromBody] CreateSampleItemRequest request,
        CancellationToken cancellationToken)
    {
        // Extract authenticated user identity safely from validated claims, NEVER from untrusted client headers
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        _logger.LogInformation("HTTP POST api/v1/sample-items requested by authenticated user {UserId} with name: {Name}", userId, request.Name);

        var created = await _sampleItemService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing sample item resource by ID. Requires authenticated USER or ADMIN role.
    /// </summary>
    /// <param name="id">Unique GUID identifier of the sample item to update.</param>
    /// <param name="request">Update payload with name, optional description, and active status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Item updated successfully, returns the updated resource.</response>
    /// <response code="400">Request validation failed or payload invalid.</response>
    /// <response code="401">Missing or invalid JWT Bearer token.</response>
    /// <response code="403">Authenticated user does not have USER or ADMIN role.</response>
    /// <response code="404">Item with specified ID was not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "USER,ADMIN")]
    [ProducesResponseType(typeof(SampleItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SampleItemResponse>> Update(
        Guid id,
        [FromBody] UpdateSampleItemRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        _logger.LogInformation("HTTP PUT api/v1/sample-items/{Id} requested by user {UserId} with name: {Name}", id, userId, request.Name);

        var updated = await _sampleItemService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes a sample item resource by ID. Requires ADMIN role.
    /// </summary>
    /// <param name="id">Unique GUID identifier of the sample item to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="204">Item deleted successfully.</response>
    /// <response code="401">Missing or invalid JWT Bearer token.</response>
    /// <response code="403">User does not have ADMIN role.</response>
    /// <response code="404">Item with specified ID was not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        _logger.LogInformation("HTTP DELETE api/v1/sample-items/{Id} requested by admin {AdminId}.", id, adminId);

        await _sampleItemService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Administrative privileged test endpoint. Requires ADMIN role.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnlyEndpoint()
    {
        return Ok(new { message = "Access granted to administrator.", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Authenticated profile claim extraction demonstration endpoint.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUserProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? User.FindFirst("role")?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;

        return Ok(new { userId, email, role });
    }
}
