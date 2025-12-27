using Microsoft.AspNetCore.Mvc;
using test_service.Data;

namespace test_service.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MedicineController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MedicineController> _logger;

    public MedicineController(ApplicationDbContext context, ILogger<MedicineController> logger)
    {
    _context = context;
        _logger = logger;
    }
}
